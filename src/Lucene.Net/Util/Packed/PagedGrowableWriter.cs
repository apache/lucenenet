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
    /// A <see cref="PagedGrowableWriter"/>. This class slices data into fixed-size blocks
    /// which have independent numbers of bits per value and grow on-demand.
    /// <para/>
    /// You should use this class instead of the <see cref="AbstractAppendingInt64Buffer"/> related ones only when
    /// you need random write-access. Otherwise this class will likely be slower and
    /// less memory-efficient.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class PagedGrowableWriter : AbstractPagedMutable<PagedGrowableWriter>
    {
        internal readonly float acceptableOverheadRatio;

        /// <summary>
        /// Create a new <see cref="PagedGrowableWriter"/> instance.
        /// </summary>
        /// <param name="size"> The number of values to store. </param>
        /// <param name="pageSize"> The number of values per page. </param>
        /// <param name="startBitsPerValue"> The initial number of bits per value. </param>
        /// <param name="acceptableOverheadRatio"> An acceptable overhead ratio. </param>
        public PagedGrowableWriter(long size, int pageSize, int startBitsPerValue, float acceptableOverheadRatio)
            : this(size, pageSize, startBitsPerValue, acceptableOverheadRatio, true)
        {
        }

        internal PagedGrowableWriter(long size, int pageSize, int startBitsPerValue, float acceptableOverheadRatio, bool fillPages)
            : base(startBitsPerValue, size, pageSize)
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
            if (fillPages)
            {
                FillPages();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Mutable NewMutable(int valueCount, int bitsPerValue)
        {
            return new GrowableWriter(bitsPerValue, valueCount, acceptableOverheadRatio);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override PagedGrowableWriter NewUnfilledCopy(long newSize)
        {
            return new PagedGrowableWriter(newSize, PageSize, bitsPerValue, acceptableOverheadRatio, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override long BaseRamBytesUsed()
        {
            return base.BaseRamBytesUsed() + RamUsageEstimator.NUM_BYTES_SINGLE;
        }
    }
}