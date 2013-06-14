using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
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

        public override int LongBlockCount
        {
            get { return BLOCK_COUNT; }
        }

        public override int ByteBlockCount
        {
            get { return BLOCK_COUNT * 8; }
        }

        public override int LongValueCount
        {
            get { return valueCount; }
        }

        public override int ByteValueCount
        {
            get { return valueCount; }
        }

        private static long ReadLong(sbyte[] blocks, int blocksOffset)
        {
            return (blocks[blocksOffset++] & 0xFFL) << 56
                | (blocks[blocksOffset++] & 0xFFL) << 48
                | (blocks[blocksOffset++] & 0xFFL) << 40
                | (blocks[blocksOffset++] & 0xFFL) << 32
                | (blocks[blocksOffset++] & 0xFFL) << 24
                | (blocks[blocksOffset++] & 0xFFL) << 16
                | (blocks[blocksOffset++] & 0xFFL) << 8
                | blocks[blocksOffset++] & 0xFFL;
        }

        private int Decode(long block, long[] values, int valuesOffset)
        {
            values[valuesOffset++] = block & mask;
            for (int j = 1; j < valueCount; ++j)
            {
                block = Number.URShift(block, bitsPerValue);
                values[valuesOffset++] = block & mask;
            }
            return valuesOffset;
        }

        private int Decode(long block, int[] values, int valuesOffset)
        {
            values[valuesOffset++] = (int)(block & mask);
            for (int j = 1; j < valueCount; ++j)
            {
                block = Number.URShift(block, bitsPerValue);
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

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
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
            if (bitsPerValue > 32)
            {
                throw new NotSupportedException("Cannot decode " + bitsPerValue + "-bits values into an int[]");
            }
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                valuesOffset = Decode(block, values, valuesOffset);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            if (bitsPerValue > 32)
            {
                throw new NotSupportedException("Cannot decode " + bitsPerValue + "-bits values into an int[]");
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

        public override void Encode(long[] values, int valuesOffset, sbyte[] blocks, int blocksOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = Encode(values, valuesOffset);
                valuesOffset += valueCount;
                blocksOffset = WriteLong(block, blocks, blocksOffset);
            }
        }

        public override void Encode(int[] values, int valuesOffset, sbyte[] blocks, int blocksOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = Encode(values, valuesOffset);
                valuesOffset += valueCount;
                blocksOffset = WriteLong(block, blocks, blocksOffset);
            }
        }
    }
}
