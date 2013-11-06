using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public class FourFlagsIntEncoder : ChunksIntEncoder
    {
        private static readonly byte[][] ENCODE_TABLE = new byte[][]
        {
            new byte[] { 0x00, 0x00, 0x00, 0x00 }, 
            new byte[] { 0x01, 0x04, 0x10, 0x40 }, 
            new byte[] { 0x02, 0x08, 0x20, (byte)0x80 },
            new byte[] { 0x03, 0x0C, 0x30, (byte)0xC0 }
        };

        public FourFlagsIntEncoder()
            : base(4)
        {
        }

        public override void Encode(IntsRef values, BytesRef buf)
        {
            buf.offset = buf.length = 0;
            int upto = values.offset + values.length;
            for (int i = values.offset; i < upto; i++)
            {
                int value = values.ints[i];
                if (value <= 3)
                {
                    indicator |= ENCODE_TABLE[value][ordinal];
                }
                else
                {
                    encodeQueue.ints[encodeQueue.length++] = value - 4;
                }

                ++ordinal;
                if (ordinal == 4)
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
            return new FourFlagsIntDecoder();
        }

        public override string ToString()
        {
            return @"FourFlags(VInt)";
        }
    }
}
