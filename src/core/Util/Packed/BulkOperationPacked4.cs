using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked4 : BulkOperationPacked
    {

        public BulkOperationPacked4()
            : base(4)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                for (int shift = 60; shift >= 0; shift -= 4)
                {
                    values[valuesOffset++] = (int)(Number.URShift(block, shift) & 15);
                }
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int j = 0; j < iterations; ++j)
            {
                sbyte block = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block, 4) & 15;
                values[valuesOffset++] = block & 15;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                for (int shift = 60; shift >= 0; shift -= 4)
                {
                    values[valuesOffset++] = Number.URShift(block, shift) & 15;
                }
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int j = 0; j < iterations; ++j)
            {
                sbyte block = blocks[blocksOffset++];
                values[valuesOffset++] = Number.URShift(block, 4) & 15;
                values[valuesOffset++] = block & 15;
            }
        }

    }

}