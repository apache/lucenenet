using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs.Lucene42
{
    internal class Lucene42DocValuesConsumer : DocValuesConsumer
    {
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;

        internal const sbyte NUMBER = 0;
        internal const sbyte BYTES = 1;
        internal const sbyte FST = 2;

        internal const int BLOCK_SIZE = 4096;

        internal const sbyte DELTA_COMPRESSED = 0;
        internal const sbyte TABLE_COMPRESSED = 1;
        internal const sbyte UNCOMPRESSED = 2;

        internal readonly IndexOutput data, meta;
        internal readonly int maxDoc;
        internal readonly float acceptableOverheadRatio;

        internal Lucene42DocValuesConsumer(SegmentWriteState state, String dataCodec, String dataExtension, String metaCodec, String metaExtension, float acceptableOverheadRatio)
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
            maxDoc = state.segmentInfo.DocCount;
            bool success = false;
            try
            {
                String dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix, dataExtension);
                data = state.directory.CreateOutput(dataName, state.context);
                CodecUtil.WriteHeader(data, dataCodec, VERSION_CURRENT);
                String metaName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix, metaExtension);
                meta = state.directory.CreateOutput(metaName, state.context);
                CodecUtil.WriteHeader(meta, metaCodec, VERSION_CURRENT);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)this);
                }
            }
        }

        public override void AddNumericField(FieldInfo field, IEnumerable<long> values)
        {
            meta.WriteVInt(field.number);
            meta.WriteByte(NUMBER);
            meta.WriteLong(data.FilePointer);
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;
            // TODO: more efficient?
            HashSet<long> uniqueValues = new HashSet<long>();
            foreach (long nv in values)
            {
                long v = nv;
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
            }

            if (uniqueValues != null)
            {
                // small number of unique values
                int bitsPerValue = PackedInts.BitsRequired(uniqueValues.Count - 1);
                PackedInts.FormatAndBits formatAndBits = PackedInts.FastestFormatAndBits(maxDoc, bitsPerValue, acceptableOverheadRatio);
                if (formatAndBits.BitsPerValue == 8 && minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
                {
                    meta.WriteByte(UNCOMPRESSED); // uncompressed
                    foreach (long nv in values)
                    {
                        data.WriteByte((sbyte)nv);
                    }
                }
                else
                {
                    meta.WriteByte(TABLE_COMPRESSED); // table-compressed
                    long[] decode = uniqueValues.ToArray();
                    HashMap<long, int> encode = new HashMap<long, int>();
                    data.WriteVInt(decode.Length);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        data.WriteLong(decode[i]);
                        encode[decode[i]] = i;
                    }

                    meta.WriteVInt(PackedInts.VERSION_CURRENT);
                    data.WriteVInt(formatAndBits.Format.GetId());
                    data.WriteVInt(formatAndBits.BitsPerValue);

                    PackedInts.Writer writer = PackedInts.GetWriterNoHeader(data, formatAndBits.Format, maxDoc, formatAndBits.BitsPerValue, PackedInts.DEFAULT_BUFFER_SIZE);
                    foreach (long nv in values)
                    {
                        writer.Add(encode[nv]);
                    }
                    writer.Finish();
                }
            }
            else
            {
                meta.WriteByte(DELTA_COMPRESSED); // delta-compressed

                meta.WriteVInt(PackedInts.VERSION_CURRENT);
                data.WriteVInt(BLOCK_SIZE);

                BlockPackedWriter writer = new BlockPackedWriter(data, BLOCK_SIZE);
                foreach (long nv in values)
                {
                    writer.Add(nv);
                }
                writer.Finish();
            }
        }

        protected override void Dispose(bool disposing)
        {
            bool success = false;
            try
            {
                if (meta != null)
                {
                    meta.WriteVInt(-1); // write EOF marker
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
                    IOUtils.CloseWhileHandlingException((IDisposable)data, meta);
                }
            }
        }

        public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
        {
            // write the byte[] data
            meta.WriteVInt(field.number);
            meta.WriteByte(BYTES);
            int minLength = int.MaxValue;
            int maxLength = int.MinValue;
            long startFP = data.FilePointer;
            foreach (BytesRef v in values)
            {
                minLength = Math.Min(minLength, v.length);
                maxLength = Math.Max(maxLength, v.length);
                data.WriteBytes(v.bytes, v.offset, v.length);
            }
            meta.WriteLong(startFP);
            meta.WriteLong(data.FilePointer - startFP);
            meta.WriteVInt(minLength);
            meta.WriteVInt(maxLength);

            // if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
            // otherwise, we need to record the length fields...
            if (minLength != maxLength)
            {
                meta.WriteVInt(PackedInts.VERSION_CURRENT);
                meta.WriteVInt(BLOCK_SIZE);

                MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(data, BLOCK_SIZE);
                long addr = 0;
                foreach (BytesRef v in values)
                {
                    addr += v.length;
                    writer.Add(addr);
                }
                writer.Finish();
            }
        }

        private void WriteFST(FieldInfo field, IEnumerable<BytesRef> values)
        {
            meta.WriteVInt(field.number);
            meta.WriteByte(FST);
            meta.WriteLong(data.FilePointer);
            PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton(true);
            Builder<long> builder = new Builder<long>(Lucene.Net.Util.Fst.FST.INPUT_TYPE.BYTE1, outputs);
            IntsRef scratch = new IntsRef();
            long ord = 0;
            foreach (BytesRef v in values)
            {
                builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(v, scratch), ord);
                ord++;
            }
            FST<long> fst = builder.Finish();
            if (fst != null)
            {
                fst.Save(data);
            }
            meta.WriteVLong(ord);
        }

        public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrd)
        {
            // write the ordinals as numerics
            AddNumericField(field, docToOrd.Cast<long>());

            // write the values as FST
            WriteFST(field, values);
        }

        public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
        {
            AddBinaryField(field, new AnonymousAddSortedSetFieldEnumerable(docToOrdCount, ords));
            // write the ordinals as a binary field
            //AddBinaryField(field, new Iterable<BytesRef>() {
            //  @Override
            //  public Iterator<BytesRef> iterator() {
            //    return new SortedSetIterator(docToOrdCount.iterator(), ords.iterator());
            //  }
            //});

            // write the values as FST
            WriteFST(field, values);
        }

        private sealed class AnonymousAddSortedSetFieldEnumerable : IEnumerable<BytesRef>
        {
            private readonly IEnumerable<int> docToOrdCount;
            private readonly IEnumerable<long> ords;

            public AnonymousAddSortedSetFieldEnumerable(IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
            {
                this.docToOrdCount = docToOrdCount;
                this.ords = ords;
            }

            public IEnumerator<BytesRef> GetEnumerator()
            {
                return new SortedSetIterator(docToOrdCount.GetEnumerator(), ords.GetEnumerator());
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal class SortedSetIterator : IEnumerator<BytesRef>
        {
            internal byte[] buffer = new byte[10];
            internal ByteArrayDataOutput output = new ByteArrayDataOutput();
            internal BytesRef bytesref = new BytesRef();

            internal IEnumerator<int> counts;
            internal IEnumerator<long> ords;

            internal SortedSetIterator(IEnumerator<int> counts, IEnumerator<long> ords)
            {
                this.counts = counts;
                this.ords = ords;
            }

            public BytesRef Current
            {
                get { return bytesref; }
            }

            public void Dispose()
            {
                counts.Dispose();
                ords.Dispose();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return bytesref; }
            }

            public bool MoveNext()
            {
                if (!counts.MoveNext())
                    return false;

                int count = counts.Current;
                int maxSize = count * 9; // worst case
                if (maxSize > buffer.Length)
                {
                    buffer = ArrayUtil.Grow(buffer, maxSize);
                }

                try
                {
                    EncodeValues(count);
                }
                catch (System.IO.IOException)
                {
                    throw;
                }

                bytesref.bytes = (sbyte[])(Array)buffer;
                bytesref.offset = 0;
                bytesref.length = output.Position;

                return true;
            }

            // encodes count values to buffer
            private void EncodeValues(int count)
            {
                output.Reset(buffer);
                long lastOrd = 0;
                for (int i = 0; i < count; i++)
                {
                    ords.MoveNext();
                    long ord = ords.Current;
                    output.WriteVLong(ord - lastOrd);
                    lastOrd = ord;
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
