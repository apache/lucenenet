using J2N.Numerics;
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
    /// This class is similar to <see cref="Packed64"/> except that it trades space for
    /// speed by ensuring that a single block needs to be read/written in order to
    /// read/write a value.
    /// </summary>
    internal abstract class Packed64SingleBlock : PackedInt32s.MutableImpl
    {
        public const int MAX_SUPPORTED_BITS_PER_VALUE = 32;
        private static readonly int[] SUPPORTED_BITS_PER_VALUE = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 16, 21, 32 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSupported(int bitsPerValue)
        {
            return Array.BinarySearch(SUPPORTED_BITS_PER_VALUE, bitsPerValue) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RequiredCapacity(int valueCount, int valuesPerBlock)
        {
            return valueCount / valuesPerBlock + (valueCount % valuesPerBlock == 0 ? 0 : 1);
        }

        internal readonly long[] blocks;

        private protected Packed64SingleBlock(int valueCount, int bitsPerValue) // LUCENENET: Changed from internal to private protected
            : base(valueCount, bitsPerValue)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsSupported(bitsPerValue));
            int valuesPerBlock = 64 / bitsPerValue;
            blocks = new long[RequiredCapacity(valueCount, valuesPerBlock)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Clear()
        {
            Arrays.Fill(blocks, 0L);
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER 
                + 2 * RamUsageEstimator.NUM_BYTES_INT32 // valueCount,bitsPerValue
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF) // blocks ref 
                + RamUsageEstimator.SizeOf(blocks);
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
                Debugging.Assert(index >= 0 && index < m_valueCount);
            }
            len = Math.Min(len, m_valueCount - index);
            if (Debugging.AssertsEnabled) Debugging.Assert(off + len <= arr.Length);

            int originalIndex = index;

            // go to the next block boundary
            int valuesPerBlock = 64 / m_bitsPerValue;
            int offsetInBlock = index % valuesPerBlock;
            if (offsetInBlock != 0)
            {
                for (int i = offsetInBlock; i < valuesPerBlock && len > 0; ++i)
                {
                    arr[off++] = Get(index++);
                    --len;
                }
                if (len == 0)
                {
                    return index - originalIndex;
                }
            }

            // bulk get
            if (Debugging.AssertsEnabled) Debugging.Assert(index % valuesPerBlock == 0);
            PackedInt32s.IDecoder decoder = BulkOperation.Of(PackedInt32s.Format.PACKED_SINGLE_BLOCK, m_bitsPerValue);
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(decoder.Int64BlockCount == 1);
                Debugging.Assert(decoder.Int64ValueCount == valuesPerBlock);
            }
            int blockIndex = index / valuesPerBlock;
            int nblocks = (index + len) / valuesPerBlock - blockIndex;
            decoder.Decode(blocks, blockIndex, arr, off, nblocks);
            int diff = nblocks * valuesPerBlock;
            index += diff;
            len -= diff;

            if (index > originalIndex)
            {
                // stay at the block boundary
                return index - originalIndex;
            }
            else
            {
                // no progress so far => already at a block boundary but no full block to
                // get
                if (Debugging.AssertsEnabled) Debugging.Assert(index == originalIndex);
                return base.Get(index, arr, off, len);
            }
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
                Debugging.Assert(index >= 0 && index < m_valueCount);
            }
            len = Math.Min(len, m_valueCount - index);
            if (Debugging.AssertsEnabled) Debugging.Assert(off + len <= arr.Length);

            int originalIndex = index;

            // go to the next block boundary
            int valuesPerBlock = 64 / m_bitsPerValue;
            int offsetInBlock = index % valuesPerBlock;
            if (offsetInBlock != 0)
            {
                for (int i = offsetInBlock; i < valuesPerBlock && len > 0; ++i)
                {
                    Set(index++, arr[off++]);
                    --len;
                }
                if (len == 0)
                {
                    return index - originalIndex;
                }
            }

            // bulk set
            if (Debugging.AssertsEnabled) Debugging.Assert(index % valuesPerBlock == 0);
            BulkOperation op = BulkOperation.Of(PackedInt32s.Format.PACKED_SINGLE_BLOCK, m_bitsPerValue);
            if (Debugging.AssertsEnabled) Debugging.Assert(op.Int64BlockCount == 1);
            if (Debugging.AssertsEnabled) Debugging.Assert(op.Int64ValueCount == valuesPerBlock);
            int blockIndex = index / valuesPerBlock;
            int nblocks = (index + len) / valuesPerBlock - blockIndex;
            op.Encode(arr, off, blocks, blockIndex, nblocks);
            int diff = nblocks * valuesPerBlock;
            index += diff;
            len -= diff;

            if (index > originalIndex)
            {
                // stay at the block boundary
                return index - originalIndex;
            }
            else
            {
                // no progress so far => already at a block boundary but no full block to
                // set
                if (Debugging.AssertsEnabled) Debugging.Assert(index == originalIndex);
                return base.Set(index, arr, off, len);
            }
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(fromIndex >= 0);
                Debugging.Assert(fromIndex <= toIndex);
                Debugging.Assert(PackedInt32s.BitsRequired(val) <= m_bitsPerValue);
            }

            int valuesPerBlock = 64 / m_bitsPerValue;
            if (toIndex - fromIndex <= valuesPerBlock << 1)
            {
                // there needs to be at least one full block to set for the block
                // approach to be worth trying
                base.Fill(fromIndex, toIndex, val);
                return;
            }

            // set values naively until the next block start
            int fromOffsetInBlock = fromIndex % valuesPerBlock;
            if (fromOffsetInBlock != 0)
            {
                for (int i = fromOffsetInBlock; i < valuesPerBlock; ++i)
                {
                    Set(fromIndex++, val);
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(fromIndex % valuesPerBlock == 0);
            }

            // bulk set of the inner blocks
            int fromBlock = fromIndex / valuesPerBlock;
            int toBlock = toIndex / valuesPerBlock;
            if (Debugging.AssertsEnabled) Debugging.Assert(fromBlock * valuesPerBlock == fromIndex);

            long blockValue = 0L;
            for (int i = 0; i < valuesPerBlock; ++i)
            {
                blockValue |= (val << (i * m_bitsPerValue));
            }
            Arrays.Fill(blocks, fromBlock, toBlock, blockValue);

            // fill the gap
            for (int i = valuesPerBlock * toBlock; i < toIndex; ++i)
            {
                Set(i, val);
            }
        }

        internal override PackedInt32s.Format Format => PackedInt32s.Format.PACKED_SINGLE_BLOCK;

        public override string ToString()
        {
            return this.GetType().Name + "(bitsPerValue=" + m_bitsPerValue + ", size=" + Count + ", elements.length=" + blocks.Length + ")";
        }

        public static Packed64SingleBlock Create(DataInput @in, int valueCount, int bitsPerValue)
        {
            Packed64SingleBlock reader = Create(valueCount, bitsPerValue);
            for (int i = 0; i < reader.blocks.Length; ++i)
            {
                reader.blocks[i] = @in.ReadInt64();
            }
            return reader;
        }

        public static Packed64SingleBlock Create(int valueCount, int bitsPerValue)
        {
            switch (bitsPerValue)
            {
                case 1:
                    return new Packed64SingleBlock1(valueCount);

                case 2:
                    return new Packed64SingleBlock2(valueCount);

                case 3:
                    return new Packed64SingleBlock3(valueCount);

                case 4:
                    return new Packed64SingleBlock4(valueCount);

                case 5:
                    return new Packed64SingleBlock5(valueCount);

                case 6:
                    return new Packed64SingleBlock6(valueCount);

                case 7:
                    return new Packed64SingleBlock7(valueCount);

                case 8:
                    return new Packed64SingleBlock8(valueCount);

                case 9:
                    return new Packed64SingleBlock9(valueCount);

                case 10:
                    return new Packed64SingleBlock10(valueCount);

                case 12:
                    return new Packed64SingleBlock12(valueCount);

                case 16:
                    return new Packed64SingleBlock16(valueCount);

                case 21:
                    return new Packed64SingleBlock21(valueCount);

                case 32:
                    return new Packed64SingleBlock32(valueCount);

                default:
                    throw new ArgumentException("Unsupported number of bits per value: " + 32);
            }
        }

        internal class Packed64SingleBlock1 : Packed64SingleBlock
        {
            internal Packed64SingleBlock1(int valueCount)
                : base(valueCount, 1)
            {
            }

            public override long Get(int index)
            {
                int o = index.TripleShift(6);
                int b = index & 63;
                int shift = b << 0;
                return (blocks[o].TripleShift(shift)) & 1L;
            }

            public override void Set(int index, long value)
            {
                int o = index.TripleShift(6);
                int b = index & 63;
                int shift = b << 0;
                blocks[o] = (blocks[o] & ~(1L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock2 : Packed64SingleBlock
        {
            internal Packed64SingleBlock2(int valueCount)
                : base(valueCount, 2)
            {
            }

            public override long Get(int index)
            {
                int o = index.TripleShift(5);
                int b = index & 31;
                int shift = b << 1;
                return (blocks[o].TripleShift(shift)) & 3L;
            }

            public override void Set(int index, long value)
            {
                int o = index.TripleShift(5);
                int b = index & 31;
                int shift = b << 1;
                blocks[o] = (blocks[o] & ~(3L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock3 : Packed64SingleBlock
        {
            internal Packed64SingleBlock3(int valueCount)
                : base(valueCount, 3)
            {
            }

            public override long Get(int index)
            {
                int o = index / 21;
                int b = index % 21;
                int shift = b * 3;
                return (blocks[o].TripleShift(shift)) & 7L;
            }

            public override void Set(int index, long value)
            {
                int o = index / 21;
                int b = index % 21;
                int shift = b * 3;
                blocks[o] = (blocks[o] & ~(7L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock4 : Packed64SingleBlock
        {
            internal Packed64SingleBlock4(int valueCount)
                : base(valueCount, 4)
            {
            }

            public override long Get(int index)
            {
                int o = index.TripleShift(4);
                int b = index & 15;
                int shift = b << 2;
                return (blocks[o].TripleShift(shift)) & 15L;
            }

            public override void Set(int index, long value)
            {
                int o = index.TripleShift(4);
                int b = index & 15;
                int shift = b << 2;
                blocks[o] = (blocks[o] & ~(15L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock5 : Packed64SingleBlock
        {
            internal Packed64SingleBlock5(int valueCount)
                : base(valueCount, 5)
            {
            }

            public override long Get(int index)
            {
                int o = index / 12;
                int b = index % 12;
                int shift = b * 5;
                return (blocks[o].TripleShift(shift)) & 31L;
            }

            public override void Set(int index, long value)
            {
                int o = index / 12;
                int b = index % 12;
                int shift = b * 5;
                blocks[o] = (blocks[o] & ~(31L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock6 : Packed64SingleBlock
        {
            internal Packed64SingleBlock6(int valueCount)
                : base(valueCount, 6)
            {
            }

            public override long Get(int index)
            {
                int o = index / 10;
                int b = index % 10;
                int shift = b * 6;
                return (blocks[o].TripleShift(shift)) & 63L;
            }

            public override void Set(int index, long value)
            {
                int o = index / 10;
                int b = index % 10;
                int shift = b * 6;
                blocks[o] = (blocks[o] & ~(63L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock7 : Packed64SingleBlock
        {
            internal Packed64SingleBlock7(int valueCount)
                : base(valueCount, 7)
            {
            }

            public override long Get(int index)
            {
                int o = index / 9;
                int b = index % 9;
                int shift = b * 7;
                return (blocks[o].TripleShift(shift)) & 127L;
            }

            public override void Set(int index, long value)
            {
                int o = index / 9;
                int b = index % 9;
                int shift = b * 7;
                blocks[o] = (blocks[o] & ~(127L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock8 : Packed64SingleBlock
        {
            internal Packed64SingleBlock8(int valueCount)
                : base(valueCount, 8)
            {
            }

            public override long Get(int index)
            {
                int o = index.TripleShift(3);
                int b = index & 7;
                int shift = b << 3;
                return (blocks[o].TripleShift(shift)) & 255L;
            }

            public override void Set(int index, long value)
            {
                int o = index.TripleShift(3);
                int b = index & 7;
                int shift = b << 3;
                blocks[o] = (blocks[o] & ~(255L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock9 : Packed64SingleBlock
        {
            internal Packed64SingleBlock9(int valueCount)
                : base(valueCount, 9)
            {
            }

            public override long Get(int index)
            {
                int o = index / 7;
                int b = index % 7;
                int shift = b * 9;
                return (blocks[o].TripleShift(shift)) & 511L;
            }

            public override void Set(int index, long value)
            {
                int o = index / 7;
                int b = index % 7;
                int shift = b * 9;
                blocks[o] = (blocks[o] & ~(511L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock10 : Packed64SingleBlock
        {
            internal Packed64SingleBlock10(int valueCount)
                : base(valueCount, 10)
            {
            }

            public override long Get(int index)
            {
                int o = index / 6;
                int b = index % 6;
                int shift = b * 10;
                return (blocks[o].TripleShift(shift)) & 1023L;
            }

            public override void Set(int index, long value)
            {
                int o = index / 6;
                int b = index % 6;
                int shift = b * 10;
                blocks[o] = (blocks[o] & ~(1023L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock12 : Packed64SingleBlock
        {
            internal Packed64SingleBlock12(int valueCount)
                : base(valueCount, 12)
            {
            }

            public override long Get(int index)
            {
                int o = index / 5;
                int b = index % 5;
                int shift = b * 12;
                return (blocks[o].TripleShift(shift)) & 4095L;
            }

            public override void Set(int index, long value)
            {
                int o = index / 5;
                int b = index % 5;
                int shift = b * 12;
                blocks[o] = (blocks[o] & ~(4095L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock16 : Packed64SingleBlock
        {
            internal Packed64SingleBlock16(int valueCount)
                : base(valueCount, 16)
            {
            }

            public override long Get(int index)
            {
                int o = index.TripleShift(2);
                int b = index & 3;
                int shift = b << 4;
                return (blocks[o].TripleShift(shift)) & 65535L;
            }

            public override void Set(int index, long value)
            {
                int o = index.TripleShift(2);
                int b = index & 3;
                int shift = b << 4;
                blocks[o] = (blocks[o] & ~(65535L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock21 : Packed64SingleBlock
        {
            internal Packed64SingleBlock21(int valueCount)
                : base(valueCount, 21)
            {
            }

            public override long Get(int index)
            {
                int o = index / 3;
                int b = index % 3;
                int shift = b * 21;
                return (blocks[o].TripleShift(shift)) & 2097151L;
            }

            public override void Set(int index, long value)
            {
                int o = index / 3;
                int b = index % 3;
                int shift = b * 21;
                blocks[o] = (blocks[o] & ~(2097151L << shift)) | (value << shift);
            }
        }

        internal class Packed64SingleBlock32 : Packed64SingleBlock
        {
            internal Packed64SingleBlock32(int valueCount)
                : base(valueCount, 32)
            {
            }

            public override long Get(int index)
            {
                int o = index.TripleShift(1);
                int b = index & 1;
                int shift = b << 5;
                return (blocks[o].TripleShift(shift)) & 4294967295L;
            }

            public override void Set(int index, long value)
            {
                int o = index.TripleShift(1);
                int b = index & 1;
                int shift = b << 5;
                blocks[o] = (blocks[o] & ~(4294967295L << shift)) | (value << shift);
            }
        }
    }
}