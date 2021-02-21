using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.IO;
using System.Runtime.CompilerServices;

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
    /// Space optimized random access capable array of values with a fixed number of
    /// bits/value. Values are packed contiguously.
    /// <para/>
    /// The implementation strives to perform af fast as possible under the
    /// constraint of contiguous bits, by avoiding expensive operations. This comes
    /// at the cost of code clarity.
    /// <para/>
    /// Technical details: this implementation is a refinement of a non-branching
    /// version. The non-branching get and set methods meant that 2 or 4 atomics in
    /// the underlying array were always accessed, even for the cases where only
    /// 1 or 2 were needed. Even with caching, this had a detrimental effect on
    /// performance.
    /// Related to this issue, the old implementation used lookup tables for shifts
    /// and masks, which also proved to be a bit slower than calculating the shifts
    /// and masks on the fly.
    /// See https://issues.apache.org/jira/browse/LUCENE-4062 for details.
    /// </summary>
    public class Packed64 : PackedInt32s.MutableImpl
    {
        internal const int BLOCK_SIZE = 64; // 32 = int, 64 = long
        internal const int BLOCK_BITS = 6; // The #bits representing BLOCK_SIZE
        internal const int MOD_MASK = BLOCK_SIZE - 1; // x % BLOCK_SIZE

        /// <summary>
        /// Values are stores contiguously in the blocks array.
        /// </summary>
        private readonly long[] blocks;

        /// <summary>
        /// A right-aligned mask of width BitsPerValue used by <see cref="Get(int)"/>.
        /// </summary>
        private readonly long maskRight;

        /// <summary>
        /// Optimization: Saves one lookup in <see cref="Get(int)"/>.
        /// </summary>
        private readonly int bpvMinusBlockSize;

        /// <summary>
        /// Creates an array with the internal structures adjusted for the given
        /// limits and initialized to 0. </summary>
        /// <param name="valueCount">   The number of elements. </param>
        /// <param name="bitsPerValue"> The number of bits available for any given value. </param>
        public Packed64(int valueCount, int bitsPerValue)
            : base(valueCount, bitsPerValue)
        {
            PackedInt32s.Format format = PackedInt32s.Format.PACKED;
            int longCount = format.Int64Count(PackedInt32s.VERSION_CURRENT, valueCount, bitsPerValue);
            this.blocks = new long[longCount];
            maskRight = (~0L << (BLOCK_SIZE - bitsPerValue)).TripleShift(BLOCK_SIZE - bitsPerValue);
            bpvMinusBlockSize = bitsPerValue - BLOCK_SIZE;
        }

        /// <summary>
        /// Creates an array with content retrieved from the given <see cref="DataInput"/>. </summary>
        /// <param name="in">       A <see cref="DataInput"/>, positioned at the start of Packed64-content. </param>
        /// <param name="valueCount">  The number of elements. </param>
        /// <param name="bitsPerValue"> The number of bits available for any given value. </param>
        /// <exception cref="IOException"> If the values for the backing array could not
        ///                             be retrieved. </exception>
        public Packed64(int packedIntsVersion, DataInput @in, int valueCount, int bitsPerValue)
            : base(valueCount, bitsPerValue)
        {
            PackedInt32s.Format format = PackedInt32s.Format.PACKED;
            long byteCount = format.ByteCount(packedIntsVersion, valueCount, bitsPerValue); // to know how much to read
            int longCount = format.Int64Count(PackedInt32s.VERSION_CURRENT, valueCount, bitsPerValue); // to size the array
            blocks = new long[longCount];
            // read as many longs as we can
            for (int i = 0; i < byteCount / 8; ++i)
            {
                blocks[i] = @in.ReadInt64();
            }
            int remaining = (int)(byteCount % 8);
            if (remaining != 0)
            {
                // read the last bytes
                long lastLong = 0;
                for (int i = 0; i < remaining; ++i)
                {
                    lastLong |= (@in.ReadByte() & 0xFFL) << (56 - i * 8);
                }
                blocks[blocks.Length - 1] = lastLong;
            }
            maskRight = (~0L << (BLOCK_SIZE - bitsPerValue)).TripleShift(BLOCK_SIZE - bitsPerValue);
            bpvMinusBlockSize = bitsPerValue - BLOCK_SIZE;
        }

        /// <param name="index"> The position of the value. </param>
        /// <returns> The value at the given index. </returns>
        public override long Get(int index)
        {
            // The abstract index in a bit stream
            long majorBitPos = (long)index * m_bitsPerValue;
            // The index in the backing long-array
            int elementPos = (int)majorBitPos.TripleShift(BLOCK_BITS);
            // The number of value-bits in the second long
            long endBits = (majorBitPos & MOD_MASK) + bpvMinusBlockSize;

            if (endBits <= 0) // Single block
            {
                return (blocks[elementPos].TripleShift((int)-endBits)) & maskRight;
            }
            // Two blocks
            return ((blocks[elementPos] << (int)endBits) | (blocks[elementPos + 1].TripleShift((int)(BLOCK_SIZE - endBits)))) & maskRight;
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < m_valueCount);
            len = Math.Min(len, m_valueCount - index);
            if (Debugging.AssertsEnabled) Debugging.Assert(off + len <= arr.Length);

            int originalIndex = index;
            PackedInt32s.IDecoder decoder = BulkOperation.Of(PackedInt32s.Format.PACKED, m_bitsPerValue);

            // go to the next block where the value does not span across two blocks
            int offsetInBlocks = index % decoder.Int64ValueCount;
            if (offsetInBlocks != 0)
            {
                for (int i = offsetInBlocks; i < decoder.Int64ValueCount && len > 0; ++i)
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
            if (Debugging.AssertsEnabled) Debugging.Assert(index % decoder.Int64ValueCount == 0);
            int blockIndex = (int)(((long)index * m_bitsPerValue).TripleShift(BLOCK_BITS));
            if (Debugging.AssertsEnabled) Debugging.Assert((((long)index * m_bitsPerValue) & MOD_MASK) == 0);
            int iterations = len / decoder.Int64ValueCount;
            decoder.Decode(blocks, blockIndex, arr, off, iterations);
            int gotValues = iterations * decoder.Int64ValueCount;
            index += gotValues;
            len -= gotValues;
            if (Debugging.AssertsEnabled) Debugging.Assert(len >= 0);

            if (index > originalIndex)
            {
                // stay at the block boundary
                return index - originalIndex;
            }
            else
            {
                // no progress so far => already at a block boundary but no full block to get
                if (Debugging.AssertsEnabled) Debugging.Assert(index == originalIndex);
                return base.Get(index, arr, off, len);
            }
        }

        public override void Set(int index, long value)
        {
            // The abstract index in a contiguous bit stream
            long majorBitPos = (long)index * m_bitsPerValue;
            // The index in the backing long-array
            int elementPos = (int)(majorBitPos.TripleShift(BLOCK_BITS)); // / BLOCK_SIZE
            // The number of value-bits in the second long
            long endBits = (majorBitPos & MOD_MASK) + bpvMinusBlockSize;

            if (endBits <= 0) // Single block
            {
                blocks[elementPos] = blocks[elementPos] & ~(maskRight << (int)-endBits) | (value << (int)-endBits);
                return;
            }
            // Two blocks
            blocks[elementPos] = blocks[elementPos] & ~(maskRight.TripleShift((int)endBits))
                | (value.TripleShift((int)endBits));
            blocks[elementPos + 1] = blocks[elementPos + 1] & (~0L).TripleShift((int)endBits)
                | (value << (int)(BLOCK_SIZE - endBits));
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < m_valueCount);
            len = Math.Min(len, m_valueCount - index);
            if (Debugging.AssertsEnabled) Debugging.Assert(off + len <= arr.Length);

            int originalIndex = index;
            PackedInt32s.IEncoder encoder = BulkOperation.Of(PackedInt32s.Format.PACKED, m_bitsPerValue);

            // go to the next block where the value does not span across two blocks
            int offsetInBlocks = index % encoder.Int64ValueCount;
            if (offsetInBlocks != 0)
            {
                for (int i = offsetInBlocks; i < encoder.Int64ValueCount && len > 0; ++i)
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
            if (Debugging.AssertsEnabled) Debugging.Assert(index % encoder.Int64ValueCount == 0);
            int blockIndex = (int)(((long)index * m_bitsPerValue).TripleShift(BLOCK_BITS));
            if (Debugging.AssertsEnabled) Debugging.Assert((((long)index * m_bitsPerValue) & MOD_MASK) == 0);
            int iterations = len / encoder.Int64ValueCount;
            encoder.Encode(arr, off, blocks, blockIndex, iterations);
            int setValues = iterations * encoder.Int64ValueCount;
            index += setValues;
            len -= setValues;
            if (Debugging.AssertsEnabled) Debugging.Assert(len >= 0);

            if (index > originalIndex)
            {
                // stay at the block boundary
                return index - originalIndex;
            }
            else
            {
                // no progress so far => already at a block boundary but no full block to get
                if (Debugging.AssertsEnabled) Debugging.Assert(index == originalIndex);
                return base.Set(index, arr, off, len);
            }
        }

        public override string ToString()
        {
            return "Packed64(bitsPerValue=" + m_bitsPerValue + ", size=" + Count + ", elements.length=" + blocks.Length + ")";
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER 
                + 3 * RamUsageEstimator.NUM_BYTES_INT32 // bpvMinusBlockSize,valueCount,bitsPerValue
                + RamUsageEstimator.NUM_BYTES_INT64 // maskRight
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF) // blocks ref
                + RamUsageEstimator.SizeOf(blocks);
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(PackedInt32s.BitsRequired(val) <= BitsPerValue);
                Debugging.Assert(fromIndex <= toIndex);
            }

            // minimum number of values that use an exact number of full blocks
            int nAlignedValues = 64 / Gcd(64, m_bitsPerValue);
            int span = toIndex - fromIndex;
            if (span <= 3 * nAlignedValues)
            {
                // there needs be at least 2 * nAlignedValues aligned values for the
                // block approach to be worth trying
                base.Fill(fromIndex, toIndex, val);
                return;
            }

            // fill the first values naively until the next block start
            int fromIndexModNAlignedValues = fromIndex % nAlignedValues;
            if (fromIndexModNAlignedValues != 0)
            {
                for (int i = fromIndexModNAlignedValues; i < nAlignedValues; ++i)
                {
                    Set(fromIndex++, val);
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(fromIndex % nAlignedValues == 0);

            // compute the long[] blocks for nAlignedValues consecutive values and
            // use them to set as many values as possible without applying any mask
            // or shift
            int nAlignedBlocks = (nAlignedValues * m_bitsPerValue) >> 6;
            long[] nAlignedValuesBlocks;
            {
                Packed64 values = new Packed64(nAlignedValues, m_bitsPerValue);
                for (int i = 0; i < nAlignedValues; ++i)
                {
                    values.Set(i, val);
                }
                nAlignedValuesBlocks = values.blocks;
                if (Debugging.AssertsEnabled) Debugging.Assert(nAlignedBlocks <= nAlignedValuesBlocks.Length);
            }
            int startBlock = (int)(((long)fromIndex * m_bitsPerValue).TripleShift(6));
            int endBlock = (int)(((long)toIndex * m_bitsPerValue).TripleShift(6));
            for (int block = startBlock; block < endBlock; ++block)
            {
                long blockValue = nAlignedValuesBlocks[block % nAlignedBlocks];
                blocks[block] = blockValue;
            }

            // fill the gap
            for (int i = (int)(((long)endBlock << 6) / m_bitsPerValue); i < toIndex; ++i)
            {
                Set(i, val);
            }
        }

        private static int Gcd(int a, int b)
        {
            if (a < b)
            {
                return Gcd(b, a);
            }
            else if (b == 0)
            {
                return a;
            }
            else
            {
                return Gcd(b, a % b);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Clear()
        {
            Arrays.Fill(blocks, 0L);
        }
    }
}