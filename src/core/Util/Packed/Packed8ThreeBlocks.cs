using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class Packed8ThreeBlocks : PackedInts.Mutable
    {
        internal readonly sbyte[] blocks;

        public const int MAX_SIZE = int.MaxValue / 3;

        public Packed8ThreeBlocks(int valueCount)
            : base(valueCount, 24)
        {
            if (valueCount > MAX_SIZE)
            {
                throw new IndexOutOfRangeException("MAX_SIZE exceeded");
            }
            blocks = new sbyte[valueCount * 3];
        }

        public Packed8ThreeBlocks(int packedIntsVersion, DataInput input, int valueCount)
            : this(valueCount)
        {
            input.ReadBytes(blocks, 0, 3 * valueCount);
            // because packed ints have not always been byte-aligned
            int remaining = (int)(PackedInts.Format.PACKED.ByteCount(packedIntsVersion, valueCount, 24) - 3L * valueCount * 1);
            for (int i = 0; i < remaining; ++i)
            {
                input.ReadByte();
            }
        }

        public override long Get(int index)
        {
            int o = index * 3;
            return (blocks[o] & 0xFFL) << 16 | (blocks[o + 1] & 0xFFL) << 8 | (blocks[o + 2] & 0xFFL);
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            //assert len > 0 : "len must be > 0 (got " + len + ")";
            //assert index >= 0 && index < valueCount;
            //assert off + len <= arr.length;

            int gets = Math.Min(valueCount - index, len);
            for (int i = index * 3, end = (index + gets) * 3; i < end; i += 3)
            {
                arr[off++] = (blocks[i] & 0xFFL) << 16 | (blocks[i + 1] & 0xFFL) << 8 | (blocks[i + 2] & 0xFFL);
            }
            return gets;
        }

        public override void Set(int index, long value)
        {
            int o = index * 3;
            blocks[o] = (sbyte)Number.URShift(value, 16);
            blocks[o + 1] = (sbyte)Number.URShift(value, 8);
            blocks[o + 2] = (sbyte)value;
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
                blocks[o++] = (sbyte)Number.URShift(value, 16);
                blocks[o++] = (sbyte)Number.URShift(value, 8);
                blocks[o++] = (sbyte)value;
            }
            return sets;
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            sbyte block1 = (sbyte)Number.URShift(val, 16);
            sbyte block2 = (sbyte)Number.URShift(val, 8);
            sbyte block3 = (sbyte)val;
            for (int i = fromIndex * 3, end = toIndex * 3; i < end; i += 3)
            {
                blocks[i] = block1;
                blocks[i + 1] = block2;
                blocks[i + 2] = block3;
            }
        }

        public override void Clear()
        {
            Arrays.Fill(blocks, (sbyte)0);
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
