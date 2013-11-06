using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public sealed class VInt8IntEncoder : IntEncoder
    {
        public override void Encode(IntsRef values, BytesRef buf)
        {
            buf.offset = buf.length = 0;
            int maxBytesNeeded = 5 * values.length;
            if (buf.bytes.Length < maxBytesNeeded)
            {
                buf.Grow(maxBytesNeeded);
            }

            int upto = values.offset + values.length;
            for (int i = values.offset; i < upto; i++)
            {
                int value = values.ints[i];
                if ((value & ~0x7F) == 0)
                {
                    buf.bytes[buf.length] = (sbyte)value;
                    buf.length++;
                }
                else if ((value & ~0x3FFF) == 0)
                {
                    buf.bytes[buf.length] = (sbyte)(0x80 | ((value & 0x3F80) >> 7));
                    buf.bytes[buf.length + 1] = (sbyte)(value & 0x7F);
                    buf.length += 2;
                }
                else if ((value & ~0x1FFFFF) == 0)
                {
                    buf.bytes[buf.length] = (sbyte)(0x80 | ((value & 0x1FC000) >> 14));
                    buf.bytes[buf.length + 1] = (sbyte)(0x80 | ((value & 0x3F80) >> 7));
                    buf.bytes[buf.length + 2] = (sbyte)(value & 0x7F);
                    buf.length += 3;
                }
                else if ((value & ~0xFFFFFFF) == 0)
                {
                    buf.bytes[buf.length] = (sbyte)(0x80 | ((value & 0xFE00000) >> 21));
                    buf.bytes[buf.length + 1] = (sbyte)(0x80 | ((value & 0x1FC000) >> 14));
                    buf.bytes[buf.length + 2] = (sbyte)(0x80 | ((value & 0x3F80) >> 7));
                    buf.bytes[buf.length + 3] = (sbyte)(value & 0x7F);
                    buf.length += 4;
                }
                else
                {
                    buf.bytes[buf.length] = (sbyte)(0x80 | ((value & 0xF0000000) >> 28));
                    buf.bytes[buf.length + 1] = (sbyte)(0x80 | ((value & 0xFE00000) >> 21));
                    buf.bytes[buf.length + 2] = (sbyte)(0x80 | ((value & 0x1FC000) >> 14));
                    buf.bytes[buf.length + 3] = (sbyte)(0x80 | ((value & 0x3F80) >> 7));
                    buf.bytes[buf.length + 4] = (sbyte)(value & 0x7F);
                    buf.length += 5;
                }
            }
        }

        public override IntDecoder CreateMatchingDecoder()
        {
            return new VInt8IntDecoder();
        }

        public override string ToString()
        {
            return @"VInt8";
        }
    }
}
