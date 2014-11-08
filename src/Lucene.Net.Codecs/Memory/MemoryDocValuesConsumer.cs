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

using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Index;
using Lucene.Net.Util.Fst;

namespace Lucene.Net.Codecs.Memory
{


    using FieldInfo = Index.FieldInfo;
    using IndexFileNames = Index.IndexFileNames;
    using SegmentWriteState = Index.SegmentWriteState;
    using ByteArrayDataOutput = Store.ByteArrayDataOutput;
    using IndexOutput = Store.IndexOutput;
    using ArrayUtil = Util.ArrayUtil;
    using BytesRef = Util.BytesRef;
    using IOUtils = Util.IOUtils;
    using IntsRef = Util.IntsRef;
    using MathUtil = Util.MathUtil;
    using Builder = Util.Fst.Builder;
    using INPUT_TYPE = Util.Fst.FST.INPUT_TYPE;
    using FST = Util.Fst.FST;
    using PositiveIntOutputs = Util.Fst.PositiveIntOutputs;
    using Util = Util.Fst.Util;
    using BlockPackedWriter = Util.Packed.BlockPackedWriter;
    using MonotonicBlockPackedWriter = Util.Packed.MonotonicBlockPackedWriter;
    using FormatAndBits = Util.Packed.PackedInts.FormatAndBits;
    using PackedInts = Util.Packed.PackedInts;

