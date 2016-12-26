using System;
using System.Diagnostics;

// this file has been automatically generated, DO NOT EDIT

namespace Lucene.Net.Util.Packed
{
    using Lucene.Net.Support;

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
    /// Packs integers into 3 bytes (24 bits per value).
    /// @lucene.internal
    /// </summary>
    internal sealed class Packed8ThreeBlocks : PackedInts.MutableImpl
    {
        readonly byte[] Blocks;

        public static readonly int MAX_SIZE = int.MaxValue / 3;

        internal Packed8ThreeBlocks(int valueCount)
            : base(valueCount, 24)
        {
            if (valueCount > MAX_SIZE)
            {
                throw new System.IndexOutOfRangeException("MAX_SIZE exceeded");
            }
            Blocks = new byte[valueCount * 3];
        }

        internal Packed8ThreeBlocks(int packedIntsVersion, DataInput @in, int valueCount)
            : this(valueCount)
        {
            @in.ReadBytes(Blocks, 0, 3 * valueCount);
            // because packed ints have not always been byte-aligned
            var remaining = (int)(PackedInts.Format.PACKED.ByteCount(packedIntsVersion, valueCount, 24) - 3L * valueCount * 1);
            for (int i = 0; i < remaining; ++i)
            {
                @in.ReadByte();
            }
        }

        public override long Get(int index)
        {
            int o = index * 3;
            return (Blocks[o] & 0xFFL) << 16 | (Blocks[o + 1] & 0xFFL) << 8 | (Blocks[o + 2] & 0xFFL);
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
            Debug.Assert(index >= 0 && index < valueCount);
            Debug.Assert(off + len <= arr.Length);

            int gets = Math.Min(valueCount - index, len);
            for (int i = index * 3, end = (index + gets) * 3; i < end; i += 3)
            {
                arr[off++] = (Blocks[i] & 0xFFL) << 16 | (Blocks[i + 1] & 0xFFL) << 8 | (Blocks[i + 2] & 0xFFL);
            }
            return gets;
        }

        public override void Set(int index, long value)
        {
            int o = index * 3;
            Blocks[o] = (byte)((long)((ulong)value >> 16));
            Blocks[o + 1] = (byte)((long)((ulong)value >> 8));
            Blocks[o + 2] = (byte)value;
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
            Debug.Assert(index >= 0 && index < valueCount);
            Debug.Assert(off + len <= arr.Length);

            int sets = Math.Min(valueCount - index, len);
            for (int i = off, o = index * 3, end = off + sets; i < end; ++i)
            {
                long value = arr[i];
                Blocks[o++] = (byte)((long)((ulong)value >> 16));
                Blocks[o++] = (byte)((long)((ulong)value >> 8));
                Blocks[o++] = (byte)value;
            }
            return sets;
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            var block1 = (byte)((long)((ulong)val >> 16));
            var block2 = (byte)((long)((ulong)val >> 8));
            var block3 = (byte)val;
            for (int i = fromIndex * 3, end = toIndex * 3; i < end; i += 3)
            {
                Blocks[i] = block1;
                Blocks[i + 1] = block2;
                Blocks[i + 2] = block3;
            }
        }

        public override void Clear()
        {
            Arrays.Fill(Blocks, (byte)0);
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
            return this.GetType().Name + "(bitsPerValue=" + bitsPerValue + ", size=" + Size() + ", elements.length=" + Blocks.Length + ")";
        }
    }
}