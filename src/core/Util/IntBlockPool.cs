using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class IntBlockPool
    {
        public const int INT_BLOCK_SHIFT = 13;
        public const int INT_BLOCK_SIZE = 1 << INT_BLOCK_SHIFT;
        public const int INT_BLOCK_MASK = INT_BLOCK_SIZE - 1;

        public abstract class Allocator
        {
            protected readonly int blockSize;

            public Allocator(int blockSize)
            {
                this.blockSize = blockSize;
            }

            public abstract void RecycleIntBlocks(int[][] blocks, int start, int end);

            public virtual int[] IntBlock
            {
                get
                {
                    return new int[blockSize];
                }
            }
        }

        public sealed class DirectAllocator : Allocator
        {
            public DirectAllocator()
                : base(INT_BLOCK_SIZE)
            {
            }

            public override void RecycleIntBlocks(int[][] blocks, int start, int end)
            {
            }
        }

        /** array of buffers currently used in the pool. Buffers are allocated if needed don't modify this outside of this class */
        public int[][] buffers = new int[10][];

        /** index into the buffers array pointing to the current buffer used as the head */
        private int bufferUpto = -1;
        /** Pointer to the current position in head buffer */
        public int intUpto = INT_BLOCK_SIZE;
        /** Current head buffer */
        public int[] buffer;
        /** Current head offset */
        public int intOffset = -INT_BLOCK_SIZE;

        private readonly Allocator allocator;

        public IntBlockPool()
            : this(new DirectAllocator())
        {
        }

        public IntBlockPool(Allocator allocator)
        {
            this.allocator = allocator;
        }

        public void Reset()
        {
            this.Reset(true, true);
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
                        Arrays.Fill(buffers[i], 0);
                    }
                    // Partial zero fill the final buffer
                    Arrays.Fill(buffers[bufferUpto], 0, intUpto, 0);
                }

                if (bufferUpto > 0 || !reuseFirst)
                {
                    int offset = reuseFirst ? 1 : 0;
                    // Recycle all but the first buffer
                    allocator.RecycleIntBlocks(buffers, offset, 1 + bufferUpto);
                    Arrays.Fill(buffers, offset, bufferUpto + 1, null);
                }
                if (reuseFirst)
                {
                    // Re-use the first buffer
                    bufferUpto = 0;
                    intUpto = 0;
                    intOffset = 0;
                    buffer = buffers[0];
                }
                else
                {
                    bufferUpto = -1;
                    intUpto = INT_BLOCK_SIZE;
                    intOffset = -INT_BLOCK_SIZE;
                    buffer = null;
                }
            }
        }

        public void NextBuffer()
        {
            if (1 + bufferUpto == buffers.Length)
            {
                int[][] newBuffers = new int[(int)(buffers.Length * 1.5)][];
                Array.Copy(buffers, 0, newBuffers, 0, buffers.Length);
                buffers = newBuffers;
            }
            buffer = buffers[1 + bufferUpto] = allocator.IntBlock;
            bufferUpto++;

            intUpto = 0;
            intOffset += INT_BLOCK_SIZE;
        }

        private int NewSlice(int size)
        {
            if (intUpto > INT_BLOCK_SIZE - size)
            {
                NextBuffer();
                //assert assertSliceBuffer(buffer);
            }

            int upto = intUpto;
            intUpto += size;
            buffer[intUpto - 1] = 1;
            return upto;
        }

        private static bool AssertSliceBuffer(int[] buffer)
        {
            int count = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                count += buffer[i]; // for slices the buffer must only have 0 values
            }
            return count == 0;
        }

        // no need to make this public unless we support different sizes
        // TODO make the levels and the sizes configurable
        /**
         * An array holding the offset into the {@link IntBlockPool#LEVEL_SIZE_ARRAY}
         * to quickly navigate to the next slice level.
         */
        private static readonly int[] NEXT_LEVEL_ARRAY = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 9 };

        /**
         * An array holding the level sizes for int slices.
         */
        private static readonly int[] LEVEL_SIZE_ARRAY = { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024 };

        /**
         * The first level size for new slices
         */
        private static readonly int FIRST_LEVEL_SIZE = LEVEL_SIZE_ARRAY[0];

        private int AllocSlice(int[] slice, int sliceOffset)
        {
            int level = slice[sliceOffset];
            int newLevel = NEXT_LEVEL_ARRAY[level - 1];
            int newSize = LEVEL_SIZE_ARRAY[newLevel];
            // Maybe allocate another block
            if (intUpto > INT_BLOCK_SIZE - newSize)
            {
                NextBuffer();
                //assert assertSliceBuffer(buffer);
            }

            int newUpto = intUpto;
            int offset = newUpto + intOffset;
            intUpto += newSize;
            // Write forwarding address at end of last slice:
            slice[sliceOffset] = offset;

            // Write new level:
            buffer[intUpto - 1] = newLevel;

            return newUpto;
        }

        public class SliceWriter
        {
            private int offset;
            private readonly IntBlockPool pool;

            public SliceWriter(IntBlockPool pool)
            {
                this.pool = pool;
            }

            public virtual void Reset(int sliceOffset)
            {
                this.offset = sliceOffset;
            }

            public virtual void WriteInt(int value)
            {
                int[] ints = pool.buffers[offset >> INT_BLOCK_SHIFT];
                //assert ints != null;
                int relativeOffset = offset & INT_BLOCK_MASK;
                if (ints[relativeOffset] != 0)
                {
                    // End of slice; allocate a new one
                    relativeOffset = pool.AllocSlice(ints, relativeOffset);
                    ints = pool.buffer;
                    offset = relativeOffset + pool.intOffset;
                }
                ints[relativeOffset] = value;
                offset++;
            }

            public virtual int StartNewSlice()
            {
                return offset = pool.NewSlice(FIRST_LEVEL_SIZE) + pool.intOffset;
            }

            public virtual int CurrentOffset
            {
                get
                {
                    return offset;
                }
            }
        }

        public sealed class SliceReader
        {
            private readonly IntBlockPool pool;
            private int upto;
            private int bufferUpto;
            private int bufferOffset;
            private int[] buffer;
            private int limit;
            private int level;
            private int end;

            public SliceReader(IntBlockPool pool)
            {
                this.pool = pool;
            }

            public void Reset(int startOffset, int endOffset)
            {
                bufferUpto = startOffset / INT_BLOCK_SIZE;
                bufferOffset = bufferUpto * INT_BLOCK_SIZE;
                this.end = endOffset;
                upto = startOffset;
                level = 1;

                buffer = pool.buffers[bufferUpto];
                upto = startOffset & INT_BLOCK_MASK;

                int firstSize = IntBlockPool.LEVEL_SIZE_ARRAY[0];
                if (startOffset + firstSize >= endOffset)
                {
                    // There is only this one slice to read
                    limit = endOffset & INT_BLOCK_MASK;
                }
                else
                {
                    limit = upto + firstSize - 1;
                }
            }

            public bool EndOfSlice()
            {
                //assert upto + bufferOffset <= end;
                return upto + bufferOffset == end;
            }

            public int ReadInt()
            {
                //assert !endOfSlice();
                //assert upto <= limit;
                if (upto == limit)
                    NextSlice();
                return buffer[upto++];
            }

            private void NextSlice()
            {
                // Skip to our next slice
                int nextIndex = buffer[limit];
                level = NEXT_LEVEL_ARRAY[level - 1];
                int newSize = LEVEL_SIZE_ARRAY[level];

                bufferUpto = nextIndex / INT_BLOCK_SIZE;
                bufferOffset = bufferUpto * INT_BLOCK_SIZE;

                buffer = pool.buffers[bufferUpto];
                upto = nextIndex & INT_BLOCK_MASK;

                if (nextIndex + newSize >= end)
                {
                    // We are advancing to the final slice
                    //assert end - nextIndex > 0;
                    limit = end - bufferOffset;
                }
                else
                {
                    // This is not the final slice (subtract 4 for the
                    // forwarding address at the end of this new slice)
                    limit = upto + newSize - 1;
                }
            }
        }
    }
}
