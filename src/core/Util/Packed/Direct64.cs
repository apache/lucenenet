using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal sealed class Direct64 : PackedInts.Mutable
    {
        internal readonly long[] values;

        public Direct64(int valueCount)
            : base(valueCount, 64)
        {
            values = new long[valueCount];
        }

        public Direct64(int packedIntsVersion, DataInput input, int valueCount)
            : this(valueCount)
        {
            for (int i = 0; i < valueCount; ++i)
            {
                values[i] = input.ReadLong();
            }
        }

        public override long Get(int index)
        {
            return values[index];
        }

        public override void Set(int index, long value)
        {
            values[index] = value;
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
            Arrays.Fill(values, 0L);
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
            Array.Copy(values, index, arr, off, gets);
            return gets;
        }

        public override int Set(int index, long[] arr, int off, int len)
        {
            //assert len > 0 : "len must be > 0 (got " + len + ")";
            //assert index >= 0 && index < valueCount;
            //assert off + len <= arr.length;

            int sets = Math.Min(valueCount - index, len);
            Array.Copy(arr, off, values, index, sets);
            return sets;
        }

        public override void Fill(int fromIndex, int toIndex, long val)
        {
            Arrays.Fill(values, fromIndex, toIndex, val);
        }
    }
}
