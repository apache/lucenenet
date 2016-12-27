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
        private int[][] FreeByteBlocks;
        private readonly int MaxBufferedBlocks_Renamed;
        private int FreeBlocks_Renamed = 0;
        private readonly Counter BytesUsed_Renamed;
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
            FreeByteBlocks = new int[maxBufferedBlocks][];
            this.MaxBufferedBlocks_Renamed = maxBufferedBlocks;
            this.BytesUsed_Renamed = bytesUsed;
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
            if (FreeBlocks_Renamed == 0)
            {
                BytesUsed_Renamed.AddAndGet(BlockSize * RamUsageEstimator.NUM_BYTES_INT);
                return new int[BlockSize];
            }
            int[] b = FreeByteBlocks[--FreeBlocks_Renamed];
            FreeByteBlocks[FreeBlocks_Renamed] = null;
            return b;
        }

        public override void RecycleIntBlocks(int[][] blocks, int start, int end) // LUCENENET TODO: Rename RecycleInt32Blocks ?
        {
            int numBlocks = Math.Min(MaxBufferedBlocks_Renamed - FreeBlocks_Renamed, end - start);
            int size = FreeBlocks_Renamed + numBlocks;
            if (size >= FreeByteBlocks.Length)
            {
                int[][] newBlocks = new int[ArrayUtil.Oversize(size, RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
                Array.Copy(FreeByteBlocks, 0, newBlocks, 0, FreeBlocks_Renamed);
                FreeByteBlocks = newBlocks;
            }
            int stop = start + numBlocks;
            for (int i = start; i < stop; i++)
            {
                FreeByteBlocks[FreeBlocks_Renamed++] = blocks[i];
                blocks[i] = null;
            }
            for (int i = stop; i < end; i++)
            {
                blocks[i] = null;
            }
            BytesUsed_Renamed.AddAndGet(-(end - stop) * (BlockSize * RamUsageEstimator.NUM_BYTES_INT));
            Debug.Assert(BytesUsed_Renamed.Get() >= 0);
        }

        /// <returns> the number of currently buffered blocks </returns>
        public int NumBufferedBlocks() // LUCENENET TODO: make property
        {
            return FreeBlocks_Renamed;
        }

        /// <returns> the number of bytes currently allocated by this <seealso cref="Allocator"/> </returns>
        public long BytesUsed() // LUCENENET TODO: make property ?
        {
            return BytesUsed_Renamed.Get();
        }

        /// <returns> the maximum number of buffered byte blocks </returns>
        public int MaxBufferedBlocks() // LUCENENET TODO: make property
        {
            return MaxBufferedBlocks_Renamed;
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
            if (num > FreeBlocks_Renamed)
            {
                stop = 0;
                count = FreeBlocks_Renamed;
            }
            else
            {
                stop = FreeBlocks_Renamed - num;
                count = num;
            }
            while (FreeBlocks_Renamed > stop)
            {
                FreeByteBlocks[--FreeBlocks_Renamed] = null;
            }
            BytesUsed_Renamed.AddAndGet(-count * BlockSize * RamUsageEstimator.NUM_BYTES_INT);
            Debug.Assert(BytesUsed_Renamed.Get() >= 0);
            return count;
        }
    }
}