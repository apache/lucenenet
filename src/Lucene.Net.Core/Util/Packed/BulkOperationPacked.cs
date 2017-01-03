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
        private readonly int bitsPerValue;
        private readonly int longBlockCount;
        private readonly int longValueCount;
        private readonly int byteBlockCount;
        private readonly int byteValueCount;
        private readonly long mask;
        private readonly int intMask;

        public BulkOperationPacked(int bitsPerValue)
        {
            this.bitsPerValue = bitsPerValue;
            Debug.Assert(bitsPerValue > 0 && bitsPerValue <= 64);
            int blocks = bitsPerValue;
            while ((blocks & 1) == 0)
            {
                blocks = (int)((uint)blocks >> 1);
            }
            this.longBlockCount = blocks;
            this.longValueCount = 64 * longBlockCount / bitsPerValue;
            int byteBlockCount = 8 * longBlockCount;
            int byteValueCount = longValueCount;
            while ((byteBlockCount & 1) == 0 && (byteValueCount & 1) == 0)
            {
                byteBlockCount = (int)((uint)byteBlockCount >> 1);
                byteValueCount = (int)((uint)byteValueCount >> 1);
            }
            this.byteBlockCount = byteBlockCount;
            this.byteValueCount = byteValueCount;
            if (bitsPerValue == 64)
            {
                this.mask = ~0L;
            }
            else
            {
                this.mask = (1L << bitsPerValue) - 1;
            }
            this.intMask = (int)mask;
            Debug.Assert(longValueCount * bitsPerValue == 64 * longBlockCount);
        }

        public override int LongBlockCount
        {
            get { return longBlockCount; }
        }

        public override int LongValueCount
        {
            get { return longValueCount; }
        }

        public override int ByteBlockCount
        {
            get { return byteBlockCount; }
        }

        public override int ByteValueCount
        {
            get { return byteValueCount; }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            int bitsLeft = 64;
            for (int i = 0; i < longValueCount * iterations; ++i)
            {
                bitsLeft -= bitsPerValue;
                if (bitsLeft < 0)
                {
                    values[valuesOffset++] = ((blocks[blocksOffset++] & ((1L << (bitsPerValue + bitsLeft)) - 1)) << -bitsLeft) | ((long)((ulong)blocks[blocksOffset] >> (64 + bitsLeft)));
                    bitsLeft += 64;
                }
                else
                {
                    values[valuesOffset++] = ((long)((ulong)blocks[blocksOffset] >> bitsLeft)) & mask;
                }
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            long nextValue = 0L;
            int bitsLeft = bitsPerValue;
            for (int i = 0; i < iterations * byteBlockCount; ++i)
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
                    while (bits >= bitsPerValue)
                    {
                        bits -= bitsPerValue;
                        values[valuesOffset++] = ((long)((ulong)bytes >> bits)) & mask;
                    }
                    // then buffer
                    bitsLeft = bitsPerValue - bits;
                    nextValue = (bytes & ((1L << bits) - 1)) << bitsLeft;
                }
            }
            Debug.Assert(bitsLeft == bitsPerValue);
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            if (bitsPerValue > 32)
            {
                throw new System.NotSupportedException("Cannot decode " + bitsPerValue + "-bits values into an int[]");
            }
            int bitsLeft = 64;
            for (int i = 0; i < longValueCount * iterations; ++i)
            {
                bitsLeft -= bitsPerValue;
                if (bitsLeft < 0)
                {
                    values[valuesOffset++] = (int)(((blocks[blocksOffset++] & ((1L << (bitsPerValue + bitsLeft)) - 1)) << -bitsLeft) | ((long)((ulong)blocks[blocksOffset] >> (64 + bitsLeft))));
                    bitsLeft += 64;
                }
                else
                {
                    values[valuesOffset++] = (int)(((long)((ulong)blocks[blocksOffset] >> bitsLeft)) & mask);
                }
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            int nextValue = 0;
            int bitsLeft = bitsPerValue;
            for (int i = 0; i < iterations * byteBlockCount; ++i)
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
                    while (bits >= bitsPerValue)
                    {
                        bits -= bitsPerValue;
                        values[valuesOffset++] = ((int)((uint)bytes >> bits)) & intMask;
                    }
                    // then buffer
                    bitsLeft = bitsPerValue - bits;
                    nextValue = (bytes & ((1 << bits) - 1)) << bitsLeft;
                }
            }
            Debug.Assert(bitsLeft == bitsPerValue);
        }

        public override void Encode(long[] values, int valuesOffset, long[] blocks, int blocksOffset, int iterations)
        {
            long nextBlock = 0;
            int bitsLeft = 64;
            for (int i = 0; i < longValueCount * iterations; ++i)
            {
                bitsLeft -= bitsPerValue;
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
            for (int i = 0; i < longValueCount * iterations; ++i)
            {
                bitsLeft -= bitsPerValue;
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
            for (int i = 0; i < byteValueCount * iterations; ++i)
            {
                long v = values[valuesOffset++];
                Debug.Assert(bitsPerValue == 64 || PackedInts.BitsRequired(v) <= bitsPerValue);
                if (bitsPerValue < bitsLeft)
                {
                    // just buffer
                    nextBlock |= (int)(v << (bitsLeft - bitsPerValue));
                    bitsLeft -= bitsPerValue;
                }
                else
                {
                    // flush as many blocks as possible
                    int bits = bitsPerValue - bitsLeft;
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
            for (int i = 0; i < byteValueCount * iterations; ++i)
            {
                int v = values[valuesOffset++];
                Debug.Assert(PackedInts.BitsRequired(v & 0xFFFFFFFFL) <= bitsPerValue);
                if (bitsPerValue < bitsLeft)
                {
                    // just buffer
                    nextBlock |= v << (bitsLeft - bitsPerValue);
                    bitsLeft -= bitsPerValue;
                }
                else
                {
                    // flush as many blocks as possible
                    int bits = bitsPerValue - bitsLeft;
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