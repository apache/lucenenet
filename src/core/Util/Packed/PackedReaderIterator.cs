using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class PackedReaderIterator : PackedInts.ReaderIterator
    {
        internal readonly int packedIntsVersion;
        internal readonly PackedInts.Format format;
        internal readonly BulkOperation bulkOperation;
        internal readonly sbyte[] nextBlocks;
        internal readonly LongsRef nextValues;
        internal readonly int iterations;
        internal int position;

        public PackedReaderIterator(PackedInts.Format format, int packedIntsVersion, int valueCount, int bitsPerValue, DataInput input, int mem)
            : base(valueCount, bitsPerValue, input)
        {
            this.format = format;
            this.packedIntsVersion = packedIntsVersion;
            bulkOperation = BulkOperation.Of(format, bitsPerValue);
            iterations = Iterations(mem);
            //assert valueCount == 0 || iterations > 0;
            nextBlocks = new sbyte[iterations * bulkOperation.ByteBlockCount];
            nextValues = new LongsRef(new long[iterations * bulkOperation.ByteValueCount], 0, 0);
            nextValues.offset = nextValues.longs.Length;
            position = -1;
        }

        private int Iterations(int mem)
        {
            int iterations = bulkOperation.ComputeIterations(valueCount, mem);
            if (packedIntsVersion < PackedInts.VERSION_BYTE_ALIGNED)
            {
                // make sure iterations is a multiple of 8
                iterations = (int)((iterations + 7) & 0xFFFFFFF8);
            }
            return iterations;
        }

        public override LongsRef Next(int count)
        {
            //assert nextValues.length >= 0;
            //assert count > 0;
            //assert nextValues.offset + nextValues.length <= nextValues.longs.length;

            nextValues.offset += nextValues.length;

            int remaining = valueCount - position - 1;
            if (remaining <= 0)
            {
                throw new System.IO.EndOfStreamException();
            }
            count = Math.Min(remaining, count);

            if (nextValues.offset == nextValues.longs.Length)
            {
                long remainingBlocks = format.ByteCount(packedIntsVersion, remaining, bitsPerValue);
                int blocksToRead = (int)Math.Min(remainingBlocks, nextBlocks.Length);
                input.ReadBytes(nextBlocks, 0, blocksToRead);
                if (blocksToRead < nextBlocks.Length)
                {
                    Arrays.Fill(nextBlocks, blocksToRead, nextBlocks.Length, (sbyte)0);
                }

                bulkOperation.Decode(nextBlocks, 0, nextValues.longs, 0, iterations);
                nextValues.offset = 0;
            }

            nextValues.length = Math.Min(nextValues.longs.Length - nextValues.offset, count);
            position += nextValues.length;
            return nextValues;
        }

        public override int Ord()
        {
            return position;
        }
    }
}
