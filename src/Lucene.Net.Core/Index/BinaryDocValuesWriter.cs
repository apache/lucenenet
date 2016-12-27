using System;
using System.Collections.Generic;

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

    using AppendingDeltaPackedLongBuffer = Lucene.Net.Util.Packed.AppendingDeltaPackedLongBuffer;
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Counter = Lucene.Net.Util.Counter;
    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;
    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using PagedBytes = Lucene.Net.Util.PagedBytes;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// Buffers up pending byte[] per doc, then flushes when
    ///  segment flushes.
    /// </summary>
    internal class BinaryDocValuesWriter : DocValuesWriter
    {
        /// <summary>
        /// Maximum length for a binary field. </summary>
        private static readonly int MAX_LENGTH = ArrayUtil.MAX_ARRAY_LENGTH;

        // 32 KB block sizes for PagedBytes storage:
        private const int BLOCK_BITS = 15;

        private readonly PagedBytes Bytes;
        private readonly DataOutput BytesOut;

        private readonly Counter IwBytesUsed;
        private readonly AppendingDeltaPackedLongBuffer Lengths;
        private FixedBitSet DocsWithField;
        private readonly FieldInfo FieldInfo;
        private int AddedValues;
        private long BytesUsed;

        public BinaryDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed)
        {
            this.FieldInfo = fieldInfo;
            this.Bytes = new PagedBytes(BLOCK_BITS);
            this.BytesOut = Bytes.GetDataOutput();
            this.Lengths = new AppendingDeltaPackedLongBuffer(PackedInts.COMPACT);
            this.IwBytesUsed = iwBytesUsed;
            this.DocsWithField = new FixedBitSet(64);
            this.BytesUsed = DocsWithFieldBytesUsed();
            iwBytesUsed.AddAndGet(BytesUsed);
        }

        public virtual void AddValue(int docID, BytesRef value)
        {
            if (docID < AddedValues)
            {
                throw new System.ArgumentException("DocValuesField \"" + FieldInfo.Name + "\" appears more than once in this document (only one value is allowed per field)");
            }
            if (value == null)
            {
                throw new System.ArgumentException("field=\"" + FieldInfo.Name + "\": null value not allowed");
            }
            if (value.Length > MAX_LENGTH)
            {
                throw new System.ArgumentException("DocValuesField \"" + FieldInfo.Name + "\" is too large, must be <= " + MAX_LENGTH);
            }

            // Fill in any holes:
            while (AddedValues < docID)
            {
                AddedValues++;
                Lengths.Add(0);
            }
            AddedValues++;
            Lengths.Add(value.Length);
            try
            {
                BytesOut.WriteBytes(value.Bytes, value.Offset, value.Length);
            }
            catch (System.IO.IOException ioe)
            {
                // Should never happen!
                throw new Exception(ioe.Message, ioe);
            }
            DocsWithField = FixedBitSet.EnsureCapacity(DocsWithField, docID);
            DocsWithField.Set(docID);
            UpdateBytesUsed();
        }

        private long DocsWithFieldBytesUsed()
        {
            // size of the long[] + some overhead
            return RamUsageEstimator.SizeOf(DocsWithField.Bits) + 64;
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = Lengths.RamBytesUsed() + Bytes.RamBytesUsed() + DocsWithFieldBytesUsed();
            IwBytesUsed.AddAndGet(newBytesUsed - BytesUsed);
            BytesUsed = newBytesUsed;
        }

        public override void Finish(int maxDoc)
        {
        }

        public override void Flush(SegmentWriteState state, DocValuesConsumer dvConsumer)
        {
            int maxDoc = state.SegmentInfo.DocCount;
            Bytes.Freeze(false);
            dvConsumer.AddBinaryField(FieldInfo, GetBytesIterator(maxDoc));
        }

        public override void Abort()
        {
        }

        private IEnumerable<BytesRef> GetBytesIterator(int maxDocParam)
        {
            // Use yield return instead of ucsom IEnumerable

            AppendingDeltaPackedLongBuffer.Iterator lengthsIterator = Lengths.GetIterator();
            int size = (int)Lengths.Size();
            DataInput bytesIterator = Bytes.GetDataInput();
            int maxDoc = maxDocParam;
            int upto = 0;

            while (upto < maxDoc)
            {
                BytesRef v = null;
                if (upto < size)
                {
                    int length = (int)lengthsIterator.Next();
                    var value = new BytesRef();
                    value.Grow(length);
                    value.Length = length;
                    bytesIterator.ReadBytes(value.Bytes, value.Offset, value.Length);

                    if (DocsWithField.Get(upto))
                    {
                        v = value;
                    }
                }

                upto++;
                yield return v;
            }
        }
    }
}