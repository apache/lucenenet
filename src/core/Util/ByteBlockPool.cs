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

            public abstract void RecycleByteBlocks(sbyte[][] blocks, int start, int end);

            public virtual void RecycleByteBlocks(List<sbyte[]> blocks)
            {
                sbyte[][] b = blocks.ToArray();
                RecycleByteBlocks(b, 0, b.Length);
            }

            public virtual sbyte[] ByteBlock
            {
                get
                {
                    return new sbyte[blockSize];
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

            public override void RecycleByteBlocks(sbyte[][] blocks, int start, int end)
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

            public override void RecycleByteBlocks(sbyte[][] blocks, int start, int end)
            {
                bytesUsed.AddAndGet(-((end - start) * blockSize));
                for (int i = start; i < end; i++)
                {
                    blocks[i] = null;
                }
            }
        }

        public sbyte[][] buffers = new sbyte[10][];

        private int bufferUpto = -1;

        internal int byteUpto = BYTE_BLOCK_SIZE;

        public sbyte[] buffer;

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
                        Arrays.Fill(buffers[i], (sbyte)0);
                    }
                    // Partial zero fill the final buffer
                    Arrays.Fill(buffers[bufferUpto], 0, byteUpto, (sbyte)0);
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
                sbyte[][] newBuffers = new sbyte[ArrayUtil.Oversize(buffers.Length + 1,
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

        public static readonly int[] NEXT_LEVEL_ARRAY = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 9 };

        public static readonly int[] LEVEL_SIZE_ARRAY = { 5, 14, 20, 30, 40, 40, 80, 80, 120, 200 };

        public static readonly int FIRST_LEVEL_SIZE = LEVEL_SIZE_ARRAY[0];

        public int AllocSlice(sbyte[] slice, int upto)
        {
            int level = slice[upto] & 15;
            int newLevel = NEXT_LEVEL_ARRAY[level];
            int newSize = LEVEL_SIZE_ARRAY[newLevel];

            // Maybe allocate another block
            if (byteUpto > BYTE_BLOCK_SIZE - newSize)
            {
                NextBuffer();
            }

            int newUpto = byteUpto;
            int offset = newUpto + byteOffset;
            byteUpto += newSize;

            // Copy forward the past 3 bytes (which we are about
            // to overwrite with the forwarding address):
            buffer[newUpto] = slice[upto - 3];
            buffer[newUpto + 1] = slice[upto - 2];
            buffer[newUpto + 2] = slice[upto - 1];

            // Write forwarding address at end of last slice:
            slice[upto - 3] = (sbyte)Number.URShift(offset, 24);
            slice[upto - 2] = (sbyte)Number.URShift(offset, 16);
            slice[upto - 1] = (sbyte)Number.URShift(offset, 8);
            slice[upto] = (sbyte)offset;

            // Write new level:
            buffer[byteUpto - 1] = (sbyte)(16 | newLevel);

            return newUpto + 3;
        }

        public void SetBytesRef(BytesRef term, int textStart)
        {
            sbyte[] bytes = term.bytes = buffers[textStart >> BYTE_BLOCK_SHIFT];
            int pos = textStart & BYTE_BLOCK_MASK;
            if ((bytes[pos] & 0x80) == 0)
            {
                // length is 1 byte
                term.length = bytes[pos];
                term.offset = pos + 1;
            }
            else
            {
                // length is 2 bytes
                term.length = (bytes[pos] & 0x7f) + ((bytes[pos + 1] & 0xff) << 7);
                term.offset = pos + 2;
            }
            //assert term.length >= 0;
        }

        public void Append(BytesRef bytes)
        {
            int length = bytes.length;
            if (length == 0)
            {
                return;
            }
            int offset = bytes.offset;
            int overflow = (length + byteUpto) - BYTE_BLOCK_SIZE;
            do
            {
                if (overflow <= 0)
                {
                    Array.Copy(bytes.bytes, offset, buffer, byteUpto, length);
                    byteUpto += length;
                    break;
                }
                else
                {
                    int bytesToCopy = length - overflow;
                    if (bytesToCopy > 0)
                    {
                        Array.Copy(bytes.bytes, offset, buffer, byteUpto, bytesToCopy);
                        offset += bytesToCopy;
                        length -= bytesToCopy;
                    }
                    NextBuffer();
                    overflow = overflow - BYTE_BLOCK_SIZE;
                }
            } while (true);
        }

        public void ReadBytes(long offset, sbyte[] bytes, int off, int length)
        {
            if (length == 0)
            {
                return;
            }
            int bytesOffset = off;
            int bytesLength = length;
            int bufferIndex = (int)(offset >> BYTE_BLOCK_SHIFT);
            sbyte[] buffer = buffers[bufferIndex];
            int pos = (int)(offset & BYTE_BLOCK_MASK);
            int overflow = (pos + length) - BYTE_BLOCK_SIZE;
            do
            {
                if (overflow <= 0)
                {
                    Array.Copy(buffer, pos, bytes, bytesOffset, bytesLength);
                    break;
                }
                else
                {
                    int bytesToCopy = length - overflow;
                    Array.Copy(buffer, pos, bytes, bytesOffset, bytesToCopy);
                    pos = 0;
                    bytesLength -= bytesToCopy;
                    bytesOffset += bytesToCopy;
                    buffer = buffers[++bufferIndex];
                    overflow = overflow - BYTE_BLOCK_SIZE;
                }
            } while (true);
        }
    }
}
