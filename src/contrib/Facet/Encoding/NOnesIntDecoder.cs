using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public class NOnesIntDecoder : FourFlagsIntDecoder
    {
        private readonly int n;
        private readonly IntsRef internalBuffer;

        public NOnesIntDecoder(int n)
        {
            this.n = n;
            internalBuffer = new IntsRef(100);
        }

        public override void Decode(BytesRef buf, IntsRef values)
        {
            values.offset = values.length = 0;
            internalBuffer.length = 0;
            base.Decode(buf, internalBuffer);
            if (values.ints.Length < internalBuffer.length)
            {
                values.Grow(internalBuffer.length * n / 2);
            }

            for (int i = 0; i < internalBuffer.length; i++)
            {
                int decode = internalBuffer.ints[i];
                if (decode == 1)
                {
                    if (values.length == values.ints.Length)
                    {
                        values.Grow(values.length + 10);
                    }

                    values.ints[values.length++] = 1;
                }
                else if (decode == 2)
                {
                    if (values.length + n >= values.ints.Length)
                    {
                        values.Grow(values.length + n);
                    }

                    for (int j = 0; j < n; j++)
                    {
                        values.ints[values.length++] = 1;
                    }
                }
                else
                {
                    if (values.length == values.ints.Length)
                    {
                        values.Grow(values.length + 10);
                    }

                    values.ints[values.length++] = decode - 1;
                }
            }
        }

        public override string ToString()
        {
            return @"NOnes(" + n + @") (" + base.ToString() + @")";
        }
    }
}
