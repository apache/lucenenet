using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class Direct32 : PackedInts.Mutable
    {
        internal readonly int[] values;

        public Direct32(int valueCount)
            : base(valueCount, 32)
        {
            values = new int[valueCount];
        }

        public Direct32(int packedIntsVersion, DataInput input, int valueCount)
            : this(valueCount)
        {
            for (int i = 0; i < valueCount; ++i)
            {
                values[i] = input.ReadInt();
            }
            // because packed ints have not always been byte-aligned
            int remaining = (int)(PackedInts.Format.PACKED.ByteCount(packedIntsVersion, valueCount, 32) - 4L * valueCount);
            for (int i = 0; i < remaining; ++i)
            {
                input.ReadByte();
            }
        }

        public override long Get(int index)
        {
            return values[index] & 0xFFFFFFFFL;
        }

        public override void Set(int index, long value)
        {
            values[index] = (int)(value);
        }

        public override long RamBytesUsed()
        {
            return RamUsageEstimator.AlignObjectSize(
                RamUsageEstimator.NUM_BYTES_OBJECT_HEADER
                + 2 * RamUsageEstimator.NUM_BYTES_INT     // valueCount,bitsPerValue
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF) // values ref
                + RamUsageEstimator.SizeOf(values);
        }

        public override void Clear()
        {
            Arrays.Fill(values, (int)0L);
        }

        public override object GetArray()
        {
            return values;
        }

        public override bool HasArray()
        {
            return true;
        }

        public override int Get(int index, long[] arr, int off, int len)
        {
            //assert len > 0 : "len must be > 0 (got " + len + ")";
            //assert index >= 0 && index < valueCount;
            //assert off + len <= arr.length;

            int gets = Math.Min(valueCount - index, len);
            for (int i = index, o = off, end = index + gets; i < end; ++i, ++o)
            {
                arr[o] = values[i] & 0xFFFFFFFFL;
            }
            return gets;
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            //assert len > 0 : "len must be > 0 (got " + len + ")";
            //assert index >= 0 && index < valueCount;
            //assert off + len <= arr.length;

            int sets = Math.Min(valueCount - index, len);
            for (int i = index, o = off, end = index + sets; i < end; ++i, ++o)
            {
                values[i] = (int)arr[o];
            }
            return sets;
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            //assert val == (val & 0xFFFFFFFFL);
            Arrays.Fill(values, fromIndex, toIndex, (int)val);
        }
    }
}
