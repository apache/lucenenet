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
    /// Direct wrapping of 64-bits values to a backing array.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    internal sealed class Direct64 : PackedInt32s.MutableImpl
    {
        internal readonly long[] values;

        internal Direct64(int valueCount)
            : base(valueCount, 64)
        {
            values = new long[valueCount];
        }

        internal Direct64(/*int packedIntsVersion,*/ DataInput @in, int valueCount) // LUCENENET specific - removed unused parameter
            : this(valueCount)
        {
            for (int i = 0; i < valueCount; ++i)
            {
                values[i] = @in.ReadInt64();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long Get(int index)
        {
            return values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Set(int index, long value)
        {
            values[index] = (value);
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
            Arrays.Fill(values, 0L);
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
            Arrays.Copy(values, index, arr, off, gets);
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
            Arrays.Copy(arr, off, values, index, sets);
            return sets;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Fill(int fromIndex, int toIndex, long val)
        {
            Arrays.Fill(values, fromIndex, toIndex, val);
        }
    }
}