/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Support;
using System;

namespace Lucene.Net.Util
{

    /// <summary> Borrowed from Cglib. Allows custom swap so that two arrays can be sorted
    /// at the same time.
    /// </summary>
    public abstract class SorterTemplate
    {
        private const int TIMSORT_MINRUN = 32;
        private const int TIMSORT_THRESHOLD = 64;
        private const int TIMSORT_STACKSIZE = 40;
        private const int MERGESORT_THRESHOLD = 12;
        private const int QUICKSORT_THRESHOLD = 7;

        static SorterTemplate()
        {
            long[] lengths = new long[TIMSORT_STACKSIZE];
            lengths[0] = TIMSORT_MINRUN;
            lengths[1] = lengths[0] + 1;
            for (int i = 2; i < TIMSORT_STACKSIZE; ++i)
            {
                lengths[i] = lengths[i - 2] + lengths[i - 1] + 1;
            }
            if (lengths[TIMSORT_STACKSIZE - 1] < Int32.MaxValue)
            {
                throw new Exception("TIMSORT_STACKSIZE is too small");
            }
        }

        abstract protected internal void Swap(int i, int j);

        abstract protected internal int Compare(int i, int j);

        abstract protected internal void SetPivot(int i);

        abstract protected internal int ComparePivot(int j);

