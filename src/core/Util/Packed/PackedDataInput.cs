using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public sealed class PackedDataInput
    {
        internal readonly DataInput input;
        internal long current;
        internal int remainingBits;

        public PackedDataInput(DataInput input)
        {
            this.input = input;
            SkipToNextByte();
        }

        public long ReadLong(int bitsPerValue)
        {
            //assert bitsPerValue > 0 && bitsPerValue <= 64 : bitsPerValue;
            long r = 0;
            while (bitsPerValue > 0)
            {
                if (remainingBits == 0)
                {
                    current = input.ReadByte() & 0xFF;
                    remainingBits = 8;
                }
                int bits = Math.Min(bitsPerValue, remainingBits);
                r = (r << bits) | (Number.URShift(current, (remainingBits - bits)) & ((1L << bits) - 1));
                bitsPerValue -= bits;
                remainingBits -= bits;
            }
            return r;
        }

        public void SkipToNextByte()
        {
            remainingBits = 0;
        }
    }
}
