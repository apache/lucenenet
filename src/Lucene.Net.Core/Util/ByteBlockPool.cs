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

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    ///     Class that Posting and PostingVector use to write byte
    ///     streams into shared fixed-size byte[] arrays.  
    /// </summary>
    /// <remarks>
    ///  <para>
    ///     The idea is to allocate slices of increasing lengths For
    ///     example, the first slice is 5 bytes, the next slice is
    ///     14, etc.  We start by writing our bytes into the first
    ///     5 bytes.  When we hit the end of the slice, we allocate
    ///     the next slice and then write the address of the new
    ///     slice into the last 4 bytes of the previous slice (the
    ///     "forwarding address").
    /// </para>
    /// <para>
    ///     Each slice is filled with 0's initially, and we mark
    ///     the end with a non-zero byte.  this way the methods
    ///     that are writing into the slice don't need to record
    ///     its length and instead allocate a new slice once they
    ///     hit a non-zero byte.
    ///     @lucene.internal
    /// </para>
    /// </remarks>
    public sealed class ByteBlockPool
    {

        /// <summary>
        /// The byte block shift.
        /// </summary>
        public const int BYTE_BLOCK_SHIFT = 15;

        /// <summary>
        /// The byte block size.
        /// </summary>
        public static readonly int BYTE_BLOCK_SIZE = 1 << BYTE_BLOCK_SHIFT;
        
        /// <summary>
        /// The byte block mask;
        /// </summary>
        public static readonly int BYTE_BLOCK_MASK = BYTE_BLOCK_SIZE - 1;

        /// <summary>
        ///     An array holding the offset into the <seealso cref="ByteBlockPool#LEVEL_SIZE_ARRAY" />
        ///     to quickly navigate to the next slice level.
        /// </summary>
        public static readonly int[] NEXT_LEVEL_ARRAY = {1, 2, 3, 4, 5, 6, 7, 8, 9, 9};

        /// <summary>
        ///     An array holding the level sizes for byte slices.
        /// </summary>
        public static readonly int[] LEVEL_SIZE_ARRAY = {5, 14, 20, 30, 40, 40, 80, 80, 120, 200};

        /// <summary>
        ///     The first level size for new slices
        /// </summary>
        /// <seealso cref="ByteBlockPool.NewSlice(int)" />
        public static readonly int FIRST_LEVEL_SIZE = LEVEL_SIZE_ARRAY[0];

        private readonly Allocator allocator;

        /// <summary>
        ///     Current head buffer
        /// </summary>
        public byte[] Buffer;

        /// <summary>
        ///     array of buffers currently used in the pool. Buffers are allocated if
        ///     needed don't modify this outside of this class.
        /// </summary>
        public byte[][] Buffers = new byte[10][];

        /// <summary>
        ///     Current head offset
        /// </summary>
        public int ByteOffset = -BYTE_BLOCK_SIZE;

        /// <summary>
        /// Where we are in head buffer
        /// </summary>
        // renamed from ByteUpto as position is easier to understand.
        public int BytePosition = BYTE_BLOCK_SIZE;

        /// <summary>
        ///     index into the buffers array pointing to the current buffer used as the head
        /// </summary>
        private int bufferPosition = -1; // The position of the current buffer

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteBlockPool"/> class.
        /// </summary>
        /// <param name="allocator">The allocator.</param>
        public ByteBlockPool(Allocator allocator)
        {
            this.allocator = allocator;
            // this should always be called when a pool is created.
            this.NextBuffer();
        }

        /// <summary>
        ///     Resets the pool to its initial state reusing the first buffer and fills all
        ///     buffers with <tt>0</tt> bytes before they reused or passed to
        ///     <seealso cref="Allocator#recycleByteBlocks(byte[][], int, int)" />. Calling
        ///     <seealso cref="ByteBlockPool#nextBuffer()" /> is not needed after reset.
        /// </summary>
        public void Reset()
        {
            Reset(true, true);
        }

        /// <summary>
        ///     Expert: Resets the pool to its initial state reusing the first buffer. Calling
        ///     <seealso cref="ByteBlockPool#nextBuffer()" /> is not needed after reset.
        /// </summary>
        /// <param name="zeroFillBuffers">
        ///     if <code>true</code> the buffers are filled with <tt>0</tt>.
        ///     this should be set to <code>true</code> if this pool is used with slices.
        /// </param>
        /// <param name="reuseFirst">
        ///     if <code>true</code> the first buffer will be reused and calling
        ///     <seealso cref="ByteBlockPool#nextBuffer()" /> is not needed after reset iff the
        ///     block pool was used before ie. <seealso cref="ByteBlockPool#nextBuffer()" /> was called before.
        /// </param>
        public void Reset(bool zeroFillBuffers, bool reuseFirst)
        {
            if (this.bufferPosition != -1)
            {
                // We allocated at least one buffer

                if (zeroFillBuffers)
                {
                    for (var i = 0; i < this.bufferPosition; i++)
                    {
                        // Fully zero fill buffers that we fully used
                        Array.Clear(Buffers[i], 0, Buffers[i].Length);
                    }
                    // Partial zero fill the final buffer
                    Array.Clear(Buffers[this.bufferPosition], 0, this.bufferPosition);
                }

                if (this.bufferPosition > 0 || !reuseFirst)
                {
                    var offset = reuseFirst ? 1 : 0;
                    // Recycle all but the first buffer
                    allocator.RecycleByteBlocks(Buffers, offset, 1 + this.bufferPosition);


                    for (var i = offset; i < (this.bufferPosition + 1); i++)
                    {
                        Buffers[i] = new byte[10];
                    }
                }
                if (reuseFirst)
                {
                    // Re-use the first buffer
                    this.bufferPosition = 0;
                    BytePosition = 0;
                    ByteOffset = 0;
                    this.Buffer = this.Buffers[0];
                }
                else
                {
                    this.bufferPosition = -1;
                    BytePosition = BYTE_BLOCK_SIZE;
                    ByteOffset = -BYTE_BLOCK_SIZE;
                    Buffer = null;
                   
                }
            }
        }

        /// <summary>
        /// Advances the pool to its next buffer. this method should be called once
        /// after the constructor to initialize the pool. In contrast to the
        /// constructor a <seealso cref="ByteBlockPool#reset()" /> call will advance the pool to
        /// its first buffer immediately.
        /// </summary>
        public void NextBuffer()
        {
            if (1 + this.bufferPosition == Buffers.Length)
            {
                var newBuffers =
                    new byte[ArrayUtil.Oversize(Buffers.Length + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
                Array.Copy(Buffers, 0, newBuffers, 0, Buffers.Length);
                
                this.Buffers = newBuffers;
            }
           this.Buffer = Buffers[1 + this.bufferPosition] = allocator.ByteBlock;
            this.bufferPosition++;

            BytePosition = 0;
            ByteOffset += BYTE_BLOCK_SIZE;
        }

        /// <summary>
        /// Allocates a new slice with the given size.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>System.Int32.</returns>
        /// <seealso cref="ByteBlockPool.FIRST_LEVEL_SIZE" />
        public int NewSlice(int size)
        {
            if (BytePosition > BYTE_BLOCK_SIZE - size)
            {
                NextBuffer();
            }
            var position = BytePosition;
            BytePosition += size;
            Buffer[BytePosition - 1] = 16;
            return position;
        }

        // Size of each slice.  These arrays should be at most 16
        // elements (index is encoded with 4 bits).  First array
        // is just a compact way to encode X+1 with a max.  Second
        // array is the length of each slice, ie first slice is 5
        // bytes, next slice is 14 bytes, etc.

        /// <summary>
        /// Creates a new byte slice with the given starting size and
        /// returns the slice's offset in the pool.
        /// </summary>
        /// <param name="slice">The slice.</param>
        /// <param name="startingSize">The initial size.</param>
        /// <returns>The offset of slice in the pool.</returns>
        public int AllocateSlice(byte[] slice, int startingSize)
        {
            var level = slice[startingSize] & 0xF;
            var newLevel = NEXT_LEVEL_ARRAY[level];
            var newSize = LEVEL_SIZE_ARRAY[newLevel];

            // Maybe allocate another block
            if (BytePosition > BYTE_BLOCK_SIZE - newSize)
            {
                NextBuffer();
            }

            var newOffset = BytePosition;
            var offset = newOffset + ByteOffset;
            BytePosition += newSize;

            // Copy forward the past 3 bytes (which we are about
            // to overwrite with the forwarding address):
            Buffer[newOffset] = slice[startingSize - 3];
            Buffer[newOffset + 1] = slice[startingSize - 2];
            Buffer[newOffset + 2] = slice[startingSize - 1];

            // Write forwarding address at end of last slice:
            slice[startingSize - 3] = (byte) ((int) ((uint) offset >> 24));
            slice[startingSize - 2] = (byte) ((int) ((uint) offset >> 16));
            slice[startingSize - 1] = (byte) ((int) ((uint) offset >> 8));
            slice[startingSize] = (byte) offset;

            // Write new level:
            Buffer[BytePosition - 1] = (byte) (16 | newLevel);

            return newOffset + 3;
        }


        /// <summary>
        /// Fill in a BytesRef from term's length & bytes encoded in
        ///  byte block
        /// </summary>
        /// <param name="term">The term.</param>
        /// <param name="textStart">The text start.</param>
        public void SetBytesRef(BytesRef term, int textStart)
        {
            var bytes = term.Bytes = Buffers[textStart >> BYTE_BLOCK_SHIFT];
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
            Debug.Assert(term.Length >= 0);
        }

        /// <summary>
        /// Appends the bytes in the provided <seealso cref="BytesRef" /> at
        /// the current position.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public void Append(BytesRef bytes)
        {
            var length = bytes.Length;
            if (length == 0)
            {
                return;
            }
            var offset = bytes.Offset;
            var overflow = (length + BytePosition) - BYTE_BLOCK_SIZE;
            do
            {
                if (overflow <= 0)
                {
                    if (Buffer == null)
                        this.Buffer = this.Buffers[this.bufferPosition];
                    
                    Array.Copy(bytes.Bytes, offset, Buffer, BytePosition, length);
                    BytePosition += length;
                    
                    break;
                }

                var bytesToCopy = length - overflow;
                if (bytesToCopy > 0)
                {
                    Array.Copy(bytes.Bytes, offset, this.Buffer, this.BytePosition, bytesToCopy);
                    offset += bytesToCopy;
                    length -= bytesToCopy;
                }
                this.NextBuffer();
                overflow = overflow - BYTE_BLOCK_SIZE;
            } while (true);
        }

        /// <summary>
        /// Reads bytes bytes out of the pool starting at the given offset with the given
        /// length into the given byte array at offset <tt>off</tt>.
        /// 
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <p>Note: this method allows to copy across block boundaries.</p>
        ///     </para>
        /// </remarks>
        /// <param name="offset">The offset.</param>
        /// <param name="bytes">The bytes.</param>
        /// <param name="bytesOffset">The bytes offset.</param>
        /// <param name="length">The length.</param>
        public void ReadBytes(long offset, byte[] bytes, int bytesOffset, int length)
        {
            if (length == 0)
            {
                return;
            }

            int bytesLength = length,
                bufferIndex = (int) (offset >> BYTE_BLOCK_SHIFT),
                pos = (int) (offset & BYTE_BLOCK_MASK),
                overflow = (pos + length) - BYTE_BLOCK_SIZE;

            var buffer = Buffers[bufferIndex];


            do
            {
                if (overflow <= 0)
                {
                    Array.Copy(buffer, pos, bytes, bytesOffset, bytesLength);
                    break;
                }

                var bytesToCopy = length - overflow;
                Array.Copy(buffer, pos, bytes, bytesOffset, bytesToCopy);
                pos = 0;
                bytesLength -= bytesToCopy;
                bytesOffset += bytesToCopy;
                buffer = this.Buffers[++bufferIndex];
                overflow = overflow - BYTE_BLOCK_SIZE;
            } while (true);
        }

        /// <summary>
        /// Abstract class for allocating and freeing byte
        /// blocks.
        /// </summary>
        public abstract class Allocator
        {
            protected internal readonly int BlockSize;

            /// <summary>
            /// Initializes a new instance of the <see cref="Allocator"/> class.
            /// </summary>
            /// <param name="blockSize">Size of the block.</param>
            protected Allocator(int blockSize)
            {
                this.BlockSize = blockSize;
            }

            /// <summary>
            /// Gets the byte block.
            /// </summary>
            /// <value>The byte block.</value>
            public virtual byte[] ByteBlock
            {
                get { return new byte[BlockSize]; }
            }

            /// <summary>
            /// Recycles the byte blocks.
            /// </summary>
            /// <param name="blocks">The blocks.</param>
            /// <param name="start">The start.</param>
            /// <param name="end">The end.</param>
            public abstract void RecycleByteBlocks(byte[][] blocks, int start, int end);

            /// <summary>
            /// Recycles the byte blocks.
            /// </summary>
            /// <param name="blocks">The blocks.</param>
            public virtual void RecycleByteBlocks(List<byte[]> blocks)
            {
                var b = blocks.ToArray();
                RecycleByteBlocks(b, 0, b.Length);
            }
        }

        /// <summary>
        ///     A simple <seealso cref="Allocator" /> that never recycles.
        /// </summary>
        public sealed class DirectAllocator : Allocator
        {
            public DirectAllocator()
                : this(BYTE_BLOCK_SIZE)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Allocator" /> class.
            /// </summary>
            /// <param name="blockSize">Size of the block.</param>
            public DirectAllocator(int blockSize)
                : base(blockSize)
            {
            }

            /// <summary>
            /// Recycles the byte blocks.
            /// </summary>
            /// <param name="blocks">The blocks.</param>
            /// <param name="start">The start.</param>
            /// <param name="end">The end.</param>
            public override void RecycleByteBlocks(byte[][] blocks, int start, int end)
            {
            }
        }

        /// <summary>
        ///     A simple <seealso cref="Allocator" /> that never recycles, but
        ///     tracks how much total RAM is in use.
        /// </summary>
        public class DirectTrackingAllocator : Allocator
        {
            internal readonly Counter BytesUsed;

            /// <summary>
            /// Initializes a new instance of the <see cref="DirectTrackingAllocator"/> class.
            /// </summary>
            /// <param name="bytesUsed">The bytes used.</param>
            public DirectTrackingAllocator(Counter bytesUsed)
                : this(BYTE_BLOCK_SIZE, bytesUsed)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="DirectTrackingAllocator"/> class.
            /// </summary>
            /// <param name="blockSize">Size of the block.</param>
            /// <param name="bytesUsed">The bytes used.</param>
            public DirectTrackingAllocator(int blockSize, Counter bytesUsed)
                : base(blockSize)
            {
                this.BytesUsed = bytesUsed;
            }

            /// <summary>
            /// Gets the byte block.
            /// </summary>
            /// <value>The byte block.</value>
            public override byte[] ByteBlock
            {
                get
                {
                    BytesUsed.AddAndGet(BlockSize);
                    return new byte[BlockSize];
                }
            }

            /// <summary>
            /// Recycles the byte blocks.
            /// </summary>
            /// <param name="blocks">The blocks.</param>
            /// <param name="start">The start.</param>
            /// <param name="end">The end.</param>
            public override void RecycleByteBlocks(byte[][] blocks, int start, int end)
            {
                BytesUsed.AddAndGet(-((end - start)*BlockSize));
                for (var i = start; i < end; i++)
                {
                    blocks[i] = null;
                }
            }
        }
    }
}