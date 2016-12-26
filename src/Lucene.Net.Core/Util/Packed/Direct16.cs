using Lucene.Net.Support;
using System;
using System.Diagnostics;

// this file has been automatically generated, DO NOT EDIT

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

    using DataInput = Lucene.Net.Store.DataInput;

    /// <summary>
    /// Direct wrapping of 16-bits values to a backing array.
    /// @lucene.internal
    /// </summary>
    internal sealed class Direct16 : PackedInts.MutableImpl
    {
        internal readonly short[] Values;

        internal Direct16(int valueCount)
            : base(valueCount, 16)
        {
            Values = new short[valueCount];
        }

        internal Direct16(int packedIntsVersion, DataInput @in, int valueCount)
            : this(valueCount)
        {
            for (int i = 0; i < valueCount; ++i)
            {
                Values[i] = @in.ReadShort();
            }
            // because packed ints have not always been byte-aligned
            int remaining = (int)(PackedInts.Format.PACKED.ByteCount(packedIntsVersion, valueCount, 16) - 2L * valueCount);
            for (int i = 0; i < remaining; ++i)
            {
                @in.ReadByte();
            }
        }

        public override long Get(int index)
        {
            return Values[index] & 0xFFFFL;
        }

        public override void Set(int index, long value)
        {
            Values[index] = (short)(value);
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER 
                + 2 * RamUsageEstimator.NUM_BYTES_INT // valueCount,bitsPerValue
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF) // values ref
                + RamUsageEstimator.SizeOf(Values);
        }

        public override void Clear()
        {
            Arrays.Fill(Values, (short)0L);
        }

        public override object Array
        {
            get
            {
                return Values;
            }
        }

        public override bool HasArray()
        {
            return true;
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
            Debug.Assert(index >= 0 && index < valueCount);
            Debug.Assert(off + len <= arr.Length);

            int gets = Math.Min(valueCount - index, len);
            for (int i = index, o = off, end = index + gets; i < end; ++i, ++o)
            {
                arr[o] = Values[i] & 0xFFFFL;
            }
            return gets;
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
            Debug.Assert(index >= 0 && index < valueCount);
            Debug.Assert(off + len <= arr.Length);

            int sets = Math.Min(valueCount - index, len);
            for (int i = index, o = off, end = index + sets; i < end; ++i, ++o)
            {
                Values[i] = (short)arr[o];
            }
            return sets;
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            Debug.Assert(val == (val & 0xFFFFL));
            Arrays.Fill(Values, fromIndex, toIndex, (short)val);
        }
    }
}