using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class Direct16 : PackedInts.Mutable
    {
        internal readonly short[] values;

        public Direct16(int valueCount)
            : base(valueCount, 16)
        {
            values = new short[valueCount];
        }

        public Direct16(int packedIntsVersion, DataInput input, int valueCount)
            : this(valueCount)
        {
            for (int i = 0; i < valueCount; ++i)
            {
                values[i] = input.ReadShort();
            }
            // because packed ints have not always been byte-aligned
            int remaining = (int)(PackedInts.Format.PACKED.ByteCount(packedIntsVersion, valueCount, 16) - 2L * valueCount);
            for (int i = 0; i < remaining; ++i)
            {
                input.ReadByte();
            }
        }

        public override long Get(int index)
        {
            return values[index] & 0xFFFFL;
        }

        public override void Set(int index, long value)
        {
            values[index] = (short)(value);
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
            Arrays.Fill(values, (short)0L);
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
                arr[o] = values[i] & 0xFFFFL;
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
                values[i] = (short)arr[o];
            }
            return sets;
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            //assert val == (val & 0xFFFFL);
            Arrays.Fill(values, fromIndex, toIndex, (short)val);
        }
    }
}
