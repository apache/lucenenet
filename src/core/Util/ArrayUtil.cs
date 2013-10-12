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

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Util
{

    /// <summary> Methods for manipulating arrays.</summary>
    public static class ArrayUtil
    {
        internal const float MERGE_OVERHEAD_RATIO = 0.01f;

        internal const int MERGE_EXTRA_MEMORY_THRESHOLD = (int)(15 / MERGE_OVERHEAD_RATIO);

        /*
        Begin Apache Harmony code
		
        Revision taken on Friday, June 12. https://svn.apache.org/repos/asf/harmony/enhanced/classlib/archive/java6/modules/luni/src/main/java/java/lang/Integer.java
		
        */

        /// <summary> Parses the string argument as if it was an int value and returns the
        /// result. Throws NumberFormatException if the string does not represent an
        /// int quantity.
        /// 
        /// </summary>
        /// <param name="chars">a string representation of an int quantity.
        /// </param>
        /// <returns> int the value represented by the argument
        /// </returns>
        /// <throws>  NumberFormatException if the argument could not be parsed as an int quantity. </throws>
        public static int ParseInt(char[] chars)
        {
            return ParseInt(chars, 0, chars.Length, 10);
        }

        /// <summary> Parses a char array into an int.</summary>
        /// <param name="chars">the character array
        /// </param>
        /// <param name="offset">The offset into the array
        /// </param>
        /// <param name="len">The length
        /// </param>
        /// <returns> the int
        /// </returns>
        /// <throws>  NumberFormatException if it can't parse </throws>
        public static int ParseInt(char[] chars, int offset, int len)
        {
            return ParseInt(chars, offset, len, 10);
        }

        /// <summary> Parses the string argument as if it was an int value and returns the
        /// result. Throws NumberFormatException if the string does not represent an
        /// int quantity. The second argument specifies the radix to use when parsing
        /// the value.
        /// 
        /// </summary>
        /// <param name="chars">a string representation of an int quantity.
        /// </param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <param name="radix">the base to use for conversion.
        /// </param>
        /// <returns> int the value represented by the argument
        /// </returns>
        /// <throws>  NumberFormatException if the argument could not be parsed as an int quantity. </throws>
        public static int ParseInt(char[] chars, int offset, int len, int radix)
        {
            if (chars == null || radix < 2 || radix > 36)
            {
                throw new System.FormatException();
            }
            int i = 0;
            if (len == 0)
            {
                throw new System.FormatException("chars length is 0");
            }
            bool negative = chars[offset + i] == '-';
            if (negative && ++i == len)
            {
                throw new System.FormatException("can't convert to an int");
            }
            if (negative == true)
            {
                offset++;
                len--;
            }
            return Parse(chars, offset, len, radix, negative);
        }


        private static int Parse(char[] chars, int offset, int len, int radix, bool negative)
        {
            int max = System.Int32.MinValue / radix;
            int result = 0;
            for (int i = 0; i < len; i++)
            {
                int digit = (int)System.Char.GetNumericValue(chars[i + offset]);
                if (digit == -1)
                {
                    throw new System.FormatException("Unable to parse");
                }
                if (max > result)
                {
                    throw new System.FormatException("Unable to parse");
                }
                int next = result * radix - digit;
                if (next > result)
                {
                    throw new System.FormatException("Unable to parse");
                }
                result = next;
            }
            /*while (offset < len) {
			
            }*/
            if (!negative)
            {
                result = -result;
                if (result < 0)
                {
                    throw new System.FormatException("Unable to parse");
                }
            }
            return result;
        }


        /*
		
        END APACHE HARMONY CODE
        */

        public static int Oversize(int minTargetSize, int bytesPerElement)
        {
            if (minTargetSize < 0)
            {
                // catch usage that accidentally overflows int
                throw new ArgumentException("invalid array size " + minTargetSize);
            }

            if (minTargetSize == 0)
            {
                // wait until at least one element is requested
                return 0;
            }

            // asymptotic exponential growth by 1/8th, favors
            // spending a bit more CPU to not tie up too much wasted
            // RAM:
            int extra = minTargetSize >> 3;

            if (extra < 3)
            {
                // for very small arrays, where constant overhead of
                // realloc is presumably relatively high, we grow
                // faster
                extra = 3;
            }

            int newSize = minTargetSize + extra;

            // add 7 to allow for worst case byte alignment addition below:
            if (newSize + 7 < 0)
            {
                // int overflowed -- return max allowed array size
                return Int32.MaxValue;
            }

            if (Constants.JRE_IS_64BIT)
            {
                // round up to 8 byte alignment in 64bit env
                switch (bytesPerElement)
                {
                    case 4:
                        // round up to multiple of 2
                        return (newSize + 1) & 0x7ffffffe;
                    case 2:
                        // round up to multiple of 4
                        return (newSize + 3) & 0x7ffffffc;
                    case 1:
                        // round up to multiple of 8
                        return (newSize + 7) & 0x7ffffff8;
                    case 8:
                    // no rounding
                    default:
                        // odd (invalid?) size
                        return newSize;
                }
            }
            else
            {
                // round up to 4 byte alignment in 64bit env
                switch (bytesPerElement)
                {
                    case 2:
                        // round up to multiple of 2
                        return (newSize + 1) & 0x7ffffffe;
                    case 1:
                        // round up to multiple of 4
                        return (newSize + 3) & 0x7ffffffc;
                    case 4:
                    case 8:
                    // no rounding
                    default:
                        // odd (invalid?) size
                        return newSize;
                }
            }
        }

        public static int GetShrinkSize(int currentSize, int targetSize, int bytesPerElement)
        {
            int newSize = Oversize(targetSize, bytesPerElement);
            // Only reallocate if we are "substantially" smaller.
            // This saves us from "running hot" (constantly making a
            // bit bigger then a bit smaller, over and over):
            if (newSize < currentSize / 2)
                return newSize;
            else
                return currentSize;
        }

        public static short[] Grow(short[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                short[] newArray = new short[Oversize(minSize, RamUsageEstimator.NUM_BYTES_SHORT)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static short[] Grow(short[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static float[] Grow(float[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                float[] newArray = new float[Oversize(minSize, RamUsageEstimator.NUM_BYTES_FLOAT)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static float[] Grow(float[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static double[] Grow(double[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                double[] newArray = new double[Oversize(minSize, RamUsageEstimator.NUM_BYTES_DOUBLE)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static double[] Grow(double[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static short[] Shrink(short[] array, int targetSize)
        {
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_SHORT);
            if (newSize != array.Length)
            {
                short[] newArray = new short[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
                return array;
        }

        public static int[] Grow(int[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                int[] newArray = new int[Oversize(minSize, RamUsageEstimator.NUM_BYTES_INT)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static int[] Grow(int[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static int[] Shrink(int[] array, int targetSize)
        {
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_INT);
            if (newSize != array.Length)
            {
                int[] newArray = new int[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
                return array;
        }

        public static long[] Grow(long[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                long[] newArray = new long[Oversize(minSize, RamUsageEstimator.NUM_BYTES_LONG)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static long[] Grow(long[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static long[] Shrink(long[] array, int targetSize)
        {
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_LONG);
            if (newSize != array.Length)
            {
                long[] newArray = new long[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
                return array;
        }

        public static sbyte[] Grow(sbyte[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                sbyte[] newArray = new sbyte[Oversize(minSize, 1)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static sbyte[] Grow(sbyte[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static byte[] Grow(byte[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                byte[] newArray = new byte[Oversize(minSize, 1)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static byte[] Grow(byte[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static sbyte[] Shrink(sbyte[] array, int targetSize)
        {
            int newSize = GetShrinkSize(array.Length, targetSize, 1);
            if (newSize != array.Length)
            {
                sbyte[] newArray = new sbyte[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
                return array;
        }

        public static byte[] Shrink(byte[] array, int targetSize)
        {
            int newSize = GetShrinkSize(array.Length, targetSize, 1);
            if (newSize != array.Length)
            {
                byte[] newArray = new byte[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
                return array;
        }

        public static bool[] Grow(bool[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                bool[] newArray = new bool[Oversize(minSize, 1)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static bool[] Grow(bool[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static bool[] Shrink(bool[] array, int targetSize)
        {
            int newSize = GetShrinkSize(array.Length, targetSize, 1);
            if (newSize != array.Length)
            {
                bool[] newArray = new bool[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
                return array;
        }

        public static char[] Grow(char[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                char[] newArray = new char[Oversize(minSize, RamUsageEstimator.NUM_BYTES_CHAR)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static char[] Grow(char[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static char[] Shrink(char[] array, int targetSize)
        {
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_CHAR);
            if (newSize != array.Length)
            {
                char[] newArray = new char[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
                return array;
        }

        public static int[][] Grow(int[][] array, int minSize)
        {
            if (array.Length < minSize)
            {
                int[][] newArray = new int[Oversize(minSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static int[][] Grow(int[][] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static int[][] Shrink(int[][] array, int targetSize)
        {
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
            if (newSize != array.Length)
            {
                int[][] newArray = new int[newSize][];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
                return array;
        }

        public static float[][] Grow(float[][] array, int minSize)
        {
            if (array.Length < minSize)
            {
                float[][] newArray = new float[Oversize(minSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
                return array;
        }

        public static float[][] Grow(float[][] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static float[][] Shrink(float[][] array, int targetSize)
        {
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
            if (newSize != array.Length)
            {
                float[][] newArray = new float[newSize][];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
                return array;
        }

        /// <summary> Returns hash of chars in range start (inclusive) to
        /// end (inclusive)
        /// </summary>
        public static int HashCode(char[] array, int start, int end)
        {
            int code = 0;
            for (int i = end - 1; i >= start; i--)
                code = code * 31 + array[i];
            return code;
        }

        /// <summary> Returns hash of chars in range start (inclusive) to
        /// end (inclusive)
        /// </summary>
        public static int HashCode(sbyte[] array, int start, int end)
        {
            int code = 0;
            for (int i = end - 1; i >= start; i--)
                code = code * 31 + array[i];
            return code;
        }

        public static bool Equals(char[] left, int offsetLeft, char[] right, int offsetRight, int length)
        {
            if ((offsetLeft + length <= left.Length) && (offsetRight + length <= right.Length))
            {
                for (int i = 0; i < length; i++)
                {
                    if (left[offsetLeft + i] != right[offsetRight + i])
                    {
                        return false;
                    }

                }
                return true;
            }
            return false;
        }

        public static bool Equals(sbyte[] left, int offsetLeft, sbyte[] right, int offsetRight, int length)
        {
            if ((offsetLeft + length <= left.Length) && (offsetRight + length <= right.Length))
            {
                for (int i = 0; i < length; i++)
                {
                    if (left[offsetLeft + i] != right[offsetRight + i])
                    {
                        return false;
                    }

                }
                return true;
            }
            return false;
        }

        public static bool Equals(int[] left, int offsetLeft, int[] right, int offsetRight, int length)
        {
            if ((offsetLeft + length <= left.Length) && (offsetRight + length <= right.Length))
            {
                for (int i = 0; i < length; i++)
                {
                    if (left[offsetLeft + i] != right[offsetRight + i])
                    {
                        return false;
                    }

                }
                return true;
            }
            return false;
        }

        public static int[] ToIntArray(ICollection<int> ints)
        {
            int[] result = new int[ints.Count];
            int upto = 0;
            foreach (int v in ints)
            {
                result[upto++] = v;
            }

            // paranoia:
            Trace.Assert(upto == result.Length);

            return result;
        }

        private abstract class ArraySorterTemplate<T> : SorterTemplate
        {
            protected readonly T[] a;

            public ArraySorterTemplate(T[] a)
            {
                this.a = a;
            }

            protected abstract int Compare(T a, T b);

            protected internal override void Swap(int i, int j)
            {
                T o = a[i];
                a[i] = a[j];
                a[j] = o;
            }

            protected internal override int Compare(int i, int j)
            {
                return Compare(a[i], a[j]);
            }

            protected internal override void SetPivot(int i)
            {
                pivot = a[i];
            }

            protected internal override int ComparePivot(int j)
            {
                return Compare(pivot, a[j]);
            }

            private T pivot;
        }

        private abstract class ArrayMergeSorterTemplate<T> : ArraySorterTemplate<T>
        {
            private readonly int threshold;
            private readonly T[] tmp;

            public ArrayMergeSorterTemplate(T[] a, float overheadRatio)
                : base(a)
            {
                this.threshold = (int)(a.Length * overheadRatio);
                // TODO: java has @SuppressWarnings("unchecked") here. should any of this be in an unchecked block?
                T[] tmpBuf = new T[threshold];
                this.tmp = tmpBuf;
            }

            private void MergeWithExtraMemory(int lo, int pivot, int hi, int len1, int len2)
            {
                Array.Copy(a, lo, tmp, 0, len1);
                int i = 0, j = pivot, dest = lo;
                while (i < len1 && j < hi)
                {
                    if (Compare(tmp[i], a[j]) <= 0)
                    {
                        a[dest++] = tmp[i++];
                    }
                    else
                    {
                        a[dest++] = a[j++];
                    }
                }
                while (i < len1)
                {
                    a[dest++] = tmp[i++];
                }
                Trace.Assert(j == dest);
            }

            protected override void Merge(int lo, int pivot, int hi, int len1, int len2)
            {
                if (len1 <= threshold)
                {
                    MergeWithExtraMemory(lo, pivot, hi, len1, len2);
                }
                else
                {
                    // since this method recurses to run merge on smaller arrays, it will
                    // end up using mergeWithExtraMemory
                    base.Merge(lo, pivot, hi, len1, len2);
                }
            }

        }

        /// <summary>
        /// This class is the equivalent of the anonymous class in the java version of GetSorter(T[], IComparer{T}).
        /// </summary>
        /// <typeparam name="T">The type of object to sort.</typeparam>
        private sealed class ArraySorterWithCustomComparer<T> : ArraySorterTemplate<T>
        {
            private readonly IComparer<T> comp;

            public ArraySorterWithCustomComparer(T[] a, IComparer<T> comp)
                : base(a)
            {
                this.comp = comp;
            }

            protected override int Compare(T a, T b)
            {
                return comp.Compare(a, b);
            }
        }

        private static SorterTemplate GetSorter<T>(T[] a, IComparer<T> comp)
        {
            return new ArraySorterWithCustomComparer<T>(a, comp);
        }

        /// <summary>
        /// This class is the equivalent of the anonymous class in the java version of GetSorter(T[])
        /// </summary>
        /// <typeparam name="T">The type of object being sorted</typeparam>
        private sealed class ArrayNaturalSorter<T> : ArraySorterTemplate<T>
            where T : IComparable<T>
        {
            public ArrayNaturalSorter(T[] a)
                : base(a)
            {
            }

            protected override int Compare(T a, T b)
            {
                return a.CompareTo(b);
            }
        }

        private static SorterTemplate GetSorter<T>(T[] a)
            where T : IComparable<T>
        {
            return new ArrayNaturalSorter<T>(a);
        }

        /// <summary>
        /// This class is the equivalent of the anonymous class in the java version of GetMergeSorter(T[], IComparer{T})
        /// </summary>
        /// <typeparam name="T">The type of object being sorted</typeparam>
        private sealed class ArrayMergeSorterWithCustomComparer<T> : ArrayMergeSorterTemplate<T>
        {
            private readonly IComparer<T> comp;

            public ArrayMergeSorterWithCustomComparer(T[] a, float overheadRatio, IComparer<T> comp)
                : base(a, overheadRatio)
            {
                this.comp = comp;
            }

            protected override int Compare(T a, T b)
            {
                return comp.Compare(a, b);
            }
        }

        private static SorterTemplate GetMergeSorter<T>(T[] a, IComparer<T> comp)
        {
            if (a.Length < MERGE_EXTRA_MEMORY_THRESHOLD)
            {
                return GetSorter(a, comp);
            }
            else
            {
                return new ArrayMergeSorterWithCustomComparer<T>(a, MERGE_OVERHEAD_RATIO, comp);
            }
        }

        /// <summary>
        /// This class is the equivalent of the anonymous class in the java version of GetMergeSorter(T[])
        /// </summary>
        /// <typeparam name="T">The type of object being sorted</typeparam>
        private sealed class ArrayNaturalMergeSorter<T> : ArrayMergeSorterTemplate<T>
            where T : IComparable<T>
        {
            public ArrayNaturalMergeSorter(T[] a, float overheadRatio)
                : base(a, overheadRatio)
            {
            }

            protected override int Compare(T a, T b)
            {
                return a.CompareTo(b);
            }
        }

        private static SorterTemplate GetMergeSorter<T>(T[] a)
            where T : IComparable<T>
        {
            if (a.Length < MERGE_EXTRA_MEMORY_THRESHOLD)
            {
                return GetSorter(a);
            }
            else
            {
                return new ArrayNaturalMergeSorter<T>(a, MERGE_OVERHEAD_RATIO);
            }
        }

        public static void QuickSort<T>(T[] a, int fromIndex, int toIndex, IComparer<T> comp)
        {
            if (toIndex - fromIndex <= 1) return;

            GetSorter(a, comp).QuickSort(fromIndex, toIndex - 1);
        }

        public static void QuickSort<T>(T[] a, IComparer<T> comp)
        {
            QuickSort(a, 0, a.Length, comp);
        }

        public static void QuickSort<T>(T[] a, int fromIndex, int toIndex)
            where T : IComparable<T>
        {
            if (toIndex - fromIndex <= 1) return;
            GetSorter(a).QuickSort(fromIndex, toIndex - 1);
        }

        public static void QuickSort<T>(T[] a)
            where T : IComparable<T>
        {
            QuickSort(a, 0, a.Length);
        }

        public static void MergeSort<T>(T[] a, int fromIndex, int toIndex, IComparer<T> comp)
        {
            if (toIndex - fromIndex <= 1) return;

            GetMergeSorter(a, comp).MergeSort(fromIndex, toIndex - 1);
        }

        public static void MergeSort<T>(T[] a, IComparer<T> comp)
        {
            MergeSort(a, 0, a.Length, comp);
        }

        public static void MergeSort<T>(T[] a, int fromIndex, int toIndex)
            where T : IComparable<T>
        {
            if (toIndex - fromIndex <= 1) return;
            GetMergeSorter(a).MergeSort(fromIndex, toIndex - 1);
        }

        public static void MergeSort<T>(T[] a)
            where T : IComparable<T>
        {
            MergeSort(a, 0, a.Length);
        }

        public static void TimSort<T>(T[] a, int fromIndex, int toIndex, IComparer<T> comp)
        {
            if (toIndex - fromIndex <= 1) return;

            GetMergeSorter(a, comp).TimSort(fromIndex, toIndex - 1);
        }

        public static void TimSort<T>(T[] a, IComparer<T> comp)
        {
            TimSort(a, 0, a.Length, comp);
        }

        public static void TimSort<T>(T[] a, int fromIndex, int toIndex)
            where T : IComparable<T>
        {
            if (toIndex - fromIndex <= 1) return;
            GetMergeSorter(a).TimSort(fromIndex, toIndex - 1);
        }

        public static void TimSort<T>(T[] a)
            where T : IComparable<T>
        {
            TimSort(a, 0, a.Length);
        }

        public static void InsertionSort<T>(T[] a, int fromIndex, int toIndex, IComparer<T> comp)
        {
            if (toIndex - fromIndex <= 1) return;

            GetSorter(a, comp).InsertionSort(fromIndex, toIndex - 1);
        }

        public static void InsertionSort<T>(T[] a, IComparer<T> comp)
        {
            InsertionSort(a, 0, a.Length, comp);
        }

        public static void InsertionSort<T>(T[] a, int fromIndex, int toIndex)
            where T : IComparable<T>
        {
            if (toIndex - fromIndex <= 1) return;
            GetSorter(a).InsertionSort(fromIndex, toIndex - 1);
        }

        public static void InsertionSort<T>(T[] a)
            where T : IComparable<T>
        {
            InsertionSort(a, 0, a.Length);
        }

        public static void BinarySort<T>(T[] a, int fromIndex, int toIndex, IComparer<T> comp)
        {
            if (toIndex - fromIndex <= 1) return;

            GetSorter(a, comp).BinarySort(fromIndex, toIndex - 1);
        }

        public static void BinarySort<T>(T[] a, IComparer<T> comp)
        {
            BinarySort(a, 0, a.Length, comp);
        }

        public static void BinarySort<T>(T[] a, int fromIndex, int toIndex)
            where T : IComparable<T>
        {
            if (toIndex - fromIndex <= 1) return;
            GetSorter(a).BinarySort(fromIndex, toIndex - 1);
        }

        public static void BinarySort<T>(T[] a)
            where T : IComparable<T>
        {
            BinarySort(a, 0, a.Length);
        }

    }
}