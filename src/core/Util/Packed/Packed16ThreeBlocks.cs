using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class Packed16ThreeBlocks : PackedInts.Mutable
    {
        internal readonly short[] blocks;

        public const int MAX_SIZE = int.MaxValue / 3;

        public Packed16ThreeBlocks(int valueCount)
            : base(valueCount, 48)
        {
            if (valueCount > MAX_SIZE)
            {
                throw new IndexOutOfRangeException("MAX_SIZE exceeded");
            }
            blocks = new short[valueCount * 3];
        }

        public Packed16ThreeBlocks(int packedIntsVersion, DataInput input, int valueCount)
            : this(valueCount)
        {
            for (int i = 0; i < 3 * valueCount; ++i)
            {
                blocks[i] = input.ReadShort();
            }
            // because packed ints have not always been byte-aligned
            int remaining = (int)(PackedInts.Format.PACKED.ByteCount(packedIntsVersion, valueCount, 48) - 3L * valueCount * 2);
            for (int i = 0; i < remaining; ++i)
            {
                input.ReadByte();
            }
        }

        public override long Get(int index)
        {
            int o = index * 3;
            return (blocks[o] & 0xFFFFL) << 32 | (blocks[o + 1] & 0xFFFFL) << 16 | (blocks[o + 2] & 0xFFFFL);
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            //assert len > 0 : "len must be > 0 (got " + len + ")";
            //assert index >= 0 && index < valueCount;
            //assert off + len <= arr.length;

            int gets = Math.Min(valueCount - index, len);
            for (int i = index * 3, end = (index + gets) * 3; i < end; i += 3)
            {
                arr[off++] = (blocks[i] & 0xFFFFL) << 32 | (blocks[i + 1] & 0xFFFFL) << 16 | (blocks[i + 2] & 0xFFFFL);
            }
            return gets;
        }

        public override void Set(int index, long value)
        {
            int o = index * 3;
            blocks[o] = (short)Number.URShift(value, 32);
            blocks[o + 1] = (short)Number.URShift(value,16);
            blocks[o + 2] = (short)value;
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            //assert len > 0 : "len must be > 0 (got " + len + ")";
            //assert index >= 0 && index < valueCount;
            //assert off + len <= arr.length;

            int sets = Math.Min(valueCount - index, len);
            for (int i = off, o = index * 3, end = off + sets; i < end; ++i)
            {
                long value = arr[i];
                blocks[o++] = (short)Number.URShift(value, 32);
                blocks[o++] = (short)Number.URShift(value, 16);
                blocks[o++] = (short)value;
            }
            return sets;
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            short block1 = (short)Number.URShift(val, 32);
            short block2 = (short)Number.URShift(val, 16);
            short block3 = (short)val;
            for (int i = fromIndex * 3, end = toIndex * 3; i < end; i += 3)
            {
                blocks[i] = block1;
                blocks[i + 1] = block2;
                blocks[i + 2] = block3;
            }
        }

        public override void Clear()
        {
            Arrays.Fill(blocks, (short)0);
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER
                + 2 * RamUsageEstimator.NUM_BYTES_INT     // valueCount,bitsPerValue
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF) // blocks ref
                + RamUsageEstimator.SizeOf(blocks);
        }

        public override string ToString()
        {
            return GetType().Name + "(bitsPerValue=" + bitsPerValue
                + ", size=" + Size() + ", elements.length=" + blocks.Length + ")";
        }
    }
}
