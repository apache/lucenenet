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

    using Allocator = Lucene.Net.Util.IntBlockPool.Allocator;

    /// <summary>
    /// A <seealso cref="Allocator"/> implementation that recycles unused int
    /// blocks in a buffer and reuses them in subsequent calls to
    /// <seealso cref="#getIntBlock()"/>.
    /// <p>
    /// Note: this class is not thread-safe
    /// </p>
    /// @lucene.internal
    /// </summary>
    public sealed class RecyclingIntBlockAllocator : Allocator // LUCENENET TODO: Rename RecyclingInt32BlockAllocator ?
    {
        private int[][] freeByteBlocks;
        private readonly int maxBufferedBlocks;
        private int freeBlocks = 0;
        private readonly Counter bytesUsed;
        public const int DEFAULT_BUFFERED_BLOCKS = 64;

        /// <summary>
        /// Creates a new <seealso cref="RecyclingIntBlockAllocator"/>
        /// </summary>
        /// <param name="blockSize">
        ///          the block size in bytes </param>
        /// <param name="maxBufferedBlocks">
        ///          maximum number of buffered int block </param>
        /// <param name="bytesUsed">
        ///          <seealso cref="Counter"/> reference counting internally allocated bytes </param>
        public RecyclingIntBlockAllocator(int blockSize, int maxBufferedBlocks, Counter bytesUsed)
            : base(blockSize)
        {
            freeByteBlocks = new int[maxBufferedBlocks][];
            this.maxBufferedBlocks = maxBufferedBlocks;
            this.bytesUsed = bytesUsed;
        }

        /// <summary>
        /// Creates a new <seealso cref="RecyclingIntBlockAllocator"/>.
        /// </summary>
        /// <param name="blockSize">
        ///          the size of each block returned by this allocator </param>
        /// <param name="maxBufferedBlocks">
        ///          maximum number of buffered int blocks </param>
        public RecyclingIntBlockAllocator(int blockSize, int maxBufferedBlocks)
            : this(blockSize, maxBufferedBlocks, Counter.NewCounter(false))
        {
        }

        /// <summary>
        /// Creates a new <seealso cref="RecyclingIntBlockAllocator"/> with a block size of
        /// <seealso cref="IntBlockPool#INT_BLOCK_SIZE"/>, upper buffered docs limit of
        /// <seealso cref="#DEFAULT_BUFFERED_BLOCKS"/> ({@value #DEFAULT_BUFFERED_BLOCKS}).
        ///
        /// </summary>
        public RecyclingIntBlockAllocator()
            : this(IntBlockPool.INT_BLOCK_SIZE, 64, Counter.NewCounter(false))
        {
        }

        public override int[] GetIntBlock() // LUCENENET TODO: Rename GetInt32Block() ?
        {
            if (freeBlocks == 0)
            {
                bytesUsed.AddAndGet(m_blockSize * RamUsageEstimator.NUM_BYTES_INT);
                return new int[m_blockSize];
            }
            int[] b = freeByteBlocks[--freeBlocks];
            freeByteBlocks[freeBlocks] = null;
            return b;
        }

        public override void RecycleIntBlocks(int[][] blocks, int start, int end) // LUCENENET TODO: Rename RecycleInt32Blocks ?
        {
            int numBlocks = Math.Min(maxBufferedBlocks - freeBlocks, end - start);
            int size = freeBlocks + numBlocks;
            if (size >= freeByteBlocks.Length)
            {
                int[][] newBlocks = new int[ArrayUtil.Oversize(size, RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
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
            bytesUsed.AddAndGet(-(end - stop) * (m_blockSize * RamUsageEstimator.NUM_BYTES_INT));
            Debug.Assert(bytesUsed.Get() >= 0);
        }

        /// <returns> the number of currently buffered blocks </returns>
        public int NumBufferedBlocks
        {
            get { return freeBlocks; }
        }

        /// <returns> the number of bytes currently allocated by this <seealso cref="Allocator"/> </returns>
        public long BytesUsed 
        {
            get { return bytesUsed.Get(); }
        }

        /// <returns> the maximum number of buffered byte blocks </returns>
        public int MaxBufferedBlocks
        {
            get { return maxBufferedBlocks; }
        }

        /// <summary>
        /// Removes the given number of int blocks from the buffer if possible.
        /// </summary>
        /// <param name="num">
        ///          the number of int blocks to remove </param>
        /// <returns> the number of actually removed buffers </returns>
        public int FreeBlocks(int num)
        {
            Debug.Assert(num >= 0, "free blocks must be >= 0 but was: " + num);
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
            bytesUsed.AddAndGet(-count * m_blockSize * RamUsageEstimator.NUM_BYTES_INT);
            Debug.Assert(bytesUsed.Get() >= 0);
            return count;
        }
    }
}