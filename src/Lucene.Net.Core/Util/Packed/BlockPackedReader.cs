using Lucene.Net.Store;
using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Packed
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
    /// Provides random access to a stream written with <seealso cref="BlockPackedWriter"/>.
    /// @lucene.internal
    /// </summary>
    public sealed class BlockPackedReader : LongValues
    {
        private readonly int blockShift, blockMask;
        private readonly long valueCount;
        private readonly long[] minValues;
        private readonly PackedInts.Reader[] subReaders;

        /// <summary>
        /// Sole constructor. </summary>
        public BlockPackedReader(IndexInput @in, int packedIntsVersion, int blockSize, long valueCount, bool direct)
        {
            this.valueCount = valueCount;
            blockShift = PackedInts.CheckBlockSize(blockSize, AbstractBlockPackedWriter.MIN_BLOCK_SIZE, AbstractBlockPackedWriter.MAX_BLOCK_SIZE);
            blockMask = blockSize - 1;
            int numBlocks = PackedInts.NumBlocks(valueCount, blockSize);
            long[] minValues = null;
            subReaders = new PackedInts.Reader[numBlocks];
            for (int i = 0; i < numBlocks; ++i)
            {
                int token = @in.ReadByte() & 0xFF;
                int bitsPerValue = (int)((uint)token >> AbstractBlockPackedWriter.BPV_SHIFT);
                if (bitsPerValue > 64)
                {
                    throw new Exception("Corrupted");
                }
                if ((token & AbstractBlockPackedWriter.MIN_VALUE_EQUALS_0) == 0)
                {
                    if (minValues == null)
                    {
                        minValues = new long[numBlocks];
                    }
                    minValues[i] = BlockPackedReaderIterator.ZigZagDecode(1L + BlockPackedReaderIterator.ReadVLong(@in));
                }
                if (bitsPerValue == 0)
                {
                    subReaders[i] = new PackedInts.NullReader(blockSize);
                }
                else
                {
                    int size = (int)Math.Min(blockSize, valueCount - (long)i * blockSize);
                    if (direct)
                    {
                        long pointer = @in.FilePointer;
                        subReaders[i] = PackedInts.GetDirectReaderNoHeader(@in, PackedInts.Format.PACKED, packedIntsVersion, size, bitsPerValue);
                        @in.Seek(pointer + PackedInts.Format.PACKED.ByteCount(packedIntsVersion, size, bitsPerValue));
                    }
                    else
                    {
                        subReaders[i] = PackedInts.GetReaderNoHeader(@in, PackedInts.Format.PACKED, packedIntsVersion, size, bitsPerValue);
                    }
                }
            }
            this.minValues = minValues;
        }

        public override long Get(long index)
        {
            Debug.Assert(index >= 0 && index < valueCount);
            int block = (int)((long)((ulong)index >> blockShift));
            int idx = (int)(index & blockMask);
            return (minValues == null ? 0 : minValues[block]) + subReaders[block].Get(idx);
        }

        /// <summary>
        /// Returns approximate RAM bytes used </summary>
        public long RamBytesUsed()
        {
            long size = 0;
            foreach (PackedInts.Reader reader in subReaders)
            {
                size += reader.RamBytesUsed();
            }
            return size;
        }
    }
}