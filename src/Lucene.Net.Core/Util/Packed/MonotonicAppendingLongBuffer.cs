using Lucene.Net.Support;
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
    /// Utility class to buffer signed longs in memory, which is optimized for the
    /// case where the sequence is monotonic, although it can encode any sequence of
    /// arbitrary longs. It only supports appending.
    ///
    /// @lucene.internal
    /// </summary>
    public sealed class MonotonicAppendingLongBuffer : AbstractAppendingLongBuffer
    {
        internal static long ZigZagDecode(long n)
        {
            return (((long)((ulong)n >> 1)) ^ -(n & 1));
        }

        internal static long ZigZagEncode(long n)
        {
            return (n >> 63) ^ (n << 1);
        }

        internal float[] Averages;
        internal long[] MinValues;

        /// <param name="initialPageCount">        the initial number of pages </param>
        /// <param name="pageSize">                the size of a single page </param>
        /// <param name="acceptableOverheadRatio"> an acceptable overhead ratio per value </param>
        public MonotonicAppendingLongBuffer(int initialPageCount, int pageSize, float acceptableOverheadRatio)
            : base(initialPageCount, pageSize, acceptableOverheadRatio)
        {
            Averages = new float[Values.Length];
            MinValues = new long[Values.Length];
        }

        /// <summary>
        /// Create an <seealso cref="MonotonicAppendingLongBuffer"/> with initialPageCount=16,
        /// pageSize=1024 and acceptableOverheadRatio=<seealso cref="PackedInts#DEFAULT"/>
        /// </summary>
        public MonotonicAppendingLongBuffer()
            : this(16, 1024, PackedInts.DEFAULT)
        {
        }

        /// <summary>
        /// Create an <seealso cref="AppendingDeltaPackedLongBuffer"/> with initialPageCount=16,
        /// pageSize=1024
        /// </summary>
        public MonotonicAppendingLongBuffer(float acceptableOverheadRatio)
            : this(16, 1024, acceptableOverheadRatio)
        {
        }

        public override Iterator GetIterator() // LUCENENET TODO: This can be handled by the base class
        {
            return new Iterator(this);
        }

        internal override long Get(int block, int element)
        {
            if (block == ValuesOff)
            {
                return Pending[element];
            }
            else
            {
                long @base = MinValues[block] + (long)(Averages[block] * (long)element);
                if (Values[block] == null)
                {
                    return @base;
                }
                else
                {
                    return @base + ZigZagDecode(Values[block].Get(element));
                }
            }
        }

        internal override int Get(int block, int element, long[] arr, int off, int len)
        {
            if (block == ValuesOff)
            {
                int sysCopyToRead = Math.Min(len, PendingOff - element);
                Array.Copy(Pending, element, arr, off, sysCopyToRead);
                return sysCopyToRead;
            }
            else
            {
                if (Values[block] == null)
                {
                    int toFill = Math.Min(len, Pending.Length - element);
                    for (int r = 0; r < toFill; r++, off++, element++)
                    {
                        arr[off] = MinValues[block] + (long)(Averages[block] * (long)element);
                    }
                    return toFill;
                }
                else
                {
                    /* packed block */
                    int read = Values[block].Get(element, arr, off, len);
                    for (int r = 0; r < read; r++, off++, element++)
                    {
                        arr[off] = MinValues[block] + (long)(Averages[block] * (long)element) + ZigZagDecode(arr[off]);
                    }
                    return read;
                }
            }
        }

        internal override void Grow(int newBlockCount)
        {
            base.Grow(newBlockCount);
            this.Averages = Arrays.CopyOf(Averages, newBlockCount);
            this.MinValues = Arrays.CopyOf(MinValues, newBlockCount);
        }

        internal override void PackPendingValues()
        {
            Debug.Assert(PendingOff > 0);
            MinValues[ValuesOff] = Pending[0];
            Averages[ValuesOff] = PendingOff == 1 ? 0 : (float)(Pending[PendingOff - 1] - Pending[0]) / (PendingOff - 1);

            for (int i = 0; i < PendingOff; ++i)
            {
                Pending[i] = ZigZagEncode(Pending[i] - MinValues[ValuesOff] - (long)(Averages[ValuesOff] * (long)i));
            }
            long maxDelta = 0;
            for (int i = 0; i < PendingOff; ++i)
            {
                if (Pending[i] < 0)
                {
                    maxDelta = -1;
                    break;
                }
                else
                {
                    maxDelta = Math.Max(maxDelta, Pending[i]);
                }
            }
            if (maxDelta == 0)
            {
                Values[ValuesOff] = new PackedInts.NullReader(PendingOff);
            }
            else
            {
                int bitsRequired = maxDelta < 0 ? 64 : PackedInts.BitsRequired(maxDelta);
                PackedInts.Mutable mutable = PackedInts.GetMutable(PendingOff, bitsRequired, AcceptableOverheadRatio);
                for (int i = 0; i < PendingOff; )
                {
                    i += mutable.Set(i, Pending, i, PendingOff - i);
                }
                Values[ValuesOff] = mutable;
            }
        }

        internal override long BaseRamBytesUsed()
        {
            return base.BaseRamBytesUsed() + 2 * RamUsageEstimator.NUM_BYTES_OBJECT_REF; // 2 additional arrays
        }

        public override long RamBytesUsed()
        {
            return base.RamBytesUsed() + RamUsageEstimator.SizeOf(Averages) + RamUsageEstimator.SizeOf(MinValues);
        }
    }
}