using J2N.Numerics;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#nullable enable

namespace Lucene.Net.Util
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
    /// Provides the sentinel instances of <typeparamref name="T"/> to a
    /// <see cref="PriorityQueue{T}"/>.
    /// </summary>
    /// <remarks>
    /// This interface can be implemented to provide sentinel
    /// instances. The implementation of this interface can be passed to
    /// a constructor of <see cref="PriorityQueue{T}"/> or <see cref="ValuePriorityQueue{T}"/>.
    /// If an instance is provided, the either constructor will use
    /// the <see cref="Create()"/> method to fill the queue,
    /// so that code which uses the queue can always assume it is full and only
    /// change the top without attempting to insert any new items.
    /// <para/>
    /// Those sentinel values should always compare worse than any non-sentinel
    /// value (i.e., <see cref="PriorityQueue{T}.LessThan(T, T)"/>,
    /// <see cref="PriorityComparer{T}.LessThan(T, T)"/>, or
    /// <see cref="IComparer{T}.Compare(T, T)"/> should always favor the
    /// non-sentinel values).
    /// <para/>
    /// When using a <see cref="ISentinelFactory{T}"/>, the following usage pattern
    /// is recommended:
    /// <code>
    /// // Implements ISentinelFactory&lt;T&gt;.Create(PriorityQueue&lt;T&gt;)
    /// var sentinelFactory = new MySentinelFactory&lt;MyObject&gt;();
    /// PriorityQueue&lt;MyObject&gt; pq = new MyQueue&lt;MyObject&gt;(sentinelFactory);
    /// // save the 'top' element, which is guaranteed to not be <c>default</c>.
    /// MyObject pqTop = pq.Top;
    /// &lt;...&gt;
    /// // now in order to add a new element, which is 'better' than top (after
    /// // you've verified it is better), it is as simple as:
    /// pqTop.Change();
    /// pqTop = pq.UpdateTop();
    /// </code>
    /// <b>NOTE:</b> <see cref="Create()"/> will be called by the
    /// <see cref="PriorityQueue{T}"/> or <see cref="ValuePriorityQueue{T}"/> constructor
    /// <see cref="PriorityQueue{T}.Count"/> or <see cref="ValuePriorityQueue{T}.Count"/>
    /// times, relying on a new instance to be returned. Therefore you should ensure any call to this
    /// <see cref="Create()"/> creates a new instance and behaves consistently, e.g., it cannot
    /// return <c>null</c> if it previously returned non-<c>null</c>.
    /// </remarks>
    /// <typeparam name="T">The type of sentinel instance to create.</typeparam>
    // LUCENENET specific interface to eliminate the virtual method call in the PriorityQueue<T> constructor.
    public interface ISentinelFactory<T>
    {
        /// <summary>
        /// Creates a sentinel instance of <typeparamref name="T"/> to fill an element
        /// of a <see cref="PriorityQueue{T}"/>.
        /// </summary>
        /// <returns>A newly created sentinel instance for use in a single element of <see cref="PriorityQueue{T}"/>.</returns>
        T Create();
    }

    /// <summary>
    /// An adapter comparer for use with <see cref="ValuePriorityQueue{T}"/>.
    /// <para/>
    /// This comparer makes it easy to convert <see cref="PriorityQueue{T}"/>-derived
    /// subclasses into comparers that can be used with <see cref="ValuePriorityQueue{T}"/>.
    /// <para/>
    /// <b>NOTE:</b> This adapter converts the 2-state <see cref="LessThan(T, T)"/>
    /// method to the 3-state <see cref="IComparer{T}.Compare(T, T)"/> by always assuming if
    /// <see cref="LessThan(T, T)"/> returns <c>false</c> that the left node is greater than
    /// the right node. This means nodes will never compare equal, even if they actually are.
    /// Therefore, it may not work correctly for most applications of <see cref="IComparer{T}"/>,
    /// but will work correctly with <see cref="ValuePriorityQueue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of item to compare. This may be either a value type or a reference type.</typeparam>
    public abstract class PriorityComparer<T> : IComparer<T>
    {
        /// <summary>
        /// Determines the ordering of objects in this priority queue.  Subclasses
        /// must define this one method.
        /// </summary>
        /// <param name="a">The left value to compare.</param>
        /// <param name="b">The right value to compare.</param>
        /// <returns> <c>true</c> if parameter <paramref name="a"/> is less than parameter <paramref name="b"/>; otherwise <c>false</c>. </returns>
        protected internal abstract bool LessThan(T a, T b);

        int IComparer<T>.Compare(T? a, T? b) => LessThan(a!, b!) ? -1 : 1;
    }

    /// <summary>
    /// A <see cref="ValuePriorityQueue{T}"/> maintains a partial ordering of its elements such that the
    /// element with least priority can always be found in constant time. Put()'s and Pop()'s
    /// require log(size) time.
    /// <para/>
    /// This ref struct has the same behavior as <see cref="PriorityQueue{T}"/>, but can be
    /// used to reduce allocations in scenarios where ref structs are allowed. If the type of
    /// <typeparamref name="T"/> is a struct the passed in <see cref="Span{T}"/> buffer
    /// may also be allocated on the stack, as allowed.
    ///
    /// <para/><b>NOTE</b>: The allocation size of the supplied <see cref="Span{T}"/> buffer
    /// must be <c>maxSize+1</c>. The <see cref="PriorityQueue.GetArrayHeapSize(int)"/> method
    /// can be used to get the correct calculation and to validate bounds of maxSize.
    /// <para/>
    /// If instantiated with a constructor that accepts a <see cref="ISentinelFactory{T}"/>,
    /// that factory will be used to populate the elements of the array.
    /// <para/>
    /// <b>NOTE</b>: The type of <typeparamref name="T"/> may be either a class
    /// or a value type. But do note that it was specifically designed with
    /// nullable types in mind, so there may be cases where you need to use a
    /// nullable value type to get it to behave correctly.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public ref struct ValuePriorityQueue<T>
    {
        private int size;
        internal int maxSize; // LUCENENET: Internal for testing
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Major Code Smell", "S2933:Fields that are only assigned in the constructor should be \"readonly\"", Justification = "Structs are known to have performance issues with readonly fields")]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Structs are known to have performance issues with readonly fields")]
        private Span<T> heap;
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Major Code Smell", "S2933:Fields that are only assigned in the constructor should be \"readonly\"", Justification = "Structs are known to have performance issues with readonly fields")]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Structs are known to have performance issues with readonly fields")]
        private IComparer<T> comparer;

        /// <summary>
        /// Initializes a new instance of <see cref="ValuePriorityQueue{T}"/> with the specified
        /// <paramref name="buffer"/> and <paramref name="comparer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to use for priority queue operations.
        /// <para/>
        /// To determine the correct size of buffer to allocate, use the
        /// <see cref="PriorityQueue.GetArrayHeapSize(int)"/> method.
        /// </param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> implementation that is
        /// used to determine the order of items in the priority queue.
        /// Note that if the comparer returns 0, the value is treated no differently than
        /// if it were 1 (greater than).</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <c>null</c>.</exception>
        public ValuePriorityQueue(Span<T> buffer, IComparer<T> comparer)
            : this(buffer, comparer, sentinelFactory: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ValuePriorityQueue{T}"/> with the specified
        /// <paramref name="buffer"/> and <paramref name="comparer"/>.
        /// </summary>
        /// <param name="buffer">The buffer to use for priority queue operations.
        /// <para/>
        /// To determine the correct size of buffer to allocate, use the
        /// <see cref="PriorityQueue.GetArrayHeapSize(int)"/> method.
        /// </param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> implementation that is
        /// used to determine the order of items in the priority queue.
        /// Note that if the comparer returns 0, the value is treated no differently than
        /// if it were 1 (greater than).</param>
        /// <param name="sentinelFactory">If not <c>null</c>, the queue will be pre-populated.
        /// This factory will be called <paramref name="buffer"/>.Length - 1 times to get an instance
        /// to provide to each element in the queue. <paramref name="sentinelFactory"/> should
        /// return a new instance on each call, but each sentinel instance should consistently
        /// represent the same value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <c>null</c>.</exception>
        /// <seealso cref="ISentinelFactory{T}"/>
        public ValuePriorityQueue(Span<T> buffer, IComparer<T> comparer, ISentinelFactory<T>? sentinelFactory)
        {
            this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
            heap = buffer;
            // NOTE: we subtract 1 because all access to heap is
            // 1-based not 0-based.  heap[0] is unused.
            maxSize = buffer.Length - 1;
            size = 0;
            // LUCENENET: If we have a sentinel factory, it means we should pre-populate the array
            // with sentinel values.
            if (sentinelFactory is not null)
            {
                for (int i = 1; i < heap.Length; i++)
                {
                    heap[i] = sentinelFactory.Create();
                }
                size = maxSize;
            }
        }

        /// <summary>Returns the underlying storage of the queue.</summary>
        public Span<T> RawHeap => heap;

        /// <summary>
        /// Gets the comparer used to determine the order of objects in this priority queue.
        /// </summary>
        public IComparer<T> Comparer => comparer;

        /// <summary>
        /// Determines the ordering of objects in this priority queue.
        /// </summary>
        /// <returns><c>true</c> if parameter <paramref name="a"/> is less than parameter
        /// <paramref name="b"/>; othewise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool LessThan(T a, T b)
            => comparer.Compare(a, b) < 0;

#nullable restore

        /// <summary>
        /// Adds an Object to a <see cref="PriorityQueue{T}"/> in log(size) time. If one tries to add
        /// more objects than <see cref="maxSize"/> from initialize and it is not possible to resize
        /// the heap, an <see cref="IndexOutOfRangeException"/> is thrown.
        /// </summary>
        /// <returns> The new 'top' element in the queue. </returns>
        // LUCENENET: IMPORTANT - this implementation should remain in sync with PriorityQueue<T>
        public T Add(T element) // LUCENENET TODO: Factor out the IndexOutOfRangeException here and make a TryAdd() method.
        {
            size++;
            heap[size] = element;
            UpHeap();
            return heap[1];
        }

        /// <summary>
        /// Adds an Object to a <see cref="PriorityQueue{T}"/> in log(size) time.
        /// If the given <paramref name="element"/> is smaller than then full
        /// heap's minimum, it won't be added.
        /// </summary>
        // LUCENENET: IMPORTANT - this implementation should remain in sync with PriorityQueue<T>
        public void Insert(T element) // LUCENENET specific - added as a more efficient way to insert value types without reuse
        {
            if (size < maxSize)
            {
                Add(element);
            }
            else if (size > 0 && !LessThan(element, heap[1]))
            {
                heap[1] = element;
                UpdateTop();
            }
        }

        /// <summary>
        /// Adds an Object to a <see cref="PriorityQueue{T}"/> in log(size) time.
        /// It returns the object (if any) that was
        /// dropped off the heap because it was full. This can be
        /// the given parameter (in case it is smaller than the
        /// full heap's minimum, and couldn't be added), or another
        /// object that was previously the smallest value in the
        /// heap and now has been replaced by a larger one, or <c>null</c>
        /// if the queue wasn't yet full with <see cref="maxSize"/> elements.
        /// </summary>
        // LUCENENET: IMPORTANT - this implementation should remain in sync with PriorityQueue<T>
        public T InsertWithOverflow(T element)
        {
            if (size < maxSize)
            {
                Add(element);
                return default;
            }
            else if (size > 0 && !LessThan(element, heap[1]))
            {
                T ret = heap[1];
                heap[1] = element;
                UpdateTop();
                return ret;
            }
            else
            {
                return element;
            }
        }

        /// <summary>
        /// Returns the least element of the <see cref="PriorityQueue{T}"/> in constant time.
        /// Returns <c>null</c> if the queue is empty. </summary>
        public T Top =>
            // We don't need to check size here: if maxSize is 0,
            // then heap is length 2 array with both entries null.
            // If size is 0 then heap[1] is already null.
            heap[1];

        /// <summary>
        /// Removes and returns the least element of the <see cref="PriorityQueue{T}"/> in log(size)
        /// time.
        /// </summary>
        // LUCENENET: IMPORTANT - this implementation should remain in sync with PriorityQueue<T>
        public T Pop()
        {
            if (size > 0)
            {
                T result = heap[1]; // save first value
                heap[1] = heap[size]; // move last to first
                heap[size] = default; // permit GC of objects
                size--;
                DownHeap(); // adjust heap
                return result;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Should be called when the Object at top changes values. Still log(n) worst
        /// case, but it's at least twice as fast to
        ///
        /// <code>
        /// pq.Top.Change();
        /// pq.UpdateTop();
        /// </code>
        ///
        /// instead of
        ///
        /// <code>
        /// o = pq.Pop();
        /// o.Change();
        /// pq.Push(o);
        /// </code>
        /// </summary>
        /// <returns> The new 'top' element. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // LUCENENET: IMPORTANT - this implementation should remain in sync with PriorityQueue<T>
        public T UpdateTop()
        {
            DownHeap();
            return heap[1];
        }

        /// <summary>
        /// Returns the number of elements currently stored in the <see cref="PriorityQueue{T}"/>.
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public int Count => size;

        /// <summary>
        /// Removes all entries from the <see cref="PriorityQueue{T}"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // LUCENENET: IMPORTANT - this implementation should remain in sync with PriorityQueue<T>
        public void Clear()
        {
            for (int i = 0; i <= size; i++)
            {
                heap[i] = default;
            }
            size = 0;
        }

        // LUCENENET: IMPORTANT - this implementation should remain in sync with PriorityQueue<T>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpHeap()
        {
            int i = size;
            T node = heap[i]; // save bottom node
            int j = i.TripleShift(1);
            while (j > 0 && LessThan(node, heap[j]))
            {
                heap[i] = heap[j]; // shift parents down
                i = j;
                j = j.TripleShift(1);
            }
            heap[i] = node; // install saved node
        }

        // LUCENENET: IMPORTANT - this implementation should remain in sync with PriorityQueue<T>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DownHeap()
        {
            int i = 1;
            T node = heap[i]; // save top node
            int j = i << 1; // find smaller child
            int k = j + 1;
            if (k <= size && LessThan(heap[k], heap[j]))
            {
                j = k;
            }
            while (j <= size && LessThan(heap[j], node))
            {
                heap[i] = heap[j]; // shift up child
                i = j;
                j = i << 1;
                k = j + 1;
                if (k <= size && LessThan(heap[k], heap[j]))
                {
                    j = k;
                }
            }
            heap[i] = node; // install saved node
        }
    }

#nullable enable

    /// <summary>
    /// LUCENENET specific static methods for managing priority queues.
    /// </summary>
    public static class PriorityQueue
    {
        /// <summary>
        /// Gets the heap size for the specified <paramref name="maxSize"/> of
        /// a priority queue and checks to ensure it is within bounds.
        /// <para/>
        /// Heap size is 1 greater than <paramref name="maxSize"/>, since priority
        /// queue is 1-based rather than 0-based.
        /// </summary>
        /// <param name="maxSize">The maximum length of the priority queue.</param>
        /// <returns>The heap size to allocate for the priority queue.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxSize"/> is less than 0.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="maxSize"/> is greater than <see cref="ArrayUtil.MAX_ARRAY_LENGTH"/>.
        /// </exception>
        public static int GetArrayHeapSize(int maxSize)
        {
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Non-negative number required.");

            int heapSize;
            if (0 == maxSize)
            {
                // We allocate 1 extra to avoid if statement in top()
                heapSize = 2;
            }
            else
            {
                if (maxSize > ArrayUtil.MAX_ARRAY_LENGTH)
                {
                    // Don't wrap heapSize to -1, in this case, which
                    // causes a confusing NegativeArraySizeException.
                    // Note that very likely this will simply then hit
                    // an OOME, but at least that's more indicative to
                    // caller that this values is too big.  We don't +1
                    // in this case, but it's very unlikely in practice
                    // one will actually insert this many objects into
                    // the PQ:
                    // Throw exception to prevent confusing OOME:
                    throw new ArgumentOutOfRangeException(nameof(maxSize), "maxSize must be <= " + ArrayUtil.MAX_ARRAY_LENGTH + "; got: " + maxSize); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                else
                {
                    // NOTE: we add +1 because all access to heap is
                    // 1-based not 0-based.  heap[0] is unused.
                    heapSize = maxSize + 1;
                }
            }
            return heapSize;
        }
    }

    /// <summary>
    /// A <see cref="PriorityQueue{T}"/> maintains a partial ordering of its elements such that the
    /// element with least priority can always be found in constant time. Put()'s and Pop()'s
    /// require log(size) time.
    ///
    /// <para/><b>NOTE</b>: this class will pre-allocate a full array of
    /// length <c>maxSize+1</c> if instantiated with a constructor that
    /// accepts a <see cref="ISentinelFactory{T}"/>. That maximum size can
    /// grow as we insert elements over time.
    /// <para/>
    /// <b>NOTE</b>: The type of <typeparamref name="T"/> may be either a class
    /// or a value type. But do note that it was specifically designed with
    /// nullable types in mind, so there may be cases where you need to use a
    /// nullable value type to get it to behave correctly.
    /// <para/>
    /// @lucene.internal
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class PriorityQueue<T>
    {
        private int size = 0;
        internal readonly int maxSize; // LUCENENET: Internal for testing
        internal T?[] heap; // LUCENENET: Internal for testing

        /// <summary>
        /// Initializes a new instance of <see cref="PriorityQueue{T}"/> with the
        /// specified <paramref name="maxSize"/>.
        /// </summary>
        /// <param name="maxSize">The maximum number of elements this queue can hold.</param>
        protected PriorityQueue(int maxSize) // LUCENENET specific - made protected instead of public
            : this(maxSize, sentinelFactory: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PriorityQueue{T}"/> with the
        /// specified <paramref name="maxSize"/> and <paramref name="sentinelFactory"/>.
        /// </summary>
        /// <param name="maxSize">The maximum number of elements this queue can hold.</param>
        /// <param name="sentinelFactory">If not <c>null</c>, the queue will be pre-populated.
        /// This factory will be called <paramref name="maxSize"/> times to get an instance
        /// to provide to each element in the queue. <paramref name="sentinelFactory"/> should
        /// return a new instance on each call, but each sentinel instance should consistently
        /// represent the same value.</param>
        /// <seealso cref="ISentinelFactory{T}"/>
        protected PriorityQueue(int maxSize, ISentinelFactory<T>? sentinelFactory)
        {
            int heapSize = PriorityQueue.GetArrayHeapSize(maxSize);
            this.maxSize = maxSize;
            this.heap = new T[heapSize];
            // LUCENENET: If we have a sentinel factory, it means we should pre-populate the array
            // with sentinel values.
            if (sentinelFactory is not null)
            {
                for (int i = 1; i < heap.Length; i++)
                {
                    heap[i] = sentinelFactory.Create();
                }
                size = maxSize;
            }
        }

        /// <summary>
        /// Determines the ordering of objects in this priority queue.  Subclasses
        /// must define this one method. </summary>
        /// <returns><c>true</c> if parameter <paramref name="a"/> is less than parameter
        /// <paramref name="b"/>; othewise, <c>false</c>.</returns>
        protected internal abstract bool LessThan(T a, T b); // LUCENENET: Internal for testing

        // LUCENENET specific - refactored getSentinelObject() method into ISentinelFactory<T> and
        // SentinelFactory<T, TPriorityQueue>

#nullable restore

        /// <summary>
        /// Adds an Object to a <see cref="PriorityQueue{T}"/> in log(size) time. If one tries to add
        /// more objects than <see cref="maxSize"/> from initialize and it is not possible to resize
        /// the heap, an <see cref="IndexOutOfRangeException"/> is thrown.
        /// </summary>
        /// <returns> The new 'top' element in the queue. </returns>
        // LUCENENET: IMPORTANT - this implementation should remain in sync with ValuePriorityQueue<T>
        public T Add(T element)
        {
            size++;
            heap[size] = element;
            UpHeap();
            return heap[1];
        }

        /// <summary>
        /// Adds an Object to a <see cref="PriorityQueue{T}"/> in log(size) time.
        /// If the given <paramref name="element"/> is smaller than then full
        /// heap's minimum, it won't be added.
        /// </summary>
        // LUCENENET: IMPORTANT - this implementation should remain in sync with ValuePriorityQueue<T>
        public virtual void Insert(T element) // LUCENENET specific - added as a more efficient way to insert value types without reuse
        {
            if (size < maxSize)
            {
                Add(element);
            }
            else if (size > 0 && !LessThan(element, heap[1]))
            {
                heap[1] = element;
                UpdateTop();
            }
        }

        /// <summary>
        /// Adds an Object to a <see cref="PriorityQueue{T}"/> in log(size) time.
        /// It returns the object (if any) that was
        /// dropped off the heap because it was full. This can be
        /// the given parameter (in case it is smaller than the
        /// full heap's minimum, and couldn't be added), or another
        /// object that was previously the smallest value in the
        /// heap and now has been replaced by a larger one, or <c>null</c>
        /// if the queue wasn't yet full with <see cref="maxSize"/> elements.
        /// </summary>
        // LUCENENET: IMPORTANT - this implementation should remain in sync with ValuePriorityQueue<T>
        public virtual T InsertWithOverflow(T element)
        {
            if (size < maxSize)
            {
                Add(element);
                return default;
            }
            else if (size > 0 && !LessThan(element, heap[1]))
            {
                T ret = heap[1];
                heap[1] = element;
                UpdateTop();
                return ret;
            }
            else
            {
                return element;
            }
        }

        /// <summary>
        /// Returns the least element of the <see cref="PriorityQueue{T}"/> in constant time.
        /// Returns <c>null</c> if the queue is empty. </summary>
        // LUCENENET: IMPORTANT - this implementation should remain in sync with ValuePriorityQueue<T>
        public T Top =>
            // We don't need to check size here: if maxSize is 0,
            // then heap is length 2 array with both entries null.
            // If size is 0 then heap[1] is already null.
            heap[1];

        /// <summary>
        /// Removes and returns the least element of the <see cref="PriorityQueue{T}"/> in log(size)
        /// time.
        /// </summary>
        // LUCENENET: IMPORTANT - this implementation should remain in sync with ValuePriorityQueue<T>
        public T Pop()
        {
            if (size > 0)
            {
                T result = heap[1]; // save first value
                heap[1] = heap[size]; // move last to first
                heap[size] = default; // permit GC of objects
                size--;
                DownHeap(); // adjust heap
                return result;
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Should be called when the Object at top changes values. Still log(n) worst
        /// case, but it's at least twice as fast to
        ///
        /// <code>
        /// pq.Top.Change();
        /// pq.UpdateTop();
        /// </code>
        ///
        /// instead of
        ///
        /// <code>
        /// o = pq.Pop();
        /// o.Change();
        /// pq.Push(o);
        /// </code>
        /// </summary>
        /// <returns> The new 'top' element. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // LUCENENET: IMPORTANT - this implementation should remain in sync with ValuePriorityQueue<T>
        public T UpdateTop()
        {
            DownHeap();
            return heap[1];
        }

        /// <summary>
        /// Returns the number of elements currently stored in the <see cref="PriorityQueue{T}"/>.
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public int Count => size;

        /// <summary>
        /// Removes all entries from the <see cref="PriorityQueue{T}"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            for (int i = 0; i <= size; i++)
            {
                heap[i] = default;
            }
            size = 0;
        }

        // LUCENENET: IMPORTANT - this implementation should remain in sync with ValuePriorityQueue<T>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpHeap()
        {
            int i = size;
            T node = heap[i]; // save bottom node
            int j = i.TripleShift(1);
            while (j > 0 && LessThan(node, heap[j]))
            {
                heap[i] = heap[j]; // shift parents down
                i = j;
                j = j.TripleShift(1);
            }
            heap[i] = node; // install saved node
        }

        // LUCENENET: IMPORTANT - this implementation should remain in sync with ValuePriorityQueue<T>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DownHeap()
        {
            int i = 1;
            T node = heap[i]; // save top node
            int j = i << 1; // find smaller child
            int k = j + 1;
            if (k <= size && LessThan(heap[k], heap[j]))
            {
                j = k;
            }
            while (j <= size && LessThan(heap[j], node))
            {
                heap[i] = heap[j]; // shift up child
                i = j;
                j = i << 1;
                k = j + 1;
                if (k <= size && LessThan(heap[k], heap[j]))
                {
                    j = k;
                }
            }
            heap[i] = node; // install saved node
        }

        /// <summary>
        /// Gets the internal heap array as T[].
        /// <para/>
        /// @lucene.internal
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        protected internal T[] HeapArray => heap;
    }
}