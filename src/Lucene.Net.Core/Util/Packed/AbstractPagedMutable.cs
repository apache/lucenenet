using System;
using System.Diagnostics;

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
    /// Base implementation for <seealso cref="PagedMutable"/> and <seealso cref="PagedGrowableWriter"/>.
    /// @lucene.internal
    /// </summary>
    public abstract class AbstractPagedMutable<T> : LongValues where T : AbstractPagedMutable<T> // LUCENENET NOTE: made public rather than internal because has public subclasses
    {
        internal static readonly int MIN_BLOCK_SIZE = 1 << 6;
        internal static readonly int MAX_BLOCK_SIZE = 1 << 30;

        internal readonly long size;
        internal readonly int pageShift;
        internal readonly int pageMask;
        internal readonly PackedInts.Mutable[] subMutables;
        internal readonly int bitsPerValue;

        internal AbstractPagedMutable(int bitsPerValue, long size, int pageSize)
        {
            this.bitsPerValue = bitsPerValue;
            this.size = size;
            pageShift = PackedInts.CheckBlockSize(pageSize, MIN_BLOCK_SIZE, MAX_BLOCK_SIZE);
            pageMask = pageSize - 1;
            int numPages = PackedInts.NumBlocks(size, pageSize);
            subMutables = new PackedInts.Mutable[numPages];
        }

        protected void FillPages()
        {
            int numPages = PackedInts.NumBlocks(size, PageSize);
            for (int i = 0; i < numPages; ++i)
            {
                // do not allocate for more entries than necessary on the last page
                int valueCount = i == numPages - 1 ? LastPageSize(size) : PageSize;
                subMutables[i] = NewMutable(valueCount, bitsPerValue);
            }
        }

        protected abstract PackedInts.Mutable NewMutable(int valueCount, int bitsPerValue);

        internal int LastPageSize(long size)
        {
            int sz = IndexInPage(size);
            return sz == 0 ? PageSize : sz;
        }

        internal int PageSize // LUCENENET TODO: change to PageCount ?
        {
            get { return pageMask + 1; }
        }

        /// <summary>
        /// The number of values. </summary>
        public long Size // LUCENENET TODO: change to Count
        {
            get { return size; }
        }

        internal int PageIndex(long index)
        {
            return (int)((long)((ulong)index >> pageShift));
        }

        internal int IndexInPage(long index)
        {
            return (int)index & pageMask;
        }

        public override sealed long Get(long index)
        {
            Debug.Assert(index >= 0 && index < size);
            int pageIndex = PageIndex(index);
            int indexInPage = IndexInPage(index);
            return subMutables[pageIndex].Get(indexInPage);
        }

        /// <summary>
        /// Set value at <code>index</code>. </summary>
        public void Set(long index, long value)
        {
            Debug.Assert(index >= 0 && index < size);
            int pageIndex = PageIndex(index);
            int indexInPage = IndexInPage(index);
            subMutables[pageIndex].Set(indexInPage, value);
        }

        protected virtual long BaseRamBytesUsed()
        {
            return RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_LONG + 3 * RamUsageEstimator.NUM_BYTES_INT;
        }

        /// <summary>
        /// Return the number of bytes used by this object. </summary>
        public virtual long RamBytesUsed()
        {
            long bytesUsed = RamUsageEstimator.AlignObjectSize(BaseRamBytesUsed());
            bytesUsed += RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + (long)RamUsageEstimator.NUM_BYTES_OBJECT_REF * subMutables.Length);
            foreach (PackedInts.Mutable gw in subMutables)
            {
                bytesUsed += gw.RamBytesUsed();
            }
            return bytesUsed;
        }

        protected abstract T NewUnfilledCopy(long newSize);

        /// <summary>
        /// Create a new copy of size <code>newSize</code> based on the content of
        ///  this buffer. this method is much more efficient than creating a new
        ///  instance and copying values one by one.
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
                    int copyLength = Math.Min(valueCount, subMutables[i].Size);
                    PackedInts.Copy(subMutables[i], 0, copy.subMutables[i], 0, copyLength, copyBuffer);
                }
            }
            return copy;
        }

        /// <summary>
        /// Similar to <seealso cref="ArrayUtil#grow(long[], int)"/>. </summary>
        public T Grow(long minSize)
        {
            Debug.Assert(minSize >= 0);
            if (minSize <= Size)
            {
                T result = (T)this;
                return result;
            }
            long extra = (long)((ulong)minSize >> 3);
            if (extra < 3)
            {
                extra = 3;
            }
            long newSize = minSize + extra;
            return Resize(newSize);
        }

        /// <summary>
        /// Similar to <seealso cref="ArrayUtil#grow(long[])"/>. </summary>
        public T Grow()
        {
            return Grow(Size + 1);
        }

        public override sealed string ToString()
        {
            return this.GetType().Name + "(size=" + Size + ",pageSize=" + PageSize + ")";
        }
    }
}