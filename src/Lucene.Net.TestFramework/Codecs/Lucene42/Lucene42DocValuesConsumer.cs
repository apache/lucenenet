using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;
using static Lucene.Net.Util.Fst.FST;
using static Lucene.Net.Util.Packed.PackedInt32s;

namespace Lucene.Net.Codecs.Lucene42
{
    using Util = Lucene.Net.Util.Fst.Util;

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

    //   Constants use Lucene42DocValuesProducer.

    /// <summary>
    /// Writer for <see cref="Lucene42DocValuesFormat"/>.
    /// </summary>
#pragma warning disable 612, 618
    internal class Lucene42DocValuesConsumer : DocValuesConsumer
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        internal readonly IndexOutput data, meta;
#pragma warning restore CA2213 // Disposable fields should be disposed
        internal readonly int maxDoc;
        internal readonly float acceptableOverheadRatio;

        internal Lucene42DocValuesConsumer(SegmentWriteState state, string dataCodec, string dataExtension, string metaCodec, string metaExtension, float acceptableOverheadRatio)
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
            maxDoc = state.SegmentInfo.DocCount;
            bool success = false;
            try
            {
                string dataName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, dataExtension);
                data = state.Directory.CreateOutput(dataName, state.Context);
                // this writer writes the format 4.2 did!
                CodecUtil.WriteHeader(data, dataCodec, Lucene42DocValuesProducer.VERSION_GCD_COMPRESSION);
                string metaName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix, metaExtension);
                meta = state.Directory.CreateOutput(metaName, state.Context);
                CodecUtil.WriteHeader(meta, metaCodec, Lucene42DocValuesProducer.VERSION_GCD_COMPRESSION);
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
            meta.WriteByte((byte)Lucene42DocValuesProducer.NUMBER);
            meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            long gcd = 0;
            // TODO: more efficient?
            ISet<long> uniqueValues = null;
            if (optimizeStorage)
            {
                uniqueValues = new JCG.HashSet<long>();

                long count = 0;
                foreach (long? nv in values)
                {
                    // TODO: support this as MemoryDVFormat (and be smart about missing maybe)
                    long v = nv.GetValueOrDefault();

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

            if (uniqueValues != null)
            {
                // small number of unique values
                int bitsPerValue = PackedInt32s.BitsRequired(uniqueValues.Count - 1);
                FormatAndBits formatAndBits = PackedInt32s.FastestFormatAndBits(maxDoc, bitsPerValue, acceptableOverheadRatio);
                if (formatAndBits.BitsPerValue == 8 && minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
                {
                    meta.WriteByte((byte)Lucene42DocValuesProducer.UNCOMPRESSED); // uncompressed
                    foreach (long? nv in values)
                    {
                        data.WriteByte((byte)nv.GetValueOrDefault());
                    }
                }
                else
                {
                    meta.WriteByte((byte)Lucene42DocValuesProducer.TABLE_COMPRESSED); // table-compressed
                    var decode = new long[uniqueValues.Count];
                    uniqueValues.CopyTo(decode, 0);
                    var encode = new Dictionary<long, int>();
                    data.WriteVInt32(decode.Length);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        data.WriteInt64(decode[i]);
                        encode[decode[i]] = i;
                    }

                    meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                    data.WriteVInt32(formatAndBits.Format.Id);
                    data.WriteVInt32(formatAndBits.BitsPerValue);

                    PackedInt32s.Writer writer = PackedInt32s.GetWriterNoHeader(data, formatAndBits.Format, maxDoc, formatAndBits.BitsPerValue, PackedInt32s.DEFAULT_BUFFER_SIZE);
                    foreach (long? nv in values)
                    {
                        writer.Add(encode[nv.GetValueOrDefault()]);
                    }
                    writer.Finish();
                }
            }
            else if (gcd != 0 && gcd != 1)
            {
                meta.WriteByte((byte)Lucene42DocValuesProducer.GCD_COMPRESSED);
                meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                data.WriteInt64(minValue);
                data.WriteInt64(gcd);
                data.WriteVInt32(Lucene42DocValuesProducer.BLOCK_SIZE);

                BlockPackedWriter writer = new BlockPackedWriter(data, Lucene42DocValuesProducer.BLOCK_SIZE);
                foreach (long? nv in values)
                {
                    writer.Add((nv.GetValueOrDefault() - minValue) / gcd);
                }
                writer.Finish();
            }
            else
            {
                meta.WriteByte((byte)Lucene42DocValuesProducer.DELTA_COMPRESSED); // delta-compressed

                meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                data.WriteVInt32(Lucene42DocValuesProducer.BLOCK_SIZE);

                BlockPackedWriter writer = new BlockPackedWriter(data, Lucene42DocValuesProducer.BLOCK_SIZE);
                foreach (long? nv in values)
                {
                    writer.Add(nv.GetValueOrDefault());
                }
                writer.Finish();
            }
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
                }
            }
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // write the byte[] data
            meta.WriteVInt32(field.Number);
            meta.WriteByte((byte)Lucene42DocValuesProducer.BYTES);
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;
            long startFP = data.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            foreach (BytesRef v in values)
            {
                int length = v is null ? 0 : v.Length;
                if (length > Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH)
                {
                    throw new ArgumentException("DocValuesField \"" + field.Name + "\" is too large, must be <= " + Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH);
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
            meta.WriteVInt32(minLength);
            meta.WriteVInt32(maxLength);

            // if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
            // otherwise, we need to record the length fields...
            if (minLength != maxLength)
            {
                meta.WriteVInt32(PackedInt32s.VERSION_CURRENT);
                meta.WriteVInt32(Lucene42DocValuesProducer.BLOCK_SIZE);

                MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(data, Lucene42DocValuesProducer.BLOCK_SIZE);
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
            meta.WriteByte((byte)Lucene42DocValuesProducer.FST);
            meta.WriteInt64(data.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
            Builder<Int64> builder = new Builder<Int64>(INPUT_TYPE.BYTE1, outputs);
            Int32sRef scratch = new Int32sRef();
            long ord = 0;
            foreach (BytesRef v in values)
            {
                builder.Add(Util.ToInt32sRef(v, scratch), ord);
                ord++;
            }

            var fst = builder.Finish();
            if (fst != null)
            {
                fst.Save(data);
            }
            meta.WriteVInt64(ord);
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
        {
            // three cases for simulating the old writer:
            // 1. no missing
            // 2. missing (and empty string in use): remap ord=-1 -> ord=0
            // 3. missing (and empty string not in use): remap all ords +1, insert empty string into values
            bool anyMissing = false;
            foreach (long? n in docToOrd)
            {
                if (n.Value == -1)
                {
                    anyMissing = true;
                    break;
                }
            }

            bool hasEmptyString = false;
            foreach (BytesRef b in values)
            {
                hasEmptyString = b.Length == 0;
                break;
            }

            if (!anyMissing)
            {
                // nothing to do
            }
            else if (hasEmptyString)
            {
                docToOrd = MissingOrdRemapper.MapMissingToOrd0(docToOrd);
            }
            else
            {
                docToOrd = MissingOrdRemapper.MapAllOrds(docToOrd);
                values = MissingOrdRemapper.InsertEmptyValue(values);
            }

            // write the ordinals as numerics
            AddNumericField(field, docToOrd, false);

            // write the values as FST
            WriteFST(field, values);
        }

        // note: this might not be the most efficient... but its fairly simple
        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
        {
            // write the ordinals as a binary field
            AddBinaryField(field, new EnumerableAnonymousClass(docToOrdCount, ords));

            // write the values as FST
            WriteFST(field, values);
        }

        private sealed class EnumerableAnonymousClass : IEnumerable<BytesRef>
        {
            private readonly IEnumerable<long?> docToOrdCount;
            private readonly IEnumerable<long?> ords;

            public EnumerableAnonymousClass(IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
            {
                this.docToOrdCount = docToOrdCount;
                this.ords = ords;
            }

            public IEnumerator<BytesRef> GetEnumerator()
            {
                return new SortedSetEnumerator(docToOrdCount.GetEnumerator(), ords.GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        // per-document vint-encoded byte[]
        internal class SortedSetEnumerator : IEnumerator<BytesRef>
        {
            internal byte[] buffer = new byte[10];
            internal ByteArrayDataOutput @out = new ByteArrayDataOutput();
            internal BytesRef @ref = new BytesRef();

            internal readonly IEnumerator<long?> counts;
            internal readonly IEnumerator<long?> ords;

            internal SortedSetEnumerator(IEnumerator<long?> counts, IEnumerator<long?> ords)
            {
                this.counts = counts;
                this.ords = ords;
            }

            public bool MoveNext()
            {
                if (!counts.MoveNext())
                {
                    return false;
                }

                int count = (int)counts.Current;
                int maxSize = count * 9; //worst case
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

                @ref.Bytes = buffer;
                @ref.Offset = 0;
                @ref.Length = @out.Position;

                return true;
            }

            public BytesRef Current => @ref;

            object IEnumerator.Current => Current;

            // encodes count values to buffer
            internal virtual void EncodeValues(int count)
            {
                @out.Reset(buffer);
                long lastOrd = 0;
                for (int i = 0; i < count; i++)
                {
                    ords.MoveNext();
                    long ord = ords.Current.Value;
                    @out.WriteVInt64(ord - lastOrd);
                    lastOrd = ord;
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }
        }
    }
#pragma warning restore 612, 618
}