using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class ByteBlockPool
    {
        public const int BYTE_BLOCK_SHIFT = 15;
        public const int BYTE_BLOCK_SIZE = 1 << BYTE_BLOCK_SHIFT;
        public const int BYTE_BLOCK_MASK = BYTE_BLOCK_SIZE - 1;

        public abstract class Allocator
        {
            protected readonly int blockSize;

            public Allocator(int blockSize)
            {
                this.blockSize = blockSize;
            }

            public abstract void RecycleByteBlocks(byte[][] blocks, int start, int end);

            public virtual void RecycleByteBlocks(List<byte[]> blocks)
            {
                byte[][] b = blocks.ToArray();
                RecycleByteBlocks(b, 0, b.Length);
            }

            public virtual byte[] ByteBlock
            {
                get
                {
                    return new byte[blockSize];
                }
            }
        }

        public sealed class DirectAllocator : Allocator
        {
            public DirectAllocator()
                : this(BYTE_BLOCK_SIZE)
            {
            }

            public DirectAllocator(int blockSize)
                : base(blockSize)
            {
            }

            public override void RecycleByteBlocks(byte[][] blocks, int start, int end)
            {
            }
        }

        public class DirectTrackingAllocator : Allocator
        {
            private readonly Counter bytesUsed;

            public DirectTrackingAllocator(Counter bytesUsed)
                : this(BYTE_BLOCK_SIZE, bytesUsed)
            {
            }

            public DirectTrackingAllocator(int blockSize, Counter bytesUsed)
                : base(blockSize)
            {
                this.bytesUsed = bytesUsed;
            }

            public override byte[] ByteBlock
            {
                get
                {
                    bytesUsed.AddAndGet(blockSize);
                    return new byte[blockSize];
                }
            }

            public override void RecycleByteBlocks(byte[][] blocks, int start, int end)
            {
                bytesUsed.AddAndGet(-((end - start) * blockSize));
                for (int i = start; i < end; i++)
                {
                    blocks[i] = null;
                }
            }
        }

        public byte[][] buffers = new byte[10][];

        private int bufferUpto = -1;

        private int byteUpto = BYTE_BLOCK_SIZE;

        public byte[] buffer;

        public int byteOffset = -BYTE_BLOCK_SIZE;

        private readonly Allocator allocator;

        public ByteBlockPool(Allocator allocator)
        {
            this.allocator = allocator;
        }

        public void Reset()
        {
            Reset(true, true);
        }

        public void Reset(bool zeroFillBuffers, bool reuseFirst)
        {
            if (bufferUpto != -1)
            {
                // We allocated at least one buffer

                if (zeroFillBuffers)
                {
                    for (int i = 0; i < bufferUpto; i++)
                    {
                        // Fully zero fill buffers that we fully used
                        Arrays.Fill(buffers[i], (byte)0);
                    }
                    // Partial zero fill the final buffer
                    Arrays.Fill(buffers[bufferUpto], 0, byteUpto, (byte)0);
                }

                if (bufferUpto > 0 || !reuseFirst)
                {
                    int offset = reuseFirst ? 1 : 0;
                    // Recycle all but the first buffer
                    allocator.RecycleByteBlocks(buffers, offset, 1 + bufferUpto);
                    Arrays.Fill(buffers, offset, 1 + bufferUpto, null);
                }
                if (reuseFirst)
                {
                    // Re-use the first buffer
                    bufferUpto = 0;
                    byteUpto = 0;
                    byteOffset = 0;
                    buffer = buffers[0];
                }
                else
                {
                    bufferUpto = -1;
                    byteUpto = BYTE_BLOCK_SIZE;
                    byteOffset = -BYTE_BLOCK_SIZE;
                    buffer = null;
                }
            }
        }

        public void NextBuffer()
        {
            if (1 + bufferUpto == buffers.Length)
            {
                byte[][] newBuffers = new byte[ArrayUtil.Oversize(buffers.Length + 1,
                                                                  RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
                Array.Copy(buffers, 0, newBuffers, 0, buffers.Length);
                buffers = newBuffers;
            }
            buffer = buffers[1 + bufferUpto] = allocator.ByteBlock;
            bufferUpto++;

            byteUpto = 0;
            byteOffset += BYTE_BLOCK_SIZE;
        }

        public int NewSlice(int size)
        {
            if (byteUpto > BYTE_BLOCK_SIZE - size)
                NextBuffer();
            int upto = byteUpto;
            byteUpto += size;
            buffer[byteUpto - 1] = 16;
            return upto;
        }
    }
}
