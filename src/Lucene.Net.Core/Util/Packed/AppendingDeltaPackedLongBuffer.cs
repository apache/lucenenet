using Lucene.Net.Support;
using System;

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
    /// Utility class to buffer a list of signed longs in memory. this class only
    /// supports appending and is optimized for the case where values are close to
    /// each other.
    ///
    /// @lucene.internal
    /// </summary>
    public sealed class AppendingDeltaPackedLongBuffer : AbstractAppendingLongBuffer
    {
        internal long[] MinValues;

        /// <summary>
        /// Create <seealso cref="AppendingDeltaPackedLongBuffer"/> </summary>
        /// <param name="initialPageCount">        the initial number of pages </param>
        /// <param name="pageSize">                the size of a single page </param>
        /// <param name="acceptableOverheadRatio"> an acceptable overhead ratio per value </param>
        public AppendingDeltaPackedLongBuffer(int initialPageCount, int pageSize, float acceptableOverheadRatio)
            : base(initialPageCount, pageSize, acceptableOverheadRatio)
        {
            MinValues = new long[Values.Length];
        }

        /// <summary>
        /// Create an <seealso cref="AppendingDeltaPackedLongBuffer"/> with initialPageCount=16,
        /// pageSize=1024 and acceptableOverheadRatio=<seealso cref="PackedInts#DEFAULT"/>
        /// </summary>
        public AppendingDeltaPackedLongBuffer()
            : this(16, 1024, PackedInts.DEFAULT)
        {
        }

        /// <summary>
        /// Create an <seealso cref="AppendingDeltaPackedLongBuffer"/> with initialPageCount=16,
        /// pageSize=1024
        /// </summary>
        public AppendingDeltaPackedLongBuffer(float acceptableOverheadRatio)
            : this(16, 1024, acceptableOverheadRatio)
        {
        }

        internal override long Get(int block, int element)
        {
            if (block == ValuesOff)
            {
                return Pending[element];
            }
            else if (Values[block] == null)
            {
                return MinValues[block];
            }
            else
            {
                return MinValues[block] + Values[block].Get(element);
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
                /* packed block */
                int read = Values[block].Get(element, arr, off, len);
                long d = MinValues[block];
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
            long minValue = Pending[0];
            long maxValue = Pending[0];
            for (int i = 1; i < PendingOff; ++i)
            {
                minValue = Math.Min(minValue, Pending[i]);
                maxValue = Math.Max(maxValue, Pending[i]);
            }
            long delta = maxValue - minValue;

            MinValues[ValuesOff] = minValue;
            if (delta == 0)
            {
                Values[ValuesOff] = new PackedInts.NullReader(PendingOff);
            }
            else
            {
                // build a new packed reader
                int bitsRequired = delta < 0 ? 64 : PackedInts.BitsRequired(delta);
                for (int i = 0; i < PendingOff; ++i)
                {
                    Pending[i] -= minValue;
                }
                PackedInts.Mutable mutable = PackedInts.GetMutable(PendingOff, bitsRequired, AcceptableOverheadRatio);
                for (int i = 0; i < PendingOff; )
                {
                    i += mutable.Set(i, Pending, i, PendingOff - i);
                }
                Values[ValuesOff] = mutable;
            }
        }

        internal override void Grow(int newBlockCount)
        {
            base.Grow(newBlockCount);
            this.MinValues = Arrays.CopyOf(MinValues, newBlockCount);
        }

        internal override long BaseRamBytesUsed()
        {
            return base.BaseRamBytesUsed() + RamUsageEstimator.NUM_BYTES_OBJECT_REF; // additional array
        }

        public override long RamBytesUsed()
        {
            return base.RamBytesUsed() + RamUsageEstimator.SizeOf(MinValues);
        }

        public override Iterator GetIterator() // LUCENENET TODO: This can be done from the base class
        {
            return new Iterator(this);
        }
    }
}