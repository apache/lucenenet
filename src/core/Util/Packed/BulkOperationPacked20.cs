using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked20 : BulkOperationPacked
    {

        public BulkOperationPacked20()
            : base(20)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)Number.URShift(block0, 44);
                values[valuesOffset++] = (int)(Number.URShift(block0, 24) & 1048575L);
                values[valuesOffset++] = (int)(Number.URShift(block0, 4) & 1048575L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 16) | Number.URShift(block1, 48));
                values[valuesOffset++] = (int)(Number.URShift(block1, 28) & 1048575L);
                values[valuesOffset++] = (int)(Number.URShift(block1, 8) & 1048575L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 255L) << 12) | Number.URShift(block2, 52));
                values[valuesOffset++] = (int)(Number.URShift(block2, 32) & 1048575L);
                values[valuesOffset++] = (int)(Number.URShift(block2, 12) & 1048575L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 4095L) << 8) | Number.URShift(block3, 56));
                values[valuesOffset++] = (int)(Number.URShift(block3, 36) & 1048575L);
                values[valuesOffset++] = (int)(Number.URShift(block3, 16) & 1048575L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 65535L) << 4) | Number.URShift(block4, 60));
                values[valuesOffset++] = (int)(Number.URShift(block4, 40) & 1048575L);
                values[valuesOffset++] = (int)(Number.URShift(block4, 20) & 1048575L);
                values[valuesOffset++] = (int)(block4 & 1048575L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 12) | (byte1 << 4) | Number.URShift(byte2, 4);
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 15) << 16) | (byte3 << 8) | byte4;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block0, 44);
                values[valuesOffset++] = Number.URShift(block0, 24) & 1048575L;
                values[valuesOffset++] = Number.URShift(block0, 4) & 1048575L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 16) | Number.URShift(block1, 48);
                values[valuesOffset++] = Number.URShift(block1, 28) & 1048575L;
                values[valuesOffset++] = Number.URShift(block1, 8) & 1048575L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 255L) << 12) | Number.URShift(block2, 52);
                values[valuesOffset++] = Number.URShift(block2, 32) & 1048575L;
                values[valuesOffset++] = Number.URShift(block2, 12) & 1048575L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 4095L) << 8) | Number.URShift(block3, 56);
                values[valuesOffset++] = Number.URShift(block3, 36) & 1048575L;
                values[valuesOffset++] = Number.URShift(block3, 16) & 1048575L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 65535L) << 4) | Number.URShift(block4, 60);
                values[valuesOffset++] = Number.URShift(block4, 40) & 1048575L;
                values[valuesOffset++] = Number.URShift(block4, 20) & 1048575L;
                values[valuesOffset++] = block4 & 1048575L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 12) | (byte1 << 4) | Number.URShift(byte2, 4);
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 15) << 16) | (byte3 << 8) | byte4;
            }
        }

    }

}