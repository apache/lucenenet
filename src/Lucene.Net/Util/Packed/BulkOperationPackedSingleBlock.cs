using J2N.Numerics;
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
    /// Non-specialized <see cref="BulkOperation"/> for <see cref="PackedInt32s.Format.PACKED_SINGLE_BLOCK"/>.
    /// </summary>
    internal sealed class BulkOperationPackedSingleBlock : BulkOperation
    {
        private const int BLOCK_COUNT = 1;

        private readonly int bitsPerValue;
        private readonly int valueCount;
        private readonly long mask;

        public BulkOperationPackedSingleBlock(int bitsPerValue)
        {
            this.bitsPerValue = bitsPerValue;
            this.valueCount = 64 / bitsPerValue;
            this.mask = (1L << bitsPerValue) - 1;
        }

        /// <summary>
        /// NOTE: This was longBlockCount() in Lucene.
        /// </summary>
        public override sealed int Int64BlockCount => BLOCK_COUNT;

        public override sealed int ByteBlockCount => BLOCK_COUNT * 8;

        /// <summary>
        /// NOTE: This was longValueCount() in Lucene.
        /// </summary>
        public override int Int64ValueCount => valueCount;

        public override sealed int ByteValueCount => valueCount;

        /// <summary>
        /// NOTE: This was readLong() in Lucene.
        /// </summary>
        private static long ReadInt64(byte[] blocks, int blocksOffset)
        {
            return (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 56 | (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 48 | 
                (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 40 | (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 32 |
                (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 24 | (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 16 |
                (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 8 | ((sbyte)blocks[blocksOffset++]) & 0xFFL;
        }

        private int Decode(long block, long[] values, int valuesOffset)
        {
            values[valuesOffset++] = block & mask;
            for (int j = 1; j < valueCount; ++j)
            {
                block = block.TripleShift(bitsPerValue);
                values[valuesOffset++] = block & mask;
            }
            return valuesOffset;
        }

        private int Decode(long block, int[] values, int valuesOffset)
        {
            values[valuesOffset++] = (int)(block & mask);
            for (int j = 1; j < valueCount; ++j)
            {
                block = block.TripleShift(bitsPerValue);
                values[valuesOffset++] = (int)(block & mask);
            }
            return valuesOffset;
        }

        private long Encode(long[] values, int valuesOffset)
        {
            long block = values[valuesOffset++];
            for (int j = 1; j < valueCount; ++j)
            {
                block |= values[valuesOffset++] << (j * bitsPerValue);
            }
            return block;
        }

        private long Encode(int[] values, int valuesOffset)
        {
            long block = values[valuesOffset++] & 0xFFFFFFFFL;
            for (int j = 1; j < valueCount; ++j)
            {
                block |= (values[valuesOffset++] & 0xFFFFFFFFL) << (j * bitsPerValue);
            }
            return block;
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                valuesOffset = Decode(block, values, valuesOffset);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = ReadInt64(blocks, blocksOffset);
                blocksOffset += 8;
                valuesOffset = Decode(block, values, valuesOffset);
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            if (bitsPerValue > 32)
            {
                throw UnsupportedOperationException.Create("Cannot decode " + bitsPerValue + "-bits values into an int[]");
            }
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                valuesOffset = Decode(block, values, valuesOffset);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            if (bitsPerValue > 32)
            {
                throw UnsupportedOperationException.Create("Cannot decode " + bitsPerValue + "-bits values into an int[]");
            }
            for (int i = 0; i < iterations; ++i)
            {
                long block = ReadInt64(blocks, blocksOffset);
                blocksOffset += 8;
                valuesOffset = Decode(block, values, valuesOffset);
            }
        }

        public override void Encode(long[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                blocks[blocksOffset++] = Encode(values, valuesOffset);
                valuesOffset += valueCount;
            }
        }

        public override void Encode(int[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                blocks[blocksOffset++] = Encode(values, valuesOffset);
                valuesOffset += valueCount;
            }
        }

        public override void Encode(long[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = Encode(values, valuesOffset);
                valuesOffset += valueCount;
                blocksOffset = WriteInt64(block, blocks, blocksOffset);
            }
        }

        public override void Encode(int[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = Encode(values, valuesOffset);
                valuesOffset += valueCount;
                blocksOffset = WriteInt64(block, blocks, blocksOffset);
            }
        }
    }
}