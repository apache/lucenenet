using Lucene.Net.Store;
using Lucene.Net.Support;
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
    /// Provides random access to a stream written with
    /// <seealso cref="MonotonicBlockPackedWriter"/>.
    /// @lucene.internal
    /// </summary>
    public sealed class MonotonicBlockPackedReader : LongValues
    {
        private readonly int blockShift, blockMask;
        private readonly long valueCount;
        private readonly long[] minValues;
        private readonly float[] averages;
        private readonly PackedInts.Reader[] subReaders;

        /// <summary>
        /// Sole constructor. </summary>
        public MonotonicBlockPackedReader(IndexInput @in, int packedIntsVersion, int blockSize, long valueCount, bool direct)
        {
            this.valueCount = valueCount;
            blockShift = PackedInts.CheckBlockSize(blockSize, AbstractBlockPackedWriter.MIN_BLOCK_SIZE, AbstractBlockPackedWriter.MAX_BLOCK_SIZE);
            blockMask = blockSize - 1;
            int numBlocks = PackedInts.NumBlocks(valueCount, blockSize);
            minValues = new long[numBlocks];
            averages = new float[numBlocks];
            subReaders = new PackedInts.Reader[numBlocks];
            for (int i = 0; i < numBlocks; ++i)
            {
                minValues[i] = @in.ReadVLong();
                averages[i] = Number.IntBitsToFloat(@in.ReadInt());
                int bitsPerValue = @in.ReadVInt();
                if (bitsPerValue > 64)
                {
                    throw new Exception("Corrupted");
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
        }

        public override long Get(long index)
        {
            Debug.Assert(index >= 0 && index < valueCount);
            int block = (int)((long)((ulong)index >> blockShift));
            int idx = (int)(index & blockMask);
            return minValues[block] + (long)(idx * averages[block]) + BlockPackedReaderIterator.ZigZagDecode(subReaders[block].Get(idx));
        }

        /// <summary>
        /// Returns the number of values </summary>
        public long Size // LUCENENET TODO: rename Count
        {
            get { return valueCount; }
        }

        /// <summary>
        /// Returns the approximate RAM bytes used </summary>
        public long RamBytesUsed()
        {
            long sizeInBytes = 0;
            sizeInBytes += RamUsageEstimator.SizeOf(minValues);
            sizeInBytes += RamUsageEstimator.SizeOf(averages);
            foreach (PackedInts.Reader reader in subReaders)
            {
                sizeInBytes += reader.RamBytesUsed();
            }
            return sizeInBytes;
        }
    }
}