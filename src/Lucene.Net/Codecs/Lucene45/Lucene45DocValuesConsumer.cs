using Lucene.Net.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene45
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using BlockPackedWriter = Lucene.Net.Util.Packed.BlockPackedWriter;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MathUtil = Lucene.Net.Util.MathUtil;
    using MonotonicBlockPackedWriter = Lucene.Net.Util.Packed.MonotonicBlockPackedWriter;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using StringHelper = Lucene.Net.Util.StringHelper;

    /// <summary>
    /// Writer for <see cref="Lucene45DocValuesFormat"/> </summary>
    public class Lucene45DocValuesConsumer : DocValuesConsumer // LUCENENET specific - removed IDisposable, it is already implemented in base class
    {
        internal const int BLOCK_SIZE = 16384;
        internal const int ADDRESS_INTERVAL = 16;
        internal const long MISSING_ORD = -1L;

        /// <summary>
        /// Compressed using packed blocks of <see cref="int"/>s. </summary>
        public const int DELTA_COMPRESSED = 0;

        /// <summary>
        /// Compressed by computing the GCD. </summary>
        public const int GCD_COMPRESSED = 1;

        /// <summary>
        /// Compressed by giving IDs to unique values. </summary>
        public const int TABLE_COMPRESSED = 2;

        /// <summary>
        /// Uncompressed binary, written directly (fixed length). </summary>
        public const int BINARY_FIXED_UNCOMPRESSED = 0;

        /// <summary>
        /// Uncompressed binary, written directly (variable length). </summary>
        public const int BINARY_VARIABLE_UNCOMPRESSED = 1;

        /// <summary>
        /// Compressed binary with shared prefixes </summary>
        public const int BINARY_PREFIX_COMPRESSED = 2;

        /// <summary>
        /// Standard storage for sorted set values with 1 level of indirection:
        /// docId -> address -> ord.
        /// </summary>
        public static readonly int SORTED_SET_WITH_ADDRESSES = 0;

        /// <summary>
        /// Single-valued sorted set values, encoded as sorted values, so no level
        /// of indirection: docId -> ord.
        /// </summary>
        public static readonly int SORTED_SET_SINGLE_VALUED_SORTED = 1;

#pragma warning disable CA2213 // Disposable fields should be disposed
        internal IndexOutput data, meta;
#pragma warning restore CA2213 // Disposable fields should be disposed
        internal readonly int maxDoc;

        /// <summary>
        /// Expert: Creates a new writer. </summary>
        public Lucene45DocValuesConsumer(SegmentWriteState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension)
        {
            bool success = false;
            try
            {
                string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, dataExtension);
                data = state.Directory.CreateOutput(dataName, state.Context);
                CodecUtil.WriteHeader(data, dataCodec, Lucene45DocValuesFormat.VERSION_CURRENT);
                string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
                meta = state.Directory.CreateOutput(metaName, state.Context);
                CodecUtil.WriteHeader(meta, metaCodec, Lucene45DocValuesFormat.VERSION_CURRENT);
                maxDoc = state.SegmentInfo.DocCount;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(this);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
        {
            AddNumericField(field, values, true);
        }

        internal virtual void AddNumericField(FieldInfo field, IEnumerable<long?> values, bool optimizeStorage)
        {
            long count = 0;
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            long gcd = 0;
            bool missing = false;
            // TODO: more efficient?
            JCG.HashSet<long> uniqueValues = null;
            
            if (optimizeStorage)
            {
                uniqueValues = new JCG.HashSet<long>();

                foreach (long? nv in values)
                {
                    long v;
                    if (nv is null)
                    {
                        v = 0;
                        missing = true;
                    }
                    else
                    {
                        v = nv.Value;
                    }

                    if (gcd != 1)
                    {
                        if (v < long.MinValue / 2 || v > long.MaxValue / 2)
                        {
                            // in that case v - minValue might overflow and make the GCD computation return
                            // wrong results. Since these extreme values are unlikely, we just discard
                            // GCD computation for them
                            gcd = 1;
                        } // minValue needs to be set first
                        else if (count != 0)
                        {
                            gcd = MathUtil.Gcd(gcd, v - minValue);
                        }
                    }

                    minValue = Math.Min(minValue, v);
                    maxValue = Math.Max(maxValue, v);

                    if (uniqueValues != null)
                    {
                        if (uniqueValues.Add(v))
                        {
                            if (uniqueValues.Count > 256)
                            {
                                uniqueValues = null;
                            }
                        }
                    }

                    ++count;
                }
            }
            else
            {
                foreach (var nv in values)
                {
                    ++count;
                }
            }

            long delta = maxValue - minValue;

            int format;
            if (uniqueValues != null && (delta < 0L || PackedInt32s.BitsRequired(uniqueValues.Count - 1) < PackedInt32s.BitsRequired(delta)) && count <= int.MaxValue)
            {
                format = TABLE_COMPRESSED;
            }
            else if (gcd != 0 && gcd != 1)
            {
                format = GCD_COMPRESSED;
            }
            else
            {
                format = DELTA_COMPRESSED;
            }
            meta.WriteVInt32(field.Number);
            meta.WriteByte((byte)Lucene45DocValuesFormat.NUMERIC);
            meta.WriteVInt32(format);
            if (missing)
            {
                meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                WriteMissingBitset(values);
            }
            else
            {
                meta.WriteInt64(-1L);
            }
            meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
            meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            meta.WriteVInt64(count);
            meta.WriteVInt32(BLOCK_SIZE);

            switch (format)
            {
                case GCD_COMPRESSED:
                    meta.WriteInt64(minValue);
                    meta.WriteInt64(gcd);
                    BlockPackedWriter quotientWriter = new BlockPackedWriter(data, BLOCK_SIZE);
                    foreach (long? nv in values)
                    {
                        quotientWriter.Add((nv.GetValueOrDefault() - minValue) / gcd);
                    }
                    quotientWriter.Finish();
                    break;

                case DELTA_COMPRESSED:
                    BlockPackedWriter writer = new BlockPackedWriter(data, BLOCK_SIZE);
                    foreach (long? nv in values)
                    {
                        writer.Add(nv.GetValueOrDefault());
                    }
                    writer.Finish();
                    break;

                case TABLE_COMPRESSED:
                    // LUCENENET NOTE: diming an array and then using .CopyTo() for better efficiency than LINQ .ToArray()
                    long[] decode = new long[uniqueValues.Count];
                    uniqueValues.CopyTo(decode, 0);
                    Dictionary<long, int> encode = new Dictionary<long, int>();
                    meta.WriteVInt32(decode.Length);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        meta.WriteInt64(decode[i]);
                        encode[decode[i]] = i;
                    }
                    int bitsRequired = PackedInt32s.BitsRequired(uniqueValues.Count - 1);
                    PackedInt32s.Writer ordsWriter = PackedInt32s.GetWriterNoHeader(data, PackedInt32s.Format.PACKED, (int)count, bitsRequired, PackedInt32s.DEFAULT_BUFFER_SIZE);
                    foreach (long? nv in values)
                    {
                        ordsWriter.Add(encode[nv.GetValueOrDefault()]);
                    }
                    ordsWriter.Finish();
                    break;

                default:
                    throw AssertionError.Create();
            }
        }

        // TODO: in some cases representing missing with minValue-1 wouldn't take up additional space and so on,
        // but this is very simple, and algorithms only check this for values of 0 anyway (doesnt slow down normal decode)
        internal virtual void WriteMissingBitset(IEnumerable values)
        {
            sbyte bits = 0;
            int count = 0;
            foreach (object v in values)
            {
                if (count == 8)
                {
                    data.WriteByte((byte)bits);
                    count = 0;
                    bits = 0;
                }
                if (v != null)
                {
                    bits |= (sbyte)(1 << (count & 7));
                }
                count++;
            }
            if (count > 0)
            {
                data.WriteByte((byte)bits);
            }
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // write the byte[] data
            meta.WriteVInt32(field.Number);
            meta.WriteByte((byte)Lucene45DocValuesFormat.BINARY);
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;
            long startFP = data.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            long count = 0;
            bool missing = false;
            foreach (BytesRef v in values)
            {
                int length;
                if (v is null)
                {
                    length = 0;
                    missing = true;
                }
                else
                {
                    length = v.Length;
                }
                minLength = Math.Min(minLength, length);
                maxLength = Math.Max(maxLength, length);
                if (v != null)
                {
                    data.WriteBytes(v.Bytes, v.Offset, v.Length);
                }
                count++;
            }
            meta.WriteVInt32(minLength == maxLength ? BINARY_FIXED_UNCOMPRESSED : BINARY_VARIABLE_UNCOMPRESSED);
            if (missing)
            {
                meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                WriteMissingBitset(values);
            }
            else
            {
                meta.WriteInt64(-1L);
            }
            meta.WriteVInt32(minLength);
            meta.WriteVInt32(maxLength);
            meta.WriteVInt64(count);
            meta.WriteInt64(startFP);

            // if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
            // otherwise, we need to record the length fields...
            if (minLength != maxLength)
            {
                meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                meta.WriteVInt32(BLOCK_SIZE);

                MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(data, BLOCK_SIZE);
                long addr = 0;
                foreach (BytesRef v in values)
                {
                    if (v != null)
                    {
                        addr += v.Length;
                    }
                    writer.Add(addr);
                }
                writer.Finish();
            }
        }

        /// <summary>
        /// Expert: writes a value dictionary for a sorted/sortedset field. </summary>
        protected virtual void AddTermsDict(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // first check if its a "fixed-length" terms dict
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;
            foreach (BytesRef v in values)
            {
                minLength = Math.Min(minLength, v.Length);
                maxLength = Math.Max(maxLength, v.Length);
            }
            if (minLength == maxLength)
            {
                // no index needed: direct addressing by mult
                AddBinaryField(field, values);
            }
            else
            {
                // header
                meta.WriteVInt32(field.Number);
                meta.WriteByte((byte)Lucene45DocValuesFormat.BINARY);
                meta.WriteVInt32(BINARY_PREFIX_COMPRESSED);
                meta.WriteInt64(-1L);
                // now write the bytes: sharing prefixes within a block
                long startFP = data.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                // currently, we have to store the delta from expected for every 1/nth term
                // we could avoid this, but its not much and less overall RAM than the previous approach!
                RAMOutputStream addressBuffer = new RAMOutputStream();
                MonotonicBlockPackedWriter termAddresses = new MonotonicBlockPackedWriter(addressBuffer, BLOCK_SIZE);
                BytesRef lastTerm = new BytesRef();
                long count = 0;
                foreach (BytesRef v in values)
                {
                    if (count % ADDRESS_INTERVAL == 0)
                    {
                        termAddresses.Add(data.Position - startFP); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        // force the first term in a block to be abs-encoded
                        lastTerm.Length = 0;
                    }

                    // prefix-code
                    int sharedPrefix = StringHelper.BytesDifference(lastTerm, v);
                    data.WriteVInt32(sharedPrefix);
                    data.WriteVInt32(v.Length - sharedPrefix);
                    data.WriteBytes(v.Bytes, v.Offset + sharedPrefix, v.Length - sharedPrefix);
                    lastTerm.CopyBytes(v);
                    count++;
                }
                long indexStartFP = data.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                // write addresses of indexed terms
                termAddresses.Finish();
                addressBuffer.WriteTo(data);
                //addressBuffer = null; // LUCENENET: IDE0059: Remove unnecessary value assignment
                //termAddresses = null; // LUCENENET: IDE0059: Remove unnecessary value assignment
                meta.WriteVInt32(minLength);
                meta.WriteVInt32(maxLength);
                meta.WriteVInt64(count);
                meta.WriteInt64(startFP);
                meta.WriteVInt32(ADDRESS_INTERVAL);
                meta.WriteInt64(indexStartFP);
                meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                meta.WriteVInt32(BLOCK_SIZE);
            }
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            meta.WriteVInt32(field.Number);
            meta.WriteByte((byte)Lucene45DocValuesFormat.SORTED);
            AddTermsDict(field, values);
            AddNumericField(field, docToOrd, false);
        }

        private static bool IsSingleValued(IEnumerable<long?> docToOrdCount)
        {
            foreach (var ordCount in docToOrdCount)
            {
                if (ordCount.GetValueOrDefault() > 1)
                    return false;
            }
            return true;
        }

        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            meta.WriteVInt32(field.Number);
            meta.WriteByte((byte)Lucene45DocValuesFormat.SORTED_SET);

            if (IsSingleValued(docToOrdCount))
            {
                meta.WriteVInt32(SORTED_SET_SINGLE_VALUED_SORTED);
                // The field is single-valued, we can encode it as SORTED
                AddSortedField(field, values, GetSortedSetEnumerable(docToOrdCount, ords));
                return;
            }

            meta.WriteVInt32(SORTED_SET_WITH_ADDRESSES);

            // write the ord -> byte[] as a binary field
            AddTermsDict(field, values);

            // write the stream of ords as a numeric field
            // NOTE: we could return an iterator that delta-encodes these within a doc
            AddNumericField(field, ords, false);

            // write the doc -> ord count as a absolute index to the stream
            meta.WriteVInt32(field.Number);
            meta.WriteByte((byte)Lucene45DocValuesFormat.NUMERIC);
            meta.WriteVInt32(DELTA_COMPRESSED);
            meta.WriteInt64(-1L);
            meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
            meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            meta.WriteVInt64(maxDoc);
            meta.WriteVInt32(BLOCK_SIZE);

            var writer = new MonotonicBlockPackedWriter(data, BLOCK_SIZE);
            long addr = 0;
            foreach (long? v in docToOrdCount)
            {
                addr += v.Value;
                writer.Add(addr);
            }
            writer.Finish();
        }

        private static IEnumerable<long?> GetSortedSetEnumerable(IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords) // LUCENENET: CA1822: Mark members as static
        {
            IEnumerator<long?> docToOrdCountIter = docToOrdCount.GetEnumerator();
            IEnumerator<long?> ordsIter = ords.GetEnumerator();

            const long MISSING_ORD = -1;

            while (docToOrdCountIter.MoveNext())
            {
                long current = docToOrdCountIter.Current.Value;
                if (current == 0)
                {
                    yield return MISSING_ORD;
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(current == 1);
                    ordsIter.MoveNext();
                    yield return ordsIter.Current;
                }
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(!ordsIter.MoveNext());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bool success = false;
                try
                {
                    if (meta != null)
                    {
                        meta.WriteVInt32(-1); // write EOF marker
                        CodecUtil.WriteFooter(meta); // write checksum
                    }
                    if (data != null)
                    {
                        CodecUtil.WriteFooter(data); // write checksum
                    }
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Dispose(data, meta);
                    }
                    else
                    {
                        IOUtils.DisposeWhileHandlingException(data, meta);
                    }
                    meta = data = null;
                }
            }
        }
    }
}