using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked21 : BulkOperationPacked
    {

        public BulkOperationPacked21()
            : base(21)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 43);
                values[valuesOffset++] = (int)(Number.URShift(block0, 22) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 1) & 2097151L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1L) << 20) | Number.URShift(block1, 44));
                values[valuesOffset++] = (int)(Number.URShift(block1, 23) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 2) & 2097151L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 19) | Number.URShift(block2, 45));
                values[valuesOffset++] = (int)(Number.URShift(block2, 24) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 3) & 2097151L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 7L) << 18) | Number.URShift(block3, 46));
                values[valuesOffset++] = (int)(Number.URShift(block3, 25) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 4) & 2097151L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 15L) << 17) | Number.URShift(block4, 47));
                values[valuesOffset++] = (int)(Number.URShift(block4, 26) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 5) & 2097151L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 31L) << 16) | Number.URShift(block5, 48));
                values[valuesOffset++] = (int)(Number.URShift(block5, 27) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 6) & 2097151L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 63L) << 15) | Number.URShift(block6, 49));
                values[valuesOffset++] = (int)(Number.URShift(block6, 28) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block6, 7) & 2097151L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 127L) << 14) | Number.URShift(block7, 50));
                values[valuesOffset++] = (int)(Number.URShift(block7, 29) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block7, 8) & 2097151L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 255L) << 13) | Number.URShift(block8, 51));
                values[valuesOffset++] = (int)(Number.URShift(block8, 30) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block8, 9) & 2097151L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 511L) << 12) | Number.URShift(block9, 52));
                values[valuesOffset++] = (int)(Number.URShift(block9, 31) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block9, 10) & 2097151L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 1023L) << 11) | Number.URShift(block10, 53));
                values[valuesOffset++] = (int)(Number.URShift(block10, 32) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block10, 11) & 2097151L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 2047L) << 10) | Number.URShift(block11, 54));
                values[valuesOffset++] = (int)(Number.URShift(block11, 33) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block11, 12) & 2097151L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 4095L) << 9) | Number.URShift(block12, 55));
                values[valuesOffset++] = (int)(Number.URShift(block12, 34) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block12, 13) & 2097151L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 8191L) << 8) | Number.URShift(block13, 56));
                values[valuesOffset++] = (int)(Number.URShift(block13, 35) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block13, 14) & 2097151L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 16383L) << 7) | Number.URShift(block14, 57));
                values[valuesOffset++] = (int)(Number.URShift(block14, 36) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block14, 15) & 2097151L);
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block14 & 32767L) << 6) | Number.URShift(block15, 58));
                values[valuesOffset++] = (int)(Number.URShift(block15, 37) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block15, 16) & 2097151L);
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block15 & 65535L) << 5) | Number.URShift(block16, 59));
                values[valuesOffset++] = (int)(Number.URShift(block16, 38) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block16, 17) & 2097151L);
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block16 & 131071L) << 4) | Number.URShift(block17, 60));
                values[valuesOffset++] = (int)(Number.URShift(block17, 39) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block17, 18) & 2097151L);
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block17 & 262143L) << 3) | Number.URShift(block18, 61));
                values[valuesOffset++] = (int)(Number.URShift(block18, 40) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block18, 19) & 2097151L);
                long block19 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block18 & 524287L) << 2) | Number.URShift(block19, 62));
                values[valuesOffset++] = (int)(Number.URShift(block19, 41) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block19, 20) & 2097151L);
                long block20 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block19 & 1048575L) << 1) | Number.URShift(block20, 63));
                values[valuesOffset++] = (int)(Number.URShift(block20, 42) & 2097151L);
                values[valuesOffset++] = (int)(Number.URShift(block20, 21) & 2097151L);
                values[valuesOffset++] = (int)(block20 & 2097151L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 13) | (byte1 << 5) | Number.URShift(byte2, 3);
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 7) << 18) | (byte3 << 10) | (byte4 << 2) | Number.URShift(byte5, 6);
                int byte6 = blocks[blocksOffset++] & 0xFF;
                int byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 63) << 15) | (byte6 << 7) | Number.URShift(byte7, 1);
                int byte8 = blocks[blocksOffset++] & 0xFF;
                int byte9 = blocks[blocksOffset++] & 0xFF;
                int byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 1) << 20) | (byte8 << 12) | (byte9 << 4) | Number.URShift(byte10, 4);
                int byte11 = blocks[blocksOffset++] & 0xFF;
                int byte12 = blocks[blocksOffset++] & 0xFF;
                int byte13 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte10 & 15) << 17) | (byte11 << 9) | (byte12 << 1) | Number.URShift(byte13, 7);
                int byte14 = blocks[blocksOffset++] & 0xFF;
                int byte15 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte13 & 127) << 14) | (byte14 << 6) | Number.URShift(byte15, 2);
                int byte16 = blocks[blocksOffset++] & 0xFF;
                int byte17 = blocks[blocksOffset++] & 0xFF;
                int byte18 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte15 & 3) << 19) | (byte16 << 11) | (byte17 << 3) | Number.URShift(byte18, 5);
                int byte19 = blocks[blocksOffset++] & 0xFF;
                int byte20 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte18 & 31) << 16) | (byte19 << 8) | byte20;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 43);
                values[valuesOffset++] = Number.URShift(block0, 22) & 2097151L;
                values[valuesOffset++] = Number.URShift(block0, 1) & 2097151L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1L) << 20) | Number.URShift(block1, 44);
                values[valuesOffset++] = Number.URShift(block1, 23) & 2097151L;
                values[valuesOffset++] = Number.URShift(block1, 2) & 2097151L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 19) | Number.URShift(block2, 45);
                values[valuesOffset++] = Number.URShift(block2, 24) & 2097151L;
                values[valuesOffset++] = Number.URShift(block2, 3) & 2097151L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 7L) << 18) | Number.URShift(block3, 46);
                values[valuesOffset++] = Number.URShift(block3, 25) & 2097151L;
                values[valuesOffset++] = Number.URShift(block3, 4) & 2097151L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 15L) << 17) | Number.URShift(block4, 47);
                values[valuesOffset++] = Number.URShift(block4, 26) & 2097151L;
                values[valuesOffset++] = Number.URShift(block4, 5) & 2097151L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 31L) << 16) | Number.URShift(block5, 48);
                values[valuesOffset++] = Number.URShift(block5, 27) & 2097151L;
                values[valuesOffset++] = Number.URShift(block5, 6) & 2097151L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 63L) << 15) | Number.URShift(block6, 49);
                values[valuesOffset++] = Number.URShift(block6, 28) & 2097151L;
                values[valuesOffset++] = Number.URShift(block6, 7) & 2097151L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 127L) << 14) | Number.URShift(block7, 50);
                values[valuesOffset++] = Number.URShift(block7, 29) & 2097151L;
                values[valuesOffset++] = Number.URShift(block7, 8) & 2097151L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 255L) << 13) | Number.URShift(block8, 51);
                values[valuesOffset++] = Number.URShift(block8, 30) & 2097151L;
                values[valuesOffset++] = Number.URShift(block8, 9) & 2097151L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 511L) << 12) | Number.URShift(block9, 52);
                values[valuesOffset++] = Number.URShift(block9, 31) & 2097151L;
                values[valuesOffset++] = Number.URShift(block9, 10) & 2097151L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 1023L) << 11) | Number.URShift(block10, 53);
                values[valuesOffset++] = Number.URShift(block10, 32) & 2097151L;
                values[valuesOffset++] = Number.URShift(block10, 11) & 2097151L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 2047L) << 10) | Number.URShift(block11, 54);
                values[valuesOffset++] = Number.URShift(block11, 33) & 2097151L;
                values[valuesOffset++] = Number.URShift(block11, 12) & 2097151L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 4095L) << 9) | Number.URShift(block12, 55);
                values[valuesOffset++] = Number.URShift(block12, 34) & 2097151L;
                values[valuesOffset++] = Number.URShift(block12, 13) & 2097151L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 8191L) << 8) | Number.URShift(block13, 56);
                values[valuesOffset++] = Number.URShift(block13, 35) & 2097151L;
                values[valuesOffset++] = Number.URShift(block13, 14) & 2097151L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 16383L) << 7) | Number.URShift(block14, 57);
                values[valuesOffset++] = Number.URShift(block14, 36) & 2097151L;
                values[valuesOffset++] = Number.URShift(block14, 15) & 2097151L;
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block14 & 32767L) << 6) | Number.URShift(block15, 58);
                values[valuesOffset++] = Number.URShift(block15, 37) & 2097151L;
                values[valuesOffset++] = Number.URShift(block15, 16) & 2097151L;
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block15 & 65535L) << 5) | Number.URShift(block16, 59);
                values[valuesOffset++] = Number.URShift(block16, 38) & 2097151L;
                values[valuesOffset++] = Number.URShift(block16, 17) & 2097151L;
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block16 & 131071L) << 4) | Number.URShift(block17, 60);
                values[valuesOffset++] = Number.URShift(block17, 39) & 2097151L;
                values[valuesOffset++] = Number.URShift(block17, 18) & 2097151L;
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block17 & 262143L) << 3) | Number.URShift(block18, 61);
                values[valuesOffset++] = Number.URShift(block18, 40) & 2097151L;
                values[valuesOffset++] = Number.URShift(block18, 19) & 2097151L;
                long block19 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block18 & 524287L) << 2) | Number.URShift(block19, 62);
                values[valuesOffset++] = Number.URShift(block19, 41) & 2097151L;
                values[valuesOffset++] = Number.URShift(block19, 20) & 2097151L;
                long block20 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block19 & 1048575L) << 1) | Number.URShift(block20, 63);
                values[valuesOffset++] = Number.URShift(block20, 42) & 2097151L;
                values[valuesOffset++] = Number.URShift(block20, 21) & 2097151L;
                values[valuesOffset++] = block20 & 2097151L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 13) | (byte1 << 5) | Number.URShift(byte2, 3);
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 7) << 18) | (byte3 << 10) | (byte4 << 2) | Number.URShift(byte5, 6);
                long byte6 = blocks[blocksOffset++] & 0xFF;
                long byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 63) << 15) | (byte6 << 7) | Number.URShift(byte7, 1);
                long byte8 = blocks[blocksOffset++] & 0xFF;
                long byte9 = blocks[blocksOffset++] & 0xFF;
                long byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 1) << 20) | (byte8 << 12) | (byte9 << 4) | Number.URShift(byte10, 4);
                long byte11 = blocks[blocksOffset++] & 0xFF;
                long byte12 = blocks[blocksOffset++] & 0xFF;
                long byte13 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte10 & 15) << 17) | (byte11 << 9) | (byte12 << 1) | Number.URShift(byte13, 7);
                long byte14 = blocks[blocksOffset++] & 0xFF;
                long byte15 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte13 & 127) << 14) | (byte14 << 6) | Number.URShift(byte15, 2);
                long byte16 = blocks[blocksOffset++] & 0xFF;
                long byte17 = blocks[blocksOffset++] & 0xFF;
                long byte18 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte15 & 3) << 19) | (byte16 << 11) | (byte17 << 3) | Number.URShift(byte18, 5);
                long byte19 = blocks[blocksOffset++] & 0xFF;
                long byte20 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte18 & 31) << 16) | (byte19 << 8) | byte20;
            }
        }

    }

}