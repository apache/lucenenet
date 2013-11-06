using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public sealed class DGapIntDecoder : IntDecoder
    {
        private readonly IntDecoder decoder;

        public DGapIntDecoder(IntDecoder decoder)
        {
            this.decoder = decoder;
        }

        public override void Decode(BytesRef buf, IntsRef values)
        {
            decoder.Decode(buf, values);
            int prev = 0;
            for (int i = 0; i < values.length; i++)
            {
                values.ints[i] += prev;
                prev = values.ints[i];
            }
        }

        public override string ToString()
        {
            return @"DGap(" + decoder.ToString() + @")";
        }
    }
}
