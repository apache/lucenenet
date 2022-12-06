using Lucene.Net.Diagnostics;
using Lucene.Net.Util.Fst;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Codecs.Memory
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

    using ArrayUtil = Util.ArrayUtil;
    using BlockPackedWriter = Util.Packed.BlockPackedWriter;
    using ByteArrayDataOutput = Store.ByteArrayDataOutput;
    using BytesRef = Util.BytesRef;
    using FieldInfo = Index.FieldInfo;
    using FormatAndBits = Util.Packed.PackedInt32s.FormatAndBits;
    using IndexFileNames = Index.IndexFileNames;
    using IndexOutput = Store.IndexOutput;
    using INPUT_TYPE = Util.Fst.FST.INPUT_TYPE;
    using Int32sRef = Util.Int32sRef;
    using IOUtils = Util.IOUtils;
    using MathUtil = Util.MathUtil;
    using MonotonicBlockPackedWriter = Util.Packed.MonotonicBlockPackedWriter;
    using PackedInt32s = Util.Packed.PackedInt32s;
    using PositiveInt32Outputs = Util.Fst.PositiveInt32Outputs;
    using SegmentWriteState = Index.SegmentWriteState;
    using Util = Util.Fst.Util;

    /// <summary>
    /// Writer for <see cref="MemoryDocValuesFormat"/>.
    /// </summary>
    internal class MemoryDocValuesConsumer : DocValuesConsumer
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput data, meta;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private readonly int maxDoc;
        private readonly float acceptableOverheadRatio;

        internal MemoryDocValuesConsumer(SegmentWriteState state, string dataCodec, string dataExtension,
            string metaCodec,
            string metaExtension, float acceptableOverheadRatio)
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
            maxDoc = state.SegmentInfo.DocCount;
            var success = false;
            try
            {
                var dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, dataExtension);
                data = state.Directory.CreateOutput(dataName, state.Context);
                CodecUtil.WriteHeader(data, dataCodec, MemoryDocValuesProducer.VERSION_CURRENT);
                var metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
                meta = state.Directory.CreateOutput(metaName, state.Context);
                CodecUtil.WriteHeader(meta, metaCodec, MemoryDocValuesProducer.VERSION_CURRENT);
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

        public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
        {
            AddNumericField(field, values, true);
        }

        internal virtual void AddNumericField(FieldInfo field, IEnumerable<long?> values, bool optimizeStorage)
        {
            meta.WriteVInt32(field.Number);
            meta.WriteByte(MemoryDocValuesProducer.NUMBER);
            meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            long gcd = 0;
            bool missing = false;
            // TODO: more efficient?
            ISet<long> uniqueValues = null;
            if (optimizeStorage)
            {
                uniqueValues = new JCG.HashSet<long>();

                long count = 0;
                foreach (var nv in values)
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
                if (Debugging.AssertsEnabled) Debugging.Assert(count == maxDoc);
            }

            if (missing)
            {
                long start = data.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                WriteMissingBitset(values);
                meta.WriteInt64(start);
                meta.WriteInt64(data.Position - start); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
            else
            {
                meta.WriteInt64(-1L);
            }

            if (uniqueValues != null)
            {
                // small number of unique values

                int bitsPerValue = PackedInt32s.BitsRequired(uniqueValues.Count - 1);
                FormatAndBits formatAndBits = PackedInt32s.FastestFormatAndBits(maxDoc, bitsPerValue,
                    acceptableOverheadRatio);
                if (formatAndBits.BitsPerValue == 8 && minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
                {
                    meta.WriteByte(MemoryDocValuesProducer.UNCOMPRESSED); // uncompressed
                    foreach (var nv in values)
                    {
                        data.WriteByte((byte)nv.GetValueOrDefault());
                    }
                }
                else
                {
                    meta.WriteByte(MemoryDocValuesProducer.TABLE_COMPRESSED); // table-compressed
                    long[] decode = new long[uniqueValues.Count];
                    uniqueValues.CopyTo(decode, 0);

                    var encode = new Dictionary<long?, int?>();
                    data.WriteVInt32(decode.Length);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        data.WriteInt64(decode[i]);
                        encode[decode[i]] = i;
                    }

                    meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                    data.WriteVInt32(formatAndBits.Format.Id);
                    data.WriteVInt32(formatAndBits.BitsPerValue);

                    PackedInt32s.Writer writer = PackedInt32s.GetWriterNoHeader(data, formatAndBits.Format, maxDoc,
                        formatAndBits.BitsPerValue, PackedInt32s.DEFAULT_BUFFER_SIZE);
                    foreach (var nv in values)
                    {
                        var v = encode[nv.GetValueOrDefault()];

                        writer.Add((long)v);
                    }
                    writer.Finish();
                }
            }
            else if (gcd != 0 && gcd != 1)
            {
                meta.WriteByte(MemoryDocValuesProducer.GCD_COMPRESSED);
                meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                data.WriteInt64(minValue);
                data.WriteInt64(gcd);
                data.WriteVInt32(MemoryDocValuesProducer.BLOCK_SIZE);

                var writer = new BlockPackedWriter(data, MemoryDocValuesProducer.BLOCK_SIZE);
                foreach (var nv in values)
                {
                    writer.Add((nv.GetValueOrDefault() - minValue) / gcd);
                }
                writer.Finish();
            }
            else
            {
                meta.WriteByte(MemoryDocValuesProducer.DELTA_COMPRESSED); // delta-compressed

                meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                data.WriteVInt32(MemoryDocValuesProducer.BLOCK_SIZE);

                var writer = new BlockPackedWriter(data, MemoryDocValuesProducer.BLOCK_SIZE);
                foreach (var nv in values)
                {
                    writer.Add(nv.GetValueOrDefault());
                }
                writer.Finish();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            var success = false;
            try
            {
                if (meta != null)
                {
                    meta.WriteVInt32(-1); // write EOF marker
                    CodecUtil.WriteFooter(meta); // write checksum
                }
                if (data != null)
                {
                    CodecUtil.WriteFooter(data);
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
                data = meta = null;
            }
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // write the byte[] data
            meta.WriteVInt32(field.Number);
            meta.WriteByte(MemoryDocValuesProducer.BYTES);
            var minLength = int.MaxValue;
            var maxLength = int.MinValue;

            var startFP = data.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            var missing = false;
            foreach (var v in values)
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
                if (length > MemoryDocValuesFormat.MAX_BINARY_FIELD_LENGTH)
                {
                    throw new ArgumentException("DocValuesField \"" + field.Name + "\" is too large, must be <= " +
                                                       MemoryDocValuesFormat.MAX_BINARY_FIELD_LENGTH);
                }
                minLength = Math.Min(minLength, length);
                maxLength = Math.Max(maxLength, length);
                if (v != null)
                {
                    data.WriteBytes(v.Bytes, v.Offset, v.Length);
                }
            }
            meta.WriteInt64(startFP);
            meta.WriteInt64(data.Position - startFP); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            if (missing)
            {
                long start = data.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                WriteMissingBitset(values);
                meta.WriteInt64(start);
                meta.WriteInt64(data.Position - start); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
            else
            {
                meta.WriteInt64(-1L);
            }
            meta.WriteVInt32(minLength);
            meta.WriteVInt32(maxLength);

            // if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
            // otherwise, we need to record the length fields...
            if (minLength != maxLength)
            {
                meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                meta.WriteVInt32(MemoryDocValuesProducer.BLOCK_SIZE);


                var writer = new MonotonicBlockPackedWriter(data, MemoryDocValuesProducer.BLOCK_SIZE);
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

        private void WriteFST(FieldInfo field, IEnumerable<BytesRef> values)
        {
            meta.WriteVInt32(field.Number);
            meta.WriteByte(MemoryDocValuesProducer.FST);
            meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
            var builder = new Builder<Int64>(INPUT_TYPE.BYTE1, outputs);
            var scratch = new Int32sRef();
            long ord = 0;
            foreach (BytesRef v in values)
            {
                builder.Add(Util.ToInt32sRef(v, scratch), ord);
                ord++;
            }
            FST<Int64> fst = builder.Finish();
            if (fst != null)
            {
                fst.Save(data);
            }
            meta.WriteVInt64(ord);
        }

        // TODO: in some cases representing missing with minValue-1 wouldn't take up additional space and so on,
        // but this is very simple, and algorithms only check this for values of 0 anyway (doesnt slow down normal decode)

        internal virtual void WriteMissingBitset<T1>(IEnumerable<T1> values)
        {
            long bits = 0;
            int count = 0;
            foreach (object v in values)
            {
                if (count == 64)
                {
                    data.WriteInt64(bits);
                    count = 0;
                    bits = 0;
                }
                if (v != null)
                {
                    bits |= 1L << (count & 0x3f);
                }
                count++;
            }
            if (count > 0)
            {
                data.WriteInt64(bits);
            }
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            // write the ordinals as numerics
            AddNumericField(field, docToOrd, false);

            // write the values as FST
            WriteFST(field, values);
        }

        // note: this might not be the most efficient... but its fairly simple
        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values,
            IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            // write the ordinals as a binary field
            AddBinaryField(field, new EnumerableAnonymousClass(docToOrdCount, ords));

            // write the values as FST
            WriteFST(field, values);
        }

        private sealed class EnumerableAnonymousClass : IEnumerable<BytesRef>
        {
            private readonly IEnumerable<long?> _docToOrdCount;
            private readonly IEnumerable<long?> _ords;

            public EnumerableAnonymousClass(IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
            {
                _docToOrdCount = docToOrdCount;
                _ords = ords;
            }

            public IEnumerator<BytesRef> GetEnumerator()
            {
                return new SortedSetEnumerator(_docToOrdCount.GetEnumerator(), _ords.GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        // per-document vint-encoded byte[]
        internal class SortedSetEnumerator : IEnumerator<BytesRef>
        {
            private byte[] buffer = new byte[10];
            private readonly ByteArrayDataOutput @out = new ByteArrayDataOutput(); // LUCENENET: marked readonly
            private readonly BytesRef _current = new BytesRef(); // LUCENENET: marked readonly

            private readonly IEnumerator<long?> counts;
            private readonly IEnumerator<long?> ords;

            public BytesRef Current => _current;

            object IEnumerator.Current => this.Current;

            internal SortedSetEnumerator(IEnumerator<long?> counts, IEnumerator<long?> ords)
            {
                this.counts = counts;
                this.ords = ords;
            }

            public bool MoveNext()
            {

                if (!counts.MoveNext())
                    return false;

                int count = (int)counts.Current;
                int maxSize = count * 9; // worst case
                if (maxSize > buffer.Length)
                {
                    buffer = ArrayUtil.Grow(buffer, maxSize);
                }

                try
                {
                    EncodeValues(count);
                }
                catch (Exception bogus) when (bogus.IsIOException())
                {
                    throw RuntimeException.Create(bogus);
                }

                _current.Bytes = buffer;
                _current.Offset = 0;
                _current.Length = @out.Position;

                return true;
            }

            // encodes count values to buffer
            private void EncodeValues(int count)
            {
                @out.Reset(buffer);
                long lastOrd = 0;
                for (int i = 0; i < count; i++)
                {
                    if (!ords.MoveNext())
                        break;

                    long ord = ords.Current.Value;
                    @out.WriteVInt64(ord - lastOrd);
                    lastOrd = ord;
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.counts.Dispose();
                    this.ords.Dispose();
                }
            }

            // LUCENENET: Remove() not supported in .NET

            public void Reset() // LUCENENET: Required by .NET contract, but not supported here.
            {
                throw UnsupportedOperationException.Create();
            }
        }
    }
}