/*
 * This code implements priority queue which uses min-heap as underlying storage
 *
 * Copyright (C) 2010 Alexey Kurakin
 * www.avk.name
 * alexey[ at ]kurakin.me
 */

/*
 * Modified by James Blair to use IComparable<T> and MAX-heap as underlying storage
 * 6/20/2013
 */

/*
* Modified by Shad Storhaug to use either IComparable<T> or IComparer<T> depending
* on which constructor is called, similar to the way the PQ in Java works.
* 9/14/2016
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Priority queue based on binary heap,
    /// Elements with minimum priority dequeued first
    /// </summary>
    /// <typeparam name="T">Type of elements</typeparam>
    public class PriorityQueue<T> : ICollection<T>
        where T : IComparable<T>
    {
        protected readonly List<T> baseHeap;
        protected readonly IComparer<T> comparer;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of priority queue with specified initial capacity
        /// </summary>
        /// <param name="capacity">initial capacity</param>
        public PriorityQueue(int capacity)
        {
            baseHeap = new List<T>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of priority queue with specified initial capacity and specified priority comparer
        /// </summary>
        /// <param name="capacity">initial capacity</param>
        /// <param name="comparer">comparer</param>
        public PriorityQueue(int capacity, IComparer<T> comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            baseHeap = new List<T>(capacity);
            this.comparer = comparer;
        }

        /// <summary>
        /// Initializes a new instance of priority queue with default initial capacity
        /// </summary>
        public PriorityQueue()
        {
            baseHeap = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of priority queue with specified data
        /// </summary>
        /// <param name="data">data to be inserted into priority queue</param>
        public PriorityQueue(IEnumerable<T> data)
        {
            if (data == null)
                throw new ArgumentNullException();

            baseHeap = new List<T>(data);
            // heapify data
            for (int pos = baseHeap.Count / 2 - 1; pos >= 0; pos--)
                HeapifyFromBeginningToEnd(pos);
        }

        #endregion Constructors

        #region Merging

        /// <summary>
        /// Merges two priority queues and sets comparer of th resultant priority queue
        /// to that of the first priority queue. If the first priority queue doesn't 
        /// have a comparer, the resultant priority queue will use <see cref="IComparable{T}"/>
        /// to sort its items.
        /// </summary>
        /// <param name="pq1">first priority queue</param>
        /// <param name="pq2">second priority queue</param>
        /// <returns>resultant priority queue</returns>
        public static PriorityQueue<T> MergeQueues(PriorityQueue<T> pq1, PriorityQueue<T> pq2)
        {
            if (pq1 == null || pq2 == null)
                throw new ArgumentNullException();
            // merge data
            var result = new PriorityQueue<T>(pq1.Count + pq2.Count, pq1.comparer);
            result.baseHeap.AddRange(pq1.baseHeap);
            result.baseHeap.AddRange(pq2.baseHeap);
            // heapify data
            for (var pos = result.baseHeap.Count / 2 - 1; pos >= 0; pos--)
                result.HeapifyFromBeginningToEnd(pos);

            return result;
        }

        #endregion Merging

        #region Priority queue operations

        /// <summary>
        /// Enqueues element into priority queue
        /// </summary>
        /// <param name="item">object to enqueue</param>
        public virtual void Enqueue(T item)
        {
            Insert(item);
        }

        /// <summary>
        /// Dequeues element with minimum priority and return its priority and value as <see cref="KeyValuePair{TPriority,TValue}"/>
        /// </summary>
        /// <returns>the dequeued element</returns>
        /// <remarks>
        /// Method throws <see cref="InvalidOperationException"/> if priority queue is empty
        /// </remarks>
        public virtual T Dequeue()
        {
            if (!IsEmpty)
            {
                var result = baseHeap[0];
                DeleteRoot();
                return result;
            }
            else
                throw new InvalidOperationException("Priority queue is empty");
        }

        /// <summary>
        /// Returns priority and value of the element with minimun priority, without removing it from the queue
        /// </summary>
        /// <returns>priority and value of the element with minimum priority</returns>
        /// <remarks>
        /// Method throws <see cref="InvalidOperationException"/> if priority queue is empty
        /// </remarks>
        public virtual T Peek()
        {
            if (!IsEmpty)
                return baseHeap[0];
            else
                throw new InvalidOperationException("Priority queue is empty");
        }

        public virtual T Poll()
        {
            try
            {
                return Dequeue();
            }
            catch
            {
                return default(T);
            }
        }

        public virtual bool Offer(T item)
        {
            Insert(item);
            return true;
        }

        /// <summary>
        /// Gets whether priority queue is empty
        /// </summary>
        public virtual bool IsEmpty
        {
            get { return baseHeap.Count == 0; }
        }

        #endregion Priority queue operations

        #region Heap operations

        protected virtual void ExchangeElements(int pos1, int pos2)
        {
            var val = baseHeap[pos1];
            baseHeap[pos1] = baseHeap[pos2];
            baseHeap[pos2] = val;
        }

        protected virtual void Insert(T item)
        {
            baseHeap.Add(item);

            // heap[i] have children heap[2*i + 1] and heap[2*i + 2] and parent heap[(i-1)/ 2];

            // heapify after insert, from end to beginning
            HeapifyFromEndToBeginning(baseHeap.Count/* - 1*/); // LUCENENET BUG: We are subtracting 1 from count here and within the HeapifyFromEndToBeginning method
        }

        protected virtual int HeapifyFromEndToBeginning(int pos)
        {
            if (comparer == null)
            {
                return HeapifyFromEndToBeginningUsingComparable(pos);
            }
            else
            {
                return HeapifyFromEndToBeginningUsingComparer(pos);
            }
        }

        protected virtual int HeapifyFromEndToBeginningUsingComparable(int pos)
        {
            if (pos >= baseHeap.Count) return -1;

            while (pos > 0)
            {
                var parentPos = (pos - 1) / 2;
                if (baseHeap[parentPos].CompareTo(baseHeap[pos]) < 0)
                {
                    ExchangeElements(parentPos, pos);
                    pos = parentPos;
                }
                else break;
            }
            return pos;
        }

        protected virtual int HeapifyFromEndToBeginningUsingComparer(int pos)
        {
            if (pos >= baseHeap.Count) return -1;

            while (pos > 0)
            {
                var parentPos = (pos - 1) / 2;
                if (comparer.Compare(baseHeap[parentPos], baseHeap[pos]) > 0)
                    {
                    ExchangeElements(parentPos, pos);
                    pos = parentPos;
                }
                else break;
            }
            return pos;
        }

        protected virtual void DeleteRoot()
        {
            if (baseHeap.Count <= 1)
            {
                baseHeap.Clear();
                return;
            }

            baseHeap[0] = baseHeap[baseHeap.Count - 1];
            baseHeap.RemoveAt(baseHeap.Count - 1);

            // heapify
            HeapifyFromBeginningToEnd(0);
        }

        protected virtual void HeapifyFromBeginningToEnd(int pos)
        {
            if (comparer == null)
            {
                HeapifyFromEndToBeginningUsingComparable(pos);
            }
            else
            {
                HeapifyFromBeginningToEndUsingComparer(pos);
            }
        }

        protected virtual void HeapifyFromBeginningToEndUsingComparable(int pos)
        {
            if (pos >= baseHeap.Count) return;

            // heap[i] have children heap[2*i + 1] and heap[2*i + 2] and parent heap[(i-1)/ 2];

            while (true)
            {
                // on each iteration exchange element with its largest child
                var smallest = pos;
                var left = 2 * pos + 1;
                var right = 2 * pos + 2;
                if (left < baseHeap.Count && baseHeap[smallest].CompareTo(baseHeap[left]) < 0)
                    smallest = left;
                if (right < baseHeap.Count && baseHeap[smallest].CompareTo(baseHeap[right]) < 0)
                    smallest = right;

                if (smallest != pos)
                {
                    ExchangeElements(smallest, pos);
                    pos = smallest;
                }
                else break;
            }
        }

        protected virtual void HeapifyFromBeginningToEndUsingComparer(int pos)
        {
            if (pos >= baseHeap.Count) return;

            // heap[i] have children heap[2*i + 1] and heap[2*i + 2] and parent heap[(i-1)/ 2];

            while (true)
            {
                // on each iteration exchange element with its largest child
                var smallest = pos;
                var left = 2 * pos + 1;
                var right = 2 * pos + 2;
                if (left < baseHeap.Count && comparer.Compare(baseHeap[smallest], baseHeap[left]) > 0)
                    smallest = left;
                if (right < baseHeap.Count && comparer.Compare(baseHeap[smallest], baseHeap[right]) > 0)
                    smallest = right;

                if (smallest != pos)
                {
                    ExchangeElements(smallest, pos);
                    pos = smallest;
                }
                else break;
            }
        }

        #endregion Heap operations

        #region ICollection<T> implementation

        /// <summary>
        /// Enqueus element into priority queue
        /// </summary>
        /// <param name="item">element to add</param>
        public virtual void Add(T item)
        {
            Enqueue(item);
        }

        /// <summary>
        /// Clears the collection
        /// </summary>
        public virtual void Clear()
        {
            baseHeap.Clear();
        }

        /// <summary>
        /// Determines whether the priority queue contains a specific element
        /// </summary>
        /// <param name="item">The object to locate in the priority queue</param>
        /// <returns><c>true</c> if item is found in the priority queue; otherwise, <c>false.</c> </returns>
        public virtual bool Contains(T item)
        {
            return baseHeap.Contains(item);
        }

        /// <summary>
        /// Gets number of elements in the priority queue
        /// </summary>
        public virtual int Count
        {
            get { return baseHeap.Count; }
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
            baseHeap.CopyTo(array, arrayIndex);
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
        /// Removes the first occurrence of a specific object from the priority queue.
        /// </summary>
        /// <param name="item">The object to remove from the ICollection <(Of <(T >)>). </param>
        /// <returns><c>true</c> if item was successfully removed from the priority queue.
        /// This method returns false if item is not found in the collection. </returns>
        public virtual bool Remove(T item)
        {
            // find element in the collection and remove it
            var elementIdx = baseHeap.IndexOf(item);
            if (elementIdx < 0) return false;

            //remove element
            baseHeap[elementIdx] = baseHeap[baseHeap.Count - 1];
            baseHeap.RemoveAt(baseHeap.Count - 1);

            // heapify
            var newPos = HeapifyFromEndToBeginning(elementIdx);
            if (newPos == elementIdx)
                HeapifyFromBeginningToEnd(elementIdx);

            return true;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>Enumerator</returns>
        /// <remarks>
        /// Returned enumerator does not iterate elements in sorted order.</remarks>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return baseHeap.GetEnumerator();
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

        #endregion ICollection<T> implementation
    }
}