using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public sealed class MonotonicAppendingLongBuffer : AbstractAppendingLongBuffer
    {
        internal static long ZigZagDecode(long n)
        {
            return (Number.URShift(n, 1) ^ -(n & 1));
        }

        internal static long ZigZagEncode(long n)
        {
            return (n >> 63) ^ (n << 1);
        }

        private float[] averages;

        public MonotonicAppendingLongBuffer()
            : base(16)
        {
            averages = new float[16];
        }

        internal long Get(int block, int element)
        {
            if (block == valuesOff)
            {
                return pending[element];
            }
            else
            {
                long baselong = minValues[block] + (long)(averages[block] * (long)element);
                if (deltas[block] == null)
                {
                    return baselong;
                }
                else
                {
                    return baselong + ZigZagDecode(deltas[block].Get(element));
                }
            }
        }

        internal override void Grow(int newBlockCount)
        {
            base.Grow(newBlockCount);
            this.averages = Arrays.CopyOf(averages, newBlockCount);
        }

        internal override void PackPendingValues()
        {
            //assert pendingOff == MAX_PENDING_COUNT;

            minValues[valuesOff] = pending[0];
            averages[valuesOff] = (float)(pending[BLOCK_MASK] - pending[0]) / BLOCK_MASK;

            for (int i = 0; i < MAX_PENDING_COUNT; ++i)
            {
                pending[i] = ZigZagEncode(pending[i] - minValues[valuesOff] - (long)(averages[valuesOff] * (long)i));
            }
            long maxDelta = 0;
            for (int i = 0; i < MAX_PENDING_COUNT; ++i)
            {
                if (pending[i] < 0)
                {
                    maxDelta = -1;
                    break;
                }
                else
                {
                    maxDelta = Math.Max(maxDelta, pending[i]);
                }
            }
            if (maxDelta != 0)
            {
                int bitsRequired = maxDelta < 0 ? 64 : PackedInts.BitsRequired(maxDelta);
                PackedInts.IMutable mutable = PackedInts.GetMutable(pendingOff, bitsRequired, PackedInts.COMPACT);
                for (int i = 0; i < pendingOff; )
                {
                    i += mutable.Set(i, pending, i, pendingOff - i);
                }
                deltas[valuesOff] = mutable;
            }
        }

        public Iterator Iterator()
        {
            return new Iterator(this);
        }

        public sealed class Iterator : AbstractAppendingLongBuffer.Iterator
        {
            private readonly MonotonicAppendingLongBuffer parent;
            public Iterator(MonotonicAppendingLongBuffer parent)
                : base(parent)
            {
                this.parent = parent;
            }

            internal void FillValues()
            {
                if (vOff == parent.valuesOff)
                {
                    currentValues = parent.pending;
                }
                else if (parent.deltas[vOff] == null)
                {
                    for (int k = 0; k < MAX_PENDING_COUNT; ++k)
                    {
                        currentValues[k] = parent.minValues[vOff] + (long)(parent.averages[vOff] * (long)k);
                    }
                }
                else
                {
                    for (int k = 0; k < MAX_PENDING_COUNT; )
                    {
                        k += parent.deltas[vOff].Get(k, currentValues, k, MAX_PENDING_COUNT - k);
                    }
                    for (int k = 0; k < MAX_PENDING_COUNT; ++k)
                    {
                        currentValues[k] = parent.minValues[vOff] + (long)(parent.averages[vOff] * (long)k) + ZigZagDecode(currentValues[k]);
                    }
                }
            }
        }

        internal override long BaseRamBytesUsed()
        {
            return base.BaseRamBytesUsed()
                + RamUsageEstimator.NUM_BYTES_OBJECT_REF; // the additional array
        }

        public override long RamBytesUsed()
        {
            return base.RamBytesUsed()
                + RamUsageEstimator.SizeOf(averages);
        }
    }
}
