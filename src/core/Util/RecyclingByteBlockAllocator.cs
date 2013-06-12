using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class RecyclingByteBlockAllocator : ByteBlockPool.Allocator
    {
        private byte[][] freeByteBlocks;
        private readonly int maxBufferedBlocks;
        private int freeBlocks = 0;
        private readonly Counter bytesUsed;
        public const int DEFAULT_BUFFERED_BLOCKS = 64;

        public RecyclingByteBlockAllocator(int blockSize, int maxBufferedBlocks,
            Counter bytesUsed)
            : base(blockSize)
        {
            freeByteBlocks = new byte[maxBufferedBlocks][];
            this.maxBufferedBlocks = maxBufferedBlocks;
            this.bytesUsed = bytesUsed;
        }

        public RecyclingByteBlockAllocator(int blockSize, int maxBufferedBlocks)
            : this(blockSize, maxBufferedBlocks, Counter.NewCounter(false))
        {
        }

        public RecyclingByteBlockAllocator()
            : this(ByteBlockPool.BYTE_BLOCK_SIZE, 64, Counter.NewCounter(false))
        {
        }

        public override byte[] ByteBlock
        {
            get
            {
                if (freeBlocks == 0)
                {
                    bytesUsed.AddAndGet(blockSize);
                    return new byte[blockSize];
                }
                byte[] b = freeByteBlocks[--freeBlocks];
                freeByteBlocks[freeBlocks] = null;
                return b;
            }
        }

        public override void RecycleByteBlocks(byte[][] blocks, int start, int end)
        {
            int numBlocks = Math.Min(maxBufferedBlocks - freeBlocks, end - start);
            int size = freeBlocks + numBlocks;
            if (size >= freeByteBlocks.Length)
            {
                byte[][] newBlocks = new byte[ArrayUtil.Oversize(size,
                    RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
                Array.Copy(freeByteBlocks, 0, newBlocks, 0, freeBlocks);
                freeByteBlocks = newBlocks;
            }
            int stop = start + numBlocks;
            for (int i = start; i < stop; i++)
            {
                freeByteBlocks[freeBlocks++] = blocks[i];
                blocks[i] = null;
            }
            for (int i = stop; i < end; i++)
            {
                blocks[i] = null;
            }
            bytesUsed.AddAndGet(-(end - stop) * blockSize);
            //assert bytesUsed.get() >= 0;
        }

        public int NumBufferedBlocks
        {
            get { return freeBlocks; }
        }

        public long BytesUsed
        {
            get { return bytesUsed.Get(); }
        }

        public int MaxBufferedBlocks
        {
            get { return maxBufferedBlocks; }
        }

        public int FreeBlocks(int num)
        {
            //assert num >= 0 : "free blocks must be >= 0 but was: "+ num;
            int stop;
            int count;
            if (num > freeBlocks)
            {
                stop = 0;
                count = freeBlocks;
            }
            else
            {
                stop = freeBlocks - num;
                count = num;
            }
            while (freeBlocks > stop)
            {
                freeByteBlocks[--freeBlocks] = null;
            }
            bytesUsed.AddAndGet(-count * blockSize);
            //assert bytesUsed.get() >= 0;
            return count;
        }
    }
}
