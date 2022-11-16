using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;

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
    /// A <see cref="ByteBlockPool.Allocator"/> implementation that recycles unused byte
    /// blocks in a buffer and reuses them in subsequent calls to
    /// <see cref="GetByteBlock()"/>.
    /// <para>
    /// Note: this class is not thread-safe.
    /// </para>
    /// @lucene.internal
    /// </summary>
    public sealed class RecyclingByteBlockAllocator : ByteBlockPool.Allocator
    {
        private byte[][] freeByteBlocks;
        private readonly int maxBufferedBlocks;
        private int freeBlocks = 0;
        private readonly Counter bytesUsed;
        public const int DEFAULT_BUFFERED_BLOCKS = 64;

        /// <summary>
        /// Creates a new <see cref="RecyclingByteBlockAllocator"/>
        /// </summary>
        /// <param name="blockSize">
        ///          The block size in bytes. </param>
        /// <param name="maxBufferedBlocks">
        ///          Maximum number of buffered byte block. </param>
        /// <param name="bytesUsed">
        ///          <see cref="Counter"/> reference counting internally allocated bytes. </param>
        public RecyclingByteBlockAllocator(int blockSize, int maxBufferedBlocks, Counter bytesUsed)
            : base(blockSize)
        {
            freeByteBlocks = new byte[maxBufferedBlocks][];
            this.maxBufferedBlocks = maxBufferedBlocks;
            this.bytesUsed = bytesUsed;
        }

        /// <summary>
        /// Creates a new <see cref="RecyclingByteBlockAllocator"/>.
        /// </summary>
        /// <param name="blockSize">
        ///          The block size in bytes. </param>
        /// <param name="maxBufferedBlocks">
        ///          Maximum number of buffered byte block. </param>
        public RecyclingByteBlockAllocator(int blockSize, int maxBufferedBlocks)
            : this(blockSize, maxBufferedBlocks, Counter.NewCounter(false))
        {
        }

        /// <summary>
        /// Creates a new <see cref="RecyclingByteBlockAllocator"/> with a block size of
        /// <see cref="ByteBlockPool.BYTE_BLOCK_SIZE"/>, upper buffered docs limit of
        /// <see cref="DEFAULT_BUFFERED_BLOCKS"/> (64).
        /// </summary>
        public RecyclingByteBlockAllocator()
            : this(ByteBlockPool.BYTE_BLOCK_SIZE, 64, Counter.NewCounter(false))
        {
        }

        public override byte[] GetByteBlock() 
        {
            if (freeBlocks == 0)
            {
                bytesUsed.AddAndGet(m_blockSize);
                return new byte[m_blockSize];
            }
            var b = freeByteBlocks[--freeBlocks];
            freeByteBlocks[freeBlocks] = null;
            return b;
        }

        public override void RecycleByteBlocks(byte[][] blocks, int start, int end)
        {
            int numBlocks = Math.Min(maxBufferedBlocks - freeBlocks, end - start);
            int size = freeBlocks + numBlocks;
            if (size >= freeByteBlocks.Length)
            {
                var newBlocks = new byte[ArrayUtil.Oversize(size, RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
                Arrays.Copy(freeByteBlocks, 0, newBlocks, 0, freeBlocks);
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
            bytesUsed.AddAndGet(-(end - stop) * m_blockSize);
            if (Debugging.AssertsEnabled) Debugging.Assert(bytesUsed >= 0);
        }

        /// <returns> The number of currently buffered blocks. </returns>
        public int NumBufferedBlocks => freeBlocks;

        /// <returns> The number of bytes currently allocated by this <see cref="ByteBlockPool.Allocator"/>. </returns>
        public long BytesUsed => bytesUsed;

        /// <returns> The maximum number of buffered byte blocks. </returns>
        public int MaxBufferedBlocks => maxBufferedBlocks;

        /// <summary>
        /// Removes the given number of byte blocks from the buffer if possible.
        /// </summary>
        /// <param name="num">
        ///          The number of byte blocks to remove. </param>
        /// <returns> The number of actually removed buffers. </returns>
        public int FreeBlocks(int num)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(num >= 0, "free blocks must be >= 0 but was: {0}", num);
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
            bytesUsed.AddAndGet(-count * m_blockSize);
            if (Debugging.AssertsEnabled) Debugging.Assert(bytesUsed >= 0);
            return count;
        }
    }
}