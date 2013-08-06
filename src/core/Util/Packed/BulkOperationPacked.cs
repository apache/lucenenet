using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
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
            //assert bitsPerValue > 0 && bitsPerValue <= 64;
            int blocks = bitsPerValue;
            while ((blocks & 1) == 0)
            {
                blocks = Number.URShift(blocks, 1);
            }
            this.longBlockCount = blocks;
            this.longValueCount = 64 * longBlockCount / bitsPerValue;
            int byteBlockCount = 8 * longBlockCount;
            int byteValueCount = longValueCount;
            while ((byteBlockCount & 1) == 0 && (byteValueCount & 1) == 0)
            {
                byteBlockCount = Number.URShift(byteBlockCount, 1);
                byteValueCount = Number.URShift(byteValueCount, 1);
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
            //assert longValueCount * bitsPerValue == 64 * longBlockCount;
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
                    values[valuesOffset++] =
                        ((blocks[blocksOffset++] & ((1L << (bitsPerValue + bitsLeft)) - 1)) << -bitsLeft)
                        | Number.URShift(blocks[blocksOffset], (64 + bitsLeft));
                    bitsLeft += 64;
                }
                else
                {
                    values[valuesOffset++] = Number.URShift(blocks[blocksOffset], bitsLeft) & mask;
                }
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
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
                    values[valuesOffset++] = nextValue | Number.URShift(bytes, bits);
                    while (bits >= bitsPerValue)
                    {
                        bits -= bitsPerValue;
                        values[valuesOffset++] = Number.URShift(bytes, bits) & mask;
                    }
                    // then buffer
                    bitsLeft = bitsPerValue - bits;
                    nextValue = (bytes & ((1L << bits) - 1)) << bitsLeft;
                }
            }
            //assert bitsLeft == bitsPerValue;
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            if (bitsPerValue > 32)
            {
                throw new NotSupportedException("Cannot decode " + bitsPerValue + "-bits values into an int[]");
            }
            int bitsLeft = 64;
            for (int i = 0; i < longValueCount * iterations; ++i)
            {
                bitsLeft -= bitsPerValue;
                if (bitsLeft < 0)
                {
                    values[valuesOffset++] = (int)
                        (((blocks[blocksOffset++] & ((1L << (bitsPerValue + bitsLeft)) - 1)) << -bitsLeft)
                        | Number.URShift(blocks[blocksOffset], (64 + bitsLeft)));
                    bitsLeft += 64;
                }
                else
                {
                    values[valuesOffset++] = (int)(Number.URShift(blocks[blocksOffset], bitsLeft) & mask);
                }
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
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
                    values[valuesOffset++] = nextValue | Number.URShift(bytes, bits);
                    while (bits >= bitsPerValue)
                    {
                        bits -= bitsPerValue;
                        values[valuesOffset++] = Number.URShift(bytes, bits) & intMask;
                    }
                    // then buffer
                    bitsLeft = bitsPerValue - bits;
                    nextValue = (bytes & ((1 << bits) - 1)) << bitsLeft;
                }
            }
            //assert bitsLeft == bitsPerValue;
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
                }
                else
                { // bitsLeft < 0
                    nextBlock |= Number.URShift(values[valuesOffset], -bitsLeft);
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
                }
                else
                { // bitsLeft < 0
                    nextBlock |= Number.URShift((values[valuesOffset] & 0xFFFFFFFFL), -bitsLeft);
                    blocks[blocksOffset++] = nextBlock;
                    nextBlock = (values[valuesOffset++] & ((1L << -bitsLeft) - 1)) << (64 + bitsLeft);
                    bitsLeft += 64;
                }
            }
        }

        public override void Encode(long[] values, int valuesOffset, sbyte[] blocks, int blocksOffset, int iterations)
        {
            uint nextBlock = 0;
            int bitsLeft = 8;
            for (int i = 0; i < byteValueCount * iterations; ++i)
            {
                long v = values[valuesOffset++];
                //assert bitsPerValue == 64 || PackedInts.bitsRequired(v) <= bitsPerValue;
                if (bitsPerValue < bitsLeft)
                {
                    // just buffer
                    nextBlock |= (uint)(v << (bitsLeft - bitsPerValue));
                    bitsLeft -= bitsPerValue;
                }
                else
                {
                    // flush as many blocks as possible
                    int bits = bitsPerValue - bitsLeft;
                    blocks[blocksOffset++] = (sbyte)(nextBlock | Number.URShift(v, bits));
                    while (bits >= 8)
                    {
                        bits -= 8;
                        blocks[blocksOffset++] = (sbyte)Number.URShift(v, bits);
                    }
                    // then buffer
                    bitsLeft = 8 - bits;
                    nextBlock = (uint)((v & ((1L << bits) - 1)) << bitsLeft);
                }
            }
            //assert bitsLeft == 8;
        }

        public override void Encode(int[] values, int valuesOffset, sbyte[] blocks, int blocksOffset, int iterations)
        {
            int nextBlock = 0;
            int bitsLeft = 8;
            for (int i = 0; i < byteValueCount * iterations; ++i)
            {
                int v = values[valuesOffset++];
                //assert PackedInts.bitsRequired(v & 0xFFFFFFFFL) <= bitsPerValue;
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
                    blocks[blocksOffset++] = (sbyte)(nextBlock | Number.URShift(v, bits));
                    while (bits >= 8)
                    {
                        bits -= 8;
                        blocks[blocksOffset++] = (sbyte)Number.URShift(v, bits);
                    }
                    // then buffer
                    bitsLeft = 8 - bits;
                    nextBlock = (v & ((1 << bits) - 1)) << bitsLeft;
                }
            }
            //assert bitsLeft == 8;
        }
    }
}
