using System;

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
    /// A PriorityQueue maintains a partial ordering of its elements such that the
    /// element with least priority can always be found in constant time. It is represented as a
    /// Min-Heap so that Add()'s and Pop()'s require log(size) time.
    ///
    /// <p><b>NOTE</b>: this class will pre-allocate a full array of
    /// length <code>maxSize+1</code> if instantiated via the
    /// <seealso cref="#PriorityQueue(int,boolean)"/> constructor with
    /// <code>prepopulate</code> set to <code>true</code>. That maximum
    /// size can grow as we insert elements over the time.
    ///
    /// @lucene.internal
    /// </summary>
    public abstract class PriorityQueue<T>
    {
        private int QueueSize = 0;
        private int MaxSize;
        private T[] Heap;
        private bool resizable;

        public PriorityQueue() // LUCENENET TODO: Remove this constructor (and revert the rest of the implementation back to its original state)
            : this(8, false)
        {
            resizable = true;
        } 

        public PriorityQueue(int maxSize = 128)
            : this(maxSize, true)
        {
        }
 
        public PriorityQueue(int maxSize, bool prepopulate)
        {
            resizable = false;

            if (maxSize < 0)
            {
                throw new System.ArgumentException("maxSize must be >= 0; got: " + maxSize);
            }
            else
            {
                if (0 == maxSize)
                {
                    // We allocate 1 extra to avoid if statement in top()
                    maxSize++;
                }
                else
                {
                    if (maxSize >= ArrayUtil.MAX_ARRAY_LENGTH)
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
                        throw new System.ArgumentException("maxSize must be < " + ArrayUtil.MAX_ARRAY_LENGTH + "; got: " + maxSize);
                    }
                }
            }

            // NOTE: we add +1 because all access to heap is
            // 1-based not 0-based.  heap[0] is unused.
            int heapSize = maxSize + 1;
            
            // T is unbounded type, so this unchecked cast works always:
            T[] h = new T[heapSize];
            this.Heap = h;
            this.MaxSize = maxSize;

            if (prepopulate)
            {
                // If sentinel objects are supported, populate the queue with them
                T sentinel = SentinelObject;
                if (sentinel != null)
                {
                    Heap[1] = sentinel;
                    for (int i = 2; i < Heap.Length; i++)
                    {
                        Heap[i] = SentinelObject;
                    }
                    QueueSize = maxSize;
                }
            }
        }

        /// <summary>
        /// Determines the ordering of objects in this priority queue.  Subclasses
        ///  must define this one method. </summary>
        ///  <returns> <code>true</code> iff parameter <tt>a</tt> is less than parameter <tt>b</tt>. </returns>
        protected internal abstract bool LessThan(T a, T b);

        /// <summary>
        /// this method can be overridden by extending classes to return a sentinel
        /// object which will be used by the <seealso cref="PriorityQueue#PriorityQueue(int,boolean)"/>
        /// constructor to fill the queue, so that the code which uses that queue can always
        /// assume it's full and only change the top without attempting to insert any new
        /// object.<br>
        ///
        /// Those sentinel values should always compare worse than any non-sentinel
        /// value (i.e., <seealso cref="#lessThan"/> should always favor the
        /// non-sentinel values).<br>
        ///
        /// By default, this method returns false, which means the queue will not be
        /// filled with sentinel values. Otherwise, the value returned will be used to
        /// pre-populate the queue. Adds sentinel values to the queue.<br>
        ///
        /// If this method is extended to return a non-null value, then the following
        /// usage pattern is recommended:
        ///
        /// <pre class="prettyprint">
        /// // extends getSentinelObject() to return a non-null value.
        /// PriorityQueue&lt;MyObject&gt; pq = new MyQueue&lt;MyObject&gt;(numHits);
        /// // save the 'top' element, which is guaranteed to not be null.
        /// MyObject pqTop = pq.top();
        /// &lt;...&gt;
        /// // now in order to add a new element, which is 'better' than top (after
        /// // you've verified it is better), it is as simple as:
        /// pqTop.change().
        /// pqTop = pq.updateTop();
        /// </pre>
        ///
        /// <b>NOTE:</b> if this method returns a non-null value, it will be called by
        /// the <seealso cref="PriorityQueue#PriorityQueue(int,boolean)"/> constructor
        /// <seealso cref="#size()"/> times, relying on a new object to be returned and will not
        /// check if it's null again. Therefore you should ensure any call to this
        /// method creates a new instance and behaves consistently, e.g., it cannot
        /// return null if it previously returned non-null.
        /// </summary>
        /// <returns> the sentinel object to use to pre-populate the queue, or null if
        ///         sentinel objects are not supported. </returns>
        protected virtual T SentinelObject // LUCENENET TODO: Change to GetSentinalObject() (returns new instance in some cases)
        {
            get
            {
                return default(T);
            }
        }

        /// <summary>
        /// Adds an Object to a PriorityQueue in log(size) time. If one tries to add
        /// more objects than maxSize from initialize and it is not possible to resize
        /// the heap, an <seealso cref="IndexOutOfRangeException"/> is thrown.
        /// </summary>
        /// <returns> the new 'top' element in the queue. </returns>
        public T Add(T element)
        {
            QueueSize++;
            if (resizable && QueueSize > MaxSize)
            {
                Resize();
            }
            Heap[QueueSize] = element;
            UpHeap();
            return Heap[1];
        }

        /// <summary>
        /// Adds an Object to a PriorityQueue in log(size) time.
        /// It returns the object (if any) that was
        /// dropped off the heap because it was full. this can be
        /// the given parameter (in case it is smaller than the
        /// full heap's minimum, and couldn't be added), or another
        /// object that was previously the smallest value in the
        /// heap and now has been replaced by a larger one, or null
        /// if the queue wasn't yet full with maxSize elements.
        /// </summary>
        public virtual T InsertWithOverflow(T element)
        {
            if (QueueSize < MaxSize)
            {
                Add(element);
                return default(T);
            }
            else if (QueueSize > 0 && !LessThan(element, Heap[1]))
            {
                T ret = Heap[1];
                Heap[1] = element;
                DownHeap();
                return ret;
            }
            else
            {
                return element;
            }
        }

        /// <summary>
        /// Returns the least element of the PriorityQueue in constant time.
        /// Returns null if the queue is empty. </summary>
        public T Top() // LUCENENET TODO: Change to property
        {
            // We don't need to check size here: if maxSize is 0,
            // then heap is length 2 array with both entries null.
            // If size is 0 then heap[1] is already null.
            return Heap[1]; // LUCENENET TODO: add check to ensure there is a value so this doesn't throw an exception
        }

        /// <summary>
        /// Removes and returns the least element of the PriorityQueue in log(size)
        ///  time.
        /// </summary>
        public T Pop()
        {
            if (QueueSize > 0)
            {
                T result = Heap[1]; // save first value
                Heap[1] = Heap[QueueSize]; // move last to first
                Heap[QueueSize] = default(T); // permit GC of objects
                QueueSize--;
                DownHeap(); // adjust heap
                return result;
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Should be called when the Object at top changes values. Still log(n) worst
        /// case, but it's at least twice as fast to
        ///
        /// <pre class="prettyprint">
        /// pq.top().change();
        /// pq.updateTop();
        /// </pre>
        ///
        /// instead of
        ///
        /// <pre class="prettyprint">
        /// o = pq.pop();
        /// o.change();
        /// pq.push(o);
        /// </pre>
        /// </summary>
        /// <returns> the new 'top' element. </returns>
        public T UpdateTop()
        {
            DownHeap();
            return Heap[1];
        }

        /// <summary>
        /// Returns the number of elements currently stored in the PriorityQueue. </summary>
        public int Size() // LUCENENET TODO: make property, rename Count
        {
            return QueueSize;
        }

        /// <summary>
        /// Removes all entries from the PriorityQueue. </summary>
        public void Clear()
        {
            for (int i = 0; i <= QueueSize; i++)
            {
                Heap[i] = default(T);
            }
            QueueSize = 0;
        }

        public T[] ToArray() // LUCENENET TODO: Remove this method (after TopTermsRewrite is fixed)
        {
            T[] copy = new T[QueueSize];
            Array.Copy(Heap, 1, copy, 0, QueueSize);
            return copy;
        }

        private void Resize() // LUCENENET TODO: Remove this method
        {
            int newSize = Math.Min(ArrayUtil.MAX_ARRAY_LENGTH - 1, 2*MaxSize);
            T[] newHeap = new T[newSize + 1];
            Array.Copy(Heap, newHeap, MaxSize + 1);
            MaxSize = newSize;
            Heap = newHeap;
        }

        private void UpHeap()
        {
            int i = QueueSize;
            T node = Heap[i]; // save bottom node
            int j = (int)((uint)i >> 1);
            while (j > 0 && LessThan(node, Heap[j]))
            {
                Heap[i] = Heap[j]; // shift parents down
                i = j;
                j = (int)((uint)j >> 1);
            }
            Heap[i] = node; // install saved node
        }

        private void DownHeap()
        {
            int i = 1;
            T node = Heap[i]; // save top node
            int j = i << 1; // find smaller child
            int k = j + 1;
            if (k <= QueueSize && LessThan(Heap[k], Heap[j]))
            {
                j = k;
            }
            while (j <= QueueSize && LessThan(Heap[j], node))
            {
                Heap[i] = Heap[j]; // shift up child
                i = j;
                j = i << 1;
                k = j + 1;
                if (k <= QueueSize && LessThan(Heap[k], Heap[j]))
                {
                    j = k;
                }
            }
            Heap[i] = node; // install saved node
        }

        /// <summary>
        /// this method returns the internal heap array as Object[].
        /// @lucene.internal
        /// </summary>
        protected internal object[] HeapArray // LUCENENET TODO: change to GetHeapArray() (array)
        {
            get
            {
                return (object[])(Array)Heap;
            }
        }
    }
}