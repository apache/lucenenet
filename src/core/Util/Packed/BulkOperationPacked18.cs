using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked18 : BulkOperationPacked
    {

        public BulkOperationPacked18()
            : base(18)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 46);
                values[valuesOffset++] = (int)(Number.URShift(block0, 28) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 10) & 262143L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1023L) << 8) | Number.URShift(block1, 56));
                values[valuesOffset++] = (int)(Number.URShift(block1, 38) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 20) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 2) & 262143L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 16) | Number.URShift(block2, 48));
                values[valuesOffset++] = (int)(Number.URShift(block2, 30) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 12) & 262143L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 4095L) << 6) | Number.URShift(block3, 58));
                values[valuesOffset++] = (int)(Number.URShift(block3, 40) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 22) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 4) & 262143L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 15L) << 14) | Number.URShift(block4, 50));
                values[valuesOffset++] = (int)(Number.URShift(block4, 32) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 14) & 262143L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 16383L) << 4) | Number.URShift(block5, 60));
                values[valuesOffset++] = (int)(Number.URShift(block5, 42) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 24) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 6) & 262143L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 63L) << 12) | Number.URShift(block6, 52));
                values[valuesOffset++] = (int)(Number.URShift(block6, 34) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block6, 16) & 262143L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 65535L) << 2) | Number.URShift(block7, 62));
                values[valuesOffset++] = (int)(Number.URShift(block7, 44) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block7, 26) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block7, 8) & 262143L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 255L) << 10) | Number.URShift(block8, 54));
                values[valuesOffset++] = (int)(Number.URShift(block8, 36) & 262143L);
                values[valuesOffset++] = (int)(Number.URShift(block8, 18) & 262143L);
                values[valuesOffset++] = (int)(block8 & 262143L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 10) | (byte1 << 2) | Number.URShift(byte2, 6);
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 63) << 12) | (byte3 << 4) | Number.URShift(byte4, 4);
                int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 15) << 14) | (byte5 << 6) | Number.URShift(byte6, 2);
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 3) << 16) | (byte7 << 8) | byte8;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 46);
                values[valuesOffset++] = Number.URShift(block0, 28) & 262143L;
                values[valuesOffset++] = Number.URShift(block0, 10) & 262143L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1023L) << 8) | Number.URShift(block1, 56);
                values[valuesOffset++] = Number.URShift(block1, 38) & 262143L;
                values[valuesOffset++] = Number.URShift(block1, 20) & 262143L;
                values[valuesOffset++] = Number.URShift(block1, 2) & 262143L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 16) | Number.URShift(block2, 48);
                values[valuesOffset++] = Number.URShift(block2, 30) & 262143L;
                values[valuesOffset++] = Number.URShift(block2, 12) & 262143L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 4095L) << 6) | Number.URShift(block3, 58);
                values[valuesOffset++] = Number.URShift(block3, 40) & 262143L;
                values[valuesOffset++] = Number.URShift(block3, 22) & 262143L;
                values[valuesOffset++] = Number.URShift(block3, 4) & 262143L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 15L) << 14) | Number.URShift(block4, 50);
                values[valuesOffset++] = Number.URShift(block4, 32) & 262143L;
                values[valuesOffset++] = Number.URShift(block4, 14) & 262143L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 16383L) << 4) | Number.URShift(block5, 60);
                values[valuesOffset++] = Number.URShift(block5, 42) & 262143L;
                values[valuesOffset++] = Number.URShift(block5, 24) & 262143L;
                values[valuesOffset++] = Number.URShift(block5, 6) & 262143L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 63L) << 12) | Number.URShift(block6, 52);
                values[valuesOffset++] = Number.URShift(block6, 34) & 262143L;
                values[valuesOffset++] = Number.URShift(block6, 16) & 262143L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 65535L) << 2) | Number.URShift(block7, 62);
                values[valuesOffset++] = Number.URShift(block7, 44) & 262143L;
                values[valuesOffset++] = Number.URShift(block7, 26) & 262143L;
                values[valuesOffset++] = Number.URShift(block7, 8) & 262143L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 255L) << 10) | Number.URShift(block8, 54);
                values[valuesOffset++] = Number.URShift(block8, 36) & 262143L;
                values[valuesOffset++] = Number.URShift(block8, 18) & 262143L;
                values[valuesOffset++] = block8 & 262143L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 10) | (byte1 << 2) | Number.URShift(byte2, 6);
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 63) << 12) | (byte3 << 4) | Number.URShift(byte4, 4);
                long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 15) << 14) | (byte5 << 6) | Number.URShift(byte6, 2);
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 3) << 16) | (byte7 << 8) | byte8;
            }
        }

    }

}