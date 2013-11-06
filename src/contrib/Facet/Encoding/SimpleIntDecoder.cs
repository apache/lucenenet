using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public sealed class SimpleIntDecoder : IntDecoder
    {
        public override void Decode(BytesRef buf, IntsRef values)
        {
            values.offset = values.length = 0;
            int numValues = buf.length / 4;
            if (values.ints.Length < numValues)
            {
                values.ints = new int[ArrayUtil.Oversize(numValues, RamUsageEstimator.NUM_BYTES_INT)];
            }

            int offset = buf.offset;
            int upto = buf.offset + buf.length;
            while (offset < upto)
            {
                values.ints[values.length++] = ((buf.bytes[offset++] & 0xFF) << 24) | ((buf.bytes[offset++] & 0xFF) << 16) | ((buf.bytes[offset++] & 0xFF) << 8) | (buf.bytes[offset++] & 0xFF);
            }
        }

        public override string ToString()
        {
            return @"Simple";
        }
    }
}
