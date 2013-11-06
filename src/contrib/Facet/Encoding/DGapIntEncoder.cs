using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public sealed class DGapIntEncoder : IntEncoderFilter
    {
        public DGapIntEncoder(IntEncoder encoder)
            : base(encoder)
        {
        }

        public override void Encode(IntsRef values, BytesRef buf)
        {
            int prev = 0;
            int upto = values.offset + values.length;
            for (int i = values.offset; i < upto; i++)
            {
                int tmp = values.ints[i];
                values.ints[i] -= prev;
                prev = tmp;
            }

            encoder.Encode(values, buf);
        }

        public override IntDecoder CreateMatchingDecoder()
        {
            return new DGapIntDecoder(encoder.CreateMatchingDecoder());
        }

        public override string ToString()
        {
            return @"DGap(" + encoder.ToString() + @")";
        }
    }
}
