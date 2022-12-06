using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
    /// A simple append only random-access <see cref="BytesRef"/> array that stores full
    /// copies of the appended bytes in a <see cref="ByteBlockPool"/>.
    /// <para/>
    /// <b>Note: this class is not Thread-Safe!</b>
    /// <para/>
    /// @lucene.internal
    /// @lucene.experimental
    /// </summary>
    public sealed class BytesRefArray
    {
        private readonly ByteBlockPool pool;
        private int[] offsets = new int[1];
        private int lastElement = 0;
        private int currentOffset = 0;
        private readonly Counter bytesUsed;

        /// <summary>
        /// Creates a new <see cref="BytesRefArray"/> with a counter to track allocated bytes
        /// </summary>
        public BytesRefArray(Counter bytesUsed)
        {
            this.pool = new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(bytesUsed));
            pool.NextBuffer();
            bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + RamUsageEstimator.NUM_BYTES_INT32);
            this.bytesUsed = bytesUsed;
        }

        /// <summary>
        /// Clears this <see cref="BytesRefArray"/>
        /// </summary>
        public void Clear()
        {
            lastElement = 0;
            currentOffset = 0;
            Arrays.Fill(offsets, 0);
            pool.Reset(false, true); // no need to 0 fill the buffers we control the allocator
        }

        /// <summary>
        /// Appends a copy of the given <see cref="BytesRef"/> to this <see cref="BytesRefArray"/>. </summary>
        /// <param name="bytes"> The bytes to append </param>
        /// <returns> The index of the appended bytes </returns>
        public int Append(BytesRef bytes)
        {
            if (lastElement >= offsets.Length)
            {
                int oldLen = offsets.Length;
                offsets = ArrayUtil.Grow(offsets, offsets.Length + 1);
                bytesUsed.AddAndGet((offsets.Length - oldLen) * RamUsageEstimator.NUM_BYTES_INT32);
            }
            pool.Append(bytes);
            offsets[lastElement++] = currentOffset;
            currentOffset += bytes.Length;
            return lastElement - 1;
        }

        /// <summary>
        /// Returns the current size of this <see cref="BytesRefArray"/>.
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        /// <returns> The current size of this <see cref="BytesRefArray"/> </returns>
        public int Length => lastElement;

        /// <summary>
        /// Returns the <i>n'th</i> element of this <see cref="BytesRefArray"/> </summary>
        /// <param name="spare"> A spare <see cref="BytesRef"/> instance </param>
        /// <param name="index"> The elements index to retrieve </param>
        /// <returns> The <i>n'th</i> element of this <see cref="BytesRefArray"/> </returns>
        public BytesRef Get(BytesRef spare, int index)
        {
            if (lastElement > index)
            {
                int offset = offsets[index];
                int length = index == lastElement - 1 ? currentOffset - offset : offsets[index + 1] - offset;
                if (Debugging.AssertsEnabled) Debugging.Assert(spare.Offset == 0);
                spare.Grow(length);
                spare.Length = length;
                pool.ReadBytes(offset, spare.Bytes, spare.Offset, spare.Length);
                return spare;
            }
            throw new IndexOutOfRangeException("index " + index + " must be less than the size: " + lastElement);
        }

        private int[] Sort(IComparer<BytesRef> comp)
        {
            int[] orderedEntries = new int[Length];
            for (int i = 0; i < orderedEntries.Length; i++)
            {
                orderedEntries[i] = i;
            }
            new IntroSorterAnonymousClass(this, comp, orderedEntries).Sort(0, Length);
            return orderedEntries;
        }

        private sealed class IntroSorterAnonymousClass : IntroSorter
        {
            private readonly BytesRefArray outerInstance;

            private readonly IComparer<BytesRef> comp;
            private readonly int[] orderedEntries;

            public IntroSorterAnonymousClass(BytesRefArray outerInstance, IComparer<BytesRef> comp, int[] orderedEntries)
            {
                this.outerInstance = outerInstance;
                this.comp = comp;
                this.orderedEntries = orderedEntries;
                pivot = new BytesRef();
                scratch1 = new BytesRef();
                scratch2 = new BytesRef();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void Swap(int i, int j)
            {
                int o = orderedEntries[i];
                orderedEntries[i] = orderedEntries[j];
                orderedEntries[j] = o;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override int Compare(int i, int j)
            {
                int idx1 = orderedEntries[i], idx2 = orderedEntries[j];
                return comp.Compare(outerInstance.Get(scratch1, idx1), outerInstance.Get(scratch2, idx2));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void SetPivot(int i)
            {
                int index = orderedEntries[i];
                outerInstance.Get(pivot, index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override int ComparePivot(int j)
            {
                int index = orderedEntries[j];
                return comp.Compare(pivot, outerInstance.Get(scratch2, index));
            }

            private readonly BytesRef pivot;
            private readonly BytesRef scratch1;
            private readonly BytesRef scratch2;
        }

        /// <summary>
        /// Sugar for <see cref="GetIterator(IComparer{BytesRef})"/> with a <c>null</c> comparer
        /// </summary>
        [Obsolete("Use GetEnumerator() instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IBytesRefIterator GetIterator()
        {
            return GetIterator(null);
        }

        /// <summary>
        /// <para>
        /// Returns a <see cref="IBytesRefIterator"/> with point in time semantics. The
        /// iterator provides access to all so far appended <see cref="BytesRef"/> instances.
        /// </para>
        /// <para>
        /// If a non <c>null</c> <see cref="T:IComparer{BytesRef}"/> is provided the iterator will
        /// iterate the byte values in the order specified by the comparer. Otherwise
        /// the order is the same as the values were appended.
        /// </para>
        /// <para>
        /// This is a non-destructive operation.
        /// </para>
        /// </summary>
        [Obsolete("Use GetEnumerator(IComparer<BytesRef>) instead. This method will be removed in 4.8.0 release candidate"), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IBytesRefIterator GetIterator(IComparer<BytesRef> comp)
        {
            BytesRef spare = new BytesRef();
            int size = Length;
            int[] indices = comp is null ? null : Sort(comp);
            return new BytesRefIteratorAnonymousClass(this, comp, spare, size, indices);
        }

        [Obsolete("This class will be removed in 4.8.0 release candidate"), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private sealed class BytesRefIteratorAnonymousClass : IBytesRefIterator
        {
            private readonly BytesRefArray outerInstance;

            private readonly IComparer<BytesRef> comp;
            private readonly BytesRef spare;
            private readonly int size;
            private readonly int[] indices;

            public BytesRefIteratorAnonymousClass(BytesRefArray outerInstance, IComparer<BytesRef> comp, BytesRef spare, int size, int[] indices)
            {
                this.outerInstance = outerInstance;
                this.comp = comp;
                this.spare = spare;
                this.size = size;
                this.indices = indices;
                pos = 0;
            }

            internal int pos;

            public BytesRef Next()
            {
                if (pos < size)
                {
                    return outerInstance.Get(spare, indices is null ? pos++ : indices[pos++]);
                }
                return null;
            }

            public IComparer<BytesRef> Comparer => comp;
        }

        /// <summary>
        /// Sugar for <see cref="GetEnumerator(IComparer{BytesRef})"/> with a <c>null</c> comparer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBytesRefEnumerator GetEnumerator()
            => GetEnumerator(null);


        /// <summary>
        /// <para>
        /// Returns a <see cref="IBytesRefEnumerator"/> with point in time semantics. The
        /// enumerator provides access to all so far appended <see cref="BytesRef"/> instances.
        /// </para>
        /// <para>
        /// If a non <c>null</c> <see cref="T:IComparer{BytesRef}"/> is provided the enumerator will
        /// iterate the byte values in the order specified by the comparer. Otherwise
        /// the order is the same as the values were appended.
        /// </para>
        /// <para>
        /// This is a non-destructive operation.
        /// </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IBytesRefEnumerator GetEnumerator(IComparer<BytesRef> comparer)
        {
            int[] indices = comparer is null ? null : Sort(comparer);
            return new Enumerator(this, comparer, this.Length, indices);
        }

        private struct Enumerator : IBytesRefEnumerator
        {
            private readonly IComparer<BytesRef> comparer;
            private readonly BytesRef spare;
            private readonly int size;
            private readonly int[] indices;

            private readonly BytesRefArray bytesRefArray;
            private int pos;

            public Enumerator(BytesRefArray bytesRefArray, IComparer<BytesRef> comparer, int size, int[] indices)
            {
                this.spare = new BytesRef();
                this.pos = 0;
                this.Current = null;
                this.bytesRefArray = bytesRefArray;
                this.comparer = comparer;
                this.size = size;
                this.indices = indices;
            }

            public BytesRef Current { get; private set; }

            public bool MoveNext()
            {
                if (pos < size)
                {
                    Current = bytesRefArray.Get(spare, indices is null ? pos++ : indices[pos++]);
                    return true;
                }
                Current = null;
                return false;
            }

            public IComparer<BytesRef> Comparer => comparer;
        }
    }
}