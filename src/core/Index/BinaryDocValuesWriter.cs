using Lucene.Net.Codecs;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class BinaryDocValuesWriter : DocValuesWriter
    {
        private readonly ByteBlockPool pool;
        private readonly AppendingLongBuffer lengths;
        private readonly FieldInfo fieldInfo;
        private int addedValues = 0;

        public BinaryDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed)
        {
            this.fieldInfo = fieldInfo;
            this.pool = new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(iwBytesUsed));
            this.lengths = new AppendingLongBuffer();
        }

        public void AddValue(int docID, BytesRef value)
        {
            if (docID < addedValues)
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.name + "\" appears more than once in this document (only one value is allowed per field)");
            }
            if (value == null)
            {
                throw new ArgumentException("field=\"" + fieldInfo.name + "\": null value not allowed");
            }
            if (value.length > (ByteBlockPool.BYTE_BLOCK_SIZE - 2))
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.name + "\" is too large, must be <= " + (ByteBlockPool.BYTE_BLOCK_SIZE - 2));
            }

            // Fill in any holes:
            while (addedValues < docID)
            {
                addedValues++;
                lengths.Add(0);
            }
            addedValues++;
            lengths.Add(value.length);
            pool.Append(value);
        }

        internal override void Finish(int numDoc)
        {
        }

        internal override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.segmentInfo.DocCount;
            dvConsumer.AddBinaryField(fieldInfo, GetBytesIterator(maxDoc));
        }

        private IEnumerable<BytesRef> GetBytesIterator(int maxDocParam)
        { 
            // .NET port: using yield return instead of a custom IEnumerable type
            
            BytesRef value = new BytesRef();
            AppendingLongBuffer.Iterator lengthsIterator = lengths.GetIterator();
            int size = (int) lengths.Size;
            int maxDoc = maxDocParam;
            int upto = 0;
            long byteOffset = 0L;

            while (upto < maxDoc)
            {
                if (upto < size)
                {
                    int length = (int)lengthsIterator.Next();
                    value.Grow(length);
                    value.length = length;
                    pool.ReadBytes(byteOffset, value.bytes, value.offset, value.length);
                    byteOffset += length;
                }
                else
                {
                    // This is to handle last N documents not having
                    // this DV field in the end of the segment:
                    value.length = 0;
                }

                upto++;
                yield return value;
            }
        }
    }
}
