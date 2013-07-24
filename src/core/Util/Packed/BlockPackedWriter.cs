using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public sealed class BlockPackedWriter : AbstractBlockPackedWriter
    {
        public BlockPackedWriter(DataOutput output, int blockSize)
            : base(output, blockSize)
        {
        }

        protected override void Flush()
        {
            //assert off > 0;
            long min = long.MaxValue, max = long.MinValue;
            for (int i = 0; i < off; ++i)
            {
                min = Math.Min(values[i], min);
                max = Math.Max(values[i], max);
            }

            long delta = max - min;
            int bitsRequired = delta < 0 ? 64 : delta == 0L ? 0 : PackedInts.BitsRequired(delta);
            if (bitsRequired == 64)
            {
                // no need to delta-encode
                min = 0L;
            }
            else if (min > 0L)
            {
                // make min as small as possible so that writeVLong requires fewer bytes
                min = Math.Max(0L, max - PackedInts.MaxValue(bitsRequired));
            }

            int token = (bitsRequired << BPV_SHIFT) | (min == 0 ? MIN_VALUE_EQUALS_0 : 0);
            output.WriteByte((byte)token);

            if (min != 0)
            {
                WriteVLong(output, ZigZagEncode(min) - 1);
            }

            if (bitsRequired > 0)
            {
                if (min != 0)
                {
                    for (int i = 0; i < off; ++i)
                    {
                        values[i] -= min;
                    }
                }
                WriteValues(bitsRequired);
            }

            off = 0;

        }
    }
}