        public void InsertionSort(int lo, int hi)
        {
            for (int i = lo + 1; i <= hi; i++)
            {
                for (int j = i; j > lo; j--)
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

        public void BinarySort(int lo, int hi)
        {
            for (int i = lo + 1; i <= hi; ++i)
            {
                int l = lo;
                int h = i - 1;
                SetPivot(i);
                while (l <= h)
                {
                    int mid = Number.URShift((l + h), 1);
                    int cmp = ComparePivot(mid);
                    if (cmp < 0)
                    {
                        h = mid - 1;
                    }
                    else
                    {
                        l = mid + 1;
                    }
                }
                for (int j = i; j > l; --j)
                {
                    Swap(j - 1, j);
                }
            }
        }

        public void QuickSort(int lo, int hi)
        {
            if (hi <= lo) return;
            QuickSort(lo, hi, (32 - Number.NumberOfLeadingZeros(hi - lo)) << 1);
        }

        private void QuickSort(int lo, int hi, int maxDepth)
        {
            // fall back to insertion when array has short length
            int diff = hi - lo;
            if (diff <= QUICKSORT_THRESHOLD)
            {
                InsertionSort(lo, hi);
                return;
            }

            // fall back to merge sort when recursion depth gets too big
            if (--maxDepth == 0)
            {
                MergeSort(lo, hi);
                return;
            }

            int mid = lo + Number.URShift(diff, 1);

            if (Compare(lo, mid) > 0)
            {
                Swap(lo, mid);
            }

            if (Compare(mid, hi) > 0)
            {
                Swap(mid, hi);
                if (Compare(lo, mid) > 0)
                {
                    Swap(lo, mid);
                }
            }

            int left = lo + 1;
            int right = hi - 1;

            SetPivot(mid);
            for (; ; )
            {
                while (ComparePivot(right) < 0)
                    --right;

                while (left < right && ComparePivot(left) >= 0)
                    ++left;

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

            QuickSort(lo, left, maxDepth);
            QuickSort(left + 1, hi, maxDepth);
        }

        private class TimSort
        {
            readonly SorterTemplate parent;
            readonly int hi;
            readonly int minRun;
            readonly int[] runEnds;
            int stackSize;

            public TimSort(SorterTemplate parent, int lo, int hi)
            {
                if (hi <= lo)
                    throw new ArgumentOutOfRangeException();
                // +1 because the first slot is reserved and always lo
                runEnds = new int[TIMSORT_STACKSIZE + 1];
                runEnds[0] = lo;
                stackSize = 0;
                this.hi = hi;
                this.minRun = MinRun(hi - lo + 1);
                this.parent = parent;
            }

            int MinRun(int length)
            {
                if (length < TIMSORT_MINRUN)
                    throw new ArgumentOutOfRangeException("length");

                int n = length;
                int r = 0;
                while (n >= 64)
                {
                    r |= n & 1;
                    n = Number.URShift(n, 1);
                }
                int minRun = n + r;
                //assert minRun >= TIMSORT_MINRUN && minRun <= 64;
                return minRun;
            }

            int RunLen(int i)
            {
                int off = stackSize - i;
                return runEnds[off] - runEnds[off - 1];
            }

            int RunBase(int i)
            {
                return runEnds[stackSize - i - 1];
            }

            int RunEnd(int i)
            {
                return runEnds[stackSize - i];
            }

            void SetRunEnd(int i, int runEnd)
            {
                runEnds[stackSize - i] = runEnd;
            }

            void PushRunLen(int len)
            {
                runEnds[stackSize + 1] = runEnds[stackSize] + len;
                ++stackSize;
            }

            /** Merge run i with run i+1 */
            void MergeAt(int i)
            {
                //assert stackSize > i + 1;
                int l = RunBase(i + 1);
                int pivot = RunBase(i);
                int h = RunEnd(i);
                parent.RunMerge(l, pivot, h, pivot - l, h - pivot);
                for (int j = i + 1; j > 0; --j)
                {
                    SetRunEnd(j, RunEnd(j - 1));
                }
                --stackSize;
            }

            /** Compute the length of the next run, make the run sorted and return its
             *  length. */
            int NextRun()
            {
                int runBase = RunEnd(0);
                if (runBase == hi)
                {
                    return 1;
                }
                int l = 1; // length of the run
                if (parent.Compare(runBase, runBase + 1) > 0)
                {
                    // run must be strictly descending
                    while (runBase + l <= hi && parent.Compare(runBase + l - 1, runBase + l) > 0)
                    {
                        ++l;
                    }
                    if (l < minRun && runBase + l <= hi)
                    {
                        l = Math.Min(hi - runBase + 1, minRun);
                        parent.BinarySort(runBase, runBase + l - 1);
                    }
                    else
                    {
                        // revert
                        for (int i = 0, halfL = l / 2; i < halfL; ++i)
                        {
                            parent.Swap(runBase + i, runBase + l - i - 1);
                        }
                    }
                }
                else
                {
                    // run must be non-descending
                    while (runBase + l <= hi && parent.Compare(runBase + l - 1, runBase + l) <= 0)
                    {
                        ++l;
                    }
                    if (l < minRun && runBase + l <= hi)
                    {
                        l = Math.Min(hi - runBase + 1, minRun);
                        parent.BinarySort(runBase, runBase + l - 1);
                    } // else nothing to do, the run is already sorted
                }
                return l;
            }

            void EnsureInvariants()
            {
                while (stackSize > 1)
                {
                    int runLen0 = RunLen(0);
                    int runLen1 = RunLen(1);

                    if (stackSize > 2)
                    {
                        int runLen2 = RunLen(2);

                        if (runLen2 <= runLen1 + runLen0)
                        {
                            // merge the smaller of 0 and 2 with 1
                            if (runLen2 < runLen0)
                            {
                                MergeAt(1);
                            }
                            else
                            {
                                MergeAt(0);
                            }
                            continue;
                        }
                    }

                    if (runLen1 <= runLen0)
                    {
                        MergeAt(0);
                        continue;
                    }

                    break;
                }
            }

            void ExhaustStack()
            {
                while (stackSize > 1)
                {
                    MergeAt(0);
                }
            }

            public void Sort()
            {
                do
                {
                    EnsureInvariants();

                    // Push a new run onto the stack
                    PushRunLen(NextRun());

                } while (RunEnd(0) <= hi);

                ExhaustStack();
                //assert runEnd(0) == hi + 1;
            }
        }

        public void TimSort(int lo, int hi)
        {
            if (hi - lo <= TIMSORT_THRESHOLD)
            {
                BinarySort(lo, hi);
                return;
            }

            new TimSort(this, lo, hi).Sort();
        }

        public void MergeSort(int lo, int hi)
        {
            int diff = hi - lo;
            if (diff <= MERGESORT_THRESHOLD)
            {
                InsertionSort(lo, hi);
                return;
            }
            int mid = lo + diff / 2;
            MergeSort(lo, mid);
            MergeSort(mid, hi);
            RunMerge(lo, mid, hi, mid - lo, hi - mid);
        }

        private void RunMerge(int lo, int pivot, int hi, int len1, int len2)
        {
            if (len1 == 0 || len2 == 0)
            {
                return;
            }

            SetPivot(pivot - 1);
            if (ComparePivot(pivot) <= 0)
            {
                // all values from the first run are below all values from the 2nd run
                // this shortcut makes mergeSort run in linear time on sorted arrays
                return;
            }
            while (ComparePivot(hi - 1) <= 0)
            {
                --hi;
                --len2;
            }
            SetPivot(pivot);
            while (ComparePivot(lo) >= 0)
            {
                ++lo;
                --len1;
            }
            if (len1 + len2 == 2)
            {
                //assert len1 == len2;
                //assert compare(lo, pivot) > 0;
                Swap(pivot, lo);
                return;
            }
            Merge(lo, pivot, hi, len1, len2);
        }

        protected void Merge(int lo, int pivot, int hi, int len1, int len2)
        {
            int first_cut, second_cut;
            int len11, len22;
            if (len1 > len2)
            {
                len11 = len1 / 2;
                first_cut = lo + len11;
                second_cut = Lower(pivot, hi, first_cut);
                len22 = second_cut - pivot;
            }
            else
            {
                len22 = len2 / 2;
                second_cut = pivot + len22;
                first_cut = Upper(lo, pivot, second_cut);
                len11 = first_cut - lo;
            }
            Rotate(first_cut, pivot, second_cut);
            int new_mid = first_cut + len22;
            RunMerge(lo, first_cut, new_mid, len11, len22);
            RunMerge(new_mid, second_cut, hi, len1 - len11, len2 - len22);
        }

        private void Rotate(int lo, int mid, int hi)
        {
            int lot = lo;
            int hit = mid - 1;
            while (lot < hit)
            {
                Swap(lot++, hit--);
            }
            lot = mid; hit = hi - 1;
            while (lot < hit)
            {
                Swap(lot++, hit--);
            }
            lot = lo; hit = hi - 1;
            while (lot < hit)
            {
                Swap(lot++, hit--);
            }
        }

        private int Lower(int lo, int hi, int val)
        {
            int len = hi - lo;
            while (len > 0)
            {
                int half = len / 2;
                int mid = lo + half;
                if (Compare(mid, val) < 0)
                {
                    lo = mid + 1;
                    len = len - half - 1;
                }
                else
                {
                    len = half;
                }
            }
            return lo;
        }

        private int Upper(int lo, int hi, int val)
        {
            int len = hi - lo;
            while (len > 0)
            {
                int half = len / 2;
                int mid = lo + half;
                if (Compare(val, mid) < 0)
                {
                    len = half;
                }
                else
                {
                    lo = mid + 1;
                    len = len - half - 1;
                }
            }
            return lo;
        }
    }
}