using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked14 : BulkOperationPacked
    {

        public BulkOperationPacked14()
            : base(14)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 50);
                values[valuesOffset++] = (int)(Number.URShift(block0, 36) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 22) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 8) & 16383L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 255L) << 6) | Number.URShift(block1, 58));
                values[valuesOffset++] = (int)(Number.URShift(block1, 44) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 30) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 16) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 2) & 16383L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 12) | Number.URShift(block2, 52));
                values[valuesOffset++] = (int)(Number.URShift(block2, 38) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 24) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 10) & 16383L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 1023L) << 4) | Number.URShift(block3, 60));
                values[valuesOffset++] = (int)(Number.URShift(block3, 46) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 32) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 18) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 4) & 16383L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 15L) << 10) | Number.URShift(block4, 54));
                values[valuesOffset++] = (int)(Number.URShift(block4, 40) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 26) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 12) & 16383L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 4095L) << 2) | Number.URShift(block5, 62));
                values[valuesOffset++] = (int)(Number.URShift(block5, 48) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 34) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 20) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 6) & 16383L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 63L) << 8) | Number.URShift(block6, 56));
                values[valuesOffset++] = (int)(Number.URShift(block6, 42) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block6, 28) & 16383L);
                values[valuesOffset++] = (int)(Number.URShift(block6, 14) & 16383L);
                values[valuesOffset++] = (int)(block6 & 16383L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 6) | Number.URShift(byte1, 2);
                int byte2 = blocks[blocksOffset++] & 0xFF;
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 3) << 12) | (byte2 << 4) | Number.URShift(byte3, 4);
                int byte4 = blocks[blocksOffset++] & 0xFF;
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 15) << 10) | (byte4 << 2) | Number.URShift(byte5, 6);
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 63) << 8) | byte6;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 50);
                values[valuesOffset++] = Number.URShift(block0, 36) & 16383L;
                values[valuesOffset++] = Number.URShift(block0, 22) & 16383L;
                values[valuesOffset++] = Number.URShift(block0, 8) & 16383L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 255L) << 6) | Number.URShift(block1, 58);
                values[valuesOffset++] = Number.URShift(block1, 44) & 16383L;
                values[valuesOffset++] = Number.URShift(block1, 30) & 16383L;
                values[valuesOffset++] = Number.URShift(block1, 16) & 16383L;
                values[valuesOffset++] = Number.URShift(block1, 2) & 16383L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 12) | Number.URShift(block2, 52);
                values[valuesOffset++] = Number.URShift(block2, 38) & 16383L;
                values[valuesOffset++] = Number.URShift(block2, 24) & 16383L;
                values[valuesOffset++] = Number.URShift(block2, 10) & 16383L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 1023L) << 4) | Number.URShift(block3, 60);
                values[valuesOffset++] = Number.URShift(block3, 46) & 16383L;
                values[valuesOffset++] = Number.URShift(block3, 32) & 16383L;
                values[valuesOffset++] = Number.URShift(block3, 18) & 16383L;
                values[valuesOffset++] = Number.URShift(block3, 4) & 16383L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 15L) << 10) | Number.URShift(block4, 54);
                values[valuesOffset++] = Number.URShift(block4, 40) & 16383L;
                values[valuesOffset++] = Number.URShift(block4, 26) & 16383L;
                values[valuesOffset++] = Number.URShift(block4, 12) & 16383L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 4095L) << 2) | Number.URShift(block5, 62);
                values[valuesOffset++] = Number.URShift(block5, 48) & 16383L;
                values[valuesOffset++] = Number.URShift(block5, 34) & 16383L;
                values[valuesOffset++] = Number.URShift(block5, 20) & 16383L;
                values[valuesOffset++] = Number.URShift(block5, 6) & 16383L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 63L) << 8) | Number.URShift(block6, 56);
                values[valuesOffset++] = Number.URShift(block6, 42) & 16383L;
                values[valuesOffset++] = Number.URShift(block6, 28) & 16383L;
                values[valuesOffset++] = Number.URShift(block6, 14) & 16383L;
                values[valuesOffset++] = block6 & 16383L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 6) | Number.URShift(byte1, 2);
                long byte2 = blocks[blocksOffset++] & 0xFF;
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 3) << 12) | (byte2 << 4) | Number.URShift(byte3, 4);
                long byte4 = blocks[blocksOffset++] & 0xFF;
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 15) << 10) | (byte4 << 2) | Number.URShift(byte5, 6);
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 63) << 8) | byte6;
            }
        }

    }

}