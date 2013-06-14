using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class BulkOperationPacked1 : BulkOperationPacked
    {
        public BulkOperationPacked1()
            : base(1)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                for (int shift = 63; shift >= 0; shift -= 1)
                {
                    values[valuesOffset++] = (int)(Number.URShift(block, shift) & 1);
                }
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int j = 0; j < iterations; ++j)
            {
                // .NET Port: Casting to byte for shifting below, rather than the more expensive Number.URShift
                byte block = (byte)blocks[blocksOffset++];
                values[valuesOffset++] = (block >> 7) & 1;
                values[valuesOffset++] = (block >> 6) & 1;
                values[valuesOffset++] = (block >> 5) & 1;
                values[valuesOffset++] = (block >> 4) & 1;
                values[valuesOffset++] = (block >> 3) & 1;
                values[valuesOffset++] = (block >> 2) & 1;
                values[valuesOffset++] = (block >> 1) & 1;
                values[valuesOffset++] = block & 1;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block = blocks[blocksOffset++];
                for (int shift = 63; shift >= 0; shift -= 1)
                {
                    values[valuesOffset++] = Number.URShift(block, shift) & 1;
                }
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int j = 0; j < iterations; ++j)
            {
                // .NET Port: Casting to byte for shifting below, rather than the more expensive Number.URShift
                byte block = (byte)blocks[blocksOffset++];
                values[valuesOffset++] = (block >> 7) & 1;
                values[valuesOffset++] = (block >> 6) & 1;
                values[valuesOffset++] = (block >> 5) & 1;
                values[valuesOffset++] = (block >> 4) & 1;
                values[valuesOffset++] = (block >> 3) & 1;
                values[valuesOffset++] = (block >> 2) & 1;
                values[valuesOffset++] = (block >> 1) & 1;
                values[valuesOffset++] = block & 1;
            }
        }
    }
}
