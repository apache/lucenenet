using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;
using System.Runtime.CompilerServices;

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
    /// Base class for sorting algorithms implementations.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class Sorter
    {
        internal const int THRESHOLD = 20;

        /// <summary>
        /// Sole constructor, used for inheritance. </summary>
        protected Sorter()
        {
        }

        /// <summary>
        /// Compare entries found in slots <paramref name="i"/> and <paramref name="j"/>.
        /// The contract for the returned value is the same as
        /// <see cref="System.Collections.Generic.IComparer{T}.Compare(T, T)"/>.
        /// </summary>
        protected abstract int Compare(int i, int j);

        /// <summary>
        /// Swap values at slots <paramref name="i"/> and <paramref name="j"/>. </summary>
        protected abstract void Swap(int i, int j);

        /// <summary>
        /// Sort the slice which starts at <paramref name="from"/> (inclusive) and ends at
        /// <paramref name="to"/> (exclusive).
        /// </summary>
        public abstract void Sort(int from, int to);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void CheckRange(int from, int to)
        {
            if (to < from)
            {
                throw new ArgumentException("'to' must be >= 'from', got from=" + from + " and to=" + to);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void MergeInPlace(int from, int mid, int to)
        {
            if (from == mid || mid == to || Compare(mid - 1, mid) <= 0)
            {
                return;
            }
            else if (to - from == 2)
            {
                Swap(mid - 1, mid);
                return;
            }
            while (Compare(from, mid) <= 0)
            {
                ++from;
            }
            while (Compare(mid - 1, to - 1) <= 0)
            {
                --to;
            }
            int first_cut, second_cut;
            int len11, len22;
            if (mid - from > to - mid)
            {
                len11 = (mid - from).TripleShift(1);
                first_cut = from + len11;
                second_cut = Lower(mid, to, first_cut);
                len22 = second_cut - mid;
            }
            else
            {
                len22 = (to - mid).TripleShift(1);
                second_cut = mid + len22;
                first_cut = Upper(from, mid, second_cut);
                //len11 = first_cut - from; // LUCENENET: Unnecessary assignment
            }
            Rotate(first_cut, mid, second_cut);
            int new_mid = first_cut + len22;
            MergeInPlace(from, first_cut, new_mid);
            MergeInPlace(new_mid, second_cut, to);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int Lower(int from, int to, int val)
        {
            int len = to - from;
            while (len > 0)
            {
                int half = len.TripleShift(1);
                int mid = from + half;
                if (Compare(mid, val) < 0)
                {
                    from = mid + 1;
                    len = len - half - 1;
                }
                else
                {
                    len = half;
                }
            }
            return from;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int Upper(int from, int to, int val)
        {
            int len = to - from;
            while (len > 0)
            {
                int half = len.TripleShift(1);
                int mid = from + half;
                if (Compare(val, mid) < 0)
                {
                    len = half;
                }
                else
                {
                    from = mid + 1;
                    len = len - half - 1;
                }
            }
            return from;
        }

        // faster than lower when val is at the end of [from:to[
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int Lower2(int from, int to, int val)
        {
            int f = to - 1, t = to;
            while (f > from)
            {
                if (Compare(f, val) < 0)
                {
                    return Lower(f, t, val);
                }
                int delta = t - f;
                t = f;
                f -= delta << 1;
            }
            return Lower(from, t, val);
        }

        // faster than upper when val is at the beginning of [from:to[
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int Upper2(int from, int to, int val)
        {
            int f = from, t = f + 1;
            while (t < to)
            {
                if (Compare(t, val) > 0)
                {
                    return Upper(f, t, val);
                }
                int delta = t - f;
                f = t;
                t += delta << 1;
            }
            return Upper(f, to, val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reverse(int from, int to)
        {
            for (--to; from < to; ++from, --to)
            {
                Swap(from, to);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Rotate(int lo, int mid, int hi)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(lo <= mid && mid <= hi);
            if (lo == mid || mid == hi)
            {
                return;
            }
            DoRotate(lo, mid, hi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void DoRotate(int lo, int mid, int hi)
        {
            if (mid - lo == hi - mid)
            {
                // happens rarely but saves n/2 swaps
                while (mid < hi)
                {
                    Swap(lo++, mid++);
                }
            }
            else
            {
                Reverse(lo, mid);
                Reverse(mid, hi);
                Reverse(lo, hi);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void InsertionSort(int from, int to)
        {
            for (int i = from + 1; i < to; ++i)
            {
                for (int j = i; j > from; --j)
                {
                    if (Compare(j - 1, j) > 0)
                    {
                        Swap(j - 1, j);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void BinarySort(int from, int to)
        {
            BinarySort(from, to, from + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void BinarySort(int from, int to, int i)
        {
            for (; i < to; ++i)
            {
                int l = from;
                int h = i - 1;
                while (l <= h)
                {
                    int mid = (l + h).TripleShift(1);
                    int cmp = Compare(i, mid);
                    if (cmp < 0)
                    {
                        h = mid - 1;
                    }
                    else
                    {
                        l = mid + 1;
                    }
                }
                switch (i - l)
                {
                    case 2:
                        Swap(l + 1, l + 2);
                        Swap(l, l + 1);
                        break;

                    case 1:
                        Swap(l, l + 1);
                        break;

                    case 0:
                        break;

                    default:
                        for (int j = i; j > l; --j)
                        {
                            Swap(j - 1, j);
                        }
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void HeapSort(int from, int to)
        {
            if (to - from <= 1)
            {
                return;
            }
            Heapify(from, to);
            for (int end = to - 1; end > from; --end)
            {
                Swap(from, end);
                SiftDown(from, from, end);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Heapify(int from, int to)
        {
            for (int i = HeapParent(from, to - 1); i >= from; --i)
            {
                SiftDown(i, from, to);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void SiftDown(int i, int from, int to)
        {
            for (int leftChild = HeapChild(from, i); leftChild < to; leftChild = HeapChild(from, i))
            {
                int rightChild = leftChild + 1;
                if (Compare(i, leftChild) < 0)
                {
                    if (rightChild < to && Compare(leftChild, rightChild) < 0)
                    {
                        Swap(i, rightChild);
                        i = rightChild;
                    }
                    else
                    {
                        Swap(i, leftChild);
                        i = leftChild;
                    }
                }
                else if (rightChild < to && Compare(i, rightChild) < 0)
                {
                    Swap(i, rightChild);
                    i = rightChild;
                }
                else
                {
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int HeapParent(int from, int i)
        {
            return ((i - 1 - from).TripleShift(1)) + from;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int HeapChild(int from, int i)
        {
            return ((i - from) << 1) + 1 + from;
        }
    }
}