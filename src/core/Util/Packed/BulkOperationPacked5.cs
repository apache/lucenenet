using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked5 : BulkOperationPacked
    {

        public BulkOperationPacked5()
            : base(5)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 59);
                values[valuesOffset++] = (int)(Number.URShift(block0, 54) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 49) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 44) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 39) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 34) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 29) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 24) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 19) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 14) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 9) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 4) & 31L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 1) | Number.URShift(block1, 63));
                values[valuesOffset++] = (int)(Number.URShift(block1, 58) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 53) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 48) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 43) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 38) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 33) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 28) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 23) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 18) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 13) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 8) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 3) & 31L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 7L) << 2) | Number.URShift(block2, 62));
                values[valuesOffset++] = (int)(Number.URShift(block2, 57) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 52) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 47) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 42) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 37) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 32) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 27) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 22) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 17) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 12) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 7) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 2) & 31L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 3L) << 3) | Number.URShift(block3, 61));
                values[valuesOffset++] = (int)(Number.URShift(block3, 56) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 51) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 46) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 41) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 36) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 31) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 26) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 21) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 16) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 11) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 6) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 1) & 31L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 1L) << 4) | Number.URShift(block4, 60));
                values[valuesOffset++] = (int)(Number.URShift(block4, 55) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 50) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 45) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 40) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 35) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 30) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 25) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 20) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 15) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 10) & 31L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 5) & 31L);
                values[valuesOffset++] = (int)(block4 & 31L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = Number.URShift(byte0, 3);
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 7) << 2) | Number.URShift(byte1, 6);
                values[valuesOffset++] = Number.URShift(byte1, 1) & 31;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 4) | Number.URShift(byte2, 4);
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 15) << 1) | Number.URShift(byte3, 7);
                values[valuesOffset++] = Number.URShift(byte3, 2) & 31;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 3) | Number.URShift(byte4, 5);
                values[valuesOffset++] = byte4 & 31;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 59);
                values[valuesOffset++] = Number.URShift(block0, 54) & 31L;
                values[valuesOffset++] = Number.URShift(block0, 49) & 31L;
                values[valuesOffset++] = Number.URShift(block0, 44) & 31L;
                values[valuesOffset++] = Number.URShift(block0, 39) & 31L;
                values[valuesOffset++] = Number.URShift(block0, 34) & 31L;
                values[valuesOffset++] = Number.URShift(block0, 29) & 31L;
                values[valuesOffset++] = Number.URShift(block0, 24) & 31L;
                values[valuesOffset++] = Number.URShift(block0, 19) & 31L;
                values[valuesOffset++] = Number.URShift(block0, 14) & 31L;
                values[valuesOffset++] = Number.URShift(block0, 9) & 31L;
                values[valuesOffset++] = Number.URShift(block0, 4) & 31L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 1) | Number.URShift(block1, 63);
                values[valuesOffset++] = Number.URShift(block1, 58) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 53) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 48) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 43) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 38) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 33) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 28) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 23) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 18) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 13) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 8) & 31L;
                values[valuesOffset++] = Number.URShift(block1, 3) & 31L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 7L) << 2) | Number.URShift(block2, 62);
                values[valuesOffset++] = Number.URShift(block2, 57) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 52) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 47) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 42) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 37) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 32) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 27) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 22) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 17) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 12) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 7) & 31L;
                values[valuesOffset++] = Number.URShift(block2, 2) & 31L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 3L) << 3) | Number.URShift(block3, 61);
                values[valuesOffset++] = Number.URShift(block3, 56) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 51) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 46) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 41) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 36) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 31) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 26) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 21) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 16) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 11) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 6) & 31L;
                values[valuesOffset++] = Number.URShift(block3, 1) & 31L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 1L) << 4) | Number.URShift(block4, 60);
                values[valuesOffset++] = Number.URShift(block4, 55) & 31L;
                values[valuesOffset++] = Number.URShift(block4, 50) & 31L;
                values[valuesOffset++] = Number.URShift(block4, 45) & 31L;
                values[valuesOffset++] = Number.URShift(block4, 40) & 31L;
                values[valuesOffset++] = Number.URShift(block4, 35) & 31L;
                values[valuesOffset++] = Number.URShift(block4, 30) & 31L;
                values[valuesOffset++] = Number.URShift(block4, 25) & 31L;
                values[valuesOffset++] = Number.URShift(block4, 20) & 31L;
                values[valuesOffset++] = Number.URShift(block4, 15) & 31L;
                values[valuesOffset++] = Number.URShift(block4, 10) & 31L;
                values[valuesOffset++] = Number.URShift(block4, 5) & 31L;
                values[valuesOffset++] = block4 & 31L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = Number.URShift(byte0, 3);
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 7) << 2) | Number.URShift(byte1, 6);
                values[valuesOffset++] = Number.URShift(byte1, 1) & 31;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 4) | Number.URShift(byte2, 4);
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 15) << 1) | Number.URShift(byte3, 7);
                values[valuesOffset++] = Number.URShift(byte3, 2) & 31;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 3) | Number.URShift(byte4, 5);
                values[valuesOffset++] = byte4 & 31;
            }
        }

    }

}