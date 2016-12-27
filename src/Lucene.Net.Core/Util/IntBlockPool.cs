using Lucene.Net.Support;
using System;
using System.Diagnostics;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// A pool for int blocks similar to <seealso cref="ByteBlockPool"/>
    /// @lucene.internal
    /// </summary>
    public sealed class IntBlockPool
    {
        public static readonly int INT_BLOCK_SHIFT = 13;
        public static readonly int INT_BLOCK_SIZE = 1 << INT_BLOCK_SHIFT;
        public static readonly int INT_BLOCK_MASK = INT_BLOCK_SIZE - 1;

        /// <summary>
        /// Abstract class for allocating and freeing int
        ///  blocks.
        /// </summary>
        public abstract class Allocator
        {
            protected readonly int m_blockSize;

            public Allocator(int blockSize)
            {
                this.m_blockSize = blockSize;
            }

            public abstract void RecycleIntBlocks(int[][] blocks, int start, int end);

            public virtual int[] GetIntBlock() // LUCENENET TODO: Rename GetInt32Block() ?
            {
                return new int[m_blockSize];
            }
        }

        /// <summary>
        /// A simple <seealso cref="Allocator"/> that never recycles. </summary>
        public sealed class DirectAllocator : Allocator
        {
            /// <summary>
            /// Creates a new <seealso cref="DirectAllocator"/> with a default block size
            /// </summary>
            public DirectAllocator()
                : base(INT_BLOCK_SIZE)
            {
            }

            public override void RecycleIntBlocks(int[][] blocks, int start, int end)
            {
            }
        }

        /// <summary>
        /// array of buffers currently used in the pool. Buffers are allocated if needed don't modify this outside of this class </summary>
        public int[][] Buffers = new int[10][]; // LUCENENET TODO: make property ?

        /// <summary>
        /// index into the buffers array pointing to the current buffer used as the head </summary>
        private int bufferUpto = -1;

        /// <summary>
        /// Pointer to the current position in head buffer </summary>
        public int IntUpto { get; set; }

        /// <summary>
        /// Current head buffer </summary>
        public int[] Buffer; // LUCENENET TODO: make property ?

        /// <summary>
        /// Current head offset </summary>
        public int IntOffset { get; set; }

        private readonly Allocator allocator;

        /// <summary>
        /// Creates a new <seealso cref="IntBlockPool"/> with a default <seealso cref="Allocator"/>. </summary>
        /// <seealso cref= IntBlockPool#nextBuffer() </seealso>
        public IntBlockPool()
            : this(new DirectAllocator())
        {
        }

        /// <summary>
        /// Creates a new <seealso cref="IntBlockPool"/> with the given <seealso cref="Allocator"/>. </summary>
        /// <seealso cref= IntBlockPool#nextBuffer() </seealso>
        public IntBlockPool(Allocator allocator)
        {
            // set defaults
            IntUpto = INT_BLOCK_SIZE;
            IntOffset = -INT_BLOCK_SIZE;

            this.allocator = allocator;
        }

        /// <summary>
        /// Resets the pool to its initial state reusing the first buffer. Calling
        /// <seealso cref="IntBlockPool#nextBuffer()"/> is not needed after reset.
        /// </summary>
        public void Reset()
        {
            this.Reset(true, true);
        }

        /// <summary>
        /// Expert: Resets the pool to its initial state reusing the first buffer. </summary>
        /// <param name="zeroFillBuffers"> if <code>true</code> the buffers are filled with <tt>0</tt>.
        ///        this should be set to <code>true</code> if this pool is used with
        ///        <seealso cref="SliceWriter"/>. </param>
        /// <param name="reuseFirst"> if <code>true</code> the first buffer will be reused and calling
        ///        <seealso cref="IntBlockPool#nextBuffer()"/> is not needed after reset iff the
        ///        block pool was used before ie. <seealso cref="IntBlockPool#nextBuffer()"/> was called before. </param>
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
                        Arrays.Fill(Buffers[i], 0);
                    }
                    // Partial zero fill the final buffer
                    Arrays.Fill(Buffers[bufferUpto], 0, IntUpto, 0);
                }

                if (bufferUpto > 0 || !reuseFirst)
                {
                    int offset = reuseFirst ? 1 : 0;
                    // Recycle all but the first buffer
                    allocator.RecycleIntBlocks(Buffers, offset, 1 + bufferUpto);
                    Arrays.Fill(Buffers, offset, bufferUpto + 1, null);
                }
                if (reuseFirst)
                {
                    // Re-use the first buffer
                    bufferUpto = 0;
                    IntUpto = 0;
                    IntOffset = 0;
                    Buffer = Buffers[0];
                }
                else
                {
                    bufferUpto = -1;
                    IntUpto = INT_BLOCK_SIZE;
                    IntOffset = -INT_BLOCK_SIZE;
                    Buffer = null;
                }
            }
        }

        /// <summary>
        /// Advances the pool to its next buffer. this method should be called once
        /// after the constructor to initialize the pool. In contrast to the
        /// constructor a <seealso cref="IntBlockPool#reset()"/> call will advance the pool to
        /// its first buffer immediately.
        /// </summary>
        public void NextBuffer()
        {
            if (1 + bufferUpto == Buffers.Length)
            {
                int[][] newBuffers = new int[(int)(Buffers.Length * 1.5)][];
                Array.Copy(Buffers, 0, newBuffers, 0, Buffers.Length);
                Buffers = newBuffers;
            }
            Buffer = Buffers[1 + bufferUpto] = allocator.GetIntBlock();
            bufferUpto++;

            IntUpto = 0;
            IntOffset += INT_BLOCK_SIZE;
        }

        /// <summary>
        /// Creates a new int slice with the given starting size and returns the slices offset in the pool. </summary>
        /// <seealso cref= SliceReader </seealso>
        private int NewSlice(int size)
        {
            if (IntUpto > INT_BLOCK_SIZE - size)
            {
                NextBuffer();
                Debug.Assert(AssertSliceBuffer(Buffer));
            }

            int upto = IntUpto;
            IntUpto += size;
            Buffer[IntUpto - 1] = 1;
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
        /// <summary>
        /// An array holding the offset into the <seealso cref="IntBlockPool#LEVEL_SIZE_ARRAY"/>
        /// to quickly navigate to the next slice level.
        /// </summary>
        private static readonly int[] NEXT_LEVEL_ARRAY = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 9 };

        /// <summary>
        /// An array holding the level sizes for int slices.
        /// </summary>
        private static readonly int[] LEVEL_SIZE_ARRAY = new int[] { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024 };

        /// <summary>
        /// The first level size for new slices
        /// </summary>
        private static readonly int FIRST_LEVEL_SIZE = LEVEL_SIZE_ARRAY[0];

        /// <summary>
        /// Allocates a new slice from the given offset
        /// </summary>
        private int AllocSlice(int[] slice, int sliceOffset)
        {
            int level = slice[sliceOffset];
            int newLevel = NEXT_LEVEL_ARRAY[level - 1];
            int newSize = LEVEL_SIZE_ARRAY[newLevel];
            // Maybe allocate another block
            if (IntUpto > INT_BLOCK_SIZE - newSize)
            {
                NextBuffer();
                Debug.Assert(AssertSliceBuffer(Buffer));
            }

            int newUpto = IntUpto;
            int offset = newUpto + IntOffset;
            IntUpto += newSize;
            // Write forwarding address at end of last slice:
            slice[sliceOffset] = offset;

            // Write new level:
            Buffer[IntUpto - 1] = newLevel;

            return newUpto;
        }

        /// <summary>
        /// A <seealso cref="SliceWriter"/> that allows to write multiple integer slices into a given <seealso cref="IntBlockPool"/>.
        /// </summary>
        ///  <seealso cref= SliceReader
        ///  @lucene.internal </seealso>
        public class SliceWriter
        {
            private int offset;
            private readonly IntBlockPool pool;

            public SliceWriter(IntBlockPool pool)
            {
                this.pool = pool;
            }

            ///
            public virtual void Reset(int sliceOffset)
            {
                this.offset = sliceOffset;
            }

            /// <summary>
            /// Writes the given value into the slice and resizes the slice if needed
            /// </summary>
            public virtual void WriteInt(int value) // LUCENENET TODO: rename WriteInt32 ?
            {
                int[] ints = pool.Buffers[offset >> INT_BLOCK_SHIFT];
                Debug.Assert(ints != null);
                int relativeOffset = offset & INT_BLOCK_MASK;
                if (ints[relativeOffset] != 0)
                {
                    // End of slice; allocate a new one
                    relativeOffset = pool.AllocSlice(ints, relativeOffset);
                    ints = pool.Buffer;
                    offset = relativeOffset + pool.IntOffset;
                }
                ints[relativeOffset] = value;
                offset++;
            }

            /// <summary>
            /// starts a new slice and returns the start offset. The returned value
            /// should be used as the start offset to initialize a <seealso cref="SliceReader"/>.
            /// </summary>
            public virtual int StartNewSlice()
            {
                return offset = pool.NewSlice(FIRST_LEVEL_SIZE) + pool.IntOffset;
            }

            /// <summary>
            /// Returns the offset of the currently written slice. The returned value
            /// should be used as the end offset to initialize a <seealso cref="SliceReader"/> once
            /// this slice is fully written or to reset the this writer if another slice
            /// needs to be written.
            /// </summary>
            public virtual int CurrentOffset
            {
                get
                {
                    return offset;
                }
            }
        }

        /// <summary>
        /// A <seealso cref="SliceReader"/> that can read int slices written by a <seealso cref="SliceWriter"/>
        /// @lucene.internal
        /// </summary>
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

            /// <summary>
            /// Creates a new <seealso cref="SliceReader"/> on the given pool
            /// </summary>
            public SliceReader(IntBlockPool pool)
            {
                this.pool = pool;
            }

            /// <summary>
            /// Resets the reader to a slice give the slices absolute start and end offset in the pool
            /// </summary>
            public void Reset(int startOffset, int endOffset)
            {
                bufferUpto = startOffset / INT_BLOCK_SIZE;
                bufferOffset = bufferUpto * INT_BLOCK_SIZE;
                this.end = endOffset;
                upto = startOffset;
                level = 1;

                buffer = pool.Buffers[bufferUpto];
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

            /// <summary>
            /// Returns <code>true</code> iff the current slice is fully read. If this
            /// method returns <code>true</code> <seealso cref="SliceReader#readInt()"/> should not
            /// be called again on this slice.
            /// </summary>
            public bool EndOfSlice()
            {
                Debug.Assert(upto + bufferOffset <= end);
                return upto + bufferOffset == end;
            }

            /// <summary>
            /// Reads the next int from the current slice and returns it. </summary>
            /// <seealso cref= SliceReader#endOfSlice() </seealso>
            public int ReadInt() // LUCENENET TODO: Rename ReadInt32() ?
            {
                Debug.Assert(!EndOfSlice());
                Debug.Assert(upto <= limit);
                if (upto == limit)
                {
                    NextSlice();
                }
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

                buffer = pool.Buffers[bufferUpto];
                upto = nextIndex & INT_BLOCK_MASK;

                if (nextIndex + newSize >= end)
                {
                    // We are advancing to the final slice
                    Debug.Assert(end - nextIndex > 0);
                    limit = end - bufferOffset;
                }
                else
                {
                    // this is not the final slice (subtract 4 for the
                    // forwarding address at the end of this new slice)
                    limit = upto + newSize - 1;
                }
            }
        }
    }
}