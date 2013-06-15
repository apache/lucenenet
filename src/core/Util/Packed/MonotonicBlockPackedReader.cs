using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public sealed class MonotonicBlockPackedReader
    {
        private readonly int blockShift, blockMask;
        private readonly long valueCount;
        private readonly long[] minValues;
        private readonly float[] averages;
        private readonly PackedInts.IReader[] subReaders;

        public MonotonicBlockPackedReader(IndexInput input, int packedIntsVersion, int blockSize, long valueCount, bool direct)
        {
            AbstractBlockPackedWriter.CheckBlockSize(blockSize);
            this.valueCount = valueCount;
            blockShift = Number.NumberOfTrailingZeros(blockSize);
            blockMask = blockSize - 1;
            int numBlocks = (int)(valueCount / blockSize) + (valueCount % blockSize == 0 ? 0 : 1);
            if ((long)numBlocks * blockSize < valueCount)
            {
                throw new ArgumentException("valueCount is too large for this block size");
            }
            minValues = new long[numBlocks];
            averages = new float[numBlocks];
            subReaders = new PackedInts.Reader[numBlocks];
            for (int i = 0; i < numBlocks; ++i)
            {
                minValues[i] = input.ReadVLong();
                averages[i] = Number.IntBitsToFloat(input.ReadInt());
                int bitsPerValue = input.ReadVInt();
                if (bitsPerValue > 64)
                {
                    throw new System.IO.IOException("Corrupted");
                }
                if (bitsPerValue == 0)
                {
                    subReaders[i] = new PackedInts.NullReader(blockSize);
                }
                else
                {
                    int size = (int)Math.Min(blockSize, valueCount - (long)i * blockSize);
                    if (direct)
                    {
                        long pointer = input.FilePointer;
                        subReaders[i] = PackedInts.GetDirectReaderNoHeader(input, PackedInts.Format.PACKED, packedIntsVersion, size, bitsPerValue);
                        input.Seek(pointer + PackedInts.Format.PACKED.ByteCount(packedIntsVersion, size, bitsPerValue));
                    }
                    else
                    {
                        subReaders[i] = PackedInts.GetReaderNoHeader(input, PackedInts.Format.PACKED, packedIntsVersion, size, bitsPerValue);
                    }
                }
            }
        }

        public long Get(long index)
        {
            //assert index >= 0 && index < valueCount;
            int block = (int) Number.URShift(index, blockShift);
            int idx = (int) (index & blockMask);
            return minValues[block] + (long)(idx * averages[block]) + BlockPackedReaderIterator.ZigZagDecode(subReaders[block].Get(idx));
        }
    }
}
