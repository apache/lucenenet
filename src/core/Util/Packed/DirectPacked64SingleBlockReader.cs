using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class DirectPacked64SingleBlockReader : PackedInts.Reader
    {
        private readonly IndexInput input;
        private readonly long startPointer;
        private readonly int valuesPerBlock;
        private readonly long mask;

        public DirectPacked64SingleBlockReader(int bitsPerValue, int valueCount, IndexInput input)
            : base(valueCount, bitsPerValue)
        {
            this.input = input;
            startPointer = input.FilePointer;
            valuesPerBlock = 64 / bitsPerValue;
            mask = ~(~0L << bitsPerValue);
        }

        public override long Get(int index)
        {
            int blockOffset = index / valuesPerBlock;
            long skip = ((long)blockOffset) << 3;
            try
            {
                input.Seek(startPointer + skip);

                long block = input.ReadLong();
                int offsetInBlock = index % valuesPerBlock;
                return Number.URShift(block, (offsetInBlock * bitsPerValue)) & mask;
            }
            catch (System.IO.IOException e)
            {
                throw new InvalidOperationException("failed", e);
            }
        }

        public override long RamBytesUsed()
        {
            return 0;
        }
    }
}
