using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public sealed class SortingIntEncoder : IntEncoderFilter
    {
        public SortingIntEncoder(IntEncoder encoder)
            : base(encoder)
        {
        }

        public override void Encode(IntsRef values, BytesRef buf)
        {
            Array.Sort(values.ints, values.offset, values.offset + values.length);
            encoder.Encode(values, buf);
        }

        public override IntDecoder CreateMatchingDecoder()
        {
            return encoder.CreateMatchingDecoder();
        }

        public override string ToString()
        {
            return @"Sorting(" + encoder.ToString() + @")";
        }
    }
}
