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
    /// Non-specialized <seealso cref="BulkOperation"/> for <seealso cref="PackedInts.Format#PACKED_SINGLE_BLOCK"/>.
    /// </summary>
    internal sealed class BulkOperationPackedSingleBlock : BulkOperation
    {
        private const int BLOCK_COUNT = 1;

        private readonly int BitsPerValue;
        private readonly int ValueCount;
        private readonly long Mask;

        public BulkOperationPackedSingleBlock(int bitsPerValue)
        {
            this.BitsPerValue = bitsPerValue;
            this.ValueCount = 64 / bitsPerValue;
            this.Mask = (1L << bitsPerValue) - 1;
        }

        public override sealed int LongBlockCount()
        {
            return BLOCK_COUNT;
        }

        public override sealed int ByteBlockCount()
        {
            return BLOCK_COUNT * 8;
        }

        public override int LongValueCount()
        {
            return ValueCount;
        }

        public override sealed int ByteValueCount()
        {
            return ValueCount;
        }

        private static long ReadLong(byte[] blocks, int blocksOffset)
        {
            return (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 56 | (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 48 | 
                (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 40 | (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 32 |
                (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 24 | (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 16 |
                (((sbyte)blocks[blocksOffset++]) & 0xFFL) << 8 | ((sbyte)blocks[blocksOffset++]) & 0xFFL;
        }

        private int Decode(long block, long[] values, int valuesOffset)
        {
            values[valuesOffset++] = block & Mask;
            for (int j = 1; j < ValueCount; ++j)
            {
                block = (long)((ulong)block >> BitsPerValue);
                values[valuesOffset++] = block & Mask;
            }
            return valuesOffset;
        }

        private int Decode(long block, int[] values, int valuesOffset)
        {
            values[valuesOffset++] = (int)(block & Mask);
            for (int j = 1; j < ValueCount; ++j)
            {
                block = (long)((ulong)block >> BitsPerValue);
                values[valuesOffset++] = (int)(block & Mask);
            }
            return valuesOffset;
        }

        private long Encode(long[] values, int valuesOffset)
        {
            long block = values[valuesOffset++];
            for (int j = 1; j < ValueCount; ++j)
            {
                block |= values[valuesOffset++] << (j * BitsPerValue);
            }
            return block;
        }

        private long Encode(int[] values, int valuesOffset)
        {
            long block = values[valuesOffset++] & 0xFFFFFFFFL;
            for (int j = 1; j < ValueCount; ++j)
            {
                block |= (values[valuesOffset++] & 0xFFFFFFFFL) << (j * BitsPerValue);
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
                long block = ReadLong(blocks, blocksOffset);
                blocksOffset += 8;
                valuesOffset = Decode(block, values, valuesOffset);
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            if (BitsPerValue > 32)
            {
                throw new System.NotSupportedException("Cannot decode " + BitsPerValue + "-bits values into an int[]");
            }
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                valuesOffset = Decode(block, values, valuesOffset);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            if (BitsPerValue > 32)
            {
                throw new System.NotSupportedException("Cannot decode " + BitsPerValue + "-bits values into an int[]");
            }
            for (int i = 0; i < iterations; ++i)
            {
                long block = ReadLong(blocks, blocksOffset);
                blocksOffset += 8;
                valuesOffset = Decode(block, values, valuesOffset);
            }
        }

        public override void Encode(long[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                blocks[blocksOffset++] = Encode(values, valuesOffset);
                valuesOffset += ValueCount;
            }
        }

        public override void Encode(int[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                blocks[blocksOffset++] = Encode(values, valuesOffset);
                valuesOffset += ValueCount;
            }
        }

        public override void Encode(long[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = Encode(values, valuesOffset);
                valuesOffset += ValueCount;
                blocksOffset = WriteLong(block, blocks, blocksOffset);
            }
        }

        public override void Encode(int[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = Encode(values, valuesOffset);
                valuesOffset += ValueCount;
                blocksOffset = WriteLong(block, blocks, blocksOffset);
            }
        }
    }
}