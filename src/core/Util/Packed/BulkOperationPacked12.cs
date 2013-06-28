using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked12 : BulkOperationPacked
    {

        public BulkOperationPacked12()
            : base(12)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 52);
                values[valuesOffset++] = (int)(Number.URShift(block0, 40) & 4095L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 28) & 4095L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 16) & 4095L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 4) & 4095L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 8) | Number.URShift(block1, 56));
                values[valuesOffset++] = (int)(Number.URShift(block1, 44) & 4095L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 32) & 4095L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 20) & 4095L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 8) & 4095L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 255L) << 4) | Number.URShift(block2, 60));
                values[valuesOffset++] = (int)(Number.URShift(block2, 48) & 4095L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 36) & 4095L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 24) & 4095L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 12) & 4095L);
                values[valuesOffset++] = (int)(block2 & 4095L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 4) | Number.URShift(byte1, 4);
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 15) << 8) | byte2;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 52);
                values[valuesOffset++] = Number.URShift(block0, 40) & 4095L;
                values[valuesOffset++] = Number.URShift(block0, 28) & 4095L;
                values[valuesOffset++] = Number.URShift(block0, 16) & 4095L;
                values[valuesOffset++] = Number.URShift(block0, 4) & 4095L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 8) | Number.URShift(block1, 56);
                values[valuesOffset++] = Number.URShift(block1, 44) & 4095L;
                values[valuesOffset++] = Number.URShift(block1, 32) & 4095L;
                values[valuesOffset++] = Number.URShift(block1, 20) & 4095L;
                values[valuesOffset++] = Number.URShift(block1, 8) & 4095L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 255L) << 4) | Number.URShift(block2, 60);
                values[valuesOffset++] = Number.URShift(block2, 48) & 4095L;
                values[valuesOffset++] = Number.URShift(block2, 36) & 4095L;
                values[valuesOffset++] = Number.URShift(block2, 24) & 4095L;
                values[valuesOffset++] = Number.URShift(block2, 12) & 4095L;
                values[valuesOffset++] = block2 & 4095L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 4) | Number.URShift(byte1, 4);
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 15) << 8) | byte2;
            }
        }

    }

}