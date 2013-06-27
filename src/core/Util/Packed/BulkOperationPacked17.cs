using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked17 : BulkOperationPacked
    {

        public BulkOperationPacked17()
            : base(17)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 47);
                values[valuesOffset++] = (int)(Number.URShift(block0, 30) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 13) & 131071L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 8191L) << 4) | Number.URShift(block1, 60));
                values[valuesOffset++] = (int)(Number.URShift(block1, 43) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 26) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 9) & 131071L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 511L) << 8) | Number.URShift(block2, 56));
                values[valuesOffset++] = (int)(Number.URShift(block2, 39) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 22) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 5) & 131071L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 31L) << 12) | Number.URShift(block3, 52));
                values[valuesOffset++] = (int)(Number.URShift(block3, 35) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 18) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 1) & 131071L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 1L) << 16) | Number.URShift(block4, 48));
                values[valuesOffset++] = (int)(Number.URShift(block4, 31) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 14) & 131071L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 16383L) << 3) | Number.URShift(block5, 61));
                values[valuesOffset++] = (int)(Number.URShift(block5, 44) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 27) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 10) & 131071L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 1023L) << 7) | Number.URShift(block6, 57));
                values[valuesOffset++] = (int)(Number.URShift(block6, 40) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block6, 23) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block6, 6) & 131071L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 63L) << 11) | Number.URShift(block7, 53));
                values[valuesOffset++] = (int)(Number.URShift(block7, 36) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block7, 19) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block7, 2) & 131071L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 3L) << 15) | Number.URShift(block8, 49));
                values[valuesOffset++] = (int)(Number.URShift(block8, 32) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block8, 15) & 131071L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 32767L) << 2) | Number.URShift(block9, 62));
                values[valuesOffset++] = (int)(Number.URShift(block9, 45) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block9, 28) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block9, 11) & 131071L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 2047L) << 6) | Number.URShift(block10, 58));
                values[valuesOffset++] = (int)(Number.URShift(block10, 41) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block10, 24) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block10, 7) & 131071L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 127L) << 10) | Number.URShift(block11, 54));
                values[valuesOffset++] = (int)(Number.URShift(block11, 37) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block11, 20) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block11, 3) & 131071L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 7L) << 14) | Number.URShift(block12, 50));
                values[valuesOffset++] = (int)(Number.URShift(block12, 33) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block12, 16) & 131071L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 65535L) << 1) | Number.URShift(block13, 63));
                values[valuesOffset++] = (int)(Number.URShift(block13, 46) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block13, 29) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block13, 12) & 131071L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 4095L) << 5) | Number.URShift(block14, 59));
                values[valuesOffset++] = (int)(Number.URShift(block14, 42) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block14, 25) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block14, 8) & 131071L);
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block14 & 255L) << 9) | Number.URShift(block15, 55));
                values[valuesOffset++] = (int)(Number.URShift(block15, 38) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block15, 21) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block15, 4) & 131071L);
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block15 & 15L) << 13) | Number.URShift(block16, 51));
                values[valuesOffset++] = (int)(Number.URShift(block16, 34) & 131071L);
                values[valuesOffset++] = (int)(Number.URShift(block16, 17) & 131071L);
                values[valuesOffset++] = (int)(block16 & 131071L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 9) | (byte1 << 1) | Number.URShift(byte2, 7);
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 127) << 10) | (byte3 << 2) | Number.URShift(byte4, 6);
                int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 63) << 11) | (byte5 << 3) | Number.URShift(byte6, 5);
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 31) << 12) | (byte7 << 4) | Number.URShift(byte8, 4);
                int byte9 = blocks[blocksOffset++] & 0xFF;
                int byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 15) << 13) | (byte9 << 5) | Number.URShift(byte10, 3);
                int byte11 = blocks[blocksOffset++] & 0xFF;
                int byte12 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte10 & 7) << 14) | (byte11 << 6) | Number.URShift(byte12, 2);
                int byte13 = blocks[blocksOffset++] & 0xFF;
                int byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte12 & 3) << 15) | (byte13 << 7) | Number.URShift(byte14, 1);
                int byte15 = blocks[blocksOffset++] & 0xFF;
                int byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 1) << 16) | (byte15 << 8) | byte16;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 47);
                values[valuesOffset++] = Number.URShift(block0, 30) & 131071L;
                values[valuesOffset++] = Number.URShift(block0, 13) & 131071L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 8191L) << 4) | Number.URShift(block1, 60);
                values[valuesOffset++] = Number.URShift(block1, 43) & 131071L;
                values[valuesOffset++] = Number.URShift(block1, 26) & 131071L;
                values[valuesOffset++] = Number.URShift(block1, 9) & 131071L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 511L) << 8) | Number.URShift(block2, 56);
                values[valuesOffset++] = Number.URShift(block2, 39) & 131071L;
                values[valuesOffset++] = Number.URShift(block2, 22) & 131071L;
                values[valuesOffset++] = Number.URShift(block2, 5) & 131071L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 31L) << 12) | Number.URShift(block3, 52);
                values[valuesOffset++] = Number.URShift(block3, 35) & 131071L;
                values[valuesOffset++] = Number.URShift(block3, 18) & 131071L;
                values[valuesOffset++] = Number.URShift(block3, 1) & 131071L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 1L) << 16) | Number.URShift(block4, 48);
                values[valuesOffset++] = Number.URShift(block4, 31) & 131071L;
                values[valuesOffset++] = Number.URShift(block4, 14) & 131071L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 16383L) << 3) | Number.URShift(block5, 61);
                values[valuesOffset++] = Number.URShift(block5, 44) & 131071L;
                values[valuesOffset++] = Number.URShift(block5, 27) & 131071L;
                values[valuesOffset++] = Number.URShift(block5, 10) & 131071L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 1023L) << 7) | Number.URShift(block6, 57);
                values[valuesOffset++] = Number.URShift(block6, 40) & 131071L;
                values[valuesOffset++] = Number.URShift(block6, 23) & 131071L;
                values[valuesOffset++] = Number.URShift(block6, 6) & 131071L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 63L) << 11) | Number.URShift(block7, 53);
                values[valuesOffset++] = Number.URShift(block7, 36) & 131071L;
                values[valuesOffset++] = Number.URShift(block7, 19) & 131071L;
                values[valuesOffset++] = Number.URShift(block7, 2) & 131071L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 3L) << 15) | Number.URShift(block8, 49);
                values[valuesOffset++] = Number.URShift(block8, 32) & 131071L;
                values[valuesOffset++] = Number.URShift(block8, 15) & 131071L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 32767L) << 2) | Number.URShift(block9, 62);
                values[valuesOffset++] = Number.URShift(block9, 45) & 131071L;
                values[valuesOffset++] = Number.URShift(block9, 28) & 131071L;
                values[valuesOffset++] = Number.URShift(block9, 11) & 131071L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 2047L) << 6) | Number.URShift(block10, 58);
                values[valuesOffset++] = Number.URShift(block10, 41) & 131071L;
                values[valuesOffset++] = Number.URShift(block10, 24) & 131071L;
                values[valuesOffset++] = Number.URShift(block10, 7) & 131071L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 127L) << 10) | Number.URShift(block11, 54);
                values[valuesOffset++] = Number.URShift(block11, 37) & 131071L;
                values[valuesOffset++] = Number.URShift(block11, 20) & 131071L;
                values[valuesOffset++] = Number.URShift(block11, 3) & 131071L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 7L) << 14) | Number.URShift(block12, 50);
                values[valuesOffset++] = Number.URShift(block12, 33) & 131071L;
                values[valuesOffset++] = Number.URShift(block12, 16) & 131071L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 65535L) << 1) | Number.URShift(block13, 63);
                values[valuesOffset++] = Number.URShift(block13, 46) & 131071L;
                values[valuesOffset++] = Number.URShift(block13, 29) & 131071L;
                values[valuesOffset++] = Number.URShift(block13, 12) & 131071L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 4095L) << 5) | Number.URShift(block14, 59);
                values[valuesOffset++] = Number.URShift(block14, 42) & 131071L;
                values[valuesOffset++] = Number.URShift(block14, 25) & 131071L;
                values[valuesOffset++] = Number.URShift(block14, 8) & 131071L;
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block14 & 255L) << 9) | Number.URShift(block15, 55);
                values[valuesOffset++] = Number.URShift(block15, 38) & 131071L;
                values[valuesOffset++] = Number.URShift(block15, 21) & 131071L;
                values[valuesOffset++] = Number.URShift(block15, 4) & 131071L;
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block15 & 15L) << 13) | Number.URShift(block16, 51);
                values[valuesOffset++] = Number.URShift(block16, 34) & 131071L;
                values[valuesOffset++] = Number.URShift(block16, 17) & 131071L;
                values[valuesOffset++] = block16 & 131071L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 9) | (byte1 << 1) | Number.URShift(byte2, 7);
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 127) << 10) | (byte3 << 2) | Number.URShift(byte4, 6);
                long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 63) << 11) | (byte5 << 3) | Number.URShift(byte6, 5);
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 31) << 12) | (byte7 << 4) | Number.URShift(byte8, 4);
                long byte9 = blocks[blocksOffset++] & 0xFF;
                long byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 15) << 13) | (byte9 << 5) | Number.URShift(byte10, 3);
                long byte11 = blocks[blocksOffset++] & 0xFF;
                long byte12 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte10 & 7) << 14) | (byte11 << 6) | Number.URShift(byte12, 2);
                long byte13 = blocks[blocksOffset++] & 0xFF;
                long byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte12 & 3) << 15) | (byte13 << 7) | Number.URShift(byte14, 1);
                long byte15 = blocks[blocksOffset++] & 0xFF;
                long byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 1) << 16) | (byte15 << 8) | byte16;
            }
        }

    }

}