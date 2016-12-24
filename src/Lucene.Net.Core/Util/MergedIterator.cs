using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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

    using System;

    //LUCENE TO-DO Rewrote class

    /// <summary>
    /// Provides a merged sorted view from several sorted iterators.
    /// <p>
    /// If built with <code>removeDuplicates</code> set to true and an element
    /// appears in multiple iterators then it is deduplicated, that is this iterator
    /// returns the sorted union of elements.
    /// <p>
    /// If built with <code>removeDuplicates</code> set to false then all elements
    /// in all iterators are returned.
    /// <p>
    /// Caveats:
    /// <ul>
    ///   <li>The behavior is undefined if the iterators are not actually sorted.
    ///   <li>Null elements are unsupported.
    ///   <li>If removeDuplicates is set to true and if a single iterator contains
    ///       duplicates then they will not be deduplicated.
    ///   <li>When elements are deduplicated it is not defined which one is returned.
    ///   <li>If removeDuplicates is set to false then the order in which duplicates
    ///       are returned isn't defined.
    /// </ul>
    /// @lucene.internal
    /// </summary>
    public sealed class MergedIterator<T> : IEnumerator<T>
        where T : IComparable<T>
    {
        private readonly TermMergeQueue<T> Queue;
        private readonly SubIterator<T>[] Top;
        private readonly bool RemoveDuplicates;
        private int NumTop;
        private T current;

        public MergedIterator(params IEnumerator<T>[] iterators)
            : this(true, iterators)
        {
        }

        public MergedIterator(bool removeDuplicates, params IEnumerator<T>[] iterators)
        {
            this.RemoveDuplicates = removeDuplicates;
            Queue = new TermMergeQueue<T>(iterators.Length);
            Top = new SubIterator<T>[iterators.Length];
            int index = 0;
            foreach (IEnumerator<T> iter in iterators)
            {
                // If hasNext
                if (iter.MoveNext())
                {
                    SubIterator<T> sub = new SubIterator<T>();
                    sub.Current = iter.Current;
                    sub.Iterator = iter;
                    sub.Index = index++;
                    Queue.Add(sub);
                }
            }
        }

        public bool MoveNext()
        {
            PushTop();

            if (Queue.Size() > 0)
            {
                PullTop();
            }
            else
            {
                return false;
            }

            return true;
        }

        public T Current
        {
            get
            {
                return current;
            }
        }

        object System.Collections.IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        private void PullTop()
        {
            Debug.Assert(NumTop == 0);
            Top[NumTop++] = Queue.Pop();
            if (RemoveDuplicates)
            {
                //extract all subs from the queue that have the same top element
                while (Queue.Size() != 0 && Queue.Top().Current.Equals(Top[0].Current))
                {
                    Top[NumTop++] = Queue.Pop();
                }
            }
            current = Top[0].Current;
        }

        private void PushTop()
        {
            for (int i = 0; i < NumTop; ++i)
            {
                if (Top[i].Iterator.MoveNext())
                {
                    Top[i].Current = Top[i].Iterator.Current;
                    Queue.Add(Top[i]);
                }
                else
                {
                    Top[i].Current = default(T);
                }
            }
            NumTop = 0;
        }

        /*private T Current;
        private readonly TermMergeQueue<T> Queue;
        private readonly SubIterator<T>[] Top;
        private readonly bool RemoveDuplicates;
        private int NumTop;

        public MergedIterator(params IEnumerator<T>[] iterators) : this(true, iterators)
        {
        }

        public MergedIterator(bool removeDuplicates, params IEnumerator<T>[] iterators)
        {
          this.RemoveDuplicates = removeDuplicates;
          Queue = new TermMergeQueue<T>(iterators.Length);
          Top = new SubIterator[iterators.Length];
          int index = 0;
          foreach (IEnumerator<T> iterator in iterators)
          {
            if (iterator.HasNext())
            {
              SubIterator<T> sub = new SubIterator<T>();
              sub.Current = iterator.next();
              sub.Iterator = iterator;
              sub.Index = index++;
              Queue.Add(sub);
            }
          }
        }

        public override bool HasNext()
        {
          if (Queue.Size() > 0)
          {
            return true;
          }

          for (int i = 0; i < NumTop; i++)
          {
            if (Top[i].Iterator.hasNext())
            {
              return true;
            }
          }
          return false;
        }

        public override T Next()
        {
          // restore queue
          PushTop();

          // gather equal top elements
          if (Queue.Size() > 0)
          {
            PullTop();
          }
          else
          {
            Current = default(T);
          }
          if (Current == null)
          {
            throw new NoSuchElementException();
          }
          return Current;
        }

        public override void Remove()
        {
          throw new System.NotSupportedException();
        }

        private void PullTop()
        {
          Debug.Assert(NumTop == 0);
          Top[NumTop++] = Queue.Pop();
          if (RemoveDuplicates)
          {
            // extract all subs from the queue that have the same top element
            while (Queue.Size() != 0 && Queue.Top().Current.Equals(Top[0].Current))
            {
              Top[NumTop++] = Queue.Pop();
            }
          }
          Current = Top[0].Current;
        }

        private void PushTop()
        {
          // call next() on each top, and put back into queue
          for (int i = 0; i < NumTop; i++)
          {
            if (Top[i].Iterator.hasNext())
            {
              Top[i].Current = Top[i].Iterator.next();
              Queue.Add(Top[i]);
            }
            else
            {
              // no more elements
              Top[i].Current = default(T);
            }
          }
          NumTop = 0;
        }*/

        private class SubIterator<I>
            where I : IComparable<I>
        {
            internal IEnumerator<I> Iterator;
            internal I Current;
            internal int Index;
        }

        private class TermMergeQueue<C> : PriorityQueue<SubIterator<C>>
            where C : IComparable<C>
        {
            internal TermMergeQueue(int size)
                : base(size)
            {
            }

            protected internal override bool LessThan(SubIterator<C> a, SubIterator<C> b)
            {
                int cmp = a.Current.CompareTo(b.Current);
                if (cmp != 0)
                {
                    return cmp < 0;
                }
                else
                {
                    return a.Index < b.Index;
                }
            }
        }
    }
}