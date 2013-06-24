using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public abstract class AbstractAppendingLongBuffer
    {
        internal const int BLOCK_BITS = 10;
        internal const int MAX_PENDING_COUNT = 1 << BLOCK_BITS;
        internal const int BLOCK_MASK = MAX_PENDING_COUNT - 1;

        internal long[] minValues;
        internal PackedInts.IReader[] deltas;
        private long deltasBytes;
        internal int valuesOff;
        internal long[] pending;
        internal int pendingOff;

        internal AbstractAppendingLongBuffer(int initialBlockCount)
        {
            minValues = new long[16];
            deltas = new PackedInts.IReader[16];
            pending = new long[MAX_PENDING_COUNT];
            valuesOff = 0;
            pendingOff = 0;
        }

        public long Size
        {
            get { return valuesOff * (long)MAX_PENDING_COUNT + pendingOff; }
        }

        public void Add(long l)
        {
            if (pendingOff == MAX_PENDING_COUNT)
            {
                // check size
                if (deltas.Length == valuesOff)
                {
                    int newLength = ArrayUtil.Oversize(valuesOff + 1, 8);
                    Grow(newLength);
                }
                PackPendingValues();
                if (deltas[valuesOff] != null)
                {
                    deltasBytes += deltas[valuesOff].RamBytesUsed();
                }
                ++valuesOff;
                // reset pending buffer
                pendingOff = 0;
            }
            pending[pendingOff++] = l;
        }

        internal virtual void Grow(int newBlockCount)
        {
            minValues = Arrays.CopyOf(minValues, newBlockCount);
            deltas = Arrays.CopyOf(deltas, newBlockCount);
        }

        internal abstract void PackPendingValues();

        public long Get(long index)
        {
            if (index < 0 || index >= Size)
            {
                throw new IndexOutOfRangeException("" + index);
            }
            int block = (int)(index >> BLOCK_BITS);
            int element = (int)(index & BLOCK_MASK);
            return Get(block, element);
        }

        internal abstract long Get(int block, int element);

        internal abstract Iterator GetIterator();
        
        public abstract class Iterator
        {
            private readonly AbstractAppendingLongBuffer parent;
            internal long[] currentValues;
            internal int vOff, pOff;

            internal Iterator(AbstractAppendingLongBuffer parent)
            {
                this.parent = parent;
                vOff = pOff = 0;
                if (parent.valuesOff == 0)
                {
                    currentValues = parent.pending;
                }
                else
                {
                    currentValues = new long[MAX_PENDING_COUNT];
                    FillValues();
                }
            }

            internal abstract void FillValues();

            /** Whether or not there are remaining values. */
            public bool HasNext()
            {
                return vOff < parent.valuesOff || (vOff == parent.valuesOff && pOff < parent.pendingOff);
            }

            /** Return the next long in the buffer. */
            public long Next()
            {
                //assert hasNext();
                long result = currentValues[pOff++];
                if (pOff == MAX_PENDING_COUNT)
                {
                    vOff += 1;
                    pOff = 0;
                    if (vOff <= parent.valuesOff)
                    {
                        FillValues();
                    }
                }
                return result;
            }
        }

        internal virtual long BaseRamBytesUsed
        {
            get
            {
                return RamUsageEstimator.NUM_BYTES_OBJECT_HEADER
                    + 3 * RamUsageEstimator.NUM_BYTES_OBJECT_REF // the 3 arrays
                    + 2 * RamUsageEstimator.NUM_BYTES_INT; // the 2 offsets
            }
        }

        public virtual long RamBytesUsed
        {
            get
            {
                // TODO: this is called per-doc-per-norms/dv-field, can we optimize this?
                long bytesUsed = RamUsageEstimator.AlignObjectSize(BaseRamBytesUsed)
                    + RamUsageEstimator.NUM_BYTES_LONG // valuesBytes
                    + RamUsageEstimator.SizeOf(pending)
                    + RamUsageEstimator.SizeOf(minValues)
                    + RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + (long)RamUsageEstimator.NUM_BYTES_OBJECT_REF * deltas.Length); // values

                return bytesUsed + deltasBytes;
            }
        }
    }
}
