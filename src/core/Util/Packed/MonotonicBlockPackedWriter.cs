using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public sealed class MonotonicBlockPackedWriter : AbstractBlockPackedWriter
    {
        public MonotonicBlockPackedWriter(DataOutput output, int blockSize)
            : base(output, blockSize)
        {
        }

        public override void Add(long l)
        {
            //assert l >= 0;
            base.Add(l);
        }

        protected override void Flush()
        {
            //assert off > 0;

            // TODO: perform a true linear regression?
            long min = values[0];
            float avg = off == 1 ? 0f : (float)(values[off - 1] - min) / (off - 1);

            long maxZigZagDelta = 0;
            for (int i = 0; i < off; ++i)
            {
                values[i] = ZigZagEncode(values[i] - min - (long)(avg * i));
                maxZigZagDelta = Math.Max(maxZigZagDelta, values[i]);
            }

            output.WriteVLong(min);
            output.WriteInt(Number.FloatToIntBits(avg));
            if (maxZigZagDelta == 0)
            {
                output.WriteVInt(0);
            }
            else
            {
                int bitsRequired = PackedInts.BitsRequired(maxZigZagDelta);
                output.WriteVInt(bitsRequired);
                WriteValues(bitsRequired);
            }

            off = 0;
        }
    }
}
