using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.IO;
using System.Runtime.CompilerServices;

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
    /// Reader for sequences of <see cref="long"/>s written with <see cref="BlockPackedWriter"/>. 
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <seealso cref="BlockPackedWriter"/>
    public sealed class BlockPackedReaderIterator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ZigZagDecode(long n)
        {
            return ((n.TripleShift(1)) ^ -(n & 1));
        }

        // same as DataInput.ReadVInt64 but supports negative values
        /// <summary>
        /// NOTE: This was readVLong() in Lucene.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ReadVInt64(DataInput @in)
        {
            byte b = @in.ReadByte();
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return b;
            }
            long i = b & 0x7FL;
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 7;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 14;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 21;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 28;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 35;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 42;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0x7FL) << 49;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = @in.ReadByte();
            i |= (b & 0xFFL) << 56;
            return i;
        }

        internal DataInput @in;
        internal readonly int packedIntsVersion;
        internal long valueCount;
        internal readonly int blockSize;
        internal readonly long[] values;
        internal readonly Int64sRef valuesRef;
        internal byte[] blocks;
        internal int off;
        internal long ord;

        /// <summary>
        /// Sole constructor. </summary>
        /// <param name="blockSize"> The number of values of a block, must be equal to the
        ///                  block size of the <see cref="BlockPackedWriter"/> which has
        ///                  been used to write the stream. </param>
        public BlockPackedReaderIterator(DataInput @in, int packedIntsVersion, int blockSize, long valueCount)
        {
            PackedInt32s.CheckBlockSize(blockSize, AbstractBlockPackedWriter.MIN_BLOCK_SIZE, AbstractBlockPackedWriter.MAX_BLOCK_SIZE);
            this.packedIntsVersion = packedIntsVersion;
            this.blockSize = blockSize;
            this.values = new long[blockSize];
            this.valuesRef = new Int64sRef(this.values, 0, 0);
            Reset(@in, valueCount);
        }

        /// <summary>
        /// Reset the current reader to wrap a stream of <paramref name="valueCount"/>
        /// values contained in <paramref name="in"/>. The block size remains unchanged.
        /// </summary>
        public void Reset(DataInput @in, long valueCount)
        {
            this.@in = @in;
            if (Debugging.AssertsEnabled) Debugging.Assert(valueCount >= 0);
            this.valueCount = valueCount;
            off = blockSize;
            ord = 0;
        }

        /// <summary>
        /// Skip exactly <paramref name="count"/> values. </summary>
        public void Skip(long count)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(count >= 0);
            if (ord + count > valueCount || ord + count < 0)
            {
                throw EOFException.Create();
            }

            // 1. skip buffered values
            int skipBuffer = (int)Math.Min(count, blockSize - off);
            off += skipBuffer;
            ord += skipBuffer;
            count -= skipBuffer;
            if (count == 0L)
            {
                return;
            }

            // 2. skip as many blocks as necessary
            if (Debugging.AssertsEnabled) Debugging.Assert(off == blockSize);
            while (count >= blockSize)
            {
                int token = @in.ReadByte() & 0xFF;
                int bitsPerValue = token.TripleShift(AbstractBlockPackedWriter.BPV_SHIFT);
                if (bitsPerValue > 64)
                {
                    throw new IOException("Corrupted");
                }
                if ((token & AbstractBlockPackedWriter.MIN_VALUE_EQUALS_0) == 0)
                {
                    ReadVInt64(@in);
                }
                long blockBytes = PackedInt32s.Format.PACKED.ByteCount(packedIntsVersion, blockSize, bitsPerValue);
                SkipBytes(blockBytes);
                ord += blockSize;
                count -= blockSize;
            }
            if (count == 0L)
            {
                return;
            }

            // 3. skip last values
            if (Debugging.AssertsEnabled) Debugging.Assert(count < blockSize);
            Refill();
            ord += count;
            off += (int)count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipBytes(long count)
        {
            if (@in is IndexInput input)
            {
                input.Seek(input.Position + count); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
            else
            {
                if (blocks is null)
                {
                    blocks = new byte[blockSize];
                }
                long skipped = 0;
                while (skipped < count)
                {
                    int toSkip = (int)Math.Min(blocks.Length, count - skipped);
                    @in.ReadBytes(blocks, 0, toSkip);
                    skipped += toSkip;
                }
            }
        }

        /// <summary>
        /// Read the next value. </summary>
        public long Next()
        {
            if (ord == valueCount)
            {
                throw EOFException.Create();
            }
            if (off == blockSize)
            {
                Refill();
            }
            long value = values[off++];
            ++ord;
            return value;
        }

        /// <summary>
        /// Read between <c>1</c> and <paramref name="count"/> values. </summary>
        public Int64sRef Next(int count)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(count > 0);
            if (ord == valueCount)
            {
                throw EOFException.Create();
            }
            if (off == blockSize)
            {
                Refill();
            }

            count = Math.Min(count, blockSize - off);
            count = (int)Math.Min(count, valueCount - ord);

            valuesRef.Offset = off;
            valuesRef.Length = count;
            off += count;
            ord += count;
            return valuesRef;
        }

        private void Refill()
        {
            int token = @in.ReadByte() & 0xFF;
            bool minEquals0 = (token & AbstractBlockPackedWriter.MIN_VALUE_EQUALS_0) != 0;
            int bitsPerValue = token.TripleShift(AbstractBlockPackedWriter.BPV_SHIFT);
            if (bitsPerValue > 64)
            {
                throw new IOException("Corrupted");
            }
            long minValue = minEquals0 ? 0L : ZigZagDecode(1L + ReadVInt64(@in));
            if (Debugging.AssertsEnabled) Debugging.Assert(minEquals0 || minValue != 0);

            if (bitsPerValue == 0)
            {
                Arrays.Fill(values, minValue);
            }
            else
            {
                PackedInt32s.IDecoder decoder = PackedInt32s.GetDecoder(PackedInt32s.Format.PACKED, packedIntsVersion, bitsPerValue);
                int iterations = blockSize / decoder.ByteValueCount;
                int blocksSize = iterations * decoder.ByteBlockCount;
                if (blocks is null || blocks.Length < blocksSize)
                {
                    blocks = new byte[blocksSize];
                }

                int valueCount = (int)Math.Min(this.valueCount - ord, blockSize);
                int blocksCount = (int)PackedInt32s.Format.PACKED.ByteCount(packedIntsVersion, valueCount, bitsPerValue);
                @in.ReadBytes(blocks, 0, blocksCount);

                decoder.Decode(blocks, 0, values, 0, iterations);

                if (minValue != 0)
                {
                    for (int i = 0; i < valueCount; ++i)
                    {
                        values[i] += minValue;
                    }
                }
            }
            off = 0;
        }

        /// <summary>
        /// Return the offset of the next value to read. </summary>
        public long Ord => ord;
    }
}