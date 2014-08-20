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
    using Lucene.Net.Support;
    /// <summary>
    /// <seealso cref="Sorter" /> implementation based on a variant of the quicksort algorithm
    /// called <a href="http://en.wikipedia.org/wiki/Introsort">introsort</a>: when
    /// the recursion level exceeds the log of the length of the array to sort, it
    /// falls back to heapsort. this prevents quicksort from running into its
    /// worst-case quadratic runtime. Small arrays are sorted with
    /// insertion sort.
    /// @lucene.internal
    /// </summary>
    public abstract class IntroSorter : Sorter
    {
        /// <summary>
        /// Performs Ceil(Log2(n)) calculation
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>System.Int32.</returns>
        internal static int CeilLog2(int number)
        {
            //8bits in a byte
            return sizeof(int) * 8 - (number -1).NumberOfLeadingZeros();
        }


        /// <summary>
        /// Sort a slice or range which begins at the <paramref name="start" /> index to the <paramref name="count" /> index.
        /// </summary>
        /// <param name="start">The position to start the slice.</param>
        /// <param name="count">The count or length of the slice.</param>
        public override sealed void Sort(int start, int count)
        {
            //CheckRange(from, to);
            this.QuickSort(start, count, CeilLog2(count - start));
        }

        /// <summary>
        /// Performs a Quick Sort.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="count">The count.</param>
        /// <param name="maxDepth">The maximum depth.</param>
        internal virtual void QuickSort(int start, int count, int maxDepth)
        {
            if (count - start < THRESHOLD)
            {
                this.InsertionSort(start, count);
                return;
            }
            
            if (--maxDepth < 0)
            {
                this.HeapSort(start, count);
                return;
            }

            var mid = (int)((uint)(start + count) >> 1);

            if (this.Compare(start, mid) > 0)
            {
                this.Swap(start, mid);
            }

            if (Compare(mid, count - 1) > 0)
            {
                this.Swap(mid, count - 1);
                if (this.Compare(start, mid) > 0)
                {
                    Swap(start, mid);
                }
            }

            int left = start + 1;
            int right = count - 2;

            Pivot = mid;
            for (; ; )
            {
                while (this.ComparePivot(right) < 0)
                {
                    --right;
                }

                while (left < right && ComparePivot(left) >= 0)
                {
                    ++left;
                }

                if (left < right)
                {
                    this.Swap(left, right);
                    --right;
                }
                else
                {
                    break;
                }
            }

            this.QuickSort(start, left + 1, maxDepth);
            this.QuickSort(left + 1, count, maxDepth);
        }

        /// <summary>
        /// Save the value at slot <code>i</code> so that it can later be used as a
        /// pivot, see <seealso cref="ComparePivot(int)"/>.
        /// </summary>
        protected internal abstract int Pivot { set; }

        /// <summary>
        /// Compare the pivot with the slot at <code>j</code>, similarly to
        ///  <seealso cref="Sorter.Compare(int, int)"/>.
        /// </summary>
        protected internal abstract int ComparePivot(int j);
    }
}