using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;

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
    /// Non-specialized <see cref="BulkOperation"/> for <see cref="PackedInt32s.Format.PACKED"/>.
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
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue > 0 && bitsPerValue <= 64);
            int blocks = bitsPerValue;
            while ((blocks & 1) == 0)
            {
                blocks = blocks.TripleShift(1);
            }
            this.longBlockCount = blocks;
            this.longValueCount = 64 * longBlockCount / bitsPerValue;
            int byteBlockCount = 8 * longBlockCount;
            int byteValueCount = longValueCount;
            while ((byteBlockCount & 1) == 0 && (byteValueCount & 1) == 0)
            {
                byteBlockCount = byteBlockCount.TripleShift(1);
                byteValueCount = byteValueCount.TripleShift(1);
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
            if (Debugging.AssertsEnabled) Debugging.Assert(longValueCount * bitsPerValue == 64 * longBlockCount);
        }

        /// <summary>
        /// NOTE: This was longBlockCount() in Lucene.
        /// </summary>
        public override int Int64BlockCount => longBlockCount;

        /// <summary>
        /// NOTE: This was longValueCount() in Lucene.
        /// </summary>
        public override int Int64ValueCount => longValueCount;

        public override int ByteBlockCount => byteBlockCount;

        public override int ByteValueCount => byteValueCount;

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            int bitsLeft = 64;
            for (int i = 0; i < longValueCount * iterations; ++i)
            {
                bitsLeft -= bitsPerValue;
                if (bitsLeft < 0)
                {
                    values[valuesOffset++] = ((blocks[blocksOffset++] & ((1L << (bitsPerValue + bitsLeft)) - 1)) << -bitsLeft) | (blocks[blocksOffset].TripleShift(64 + bitsLeft));
                    bitsLeft += 64;
                }
                else
                {
                    values[valuesOffset++] = (blocks[blocksOffset].TripleShift(bitsLeft)) & mask;
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
                    values[valuesOffset++] = nextValue | (bytes.TripleShift(bits));
                    while (bits >= bitsPerValue)
                    {
                        bits -= bitsPerValue;
                        values[valuesOffset++] = (bytes.TripleShift(bits)) & mask;
                    }
                    // then buffer
                    bitsLeft = bitsPerValue - bits;
                    nextValue = (bytes & ((1L << bits) - 1)) << bitsLeft;
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsLeft == bitsPerValue);
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            if (bitsPerValue > 32)
            {
                throw UnsupportedOperationException.Create("Cannot decode " + bitsPerValue + "-bits values into an int[]");
            }
            int bitsLeft = 64;
            for (int i = 0; i < longValueCount * iterations; ++i)
            {
                bitsLeft -= bitsPerValue;
                if (bitsLeft < 0)
                {
                    values[valuesOffset++] = (int)(((blocks[blocksOffset++] & ((1L << (bitsPerValue + bitsLeft)) - 1)) << -bitsLeft) | (blocks[blocksOffset].TripleShift(64 + bitsLeft)));
                    bitsLeft += 64;
                }
                else
                {
                    values[valuesOffset++] = (int)((blocks[blocksOffset].TripleShift(bitsLeft)) & mask);
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
                    values[valuesOffset++] = nextValue | (bytes.TripleShift(bits));
                    while (bits >= bitsPerValue)
                    {
                        bits -= bitsPerValue;
                        values[valuesOffset++] = (bytes.TripleShift(bits)) & intMask;
                    }
                    // then buffer
                    bitsLeft = bitsPerValue - bits;
                    nextValue = (bytes & ((1 << bits) - 1)) << bitsLeft;
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsLeft == bitsPerValue);
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
                    nextBlock |= values[valuesOffset].TripleShift(-bitsLeft);
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
                    nextBlock |= (values[valuesOffset] & 0xFFFFFFFFL).TripleShift(-bitsLeft);
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
                if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue == 64 || PackedInt32s.BitsRequired(v) <= bitsPerValue);
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
                    blocks[blocksOffset++] = (byte)((uint)nextBlock | (v.TripleShift(bits)));
                    while (bits >= 8)
                    {
                        bits -= 8;
                        blocks[blocksOffset++] = (byte)(v.TripleShift(bits));
                    }
                    // then buffer
                    bitsLeft = 8 - bits;
                    nextBlock = (int)((v & ((1L << bits) - 1)) << bitsLeft);
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsLeft == 8);
        }

        public override void Encode(int[] values, int valuesOffset, byte[] blocks, int blocksOffset, int iterations)
        {
            int nextBlock = 0;
            int bitsLeft = 8;
            for (int i = 0; i < byteValueCount * iterations; ++i)
            {
                int v = values[valuesOffset++];
                if (Debugging.AssertsEnabled) Debugging.Assert(PackedInt32s.BitsRequired(v & 0xFFFFFFFFL) <= bitsPerValue);
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
                    blocks[blocksOffset++] = (byte)(nextBlock | (v.TripleShift(bits)));
                    while (bits >= 8)
                    {
                        bits -= 8;
                        blocks[blocksOffset++] = (byte)(v.TripleShift(bits));
                    }
                    // then buffer
                    bitsLeft = 8 - bits;
                    nextBlock = (v & ((1 << bits) - 1)) << bitsLeft;
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsLeft == 8);
        }
    }
}