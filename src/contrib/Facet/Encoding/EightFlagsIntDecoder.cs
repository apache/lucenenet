using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public class EightFlagsIntDecoder : IntDecoder
    {
        private static readonly byte[,] DECODE_TABLE = new byte[256,8];

        static EightFlagsIntDecoder()
        {
            for (int i = 256; i != 0; )
            {
                --i;
                for (int j = 8; j != 0; )
                {
                    --j;
                    DECODE_TABLE[i,j] = (byte)(Number.URShift(i, j) & 0x1);
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
                int capacityNeeded = values.length + 8;
                if (values.ints.Length < capacityNeeded)
                {
                    values.Grow(capacityNeeded);
                }

                while (ordinal != 8)
                {
                    if (DECODE_TABLE[indicator,ordinal++] == 0)
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
                                values.ints[values.length++] = ((value << 7) | (byte)b) + 2;
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
                        values.ints[values.length++] = 1;
                    }
                }
            }
        }

        public override string ToString()
        {
            return @"EightFlags(VInt8)";
        }
    }
}
