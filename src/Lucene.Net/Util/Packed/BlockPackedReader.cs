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
    /// Provides random access to a stream written with <see cref="BlockPackedWriter"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class BlockPackedReader : Int64Values
    {
        private readonly int blockShift, blockMask;
        private readonly long valueCount;
        private readonly long[] minValues;
        private readonly PackedInt32s.Reader[] subReaders;

        /// <summary>
        /// Sole constructor. </summary>
        public BlockPackedReader(IndexInput @in, int packedIntsVersion, int blockSize, long valueCount, bool direct)
        {
            this.valueCount = valueCount;
            blockShift = PackedInt32s.CheckBlockSize(blockSize, AbstractBlockPackedWriter.MIN_BLOCK_SIZE, AbstractBlockPackedWriter.MAX_BLOCK_SIZE);
            blockMask = blockSize - 1;
            int numBlocks = PackedInt32s.NumBlocks(valueCount, blockSize);
            long[] minValues = null;
            subReaders = new PackedInt32s.Reader[numBlocks];
            for (int i = 0; i < numBlocks; ++i)
            {
                int token = @in.ReadByte() & 0xFF;
                int bitsPerValue = token.TripleShift(AbstractBlockPackedWriter.BPV_SHIFT);
                if (bitsPerValue > 64)
                {
                    throw new IOException("Corrupted");
                }
                if ((token & AbstractBlockPackedWriter.MIN_VALUE_EQUALS_0) == 0)
                {
                    if (minValues is null)
                    {
                        minValues = new long[numBlocks];
                    }
                    minValues[i] = BlockPackedReaderIterator.ZigZagDecode(1L + BlockPackedReaderIterator.ReadVInt64(@in));
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
            this.minValues = minValues;
        }

        public override long Get(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < valueCount);
            int block = (int)(index.TripleShift(blockShift));
            int idx = (int)(index & blockMask);
            return (minValues is null ? 0 : minValues[block]) + subReaders[block].Get(idx);
        }

        /// <summary>
        /// Returns approximate RAM bytes used. </summary>
        public long RamBytesUsed()
        {
            long size = 0;
            foreach (PackedInt32s.Reader reader in subReaders)
            {
                size += reader.RamBytesUsed();
            }
            return size;
        }
    }
}