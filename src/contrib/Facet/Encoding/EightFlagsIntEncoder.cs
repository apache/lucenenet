using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public class EightFlagsIntEncoder : ChunksIntEncoder
    {
        private static readonly byte[] ENCODE_TABLE = new byte[] {
            0x1, 0x2, 0x4, 0x8, 0x10, 0x20, 0x40, (byte)0x80
        };

        public EightFlagsIntEncoder()
            : base(8)
        {
        }

        public override void Encode(IntsRef values, BytesRef buf)
        {
            buf.offset = buf.length = 0;
            int upto = values.offset + values.length;
            for (int i = values.offset; i < upto; i++)
            {
                int value = values.ints[i];
                if (value == 1)
                {
                    indicator |= ENCODE_TABLE[ordinal];
                }
                else
                {
                    encodeQueue.ints[encodeQueue.length++] = value - 2;
                }

                ++ordinal;
                if (ordinal == 8)
                {
                    EncodeChunk(buf);
                }
            }

            if (ordinal != 0)
            {
                EncodeChunk(buf);
            }
        }

        public override IntDecoder CreateMatchingDecoder()
        {
            return new EightFlagsIntDecoder();
        }

        public override string ToString()
        {
            return @"EightFlags(VInt)";
        }
    }
}
