using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked3 : BulkOperationPacked
    {

        public BulkOperationPacked3()
            : base(3)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 61);
                values[valuesOffset++] = (int)(Number.URShift(block0, 58) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 55) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 52) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 49) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 46) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 43) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 40) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 37) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 34) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 31) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 28) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 25) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 22) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 19) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 16) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 13) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 10) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 7) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 4) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 1) & 7L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1L) << 2) | Number.URShift(block1, 62));
                values[valuesOffset++] = (int)(Number.URShift(block1, 59) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 56) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 53) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 50) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 47) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 44) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 41) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 38) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 35) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 32) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 29) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 26) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 23) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 20) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 17) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 14) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 11) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 8) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 5) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 2) & 7L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 1) | Number.URShift(block2, 63));
                values[valuesOffset++] = (int)(Number.URShift(block2, 60) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 57) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 54) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 51) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 48) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 45) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 42) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 39) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 36) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 33) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 30) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 27) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 24) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 21) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 18) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 15) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 12) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 9) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 6) & 7L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 3) & 7L);
                values[valuesOffset++] = (int)(block2 & 7L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = Number.URShift(byte0, 5);
                values[valuesOffset++] = Number.URShift(byte0, 2) & 7;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 3) << 1) | Number.URShift(byte1, 7);
                values[valuesOffset++] = Number.URShift(byte1, 4) & 7;
                values[valuesOffset++] = Number.URShift(byte1, 1) & 7;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 2) | Number.URShift(byte2, 6);
                values[valuesOffset++] = Number.URShift(byte2, 3) & 7;
                values[valuesOffset++] = byte2 & 7;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 61);
                values[valuesOffset++] = Number.URShift(block0, 58) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 55) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 52) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 49) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 46) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 43) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 40) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 37) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 34) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 31) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 28) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 25) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 22) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 19) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 16) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 13) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 10) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 7) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 4) & 7L;
                values[valuesOffset++] = Number.URShift(block0, 1) & 7L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1L) << 2) | Number.URShift(block1, 62);
                values[valuesOffset++] = Number.URShift(block1, 59) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 56) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 53) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 50) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 47) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 44) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 41) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 38) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 35) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 32) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 29) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 26) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 23) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 20) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 17) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 14) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 11) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 8) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 5) & 7L;
                values[valuesOffset++] = Number.URShift(block1, 2) & 7L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 1) | Number.URShift(block2, 63);
                values[valuesOffset++] = Number.URShift(block2, 60) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 57) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 54) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 51) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 48) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 45) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 42) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 39) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 36) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 33) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 30) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 27) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 24) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 21) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 18) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 15) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 12) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 9) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 6) & 7L;
                values[valuesOffset++] = Number.URShift(block2, 3) & 7L;
                values[valuesOffset++] = block2 & 7L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = Number.URShift(byte0, 5);
                values[valuesOffset++] = Number.URShift(byte0, 2) & 7;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 3) << 1) | Number.URShift(byte1, 7);
                values[valuesOffset++] = Number.URShift(byte1, 4) & 7;
                values[valuesOffset++] = Number.URShift(byte1, 1) & 7;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 2) | Number.URShift(byte2, 6);
                values[valuesOffset++] = Number.URShift(byte2, 3) & 7;
                values[valuesOffset++] = byte2 & 7;
            }
        }

    }

}