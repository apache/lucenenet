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
    /// Packs integers into 3 shorts (48 bits per value).
    /// @lucene.internal
    /// </summary>
    internal sealed class Packed16ThreeBlocks : PackedInts.MutableImpl
    {
        internal readonly short[] Blocks;

        public static readonly int MAX_SIZE = int.MaxValue / 3;

        internal Packed16ThreeBlocks(int valueCount)
            : base(valueCount, 48)
        {
            if (valueCount > MAX_SIZE)
            {
                throw new System.IndexOutOfRangeException("MAX_SIZE exceeded");
            }
            Blocks = new short[valueCount * 3];
        }

        internal Packed16ThreeBlocks(int packedIntsVersion, DataInput @in, int valueCount)
            : this(valueCount)
        {
            for (int i = 0; i < 3 * valueCount; ++i)
            {
                Blocks[i] = @in.ReadShort();
            }
            // because packed ints have not always been byte-aligned
            int remaining = (int)(PackedInts.Format.PACKED.ByteCount(packedIntsVersion, valueCount, 48) - 3L * valueCount * 2);
            for (int i = 0; i < remaining; ++i)
            {
                @in.ReadByte();
            }
        }

        public override long Get(int index)
        {
            int o = index * 3;
            return (Blocks[o] & 0xFFFFL) << 32 | (Blocks[o + 1] & 0xFFFFL) << 16 | (Blocks[o + 2] & 0xFFFFL);
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
            Debug.Assert(index >= 0 && index < m_valueCount);
            Debug.Assert(off + len <= arr.Length);

            int gets = Math.Min(m_valueCount - index, len);
            for (int i = index * 3, end = (index + gets) * 3; i < end; i += 3)
            {
                arr[off++] = (Blocks[i] & 0xFFFFL) << 32 | (Blocks[i + 1] & 0xFFFFL) << 16 | (Blocks[i + 2] & 0xFFFFL);
            }
            return gets;
        }

        public override void Set(int index, long value)
        {
            int o = index * 3;
            Blocks[o] = (short)((long)((ulong)value >> 32));
            Blocks[o + 1] = (short)((long)((ulong)value >> 16));
            Blocks[o + 2] = (short)value;
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
            Debug.Assert(index >= 0 && index < m_valueCount);
            Debug.Assert(off + len <= arr.Length);

            int sets = Math.Min(m_valueCount - index, len);
            for (int i = off, o = index * 3, end = off + sets; i < end; ++i)
            {
                long value = arr[i];
                Blocks[o++] = (short)((long)((ulong)value >> 32));
                Blocks[o++] = (short)((long)((ulong)value >> 16));
                Blocks[o++] = (short)value;
            }
            return sets;
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            short block1 = (short)((long)((ulong)val >> 32));
            short block2 = (short)((long)((ulong)val >> 16));
            short block3 = (short)val;
            for (int i = fromIndex * 3, end = toIndex * 3; i < end; i += 3)
            {
                Blocks[i] = block1;
                Blocks[i + 1] = block2;
                Blocks[i + 2] = block3;
            }
        }

        public override void Clear()
        {
            Arrays.Fill(Blocks, (short)0);
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER 
                + 2 * RamUsageEstimator.NUM_BYTES_INT // valueCount,bitsPerValue
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF) // blocks ref 
                + RamUsageEstimator.SizeOf(Blocks);
        }

        public override string ToString()
        {
            return this.GetType().Name + "(bitsPerValue=" + m_bitsPerValue + ", size=" + Size + ", elements.length=" + Blocks.Length + ")";
        }
    }
}