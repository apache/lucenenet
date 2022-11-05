using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Util.Packed
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
    /// Base implementation for <see cref="PagedMutable"/> and <see cref="PagedGrowableWriter"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class AbstractPagedMutable<T> : Int64Values where T : AbstractPagedMutable<T> // LUCENENET NOTE: made public rather than internal because has public subclasses
    {
        internal const int MIN_BLOCK_SIZE = 1 << 6;
        internal const int MAX_BLOCK_SIZE = 1 << 30;

        internal readonly long size;
        internal readonly int pageShift;
        internal readonly int pageMask;
        internal readonly PackedInt32s.Mutable[] subMutables;
        internal readonly int bitsPerValue;

        private protected AbstractPagedMutable(int bitsPerValue, long size, int pageSize) // LUCENENET: Changed from internal to private protected
        {
            this.bitsPerValue = bitsPerValue;
            this.size = size;
            pageShift = PackedInt32s.CheckBlockSize(pageSize, MIN_BLOCK_SIZE, MAX_BLOCK_SIZE);
            pageMask = pageSize - 1;
            int numPages = PackedInt32s.NumBlocks(size, pageSize);
            subMutables = new PackedInt32s.Mutable[numPages];
        }

        protected void FillPages()
        {
            int numPages = PackedInt32s.NumBlocks(size, PageSize);
            for (int i = 0; i < numPages; ++i)
            {
                // do not allocate for more entries than necessary on the last page
                int valueCount = i == numPages - 1 ? LastPageSize(size) : PageSize;
                subMutables[i] = NewMutable(valueCount, bitsPerValue);
            }
        }

        protected abstract PackedInt32s.Mutable NewMutable(int valueCount, int bitsPerValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int LastPageSize(long size)
        {
            int sz = IndexInPage(size);
            return sz == 0 ? PageSize : sz;
        }

        internal int PageSize => pageMask + 1;

        /// <summary>
        /// The number of values.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public long Count => size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int PageIndex(long index)
        {
            return (int)index.TripleShift(pageShift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int IndexInPage(long index)
        {
            return (int)index & pageMask;
        }

        public override sealed long Get(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < size);
            int pageIndex = PageIndex(index);
            int indexInPage = IndexInPage(index);
            return subMutables[pageIndex].Get(indexInPage);
        }

        /// <summary>
        /// Set value at <paramref name="index"/>. </summary>
        public void Set(long index, long value)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < size);
            int pageIndex = PageIndex(index);
            int indexInPage = IndexInPage(index);
            subMutables[pageIndex].Set(indexInPage, value);
        }

        protected virtual long BaseRamBytesUsed()
        {
            return RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_INT64 + 3 * RamUsageEstimator.NUM_BYTES_INT32;
        }

        /// <summary>
        /// Return the number of bytes used by this object. </summary>
        public virtual long RamBytesUsed()
        {
            long bytesUsed = RamUsageEstimator.AlignObjectSize(BaseRamBytesUsed());
            bytesUsed += RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + (long)RamUsageEstimator.NUM_BYTES_OBJECT_REF * subMutables.Length);
            foreach (PackedInt32s.Mutable gw in subMutables)
            {
                bytesUsed += gw.RamBytesUsed();
            }
            return bytesUsed;
        }

        protected abstract T NewUnfilledCopy(long newSize);

        /// <summary>
        /// Create a new copy of size <paramref name="newSize"/> based on the content of
        /// this buffer. This method is much more efficient than creating a new
        /// instance and copying values one by one.
        /// </summary>
        public T Resize(long newSize)
        {
            T copy = NewUnfilledCopy(newSize);
            int numCommonPages = Math.Min(copy.subMutables.Length, subMutables.Length);
            long[] copyBuffer = new long[1024];
            for (int i = 0; i < copy.subMutables.Length; ++i)
            {
                int valueCount = i == copy.subMutables.Length - 1 ? LastPageSize(newSize) : PageSize;
                int bpv = i < numCommonPages ? subMutables[i].BitsPerValue : this.bitsPerValue;
                copy.subMutables[i] = NewMutable(valueCount, bpv);
                if (i < numCommonPages)
                {
                    int copyLength = Math.Min(valueCount, subMutables[i].Count);
                    PackedInt32s.Copy(subMutables[i], 0, copy.subMutables[i], 0, copyLength, copyBuffer);
                }
            }
            return copy;
        }

        /// <summary>
        /// Similar to <see cref="ArrayUtil.Grow(long[], int)"/>. </summary>
        public T Grow(long minSize)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(minSize >= 0);
            if (minSize <= Count)
            {
                T result = (T)this;
                return result;
            }
            long extra = minSize.TripleShift(3);
            if (extra < 3)
            {
                extra = 3;
            }
            long newSize = minSize + extra;
            return Resize(newSize);
        }

        /// <summary>
        /// Similar to <see cref="ArrayUtil.Grow(long[])"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Grow()
        {
            return Grow(Count + 1);
        }

        public override sealed string ToString()
        {
            return this.GetType().Name + "(size=" + Count + ",pageSize=" + PageSize + ")";
        }
    }
}