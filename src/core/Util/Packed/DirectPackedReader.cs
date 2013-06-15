using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    internal class DirectPackedReader : PackedInts.Reader
    {
        private readonly IndexInput input;
        private readonly long startPointer;

        public DirectPackedReader(int bitsPerValue, int valueCount, IndexInput input)
            : base(valueCount, bitsPerValue)
        {
            this.input = input;

            startPointer = input.FilePointer;
        }

        public override long Get(int index)
        {
            long majorBitPos = (long)index * bitsPerValue;
            long elementPos = Number.URShift(majorBitPos, 3);
            try
            {
                input.Seek(startPointer + elementPos);

                byte b0 = input.ReadByte();
                int bitPos = (int)(majorBitPos & 7);
                if (bitPos + bitsPerValue <= 8)
                {
                    // special case: all bits are in the first byte
                    return Number.URShift((b0 & ((1L << (8 - bitPos)) - 1)), (8 - bitPos - bitsPerValue));
                }

                // take bits from the first byte
                int remainingBits = bitsPerValue - 8 + bitPos;
                long result = (b0 & ((1L << (8 - bitPos)) - 1)) << remainingBits;

                // add bits from inner bytes
                while (remainingBits >= 8)
                {
                    remainingBits -= 8;
                    result |= (input.ReadByte() & 0xFFL) << remainingBits;
                }

                // take bits from the last byte
                if (remainingBits > 0)
                {
                    result |= Number.URShift((input.ReadByte() & 0xFFL), (8 - remainingBits));
                }

                return result;
            }
            catch (System.IO.IOException ioe)
            {
                throw new InvalidOperationException("failed", ioe);
            }
        }

        public override long RamBytesUsed()
        {
            return 0;
        }
    }
}
