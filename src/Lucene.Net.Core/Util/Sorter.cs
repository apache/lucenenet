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


namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;



    /// <summary>
    /// 
    /// </summary>
    public abstract class Sorter : IComparer<int>
    {
        protected static readonly int THRESHOLD = 20;


        protected Sorter() { }

        /// <summary>
        /// Sort a slice or range which begins at the <paramref name="start"/> index to the <paramref name="end"/> index.
        /// </summary>
        /// <param name="start">The position to start the slice.</param>
        /// <param name="end">The position to end the slice. </param>
        /// <exception cref="IndexOutOfRangeException">Throws when start is greater or equal the length or when the start + count </exception>
        public abstract void SortSlice(int start, int end);


        /// <summary>
        /// Performs a comparison of two integers and returns a value whether the values are equal to, less than, or greater than
        /// the other value.
        /// </summary>
        /// <param name="x">The left value</param>
        /// <param name="y">The right value.</param>
        /// <returns>Returns 0 if the values are equal, -1 if the value x is less than y, and 1 if the value x is greater than y.</returns>
        protected abstract int Compare(int x, int y);

        /// <summary>
        /// Switchs the index position of the values. 
        /// </summary>
        /// <param name="x">The left value.</param>
        /// <param name="y">The right value.</param>
        protected abstract void Swap(int x, int y);

        /// <summary>
        /// Throws an exception when start is greater than end.
        /// </summary>
        /// <param name="start">The start index position.</param>
        /// <param name="end">the end index position.</param>
        protected void CheckSlice(int start, int end)
        {
            if(start > end)
            {
                string message = string.Format("The start parameter must be less than the end parameter." +
                    " start was {0} and end was {1}", start, end);

                throw new ArgumentException(message);
            }
        }


        protected void MergeInPlace(int start, int middle, int end)
        {
            if (start == middle || middle == end || this.Compare(middle - 1, middle) <= 0)
            {
                return;
            }
            else if (end - start == 2)
            {
                this.Swap(middle - 1, middle);
                return;
            }
            while (this.Compare(start, middle) <= 0)
            {
                ++start;
            }
            while (this.Compare(middle - 1, end - 1) <= 0)
            {
                --end;
            }

            int firstCut, secondCut;
            int len11, len22;

            if (middle - start > end - middle)
            {
                len11 = (middle - start) >> 1;
                firstCut = start + len11;
                secondCut = this.Lower(middle, end, firstCut);
                len22 = secondCut - middle;
            }
            else
            {
                len22 = (end - middle) >> 1;
                secondCut = middle + len22;
                firstCut = this.Upper(start, middle, secondCut);
                len11 = firstCut - start;
            }

            this.Rotate(firstCut, middle, secondCut);

            var newMiddle = firstCut + len22;
            this.MergeInPlace(start, firstCut, newMiddle);
            this.MergeInPlace(newMiddle, secondCut, end);
        }


        protected int Lower(int start, int end, int value)
        {
            int len = end - start;
            while (len > 0)
            {
                int half = len >> 1;
                int middle = start + half;
                if (this.Compare(middle, value) < 0)
                {
                    start = middle + 1;
                    len = len - half - 1;
                }
                else
                {
                    len = half;
                }
            }
            return start;
        }

        protected int Upper(int start, int end, int value)
        {
            int len = end - start;
            while (len > 0)
            {
                int half = len >> 1;
                int middle = start + half;
                if (this.Compare(value, middle) < 0)
                {
                    len = half;
                }
                else
                {
                    start = middle + 1;
                    len = len - half - 1;
                }
            }
            return start;
        }

        // faster than lower when val is at the end of [from:to[
        protected int LowerFromReverse(int start, int end, int value)
        {
            int f = end - 1, t = end;
            while (f > start)
            {
                if (this.Compare(f, value) < 0)
                {
                    return this.Lower(f, t, value);
                }

                int delta = t - f;
                t = f;
                f -= delta << 1;
            }
            return this.Lower(start, t, value);
        }

        // faster than upper when val is at the beginning of [from:to[
        public int UpperFromReverse(int start, int end, int value)
        {
            int f = start, t = f + 1;
            while (t < end)
            {
                if (this.Compare(t, value) > 0)
                {
                    return this.Upper(f, t, value);
                }

                int delta = t - f;
                f = t;
                t += delta << 1;
            }
            return this.Upper(f, end, value);
        }

        protected void Reverse(int start, int end)
        {
            for (--end; start < end; ++start, --end)
            {
                this.Swap(start, end);
            }
        }

        protected void Rotate(int start, int middle, int end)
        {
            Debug.Assert(start <= middle && middle <= end);
            if (start == middle || middle == end)
            {
                return;
            }
            this.DoRotate(start, middle, end);
        }

        void DoRotate(int start, int middle, int end)
        {
            if (middle - start == end - middle)
            {
                // happens rarely but saves n/2 swaps
                while (middle < end)
                {
                   this.Swap(start++, middle++);
                }
            }
            else
            {
                this.Reverse(start, middle);
                this.Reverse(middle, end);
                this.Reverse(start, end);
            }
        }

        protected void InsertionSort(int start, int end)
        {
            for (int i = start + 1; i < end; ++i)
            {
                for (int j = i; j > start; --j)
                {
                    if (this.Compare(j - 1, j) > 0)
                    {
                        this.Swap(j - 1, j);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        void BinarySort(int start, int end)
        {
            this.BinarySort(start, end, start + 1);
        }

        void BinarySort(int start, int end, int i)
        {
            for (; i < end; ++i)
            {
                int l = start;
                int h = i - 1;
                while (l <= h)
                {
                    int mid = (l + h) >> 1;
                    int cmp = this.Compare(i, mid);
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
                        this.Swap(l + 1, l + 2);
                        this.Swap(l, l + 1);
                        break;
                    case 1:
                        this.Swap(l, l + 1);
                        break;
                    case 0:
                        break;
                    default:
                        for (int j = i; j > l; --j)
                        {
                            this.Swap(j - 1, j);
                        }
                        break;
                }
            }
        }

        void HeapSort(int from, int to)
        {
            if (to - from <= 1)
            {
                return;
            }

            this.Heapify(from, to);

            for (int end = to - 1; end > from; --end)
            {
                this.Swap(from, end);
                this.SiftDown(from, from, end);
            }
        }

        void Heapify(int from, int to)
        {
            for (int i = HeapParent(from, to - 1); i >= from; --i)
            {
                SiftDown(i, from, to);
            }
        }

        void SiftDown(int i, int from, int to)
        {
            for (int leftChild = HeapChild(from, i); leftChild < to; leftChild = HeapChild(from, i))
            {
                int rightChild = leftChild + 1;
                if (this.Compare(i, leftChild) < 0)
                {
                    if (rightChild < to && this.Compare(leftChild, rightChild) < 0)
                    {
                        this.Swap(i, rightChild);
                        i = rightChild;
                    }
                    else
                    {
                        this.Swap(i, leftChild);
                        i = leftChild;
                    }
                }
                else if (rightChild < to && this.Compare(i, rightChild) < 0)
                {
                    this.Swap(i, rightChild);
                    i = rightChild;
                }
                else
                {
                    break;
                }
            }
        }

        static int HeapParent(int start, int i)
        {
            return ((i - 1 - start) >> 1) + start;
        }

        static int HeapChild(int from, int i)
        {
            return ((i - from) << 1) + 1 + from;
        }

        #region IComparer<int>

        int IComparer<int>.Compare(int x, int y)
        {
            return this.Compare(x, y);
        }

        #endregion
    }
}
