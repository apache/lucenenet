using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked2 : BulkOperationPacked
    {

        public BulkOperationPacked2()
            : base(2)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                for (int shift = 62; shift >= 0; shift -= 2)
                {
                    values[valuesOffset++] = (int)(Number.URShift(block, shift) & 3);
                }
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int j = 0; j < iterations; ++j)
            {
                sbyte block = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block, 6) & 3;
                values[valuesOffset++] = Number.URShift(block, 4) & 3;
                values[valuesOffset++] = Number.URShift(block, 2) & 3;
                values[valuesOffset++] = block & 3;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                for (int shift = 62; shift >= 0; shift -= 2)
                {
                    values[valuesOffset++] = Number.URShift(block, shift) & 3;
                }
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int j = 0; j < iterations; ++j)
            {
                sbyte block = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block, 6) & 3;
                values[valuesOffset++] = Number.URShift(block, 4) & 3;
                values[valuesOffset++] = Number.URShift(block, 2) & 3;
                values[valuesOffset++] = block & 3;
            }
        }

    }

}