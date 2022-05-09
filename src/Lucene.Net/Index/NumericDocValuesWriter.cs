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

    using AppendingDeltaPackedInt64Buffer = Lucene.Net.Util.Packed.AppendingDeltaPackedInt64Buffer;
    using Counter = Lucene.Net.Util.Counter;
    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// Buffers up pending long per doc, then flushes when
    /// segment flushes.
    /// </summary>
    internal class NumericDocValuesWriter : DocValuesWriter
    {
        private const long MISSING = 0L;

        private readonly AppendingDeltaPackedInt64Buffer pending; // LUCENENET: marked readonly
        private readonly Counter iwBytesUsed;
        private long bytesUsed;
        private FixedBitSet docsWithField;
        private readonly FieldInfo fieldInfo;

        public NumericDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed, bool trackDocsWithField)
        {
            pending = new AppendingDeltaPackedInt64Buffer(PackedInt32s.COMPACT);
            docsWithField = trackDocsWithField ? new FixedBitSet(64) : null;
            bytesUsed = pending.RamBytesUsed() + DocsWithFieldBytesUsed();
            this.fieldInfo = fieldInfo;
            this.iwBytesUsed = iwBytesUsed;
            iwBytesUsed.AddAndGet(bytesUsed);
        }

        public virtual void AddValue(int docID, long value)
        {
            if (docID < pending.Count)
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.Name + "\" appears more than once in this document (only one value is allowed per field)");
            }

            // Fill in any holes:
            for (int i = (int)pending.Count; i < docID; ++i)
            {
                pending.Add(MISSING);
            }

            pending.Add(value);
            if (docsWithField != null)
            {
                docsWithField = FixedBitSet.EnsureCapacity(docsWithField, docID);
                docsWithField.Set(docID);
            }

            UpdateBytesUsed();
        }

        private long DocsWithFieldBytesUsed()
        {
            // size of the long[] + some overhead
            return docsWithField is null ? 0 : RamUsageEstimator.SizeOf(docsWithField.Bits) + 64;
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = pending.RamBytesUsed() + DocsWithFieldBytesUsed();
            iwBytesUsed.AddAndGet(newBytesUsed - bytesUsed);
            bytesUsed = newBytesUsed;
        }

        public override void Finish(int maxDoc)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.SegmentInfo.DocCount;

            dvConsumer.AddNumericField(fieldInfo, GetNumericIterator(maxDoc));
        }

        private IEnumerable<long?> GetNumericIterator(int maxDoc)
        {
            // LUCENENET specific: using yield return instead of custom iterator type. Much less code.
            AbstractAppendingInt64Buffer.Iterator iter = pending.GetIterator();
            int size = (int)pending.Count;
            int upto = 0;

            while (upto < maxDoc)
            {
                long? value;
                if (upto < size)
                {
                    var v = iter.Next();
                    if (docsWithField is null || docsWithField.Get(upto))
                    {
                        value = v;
                    }
                    else
                    {
                        value = null;
                    }
                }
                else
                {
                    value = docsWithField != null ? (long?) null : MISSING;
                }
                upto++;
                // TODO: make reusable Number
                yield return value;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Abort()
        {
        }
    }
}