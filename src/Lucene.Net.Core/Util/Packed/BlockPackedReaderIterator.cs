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

    using DataInput = Lucene.Net.Store.DataInput;
    using IndexInput = Lucene.Net.Store.IndexInput;

    /// <summary>
    /// Reader for sequences of longs written with <seealso cref="BlockPackedWriter"/>. </summary>
    /// <seealso cref= BlockPackedWriter
    /// @lucene.internal </seealso>
    public sealed class BlockPackedReaderIterator
    {
        internal static long ZigZagDecode(long n)
        {
            return (((long)((ulong)n >> 1)) ^ -(n & 1));
        }

        // same as DataInput.readVLong but supports negative values
        internal static long ReadVLong(DataInput @in) // LUCENENET TODO: rename to ReadVInt64 ?
        {
            byte b = @in.ReadByte();
            if ((sbyte)b >= 0)
            {
                return b;
            }
            long i = b & 0x7FL;
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 7;
            if ((sbyte)b >= 0)
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 14;
            if ((sbyte)b >= 0)
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 21;
            if ((sbyte)b >= 0)
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 28;
            if ((sbyte)b >= 0)
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 35;
            if ((sbyte)b >= 0)
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 42;
            if ((sbyte)b >= 0)
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 49;
            if ((sbyte)b >= 0)
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0xFFL) << 56;
            return i;
        }

        internal DataInput @in;
        internal readonly int PackedIntsVersion;
        internal long ValueCount;
        internal readonly int BlockSize;
        internal readonly long[] Values;
        internal readonly LongsRef ValuesRef;
        internal byte[] Blocks;
        internal int Off;
        internal long Ord_Renamed;

        /// <summary>
        /// Sole constructor. </summary>
        /// <param name="blockSize"> the number of values of a block, must be equal to the
        ///                  block size of the <seealso cref="BlockPackedWriter"/> which has
        ///                  been used to write the stream </param>
        public BlockPackedReaderIterator(DataInput @in, int packedIntsVersion, int blockSize, long valueCount)
        {
            PackedInts.CheckBlockSize(blockSize, AbstractBlockPackedWriter.MIN_BLOCK_SIZE, AbstractBlockPackedWriter.MAX_BLOCK_SIZE);
            this.PackedIntsVersion = packedIntsVersion;
            this.BlockSize = blockSize;
            this.Values = new long[blockSize];
            this.ValuesRef = new LongsRef(this.Values, 0, 0);
            Reset(@in, valueCount);
        }

        /// <summary>
        /// Reset the current reader to wrap a stream of <code>valueCount</code>
        /// values contained in <code>in</code>. The block size remains unchanged.
        /// </summary>
        public void Reset(DataInput @in, long valueCount)
        {
            this.@in = @in;
            Debug.Assert(valueCount >= 0);
            this.ValueCount = valueCount;
            Off = BlockSize;
            Ord_Renamed = 0;
        }

        /// <summary>
        /// Skip exactly <code>count</code> values. </summary>
        public void Skip(long count)
        {
            Debug.Assert(count >= 0);
            if (Ord_Renamed + count > ValueCount || Ord_Renamed + count < 0)
            {
                throw new System.IO.EndOfStreamException();
            }

            // 1. skip buffered values
            int skipBuffer = (int)Math.Min(count, BlockSize - Off);
            Off += skipBuffer;
            Ord_Renamed += skipBuffer;
            count -= skipBuffer;
            if (count == 0L)
            {
                return;
            }

            // 2. skip as many blocks as necessary
            Debug.Assert(Off == BlockSize);
            while (count >= BlockSize)
            {
                int token = @in.ReadByte() & 0xFF;
                int bitsPerValue = (int)((uint)token >> AbstractBlockPackedWriter.BPV_SHIFT);
                if (bitsPerValue > 64)
                {
                    throw new System.IO.IOException("Corrupted");
                }
                if ((token & AbstractBlockPackedWriter.MIN_VALUE_EQUALS_0) == 0)
                {
                    ReadVLong(@in);
                }
                long blockBytes = PackedInts.Format.PACKED.ByteCount(PackedIntsVersion, BlockSize, bitsPerValue);
                SkipBytes(blockBytes);
                Ord_Renamed += BlockSize;
                count -= BlockSize;
            }
            if (count == 0L)
            {
                return;
            }

            // 3. skip last values
            Debug.Assert(count < BlockSize);
            Refill();
            Ord_Renamed += count;
            Off += (int)count;
        }

        private void SkipBytes(long count)
        {
            if (@in is IndexInput)
            {
                IndexInput iin = (IndexInput)@in;
                iin.Seek(iin.FilePointer + count);
            }
            else
            {
                if (Blocks == null)
                {
                    Blocks = new byte[BlockSize];
                }
                long skipped = 0;
                while (skipped < count)
                {
                    int toSkip = (int)Math.Min(Blocks.Length, count - skipped);
                    @in.ReadBytes(Blocks, 0, toSkip);
                    skipped += toSkip;
                }
            }
        }

        /// <summary>
        /// Read the next value. </summary>
        public long Next()
        {
            if (Ord_Renamed == ValueCount)
            {
                throw new System.IO.EndOfStreamException();
            }
            if (Off == BlockSize)
            {
                Refill();
            }
            long value = Values[Off++];
            ++Ord_Renamed;
            return value;
        }

        /// <summary>
        /// Read between <tt>1</tt> and <code>count</code> values. </summary>
        public LongsRef Next(int count)
        {
            Debug.Assert(count > 0);
            if (Ord_Renamed == ValueCount)
            {
                throw new System.IO.EndOfStreamException();
            }
            if (Off == BlockSize)
            {
                Refill();
            }

            count = Math.Min(count, BlockSize - Off);
            count = (int)Math.Min(count, ValueCount - Ord_Renamed);

            ValuesRef.Offset = Off;
            ValuesRef.Length = count;
            Off += count;
            Ord_Renamed += count;
            return ValuesRef;
        }

        private void Refill()
        {
            int token = @in.ReadByte() & 0xFF;
            bool minEquals0 = (token & AbstractBlockPackedWriter.MIN_VALUE_EQUALS_0) != 0;
            int bitsPerValue = (int)((uint)token >> AbstractBlockPackedWriter.BPV_SHIFT);
            if (bitsPerValue > 64)
            {
                throw new System.IO.IOException("Corrupted");
            }
            long minValue = minEquals0 ? 0L : ZigZagDecode(1L + ReadVLong(@in));
            Debug.Assert(minEquals0 || minValue != 0);

            if (bitsPerValue == 0)
            {
                Arrays.Fill(Values, minValue);
            }
            else
            {
                PackedInts.IDecoder decoder = PackedInts.GetDecoder(PackedInts.Format.PACKED, PackedIntsVersion, bitsPerValue);
                int iterations = BlockSize / decoder.ByteValueCount;
                int blocksSize = iterations * decoder.ByteBlockCount;
                if (Blocks == null || Blocks.Length < blocksSize)
                {
                    Blocks = new byte[blocksSize];
                }

                int valueCount = (int)Math.Min(this.ValueCount - Ord_Renamed, BlockSize);
                int blocksCount = (int)PackedInts.Format.PACKED.ByteCount(PackedIntsVersion, valueCount, bitsPerValue);
                @in.ReadBytes(Blocks, 0, blocksCount);

                decoder.Decode(Blocks, 0, Values, 0, iterations);

                if (minValue != 0)
                {
                    for (int i = 0; i < valueCount; ++i)
                    {
                        Values[i] += minValue;
                    }
                }
            }
            Off = 0;
        }

        /// <summary>
        /// Return the offset of the next value to read. </summary>
        public long Ord() // LUCENENET TODO: make property ? check consistency
        {
            return Ord_Renamed;
        }
    }
}