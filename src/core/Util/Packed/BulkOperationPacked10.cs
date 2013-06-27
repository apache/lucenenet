using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked10 : BulkOperationPacked
    {

        public BulkOperationPacked10()
            : base(10)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 54);
                values[valuesOffset++] = (int)(Number.URShift(block0, 44) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 34) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 24) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 14) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 4) & 1023L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 6) | Number.URShift(block1, 58));
                values[valuesOffset++] = (int)(Number.URShift(block1, 48) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 38) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 28) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 18) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 8) & 1023L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 255L) << 2) | Number.URShift(block2, 62));
                values[valuesOffset++] = (int)(Number.URShift(block2, 52) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 42) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 32) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 22) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 12) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 2) & 1023L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 3L) << 8) | Number.URShift(block3, 56));
                values[valuesOffset++] = (int)(Number.URShift(block3, 46) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 36) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 26) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 16) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 6) & 1023L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 63L) << 4) | Number.URShift(block4, 60));
                values[valuesOffset++] = (int)(Number.URShift(block4, 50) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 40) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 30) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 20) & 1023L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 10) & 1023L);
                values[valuesOffset++] = (int)(block4 & 1023L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 2) | Number.URShift(byte1, 6);
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 63) << 4) | Number.URShift(byte2, 4);
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 15) << 6) | Number.URShift(byte3, 2);
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 8) | byte4;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 54);
                values[valuesOffset++] = Number.URShift(block0, 44) & 1023L;
                values[valuesOffset++] = Number.URShift(block0, 34) & 1023L;
                values[valuesOffset++] = Number.URShift(block0, 24) & 1023L;
                values[valuesOffset++] = Number.URShift(block0, 14) & 1023L;
                values[valuesOffset++] = Number.URShift(block0, 4) & 1023L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 6) | Number.URShift(block1, 58);
                values[valuesOffset++] = Number.URShift(block1, 48) & 1023L;
                values[valuesOffset++] = Number.URShift(block1, 38) & 1023L;
                values[valuesOffset++] = Number.URShift(block1, 28) & 1023L;
                values[valuesOffset++] = Number.URShift(block1, 18) & 1023L;
                values[valuesOffset++] = Number.URShift(block1, 8) & 1023L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 255L) << 2) | Number.URShift(block2, 62);
                values[valuesOffset++] = Number.URShift(block2, 52) & 1023L;
                values[valuesOffset++] = Number.URShift(block2, 42) & 1023L;
                values[valuesOffset++] = Number.URShift(block2, 32) & 1023L;
                values[valuesOffset++] = Number.URShift(block2, 22) & 1023L;
                values[valuesOffset++] = Number.URShift(block2, 12) & 1023L;
                values[valuesOffset++] = Number.URShift(block2, 2) & 1023L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 3L) << 8) | Number.URShift(block3, 56);
                values[valuesOffset++] = Number.URShift(block3, 46) & 1023L;
                values[valuesOffset++] = Number.URShift(block3, 36) & 1023L;
                values[valuesOffset++] = Number.URShift(block3, 26) & 1023L;
                values[valuesOffset++] = Number.URShift(block3, 16) & 1023L;
                values[valuesOffset++] = Number.URShift(block3, 6) & 1023L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 63L) << 4) | Number.URShift(block4, 60);
                values[valuesOffset++] = Number.URShift(block4, 50) & 1023L;
                values[valuesOffset++] = Number.URShift(block4, 40) & 1023L;
                values[valuesOffset++] = Number.URShift(block4, 30) & 1023L;
                values[valuesOffset++] = Number.URShift(block4, 20) & 1023L;
                values[valuesOffset++] = Number.URShift(block4, 10) & 1023L;
                values[valuesOffset++] = block4 & 1023L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 2) | Number.URShift(byte1, 6);
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 63) << 4) | Number.URShift(byte2, 4);
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 15) << 6) | Number.URShift(byte3, 2);
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 8) | byte4;
            }
        }

    }

}