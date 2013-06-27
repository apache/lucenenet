using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked15 : BulkOperationPacked
    {

        public BulkOperationPacked15()
            : base(15)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 49);
                values[valuesOffset++] = (int)(Number.URShift(block0, 34) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 19) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 4) & 32767L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 11) | Number.URShift(block1, 53));
                values[valuesOffset++] = (int)(Number.URShift(block1, 38) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 23) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 8) & 32767L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 255L) << 7) | Number.URShift(block2, 57));
                values[valuesOffset++] = (int)(Number.URShift(block2, 42) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 27) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 12) & 32767L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 4095L) << 3) | Number.URShift(block3, 61));
                values[valuesOffset++] = (int)(Number.URShift(block3, 46) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 31) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 16) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 1) & 32767L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 1L) << 14) | Number.URShift(block4, 50));
                values[valuesOffset++] = (int)(Number.URShift(block4, 35) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 20) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 5) & 32767L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 31L) << 10) | Number.URShift(block5, 54));
                values[valuesOffset++] = (int)(Number.URShift(block5, 39) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 24) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 9) & 32767L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 511L) << 6) | Number.URShift(block6, 58));
                values[valuesOffset++] = (int)(Number.URShift(block6, 43) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block6, 28) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block6, 13) & 32767L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 8191L) << 2) | Number.URShift(block7, 62));
                values[valuesOffset++] = (int)(Number.URShift(block7, 47) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block7, 32) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block7, 17) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block7, 2) & 32767L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 3L) << 13) | Number.URShift(block8, 51));
                values[valuesOffset++] = (int)(Number.URShift(block8, 36) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block8, 21) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block8, 6) & 32767L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 63L) << 9) | Number.URShift(block9, 55));
                values[valuesOffset++] = (int)(Number.URShift(block9, 40) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block9, 25) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block9, 10) & 32767L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 1023L) << 5) | Number.URShift(block10, 59));
                values[valuesOffset++] = (int)(Number.URShift(block10, 44) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block10, 29) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block10, 14) & 32767L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 16383L) << 1) | Number.URShift(block11, 63));
                values[valuesOffset++] = (int)(Number.URShift(block11, 48) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block11, 33) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block11, 18) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block11, 3) & 32767L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 7L) << 12) | Number.URShift(block12, 52));
                values[valuesOffset++] = (int)(Number.URShift(block12, 37) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block12, 22) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block12, 7) & 32767L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 127L) << 8) | Number.URShift(block13, 56));
                values[valuesOffset++] = (int)(Number.URShift(block13, 41) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block13, 26) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block13, 11) & 32767L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 2047L) << 4) | Number.URShift(block14, 60));
                values[valuesOffset++] = (int)(Number.URShift(block14, 45) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block14, 30) & 32767L);
                values[valuesOffset++] = (int)(Number.URShift(block14, 15) & 32767L);
                values[valuesOffset++] = (int)(block14 & 32767L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 7) | Number.URShift(byte1, 1);
                int byte2 = blocks[blocksOffset++] & 0xFF;
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 14) | (byte2 << 6) | Number.URShift(byte3, 2);
                int byte4 = blocks[blocksOffset++] & 0xFF;
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 13) | (byte4 << 5) | Number.URShift(byte5, 3);
                int byte6 = blocks[blocksOffset++] & 0xFF;
                int byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 7) << 12) | (byte6 << 4) | Number.URShift(byte7, 4);
                int byte8 = blocks[blocksOffset++] & 0xFF;
                int byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 15) << 11) | (byte8 << 3) | Number.URShift(byte9, 5);
                int byte10 = blocks[blocksOffset++] & 0xFF;
                int byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 31) << 10) | (byte10 << 2) | Number.URShift(byte11, 6);
                int byte12 = blocks[blocksOffset++] & 0xFF;
                int byte13 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 63) << 9) | (byte12 << 1) | Number.URShift(byte13, 7);
                int byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte13 & 127) << 8) | byte14;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 49);
                values[valuesOffset++] = Number.URShift(block0, 34) & 32767L;
                values[valuesOffset++] = Number.URShift(block0, 19) & 32767L;
                values[valuesOffset++] = Number.URShift(block0, 4) & 32767L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 11) | Number.URShift(block1, 53);
                values[valuesOffset++] = Number.URShift(block1, 38) & 32767L;
                values[valuesOffset++] = Number.URShift(block1, 23) & 32767L;
                values[valuesOffset++] = Number.URShift(block1, 8) & 32767L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 255L) << 7) | Number.URShift(block2, 57);
                values[valuesOffset++] = Number.URShift(block2, 42) & 32767L;
                values[valuesOffset++] = Number.URShift(block2, 27) & 32767L;
                values[valuesOffset++] = Number.URShift(block2, 12) & 32767L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 4095L) << 3) | Number.URShift(block3, 61);
                values[valuesOffset++] = Number.URShift(block3, 46) & 32767L;
                values[valuesOffset++] = Number.URShift(block3, 31) & 32767L;
                values[valuesOffset++] = Number.URShift(block3, 16) & 32767L;
                values[valuesOffset++] = Number.URShift(block3, 1) & 32767L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 1L) << 14) | Number.URShift(block4, 50);
                values[valuesOffset++] = Number.URShift(block4, 35) & 32767L;
                values[valuesOffset++] = Number.URShift(block4, 20) & 32767L;
                values[valuesOffset++] = Number.URShift(block4, 5) & 32767L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 31L) << 10) | Number.URShift(block5, 54);
                values[valuesOffset++] = Number.URShift(block5, 39) & 32767L;
                values[valuesOffset++] = Number.URShift(block5, 24) & 32767L;
                values[valuesOffset++] = Number.URShift(block5, 9) & 32767L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 511L) << 6) | Number.URShift(block6, 58);
                values[valuesOffset++] = Number.URShift(block6, 43) & 32767L;
                values[valuesOffset++] = Number.URShift(block6, 28) & 32767L;
                values[valuesOffset++] = Number.URShift(block6, 13) & 32767L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 8191L) << 2) | Number.URShift(block7, 62);
                values[valuesOffset++] = Number.URShift(block7, 47) & 32767L;
                values[valuesOffset++] = Number.URShift(block7, 32) & 32767L;
                values[valuesOffset++] = Number.URShift(block7, 17) & 32767L;
                values[valuesOffset++] = Number.URShift(block7, 2) & 32767L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 3L) << 13) | Number.URShift(block8, 51);
                values[valuesOffset++] = Number.URShift(block8, 36) & 32767L;
                values[valuesOffset++] = Number.URShift(block8, 21) & 32767L;
                values[valuesOffset++] = Number.URShift(block8, 6) & 32767L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 63L) << 9) | Number.URShift(block9, 55);
                values[valuesOffset++] = Number.URShift(block9, 40) & 32767L;
                values[valuesOffset++] = Number.URShift(block9, 25) & 32767L;
                values[valuesOffset++] = Number.URShift(block9, 10) & 32767L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 1023L) << 5) | Number.URShift(block10, 59);
                values[valuesOffset++] = Number.URShift(block10, 44) & 32767L;
                values[valuesOffset++] = Number.URShift(block10, 29) & 32767L;
                values[valuesOffset++] = Number.URShift(block10, 14) & 32767L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 16383L) << 1) | Number.URShift(block11, 63);
                values[valuesOffset++] = Number.URShift(block11, 48) & 32767L;
                values[valuesOffset++] = Number.URShift(block11, 33) & 32767L;
                values[valuesOffset++] = Number.URShift(block11, 18) & 32767L;
                values[valuesOffset++] = Number.URShift(block11, 3) & 32767L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 7L) << 12) | Number.URShift(block12, 52);
                values[valuesOffset++] = Number.URShift(block12, 37) & 32767L;
                values[valuesOffset++] = Number.URShift(block12, 22) & 32767L;
                values[valuesOffset++] = Number.URShift(block12, 7) & 32767L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 127L) << 8) | Number.URShift(block13, 56);
                values[valuesOffset++] = Number.URShift(block13, 41) & 32767L;
                values[valuesOffset++] = Number.URShift(block13, 26) & 32767L;
                values[valuesOffset++] = Number.URShift(block13, 11) & 32767L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 2047L) << 4) | Number.URShift(block14, 60);
                values[valuesOffset++] = Number.URShift(block14, 45) & 32767L;
                values[valuesOffset++] = Number.URShift(block14, 30) & 32767L;
                values[valuesOffset++] = Number.URShift(block14, 15) & 32767L;
                values[valuesOffset++] = block14 & 32767L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 7) | Number.URShift(byte1, 1);
                long byte2 = blocks[blocksOffset++] & 0xFF;
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 14) | (byte2 << 6) | Number.URShift(byte3, 2);
                long byte4 = blocks[blocksOffset++] & 0xFF;
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 13) | (byte4 << 5) | Number.URShift(byte5, 3);
                long byte6 = blocks[blocksOffset++] & 0xFF;
                long byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 7) << 12) | (byte6 << 4) | Number.URShift(byte7, 4);
                long byte8 = blocks[blocksOffset++] & 0xFF;
                long byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 15) << 11) | (byte8 << 3) | Number.URShift(byte9, 5);
                long byte10 = blocks[blocksOffset++] & 0xFF;
                long byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 31) << 10) | (byte10 << 2) | Number.URShift(byte11, 6);
                long byte12 = blocks[blocksOffset++] & 0xFF;
                long byte13 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 63) << 9) | (byte12 << 1) | Number.URShift(byte13, 7);
                long byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte13 & 127) << 8) | byte14;
            }
        }

    }

}