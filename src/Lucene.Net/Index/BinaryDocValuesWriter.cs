using System;
using System.Collections.Generic;
using System.IO;
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
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Counter = Lucene.Net.Util.Counter;
    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;
    using DocValuesConsumer = Lucene.Net.Codecs.DocValuesConsumer;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using PagedBytes = Lucene.Net.Util.PagedBytes;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// Buffers up pending <see cref="T:byte[]"/> per doc, then flushes when
    /// segment flushes.
    /// </summary>
    internal class BinaryDocValuesWriter : DocValuesWriter
    {
        /// <summary>
        /// Maximum length for a binary field. </summary>
        private static readonly int MAX_LENGTH = ArrayUtil.MAX_ARRAY_LENGTH;

        // 32 KB block sizes for PagedBytes storage:
        private const int BLOCK_BITS = 15;

        private readonly PagedBytes bytes;
        private readonly DataOutput bytesOut;

        private readonly Counter iwBytesUsed;
        private readonly AppendingDeltaPackedInt64Buffer lengths;
        private FixedBitSet docsWithField;
        private readonly FieldInfo fieldInfo;
        private int addedValues;
        private long bytesUsed;

        public BinaryDocValuesWriter(FieldInfo fieldInfo, Counter iwBytesUsed)
        {
            this.fieldInfo = fieldInfo;
            this.bytes = new PagedBytes(BLOCK_BITS);
            this.bytesOut = bytes.GetDataOutput();
            this.lengths = new AppendingDeltaPackedInt64Buffer(PackedInt32s.COMPACT);
            this.iwBytesUsed = iwBytesUsed;
            this.docsWithField = new FixedBitSet(64);
            this.bytesUsed = DocsWithFieldBytesUsed();
            iwBytesUsed.AddAndGet(bytesUsed);
        }

        public virtual void AddValue(int docID, BytesRef value)
        {
            if (docID < addedValues)
            {
                throw new ArgumentOutOfRangeException(nameof(docID), "DocValuesField \"" + fieldInfo.Name + "\" appears more than once in this document (only one value is allowed per field)"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            if (value is null)
            {
                throw new ArgumentNullException("field=\"" + fieldInfo.Name + "\": null value not allowed"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            if (value.Length > MAX_LENGTH)
            {
                throw new ArgumentException("DocValuesField \"" + fieldInfo.Name + "\" is too large, must be <= " + MAX_LENGTH);
            }

            // Fill in any holes:
            while (addedValues < docID)
            {
                addedValues++;
                lengths.Add(0);
            }
            addedValues++;
            lengths.Add(value.Length);
            try
            {
                bytesOut.WriteBytes(value.Bytes, value.Offset, value.Length);
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                // Should never happen!
                throw RuntimeException.Create(ioe);
            }
            docsWithField = FixedBitSet.EnsureCapacity(docsWithField, docID);
            docsWithField.Set(docID);
            UpdateBytesUsed();
        }

        private long DocsWithFieldBytesUsed()
        {
            // size of the long[] + some overhead
            return RamUsageEstimator.SizeOf(docsWithField.Bits) + 64;
        }

        private void UpdateBytesUsed()
        {
            long newBytesUsed = lengths.RamBytesUsed() + bytes.RamBytesUsed() + DocsWithFieldBytesUsed();
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
            bytes.Freeze(false);
            dvConsumer.AddBinaryField(fieldInfo, GetBytesIterator(maxDoc));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Abort()
        {
        }

        private IEnumerable<BytesRef> GetBytesIterator(int maxDocParam)
        {
            // Use yield return instead of ucsom IEnumerable
            var value = new BytesRef();
            AppendingDeltaPackedInt64Buffer.Iterator lengthsIterator = lengths.GetIterator();
            int size = (int)lengths.Count;
            DataInput bytesIterator = bytes.GetDataInput();
            int maxDoc = maxDocParam;
            int upto = 0;

            while (upto < maxDoc)
            {
                BytesRef v = null;
                if (upto < size)
                {
                    int length = (int)lengthsIterator.Next();
                    value.Grow(length);
                    value.Length = length;
                    try
                    {
                        bytesIterator.ReadBytes(value.Bytes, value.Offset, value.Length);
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        // Should never happen!
                        throw RuntimeException.Create(ioe);
                    }

                    if (docsWithField.Get(upto))
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