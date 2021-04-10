using Lucene.Net.Codecs;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
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

    /// <summary>
    /// Buffers up pending <see cref="T:byte[]"/> per doc, deref and sorting via
    /// int ord, then flushes when segment flushes.
    /// </summary>
    internal class SortedDocValuesWriter : DocValuesWriter
    {
        internal readonly BytesRefHash hash;
        private readonly AppendingDeltaPackedInt64Buffer pending; // LUCENENET: marked readonly
        private readonly Counter iwBytesUsed;
        private long bytesUsed; // this currently only tracks differences in 'pending'
        private readonly FieldInfo fieldInfo;

        private const int EMPTY_ORD = -1;

        public SortedDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed)
        {
            this.fieldInfo = fieldInfo;
            this.iwBytesUsed = iwBytesUsed;
            hash = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(iwBytesUsed)), BytesRefHash.DEFAULT_CAPACITY, new BytesRefHash.DirectBytesStartArray(BytesRefHash.DEFAULT_CAPACITY, iwBytesUsed));
            pending = new AppendingDeltaPackedInt64Buffer(PackedInt32s.COMPACT);
            bytesUsed = pending.RamBytesUsed();
            iwBytesUsed.AddAndGet(bytesUsed);
        }

        public virtual void AddValue(int docID, BytesRef value)
        {
            if (docID < pending.Count)
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.Name + "\" appears more than once in this document (only one value is allowed per field)");
            }
            if (value is null)
            {
                throw new ArgumentNullException("field \"" + fieldInfo.Name + "\": null value not allowed"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (value.Length > (ByteBlockPool.BYTE_BLOCK_SIZE - 2))
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.Name + "\" is too large, must be <= " + (ByteBlockPool.BYTE_BLOCK_SIZE - 2));
            }

            // Fill in any holes:
            while (pending.Count < docID)
            {
                pending.Add(EMPTY_ORD);
            }

            AddOneValue(value);
        }

        public override void Finish(int maxDoc)
        {
            while (pending.Count < maxDoc)
            {
                pending.Add(EMPTY_ORD);
            }
            UpdateBytesUsed();
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

            pending.Add(termID);
            UpdateBytesUsed();
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = pending.RamBytesUsed();
            iwBytesUsed.AddAndGet(newBytesUsed - bytesUsed);
            bytesUsed = newBytesUsed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.SegmentInfo.DocCount;

            if (Debugging.AssertsEnabled) Debugging.Assert(pending.Count == maxDoc);
            int valueCount = hash.Count;

            int[] sortedValues = hash.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
            int[] ordMap = new int[valueCount];

            for (int ord = 0; ord < valueCount; ord++)
            {
                ordMap[sortedValues[ord]] = ord;
            }

            dvConsumer.AddSortedField(fieldInfo, GetBytesRefEnumberable(valueCount, sortedValues),
                // doc -> ord
                                      GetOrdsEnumberable(maxDoc, ordMap));
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

        private IEnumerable<long?> GetOrdsEnumberable(int maxDoc, int[] ordMap)
        {
            AppendingDeltaPackedInt64Buffer.Iterator iter = pending.GetIterator();
            if (Debugging.AssertsEnabled) Debugging.Assert(pending.Count == maxDoc);

            for (int i = 0; i < maxDoc; ++i)
            {
                int ord = (int)iter.Next();
                yield return ord == -1 ? ord : ordMap[ord];
            }
        }
    }
}