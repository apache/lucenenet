using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public sealed class VInt8IntDecoder : IntDecoder
    {
        public override void Decode(BytesRef buf, IntsRef values)
        {
            values.offset = values.length = 0;
            if (values.ints.Length < buf.length)
            {
                values.ints = new int[ArrayUtil.Oversize(buf.length, RamUsageEstimator.NUM_BYTES_INT)];
            }

            int upto = buf.offset + buf.length;
            int value = 0;
            int offset = buf.offset;
            while (offset < upto)
            {
                sbyte b = buf.bytes[offset++];
                if (b >= 0)
                {
                    values.ints[values.length++] = (value << 7) | (byte)b;
                    value = 0;
                }
                else
                {
                    value = (value << 7) | (b & 0x7F);
                }
            }
        }

        public override string ToString()
        {
            return @"VInt8";
        }
    }
}
