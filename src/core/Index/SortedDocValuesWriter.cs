using Lucene.Net.Codecs;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class SortedDocValuesWriter : DocValuesWriter
    {
        internal readonly BytesRefHash hash;
        private AppendingLongBuffer pending;
        private readonly Counter iwBytesUsed;
        private long bytesUsed; // this currently only tracks differences in 'pending'
        private readonly FieldInfo fieldInfo;

        private static readonly BytesRef EMPTY = new BytesRef(BytesRef.EMPTY_BYTES);

        public SortedDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed)
        {
            this.fieldInfo = fieldInfo;
            this.iwBytesUsed = iwBytesUsed;
            hash = new BytesRefHash(
                new ByteBlockPool(
                    new ByteBlockPool.DirectTrackingAllocator(iwBytesUsed)),
                    BytesRefHash.DEFAULT_CAPACITY,
                    new BytesRefHash.DirectBytesStartArray(BytesRefHash.DEFAULT_CAPACITY, iwBytesUsed));
            pending = new AppendingLongBuffer();
            bytesUsed = pending.RamBytesUsed;
            iwBytesUsed.AddAndGet(bytesUsed);
        }

        public void AddValue(int docID, BytesRef value)
        {
            if (docID < pending.Size)
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.name + "\" appears more than once in this document (only one value is allowed per field)");
            }
            if (value == null)
            {
                throw new ArgumentException("field \"" + fieldInfo.name + "\": null value not allowed");
            }
            if (value.length > (ByteBlockPool.BYTE_BLOCK_SIZE - 2))
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.name + "\" is too large, must be <= " + (ByteBlockPool.BYTE_BLOCK_SIZE - 2));
            }

            // Fill in any holes:
            while (pending.Size < docID)
            {
                AddOneValue(EMPTY);
            }

            AddOneValue(value);
        }

        internal override void Finish(int maxDoc)
        {
            while (pending.Size < maxDoc)
            {
                AddOneValue(EMPTY);
            }
        }

        private void AddOneValue(BytesRef value)
        {
            int termID = hash.Add(value);
            if (termID < 0)
            {
                termID = -termID - 1;
            }
            else
            {
                // reserve additional space for each unique value:
                // 1. when indexing, when hash is 50% full, rehash() suddenly needs 2*size ints.
                //    TODO: can this same OOM happen in THPF?
                // 2. when flushing, we need 1 int per value (slot in the ordMap).
                iwBytesUsed.AddAndGet(2 * RamUsageEstimator.NUM_BYTES_INT);
            }

            pending.Add(termID);
            UpdateBytesUsed();
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = pending.RamBytesUsed;
            iwBytesUsed.AddAndGet(newBytesUsed - bytesUsed);
            bytesUsed = newBytesUsed;
        }

        internal override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.segmentInfo.DocCount;

            //assert pending.size() == maxDoc;
            int valueCount = hash.Size;

            int[] sortedValues = hash.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
            int[] ordMap = new int[valueCount];

            for (int ord = 0; ord < valueCount; ord++)
            {
                ordMap[sortedValues[ord]] = ord;
            }

            // .NET Port: using iterator methods instead of custom IEnumerator types. Much less code.
            dvConsumer.AddSortedField(fieldInfo, 
                GetValuesIterator(sortedValues, valueCount), 
                GetOrdsIterator(ordMap, maxDoc));
        }

        internal override void Abort()
        {
        }

        private IEnumerable<BytesRef> GetValuesIterator(int[] sortedValues, int valueCount)
        {
            // .NET Port: using yield return instead of custom iterator type. Much less code.

            BytesRef scratch = new BytesRef();
            int ordUpto = 0;

            while (ordUpto < valueCount)
            {
                hash.Get(sortedValues[ordUpto], scratch);
                ordUpto++;
                yield return scratch;
            }
        }

        private IEnumerable<int> GetOrdsIterator(int[] ordMap, int maxDoc)
        {
            // .NET Port: using yield return instead of custom iterator type. Much less code.

            AbstractAppendingLongBuffer.Iterator iter = pending.GetIterator();
            int docUpto = 0;

            while (docUpto < maxDoc)
            {
                int ord = (int)iter.Next();
                docUpto++;
                // TODO: make reusable Number
                yield return ordMap[ord];
            }
        }
    }
}
