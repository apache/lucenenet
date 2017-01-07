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

    using DataOutput = Lucene.Net.Store.DataOutput;

    /// <summary>
    /// Implements <seealso cref="PackedInts.Mutable"/>, but grows the
    /// bit count of the underlying packed ints on-demand.
    /// <p>Beware that this class will accept to set negative values but in order
    /// to do this, it will grow the number of bits per value to 64.
    ///
    /// <p>@lucene.internal</p>
    /// </summary>
    public class GrowableWriter : PackedInts.Mutable
    {
        private long currentMask;
        private PackedInts.Mutable current;
        private readonly float acceptableOverheadRatio;

        /// <param name="startBitsPerValue">       the initial number of bits per value, may grow depending on the data </param>
        /// <param name="valueCount">              the number of values </param>
        /// <param name="acceptableOverheadRatio"> an acceptable overhead ratio </param>
        public GrowableWriter(int startBitsPerValue, int valueCount, float acceptableOverheadRatio)
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
            current = PackedInts.GetMutable(valueCount, startBitsPerValue, this.acceptableOverheadRatio);
            currentMask = Mask(current.BitsPerValue);
        }

        private static long Mask(int bitsPerValue)
        {
            return bitsPerValue == 64 ? ~0L : PackedInts.MaxValue(bitsPerValue);
        }

        public override long Get(int index)
        {
            return current.Get(index);
        }

        public override int Count
        {
            get { return current.Count; }
        }

        public override int BitsPerValue
        {
            get
            {
                return current.BitsPerValue;
            }
        }

        public virtual PackedInts.Mutable Mutable
        {
            get
            {
                return current;
            }
        }

        public override object GetArray()
        {
            return current.GetArray();
        }

        public override bool HasArray
        {
            get { return current.HasArray; }
        }

        private void EnsureCapacity(long value)
        {
            if ((value & currentMask) == value)
            {
                return;
            }
            int bitsRequired = value < 0 ? 64 : PackedInts.BitsRequired(value);
            Debug.Assert(bitsRequired > current.BitsPerValue);
            int valueCount = Count;
            PackedInts.Mutable next = PackedInts.GetMutable(valueCount, bitsRequired, acceptableOverheadRatio);
            PackedInts.Copy(current, 0, next, 0, valueCount, PackedInts.DEFAULT_BUFFER_SIZE);
            current = next;
            currentMask = Mask(current.BitsPerValue);
        }

        public override void Set(int index, long value)
        {
            EnsureCapacity(value);
            current.Set(index, value);
        }

        public override void Clear()
        {
            current.Clear();
        }

        public virtual GrowableWriter Resize(int newSize)
        {
            GrowableWriter next = new GrowableWriter(BitsPerValue, newSize, acceptableOverheadRatio);
            int limit = Math.Min(Count, newSize);
            PackedInts.Copy(current, 0, next, 0, limit, PackedInts.DEFAULT_BUFFER_SIZE);
            return next;
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            return current.Get(index, arr, off, len);
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            long max = 0;
            for (int i = off, end = off + len; i < end; ++i)
            {
                // bitwise or is nice because either all values are positive and the
                // or-ed result will require as many bits per value as the max of the
                // values, or one of them is negative and the result will be negative,
                // forcing GrowableWriter to use 64 bits per value
                max |= arr[i];
            }
            EnsureCapacity(max);
            return current.Set(index, arr, off, len);
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            EnsureCapacity(val);
            current.Fill(fromIndex, toIndex, val);
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER 
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF 
                + RamUsageEstimator.NUM_BYTES_LONG 
                + RamUsageEstimator.NUM_BYTES_FLOAT) 
                + current.RamBytesUsed();
        }

        public override void Save(DataOutput @out)
        {
            current.Save(@out);
        }
    }
}