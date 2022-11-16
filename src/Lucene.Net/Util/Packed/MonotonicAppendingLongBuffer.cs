using J2N.Numerics;
using Lucene.Net.Diagnostics;
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
    /// Utility class to buffer signed longs in memory, which is optimized for the
    /// case where the sequence is monotonic, although it can encode any sequence of
    /// arbitrary longs. It only supports appending.
    /// <para/>
    /// NOTE: This was MonotonicAppendingLongBuffer in Lucene.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class MonotonicAppendingInt64Buffer : AbstractAppendingInt64Buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ZigZagDecode(long n)
        {
            return (n.TripleShift(1) ^ -(n & 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ZigZagEncode(long n)
        {
            return (n >> 63) ^ (n << 1);
        }

        internal float[] averages;
        internal long[] minValues;

        /// <param name="initialPageCount">        The initial number of pages. </param>
        /// <param name="pageSize">                The size of a single page. </param>
        /// <param name="acceptableOverheadRatio"> An acceptable overhead ratio per value. </param>
        public MonotonicAppendingInt64Buffer(int initialPageCount, int pageSize, float acceptableOverheadRatio)
            : base(initialPageCount, pageSize, acceptableOverheadRatio)
        {
            averages = new float[values.Length];
            minValues = new long[values.Length];
        }

        /// <summary>
        /// Create an <see cref="MonotonicAppendingInt64Buffer"/> with initialPageCount=16,
        /// pageSize=1024 and acceptableOverheadRatio=<see cref="PackedInt32s.DEFAULT"/>.
        /// </summary>
        public MonotonicAppendingInt64Buffer()
            : this(16, 1024, PackedInt32s.DEFAULT)
        {
        }

        /// <summary>
        /// Create an <see cref="AppendingDeltaPackedInt64Buffer"/> with initialPageCount=16,
        /// pageSize=1024.
        /// </summary>
        public MonotonicAppendingInt64Buffer(float acceptableOverheadRatio)
            : this(16, 1024, acceptableOverheadRatio)
        {
        }

        internal override long Get(int block, int element)
        {
            if (block == valuesOff)
            {
                return pending[element];
            }
            else
            {
                // LUCENENET NOTE: IMPORTANT: The cast to float is critical here for it to work in x86
                long @base = minValues[block] + (long)(float)(averages[block] * (long)element);
                if (values[block] is null)
                {
                    return @base;
                }
                else
                {
                    return @base + ZigZagDecode(values[block].Get(element));
                }
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
                if (values[block] is null)
                {
                    int toFill = Math.Min(len, pending.Length - element);
                    for (int r = 0; r < toFill; r++, off++, element++)
                    {
                        // LUCENENET NOTE: IMPORTANT: The cast to float is critical here for it to work in x86
                        arr[off] = minValues[block] + (long)(float)(averages[block] * (long)element);
                    }
                    return toFill;
                }
                else
                {
                    /* packed block */
                    int read = values[block].Get(element, arr, off, len);
                    for (int r = 0; r < read; r++, off++, element++)
                    {
                        // LUCENENET NOTE: IMPORTANT: The cast to float is critical here for it to work in x86
                        arr[off] = minValues[block] + (long)(float)(averages[block] * (long)element) + ZigZagDecode(arr[off]);
                    }
                    return read;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void Grow(int newBlockCount)
        {
            base.Grow(newBlockCount);
            this.averages = Arrays.CopyOf(averages, newBlockCount);
            this.minValues = Arrays.CopyOf(minValues, newBlockCount);
        }

        internal override void PackPendingValues()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(pendingOff > 0);
            minValues[valuesOff] = pending[0];
            averages[valuesOff] = pendingOff == 1 ? 0 : (float)(pending[pendingOff - 1] - pending[0]) / (pendingOff - 1);

            for (int i = 0; i < pendingOff; ++i)
            {
                // LUCENENET NOTE: IMPORTANT: The cast to float is critical here for it to work in x86
                pending[i] = ZigZagEncode(pending[i] - minValues[valuesOff] - (long)(float)(averages[valuesOff] * (long)i));
            }
            long maxDelta = 0;
            for (int i = 0; i < pendingOff; ++i)
            {
                if (pending[i] < 0)
                {
                    maxDelta = -1;
                    break;
                }
                else
                {
                    maxDelta = Math.Max(maxDelta, pending[i]);
                }
            }
            if (maxDelta == 0)
            {
                values[valuesOff] = new PackedInt32s.NullReader(pendingOff);
            }
            else
            {
                int bitsRequired = maxDelta < 0 ? 64 : PackedInt32s.BitsRequired(maxDelta);
                PackedInt32s.Mutable mutable = PackedInt32s.GetMutable(pendingOff, bitsRequired, acceptableOverheadRatio);
                for (int i = 0; i < pendingOff; )
                {
                    i += mutable.Set(i, pending, i, pendingOff - i);
                }
                values[valuesOff] = mutable;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override long BaseRamBytesUsed()
        {
            return base.BaseRamBytesUsed() + 2 * RamUsageEstimator.NUM_BYTES_OBJECT_REF; // 2 additional arrays
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed()
        {
            return base.RamBytesUsed() + RamUsageEstimator.SizeOf(averages) + RamUsageEstimator.SizeOf(minValues);
        }
    }
}