using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked19 : BulkOperationPacked
    {

        public BulkOperationPacked19()
            : base(19)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 45);
                values[valuesOffset++] = (int)(Number.URShift(block0, 26) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 7) & 524287L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 127L) << 12) | Number.URShift(block1, 52));
                values[valuesOffset++] = (int)(Number.URShift(block1, 33) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 14) & 524287L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 16383L) << 5) | Number.URShift(block2, 59));
                values[valuesOffset++] = (int)(Number.URShift(block2, 40) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 21) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 2) & 524287L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 3L) << 17) | Number.URShift(block3, 47));
                values[valuesOffset++] = (int)(Number.URShift(block3, 28) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 9) & 524287L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 511L) << 10) | Number.URShift(block4, 54));
                values[valuesOffset++] = (int)(Number.URShift(block4, 35) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 16) & 524287L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 65535L) << 3) | Number.URShift(block5, 61));
                values[valuesOffset++] = (int)(Number.URShift(block5, 42) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 23) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 4) & 524287L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 15L) << 15) | Number.URShift(block6, 49));
                values[valuesOffset++] = (int)(Number.URShift(block6, 30) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block6, 11) & 524287L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 2047L) << 8) | Number.URShift(block7, 56));
                values[valuesOffset++] = (int)(Number.URShift(block7, 37) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block7, 18) & 524287L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 262143L) << 1) | Number.URShift(block8, 63));
                values[valuesOffset++] = (int)(Number.URShift(block8, 44) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block8, 25) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block8, 6) & 524287L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 63L) << 13) | Number.URShift(block9, 51));
                values[valuesOffset++] = (int)(Number.URShift(block9, 32) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block9, 13) & 524287L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 8191L) << 6) | Number.URShift(block10, 58));
                values[valuesOffset++] = (int)(Number.URShift(block10, 39) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block10, 20) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block10, 1) & 524287L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 1L) << 18) | Number.URShift(block11, 46));
                values[valuesOffset++] = (int)(Number.URShift(block11, 27) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block11, 8) & 524287L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 255L) << 11) | Number.URShift(block12, 53));
                values[valuesOffset++] = (int)(Number.URShift(block12, 34) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block12, 15) & 524287L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 32767L) << 4) | Number.URShift(block13, 60));
                values[valuesOffset++] = (int)(Number.URShift(block13, 41) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block13, 22) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block13, 3) & 524287L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 7L) << 16) | Number.URShift(block14, 48));
                values[valuesOffset++] = (int)(Number.URShift(block14, 29) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block14, 10) & 524287L);
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block14 & 1023L) << 9) | Number.URShift(block15, 55));
                values[valuesOffset++] = (int)(Number.URShift(block15, 36) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block15, 17) & 524287L);
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block15 & 131071L) << 2) | Number.URShift(block16, 62));
                values[valuesOffset++] = (int)(Number.URShift(block16, 43) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block16, 24) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block16, 5) & 524287L);
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block16 & 31L) << 14) | Number.URShift(block17, 50));
                values[valuesOffset++] = (int)(Number.URShift(block17, 31) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block17, 12) & 524287L);
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block17 & 4095L) << 7) | Number.URShift(block18, 57));
                values[valuesOffset++] = (int)(Number.URShift(block18, 38) & 524287L);
                values[valuesOffset++] = (int)(Number.URShift(block18, 19) & 524287L);
                values[valuesOffset++] = (int)(block18 & 524287L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 11) | (byte1 << 3) | Number.URShift(byte2, 5);
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 31) << 14) | (byte3 << 6) | Number.URShift(byte4, 2);
                int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                int byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 3) << 17) | (byte5 << 9) | (byte6 << 1) | Number.URShift(byte7, 7);
                int byte8 = blocks[blocksOffset++] & 0xFF;
                int byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 127) << 12) | (byte8 << 4) | Number.URShift(byte9, 4);
                int byte10 = blocks[blocksOffset++] & 0xFF;
                int byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 15) << 15) | (byte10 << 7) | Number.URShift(byte11, 1);
                int byte12 = blocks[blocksOffset++] & 0xFF;
                int byte13 = blocks[blocksOffset++] & 0xFF;
                int byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 1) << 18) | (byte12 << 10) | (byte13 << 2) | Number.URShift(byte14, 6);
                int byte15 = blocks[blocksOffset++] & 0xFF;
                int byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 63) << 13) | (byte15 << 5) | Number.URShift(byte16, 3);
                int byte17 = blocks[blocksOffset++] & 0xFF;
                int byte18 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte16 & 7) << 16) | (byte17 << 8) | byte18;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 45);
                values[valuesOffset++] = Number.URShift(block0, 26) & 524287L;
                values[valuesOffset++] = Number.URShift(block0, 7) & 524287L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 127L) << 12) | Number.URShift(block1, 52);
                values[valuesOffset++] = Number.URShift(block1, 33) & 524287L;
                values[valuesOffset++] = Number.URShift(block1, 14) & 524287L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 16383L) << 5) | Number.URShift(block2, 59);
                values[valuesOffset++] = Number.URShift(block2, 40) & 524287L;
                values[valuesOffset++] = Number.URShift(block2, 21) & 524287L;
                values[valuesOffset++] = Number.URShift(block2, 2) & 524287L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 3L) << 17) | Number.URShift(block3, 47);
                values[valuesOffset++] = Number.URShift(block3, 28) & 524287L;
                values[valuesOffset++] = Number.URShift(block3, 9) & 524287L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 511L) << 10) | Number.URShift(block4, 54);
                values[valuesOffset++] = Number.URShift(block4, 35) & 524287L;
                values[valuesOffset++] = Number.URShift(block4, 16) & 524287L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 65535L) << 3) | Number.URShift(block5, 61);
                values[valuesOffset++] = Number.URShift(block5, 42) & 524287L;
                values[valuesOffset++] = Number.URShift(block5, 23) & 524287L;
                values[valuesOffset++] = Number.URShift(block5, 4) & 524287L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 15L) << 15) | Number.URShift(block6, 49);
                values[valuesOffset++] = Number.URShift(block6, 30) & 524287L;
                values[valuesOffset++] = Number.URShift(block6, 11) & 524287L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 2047L) << 8) | Number.URShift(block7, 56);
                values[valuesOffset++] = Number.URShift(block7, 37) & 524287L;
                values[valuesOffset++] = Number.URShift(block7, 18) & 524287L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 262143L) << 1) | Number.URShift(block8, 63);
                values[valuesOffset++] = Number.URShift(block8, 44) & 524287L;
                values[valuesOffset++] = Number.URShift(block8, 25) & 524287L;
                values[valuesOffset++] = Number.URShift(block8, 6) & 524287L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 63L) << 13) | Number.URShift(block9, 51);
                values[valuesOffset++] = Number.URShift(block9, 32) & 524287L;
                values[valuesOffset++] = Number.URShift(block9, 13) & 524287L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 8191L) << 6) | Number.URShift(block10, 58);
                values[valuesOffset++] = Number.URShift(block10, 39) & 524287L;
                values[valuesOffset++] = Number.URShift(block10, 20) & 524287L;
                values[valuesOffset++] = Number.URShift(block10, 1) & 524287L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 1L) << 18) | Number.URShift(block11, 46);
                values[valuesOffset++] = Number.URShift(block11, 27) & 524287L;
                values[valuesOffset++] = Number.URShift(block11, 8) & 524287L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 255L) << 11) | Number.URShift(block12, 53);
                values[valuesOffset++] = Number.URShift(block12, 34) & 524287L;
                values[valuesOffset++] = Number.URShift(block12, 15) & 524287L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 32767L) << 4) | Number.URShift(block13, 60);
                values[valuesOffset++] = Number.URShift(block13, 41) & 524287L;
                values[valuesOffset++] = Number.URShift(block13, 22) & 524287L;
                values[valuesOffset++] = Number.URShift(block13, 3) & 524287L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 7L) << 16) | Number.URShift(block14, 48);
                values[valuesOffset++] = Number.URShift(block14, 29) & 524287L;
                values[valuesOffset++] = Number.URShift(block14, 10) & 524287L;
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block14 & 1023L) << 9) | Number.URShift(block15, 55);
                values[valuesOffset++] = Number.URShift(block15, 36) & 524287L;
                values[valuesOffset++] = Number.URShift(block15, 17) & 524287L;
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block15 & 131071L) << 2) | Number.URShift(block16, 62);
                values[valuesOffset++] = Number.URShift(block16, 43) & 524287L;
                values[valuesOffset++] = Number.URShift(block16, 24) & 524287L;
                values[valuesOffset++] = Number.URShift(block16, 5) & 524287L;
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block16 & 31L) << 14) | Number.URShift(block17, 50);
                values[valuesOffset++] = Number.URShift(block17, 31) & 524287L;
                values[valuesOffset++] = Number.URShift(block17, 12) & 524287L;
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block17 & 4095L) << 7) | Number.URShift(block18, 57);
                values[valuesOffset++] = Number.URShift(block18, 38) & 524287L;
                values[valuesOffset++] = Number.URShift(block18, 19) & 524287L;
                values[valuesOffset++] = block18 & 524287L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 11) | (byte1 << 3) | Number.URShift(byte2, 5);
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 31) << 14) | (byte3 << 6) | Number.URShift(byte4, 2);
                long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                long byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 3) << 17) | (byte5 << 9) | (byte6 << 1) | Number.URShift(byte7, 7);
                long byte8 = blocks[blocksOffset++] & 0xFF;
                long byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 127) << 12) | (byte8 << 4) | Number.URShift(byte9, 4);
                long byte10 = blocks[blocksOffset++] & 0xFF;
                long byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 15) << 15) | (byte10 << 7) | Number.URShift(byte11, 1);
                long byte12 = blocks[blocksOffset++] & 0xFF;
                long byte13 = blocks[blocksOffset++] & 0xFF;
                long byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 1) << 18) | (byte12 << 10) | (byte13 << 2) | Number.URShift(byte14, 6);
                long byte15 = blocks[blocksOffset++] & 0xFF;
                long byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 63) << 13) | (byte15 << 5) | Number.URShift(byte16, 3);
                long byte17 = blocks[blocksOffset++] & 0xFF;
                long byte18 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte16 & 7) << 16) | (byte17 << 8) | byte18;
            }
        }

    }

}