using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public sealed class UniqueValuesIntEncoder : IntEncoderFilter
    {
        public UniqueValuesIntEncoder(IntEncoder encoder)
            : base(encoder)
        {
        }

        public override void Encode(IntsRef values, BytesRef buf)
        {
            int prev = values.ints[values.offset];
            int idx = values.offset + 1;
            int upto = values.offset + values.length;
            for (int i = idx; i < upto; i++)
            {
                if (values.ints[i] != prev)
                {
                    values.ints[idx++] = values.ints[i];
                    prev = values.ints[i];
                }
            }

            values.length = idx - values.offset;
            encoder.Encode(values, buf);
        }

        public override IntDecoder CreateMatchingDecoder()
        {
            return encoder.CreateMatchingDecoder();
        }

        public override string ToString()
        {
            return @"Unique(" + encoder.ToString() + @")";
        }
    }
}
