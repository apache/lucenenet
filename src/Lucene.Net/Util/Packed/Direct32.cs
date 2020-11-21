using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;

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
    /// Direct wrapping of 32-bits values to a backing array.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    internal sealed class Direct32 : PackedInt32s.MutableImpl
    {
        internal readonly int[] values;

        internal Direct32(int valueCount)
            : base(valueCount, 32)
        {
            values = new int[valueCount];
        }

        internal Direct32(int packedIntsVersion, DataInput @in, int valueCount)
            : this(valueCount)
        {
            for (int i = 0; i < valueCount; ++i)
            {
                values[i] = @in.ReadInt32();
            }
            // because packed ints have not always been byte-aligned
            int remaining = (int)(PackedInt32s.Format.PACKED.ByteCount(packedIntsVersion, valueCount, 32) - 4L * valueCount);
            for (int i = 0; i < remaining; ++i)
            {
                @in.ReadByte();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long Get(int index)
        {
            return values[index] & 0xFFFFFFFFL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Set(int index, long value)
        {
            values[index] = (int)(value);
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER 
                + 2 * RamUsageEstimator.NUM_BYTES_INT32 // valueCount,bitsPerValue
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF) // values ref  
                + RamUsageEstimator.SizeOf(values);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Clear()
        {
            Arrays.Fill(values, (int)0L);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object GetArray()
        {
            return values;
        }

        public override bool HasArray => true;

        public override int Get(int index, long[] arr, int off, int len)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
                Debugging.Assert(index >= 0 && index < m_valueCount);
                Debugging.Assert(off + len <= arr.Length);
            }

            int gets = Math.Min(m_valueCount - index, len);
            for (int i = index, o = off, end = index + gets; i < end; ++i, ++o)
            {
                arr[o] = values[i] & 0xFFFFFFFFL;
            }
            return gets;
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
                Debugging.Assert(index >= 0 && index < m_valueCount);
                Debugging.Assert(off + len <= arr.Length);
            }

            int sets = Math.Min(m_valueCount - index, len);
            for (int i = index, o = off, end = index + sets; i < end; ++i, ++o)
            {
                values[i] = (int)arr[o];
            }
            return sets;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Fill(int fromIndex, int toIndex, long val)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(val == (val & 0xFFFFFFFFL));
            Arrays.Fill(values, fromIndex, toIndex, (int)val);
        }
    }
}