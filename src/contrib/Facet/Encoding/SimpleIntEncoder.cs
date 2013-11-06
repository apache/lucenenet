using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public sealed class SimpleIntEncoder : IntEncoder
    {
        public override void Encode(IntsRef values, BytesRef buf)
        {
            buf.offset = buf.length = 0;
            int bytesNeeded = values.length * 4;
            if (buf.bytes.Length < bytesNeeded)
            {
                buf.Grow(bytesNeeded);
            }

            int upto = values.offset + values.length;
            for (int i = values.offset; i < upto; i++)
            {
                int value = values.ints[i];
                buf.bytes[buf.length++] = (sbyte)Number.URShift(value, 24);
                buf.bytes[buf.length++] = (sbyte)((value >> 16) & 0xFF);
                buf.bytes[buf.length++] = (sbyte)((value >> 8) & 0xFF);
                buf.bytes[buf.length++] = (sbyte)(value & 0xFF);
            }
        }

        public override IntDecoder CreateMatchingDecoder()
        {
            return new SimpleIntDecoder();
        }

        public override string ToString()
        {
            return @"Simple";
        }
    }
}
