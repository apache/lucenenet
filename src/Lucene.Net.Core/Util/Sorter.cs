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
        /// Sort a slice or range which begins at the <paramref name="start"/> index to the <paramref name="count"/> index.
        /// </summary>
        /// <param name="start">The position to start the slice.</param>
        /// <param name="count">The count or length of the slice. </param>
        /// <exception cref="IndexOutOfRangeException">Throws when start is greater or equal the length or when the start + count </exception>
        public abstract void SortRange(int start, int count);


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
        /// <param name="count">the end index position.</param>
        protected void CheckSlice(int start, int count)
        {
            if(start > count)
            {
                string message = string.Format("The start parameter must be less than the end parameter." +
                    " start was {0} and count was {1}", start, count);

                throw new ArgumentException(message);
            }
        }


        protected void MergeInPlace(int start, int middle, int count)
        {
            if (start == middle || middle == count || this.Compare(middle - 1, middle) <= 0)
            {
                return;
            }
            else if (count - start == 2)
            {
                this.Swap(middle - 1, middle);
                return;
            }
            while (this.Compare(start, middle) <= 0)
            {
                ++start;
            }
            while (this.Compare(middle - 1, count - 1) <= 0)
            {
                --count;
            }

            int firstCut, secondCut;
            int len11, len22;

            if (middle - start > count - middle)
            {
                len11 = (middle - start) >> 1;
                firstCut = start + len11;
                secondCut = this.Lower(middle, count, firstCut);
                len22 = secondCut - middle;
            }
            else
            {
                len22 = (count - middle) >> 1;
                secondCut = middle + len22;
                firstCut = this.Upper(start, middle, secondCut);
                len11 = firstCut - start;
            }

            this.Rotate(firstCut, middle, secondCut);

            var newMiddle = firstCut + len22;
            this.MergeInPlace(start, firstCut, newMiddle);
            this.MergeInPlace(newMiddle, secondCut, count);
        }


        protected int Lower(int start, int count, int value)
        {
            int len = count - start;
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

        protected int Upper(int start, int count, int value)
        {
            int len = count - start;
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
        protected int LowerFromReverse(int start, int count, int value)
        {
            int f = count - 1, t = count;
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
        public int UpperFromReverse(int start, int count, int value)
        {
            int f = start, t = f + 1;
            while (t < count)
            {
                if (this.Compare(t, value) > 0)
                {
                    return this.Upper(f, t, value);
                }

                int delta = t - f;
                f = t;
                t += delta << 1;
            }
            return this.Upper(f, count, value);
        }

        protected void Reverse(int start, int count)
        {
            for (--count; start < count; ++start, --count)
            {
                this.Swap(start, count);
            }
        }

        protected void Rotate(int start, int middle, int count)
        {
            Debug.Assert(start <= middle && middle <= count);
            if (start == middle || middle == count)
            {
                return;
            }
            this.DoRotate(start, middle, count);
        }

        void DoRotate(int start, int middle, int count)
        {
            if (middle - start == count - middle)
            {
                // happens rarely but saves n/2 swaps
                while (middle < count)
                {
                   this.Swap(start++, middle++);
                }
            }
            else
            {
                this.Reverse(start, middle);
                this.Reverse(middle, count);
                this.Reverse(start, count);
            }
        }

        protected void InsertionSort(int start, int count)
        {
            for (int i = start + 1; i < count; ++i)
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

        void BinarySort(int start, int count)
        {
            this.BinarySort(start, count, start + 1);
        }

        void BinarySort(int start, int count, int i)
        {
            for (; i < count; ++i)
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

        void HeapSort(int start, int count)
        {
            if (count - start <= 1)
            {
                return;
            }

            this.Heapify(start, count);

            for (int end = count - 1; end > start; --end)
            {
                this.Swap(start, end);
                this.SiftDown(start, start, end);
            }
        }

        void Heapify(int start, int count)
        {
            for (int i = HeapParent(start, count - 1); i >= start; --i)
            {
                SiftDown(i, start, count);
            }
        }

        void SiftDown(int i, int start, int count)
        {
            for (int leftChild = HeapChild(start, i); leftChild < count; leftChild = HeapChild(start, i))
            {
                int rightChild = leftChild + 1;
                if (this.Compare(i, leftChild) < 0)
                {
                    if (rightChild < count && this.Compare(leftChild, rightChild) < 0)
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
                else if (rightChild < count && this.Compare(i, rightChild) < 0)
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

        static int HeapChild(int start, int i)
        {
            return ((i - start) << 1) + 1 + start;
        }

        #region IComparer<int>

        int IComparer<int>.Compare(int x, int y)
        {
            return this.Compare(x, y);
        }

        #endregion
    }
}
