// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Support
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
    /// A <see cref="PriorityQueue{T}"/> holds elements on a priority heap, which orders the elements
    /// according to their natural order or according to the comparator specified at
    /// construction time. If the queue uses natural ordering, only elements that are
    /// comparable are permitted to be inserted into the queue.
    /// <para/>
    /// The least element of the specified ordering is stored at the head of the
    /// queue and the greatest element is stored at the tail of the queue.
    /// <para/>
    /// A <see cref="PriorityQueue{T}"/> is not synchronized.
    /// </summary>
    /// <typeparam name="T">Type of elements</typeparam>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class PriorityQueue<T> : ICollection<T> where T : class // LUCENENET: added constraint so we can return null like the original code
    {
        private static readonly int DEFAULT_CAPACITY = 11;
        private static readonly double DEFAULT_INIT_CAPACITY_RATIO = 1.1;
        private static readonly int DEFAULT_CAPACITY_RATIO = 2;
        private int count;
        private IComparer<T> comparer;
        private T[] elements;

        #region Constructors

        /// <summary>
        /// Constructs a priority queue with an initial capacity of 11 and natural
        /// ordering.
        /// </summary>
        public PriorityQueue()
            : this(DEFAULT_CAPACITY)
        {
        }

        /// <summary>
        /// Constructs a priority queue with the specified capacity and natural
        /// ordering.
        /// </summary>
        /// <param name="initialCapacity">initial capacity</param>
        /// <exception cref="ArgumentOutOfRangeException">if the <paramref name="initialCapacity"/> is less than 1.</exception>
        public PriorityQueue(int initialCapacity)
            : this(initialCapacity, null)
        {
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> with the specified initial capacity
        /// that orders its elements according to the specified comparer.
        /// </summary>
        /// <param name="initialCapacity">the initial capacity for this priority queue</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> that will be used to order this
        /// priority queue.  If <c>null</c>, the <see cref="IComparable{T}"/>
        /// natural ordering of the elements will be used.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">if the <paramref name="initialCapacity"/> is less than 1</exception>
        public PriorityQueue(int initialCapacity, IComparer<T> comparer)
        {
            if (initialCapacity < 1)
            {
                throw new ArgumentOutOfRangeException("initialCapacity must be at least 1");
            }

            elements = new T[initialCapacity];
            this.comparer = comparer;
        }

        /// <summary>
        /// Constructs a priority queue with the specified capacity and comparer.
        /// </summary>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> that will be used to order this
        /// priority queue.  If <c>null</c>, the <see cref="IComparable{T}"/>
        /// natural ordering of the elements will be used.
        /// </param>
        public PriorityQueue(IComparer<T> comparer)
            : this(DEFAULT_CAPACITY, comparer)
        {
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> containing the elements in the
        /// specified collection.  If the specified collection is an instance of
        /// a <see cref="SortedSet{T}"/>, <see cref="TreeSet{T}"/>,
        /// or is another <see cref="PriorityQueue{T}"/>, this
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
        /// <see cref="ArgumentNullException">if the specified collection or any
        /// of its elements are null</see>
        public PriorityQueue(ICollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            if (collection is PriorityQueue<T>)
            {
                InitFrom((PriorityQueue<T>)collection);
            }
            else if (collection is SortedSet<T>)
            {
                InitFrom((SortedSet<T>)collection);
            }
            else if (collection is TreeSet<T>)
            {
                InitFrom((TreeSet<T>)collection);
            }
            else
            {
                InitFrom(collection);
            }
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> containing the elements in the
        /// specified priority queue.  This priority queue will be
        /// ordered according to the same ordering as the given priority
        /// queue.
        /// </summary>
        /// <param name="collection">the priority queue whose elements are to be placed
        /// into this priority queue</param>
        /// <exception cref="InvalidCastException">
        /// if elements of <paramref name="collection"/>
        /// cannot be compared to one another according to <paramref name="collection"/>'s
        /// ordering
        /// </exception>
        /// <see cref="NullReferenceException">if the specified collection or any
        /// of its elements are null</see>
        public PriorityQueue(PriorityQueue<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            InitFrom(collection);
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> containing the elements in the
        /// specified <see cref="SortedSet{T}"/>.  This priority queue will be
        /// ordered according to the same ordering as the given <see cref="SortedSet{T}"/>.
        /// <para/>
        /// The constructed priority queue has the initial capacity of 110% of the
        /// size of the sorted set. The priority queue will have the same comparator
        /// as the sorted set.
        /// </summary>
        /// <param name="collection">the sorted set whose elements are to be placed
        /// into this priority queue</param>
        /// <exception cref="InvalidCastException">
        /// if elements of <paramref name="collection"/>
        /// cannot be compared to one another according to <paramref name="collection"/>'s
        /// ordering
        /// </exception>
        /// <exception cref="ArgumentNullException">if the specified collection or any
        /// of its elements are null</exception>
        public PriorityQueue(SortedSet<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            InitFrom(collection);
        }

        /// <summary>
        /// Creates a <see cref="PriorityQueue{T}"/> containing the elements in the
        /// specified <see cref="TreeSet{T}"/>.  This priority queue will be
        /// ordered according to the same ordering as the given <see cref="TreeSet{T}"/>.
        /// <para/>
        /// The constructed priority queue has the initial capacity of 110% of the
        /// size of the sorted set. The priority queue will have the same comparator
        /// as the sorted set.
        /// </summary>
        /// <param name="collection">the sorted set whose elements are to be placed
        /// into this priority queue</param>
        /// <exception cref="InvalidCastException">
        /// if elements of <paramref name="collection"/>
        /// cannot be compared to one another according to <paramref name="collection"/>'s
        /// ordering
        /// </exception>
        /// <exception cref="ArgumentNullException">if the specified collection or any
        /// of its elements are null</exception>
        public PriorityQueue(TreeSet<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            InitFrom(collection);
        }

        #endregion Constructors

        #region Initialization

        private void InitFrom(PriorityQueue<T> collection)
        {
            InitSize(collection);
            comparer = collection.Comparer;
            System.Array.Copy(collection.elements, 0, elements, 0, collection.Count);
            count = collection.Count;
        }

        private void InitFrom(SortedSet<T> collection)
        {
            InitSize(collection);
            comparer = collection.Comparer;
            foreach (var value in collection)
            {
                elements[count++] = value;
            }
        }

        private void InitFrom(TreeSet<T> collection)
        {
            InitSize(collection);
            comparer = collection.Comparer;
            foreach (var value in collection)
            {
                elements[count++] = value;
            }
        }

        private void InitFrom(ICollection<T> collection)
        {
            InitSize(collection);
            AddAll(collection);
        }

        #endregion Initialization

        #region Priority queue operations

        /// <summary>
        /// Gets the enumerator of the priority queue, which will not return elements
        /// in any specified ordering.
        /// </summary>
        /// <returns>The enumerator of the priority queue.</returns>
        /// <remarks>
        /// Returned enumerator does not iterate elements in sorted order.</remarks>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return new PriorityEnumerator(this);
        }

        /// <summary>
        /// Gets the size of the priority queue. If the size of the queue is greater
        /// than the <see cref="int.MaxValue"/>, then it returns <see cref="int.MaxValue"/>.
        /// </summary>
        public virtual int Count
        {
            get { return count; }
        }

        /// <summary>
        /// Removes all of the elements from this priority queue.
        /// </summary>
        public virtual void Clear()
        {
            Arrays.Fill(elements, null);
            count = 0;
        }

        /// <summary>
        /// Inserts the specified element into this priority queue.
        /// </summary>
        /// <param name="item">the element to add to the priority queue.</param>
        /// <returns>always <c>true</c></returns>
        /// <exception cref="InvalidCastException">if the specified element cannot be
        /// compared with elements currently in this priority queue
        /// according to the priority queue's ordering</exception>
        /// <exception cref="ArgumentNullException">if the specified element is null</exception>
        public virtual bool Offer(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            GrowToSize(count + 1);
            elements[count] = item;
            SiftUp(count++);
            return true;
        }

        /// <summary>
        /// Gets and removes the head of the queue.
        /// </summary>
        /// <returns>the head of the queue or null if the queue is empty.</returns>
        public virtual T Poll()
        {
            if (count == 0)
            {
                return null;
            }
            T result = elements[0];
            RemoveAt(0);
            return result;
        }

        /// <summary>
        /// Gets but does not remove the head of the queue.
        /// </summary>
        /// <returns>the head of the queue or null if the queue is empty.</returns>
        public virtual T Peek()
        {
            return (count == 0) ? null : elements[0];
        }

        /// <summary>
        /// Gets the <see cref="IComparer{T}"/> used to order the elements in this
        /// queue, or <see cref="Util.ArrayUtil.NaturalComparer{T}"/> if this queue is sorted according to
        /// the <see cref="IComparable{T}"/> natural ordering of its elements.
        /// </summary>
        public virtual IComparer<T> Comparer { get { return this.comparer; } }

        /// <summary>
        /// Removes the specified object from the priority queue.
        /// </summary>
        /// <param name="item">the object to be removed.</param>
        /// <returns><c>true</c> if the object was in the priority queue, <c>false</c> if the object</returns>
        public virtual bool Remove(T item)
        {
            if (item == null || count == 0)
            {
                return false;
            }
            for (int i = 0; i < count; i++)
            {
                if (item.Equals(elements[i]))
                {
                    RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds the specified object to the priority queue.
        /// </summary>
        /// <param name="item">the object to be added.</param>
        /// <exception cref="InvalidCastException">if the specified element cannot be
        /// compared with elements currently in this priority queue
        /// according to the priority queue's ordering</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="item"/> is <c>null</c>.</exception>
        public virtual void Add(T item) // LUCENENET NOTE: No return value because we need to implement ICollection<T>
        {
            Offer(item);
        }

        private sealed class PriorityEnumerator : IEnumerator<T>
        {
            private int currentIndex = -1;

            private T current;


            private readonly PriorityQueue<T> outerInstance;

            public PriorityEnumerator(PriorityQueue<T> outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public T Current
            {
                get { return current; }
            }

            object IEnumerator.Current
            {
                get { return current; }
            }

            public void Dispose()
            {
                current = null;
            }

            public bool MoveNext()
            {
                if (currentIndex < outerInstance.count - 1)
                {
                    current = outerInstance.elements[++currentIndex];
                    return true;
                }
                return false;
            }

            // LUCENENET NOTE: Remove() not implemented

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        // LUCENENET NOTE: readObject omitted

        private T[] NewElementArray(int capacity)
        {
            return new T[capacity];
        }

        // LUCENENET NOTE: writeObject omitted

        /// <summary>
        /// Removes the element at the specified <paramref name="index"/> from queue.
        /// <para/>
        /// Normally this method leaves the elements at up to <paramref name="index"/>-1,
        /// inclusive, untouched.  Under these circumstances, it returns
        /// <c>null</c>.  Occasionally, in order to maintain the heap invariant,
        /// it must swap a later element of the list with one earlier than
        /// <paramref name="index"/>.  Under these circumstances, this method returns the element
        /// that was previously at the end of the list and is now at some
        /// position before <paramref name="index"/>.
        /// </summary>
        private void RemoveAt(int index)
        {
            count--;
            elements[index] = elements[count];
            SiftDown(index);
            elements[count] = null;
        }

        private int Compare(T o1, T o2)
        {
            if (null != comparer)
            {
                return comparer.Compare(o1, o2);
            }
            return ((IComparable<T>)o1).CompareTo(o2);
        }

        private void SiftUp(int childIndex)
        {
            T target = elements[childIndex];
            int parentIndex;
            while (childIndex > 0)
            {
                parentIndex = (childIndex - 1) / 2;
                T parent = elements[parentIndex];
                if (Compare(parent, target) <= 0)
                {
                    break;
                }
                elements[childIndex] = parent;
                childIndex = parentIndex;
            }
            elements[childIndex] = target;
        }

        private void SiftDown(int rootIndex)
        {
            T target = elements[rootIndex];
            int childIndex;
            while ((childIndex = rootIndex * 2 + 1) < count)
            {
                if (childIndex + 1 < count
                        && Compare(elements[childIndex + 1], elements[childIndex]) < 0)
                {
                    childIndex++;
                }
                if (Compare(target, elements[childIndex]) <= 0)
                {
                    break;
                }
                elements[rootIndex] = elements[childIndex];
                rootIndex = childIndex;
            }
            elements[rootIndex] = target;
        }

        private void InitSize(ICollection<T> c)
        {
            if (null == c)
            {
                throw new ArgumentNullException("c");
            }
            if (c.Count == 0)
            {
                elements = NewElementArray(1);
            }
            else
            {
                int capacity = (int)Math.Ceiling(c.Count
                        * DEFAULT_INIT_CAPACITY_RATIO);
                elements = NewElementArray(capacity);
            }
        }

        private void GrowToSize(int size)
        {
            if (size > elements.Length)
            {
                T[] newElements = NewElementArray(size * DEFAULT_CAPACITY_RATIO);
                System.Array.Copy(elements, 0, newElements, 0, elements.Length);
                elements = newElements;
            }
        }

        #endregion Priority queue operations

        #region AbstractQueue operations

        public virtual bool AddAll(ICollection<T> collection)
        {
            if (null == collection)
            {
                throw new ArgumentNullException("collection");
            }
            if (this == collection)
            {
                throw new ArgumentException("collection must not be the same instance as this.", "collection");
            }
            bool result = false;
            foreach (var value in collection)
            {
                if (value == null)
                {
                    throw new ArgumentNullException("PriorityQueue values must not be null.");
                }
                if (Offer(value))
                {
                    result = true;
                }
            }
            return result;
        }

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
        public virtual T Remove()
        {
            T x = Poll();
            if (x == null)
            {
                throw new KeyNotFoundException();
            }
            return x;
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
        public virtual T Element()
        {
            T x = Peek();
            if (x == null)
            {
                throw new KeyNotFoundException();
            }
            return x;
        }

        #endregion

        #region ICollection<T> implementation (LUCENENET specific)

        private int IndexOf(T o)
        {
            if (o != null)
            {
                for (int i = 0; i < count; i++)
                    if (o.Equals(elements[i]))
                        return i;
            }
            return -1;
        }

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
                elements.CopyTo(array, arrayIndex);
            }
            else
            {
                System.Array.Copy(elements, 0, array, arrayIndex, size);
                if (array.Length > size)
                {
                    array[size] = null;
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
