using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public sealed class PackedDataOutput
    {
        internal readonly DataOutput output;
        internal long current;
        internal int remainingBits;

        public PackedDataOutput(DataOutput output)
        {
            this.output = output;
            current = 0;
            remainingBits = 8;
        }

        public void WriteLong(long value, int bitsPerValue)
        {
            //assert bitsPerValue == 64 || (value >= 0 && value <= PackedInts.maxValue(bitsPerValue));
            while (bitsPerValue > 0)
            {
                if (remainingBits == 0)
                {
                    output.WriteByte((byte)current);
                    current = 0L;
                    remainingBits = 8;
                }
                int bits = Math.Min(remainingBits, bitsPerValue);
                current = current | ((Number.URShift(value, (bitsPerValue - bits)) & ((1L << bits) - 1)) << (remainingBits - bits));
                bitsPerValue -= bits;
                remainingBits -= bits;
            }
        }

        public void Flush()
        {
            if (remainingBits < 8)
            {
                output.WriteByte((byte)current);
            }
            remainingBits = 8;
            current = 0L;
        }
    }
}
