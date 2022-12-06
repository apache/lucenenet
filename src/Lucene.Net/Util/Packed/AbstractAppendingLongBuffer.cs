using Lucene.Net.Diagnostics;
using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Util.Packed
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Common functionality shared by <see cref="AppendingDeltaPackedInt64Buffer"/> and <see cref="MonotonicAppendingInt64Buffer"/>. 
    /// <para/>
    /// NOTE: This was AbstractAppendingLongBuffer in Lucene
    /// </summary>
    public abstract class AbstractAppendingInt64Buffer : Int64Values // LUCENENET NOTE: made public rather than internal because has public subclasses
    {
        internal const int MIN_PAGE_SIZE = 64;

        // More than 1M doesn't really makes sense with these appending buffers
        // since their goal is to try to have small numbers of bits per value
        internal const int MAX_PAGE_SIZE = 1 << 20;

        internal readonly int pageShift, pageMask;
        internal PackedInt32s.Reader[] values;
        private long valuesBytes;
        internal int valuesOff;
        internal long[] pending;
        internal int pendingOff;
        internal float acceptableOverheadRatio;

        private protected AbstractAppendingInt64Buffer(int initialBlockCount, int pageSize, float acceptableOverheadRatio) // LUCENENET: Changed from internal to private protected
        {
            values = new PackedInt32s.Reader[initialBlockCount];
            pending = new long[pageSize];
            pageShift = PackedInt32s.CheckBlockSize(pageSize, MIN_PAGE_SIZE, MAX_PAGE_SIZE);
            pageMask = pageSize - 1;
            valuesOff = 0;
            pendingOff = 0;
            this.acceptableOverheadRatio = acceptableOverheadRatio;
        }

        public int PageSize => pageMask + 1;

        /// <summary>
        /// Get the number of values that have been added to the buffer.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public long Count
        {
            get
            {
                long size = pendingOff;
                if (valuesOff > 0)
                {
                    size += values[valuesOff - 1].Count;
                }
                if (valuesOff > 1)
                {
                    size += (long)(valuesOff - 1) * PageSize;
                }
                return size;
            }
        }

        /// <summary>
        /// Append a value to this buffer. </summary>
        public void Add(long l)
        {
            if (pending is null)
            {
                throw IllegalStateException.Create("this buffer is frozen");
            }
            if (pendingOff == pending.Length)
            {
                // check size
                if (values.Length == valuesOff)
                {
                    int newLength = ArrayUtil.Oversize(valuesOff + 1, 8);
                    Grow(newLength);
                }
                PackPendingValues();
                valuesBytes += values[valuesOff].RamBytesUsed();
                ++valuesOff;
                // reset pending buffer
                pendingOff = 0;
            }
            pending[pendingOff++] = l;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Grow(int newBlockCount)
        {
            Array.Resize<PackedInt32s.Reader>(ref values, newBlockCount);
        }

        internal abstract void PackPendingValues();

        public override sealed long Get(long index)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(index >= 0 && index < Count);
            int block = (int)(index >> pageShift);
            int element = (int)(index & pageMask);
            return Get(block, element);
        }

        /// <summary>
        /// Bulk get: read at least one and at most <paramref name="len"/> <see cref="long"/>s starting
        /// from <paramref name="index"/> into <c>arr[off:off+len]</c> and return
        /// the actual number of values that have been read.
        /// </summary>
        public int Get(long index, long[] arr, int off, int len)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(len > 0, "len must be > 0 (got {0})", len);
                Debugging.Assert(index >= 0 && index < Count);
                Debugging.Assert(off + len <= arr.Length);
            }

            int block = (int)(index >> pageShift);
            int element = (int)(index & pageMask);
            return Get(block, element, arr, off, len);
        }

        internal abstract long Get(int block, int element);

        internal abstract int Get(int block, int element, long[] arr, int off, int len);

        /// <summary>
        /// Return an iterator over the values of this buffer. 
        /// </summary>
        public virtual Iterator GetIterator()
        {
            return new Iterator(this);
        }

        public sealed class Iterator
        {
            private readonly AbstractAppendingInt64Buffer outerInstance;

            internal long[] currentValues;
            internal int vOff, pOff;
            internal int currentCount; // number of entries of the current page

            internal Iterator(AbstractAppendingInt64Buffer outerInstance)
            {
                this.outerInstance = outerInstance;
                vOff = pOff = 0;
                if (outerInstance.valuesOff == 0)
                {
                    currentValues = outerInstance.pending;
                    currentCount = outerInstance.pendingOff;
                }
                else
                {
                    currentValues = new long[outerInstance.values[0].Count];
                    FillValues();
                }
            }

            internal void FillValues()
            {
                if (vOff == outerInstance.valuesOff)
                {
                    currentValues = outerInstance.pending;
                    currentCount = outerInstance.pendingOff;
                }
                else
                {
                    currentCount = outerInstance.values[vOff].Count;
                    for (int k = 0; k < currentCount; )
                    {
                        k += outerInstance.Get(vOff, k, currentValues, k, currentCount - k);
                    }
                }
            }

            /// <summary>
            /// Whether or not there are remaining values. </summary>
            public bool HasNext => pOff < currentCount;

            /// <summary>
            /// Return the next long in the buffer. </summary>
            public long Next()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(HasNext);
                long result = currentValues[pOff++];
                if (pOff == currentCount)
                {
                    vOff += 1;
                    pOff = 0;
                    if (vOff <= outerInstance.valuesOff)
                    {
                        FillValues();
                    }
                    else
                    {
                        currentCount = 0;
                    }
                }
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual long BaseRamBytesUsed()
        {
            return RamUsageEstimator.NUM_BYTES_OBJECT_HEADER 
                + 2 * RamUsageEstimator.NUM_BYTES_OBJECT_REF 
                + 2 * RamUsageEstimator.NUM_BYTES_INT32 
                + 2 * RamUsageEstimator.NUM_BYTES_INT32 
                + RamUsageEstimator.NUM_BYTES_SINGLE 
                + RamUsageEstimator.NUM_BYTES_INT64; // valuesBytes -  acceptable overhead -  pageShift, pageMask -  the 2 offsets -  the 2 arrays
        }

        /// <summary>
        /// Return the number of bytes used by this instance. </summary>
        public virtual long RamBytesUsed()
        {
            // TODO: this is called per-doc-per-norms/dv-field, can we optimize this?
            long bytesUsed = RamUsageEstimator.AlignObjectSize(BaseRamBytesUsed()) + (pending != null ? RamUsageEstimator.SizeOf(pending) : 0L) + RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + (long)RamUsageEstimator.NUM_BYTES_OBJECT_REF * values.Length); // values

            return bytesUsed + valuesBytes;
        }

        /// <summary>
        /// Pack all pending values in this buffer. Subsequent calls to <see cref="Add(long)"/> will fail. </summary>
        public virtual void Freeze()
        {
            if (pendingOff > 0)
            {
                if (values.Length == valuesOff)
                {
                    Grow(valuesOff + 1); // don't oversize!
                }
                PackPendingValues();
                valuesBytes += values[valuesOff].RamBytesUsed();
                ++valuesOff;
                pendingOff = 0;
            }
            pending = null;
        }
    }
}