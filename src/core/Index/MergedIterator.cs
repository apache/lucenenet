using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal sealed class MergedIterator<T> : IEnumerator<T>
        where T : IComparable<T>
    {
        private T current;
        private readonly TermMergeQueue<T> queue;
        private readonly SubIterator<T>[] top;
        private int numTop;

        public MergedIterator(params IEnumerator<T>[] iterators)
        {
            queue = new TermMergeQueue<T>(iterators.Length);
            top = new SubIterator<T>[iterators.Length];
            int index = 0;
            foreach (IEnumerator<T> iterator in iterators)
            {
                if (iterator.MoveNext())
                {
                    SubIterator<T> sub = new SubIterator<T>();
                    sub.current = iterator.Current;
                    sub.iterator = iterator;
                    sub.index = index++;
                    queue.Add(sub);
                }
            }
        }

        private bool HasNext()
        {
            if (queue.Size > 0)
            {
                return true;
            }

            for (int i = 0; i < numTop; i++)
            {
                if (top[i].iterator.MoveNext())
                {
                    return true;
                }
            }
            return false;
        }

        public bool MoveNext()
        {
            if (!HasNext())
                return false;

            // restore queue
            PushTop();

            // gather equal top elements
            if (queue.Size > 0)
            {
                PullTop();
            }
            else
            {
                current = default(T);
            }

            if ((object)current == (object)default(T))
            {
                throw new InvalidOperationException();
            }
            return true;
        }

        public T Current
        {
            get { return current; }
        }

        public void Dispose()
        {
        }

        object System.Collections.IEnumerator.Current
        {
            get { return current; }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        private void PullTop()
        {
            // extract all subs from the queue that have the same top element
            //assert numTop == 0;
            while (true)
            {
                top[numTop++] = queue.Pop();
                if (queue.Size == 0
                    || !(queue.Top()).current.Equals(top[0].current))
                {
                    break;
                }
            }
            current = top[0].current;
        }

        private void PushTop()
        {
            // call next() on each top, and put back into queue
            for (int i = 0; i < numTop; i++)
            {
                if (top[i].iterator.MoveNext())
                {
                    top[i].current = top[i].iterator.Current;
                    queue.Add(top[i]);
                }
                else
                {
                    // no more elements
                    top[i].current = default(T);
                }
            }
            numTop = 0;
        }

        private class SubIterator<I>
            where I : IComparable<I>
        {
            internal IEnumerator<I> iterator;
            internal I current;
            internal int index;
        }

        private class TermMergeQueue<C> : PriorityQueue<SubIterator<C>>
            where C : IComparable<C>
        {
            public TermMergeQueue(int size)
                : base(size)
            {
            }

            public override bool LessThan(SubIterator<C> a, SubIterator<C> b)
            {
                int cmp = a.current.CompareTo(b.current);
                if (cmp != 0)
                {
                    return cmp < 0;
                }
                else
                {
                    return a.index < b.index;
                }
            }
        }
    }
}
