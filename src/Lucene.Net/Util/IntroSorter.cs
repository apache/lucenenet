using J2N.Numerics;
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
    /// <see cref="Sorter"/> implementation based on a variant of the quicksort algorithm
    /// called <a href="http://en.wikipedia.org/wiki/Introsort">introsort</a>: when
    /// the recursion level exceeds the log of the length of the array to sort, it
    /// falls back to heapsort. This prevents quicksort from running into its
    /// worst-case quadratic runtime. Small arrays are sorted with
    /// insertion sort.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class IntroSorter : Sorter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CeilLog2(int n)
        {
            //8bits in a byte
            return sizeof(int) * 8 - (n - 1).LeadingZeroCount();
        }

        /// <summary>
        /// Create a new <see cref="IntroSorter"/>. </summary>
        protected IntroSorter() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Sort the slice which starts at <paramref name="from"/> (inclusive) and ends at
        /// <paramref name="to"/> (exclusive).
        /// </summary>
        public override sealed void Sort(int from, int to)
        {
            CheckRange(from, to);
            Quicksort(from, to, CeilLog2(to - from));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Quicksort(int from, int to, int maxDepth)
        {
            if (to - from < THRESHOLD)
            {
                InsertionSort(from, to);
                return;
            }
            else if (--maxDepth < 0)
            {
                HeapSort(from, to);
                return;
            }

            int mid = (from + to).TripleShift(1);

            if (Compare(from, mid) > 0)
            {
                Swap(from, mid);
            }

            if (Compare(mid, to - 1) > 0)
            {
                Swap(mid, to - 1);
                if (Compare(from, mid) > 0)
                {
                    Swap(from, mid);
                }
            }

            int left = from + 1;
            int right = to - 2;

            SetPivot(mid);
            for (; ; )
            {
                while (ComparePivot(right) < 0)
                {
                    --right;
                }

                while (left < right && ComparePivot(left) >= 0)
                {
                    ++left;
                }

                if (left < right)
                {
                    Swap(left, right);
                    --right;
                }
                else
                {
                    break;
                }
            }

            Quicksort(from, left + 1, maxDepth);
            Quicksort(left + 1, to, maxDepth);
        }

        /// <summary>
        /// Save the value at slot <paramref name="i"/> so that it can later be used as a
        /// pivot, see <see cref="ComparePivot(int)"/>.
        /// </summary>
        protected abstract void SetPivot(int i);

        /// <summary>
        /// Compare the pivot with the slot at <paramref name="j"/>, similarly to
        /// Compare(i, j) (<see cref="Sorter.Compare(int, int)"/>).
        /// </summary>
        protected abstract int ComparePivot(int j);
    }
}