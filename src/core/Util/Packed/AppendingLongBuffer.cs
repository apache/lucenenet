using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public sealed class AppendingLongBuffer : AbstractAppendingLongBuffer
    {
        public AppendingLongBuffer()
            : base(16)
        {
        }

        internal override long Get(int block, int element)
        {
            if (block == valuesOff)
            {
                return pending[element];
            }
            else if (deltas[block] == null)
            {
                return minValues[block];
            }
            else
            {
                return minValues[block] + deltas[block].Get(element);
            }
        }

        internal void PackPendingValues()
        {
            //assert pendingOff == MAX_PENDING_COUNT;

            // compute max delta
            long minValue = pending[0];
            long maxValue = pending[0];
            for (int i = 1; i < pendingOff; ++i)
            {
                minValue = Math.Min(minValue, pending[i]);
                maxValue = Math.Max(maxValue, pending[i]);
            }
            long delta = maxValue - minValue;

            minValues[valuesOff] = minValue;
            if (delta != 0)
            {
                // build a new packed reader
                int bitsRequired = delta < 0 ? 64 : PackedInts.BitsRequired(delta);
                for (int i = 0; i < pendingOff; ++i)
                {
                    pending[i] -= minValue;
                }
                PackedInts.IMutable mutable = PackedInts.GetMutable(pendingOff, bitsRequired, PackedInts.COMPACT);
                for (int i = 0; i < pendingOff; )
                {
                    i += mutable.Set(i, pending, i, pendingOff - i);
                }
                deltas[valuesOff] = mutable;
            }
        }

        internal override Iterator GetIterator()
        {
            return new Iterator(this);
        }

        public sealed class Iterator : AbstractAppendingLongBuffer.Iterator
        {
            private readonly AppendingLongBuffer parent;

            internal Iterator(AppendingLongBuffer parent)
                : base(parent)
            {
                this.parent = parent;
            }

            void FillValues()
            {
                if (vOff == parent.valuesOff)
                {
                    currentValues = parent.pending;
                }
                else if (parent.deltas[vOff] == null)
                {
                    Arrays.Fill(currentValues, parent.minValues[vOff]);
                }
                else
                {
                    for (int k = 0; k < MAX_PENDING_COUNT; )
                    {
                        k += parent.deltas[vOff].Get(k, currentValues, k, MAX_PENDING_COUNT - k);
                    }
                    for (int k = 0; k < MAX_PENDING_COUNT; ++k)
                    {
                        currentValues[k] += parent.minValues[vOff];
                    }
                }
            }
        }
    }
}
