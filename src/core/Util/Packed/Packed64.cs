using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal class Packed64 : PackedInts.Mutable
    {
        internal const int BLOCK_SIZE = 64; // 32 = int, 64 = long
        internal const int BLOCK_BITS = 6; // The #bits representing BLOCK_SIZE
        internal const int MOD_MASK = BLOCK_SIZE - 1; // x % BLOCK_SIZE

        private readonly long[] blocks;

        private readonly long maskRight;

        private readonly int bpvMinusBlockSize;

        public Packed64(int valueCount, int bitsPerValue)
            : base(valueCount, bitsPerValue)
        {
            PackedInts.Format format = PackedInts.Format.PACKED;
            int longCount = format.LongCount(PackedInts.VERSION_CURRENT, valueCount, bitsPerValue);
            this.blocks = new long[longCount];
            maskRight = ~0L << Number.URShift((BLOCK_SIZE - bitsPerValue), (BLOCK_SIZE - bitsPerValue));
            bpvMinusBlockSize = bitsPerValue - BLOCK_SIZE;
        }

        public Packed64(int packedIntsVersion, DataInput input, int valueCount, int bitsPerValue)
            : base(valueCount, bitsPerValue)
        {
            PackedInts.Format format = PackedInts.Format.PACKED;
            long byteCount = format.ByteCount(packedIntsVersion, valueCount, bitsPerValue); // to know how much to read
            int longCount = format.LongCount(PackedInts.VERSION_CURRENT, valueCount, bitsPerValue); // to size the array
            blocks = new long[longCount];
            // read as many longs as we can
            for (int i = 0; i < byteCount / 8; ++i)
            {
                blocks[i] = input.ReadLong();
            }
            int remaining = (int)(byteCount % 8);
            if (remaining != 0)
            {
                // read the last bytes
                long lastLong = 0;
                for (int i = 0; i < remaining; ++i)
                {
                    lastLong |= (input.ReadByte() & 0xFFL) << (56 - i * 8);
                }
                blocks[blocks.Length - 1] = lastLong;
            }
            maskRight = ~0L << Number.URShift((BLOCK_SIZE - bitsPerValue), (BLOCK_SIZE - bitsPerValue));
            bpvMinusBlockSize = bitsPerValue - BLOCK_SIZE;
        }

        public override long Get(int index)
        {
            // The abstract index in a bit stream
            long majorBitPos = (long)index * bitsPerValue;
            // The index in the backing long-array
            int elementPos = (int)Number.URShift(majorBitPos, BLOCK_BITS);
            // The number of value-bits in the second long
            long endBits = (majorBitPos & MOD_MASK) + bpvMinusBlockSize;

            if (endBits <= 0)
            { // Single block
                return Number.URShift(blocks[elementPos], (int)-endBits) & maskRight;
            }
            // Two blocks
            return ((blocks[elementPos] << (int)endBits)
                | Number.URShift(blocks[elementPos + 1], (int)(BLOCK_SIZE - endBits)))
                & maskRight;
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            //assert len > 0 : "len must be > 0 (got " + len + ")";
            //assert index >= 0 && index < valueCount;
            len = Math.Min(len, valueCount - index);
            //assert off + len <= arr.length;

            int originalIndex = index;
            PackedInts.IDecoder decoder = BulkOperation.Of(PackedInts.Format.PACKED, bitsPerValue);

            // go to the next block where the value does not span across two blocks
            int offsetInBlocks = index % decoder.LongValueCount;
            if (offsetInBlocks != 0)
            {
                for (int i = offsetInBlocks; i < decoder.LongValueCount && len > 0; ++i)
                {
                    arr[off++] = Get(index++);
                    --len;
                }
                if (len == 0)
                {
                    return index - originalIndex;
                }
            }

            // bulk get
            //assert index % decoder.longValueCount() == 0;
            int blockIndex = (int)Number.URShift(((long)index * bitsPerValue), BLOCK_BITS);
            //assert (((long)index * bitsPerValue) & MOD_MASK) == 0;
            int iterations = len / decoder.LongValueCount;
            decoder.Decode(blocks, blockIndex, arr, off, iterations);
            int gotValues = iterations * decoder.LongValueCount;
            index += gotValues;
            len -= gotValues;
            //assert len >= 0;

            if (index > originalIndex)
            {
                // stay at the block boundary
                return index - originalIndex;
            }
            else
            {
                // no progress so far => already at a block boundary but no full block to get
                //assert index == originalIndex;
                return base.Get(index, arr, off, len);
            }
        }

        public override void Set(int index, long value)
        {
            // The abstract index in a contiguous bit stream
            long majorBitPos = (long)index * bitsPerValue;
            // The index in the backing long-array
            int elementPos = (int)Number.URShift(majorBitPos, BLOCK_BITS); // / BLOCK_SIZE
            // The number of value-bits in the second long
            long endBits = (majorBitPos & MOD_MASK) + bpvMinusBlockSize;

            if (endBits <= 0)
            { // Single block
                blocks[elementPos] = blocks[elementPos] & ~(maskRight << (int)-endBits)
                   | (value << (int)-endBits);
                return;
            }
            // Two blocks
            blocks[elementPos] = blocks[elementPos] & ~Number.URShift(maskRight, (int)endBits)
                | Number.URShift(value, (int)endBits);
            blocks[elementPos + 1] = blocks[elementPos + 1] & Number.URShift(~0L, (int)endBits)
                | (value << (int)(BLOCK_SIZE - endBits));
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            //assert len > 0 : "len must be > 0 (got " + len + ")";
            //assert index >= 0 && index < valueCount;
            len = Math.Min(len, valueCount - index);
            //assert off + len <= arr.length;

            int originalIndex = index;
            PackedInts.IEncoder encoder = BulkOperation.Of(PackedInts.Format.PACKED, bitsPerValue);

            // go to the next block where the value does not span across two blocks
            int offsetInBlocks = index % encoder.LongValueCount;
            if (offsetInBlocks != 0)
            {
                for (int i = offsetInBlocks; i < encoder.LongValueCount && len > 0; ++i)
                {
                    Set(index++, arr[off++]);
                    --len;
                }
                if (len == 0)
                {
                    return index - originalIndex;
                }
            }

            // bulk set
            //assert index % encoder.longValueCount() == 0;
            int blockIndex = (int)Number.URShift(((long)index * bitsPerValue), BLOCK_BITS);
            //assert (((long)index * bitsPerValue) & MOD_MASK) == 0;
            int iterations = len / encoder.LongValueCount;
            encoder.Encode(arr, off, blocks, blockIndex, iterations);
            int setValues = iterations * encoder.LongValueCount;
            index += setValues;
            len -= setValues;
            //assert len >= 0;

            if (index > originalIndex)
            {
                // stay at the block boundary
                return index - originalIndex;
            }
            else
            {
                // no progress so far => already at a block boundary but no full block to get
                //assert index == originalIndex;
                return base.Set(index, arr, off, len);
            }
        }

        public override string ToString()
        {
            return "Packed64(bitsPerValue=" + bitsPerValue + ", size="
                + Size() + ", elements.length=" + blocks.Length + ")";
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER
                + 3 * RamUsageEstimator.NUM_BYTES_INT     // bpvMinusBlockSize,valueCount,bitsPerValue
                + RamUsageEstimator.NUM_BYTES_LONG        // maskRight
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF) // blocks ref
                + RamUsageEstimator.SizeOf(blocks);
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            //assert PackedInts.bitsRequired(val) <= getBitsPerValue();
            //assert fromIndex <= toIndex;

            // minimum number of values that use an exact number of full blocks
            int nAlignedValues = 64 / Gcd(64, bitsPerValue);
            int span = toIndex - fromIndex;
            if (span <= 3 * nAlignedValues)
            {
                // there needs be at least 2 * nAlignedValues aligned values for the
                // block approach to be worth trying
                base.Fill(fromIndex, toIndex, val);
                return;
            }

            // fill the first values naively until the next block start
            int fromIndexModNAlignedValues = fromIndex % nAlignedValues;
            if (fromIndexModNAlignedValues != 0)
            {
                for (int i = fromIndexModNAlignedValues; i < nAlignedValues; ++i)
                {
                    Set(fromIndex++, val);
                }
            }
            //assert fromIndex % nAlignedValues == 0;

            // compute the long[] blocks for nAlignedValues consecutive values and
            // use them to set as many values as possible without applying any mask
            // or shift
            int nAlignedBlocks = (nAlignedValues * bitsPerValue) >> 6;
            long[] nAlignedValuesBlocks;
            {
                Packed64 values = new Packed64(nAlignedValues, bitsPerValue);
                for (int i = 0; i < nAlignedValues; ++i)
                {
                    values.Set(i, val);
                }
                nAlignedValuesBlocks = values.blocks;
                //assert nAlignedBlocks <= nAlignedValuesBlocks.length;
            }
            int startBlock = (int)Number.URShift(((long)fromIndex * bitsPerValue), 6);
            int endBlock = (int)Number.URShift(((long)toIndex * bitsPerValue), 6);
            for (int block = startBlock; block < endBlock; ++block)
            {
                long blockValue = nAlignedValuesBlocks[block % nAlignedBlocks];
                blocks[block] = blockValue;
            }

            // fill the gap
            for (int i = (int)(((long)endBlock << 6) / bitsPerValue); i < toIndex; ++i)
            {
                Set(i, val);
            }
        }
        
        private static int Gcd(int a, int b)
        {
            if (a < b)
            {
                return Gcd(b, a);
            }
            else if (b == 0)
            {
                return a;
            }
            else
            {
                return Gcd(b, a % b);
            }
        }

        public override void Clear()
        {
            Arrays.Fill(blocks, 0L);
        }
    }
}
