using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Encoding
{
    public abstract class ChunksIntEncoder : IntEncoder
    {
        protected readonly IntsRef encodeQueue;
        protected int indicator = 0;
        protected byte ordinal = 0;

        protected ChunksIntEncoder(int chunkSize)
        {
            encodeQueue = new IntsRef(chunkSize);
        }

        protected virtual void EncodeChunk(BytesRef buf)
        {
            int maxBytesRequired = buf.length + 1 + encodeQueue.length * 4;
            if (buf.bytes.Length < maxBytesRequired)
            {
                buf.Grow(maxBytesRequired);
            }

            buf.bytes[buf.length++] = ((sbyte)indicator);
            for (int i = 0; i < encodeQueue.length; i++)
            {
                int value = encodeQueue.ints[i];
                if ((value & ~0x7F) == 0)
                {
                    buf.bytes[buf.length] = (sbyte)value;
                    buf.length++;
                }
                else if ((value & ~0x3FFF) == 0)
                {
                    buf.bytes[buf.length] = (sbyte)(0x80 | ((value & 0x3F80) >> 7));
                    buf.bytes[buf.length + 1] = (sbyte)(value & 0x7F);
                    buf.length += 2;
                }
                else if ((value & ~0x1FFFFF) == 0)
                {
                    buf.bytes[buf.length] = (sbyte)(0x80 | ((value & 0x1FC000) >> 14));
                    buf.bytes[buf.length + 1] = (sbyte)(0x80 | ((value & 0x3F80) >> 7));
                    buf.bytes[buf.length + 2] = (sbyte)(value & 0x7F);
                    buf.length += 3;
                }
                else if ((value & ~0xFFFFFFF) == 0)
                {
                    buf.bytes[buf.length] = (sbyte)(0x80 | ((value & 0xFE00000) >> 21));
                    buf.bytes[buf.length + 1] = (sbyte)(0x80 | ((value & 0x1FC000) >> 14));
                    buf.bytes[buf.length + 2] = (sbyte)(0x80 | ((value & 0x3F80) >> 7));
                    buf.bytes[buf.length + 3] = (sbyte)(value & 0x7F);
                    buf.length += 4;
                }
                else
                {
                    buf.bytes[buf.length] = (sbyte)(0x80 | ((value & 0xF0000000) >> 28));
                    buf.bytes[buf.length + 1] = (sbyte)(0x80 | ((value & 0xFE00000) >> 21));
                    buf.bytes[buf.length + 2] = (sbyte)(0x80 | ((value & 0x1FC000) >> 14));
                    buf.bytes[buf.length + 3] = (sbyte)(0x80 | ((value & 0x3F80) >> 7));
                    buf.bytes[buf.length + 4] = (sbyte)(value & 0x7F);
                    buf.length += 5;
                }
            }

            ordinal = 0;
            indicator = 0;
            encodeQueue.length = 0;
        }
    }
}
