using System;
using System.Diagnostics;

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
    /// Common functionality shared by <seealso cref="AppendingDeltaPackedLongBuffer"/> and <seealso cref="MonotonicAppendingLongBuffer"/>. </summary>
    public abstract class AbstractAppendingLongBuffer : LongValues // LUCENENET NOTE: made public rather than internal because has public subclasses
    {
        internal const int MIN_PAGE_SIZE = 64;

        // More than 1M doesn't really makes sense with these appending buffers
        // since their goal is to try to have small numbers of bits per value
        internal static readonly int MAX_PAGE_SIZE = 1 << 20;

        internal readonly int PageShift, PageMask;
        internal PackedInts.Reader[] Values;
        private long ValuesBytes;
        internal int ValuesOff;
        internal long[] Pending;
        internal int PendingOff;
        internal float AcceptableOverheadRatio;

        internal AbstractAppendingLongBuffer(int initialBlockCount, int pageSize, float acceptableOverheadRatio)
        {
            Values = new PackedInts.Reader[initialBlockCount];
            Pending = new long[pageSize];
            PageShift = PackedInts.CheckBlockSize(pageSize, MIN_PAGE_SIZE, MAX_PAGE_SIZE);
            PageMask = pageSize - 1;
            ValuesOff = 0;
            PendingOff = 0;
            this.AcceptableOverheadRatio = acceptableOverheadRatio;
        }

        public int PageSize // LUCENENET TODO: rename PageCount ?
        {
            get { return PageMask + 1; }
        }

        /// <summary>
        /// Get the number of values that have been added to the buffer. </summary>
        public long Size // LUCENENET TODO: rename Count
        {
            get
            {
                long size = PendingOff;
                if (ValuesOff > 0)
                {
                    size += Values[ValuesOff - 1].Size();
                }
                if (ValuesOff > 1)
                {
                    size += (long)(ValuesOff - 1) * PageSize;
                }
                return size;
            }
        }

        /// <summary>
        /// Append a value to this buffer. </summary>
        public void Add(long l)
        {
            if (Pending == null)
            {
                throw new Exception("this buffer is frozen");
            }
            if (PendingOff == Pending.Length)
            {
                // check size
                if (Values.Length == ValuesOff)
                {
                    int newLength = ArrayUtil.Oversize(ValuesOff + 1, 8);
                    Grow(newLength);
                }
                PackPendingValues();
                ValuesBytes += Values[ValuesOff].RamBytesUsed();
                ++ValuesOff;
                // reset pending buffer
                PendingOff = 0;
            }
            Pending[PendingOff++] = l;
        }

        internal virtual void Grow(int newBlockCount)
        {
            Array.Resize<PackedInts.Reader>(ref Values, newBlockCount);
        }

        internal abstract void PackPendingValues();

        public override sealed long Get(long index)
        {
            Debug.Assert(index >= 0 && index < Size);
            int block = (int)(index >> PageShift);
            int element = (int)(index & PageMask);
            return Get(block, element);
        }

        /// <summary>
        /// Bulk get: read at least one and at most <code>len</code> longs starting
        /// from <code>index</code> into <code>arr[off:off+len]</code> and return
        /// the actual number of values that have been read.
        /// </summary>
        public int Get(long index, long[] arr, int off, int len)
        {
            Debug.Assert(len > 0, "len must be > 0 (got " + len + ")");
            Debug.Assert(index >= 0 && index < Size);
            Debug.Assert(off + len <= arr.Length);

            int block = (int)(index >> PageShift);
            int element = (int)(index & PageMask);
            return Get(block, element, arr, off, len);
        }

        internal abstract long Get(int block, int element);

        internal abstract int Get(int block, int element, long[] arr, int off, int len);

        /* LUCENE TO-DO
      /// <summary>
      /// Return an iterator over the values of this buffer. </summary>
      public virtual Iterator Iterator()
      {
        return new Iterator(this);
      }*/

        public abstract Iterator GetIterator(); // LUCENENET TODO: This was not abstract in the original

        public sealed class Iterator
        {
            private readonly AbstractAppendingLongBuffer OuterInstance;

            internal long[] CurrentValues;
            internal int VOff, POff;
            internal int CurrentCount; // number of entries of the current page

            internal Iterator(AbstractAppendingLongBuffer outerInstance)
            {
                this.OuterInstance = outerInstance;
                VOff = POff = 0;
                if (outerInstance.ValuesOff == 0)
                {
                    CurrentValues = outerInstance.Pending;
                    CurrentCount = outerInstance.PendingOff;
                }
                else
                {
                    CurrentValues = new long[outerInstance.Values[0].Size()];
                    FillValues();
                }
            }

            internal void FillValues()
            {
                if (VOff == OuterInstance.ValuesOff)
                {
                    CurrentValues = OuterInstance.Pending;
                    CurrentCount = OuterInstance.PendingOff;
                }
                else
                {
                    CurrentCount = OuterInstance.Values[VOff].Size();
                    for (int k = 0; k < CurrentCount; )
                    {
                        k += OuterInstance.Get(VOff, k, CurrentValues, k, CurrentCount - k);
                    }
                }
            }

            /// <summary>
            /// Whether or not there are remaining values. </summary>
            public bool HasNext
            {
                get { return POff < CurrentCount; }
            }

            /// <summary>
            /// Return the next long in the buffer. </summary>
            public long Next()
            {
                Debug.Assert(HasNext);
                long result = CurrentValues[POff++];
                if (POff == CurrentCount)
                {
                    VOff += 1;
                    POff = 0;
                    if (VOff <= OuterInstance.ValuesOff)
                    {
                        FillValues();
                    }
                    else
                    {
                        CurrentCount = 0;
                    }
                }
                return result;
            }
        }

        internal virtual long BaseRamBytesUsed()
        {
            return RamUsageEstimator.NUM_BYTES_OBJECT_HEADER 
                + 2 * RamUsageEstimator.NUM_BYTES_OBJECT_REF 
                + 2 * RamUsageEstimator.NUM_BYTES_INT 
                + 2 * RamUsageEstimator.NUM_BYTES_INT 
                + RamUsageEstimator.NUM_BYTES_FLOAT 
                + RamUsageEstimator.NUM_BYTES_LONG; // valuesBytes -  acceptable overhead -  pageShift, pageMask -  the 2 offsets -  the 2 arrays
        }

        /// <summary>
        /// Return the number of bytes used by this instance. </summary>
        public virtual long RamBytesUsed()
        {
            // TODO: this is called per-doc-per-norms/dv-field, can we optimize this?
            long bytesUsed = RamUsageEstimator.AlignObjectSize(BaseRamBytesUsed()) + (Pending != null ? RamUsageEstimator.SizeOf(Pending) : 0L) + RamUsageEstimator.AlignObjectSize(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + (long)RamUsageEstimator.NUM_BYTES_OBJECT_REF * Values.Length); // values

            return bytesUsed + ValuesBytes;
        }

        /// <summary>
        /// Pack all pending values in this buffer. Subsequent calls to <seealso cref="#add(long)"/> will fail. </summary>
        public virtual void Freeze()
        {
            if (PendingOff > 0)
            {
                if (Values.Length == ValuesOff)
                {
                    Grow(ValuesOff + 1); // don't oversize!
                }
                PackPendingValues();
                ValuesBytes += Values[ValuesOff].RamBytesUsed();
                ++ValuesOff;
                PendingOff = 0;
            }
            Pending = null;
        }
    }
}