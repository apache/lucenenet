using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using System;
using System.IO;

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
    /// <see cref="MonotonicBlockPackedWriter"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class MonotonicBlockPackedReader : Int64Values
    {
        private readonly int blockShift, blockMask;
        private readonly long valueCount;
        private readonly long[] minValues;
        private readonly float[] averages;
        private readonly PackedInt32s.Reader[] subReaders;

        /// <summary>
        /// Sole constructor. </summary>
        public MonotonicBlockPackedReader(IndexInput @in, int packedIntsVersion, int blockSize, long valueCount, bool direct)
        {
            this.valueCount = valueCount;
            blockShift = PackedInt32s.CheckBlockSize(blockSize, AbstractBlockPackedWriter.MIN_BLOCK_SIZE, AbstractBlockPackedWriter.MAX_BLOCK_SIZE);
            blockMask = blockSize - 1;
            int numBlocks = PackedInt32s.NumBlocks(valueCount, blockSize);
            minValues = new long[numBlocks];
            averages = new float[numBlocks];
            subReaders = new PackedInt32s.Reader[numBlocks];
            for (int i = 0; i < numBlocks; ++i)
            {
                minValues[i] = @in.ReadVInt64();
                averages[i] = J2N.BitConversion.Int32BitsToSingle(@in.ReadInt32());
                int bitsPerValue = @in.ReadVInt32();
                if (bitsPerValue > 64)
                {
                    throw new IOException("Corrupted");
                }
                if (bitsPerValue == 0)
                {
                    subReaders[i] = new PackedInt32s.NullReader(blockSize);
                }
                else
                {
                    int size = (int)Math.Min(blockSize, valueCount - (long)i * blockSize);
                    if (direct)
                    {
                        long pointer = @in.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        subReaders[i] = PackedInt32s.GetDirectReaderNoHeader(@in, PackedInt32s.Format.PACKED, packedIntsVersion, size, bitsPerValue);
                        @in.Seek(pointer + PackedInt32s.Format.PACKED.ByteCount(packedIntsVersion, size, bitsPerValue));
                    }
                    else
                    {
                        subReaders[i] = PackedInt32s.GetReaderNoHeader(@in, PackedInt32s.Format.PACKED, packedIntsVersion, size, bitsPerValue);
                    }
                }
            }
        }

        public override long Get(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < valueCount);
            int block = (int)(index.TripleShift(blockShift));
            int idx = (int)(index & blockMask);
            // LUCENENET NOTE: IMPORTANT: The cast to float is critical here for it to work in x86
            return minValues[block] + (long)(float)(idx * averages[block]) + BlockPackedReaderIterator.ZigZagDecode(subReaders[block].Get(idx));
        }

        /// <summary>
        /// Returns the number of values.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public long Count => valueCount;

        /// <summary>
        /// Returns the approximate RAM bytes used. </summary>
        public long RamBytesUsed()
        {
            long sizeInBytes = 0;
            sizeInBytes += RamUsageEstimator.SizeOf(minValues);
            sizeInBytes += RamUsageEstimator.SizeOf(averages);
            foreach (PackedInt32s.Reader reader in subReaders)
            {
                sizeInBytes += reader.RamBytesUsed();
            }
            return sizeInBytes;
        }
    }
}