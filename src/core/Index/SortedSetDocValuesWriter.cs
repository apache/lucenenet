using Lucene.Net.Codecs;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class SortedSetDocValuesWriter : DocValuesWriter
    {
        internal readonly BytesRefHash hash;
        private AppendingLongBuffer pending; // stream of all termIDs
        private AppendingLongBuffer pendingCounts; // termIDs per doc
        private readonly Counter iwBytesUsed;
        private long bytesUsed; // this only tracks differences in 'pending' and 'pendingCounts'
        private readonly FieldInfo fieldInfo;
        private int currentDoc;
        private int[] currentValues = new int[8];
        private int currentUpto = 0;
        private int maxCount = 0;

        public SortedSetDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed)
        {
            this.fieldInfo = fieldInfo;
            this.iwBytesUsed = iwBytesUsed;
            hash = new BytesRefHash(
                new ByteBlockPool(
                    new ByteBlockPool.DirectTrackingAllocator(iwBytesUsed)),
                    BytesRefHash.DEFAULT_CAPACITY,
                    new BytesRefHash.DirectBytesStartArray(BytesRefHash.DEFAULT_CAPACITY, iwBytesUsed));
            pending = new AppendingLongBuffer();
            pendingCounts = new AppendingLongBuffer();
            bytesUsed = pending.RamBytesUsed + pendingCounts.RamBytesUsed;
            iwBytesUsed.AddAndGet(bytesUsed);
        }

        public void AddValue(int docID, BytesRef value)
        {
            if (value == null)
            {
                throw new ArgumentException("field \"" + fieldInfo.name + "\": null value not allowed");
            }
            if (value.length > (ByteBlockPool.BYTE_BLOCK_SIZE - 2))
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.name + "\" is too large, must be <= " + (ByteBlockPool.BYTE_BLOCK_SIZE - 2));
            }

            if (docID != currentDoc)
            {
                FinishCurrentDoc();
            }

            // Fill in any holes:
            while (currentDoc < docID)
            {
                pendingCounts.Add(0); // no values
                currentDoc++;
            }

            AddOneValue(value);
            UpdateBytesUsed();
        }

        // finalize currentDoc: this deduplicates the current term ids
        private void FinishCurrentDoc()
        {
            Array.Sort(currentValues, 0, currentUpto);
            int lastValue = -1;
            int count = 0;
            for (int i = 0; i < currentUpto; i++)
            {
                int termID = currentValues[i];
                // if its not a duplicate
                if (termID != lastValue)
                {
                    pending.Add(termID); // record the term id
                    count++;
                }
                lastValue = termID;
            }
            // record the number of unique term ids for this doc
            pendingCounts.Add(count);
            maxCount = Math.Max(maxCount, count);
            currentUpto = 0;
            currentDoc++;
        }

        internal override void Finish(int maxDoc)
        {
            FinishCurrentDoc();

            // fill in any holes
            for (int i = currentDoc; i < maxDoc; i++)
            {
                pendingCounts.Add(0); // no values
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

            if (currentUpto == currentValues.Length)
            {
                currentValues = ArrayUtil.Grow(currentValues, currentValues.Length + 1);
                // reserve additional space for max # values per-doc
                // when flushing, we need an int[] to sort the mapped-ords within the doc
                iwBytesUsed.AddAndGet((currentValues.Length - currentUpto) * 2 * RamUsageEstimator.NUM_BYTES_INT);
            }

            currentValues[currentUpto] = termID;
            currentUpto++;
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = pending.RamBytesUsed + pendingCounts.RamBytesUsed;
            iwBytesUsed.AddAndGet(newBytesUsed - bytesUsed);
            bytesUsed = newBytesUsed;
        }

        internal override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.segmentInfo.DocCount;
            int maxCountPerDoc = maxCount;
            //assert pendingCounts.size() == maxDoc;
            int valueCount = hash.Size;

            int[] sortedValues = hash.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
            int[] ordMap = new int[valueCount];

            for (int ord = 0; ord < valueCount; ord++)
            {
                ordMap[sortedValues[ord]] = ord;
            }

            dvConsumer.AddSortedSetField(fieldInfo,
                GetValuesIterator(sortedValues, valueCount), 
                GetOrdCountIterator(maxDoc), 
                GetOrdsIterator(ordMap, maxCountPerDoc));
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

        private IEnumerable<long> GetOrdsIterator(int[] ordMap, int maxCount)
        {
            // .NET Port: using yield return instead of custom iterator type. Much less code.

            AppendingLongBuffer.Iterator iter = pending.GetIterator();
            AppendingLongBuffer.Iterator counts = pendingCounts.GetIterator();
            long numOrds = pending.Size;
            long ordUpto = 0L;

            int[] currentDoc = new int[maxCount];
            int currentUpto = 0;
            int currentLength = 0;

            while (ordUpto < numOrds)
            {
                while (currentUpto == currentLength)
                {
                    // refill next doc, and sort remapped ords within the doc.
                    currentUpto = 0;
                    currentLength = (int)counts.Next();
                    for (int i = 0; i < currentLength; i++)
                    {
                        currentDoc[i] = ordMap[(int)iter.Next()];
                    }
                    Array.Sort(currentDoc, 0, currentLength);
                }
                int ord = currentDoc[currentUpto];
                currentUpto++;
                ordUpto++;
                // TODO: make reusable Number
                yield return ord;
            }
        }

        private IEnumerable<int> GetOrdCountIterator(int maxDoc)
        {
            // .NET Port: using yield return instead of custom iterator type. Much less code.

            AppendingLongBuffer.Iterator iter = pendingCounts.GetIterator();
            int docUpto = 0;

            while (docUpto < maxDoc)
            {
                docUpto++;
                // TODO: make reusable Number
                yield return (int)iter.Next();
            }
        }
    }
}
