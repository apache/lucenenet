using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Index
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

    using AppendingDeltaPackedInt64Buffer = Lucene.Net.Util.Packed.AppendingDeltaPackedInt64Buffer;
    using AppendingPackedInt64Buffer = Lucene.Net.Util.Packed.AppendingPackedInt64Buffer;
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using BytesRefHash = Lucene.Net.Util.BytesRefHash;
    using Counter = Lucene.Net.Util.Counter;
    using DirectBytesStartArray = Lucene.Net.Util.BytesRefHash.DirectBytesStartArray;
    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// Buffers up pending <see cref="T:byte[]"/>s per doc, deref and sorting via
    /// int ord, then flushes when segment flushes.
    /// </summary>
    internal class SortedSetDocValuesWriter : DocValuesWriter
    {
        internal readonly BytesRefHash hash;
        private readonly AppendingPackedInt64Buffer pending; // stream of all termIDs // LUCENENET: marked readonly
        private readonly AppendingDeltaPackedInt64Buffer pendingCounts; // termIDs per doc // LUCENENET: marked readonly
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
            hash = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(iwBytesUsed)), BytesRefHash.DEFAULT_CAPACITY, new DirectBytesStartArray(BytesRefHash.DEFAULT_CAPACITY, iwBytesUsed));
            pending = new AppendingPackedInt64Buffer(PackedInt32s.COMPACT);
            pendingCounts = new AppendingDeltaPackedInt64Buffer(PackedInt32s.COMPACT);
            bytesUsed = pending.RamBytesUsed() + pendingCounts.RamBytesUsed();
            iwBytesUsed.AddAndGet(bytesUsed);
        }

        public virtual void AddValue(int docID, BytesRef value)
        {
            if (value is null)
            {
                throw new ArgumentNullException("field \"" + fieldInfo.Name + "\": null value not allowed"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (value.Length > (ByteBlockPool.BYTE_BLOCK_SIZE - 2))
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.Name + "\" is too large, must be <= " + (ByteBlockPool.BYTE_BLOCK_SIZE - 2));
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

        public override void Finish(int maxDoc)
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
                iwBytesUsed.AddAndGet(2 * RamUsageEstimator.NUM_BYTES_INT32);
            }

            if (currentUpto == currentValues.Length)
            {
                currentValues = ArrayUtil.Grow(currentValues, currentValues.Length + 1);
                // reserve additional space for max # values per-doc
                // when flushing, we need an int[] to sort the mapped-ords within the doc
                iwBytesUsed.AddAndGet((currentValues.Length - currentUpto) * 2 * RamUsageEstimator.NUM_BYTES_INT32);
            }

            currentValues[currentUpto] = termID;
            currentUpto++;
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = pending.RamBytesUsed() + pendingCounts.RamBytesUsed();
            iwBytesUsed.AddAndGet(newBytesUsed - bytesUsed);
            bytesUsed = newBytesUsed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.SegmentInfo.DocCount;
            int maxCountPerDoc = maxCount;
            if (Debugging.AssertsEnabled) Debugging.Assert(pendingCounts.Count == maxDoc);
            int valueCount = hash.Count;

            int[] sortedValues = hash.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
            int[] ordMap = new int[valueCount];

            for (int ord = 0; ord < valueCount; ord++)
            {
                ordMap[sortedValues[ord]] = ord;
            }

            dvConsumer.AddSortedSetField(fieldInfo, GetBytesRefEnumberable(valueCount, sortedValues),

                                      // doc -> ordCount
                                      GetOrdsEnumberable(maxDoc),

                                      // ords
                                      GetOrdCountEnumberable(maxCountPerDoc, ordMap));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Abort()
        {
        }

        private IEnumerable<BytesRef> GetBytesRefEnumberable(int valueCount, int[] sortedValues)
        {
            var scratch = new BytesRef();

            for (int i = 0; i < valueCount; ++i)
            {
                yield return hash.Get(sortedValues[i], scratch);
            }
        }

        private IEnumerable<long?> GetOrdsEnumberable(int maxDoc)
        {
            AppendingDeltaPackedInt64Buffer.Iterator iter = pendingCounts.GetIterator();

            if (Debugging.AssertsEnabled) Debugging.Assert(pendingCounts.Count == maxDoc,"MaxDoc: {0}, pending.Count: {1}", maxDoc, pending.Count);

            for (int i = 0; i < maxDoc; ++i)
            {
                yield return iter.Next();
            }
        }

        private IEnumerable<long?> GetOrdCountEnumberable(int maxCountPerDoc, int[] ordMap)
        {
            int currentUpTo = 0, currentLength = 0;
            AppendingPackedInt64Buffer.Iterator iter = pending.GetIterator();
            AppendingDeltaPackedInt64Buffer.Iterator counts = pendingCounts.GetIterator();
            int[] currentDoc = new int[maxCountPerDoc];

            for (long i = 0; i < pending.Count; ++i)
            {
                while (currentUpTo == currentLength)
                {
                    // refill next doc, and sort remapped ords within the doc.
                    currentUpTo = 0;
                    currentLength = (int)counts.Next();
                    for (int j = 0; j < currentLength; j++)
                    {
                        currentDoc[j] = ordMap[(int)iter.Next()];
                    }
                    Array.Sort(currentDoc, 0, currentLength);
                }
                int ord = currentDoc[currentUpTo];
                currentUpTo++;
                yield return ord;
            }
        }
    }
}