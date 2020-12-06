using Lucene.Net.Diagnostics;
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

    using Mutable = Lucene.Net.Util.Packed.PackedInt32s.Mutable;

    /// <summary>
    /// A <see cref="PagedMutable"/>. This class slices data into fixed-size blocks
    /// which have the same number of bits per value. It can be a useful replacement
    /// for <see cref="PackedInt32s.Mutable"/> to store more than 2B values.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class PagedMutable : AbstractPagedMutable<PagedMutable>
    {
        internal readonly PackedInt32s.Format format;

        /// <summary>
        /// Create a new <see cref="PagedMutable"/> instance.
        /// </summary>
        /// <param name="size"> The number of values to store. </param>
        /// <param name="pageSize"> The number of values per page. </param>
        /// <param name="bitsPerValue"> The number of bits per value. </param>
        /// <param name="acceptableOverheadRatio"> An acceptable overhead ratio. </param>
        public PagedMutable(long size, int pageSize, int bitsPerValue, float acceptableOverheadRatio)
            : this(size, pageSize, PackedInt32s.FastestFormatAndBits(pageSize, bitsPerValue, acceptableOverheadRatio))
        {
            FillPages();
        }

        internal PagedMutable(long size, int pageSize, PackedInt32s.FormatAndBits formatAndBits)
            : this(size, pageSize, formatAndBits.BitsPerValue, formatAndBits.Format)
        {
        }

        internal PagedMutable(long size, int pageSize, int bitsPerValue, PackedInt32s.Format format)
            : base(bitsPerValue, size, pageSize)
        {
            this.format = format;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Mutable NewMutable(int valueCount, int bitsPerValue)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(this.bitsPerValue >= bitsPerValue);
            return PackedInt32s.GetMutable(valueCount, this.bitsPerValue, format);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override PagedMutable NewUnfilledCopy(long newSize)
        {
            return new PagedMutable(newSize, PageSize, bitsPerValue, format);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override long BaseRamBytesUsed()
        {
            return base.BaseRamBytesUsed() + RamUsageEstimator.NUM_BYTES_OBJECT_REF;
        }
    }
}