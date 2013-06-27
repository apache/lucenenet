using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked24 : BulkOperationPacked
    {

        public BulkOperationPacked24()
            : base(24)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 40);
                values[valuesOffset++] = (int)(Number.URShift(block0, 16) & 16777215L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 65535L) << 8) | Number.URShift(block1, 56));
                values[valuesOffset++] = (int)(Number.URShift(block1, 32) & 16777215L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 8) & 16777215L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 255L) << 16) | Number.URShift(block2, 48));
                values[valuesOffset++] = (int)(Number.URShift(block2, 24) & 16777215L);
                values[valuesOffset++] = (int)(block2 & 16777215L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 16) | (byte1 << 8) | byte2;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 40);
                values[valuesOffset++] = Number.URShift(block0, 16) & 16777215L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 65535L) << 8) | Number.URShift(block1, 56);
                values[valuesOffset++] = Number.URShift(block1, 32) & 16777215L;
                values[valuesOffset++] = Number.URShift(block1, 8) & 16777215L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 255L) << 16) | Number.URShift(block2, 48);
                values[valuesOffset++] = Number.URShift(block2, 24) & 16777215L;
                values[valuesOffset++] = block2 & 16777215L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 16) | (byte1 << 8) | byte2;
            }
        }

    }

}