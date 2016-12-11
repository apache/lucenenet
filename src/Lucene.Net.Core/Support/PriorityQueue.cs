/*
 * Copyright (c) 2003, 2013, Oracle and/or its affiliates. All rights reserved.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * This code is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 2 only, as
 * published by the Free Software Foundation.  Oracle designates this
 * particular file as subject to the "Classpath" exception as provided
 * by Oracle in the LICENSE file that accompanied this code.
 *
 * This code is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * version 2 for more details (a copy is included in the LICENSE file that
 * accompanied this code).
 *
 * You should have received a copy of the GNU General Public License version
 * 2 along with this work; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 * Please contact Oracle, 500 Oracle Parkway, Redwood Shores, CA 94065 USA
 * or visit www.oracle.com if you need additional information or have any
 * questions.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Support
{
    /// <summary>
    /// An unbounded priority queue based on a priority heap.
    /// The elements of the priority queue are ordered according to their
    /// <see cref="IComparable{T}"/> natural ordering, or by a <see cref="IComparer{T}"/>
    /// provided at queue construction time, depending on which constructor is
    /// used.  A priority queue does not permit <c>null</c> elements.
    /// A priority queue relying on natural ordering also does not permit
    /// insertion of non-comparable objects (doing so may result in
    /// <see cref="InvalidCastException"/>).
    /// <para>
    /// The <em>head</em> of this queue is the <c>least</c> element
    /// with respect to the specified ordering.  If multiple elements are
    /// tied for least value, the head is one of those elements -- ties are
    /// broken arbitrarily.  The queue retrieval operations <see cref="Poll()"/>,
    /// <see cref="Remove(T)"/>, <see cref="Peek()"/>. and <see cref="Element()"/> access the
    /// element at the head of the queue.
    /// </para>
    /// <para>
    /// A priority queue is unbounded, but has an internal
    /// <c>capacity</c> governing the size of an array used to store the
    /// elements on the queue.  It is always at least as large as the queue
    /// size.  As elements are added to a priority queue, its capacity
    /// grows automatically.  The details of the growth policy are not
    /// specified.
    /// </para>
    /// <para>
    /// Note that this implementation is not synchronized.
    /// Multiple threads should not access a <see cref="PriorityQueue{T}"/>
    /// instance concurrently if any of the threads modifies the queue.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Implementation note: this implementation provides
    /// O(log(n)) time for the enqueuing and dequeuing methods
    /// (<see cref="Offer(T)"/>, <see cref="Poll()"/>, <see cref="Remove(T)"/> and <see cref="Add(T)"/>);
    /// linear time for the <see cref="Remove(T)"/> and <see cref="Contains(T)"/>
    /// methods; and constant time for the retrieval members
    /// (<see cref="Peek()"/>, <see cref="Count"/>, and <see cref="Element()"/>).
    /// </remarks>
    /// <typeparam name="T">Type of elements</typeparam>
    public class PriorityQueue<T> : ICollection<T>
    {
        private static readonly int DEFAULT_INITIAL_CAPACITY = 11;

        /// <summary>
        /// Priority queue represented as a balanced binary heap: the two
        /// children of queue[n] are queue[2*n+1] and queue[2*(n+1)].  The
        /// priority queue is ordered by comparator, or by the elements'
        /// natural ordering, if comparator is null: For each node n in the
        /// heap and each descendant d of n, n &lt;= d.  The element with the
        /// lowest value is in queue[0], assuming the queue is nonempty.
        /// </summary>
        internal T[] queue;

        /// <summary>
        /// The number of elements in the priority queue.
        /// </summary>
        private int count = 0;

        /// <summary>
        /// The <see cref="IComparer{T}"/>, or null if priority queue uses elements'
        /// natural ordering.
        /// </summary>
        private readonly IComparer<T> comparer;

        /// <summary>
        /// The number of times this priority queue has been
        /// structurally modified.
        /// </summary>
        internal int modCount = 0;

        #region Constructors

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> with the default initial
        /// capacity (11) that orders its elements according to their
        /// <see cref="IComparable{T}"/> natural ordering.
        /// </summary>
        public PriorityQueue()
            : this(DEFAULT_INITIAL_CAPACITY, null)
        {
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> with the specified initial
        /// capacity that orders its elements according to their
        /// <see cref="IComparable{T}"/> natural ordering.
        /// </summary>
        /// <param name="initialCapacity">initial capacity</param>
        public PriorityQueue(int initialCapacity)
            : this(initialCapacity, null)
        {
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> with the default initial capacity and
        /// whose elements are ordered according to the specified <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> that will be used to order this
        /// priority queue.  If <c>null</c>, the <see cref="IComparable{T}"/>
        /// natural ordering of the elements will be used.
        /// </param>
        public PriorityQueue(IComparer<T> comparer)
            : this(DEFAULT_INITIAL_CAPACITY, comparer)
        {
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> with the specified initial capacity
        /// that orders its elements according to the specified comparator.
        /// </summary>
        /// <param name="initialCapacity">the initial capacity for this priority queue</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> that will be used to order this
        /// priority queue.  If <c>null</c>, the <see cref="IComparable{T}"/>
        /// natural ordering of the elements will be used.
        /// </param>
        /// <exception cref="ArgumentException">if the <paramref name="initialCapacity"/> is less than 1</exception>
        public PriorityQueue(int initialCapacity, IComparer<T> comparer)
        {
            if (initialCapacity < 1)
                throw new ArgumentException("initialCapacity must be at least 1");

            this.queue = new T[initialCapacity];
            this.comparer = comparer;
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> containing the elements in the
        /// specified collection.  If the specified collection is an instance of
        /// a <see cref="SortedSet{T}"/> or is another <see cref="PriorityQueue{T}"/>, this
        /// priority queue will be ordered according to the same ordering.
        /// Otherwise, this priority queue will be ordered according to the
        /// <see cref="IComparable{T}"/> natural ordering of its elements.
        /// </summary>
        /// <param name="collection">collection to be inserted into priority queue</param>
        /// <exception cref="InvalidCastException">
        /// if elements of the specified collection
        /// cannot be compared to one another according to the priority
        /// queue's ordering
        /// </exception>
        /// <see cref="NullReferenceException">if the specified collection or any
        /// of its elements are null</see>
        public PriorityQueue(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (collection is SortedSet<T>)
            {
                var ss = (SortedSet<T>)collection;
                this.comparer = ss.Comparer;
                InitElementsFromEnumerable(collection);
            }
            if (collection is PriorityQueue<T>)
            {
                var pq = (PriorityQueue<T>)collection;
                this.comparer = pq.Comparer;
            }
            else
            {
                this.comparer = null;
                InitFromEnumerable(collection);
            }
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> containing the elements in the
        /// specified priority queue.  This priority queue will be
        /// ordered according to the same ordering as the given priority
        /// queue.
        /// </summary>
        /// <param name="c">the priority queue whose elements are to be placed
        /// into this priority queue</param>
        /// <exception cref="InvalidCastException">
        /// if elements of <paramref name="c"/>
        /// cannot be compared to one another according to <paramref name="c"/>'s
        /// ordering
        /// </exception>
        /// <see cref="NullReferenceException">if the specified collection or any
        /// of its elements are null</see>
        public PriorityQueue(PriorityQueue<T> c)
        {
            this.comparer = c.Comparer;
            InitFromPriorityQueue(c);
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> containing the elements in the
        /// specified sorted set.  This priority queue will be
        /// ordered according to the same ordering as the given sorted set.
        /// </summary>
        /// <param name="c">the sorted set whose elements are to be placed
        /// into this priority queue</param>
        /// <exception cref="InvalidCastException">
        /// if elements of <paramref name="c"/>
        /// cannot be compared to one another according to <paramref name="c"/>'s
        /// ordering
        /// </exception>
        /// <see cref="NullReferenceException">if the specified collection or any
        /// of its elements are null</see>
        /// </summary>
        public PriorityQueue(SortedSet<T> c)
        {
            this.comparer = c.Comparer;
            InitElementsFromEnumerable(c);
        }

        #endregion Constructors

        #region Initialization

        private void InitFromPriorityQueue(PriorityQueue<T> c)
        {
            if (c.GetType() == typeof(PriorityQueue<T>))
            {
                this.queue = c.ToArray();
                this.count = c.count;
            }
            else
            {
                InitFromEnumerable(c);
            }
        }

        private void InitElementsFromEnumerable(IEnumerable<T> c)
        {
            T[] a = c.ToArray();
            // If c.toArray incorrectly doesn't return Object[], copy it.
            if (a.GetType() != typeof(T[]))
                a = Arrays.CopyOf(a, a.Length);
            int len = a.Length;
            if (len == 1 || this.comparer != null)
                for (int i = 0; i < len; i++)
                    if (a[i] == null)
                        throw new NullReferenceException();
            this.queue = a;
            this.count = a.Length;
        }

        /// <summary>
        /// Initializes queue array with elements from the given <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="c">the collection</param>
        private void InitFromEnumerable(IEnumerable<T> c)
        {
            InitElementsFromEnumerable(c);
            Heapify();
        }

        #endregion Initialization

        /// <summary>
        /// The maximum size of array to allocate.
        /// Some VMs reserve some header words in an array.
        /// Attempts to allocate larger arrays may result in
        /// <see cref="OutOfMemoryException"/>: Requested array size exceeds VM limit
        /// </summary>
        private static readonly int MAX_ARRAY_SIZE = int.MaxValue - 8;

        #region Priority queue operations

        /// <summary>
        /// Increases the capacity of the array.
        /// </summary>
        /// <param name="minCapacity">the desired minimum capacity</param>
        private void Grow(int minCapacity)
        {
            int oldCapacity = queue.Length;
            // Double size if small; else grow by 50%
            int newCapacity = oldCapacity + ((oldCapacity < 64) ?
                                             (oldCapacity + 2) :
                                             (oldCapacity >> 1));
            // overflow-conscious code
            if (newCapacity - MAX_ARRAY_SIZE > 0)
                newCapacity = HugeCapacity(minCapacity);
            queue = Arrays.CopyOf(queue, newCapacity);
        }

        private static int HugeCapacity(int minCapacity)
        {
            if (minCapacity < 0) // overflow
                throw new OutOfMemoryException();
            return (minCapacity > MAX_ARRAY_SIZE) ?
                int.MaxValue :
                MAX_ARRAY_SIZE;
        }

        /// <summary>
        /// Inserts the specified element into this priority queue.
        /// </summary>
        /// <param name="item">element to add</param>
        /// <exception cref="InvalidCastException">if the specified element cannot be
        /// compared with elements currently in this priority queue
        /// according to the priority queue's ordering</exception>
        /// <exception cref="ArgumentNullException">if the specified element is null</exception>
        public virtual void Add(T item) // LUCENENET NOTE: No return value because we need to implement ICollection<T>
        {
            Offer(item);
        }

        /// <summary>
        /// Inserts the specified element into this priority queue.
        /// </summary>
        /// <param name="item"></param>
        /// <returns><c>true</c>if the element was added to this queue, else <c>false</c></returns>
        /// <exception cref="InvalidCastException">if the specified element cannot be
        /// compared with elements currently in this priority queue
        /// according to the priority queue's ordering</exception>
        /// <exception cref="ArgumentNullException">if the specified element is null</exception>
        public virtual bool Offer(T item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            modCount++;
            int i = count;
            if (i >= queue.Length)
                Grow(i + 1);
            count = i + 1;
            if (i == 0)
                queue[0] = item;
            else
                SiftUp(i, item);
            return true;
        }

        /// <summary>
        /// Returns priority and value of the element with minimun priority, without removing it from the queue
        /// </summary>
        /// <returns>priority and value of the element with minimum priority</returns>
        /// <remarks>
        /// Method returns default for type <see cref="T"/> if priority queue is empty
        /// </remarks>
        public virtual T Peek()
        {
            return (count == 0) ? default(T) : queue[0];
        }

        private int IndexOf(T o)
        {
            if (o != null)
            {
                for (int i = 0; i < count; i++)
                    if (o.Equals(queue[i]))
                        return i;
            }
            return -1;
        }

        /// <summary>
        /// Removes a single instance of the specified element from this queue,
        /// if it is present.  More formally, removes an element <paramref name="item"/> such
        /// that <c>o.Equals(item)</c>, if this queue contains one or more such
        /// elements.  Returns <c>true</c> if and only if this queue contained
        /// the specified element (or equivalently, if this queue changed as a
        /// result of the call).
        /// </summary>
        /// <param name="item">element to be removed from this queue, if present</param>
        /// <returns><c>true</c> if this queue changed as a result of the call.
        /// This method returns false if item is not found in the collection. </returns>
        public virtual bool Remove(T item)
        {
            int i = IndexOf(item);
            if (i == -1)
                return false;
            else
            {
                RemoveAt(i);
                return true;
            }
        }

        // LUCENENET NOTE: RemoveEq not implemented.

        /// <summary>
        /// Returns <c>true</c> if this queue contains the specified element.
        /// More formally, returns <c>true</c> if and only if this queue contains
        /// at least one element <paramref name="item"/> such that <c>o.Equals(item)</c>.
        /// </summary>
        /// <param name="item">The object to locate in the priority queue</param>
        /// <returns><c>true</c> if item is found in the priority queue; otherwise, <c>false.</c> </returns>
        public virtual bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        // LUCENENET NOTE: ToArray() methods excluded because LINQ already includes this functionality as an extension method
        // by calling into CopyTo().

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>Enumerator</returns>
        /// <remarks>
        /// Returned enumerator does not iterate elements in sorted order.</remarks>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        public sealed class Enumerator : IEnumerator<T>
        {
            /// <summary>
            /// Index (into queue array) of element to be returned by
            /// subsequent call to next.
            /// </summary>
            private int cursor = 0;

            /// <summary>
            /// Index of element returned by most recent call to next,
            /// unless that element came from the forgetMeNot list.
            /// Set to -1 if element is deleted by a call to remove.
            /// </summary>
            private int lastRet = -1;


            /// <summary>
            /// A queue of elements that were moved from the unvisited portion of
            /// the heap into the visited portion as a result of "unlucky" element
            /// removals during the iteration.  (Unlucky element removals are those
            /// that require a siftup instead of a siftdown.)  We must visit all of
            /// the elements in this list to complete the iteration.  We do this
            /// after we've completed the "normal" iteration.
            /// <para/>
            /// We expect that most iterations, even those involving removals,
            /// will not need to store elements in this field.
            /// </summary>
            private Queue<T> forgetMeNot = null;

            /// <summary>
            /// Element returned by the most recent call to next iff that
            /// element was drawn from the forgetMeNot list.
            /// </summary>
            private T lastRetElt = default(T);

            /// <summary>
            /// The modCount value that the iterator believes that the backing
            /// Queue should have.  If this expectation is violated, the iterator
            /// has detected concurrent modification.
            /// </summary>
            private int expectedModCount;

            private T current;


            private readonly PriorityQueue<T> outerInstance;

            public Enumerator(PriorityQueue<T> outerInstance)
            {
                this.outerInstance = outerInstance;
                this.expectedModCount = outerInstance.modCount;
            }

            public T Current
            {
                get
                {
                    return current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (expectedModCount != outerInstance.modCount)
                    throw new InvalidOperationException("collection was modified");
                if (cursor < outerInstance.count)
                {
                    current = outerInstance.queue[lastRet = cursor++];
                    return true;
                }
                if (forgetMeNot != null)
                {
                    lastRet = -1;
                    lastRetElt = forgetMeNot.Dequeue();
                    if (lastRetElt != null)
                    {
                        current = lastRetElt;
                        return true;
                    }
                }
                return false;
            }

            // LUCENENET NOTE: Remove() not implemented

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets number of elements in the priority queue
        /// </summary>
        public virtual int Count
        {
            get { return count; }
        }


        /// <summary>
        /// Removes all of the elements from this priority queue.
        /// The queue will be empty after this call returns.
        /// </summary>
        public virtual void Clear()
        {
            modCount++;
            for (int i = 0; i < count; i++)
                queue[i] = default(T);
            count = 0;
        }

        public virtual T Poll()
        {
            if (count == 0)
                return default(T);
            int s = --count;
            modCount++;
            T result = queue[0];
            T x = queue[s];
            queue[s] = default(T);
            if (s != 0)
                SiftDown(0, x);
            return result;
        }

        /// <summary>
        /// Removes the ith element from queue.
        /// <para/>
        /// Normally this method leaves the elements at up to i-1,
        /// inclusive, untouched.  Under these circumstances, it returns
        /// null.  Occasionally, in order to maintain the heap invariant,
        /// it must swap a later element of the list with one earlier than
        /// i.  Under these circumstances, this method returns the element
        /// that was previously at the end of the list and is now at some
        /// position before i.
        /// </summary>
        private T RemoveAt(int i)
        {
            // assert i >= 0 && i < size;
            modCount++;
            int s = --count;
            if (s == i) // removed last element
                queue[i] = default(T);
            else
            {
                T moved = queue[s];
                queue[s] = default(T);
                SiftDown(i, moved);
                if (EqualityComparer<T>.Default.Equals(queue[i], moved))
                {
                    SiftUp(i, moved);
                    if (!EqualityComparer<T>.Default.Equals(queue[i], moved))
                        return moved;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Inserts item x at position k, maintaining heap invariant by
        /// promoting x up the tree until it is greater than or equal to
        /// its parent, or is the root.
        /// <para/>
        /// To simplify and speed up coercions and comparisons. the
        /// <see cref="IComparable{T}"/> and <see cref="IComparer{T}"/>
        /// versions are separated into different
        /// methods that are otherwise identical. (Similarly for <see cref="SiftDown(int, T)"/>.)
        /// </summary>
        /// <param name="k"></param>
        /// <param name="key"></param>
        private void SiftUp(int k, T key)
        {
            if (comparer != null)
                SiftUpUsingComparator(k, key);
            else
                SiftUpComparable(k, key);
        }

        private void SiftUpComparable(int k, T key)
        {
            while (k > 0)
            {
                int parent = (int)((uint)(k - 1) >> 1);
                T e = queue[parent];
                if (((IComparable<T>)key).CompareTo(e) >= 0)
                    break;
                queue[k] = e;
                k = parent;
            }
            queue[k] = key;
        }

        private void SiftUpUsingComparator(int k, T x)
        {
            while (k > 0)
            {
                int parent = (int)((uint)(k - 1) >> 1);
                T e = queue[parent];
                if (comparer.Compare(x, e) >= 0)
                    break;
                queue[k] = e;
                k = parent;
            }
            queue[k] = x;
        }

        /// <summary>
        /// Inserts item x at position k, maintaining heap invariant by
        /// demoting x down the tree repeatedly until it is less than or
        /// equal to its children or is a leaf.
        /// </summary>
        /// <param name="k">the position to fill</param>
        /// <param name="x">the item to insert</param>
        private void SiftDown(int k, T x)
        {
            if (comparer != null)
                SiftDownUsingComparator(k, x);
            else
                SiftDownComparable(k, x);
        }

        private void SiftDownComparable(int k, T key)
        {
            int half = (int)((uint)Count >> 1);        // loop while a non-leaf
            while (k < half)
            {
                int child = (k << 1) + 1; // assume left child is least
                T c = queue[child];
                int right = child + 1;
                if (right < Count &&
                   ((IComparable<T>)c).CompareTo(queue[right]) > 0)
                    c = queue[child = right];
                if (((IComparable<T>)key).CompareTo(c) <= 0)
                    break;
                queue[k] = c;
                k = child;
            }
            queue[k] = key;
        }

        private void SiftDownUsingComparator(int k, T x)
        {
            int half = (int)((uint)Count >> 1);
            while (k < half)
            {
                int child = (k << 1) + 1;
                T c = queue[child];
                int right = child + 1;
                if (right < Count &&
                    comparer.Compare(c, queue[right]) > 0)
                    c = queue[child = right];
                if (comparer.Compare(x, c) <= 0)
                    break;
                queue[k] = c;
                k = child;
            }
            queue[k] = x;
        }

        /// <summary>
        /// Establishes the heap invariant (described above) in the entire tree,
        /// assuming nothing about the order of the elements prior to the call.
        /// </summary>
        private void Heapify()
        {
            for (int i = (int)(((uint)count) >> 1) - 1; i >= 0; i--)
            {
                SiftDown(i, queue[i]);
            }
        }

        /// <summary>
        /// Gets the <see cref="IComparer{T}"/> used to order the elements in this
        /// queue, or {@code null} if this queue is sorted according to
        /// the <see cref="IComparable{T}"/> natural ordering of its elements.
        /// </summary>
        public virtual IComparer<T> Comparer { get { return this.comparer; } }


        // LUCENENET NOTE: Serialization and Spliterator not implemented.

        #endregion Priority queue operations

        #region AbstractQueue operations

        /// <summary>
        /// Retrieves and removes the head of this queue.  This method differs
        /// from <see cref="Poll()"/> only in that it throws an exception if this
        /// queue is empty.
        /// <para/>
        /// This implementation returns the result of <see cref="Poll()"/>
        /// unless the queue is empty.
        /// </summary>
        /// <returns>the head of this queue</returns>
        /// <exception cref="KeyNotFoundException">if this queue is empty</exception>
        public T Remove()
        {
            T x = Poll();
            if (x != null)
                return x;
            else
                throw new KeyNotFoundException();
        }

        /// <summary>
        /// Retrieves, but does not remove, the head of this queue.  This method
        /// differs from <see cref="Peek()"/> only in that it throws an exception if
        /// this queue is empty.
        /// <para/>
        /// This implementation returns the result of <see cref="Peek()"/>
        /// unless the queue is empty.
        /// </summary>
        /// <returns>the head of this queue</returns>
        /// <exception cref="KeyNotFoundException">if this queue is empty</exception>
        public T Element()
        {
            T x = Peek();
            if (x != null)
                return x;
            else
                throw new KeyNotFoundException();
        }

        #endregion

        #region ICollection<T> implementation (LUCENENET specific)

        /// <summary>
        /// Copies the elements of the priority queue to an Array, starting at a particular Array index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from the priority queue. The Array must have zero-based indexing. </param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <remarks>
        /// It is not guaranteed that items will be copied in the sorted order.
        /// </remarks>
        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            int size = this.count;
            if (array.Length < size)
            {
                queue.CopyTo(array, arrayIndex);
            }
            else
            {
                System.Array.Copy(queue, 0, array, arrayIndex, size);
                if (array.Length > size)
                {
                    array[size] = default(T);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        /// <remarks>
        /// For priority queue this property returns <c>false</c>.
        /// </remarks>
        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>Enumerator</returns>
        /// <remarks>
        /// Returned enumerator does not iterate elements in sorted order.</remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion ICollection<T> implementation (LUCENENET specific)
    }
}
