using Lucene.Net.Support;
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
    /// Utility class to buffer a list of signed longs in memory. This class only
    /// supports appending and is optimized for the case where values are close to
    /// each other.
    /// <para/>
    /// NOTE: This was AppendingDeltaPackedLongBuffer in Lucene
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class AppendingDeltaPackedInt64Buffer : AbstractAppendingInt64Buffer
    {
        internal long[] minValues;

        /// <summary>
        /// Create <see cref="AppendingDeltaPackedInt64Buffer"/>. </summary>
        /// <param name="initialPageCount">        The initial number of pages. </param>
        /// <param name="pageSize">                The size of a single page. </param>
        /// <param name="acceptableOverheadRatio"> An acceptable overhead ratio per value. </param>
        public AppendingDeltaPackedInt64Buffer(int initialPageCount, int pageSize, float acceptableOverheadRatio)
            : base(initialPageCount, pageSize, acceptableOverheadRatio)
        {
            minValues = new long[values.Length];
        }

        /// <summary>
        /// Create an <see cref="AppendingDeltaPackedInt64Buffer"/> with initialPageCount=16,
        /// pageSize=1024 and acceptableOverheadRatio=<see cref="PackedInt32s.DEFAULT"/>.
        /// </summary>
        public AppendingDeltaPackedInt64Buffer()
            : this(16, 1024, PackedInt32s.DEFAULT)
        {
        }

        /// <summary>
        /// Create an <see cref="AppendingDeltaPackedInt64Buffer"/> with initialPageCount=16,
        /// pageSize=1024.
        /// </summary>
        public AppendingDeltaPackedInt64Buffer(float acceptableOverheadRatio)
            : this(16, 1024, acceptableOverheadRatio)
        {
        }

        internal override long Get(int block, int element)
        {
            if (block == valuesOff)
            {
                return pending[element];
            }
            else if (values[block] is null)
            {
                return minValues[block];
            }
            else
            {
                return minValues[block] + values[block].Get(element);
            }
        }

        internal override int Get(int block, int element, long[] arr, int off, int len)
        {
            if (block == valuesOff)
            {
                int sysCopyToRead = Math.Min(len, pendingOff - element);
                Arrays.Copy(pending, element, arr, off, sysCopyToRead);
                return sysCopyToRead;
            }
            else
            {
                /* packed block */
                int read = values[block].Get(element, arr, off, len);
                long d = minValues[block];
                for (int r = 0; r < read; r++, off++)
                {
                    arr[off] += d;
                }
                return read;
            }
        }

        internal override void PackPendingValues()
        {
            // compute max delta
            long minValue = pending[0];
            long maxValue = pending[0];
            for (int i = 1; i < pendingOff; ++i)
            {
                minValue = Math.Min(minValue, pending[i]);
                maxValue = Math.Max(maxValue, pending[i]);
            }
            long delta = maxValue - minValue;

            minValues[valuesOff] = minValue;
            if (delta == 0)
            {
                values[valuesOff] = new PackedInt32s.NullReader(pendingOff);
            }
            else
            {
                // build a new packed reader
                int bitsRequired = delta < 0 ? 64 : PackedInt32s.BitsRequired(delta);
                for (int i = 0; i < pendingOff; ++i)
                {
                    pending[i] -= minValue;
                }
                PackedInt32s.Mutable mutable = PackedInt32s.GetMutable(pendingOff, bitsRequired, acceptableOverheadRatio);
                for (int i = 0; i < pendingOff; )
                {
                    i += mutable.Set(i, pending, i, pendingOff - i);
                }
                values[valuesOff] = mutable;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void Grow(int newBlockCount)
        {
            base.Grow(newBlockCount);
            this.minValues = Arrays.CopyOf(minValues, newBlockCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override long BaseRamBytesUsed()
        {
            return base.BaseRamBytesUsed() + RamUsageEstimator.NUM_BYTES_OBJECT_REF; // additional array
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed()
        {
            return base.RamBytesUsed() + RamUsageEstimator.SizeOf(minValues);
        }
    }
}