    /// <summary>
    /// Writer for <seealso cref="MemoryDocValuesFormat"/>
    /// </summary>
    internal class MemoryDocValuesConsumer : DocValuesConsumer
    {
        internal IndexOutput data, meta;
        internal readonly int maxDoc;
        internal readonly float acceptableOverheadRatio;

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
                    IOUtils.CloseWhileHandlingException(this);
                }
            }
        }

        public override void AddNumericField(FieldInfo field, IEnumerable<long> values)
        {
            AddNumericField(field, values, true);
        }

        internal virtual void AddNumericField(FieldInfo field, IEnumerable<long> values, bool optimizeStorage)
        {
            meta.WriteVInt(field.Number);
            meta.WriteByte(MemoryDocValuesProducer.NUMBER);
            meta.WriteLong(data.FilePointer);
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            long gcd = 0;
            bool missing = false;
            // TODO: more efficient?
            HashSet<long?> uniqueValues = null;
            if (optimizeStorage)
            {
                uniqueValues = new HashSet<>();

                long count = 0;
                foreach (var nv in values)
                {
                    long v = nv;

                    if (gcd != 1)
                    {
                        if (v < long.MinValue/2 || v > long.MaxValue/2)
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
                Debug.Assert(count == maxDoc);
            }

            if (missing)
            {
                long start = data.FilePointer;
                WriteMissingBitset(values);
                meta.WriteLong(start);
                meta.WriteLong(data.FilePointer - start);
            }
            else
            {
                meta.WriteLong(-1L);
            }

            if (uniqueValues != null)
            {
                // small number of unique values

                int bitsPerValue = PackedInts.BitsRequired(uniqueValues.Count - 1);
                FormatAndBits formatAndBits = PackedInts.FastestFormatAndBits(maxDoc, bitsPerValue,
                    acceptableOverheadRatio);
                if (formatAndBits.bitsPerValue == 8 && minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
                {
                    meta.WriteByte(MemoryDocValuesProducer.UNCOMPRESSED); // uncompressed
                    foreach (var nv in values)
                    {
                        data.WriteByte(nv == null ? 0 : (long) (sbyte) nv);
                    }
                }
                else
                {
                    meta.WriteByte(MemoryDocValuesProducer.TABLE_COMPRESSED); // table-compressed
                    long?[] decode = uniqueValues.toArray(new long?[uniqueValues.Count]);

                    var encode = new Dictionary<long?, int?>();
                    data.WriteVInt(decode.Length);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        data.WriteLong(decode[i]);
                        encode[decode[i]] = i;
                    }

                    meta.WriteVInt(PackedInts.VERSION_CURRENT);
                    data.WriteVInt(formatAndBits.format.Id);
                    data.WriteVInt(formatAndBits.bitsPerValue);

                    PackedInts.Writer writer = PackedInts.GetWriterNoHeader(data, formatAndBits.format, maxDoc,
                        formatAndBits.bitsPerValue, PackedInts.DEFAULT_BUFFER_SIZE);
                    foreach (long nv in values)
                    {
                        writer.Add(encode[nv == null ? 0 : (long) nv]);
                    }
                    writer.Finish();
                }
            }
            else if (gcd != 0 && gcd != 1)
            {
                meta.WriteByte(MemoryDocValuesProducer.GCD_COMPRESSED);
                meta.WriteVInt(PackedInts.VERSION_CURRENT);
                data.WriteLong(minValue);
                data.WriteLong(gcd);
                data.WriteVInt(MemoryDocValuesProducer.BLOCK_SIZE);

                var writer = new BlockPackedWriter(data, MemoryDocValuesProducer.BLOCK_SIZE);
                foreach (var nv in values)
                {
                    writer.Add((nv - minValue)/gcd);
                }
                writer.Finish();
            }
            else
            {
                meta.WriteByte(MemoryDocValuesProducer.DELTA_COMPRESSED); // delta-compressed

                meta.WriteVInt(PackedInts.VERSION_CURRENT);
                data.WriteVInt(MemoryDocValuesProducer.BLOCK_SIZE);

                var writer = new BlockPackedWriter(data, MemoryDocValuesProducer.BLOCK_SIZE);
                foreach (var nv in values)
                {
                    writer.Add(nv);
                }
                writer.Finish();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) return;

            var success = false;
            try
            {
                if (meta != null)
                {
                    meta.WriteVInt(-1); // write EOF marker
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
                    IOUtils.Close(data, meta);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(data, meta);
                }
                data = meta = null;
            }
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // write the byte[] data
            meta.WriteVInt(field.Number);
            meta.WriteByte(MemoryDocValuesProducer.BYTES);
            var minLength = int.MaxValue;
            var maxLength = int.MinValue;

            var startFP = data.FilePointer;
            var missing = false;
            foreach (var v in values)
            {
                int length;
                if (v == null)
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
            meta.WriteLong(startFP);
            meta.WriteLong(data.FilePointer - startFP);
            if (missing)
            {
                long start = data.FilePointer;
                WriteMissingBitset(values);
                meta.WriteLong(start);
                meta.WriteLong(data.FilePointer - start);
            }
            else
            {
                meta.WriteLong(-1L);
            }
            meta.WriteVInt(minLength);
            meta.WriteVInt(maxLength);

            // if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
            // otherwise, we need to record the length fields...
            if (minLength != maxLength)
            {
                meta.WriteVInt(PackedInts.VERSION_CURRENT);
                meta.WriteVInt(MemoryDocValuesProducer.BLOCK_SIZE);


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
            meta.WriteVInt(field.Number);
            meta.WriteByte(FST);
            meta.WriteLong(data.FilePointer);
            PositiveIntOutputs outputs = PositiveIntOutputs.Singleton;
            var builder = new Builder<long?>(INPUT_TYPE.BYTE1, outputs);
            var scratch = new IntsRef();
            long ord = 0;
            foreach (BytesRef v in values)
            {
                builder.Add(Util.ToIntsRef(v, scratch), ord);
                ord++;
            }
            FST<long?> fst = builder.Finish();
            if (fst != null)
            {
                fst.Save(data);
            }
            meta.WriteVLong(ord);
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
                    data.WriteLong(bits);
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
                data.WriteLong(bits);
            }
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long> docToOrd)
        {
            // write the ordinals as numerics
            AddNumericField(field, docToOrd, false);

            // write the values as FST
            WriteFST(field, values);
        }

        // note: this might not be the most efficient... but its fairly simple
        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values,
            IEnumerable<long> docToOrdCount, IEnumerable<long> ords)
        {
            // write the ordinals as a binary field
            AddBinaryField(field, new IterableAnonymousInnerClassHelper(this, docToOrdCount, ords));

            // write the values as FST
            WriteFST(field, values);
        }

        private class IterableAnonymousInnerClassHelper : IEnumerable<BytesRef>
        {
            private readonly IEnumerable<long> _docToOrdCount;
            private readonly IEnumerable<long> _ords;

            public IterableAnonymousInnerClassHelper(MemoryDocValuesConsumer outerInstance,
                IEnumerable<long> docToOrdCount, IEnumerable<long> ords)
            {
                _docToOrdCount = docToOrdCount;
                _ords = ords;
            }

            public IEnumerator<BytesRef> GetEnumerator()
            {
                return new SortedSetIterator(_docToOrdCount.GetEnumerator(), _ords.GetEnumerator());
            }
        }

        // per-document vint-encoded byte[]
        internal class SortedSetIterator : IEnumerator<BytesRef>
        {
            internal sbyte[] buffer = new sbyte[10];
            internal ByteArrayDataOutput @out = new ByteArrayDataOutput();
            internal BytesRef @ref = new BytesRef();

            internal readonly IEnumerator<long> counts;
            internal readonly IEnumerator<long> ords;

            internal SortedSetIterator(IEnumerator<long> counts, IEnumerator<long> ords)
            {
                this.counts = counts;
                this.ords = ords;
            }

            public override bool HasNext()
            {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
                return counts.hasNext();
            }

            public override BytesRef Next()
            {
                if (!HasNext())
                {
                    throw new ArgumentOutOfRangeException();
                }

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
                int count = (int) counts.next();
                int maxSize = count*9; // worst case
                if (maxSize > buffer.Length)
                {
                    buffer = ArrayUtil.Grow(buffer, maxSize);
                }

                EncodeValues(count);
                

                @ref.Bytes = buffer;
                @ref.Offset = 0;
                @ref.Length = @out.Position;

                return @ref;
            }

            // encodes count values to buffer
            internal virtual void EncodeValues(int count)
            {
                @out.Reset(buffer);
                long lastOrd = 0;
                for (int i = 0; i < count; i++)
                {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
                    long ord = (long) ords.next();
                    @out.writeVLong(ord - lastOrd);
                    lastOrd = ord;
                }
            }

            public override void Remove()
            {
                throw new NotSupportedException();
            }
        }
    }
}