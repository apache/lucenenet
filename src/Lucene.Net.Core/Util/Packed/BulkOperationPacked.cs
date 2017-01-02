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
    /// Non-specialized <seealso cref="BulkOperation"/> for <seealso cref="PackedInts.Format#PACKED"/>.
    /// </summary>
    internal class BulkOperationPacked : BulkOperation
    {
        private readonly int BitsPerValue;
        private readonly int LongBlockCount_Renamed;
        private readonly int LongValueCount_Renamed;
        private readonly int ByteBlockCount_Renamed;
        private readonly int ByteValueCount_Renamed;
        private readonly long Mask;
        private readonly int IntMask;

        public BulkOperationPacked(int bitsPerValue)
        {
            this.BitsPerValue = bitsPerValue;
            Debug.Assert(bitsPerValue > 0 && bitsPerValue <= 64);
            int blocks = bitsPerValue;
            while ((blocks & 1) == 0)
            {
                blocks = (int)((uint)blocks >> 1);
            }
            this.LongBlockCount_Renamed = blocks;
            this.LongValueCount_Renamed = 64 * LongBlockCount_Renamed / bitsPerValue;
            int byteBlockCount = 8 * LongBlockCount_Renamed;
            int byteValueCount = LongValueCount_Renamed;
            while ((byteBlockCount & 1) == 0 && (byteValueCount & 1) == 0)
            {
                byteBlockCount = (int)((uint)byteBlockCount >> 1);
                byteValueCount = (int)((uint)byteValueCount >> 1);
            }
            this.ByteBlockCount_Renamed = byteBlockCount;
            this.ByteValueCount_Renamed = byteValueCount;
            if (bitsPerValue == 64)
            {
                this.Mask = ~0L;
            }
            else
            {
                this.Mask = (1L << bitsPerValue) - 1;
            }
            this.IntMask = (int)Mask;
            Debug.Assert(LongValueCount_Renamed * bitsPerValue == 64 * LongBlockCount_Renamed);
        }

        public override int LongBlockCount
        {
            get { return LongBlockCount_Renamed; }
        }

        public override int LongValueCount
        {
            get { return LongValueCount_Renamed; }
        }

        public override int ByteBlockCount
        {
            get { return ByteBlockCount_Renamed; }
        }

        public override int ByteValueCount
        {
            get { return ByteValueCount_Renamed; }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            int bitsLeft = 64;
            for (int i = 0; i < LongValueCount_Renamed * iterations; ++i)
            {
                bitsLeft -= BitsPerValue;
                if (bitsLeft < 0)
                {
                    values[valuesOffset++] = ((blocks[blocksOffset++] & ((1L << (BitsPerValue + bitsLeft)) - 1)) << -bitsLeft) | ((long)((ulong)blocks[blocksOffset] >> (64 + bitsLeft)));
                    bitsLeft += 64;
                }
                else
                {
                    values[valuesOffset++] = ((long)((ulong)blocks[blocksOffset] >> bitsLeft)) & Mask;
                }
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            long nextValue = 0L;
            int bitsLeft = BitsPerValue;
            for (int i = 0; i < iterations * ByteBlockCount_Renamed; ++i)
            {
                long bytes = blocks[blocksOffset++] & 0xFFL;
                if (bitsLeft > 8)
                {
                    // just buffer
                    bitsLeft -= 8;
                    nextValue |= bytes << bitsLeft;
                }
                else
                {
                    // flush
                    int bits = 8 - bitsLeft;
                    values[valuesOffset++] = nextValue | ((long)((ulong)bytes >> bits));
                    while (bits >= BitsPerValue)
                    {
                        bits -= BitsPerValue;
                        values[valuesOffset++] = ((long)((ulong)bytes >> bits)) & Mask;
                    }
                    // then buffer
                    bitsLeft = BitsPerValue - bits;
                    nextValue = (bytes & ((1L << bits) - 1)) << bitsLeft;
                }
            }
            Debug.Assert(bitsLeft == BitsPerValue);
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            if (BitsPerValue > 32)
            {
                throw new System.NotSupportedException("Cannot decode " + BitsPerValue + "-bits values into an int[]");
            }
            int bitsLeft = 64;
            for (int i = 0; i < LongValueCount_Renamed * iterations; ++i)
            {
                bitsLeft -= BitsPerValue;
                if (bitsLeft < 0)
                {
                    values[valuesOffset++] = (int)(((blocks[blocksOffset++] & ((1L << (BitsPerValue + bitsLeft)) - 1)) << -bitsLeft) | ((long)((ulong)blocks[blocksOffset] >> (64 + bitsLeft))));
                    bitsLeft += 64;
                }
                else
                {
                    values[valuesOffset++] = (int)(((long)((ulong)blocks[blocksOffset] >> bitsLeft)) & Mask);
                }
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            int nextValue = 0;
            int bitsLeft = BitsPerValue;
            for (int i = 0; i < iterations * ByteBlockCount_Renamed; ++i)
            {
                int bytes = blocks[blocksOffset++] & 0xFF;
                if (bitsLeft > 8)
                {
                    // just buffer
                    bitsLeft -= 8;
                    nextValue |= bytes << bitsLeft;
                }
                else
                {
                    // flush
                    int bits = 8 - bitsLeft;
                    values[valuesOffset++] = nextValue | ((int)((uint)bytes >> bits));
                    while (bits >= BitsPerValue)
                    {
                        bits -= BitsPerValue;
                        values[valuesOffset++] = ((int)((uint)bytes >> bits)) & IntMask;
                    }
                    // then buffer
                    bitsLeft = BitsPerValue - bits;
                    nextValue = (bytes & ((1 << bits) - 1)) << bitsLeft;
                }
            }
            Debug.Assert(bitsLeft == BitsPerValue);
        }

        public override void Encode(long[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations)
        {
            long nextBlock = 0;
            int bitsLeft = 64;
            for (int i = 0; i < LongValueCount_Renamed * iterations; ++i)
            {
                bitsLeft -= BitsPerValue;
                if (bitsLeft > 0)
                {
                    nextBlock |= values[valuesOffset++] << bitsLeft;
                }
                else if (bitsLeft == 0)
                {
                    nextBlock |= values[valuesOffset++];
                    blocks[blocksOffset++] = nextBlock;
                    nextBlock = 0;
                    bitsLeft = 64;
                } // bitsLeft < 0
                else
                {
                    nextBlock |= (long)((ulong)values[valuesOffset] >> -bitsLeft);
                    blocks[blocksOffset++] = nextBlock;
                    nextBlock = (values[valuesOffset++] & ((1L << -bitsLeft) - 1)) << (64 + bitsLeft);
                    bitsLeft += 64;
                }
            }
        }

        public override void Encode(int[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations)
        {
            long nextBlock = 0;
            int bitsLeft = 64;
            for (int i = 0; i < LongValueCount_Renamed * iterations; ++i)
            {
                bitsLeft -= BitsPerValue;
                if (bitsLeft > 0)
                {
                    nextBlock |= (values[valuesOffset++] & 0xFFFFFFFFL) << bitsLeft;
                }
                else if (bitsLeft == 0)
                {
                    nextBlock |= (values[valuesOffset++] & 0xFFFFFFFFL);
                    blocks[blocksOffset++] = nextBlock;
                    nextBlock = 0;
                    bitsLeft = 64;
                } // bitsLeft < 0
                else
                {
                    nextBlock |= (int)((uint)(values[valuesOffset] & 0xFFFFFFFFL) >> -bitsLeft);
                    blocks[blocksOffset++] = nextBlock;
                    nextBlock = (values[valuesOffset++] & ((1L << -bitsLeft) - 1)) << (64 + bitsLeft);
                    bitsLeft += 64;
                }
            }
        }

        public override void Encode(long[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations)
        {
            int nextBlock = 0;
            int bitsLeft = 8;
            for (int i = 0; i < ByteValueCount_Renamed * iterations; ++i)
            {
                long v = values[valuesOffset++];
                Debug.Assert(BitsPerValue == 64 || PackedInts.BitsRequired(v) <= BitsPerValue);
                if (BitsPerValue < bitsLeft)
                {
                    // just buffer
                    nextBlock |= (int)(v << (bitsLeft - BitsPerValue));
                    bitsLeft -= BitsPerValue;
                }
                else
                {
                    // flush as many blocks as possible
                    int bits = BitsPerValue - bitsLeft;
                    blocks[blocksOffset++] = (byte)(nextBlock | ((long)((ulong)v >> bits)));
                    while (bits >= 8)
                    {
                        bits -= 8;
                        blocks[blocksOffset++] = (byte)((long)((ulong)v >> bits));
                    }
                    // then buffer
                    bitsLeft = 8 - bits;
                    nextBlock = (int)((v & ((1L << bits) - 1)) << bitsLeft);
                }
            }
            Debug.Assert(bitsLeft == 8);
        }

        public override void Encode(int[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations)
        {
            int nextBlock = 0;
            int bitsLeft = 8;
            for (int i = 0; i < ByteValueCount_Renamed * iterations; ++i)
            {
                int v = values[valuesOffset++];
                Debug.Assert(PackedInts.BitsRequired(v & 0xFFFFFFFFL) <= BitsPerValue);
                if (BitsPerValue < bitsLeft)
                {
                    // just buffer
                    nextBlock |= v << (bitsLeft - BitsPerValue);
                    bitsLeft -= BitsPerValue;
                }
                else
                {
                    // flush as many blocks as possible
                    int bits = BitsPerValue - bitsLeft;
                    blocks[blocksOffset++] = (byte)(nextBlock | ((int)((uint)v >> bits)));
                    while (bits >= 8)
                    {
                        bits -= 8;
                        blocks[blocksOffset++] = (byte)((int)((uint)v >> bits));
                    }
                    // then buffer
                    bitsLeft = 8 - bits;
                    nextBlock = (v & ((1 << bits) - 1)) << bitsLeft;
                }
            }
            Debug.Assert(bitsLeft == 8);
        }
    }
}