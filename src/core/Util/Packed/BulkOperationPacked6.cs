using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked6 : BulkOperationPacked
    {

        public BulkOperationPacked6()
            : base(6)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 58);
                values[valuesOffset++] = (int)(Number.URShift(block0, 52) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 46) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 40) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 34) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 28) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 22) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 16) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 10) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 4) & 63L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 2) | Number.URShift(block1, 62));
                values[valuesOffset++] = (int)(Number.URShift(block1, 56) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 50) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 44) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 38) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 32) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 26) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 20) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 14) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 8) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 2) & 63L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 4) | Number.URShift(block2, 60));
                values[valuesOffset++] = (int)(Number.URShift(block2, 54) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 48) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 42) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 36) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 30) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 24) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 18) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 12) & 63L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 6) & 63L);
                values[valuesOffset++] = (int)(block2 & 63L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = Number.URShift(byte0, 2);
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 3) << 4) | Number.URShift(byte1, 4);
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 15) << 2) | Number.URShift(byte2, 6);
                values[valuesOffset++] = byte2 & 63;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 58);
                values[valuesOffset++] = Number.URShift(block0, 52) & 63L;
                values[valuesOffset++] = Number.URShift(block0, 46) & 63L;
                values[valuesOffset++] = Number.URShift(block0, 40) & 63L;
                values[valuesOffset++] = Number.URShift(block0, 34) & 63L;
                values[valuesOffset++] = Number.URShift(block0, 28) & 63L;
                values[valuesOffset++] = Number.URShift(block0, 22) & 63L;
                values[valuesOffset++] = Number.URShift(block0, 16) & 63L;
                values[valuesOffset++] = Number.URShift(block0, 10) & 63L;
                values[valuesOffset++] = Number.URShift(block0, 4) & 63L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 2) | Number.URShift(block1, 62);
                values[valuesOffset++] = Number.URShift(block1, 56) & 63L;
                values[valuesOffset++] = Number.URShift(block1, 50) & 63L;
                values[valuesOffset++] = Number.URShift(block1, 44) & 63L;
                values[valuesOffset++] = Number.URShift(block1, 38) & 63L;
                values[valuesOffset++] = Number.URShift(block1, 32) & 63L;
                values[valuesOffset++] = Number.URShift(block1, 26) & 63L;
                values[valuesOffset++] = Number.URShift(block1, 20) & 63L;
                values[valuesOffset++] = Number.URShift(block1, 14) & 63L;
                values[valuesOffset++] = Number.URShift(block1, 8) & 63L;
                values[valuesOffset++] = Number.URShift(block1, 2) & 63L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 4) | Number.URShift(block2, 60);
                values[valuesOffset++] = Number.URShift(block2, 54) & 63L;
                values[valuesOffset++] = Number.URShift(block2, 48) & 63L;
                values[valuesOffset++] = Number.URShift(block2, 42) & 63L;
                values[valuesOffset++] = Number.URShift(block2, 36) & 63L;
                values[valuesOffset++] = Number.URShift(block2, 30) & 63L;
                values[valuesOffset++] = Number.URShift(block2, 24) & 63L;
                values[valuesOffset++] = Number.URShift(block2, 18) & 63L;
                values[valuesOffset++] = Number.URShift(block2, 12) & 63L;
                values[valuesOffset++] = Number.URShift(block2, 6) & 63L;
                values[valuesOffset++] = block2 & 63L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = Number.URShift(byte0, 2);
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 3) << 4) | Number.URShift(byte1, 4);
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 15) << 2) | Number.URShift(byte2, 6);
                values[valuesOffset++] = byte2 & 63;
            }
        }

    }

}