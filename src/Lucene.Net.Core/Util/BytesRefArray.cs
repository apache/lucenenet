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

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    ///     A simple append only random-access <seealso cref="BytesRef" /> array that stores full
    ///     copies of the appended bytes in a <seealso cref="ByteBlockPool" />.
    ///     <b>Note: this class is not Thread-Safe!</b>
    ///     @lucene.internal
    ///     @lucene.experimental
    /// </summary>
    // ReSharper disable CSharpWarnings::CS1574
    public sealed class BytesRefArray : IEnumerable<BytesRef>
    {
        private readonly Counter bytesUsed;
        private readonly ByteBlockPool pool;
        private int currentOffset;
        private int lastElement;
        private int[] offsets = new int[1];

        /// <summary>
        ///     Creates a new <seealso cref="BytesRefArray" /> with a counter to track allocated bytes
        /// </summary>
        public BytesRefArray(Counter bytesUsed)
        {
            this.pool = new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(bytesUsed));
         
            bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_VALUE_TYPE_ARRAY_HEADER + RamUsageEstimator.NUM_BYTES_INT);
            this.bytesUsed = bytesUsed;
        }

        /// <summary>
        ///     Returns the current size of this <seealso cref="BytesRefArray" />
        /// </summary>
        /// <returns> the current size of this <seealso cref="BytesRefArray" /> </returns>
        public int Length
        {
            get { return lastElement; }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the
        ///     collection.
        /// </returns>
        public IEnumerator<BytesRef> GetEnumerator()
        {
            return this.GetEnumerator(null);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator(null);
        }

        /// <summary>
        ///     Clears this <seealso cref="BytesRefArray" />
        /// </summary>
        public void Clear()
        {
            lastElement = 0;
            currentOffset = 0;
            Array.Clear(offsets, 0, offsets.Length);
            pool.Reset(false, true); // no need to 0 fill the buffers we control the allocator
        }

        /// <summary>
        ///     Appends a copy of the given <seealso cref="BytesRef" /> to this <seealso cref="BytesRefArray" />.
        /// </summary>
        /// <param name="bytes"> the bytes to append </param>
        /// <returns> the index of the appended bytes </returns>
        public int Append(BytesRef bytes)
        {
            if (lastElement >= offsets.Length)
            {
                var oldLen = offsets.Length;
                offsets = offsets.Grow(offsets.Length + 1);
                bytesUsed.AddAndGet((offsets.Length - oldLen)*RamUsageEstimator.NUM_BYTES_INT);
            }
            pool.Append(bytes);
            offsets[lastElement++] = currentOffset;
            currentOffset += bytes.Length;
            return lastElement - 1;
        }

        /// <summary>
        ///     Returns the <i>n'th</i> element of this <seealso cref="BytesRefArray" />
        /// </summary>
        /// <param name="spare"> a spare <seealso cref="BytesRef" /> instance </param>
        /// <param name="index"> the elements index to retrieve </param>
        /// <returns> the <i>n'th</i> element of this <seealso cref="BytesRefArray" /> </returns>
        public BytesRef Retrieve(BytesRefBuilder spare, int index)
        {
            Debug.Assert(spare != null, "spare must never be null");
            //Debug.Assert(spare.Offset == 0);

            if (index > this.lastElement)
                throw new IndexOutOfRangeException("index " + index + " must be less than the size: " +
                                                   this.lastElement);
            var offset = this.offsets[index];
            var length = index == this.lastElement - 1 ? this.currentOffset - offset : this.offsets[index + 1] - offset;

      
            spare.Length = length;
            this.pool.ReadBytes(offset, spare.Bytes, 0, spare.Length);
            return spare.ToBytesRef();
        }

        /// <summary>
        /// Sorts the specified comp.
        /// </summary>
        /// <param name="comp">The comp.</param>
        /// <returns>System.Int32[].</returns>
        private int[] Sort(IComparer<BytesRef> comp)
        {
            var orderedEntries = new int[this.Length];
            for (var i = 0; i < orderedEntries.Length; i++)
            {
                orderedEntries[i] = i;
            }

            using (var sorter = new ByteRefArraySorter(this, comp, orderedEntries))
            {
                sorter.Sort(0, this.Length);
            }

            return orderedEntries;
        }


        /// <summary>
        /// <p>
        /// Returns a <seealso cref="BytesRefEnumerator" /> with point in time semantics. The
        /// iterator provides access to all so far appended <seealso cref="BytesRef" /> instances.
        /// </p>
        /// <p>
        /// If a non <code>null</code><seealso cref="IComparer{BytesRef}" /> is provided the iterator will
        /// iterate the byte values in the order specified by the comparator. Otherwise
        /// the order is the same as the values were appended.
        /// </p>
        /// <p>
        /// this is a non-destructive operation.
        /// </p>
        /// </summary>
        /// <param name="comp">The comp.</param>
        /// <returns>IEnumerator&lt;BytesRef&gt;.</returns>
        public IEnumerator<BytesRef> GetEnumerator(IComparer<BytesRef> comp)
        {
            var spare = new BytesRef();
            var size = this.Length;
            var indices = comp == null ? null : Sort(comp);
            return new BytesRefEnumerator(this,  size, indices);
        }

        /// <summary>
        /// Class ByteRefArraySorter.
        /// </summary>
        private class ByteRefArraySorter : IntroSorter, IDisposable
        {
            private BytesRefArray bytesRefArray;
            private IComparer<BytesRef> comparer;
            private int[] orderedEntries;
            private BytesRefBuilder pivot;
            private BytesRefBuilder scratch1;
            private BytesRefBuilder scratch2;

            /// <summary>
            /// Initializes a new instance of the <see cref="ByteRefArraySorter"/> class.
            /// </summary>
            /// <param name="outerInstance">The outer instance.</param>
            /// <param name="comp">The comp.</param>
            /// <param name="orderedEntries">The ordered entries.</param>
            public ByteRefArraySorter(BytesRefArray outerInstance, IComparer<BytesRef> comp, int[] orderedEntries)
            {
                this.bytesRefArray = outerInstance;
                this.comparer = comp;
                this.orderedEntries = orderedEntries;
                pivot = new BytesRefBuilder();
                scratch1 = new BytesRefBuilder();
                scratch2 = new BytesRefBuilder();
            }

            /// <summary>
            /// Save the value at slot <code>i</code> so that it can later be used as a
            /// pivot, see <seealso cref="ComparePivot(int)" />.
            /// </summary>
            /// <value>The pivot.</value>
            protected internal override int Pivot
            {
                set
                {
                    var index = orderedEntries[value];
                    bytesRefArray.Retrieve(pivot, index);
                }
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                GC.SuppressFinalize(this);
                this.Dispose(true);
            }

            /// <summary>
            /// Swaps the specified i.
            /// </summary>
            /// <param name="i">The i.</param>
            /// <param name="j">The j.</param>
            protected override void Swap(int i, int j)
            {
                var o = orderedEntries[i];
                orderedEntries[i] = orderedEntries[j];
                orderedEntries[j] = o;
            }

            /// <summary>
            /// Compares the specified i.
            /// </summary>
            /// <param name="i">The i.</param>
            /// <param name="j">The j.</param>
            /// <returns>System.Int32.</returns>
            protected override int Compare(int i, int j)
            {
                int idx1 = orderedEntries[i], 
                    idx2 = orderedEntries[j];
                
                return this.comparer.Compare(bytesRefArray.Retrieve(scratch1, idx1),
                    bytesRefArray.Retrieve(scratch2, idx2));
            }

            /// <summary>
            /// Compare the pivot with the slot at <code>j</code>, similarly to
            /// <seealso cref="Sorter.Compare(int, int)" />.
            /// </summary>
            /// <param name="j">The j.</param>
            /// <returns>System.Int32.</returns>
            protected internal override int ComparePivot(int j)
            {
                var index = orderedEntries[j];
                return this.comparer.Compare(this.pivot.ToBytesRef(), bytesRefArray.Retrieve(this.scratch2, index));
            }


            private void Dispose(bool disposing)
            {
                if (!disposing)
                    return;

                this.pivot = null;
                this.scratch1 = null;
                this.scratch2 = null;
                this.bytesRefArray = null;
                this.comparer = null;
                this.orderedEntries = null;
            }

            /// <summary>
            /// Finalizes an instance of the <see cref="ByteRefArraySorter"/> class.
            /// </summary>
            ~ByteRefArraySorter()
            {
                this.Dispose(false);
            }
        }


        /// <summary>
        ///     Class BytesRefEnumerator.
        /// </summary>
        // ReSharper disable once ClassWithVirtualMembersNeverInherited.Local
        private class BytesRefEnumerator : IEnumerator<BytesRef>
        {
            private readonly int[] indices;
            private readonly int size;
            private BytesRefArray bytesRefArray;
            private int position;
            private BytesRefBuilder builder;

            /// <summary>
            ///     Initializes a new instance of the <see cref="BytesRefEnumerator" /> class.
            /// </summary>
            /// <param name="bytesRefArray">The bytes reference array.</param>
            /// <param name="size">The size.</param>
            /// <param name="indices">The indices.</param>
            public BytesRefEnumerator(BytesRefArray bytesRefArray,  int size, int[] indices)
            {
                this.builder = new BytesRefBuilder();
                this.bytesRefArray = bytesRefArray;
                this.size = size;
                this.indices = indices;
                this.position = -1;
            }

            /// <summary>
            ///     Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <value>The current.</value>
            public BytesRef Current { get; private set; }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            ///     Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            ///     true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the
            ///     end of the collection.
            /// </returns>
            public bool MoveNext()
            {
                                this.position++;

                var next = position < size;

                if (!next)
                {
                    this.Current = null;
                    return false;
                }

                // return a new instance for each loop. 
                var bytesRef = new BytesRef();
                this.Current = bytesRefArray.Retrieve(this.builder, indices == null ? position : indices[position]);
                return true;
            }

            /// <summary>
            ///     Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                this.position = -1;
            }

            /// <summary>
            ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                GC.SuppressFinalize(this);
                this.Dispose(true);
            }


            /// <summary>
            ///     Releases unmanaged and - optionally - managed resources.
            /// </summary>
            /// <param name="disposing">
            ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
            ///     unmanaged resources.
            /// </param>
            protected virtual void Dispose(bool disposing)
            {
                if (!disposing)
                    return;

                this.Current = null;
                this.bytesRefArray = null;
            }

            /// <summary>
            ///     Finalizes an instance of the <see cref="BytesRefEnumerator" /> class.
            /// </summary>
            ~BytesRefEnumerator()
            {
                this.Dispose(false);
            }
        }
    }
}