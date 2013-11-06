using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public class FourFlagsIntDecoder : IntDecoder
    {
        private static readonly byte[,] DECODE_TABLE = new byte[256, 4];

        static FourFlagsIntDecoder()
        {
            for (int i = 256; i != 0; )
            {
                --i;
                for (int j = 4; j != 0; )
                {
                    --j;
                    DECODE_TABLE[i,j] = (byte)(Number.URShift(i, (j << 1)) & 0x3);
                }
            }
        }

        public override void Decode(BytesRef buf, IntsRef values)
        {
            values.offset = values.length = 0;
            int upto = buf.offset + buf.length;
            int offset = buf.offset;
            while (offset < upto)
            {
                int indicator = buf.bytes[offset++] & 0xFF;
                int ordinal = 0;
                int capacityNeeded = values.length + 4;
                if (values.ints.Length < capacityNeeded)
                {
                    values.Grow(capacityNeeded);
                }

                while (ordinal != 4)
                {
                    byte decodeVal = DECODE_TABLE[indicator,ordinal++];
                    if (decodeVal == 0)
                    {
                        if (offset == upto)
                        {
                            return;
                        }

                        int value = 0;
                        while (true)
                        {
                            sbyte b = buf.bytes[offset++];
                            if (b >= 0)
                            {
                                values.ints[values.length++] = ((value << 7) | (byte)b) + 4;
                                break;
                            }
                            else
                            {
                                value = (value << 7) | (b & 0x7F);
                            }
                        }
                    }
                    else
                    {
                        values.ints[values.length++] = decodeVal;
                    }
                }
            }
        }

        public override string ToString()
        {
            return @"FourFlags(VInt)";
        }
    }
}
