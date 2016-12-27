using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements. See the NOTICE file distributed with this
     * work for additional information regarding copyright ownership. The ASF
     * licenses this file to You under the Apache License, Version 2.0 (the
     * "License"); you may not use this file except in compliance with the License.
     * You may obtain a copy of the License at
     *
     * http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
     * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
     * License for the specific language governing permissions and limitations under
     * the License.
     */

    /// <summary>
    /// A simple append only random-access <seealso cref="BytesRef"/> array that stores full
    /// copies of the appended bytes in a <seealso cref="ByteBlockPool"/>.
    ///
    ///
    /// <b>Note: this class is not Thread-Safe!</b>
    ///
    /// @lucene.internal
    /// @lucene.experimental
    /// </summary>
    public sealed class BytesRefArray
    {
        private readonly ByteBlockPool Pool;
        private int[] Offsets = new int[1];
        private int LastElement = 0;
        private int CurrentOffset = 0;
        private readonly Counter BytesUsed;

        /// <summary>
        /// Creates a new <seealso cref="BytesRefArray"/> with a counter to track allocated bytes
        /// </summary>
        public BytesRefArray(Counter bytesUsed)
        {
            this.Pool = new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(bytesUsed));
            Pool.NextBuffer();
            bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + RamUsageEstimator.NUM_BYTES_INT);
            this.BytesUsed = bytesUsed;
        }

        /// <summary>
        /// Clears this <seealso cref="BytesRefArray"/>
        /// </summary>
        public void Clear()
        {
            LastElement = 0;
            CurrentOffset = 0;
            Array.Clear(Offsets, 0, Offsets.Length);
            Pool.Reset(false, true); // no need to 0 fill the buffers we control the allocator
        }

        /// <summary>
        /// Appends a copy of the given <seealso cref="BytesRef"/> to this <seealso cref="BytesRefArray"/>. </summary>
        /// <param name="bytes"> the bytes to append </param>
        /// <returns> the index of the appended bytes </returns>
        public int Append(BytesRef bytes)
        {
            if (LastElement >= Offsets.Length)
            {
                int oldLen = Offsets.Length;
                Offsets = ArrayUtil.Grow(Offsets, Offsets.Length + 1);
                BytesUsed.AddAndGet((Offsets.Length - oldLen) * RamUsageEstimator.NUM_BYTES_INT);
            }
            Pool.Append(bytes);
            Offsets[LastElement++] = CurrentOffset;
            CurrentOffset += bytes.Length;
            return LastElement - 1;
        }

        /// <summary>
        /// Returns the current size of this <seealso cref="BytesRefArray"/> </summary>
        /// <returns> the current size of this <seealso cref="BytesRefArray"/> </returns>
        public int Size // LUCENENET TODO: rename Count
        {
            get { return LastElement; }
        }

        /// <summary>
        /// Returns the <i>n'th</i> element of this <seealso cref="BytesRefArray"/> </summary>
        /// <param name="spare"> a spare <seealso cref="BytesRef"/> instance </param>
        /// <param name="index"> the elements index to retrieve </param>
        /// <returns> the <i>n'th</i> element of this <seealso cref="BytesRefArray"/> </returns>
        public BytesRef Get(BytesRef spare, int index)
        {
            if (LastElement > index)
            {
                int offset = Offsets[index];
                int length = index == LastElement - 1 ? CurrentOffset - offset : Offsets[index + 1] - offset;
                Debug.Assert(spare.Offset == 0);
                spare.Grow(length);
                spare.Length = length;
                Pool.ReadBytes(offset, spare.Bytes, spare.Offset, spare.Length);
                return spare;
            }
            throw new System.IndexOutOfRangeException("index " + index + " must be less than the size: " + LastElement);
        }

        private int[] Sort(IComparer<BytesRef> comp)
        {
            int[] orderedEntries = new int[Size];
            for (int i = 0; i < orderedEntries.Length; i++)
            {
                orderedEntries[i] = i;
            }
            new IntroSorterAnonymousInnerClassHelper(this, comp, orderedEntries).Sort(0, Size);
            return orderedEntries;
        }

        private class IntroSorterAnonymousInnerClassHelper : IntroSorter
        {
            private readonly BytesRefArray OuterInstance;

            private IComparer<BytesRef> Comp;
            private int[] OrderedEntries;

            public IntroSorterAnonymousInnerClassHelper(BytesRefArray outerInstance, IComparer<BytesRef> comp, int[] orderedEntries)
            {
                this.OuterInstance = outerInstance;
                this.Comp = comp;
                this.OrderedEntries = orderedEntries;
                pivot = new BytesRef();
                scratch1 = new BytesRef();
                scratch2 = new BytesRef();
            }

            protected override void Swap(int i, int j)
            {
                int o = OrderedEntries[i];
                OrderedEntries[i] = OrderedEntries[j];
                OrderedEntries[j] = o;
            }

            protected override int Compare(int i, int j)
            {
                int idx1 = OrderedEntries[i], idx2 = OrderedEntries[j];
                return Comp.Compare(OuterInstance.Get(scratch1, idx1), OuterInstance.Get(scratch2, idx2));
            }

            protected override int Pivot
            {
                set
                {
                    int index = OrderedEntries[value];
                    OuterInstance.Get(pivot, index);
                }
            }

            protected override int ComparePivot(int j)
            {
                int index = OrderedEntries[j];
                return Comp.Compare(pivot, OuterInstance.Get(scratch2, index));
            }

            private readonly BytesRef pivot;
            private readonly BytesRef scratch1;
            private readonly BytesRef scratch2;
        }

        /// <summary>
        /// sugar for <seealso cref="#iterator(Comparator)"/> with a <code>null</code> comparator
        /// </summary>
        public BytesRefIterator Iterator() // LUCENENET TODO: Rename GetIterator() ? check consistency
        {
            return Iterator(null);
        }

        /// <summary>
        /// <p>
        /// Returns a <seealso cref="BytesRefIterator"/> with point in time semantics. The
        /// iterator provides access to all so far appended <seealso cref="BytesRef"/> instances.
        /// </p>
        /// <p>
        /// If a non <code>null</code> <seealso cref="Comparator"/> is provided the iterator will
        /// iterate the byte values in the order specified by the comparator. Otherwise
        /// the order is the same as the values were appended.
        /// </p>
        /// <p>
        /// this is a non-destructive operation.
        /// </p>
        /// </summary>
        public BytesRefIterator Iterator(IComparer<BytesRef> comp)// LUCENENET TODO: Rename GetIterator() ? check consistency
        {
            BytesRef spare = new BytesRef();
            int size = Size;
            int[] indices = comp == null ? null : Sort(comp);
            return new BytesRefIteratorAnonymousInnerClassHelper(this, comp, spare, size, indices);
        }

        private class BytesRefIteratorAnonymousInnerClassHelper : BytesRefIterator
        {
            private readonly BytesRefArray OuterInstance;

            private IComparer<BytesRef> Comp;
            private BytesRef Spare;
            private int Size;
            private int[] Indices;

            public BytesRefIteratorAnonymousInnerClassHelper(BytesRefArray outerInstance, IComparer<BytesRef> comp, BytesRef spare, int size, int[] indices)
            {
                this.OuterInstance = outerInstance;
                this.Comp = comp;
                this.Spare = spare;
                this.Size = size;
                this.Indices = indices;
                pos = 0;
            }

            internal int pos;

            public virtual BytesRef Next()
            {
                if (pos < Size)
                {
                    return OuterInstance.Get(Spare, Indices == null ? pos++ : Indices[pos++]);
                }
                return null;
            }

            public virtual IComparer<BytesRef> Comparator
            {
                get
                {
                    return Comp;
                }
            }
        }
    }
}