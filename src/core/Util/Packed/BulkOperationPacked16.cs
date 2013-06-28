using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked16 : BulkOperationPacked
    {

        public BulkOperationPacked16()
            : base(16)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                for (int shift = 48; shift >= 0; shift -= 16)
                {
                    values[valuesOffset++] = (int)(Number.URShift(block, shift) & 65535);
                }
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int j = 0; j < iterations; ++j)
            {
                values[valuesOffset++] = ((blocks[blocksOffset++] & 0xFF) << 8) | (blocks[blocksOffset++] & 0xFF);
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                for (int shift = 48; shift >= 0; shift -= 16)
                {
                    values[valuesOffset++] = Number.URShift(block, shift) & 65535;
                }
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int j = 0; j < iterations; ++j)
            {
                values[valuesOffset++] = ((blocks[blocksOffset++] & 0xFFL) << 8) | (blocks[blocksOffset++] & 0xFFL);
            }
        }

    }

}