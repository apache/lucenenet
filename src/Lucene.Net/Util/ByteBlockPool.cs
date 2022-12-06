using J2N.Collections.Generic.Extensions;
using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
    /// Class that Posting and PostingVector use to write byte
    /// streams into shared fixed-size <see cref="T:byte[]"/> arrays.  The idea
    /// is to allocate slices of increasing lengths. For
    /// example, the first slice is 5 bytes, the next slice is
    /// 14, etc.  We start by writing our bytes into the first
    /// 5 bytes.  When we hit the end of the slice, we allocate
    /// the next slice and then write the address of the new
    /// slice into the last 4 bytes of the previous slice (the
    /// "forwarding address").
    /// <para/>
    /// Each slice is filled with 0's initially, and we mark
    /// the end with a non-zero byte.  This way the methods
    /// that are writing into the slice don't need to record
    /// its length and instead allocate a new slice once they
    /// hit a non-zero byte.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class ByteBlockPool
    {
        public static readonly int BYTE_BLOCK_SHIFT = 15;
        public static readonly int BYTE_BLOCK_SIZE = 1 << BYTE_BLOCK_SHIFT;
        public static readonly int BYTE_BLOCK_MASK = BYTE_BLOCK_SIZE - 1;

        /// <summary>
        /// Abstract class for allocating and freeing byte
        /// blocks.
        /// </summary>
        public abstract class Allocator
        {
            protected readonly int m_blockSize;

            protected Allocator(int blockSize)
            {
                this.m_blockSize = blockSize;
            }

            public abstract void RecycleByteBlocks(byte[][] blocks, int start, int end); // LUCENENT TODO: API - Change to use IList<byte[]>

            public virtual void RecycleByteBlocks(IList<byte[]> blocks)
            {
                var b = blocks.ToArray();
                RecycleByteBlocks(b, 0, b.Length);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual byte[] GetByteBlock()
            {
                return new byte[m_blockSize];
            }
        }

        /// <summary>
        /// A simple <see cref="Allocator"/> that never recycles. </summary>
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void RecycleByteBlocks(byte[][] blocks, int start, int end)
            {
            }
        }

        /// <summary>
        /// A simple <see cref="Allocator"/> that never recycles, but
        /// tracks how much total RAM is in use.
        /// </summary>
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override byte[] GetByteBlock()
            {
                bytesUsed.AddAndGet(m_blockSize);
                return new byte[m_blockSize];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void RecycleByteBlocks(byte[][] blocks, int start, int end)
            {
                bytesUsed.AddAndGet(-((end - start) * m_blockSize));
                for (var i = start; i < end; i++)
                {
                    blocks[i] = null;
                }
            }
        }

        /// <summary>
        /// Array of buffers currently used in the pool. Buffers are allocated if
        /// needed don't modify this outside of this class.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public byte[][] Buffers
        {
            get => buffers;
            set => buffers = value;
        }
        private byte[][] buffers = new byte[10][];

        /// <summary>
        /// index into the buffers array pointing to the current buffer used as the head </summary>
        private int bufferUpto = -1; // Which buffer we are upto

        /// <summary>
        /// Where we are in head buffer </summary>
        public int ByteUpto { get; set; }

        /// <summary>
        /// Current head buffer
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public byte[] Buffer
        {
            get => buffer;
            set => buffer = value;
        }
        private byte[] buffer;

        /// <summary>
        /// Current head offset </summary>
        public int ByteOffset { get; set; }

        private readonly Allocator allocator;

        public ByteBlockPool(Allocator allocator)
        {
            // set defaults
            ByteUpto = BYTE_BLOCK_SIZE;
            ByteOffset = -BYTE_BLOCK_SIZE;

            this.allocator = allocator;
        }

        /// <summary>
        /// Resets the pool to its initial state reusing the first buffer and fills all
        /// buffers with <c>0</c> bytes before they reused or passed to
        /// <see cref="Allocator.RecycleByteBlocks(byte[][], int, int)"/>. Calling
        /// <see cref="ByteBlockPool.NextBuffer()"/> is not needed after reset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Reset(true, true);
        }

        /// <summary>
        /// Expert: Resets the pool to its initial state reusing the first buffer. Calling
        /// <see cref="ByteBlockPool.NextBuffer()"/> is not needed after reset. </summary>
        /// <param name="zeroFillBuffers"> if <c>true</c> the buffers are filled with <tt>0</tt>.
        ///        this should be set to <c>true</c> if this pool is used with slices. </param>
        /// <param name="reuseFirst"> if <c>true</c> the first buffer will be reused and calling
        ///        <see cref="ByteBlockPool.NextBuffer()"/> is not needed after reset if the
        ///        block pool was used before ie. <see cref="ByteBlockPool.NextBuffer()"/> was called before. </param>
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
                    Arrays.Fill(buffers[bufferUpto], 0, ByteUpto, (byte)0);
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
                    ByteUpto = 0;
                    ByteOffset = 0;
                    buffer = buffers[0];
                }
                else
                {
                    bufferUpto = -1;
                    ByteUpto = BYTE_BLOCK_SIZE;
                    ByteOffset = -BYTE_BLOCK_SIZE;
                    buffer = null;
                }
            }
        }

        /// <summary>
        /// Advances the pool to its next buffer. This method should be called once
        /// after the constructor to initialize the pool. In contrast to the
        /// constructor a <see cref="ByteBlockPool.Reset()"/> call will advance the pool to
        /// its first buffer immediately.
        /// </summary>
        public void NextBuffer()
        {
            if (1 + bufferUpto == buffers.Length)
            {
                var newBuffers = new byte[ArrayUtil.Oversize(buffers.Length + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
                Arrays.Copy(buffers, 0, newBuffers, 0, buffers.Length);
                buffers = newBuffers;
            }
            buffer = buffers[1 + bufferUpto] = allocator.GetByteBlock();
            bufferUpto++;

            ByteUpto = 0;
            ByteOffset += BYTE_BLOCK_SIZE;
        }

        /// <summary>
        /// Allocates a new slice with the given size.</summary>
        /// <seealso cref="ByteBlockPool.FIRST_LEVEL_SIZE"/>
        public int NewSlice(int size)
        {
            if (ByteUpto > BYTE_BLOCK_SIZE - size)
            {
                NextBuffer();
            }
            int upto = ByteUpto;
            ByteUpto += size;
            buffer[ByteUpto - 1] = 16;
            return upto;
        }

        // Size of each slice.  These arrays should be at most 16
        // elements (index is encoded with 4 bits).  First array
        // is just a compact way to encode X+1 with a max.  Second
        // array is the length of each slice, ie first slice is 5
        // bytes, next slice is 14 bytes, etc.

        /// <summary>
        /// An array holding the offset into the <see cref="ByteBlockPool.LEVEL_SIZE_ARRAY"/>
        /// to quickly navigate to the next slice level.
        /// </summary>
        public static readonly int[] NEXT_LEVEL_ARRAY = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 9 };

        /// <summary>
        /// An array holding the level sizes for byte slices.
        /// </summary>
        public static readonly int[] LEVEL_SIZE_ARRAY = new int[] { 5, 14, 20, 30, 40, 40, 80, 80, 120, 200 };

        /// <summary>
        /// The first level size for new slices </summary>
        /// <seealso cref="ByteBlockPool.NewSlice(int)"/>
        public static readonly int FIRST_LEVEL_SIZE = LEVEL_SIZE_ARRAY[0];

        /// <summary>
        /// Creates a new byte slice with the given starting size and
        /// returns the slices offset in the pool.
        /// </summary>
        public int AllocSlice(byte[] slice, int upto)
        {
            int level = slice[upto] & 15;
            int newLevel = NEXT_LEVEL_ARRAY[level];
            int newSize = LEVEL_SIZE_ARRAY[newLevel];

            // Maybe allocate another block
            if (ByteUpto > BYTE_BLOCK_SIZE - newSize)
            {
                NextBuffer();
            }

            int newUpto = ByteUpto;
            int offset = newUpto + ByteOffset;
            ByteUpto += newSize;

            // Copy forward the past 3 bytes (which we are about
            // to overwrite with the forwarding address):
            buffer[newUpto] = slice[upto - 3];
            buffer[newUpto + 1] = slice[upto - 2];
            buffer[newUpto + 2] = slice[upto - 1];

            // Write forwarding address at end of last slice:
            slice[upto - 3] = (byte)offset.TripleShift(24);
            slice[upto - 2] = (byte)offset.TripleShift(16);
            slice[upto - 1] = (byte)offset.TripleShift(8);
            slice[upto] = (byte)offset;

            // Write new level:
            buffer[ByteUpto - 1] = (byte)(16 | newLevel);

            return newUpto + 3;
        }

        // Fill in a BytesRef from term's length & bytes encoded in
        // byte block
        public void SetBytesRef(BytesRef term, int textStart)
        {
            var bytes = term.Bytes = buffers[textStart >> BYTE_BLOCK_SHIFT];
            var pos = textStart & BYTE_BLOCK_MASK;
            if ((bytes[pos] & 0x80) == 0)
            {
                // length is 1 byte
                term.Length = bytes[pos];
                term.Offset = pos + 1;
            }
            else
            {
                // length is 2 bytes
                term.Length = (bytes[pos] & 0x7f) + ((bytes[pos + 1] & 0xff) << 7);
                term.Offset = pos + 2;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(term.Length >= 0);
        }

        /// <summary>
        /// Appends the bytes in the provided <see cref="BytesRef"/> at
        /// the current position.
        /// </summary>
        public void Append(BytesRef bytes)
        {
            var length = bytes.Length;
            if (length == 0)
            {
                return;
            }
            int offset = bytes.Offset;
            int overflow = (length + ByteUpto) - BYTE_BLOCK_SIZE;
            do
            {
                if (overflow <= 0)
                {
                    Arrays.Copy(bytes.Bytes, offset, buffer, ByteUpto, length);
                    ByteUpto += length;
                    break;
                }
                else
                {
                    int bytesToCopy = length - overflow;
                    if (bytesToCopy > 0)
                    {
                        Arrays.Copy(bytes.Bytes, offset, buffer, ByteUpto, bytesToCopy);
                        offset += bytesToCopy;
                        length -= bytesToCopy;
                    }
                    NextBuffer();
                    overflow = overflow - BYTE_BLOCK_SIZE;
                }
            } while (true);
        }

        /// <summary>
        /// Reads bytes bytes out of the pool starting at the given offset with the given
        /// length into the given byte array at offset <c>off</c>.
        /// <para>Note: this method allows to copy across block boundaries.</para>
        /// </summary>
        public void ReadBytes(long offset, byte[] bytes, int off, int length)
        {
            if (length == 0)
            {
                return;
            }
            var bytesOffset = off;
            var bytesLength = length;
            var bufferIndex = (int)(offset >> BYTE_BLOCK_SHIFT);
            var buffer = buffers[bufferIndex];
            var pos = (int)(offset & BYTE_BLOCK_MASK);
            var overflow = (pos + length) - BYTE_BLOCK_SIZE;
            do
            {
                if (overflow <= 0)
                {
                    Arrays.Copy(buffer, pos, bytes, bytesOffset, bytesLength);
                    break;
                }
                else
                {
                    int bytesToCopy = length - overflow;
                    Arrays.Copy(buffer, pos, bytes, bytesOffset, bytesToCopy);
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