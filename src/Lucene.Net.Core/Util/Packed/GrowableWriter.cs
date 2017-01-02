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
        private long CurrentMask;
        private PackedInts.Mutable Current;
        private readonly float AcceptableOverheadRatio;

        /// <param name="startBitsPerValue">       the initial number of bits per value, may grow depending on the data </param>
        /// <param name="valueCount">              the number of values </param>
        /// <param name="acceptableOverheadRatio"> an acceptable overhead ratio </param>
        public GrowableWriter(int startBitsPerValue, int valueCount, float acceptableOverheadRatio)
        {
            this.AcceptableOverheadRatio = acceptableOverheadRatio;
            Current = PackedInts.GetMutable(valueCount, startBitsPerValue, this.AcceptableOverheadRatio);
            CurrentMask = Mask(Current.BitsPerValue);
        }

        private static long Mask(int bitsPerValue)
        {
            return bitsPerValue == 64 ? ~0L : PackedInts.MaxValue(bitsPerValue);
        }

        public override long Get(int index)
        {
            return Current.Get(index);
        }

        public override int Size
        {
            get { return Current.Size; }
        }

        public override int BitsPerValue
        {
            get
            {
                return Current.BitsPerValue;
            }
        }

        public virtual PackedInts.Mutable Mutable
        {
            get
            {
                return Current;
            }
        }

        public override object GetArray()
        {
            return Current.GetArray();
        }

        public override bool HasArray
        {
            get { return Current.HasArray; }
        }

        private void EnsureCapacity(long value)
        {
            if ((value & CurrentMask) == value)
            {
                return;
            }
            int bitsRequired = value < 0 ? 64 : PackedInts.BitsRequired(value);
            Debug.Assert(bitsRequired > Current.BitsPerValue);
            int valueCount = Size;
            PackedInts.Mutable next = PackedInts.GetMutable(valueCount, bitsRequired, AcceptableOverheadRatio);
            PackedInts.Copy(Current, 0, next, 0, valueCount, PackedInts.DEFAULT_BUFFER_SIZE);
            Current = next;
            CurrentMask = Mask(Current.BitsPerValue);
        }

        public override void Set(int index, long value)
        {
            EnsureCapacity(value);
            Current.Set(index, value);
        }

        public override void Clear()
        {
            Current.Clear();
        }

        public virtual GrowableWriter Resize(int newSize)
        {
            GrowableWriter next = new GrowableWriter(BitsPerValue, newSize, AcceptableOverheadRatio);
            int limit = Math.Min(Size, newSize);
            PackedInts.Copy(Current, 0, next, 0, limit, PackedInts.DEFAULT_BUFFER_SIZE);
            return next;
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            return Current.Get(index, arr, off, len);
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
            return Current.Set(index, arr, off, len);
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            EnsureCapacity(val);
            Current.Fill(fromIndex, toIndex, val);
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER 
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF 
                + RamUsageEstimator.NUM_BYTES_LONG 
                + RamUsageEstimator.NUM_BYTES_FLOAT) 
                + Current.RamBytesUsed();
        }

        public override void Save(DataOutput @out)
        {
            Current.Save(@out);
        }
    }
}