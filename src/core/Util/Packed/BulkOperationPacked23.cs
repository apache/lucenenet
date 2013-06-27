using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked23 : BulkOperationPacked
    {

        public BulkOperationPacked23()
            : base(23)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 41);
                values[valuesOffset++] = (int)(Number.URShift(block0, 18) & 8388607L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 262143L) << 5) | Number.URShift(block1, 59));
                values[valuesOffset++] = (int)(Number.URShift(block1, 36) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 13) & 8388607L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 8191L) << 10) | Number.URShift(block2, 54));
                values[valuesOffset++] = (int)(Number.URShift(block2, 31) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 8) & 8388607L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 255L) << 15) | Number.URShift(block3, 49));
                values[valuesOffset++] = (int)(Number.URShift(block3, 26) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 3) & 8388607L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 7L) << 20) | Number.URShift(block4, 44));
                values[valuesOffset++] = (int)(Number.URShift(block4, 21) & 8388607L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 2097151L) << 2) | Number.URShift(block5, 62));
                values[valuesOffset++] = (int)(Number.URShift(block5, 39) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block5, 16) & 8388607L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 65535L) << 7) | Number.URShift(block6, 57));
                values[valuesOffset++] = (int)(Number.URShift(block6, 34) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block6, 11) & 8388607L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 2047L) << 12) | Number.URShift(block7, 52));
                values[valuesOffset++] = (int)(Number.URShift(block7, 29) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block7, 6) & 8388607L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 63L) << 17) | Number.URShift(block8, 47));
                values[valuesOffset++] = (int)(Number.URShift(block8, 24) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block8, 1) & 8388607L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 1L) << 22) | Number.URShift(block9, 42));
                values[valuesOffset++] = (int)(Number.URShift(block9, 19) & 8388607L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 524287L) << 4) | Number.URShift(block10, 60));
                values[valuesOffset++] = (int)(Number.URShift(block10, 37) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block10, 14) & 8388607L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 16383L) << 9) | Number.URShift(block11, 55));
                values[valuesOffset++] = (int)(Number.URShift(block11, 32) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block11, 9) & 8388607L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 511L) << 14) | Number.URShift(block12, 50));
                values[valuesOffset++] = (int)(Number.URShift(block12, 27) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block12, 4) & 8388607L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 15L) << 19) | Number.URShift(block13, 45));
                values[valuesOffset++] = (int)(Number.URShift(block13, 22) & 8388607L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 4194303L) << 1) | Number.URShift(block14, 63));
                values[valuesOffset++] = (int)(Number.URShift(block14, 40) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block14, 17) & 8388607L);
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block14 & 131071L) << 6) | Number.URShift(block15, 58));
                values[valuesOffset++] = (int)(Number.URShift(block15, 35) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block15, 12) & 8388607L);
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block15 & 4095L) << 11) | Number.URShift(block16, 53));
                values[valuesOffset++] = (int)(Number.URShift(block16, 30) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block16, 7) & 8388607L);
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block16 & 127L) << 16) | Number.URShift(block17, 48));
                values[valuesOffset++] = (int)(Number.URShift(block17, 25) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block17, 2) & 8388607L);
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block17 & 3L) << 21) | Number.URShift(block18, 43));
                values[valuesOffset++] = (int)(Number.URShift(block18, 20) & 8388607L);
                long block19 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block18 & 1048575L) << 3) | Number.URShift(block19, 61));
                values[valuesOffset++] = (int)(Number.URShift(block19, 38) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block19, 15) & 8388607L);
                long block20 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block19 & 32767L) << 8) | Number.URShift(block20, 56));
                values[valuesOffset++] = (int)(Number.URShift(block20, 33) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block20, 10) & 8388607L);
                long block21 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block20 & 1023L) << 13) | Number.URShift(block21, 51));
                values[valuesOffset++] = (int)(Number.URShift(block21, 28) & 8388607L);
                values[valuesOffset++] = (int)(Number.URShift(block21, 5) & 8388607L);
                long block22 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block21 & 31L) << 18) | Number.URShift(block22, 46));
                values[valuesOffset++] = (int)(Number.URShift(block22, 23) & 8388607L);
                values[valuesOffset++] = (int)(block22 & 8388607L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 15) | (byte1 << 7) | Number.URShift(byte2, 1);
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 1) << 22) | (byte3 << 14) | (byte4 << 6) | Number.URShift(byte5, 2);
                int byte6 = blocks[blocksOffset++] & 0xFF;
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 3) << 21) | (byte6 << 13) | (byte7 << 5) | Number.URShift(byte8, 3);
                int byte9 = blocks[blocksOffset++] & 0xFF;
                int byte10 = blocks[blocksOffset++] & 0xFF;
                int byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 7) << 20) | (byte9 << 12) | (byte10 << 4) | Number.URShift(byte11, 4);
                int byte12 = blocks[blocksOffset++] & 0xFF;
                int byte13 = blocks[blocksOffset++] & 0xFF;
                int byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 15) << 19) | (byte12 << 11) | (byte13 << 3) | Number.URShift(byte14, 5);
                int byte15 = blocks[blocksOffset++] & 0xFF;
                int byte16 = blocks[blocksOffset++] & 0xFF;
                int byte17 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 31) << 18) | (byte15 << 10) | (byte16 << 2) | Number.URShift(byte17, 6);
                int byte18 = blocks[blocksOffset++] & 0xFF;
                int byte19 = blocks[blocksOffset++] & 0xFF;
                int byte20 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte17 & 63) << 17) | (byte18 << 9) | (byte19 << 1) | Number.URShift(byte20, 7);
                int byte21 = blocks[blocksOffset++] & 0xFF;
                int byte22 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte20 & 127) << 16) | (byte21 << 8) | byte22;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 41);
                values[valuesOffset++] = Number.URShift(block0, 18) & 8388607L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 262143L) << 5) | Number.URShift(block1, 59);
                values[valuesOffset++] = Number.URShift(block1, 36) & 8388607L;
                values[valuesOffset++] = Number.URShift(block1, 13) & 8388607L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 8191L) << 10) | Number.URShift(block2, 54);
                values[valuesOffset++] = Number.URShift(block2, 31) & 8388607L;
                values[valuesOffset++] = Number.URShift(block2, 8) & 8388607L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 255L) << 15) | Number.URShift(block3, 49);
                values[valuesOffset++] = Number.URShift(block3, 26) & 8388607L;
                values[valuesOffset++] = Number.URShift(block3, 3) & 8388607L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 7L) << 20) | Number.URShift(block4, 44);
                values[valuesOffset++] = Number.URShift(block4, 21) & 8388607L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 2097151L) << 2) | Number.URShift(block5, 62);
                values[valuesOffset++] = Number.URShift(block5, 39) & 8388607L;
                values[valuesOffset++] = Number.URShift(block5, 16) & 8388607L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 65535L) << 7) | Number.URShift(block6, 57);
                values[valuesOffset++] = Number.URShift(block6, 34) & 8388607L;
                values[valuesOffset++] = Number.URShift(block6, 11) & 8388607L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 2047L) << 12) | Number.URShift(block7, 52);
                values[valuesOffset++] = Number.URShift(block7, 29) & 8388607L;
                values[valuesOffset++] = Number.URShift(block7, 6) & 8388607L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 63L) << 17) | Number.URShift(block8, 47);
                values[valuesOffset++] = Number.URShift(block8, 24) & 8388607L;
                values[valuesOffset++] = Number.URShift(block8, 1) & 8388607L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 1L) << 22) | Number.URShift(block9, 42);
                values[valuesOffset++] = Number.URShift(block9, 19) & 8388607L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 524287L) << 4) | Number.URShift(block10, 60);
                values[valuesOffset++] = Number.URShift(block10, 37) & 8388607L;
                values[valuesOffset++] = Number.URShift(block10, 14) & 8388607L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 16383L) << 9) | Number.URShift(block11, 55);
                values[valuesOffset++] = Number.URShift(block11, 32) & 8388607L;
                values[valuesOffset++] = Number.URShift(block11, 9) & 8388607L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 511L) << 14) | Number.URShift(block12, 50);
                values[valuesOffset++] = Number.URShift(block12, 27) & 8388607L;
                values[valuesOffset++] = Number.URShift(block12, 4) & 8388607L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 15L) << 19) | Number.URShift(block13, 45);
                values[valuesOffset++] = Number.URShift(block13, 22) & 8388607L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 4194303L) << 1) | Number.URShift(block14, 63);
                values[valuesOffset++] = Number.URShift(block14, 40) & 8388607L;
                values[valuesOffset++] = Number.URShift(block14, 17) & 8388607L;
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block14 & 131071L) << 6) | Number.URShift(block15, 58);
                values[valuesOffset++] = Number.URShift(block15, 35) & 8388607L;
                values[valuesOffset++] = Number.URShift(block15, 12) & 8388607L;
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block15 & 4095L) << 11) | Number.URShift(block16, 53);
                values[valuesOffset++] = Number.URShift(block16, 30) & 8388607L;
                values[valuesOffset++] = Number.URShift(block16, 7) & 8388607L;
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block16 & 127L) << 16) | Number.URShift(block17, 48);
                values[valuesOffset++] = Number.URShift(block17, 25) & 8388607L;
                values[valuesOffset++] = Number.URShift(block17, 2) & 8388607L;
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block17 & 3L) << 21) | Number.URShift(block18, 43);
                values[valuesOffset++] = Number.URShift(block18, 20) & 8388607L;
                long block19 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block18 & 1048575L) << 3) | Number.URShift(block19, 61);
                values[valuesOffset++] = Number.URShift(block19, 38) & 8388607L;
                values[valuesOffset++] = Number.URShift(block19, 15) & 8388607L;
                long block20 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block19 & 32767L) << 8) | Number.URShift(block20, 56);
                values[valuesOffset++] = Number.URShift(block20, 33) & 8388607L;
                values[valuesOffset++] = Number.URShift(block20, 10) & 8388607L;
                long block21 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block20 & 1023L) << 13) | Number.URShift(block21, 51);
                values[valuesOffset++] = Number.URShift(block21, 28) & 8388607L;
                values[valuesOffset++] = Number.URShift(block21, 5) & 8388607L;
                long block22 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block21 & 31L) << 18) | Number.URShift(block22, 46);
                values[valuesOffset++] = Number.URShift(block22, 23) & 8388607L;
                values[valuesOffset++] = block22 & 8388607L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 15) | (byte1 << 7) | Number.URShift(byte2, 1);
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 1) << 22) | (byte3 << 14) | (byte4 << 6) | Number.URShift(byte5, 2);
                long byte6 = blocks[blocksOffset++] & 0xFF;
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 3) << 21) | (byte6 << 13) | (byte7 << 5) | Number.URShift(byte8, 3);
                long byte9 = blocks[blocksOffset++] & 0xFF;
                long byte10 = blocks[blocksOffset++] & 0xFF;
                long byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 7) << 20) | (byte9 << 12) | (byte10 << 4) | Number.URShift(byte11, 4);
                long byte12 = blocks[blocksOffset++] & 0xFF;
                long byte13 = blocks[blocksOffset++] & 0xFF;
                long byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 15) << 19) | (byte12 << 11) | (byte13 << 3) | Number.URShift(byte14, 5);
                long byte15 = blocks[blocksOffset++] & 0xFF;
                long byte16 = blocks[blocksOffset++] & 0xFF;
                long byte17 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 31) << 18) | (byte15 << 10) | (byte16 << 2) | Number.URShift(byte17, 6);
                long byte18 = blocks[blocksOffset++] & 0xFF;
                long byte19 = blocks[blocksOffset++] & 0xFF;
                long byte20 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte17 & 63) << 17) | (byte18 << 9) | (byte19 << 1) | Number.URShift(byte20, 7);
                long byte21 = blocks[blocksOffset++] & 0xFF;
                long byte22 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte20 & 127) << 16) | (byte21 << 8) | byte22;
            }
        }

    }

}