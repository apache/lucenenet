using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public class NOnesIntEncoder : FourFlagsIntEncoder
    {
        private readonly IntsRef internalBuffer;
        private readonly int n;

        public NOnesIntEncoder(int n)
        {
            this.n = n;
            internalBuffer = new IntsRef(n);
        }

        public override void Encode(IntsRef values, BytesRef buf)
        {
            internalBuffer.length = 0;
            if (values.length > internalBuffer.ints.Length)
            {
                internalBuffer.Grow(values.length);
            }

            int onesCounter = 0;
            int upto = values.offset + values.length;
            for (int i = values.offset; i < upto; i++)
            {
                int value = values.ints[i];
                if (value == 1)
                {
                    if (++onesCounter == n)
                    {
                        internalBuffer.ints[internalBuffer.length++] = 2;
                        onesCounter = 0;
                    }
                }
                else
                {
                    while (onesCounter > 0)
                    {
                        --onesCounter;
                        internalBuffer.ints[internalBuffer.length++] = 1;
                    }

                    internalBuffer.ints[internalBuffer.length++] = value + 1;
                }
            }

            while (onesCounter > 0)
            {
                --onesCounter;
                internalBuffer.ints[internalBuffer.length++] = 1;
            }

            base.Encode(internalBuffer, buf);
        }

        public override IntDecoder CreateMatchingDecoder()
        {
            return new NOnesIntDecoder(n);
        }

        public override string ToString()
        {
            return @"NOnes(" + n + @") (" + base.ToString() + @")";
        }
    }
}
