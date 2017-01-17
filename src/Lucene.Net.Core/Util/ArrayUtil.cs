using System;
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

    /// <summary>
    /// Methods for manipulating arrays.
    ///
    /// @lucene.internal
    /// </summary>
    public sealed class ArrayUtil
    {
        /// <summary>
        /// Maximum length for an array; we set this to "a
        ///  bit" below Integer.MAX_VALUE because the exact max
        ///  allowed byte[] is JVM dependent, so we want to avoid
        ///  a case where a large value worked during indexing on
        ///  one JVM but failed later at search time with a
        ///  different JVM.
        /// </summary>
        public static readonly int MAX_ARRAY_LENGTH = int.MaxValue - 256;

        private ArrayUtil() // no instance
        {
        }

        /*
           Begin Apache Harmony code

           Revision taken on Friday, June 12. https://svn.apache.org/repos/asf/harmony/enhanced/classlib/archive/java6/modules/luni/src/main/java/java/lang/Integer.java

         */

        /// <summary>
        /// Parses the string argument as if it was an int value and returns the
        /// result. Throws NumberFormatException if the string does not represent an
        /// int quantity.
        /// </summary>
        /// <param name="chars"> a string representation of an int quantity. </param>
        /// <returns> int the value represented by the argument </returns>
        /// <exception cref="NumberFormatException"> if the argument could not be parsed as an int quantity. </exception>
        public static int ParseInt(char[] chars) // LUCENENET TODO: Rename ParseInt32 ?
        {
            return ParseInt(chars, 0, chars.Length, 10);
        }

        /// <summary>
        /// Parses a char array into an int. </summary>
        /// <param name="chars"> the character array </param>
        /// <param name="offset"> The offset into the array </param>
        /// <param name="len"> The length </param>
        /// <returns> the int </returns>
        /// <exception cref="NumberFormatException"> if it can't parse </exception>
        public static int ParseInt(char[] chars, int offset, int len) // LUCENENET TODO: Rename ParseInt32 ?
        {
            return ParseInt(chars, offset, len, 10);
        }

        /// <summary>
        /// Parses the string argument as if it was an int value and returns the
        /// result. Throws NumberFormatException if the string does not represent an
        /// int quantity. The second argument specifies the radix to use when parsing
        /// the value.
        /// </summary>
        /// <param name="chars"> a string representation of an int quantity. </param>
        /// <param name="radix"> the base to use for conversion. </param>
        /// <returns> int the value represented by the argument </returns>
        /// <exception cref="NumberFormatException"> if the argument could not be parsed as an int quantity. </exception>
        public static int ParseInt(char[] chars, int offset, int len, int radix) // LUCENENET TODO: Rename ParseInt32 ?
        {
            int minRadix = 2, maxRadix = 36;
            if (chars == null || radix < minRadix || radix > maxRadix)
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
            int max = int.MinValue / radix;
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

        /// <summary>
        /// Returns an array size >= minTargetSize, generally
        ///  over-allocating exponentially to achieve amortized
        ///  linear-time cost as the array grows.
        ///
        ///  NOTE: this was originally borrowed from Python 2.4.2
        ///  listobject.c sources (attribution in LICENSE.txt), but
        ///  has now been substantially changed based on
        ///  discussions from java-dev thread with subject "Dynamic
        ///  array reallocation algorithms", started on Jan 12
        ///  2010.
        /// </summary>
        /// <param name="minTargetSize"> Minimum required value to be returned. </param>
        /// <param name="bytesPerElement"> Bytes used by each element of
        /// the array.  See constants in <seealso cref="RamUsageEstimator"/>.
        ///
        /// @lucene.internal </param>

        public static int Oversize(int minTargetSize, int bytesPerElement)
        {
            if (minTargetSize < 0)
            {
                // catch usage that accidentally overflows int
                throw new System.ArgumentException("invalid array size " + minTargetSize);
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
                return int.MaxValue;
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
            // this saves us from "running hot" (constantly making a
            // bit bigger then a bit smaller, over and over):
            if (newSize < currentSize / 2)
            {
                return newSize;
            }
            else
            {
                return currentSize;
            }
        }

        public static short[] Grow(short[] array, int minSize)
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                short[] newArray = new short[Oversize(minSize, RamUsageEstimator.NUM_BYTES_SHORT)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static short[] Grow(short[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static float[] Grow(float[] array, int minSize)
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                float[] newArray = new float[Oversize(minSize, RamUsageEstimator.NUM_BYTES_FLOAT)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static float[] Grow(float[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static double[] Grow(double[] array, int minSize)
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                double[] newArray = new double[Oversize(minSize, RamUsageEstimator.NUM_BYTES_DOUBLE)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static double[] Grow(double[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static short[] Shrink(short[] array, int targetSize)
        {
            Debug.Assert(targetSize >= 0, "size must be positive (got " + targetSize + "): likely integer overflow?");
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_SHORT);
            if (newSize != array.Length)
            {
                short[] newArray = new short[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static int[] Grow(int[] array, int minSize)
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                int[] newArray = new int[Oversize(minSize, RamUsageEstimator.NUM_BYTES_INT)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static int[] Grow(int[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static int[] Shrink(int[] array, int targetSize)
        {
            Debug.Assert(targetSize >= 0, "size must be positive (got " + targetSize + "): likely integer overflow?");
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_INT);
            if (newSize != array.Length)
            {
                int[] newArray = new int[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static long[] Grow(long[] array, int minSize)
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                long[] newArray = new long[Oversize(minSize, RamUsageEstimator.NUM_BYTES_LONG)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static long[] Grow(long[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static long[] Shrink(long[] array, int targetSize)
        {
            Debug.Assert(targetSize >= 0, "size must be positive (got " + targetSize + "): likely integer overflow?");
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_LONG);
            if (newSize != array.Length)
            {
                long[] newArray = new long[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static sbyte[] Grow(sbyte[] array, int minSize) // LUCENENET TODO: remove this overload, mark it non-CLS compliant, or mark internal
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                var newArray = new sbyte[Oversize(minSize, 1)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static byte[] Grow(byte[] array, int minSize)
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                byte[] newArray = new byte[Oversize(minSize, 1)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static byte[] Grow(byte[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static byte[] Shrink(byte[] array, int targetSize)
        {
            Debug.Assert(targetSize >= 0, "size must be positive (got " + targetSize + "): likely integer overflow?");
            int newSize = GetShrinkSize(array.Length, targetSize, 1);
            if (newSize != array.Length)
            {
                var newArray = new byte[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static bool[] Grow(bool[] array, int minSize)
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                bool[] newArray = new bool[Oversize(minSize, 1)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static bool[] Grow(bool[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static bool[] Shrink(bool[] array, int targetSize)
        {
            Debug.Assert(targetSize >= 0, "size must be positive (got " + targetSize + "): likely integer overflow?");
            int newSize = GetShrinkSize(array.Length, targetSize, 1);
            if (newSize != array.Length)
            {
                bool[] newArray = new bool[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static char[] Grow(char[] array, int minSize)
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                char[] newArray = new char[Oversize(minSize, RamUsageEstimator.NUM_BYTES_CHAR)];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static char[] Grow(char[] array)
        {
            return Grow(array, 1 + array.Length);
        }

        public static char[] Shrink(char[] array, int targetSize)
        {
            Debug.Assert(targetSize >= 0, "size must be positive (got " + targetSize + "): likely integer overflow?");
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_CHAR);
            if (newSize != array.Length)
            {
                char[] newArray = new char[newSize];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static int[][] Grow(int[][] array, int minSize) // LUCENENET TODO: CLS compliance issue
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                var newArray = new int[Oversize(minSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static int[][] Grow(int[][] array) // LUCENENET TODO: CLS compliance issue
        {
            return Grow(array, 1 + array.Length);
        }

        public static int[][] Shrink(int[][] array, int targetSize) // LUCENENET TODO: CLS compliance issue
        {
            Debug.Assert(targetSize >= 0, "size must be positive (got " + targetSize + "): likely integer overflow?");
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
            if (newSize != array.Length)
            {
                int[][] newArray = new int[newSize][];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static float[][] Grow(float[][] array, int minSize) // LUCENENET TODO: CLS compliance issue
        {
            Debug.Assert(minSize >= 0, "size must be positive (got " + minSize + "): likely integer overflow?");
            if (array.Length < minSize)
            {
                float[][] newArray = new float[Oversize(minSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF)][];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        public static float[][] Grow(float[][] array) // LUCENENET TODO: CLS compliance issue
        {
            return Grow(array, 1 + array.Length);
        }

        public static float[][] Shrink(float[][] array, int targetSize) // LUCENENET TODO: CLS compliance issue
        {
            Debug.Assert(targetSize >= 0, "size must be positive (got " + targetSize + "): likely integer overflow?");
            int newSize = GetShrinkSize(array.Length, targetSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
            if (newSize != array.Length)
            {
                float[][] newArray = new float[newSize][];
                Array.Copy(array, 0, newArray, 0, newSize);
                return newArray;
            }
            else
            {
                return array;
            }
        }

        /// <summary>
        /// Returns hash of chars in range start (inclusive) to
        /// end (inclusive)
        /// </summary>
        public static int GetHashCode(char[] array, int start, int end)
        {
            int code = 0;
            for (int i = end - 1; i >= start; i--)
            {
                code = code * 31 + array[i];
            }
            return code;
        }

        /// <summary>
        /// Returns hash of bytes in range start (inclusive) to
        /// end (inclusive)
        /// </summary>
        public static int GetHashCode(sbyte[] array, int start, int end) // LUCENENET TODO: chnage to byte ?
        {
            int code = 0;
            for (int i = end - 1; i >= start; i--)
            {
                code = code * 31 + array[i];
            }
            return code;
        }

        // Since Arrays.equals doesn't implement offsets for equals
        /// <summary>
        /// See if two array slices are the same.
        /// </summary>
        /// <param name="left">        The left array to compare </param>
        /// <param name="offsetLeft">  The offset into the array.  Must be positive </param>
        /// <param name="right">       The right array to compare </param>
        /// <param name="offsetRight"> the offset into the right array.  Must be positive </param>
        /// <param name="length">      The length of the section of the array to compare </param>
        /// <returns> true if the two arrays, starting at their respective offsets, are equal
        /// </returns>
        /// <seealso cref= java.util.Arrays#equals(char[], char[]) </seealso>
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

        // Since Arrays.equals doesn't implement offsets for equals
        /// <summary>
        /// See if two array slices are the same.
        /// </summary>
        /// <param name="left">        The left array to compare </param>
        /// <param name="offsetLeft">  The offset into the array.  Must be positive </param>
        /// <param name="right">       The right array to compare </param>
        /// <param name="offsetRight"> the offset into the right array.  Must be positive </param>
        /// <param name="length">      The length of the section of the array to compare </param>
        /// <returns> true if the two arrays, starting at their respective offsets, are equal
        /// </returns>
        /// <seealso cref= java.util.Arrays#equals(byte[], byte[]) </seealso>
        public static bool Equals(byte[] left, int offsetLeft, byte[] right, int offsetRight, int length)
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

        /* DISABLE this FOR NOW: this has performance problems until Java creates intrinsics for Class#getComponentType() and Array.newInstance()
        public static <T> T[] grow(T[] array, int minSize) {
          assert minSize >= 0: "size must be positive (got " + minSize + "): likely integer overflow?";
          if (array.length < minSize) {
            @SuppressWarnings("unchecked") final T[] newArray =
              (T[]) Array.newInstance(array.getClass().getComponentType(), oversize(minSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF));
            System.arraycopy(array, 0, newArray, 0, array.length);
            return newArray;
          } else
            return array;
        }

        public static <T> T[] grow(T[] array) {
          return grow(array, 1 + array.length);
        }

        public static <T> T[] shrink(T[] array, int targetSize) {
          assert targetSize >= 0: "size must be positive (got " + targetSize + "): likely integer overflow?";
          final int newSize = getShrinkSize(array.length, targetSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
          if (newSize != array.length) {
            @SuppressWarnings("unchecked") final T[] newArray =
              (T[]) Array.newInstance(array.getClass().getComponentType(), newSize);
            System.arraycopy(array, 0, newArray, 0, newSize);
            return newArray;
          } else
            return array;
        }
        */

        // Since Arrays.equals doesn't implement offsets for equals
        /// <summary>
        /// See if two array slices are the same.
        /// </summary>
        /// <param name="left">        The left array to compare </param>
        /// <param name="offsetLeft">  The offset into the array.  Must be positive </param>
        /// <param name="right">       The right array to compare </param>
        /// <param name="offsetRight"> the offset into the right array.  Must be positive </param>
        /// <param name="length">      The length of the section of the array to compare </param>
        /// <returns> true if the two arrays, starting at their respective offsets, are equal
        /// </returns>
        /// <seealso cref= java.util.Arrays#equals(char[], char[]) </seealso>
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

        public static int[] ToIntArray(ICollection<int?> ints) // LUCENENET TODO: Nullable issue - the cast to int probably won't work here
        {
            int[] result = new int[ints.Count];
            int upto = 0;
            foreach (int v in ints)
            {
                result[upto++] = v;
            }

            // paranoia:
            Debug.Assert(upto == result.Length);

            return result;
        }

        // LUCENENET specific - replaced NaturalComparer<T> with Comparer<T>.Default
        //private class NaturalComparerImpl<T> : IComparer<T> where T : IComparable<T>
        //{
        //    internal NaturalComparerImpl()
        //    {
        //    }

        //    public virtual int Compare(T o1, T o2)
        //    {
        //        return o1.CompareTo(o2);
        //    }
        //}

        /// <summary>
        /// Get the natural <seealso cref="Comparer"/> for the provided object class. </summary>
        public static IComparer<T> NaturalComparer<T>()
            where T : IComparable<T>
        {
            return Comparer<T>.Default;
            //return (IComparer<T>)new NaturalComparerImpl<T>();
        }

        /// <summary>
        /// Swap values stored in slots <code>i</code> and <code>j</code> </summary>
        public static void Swap<T>(T[] arr, int i, int j)
        {
            T tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }

        // intro-sorts

        /// <summary>
        /// Sorts the given array slice using the <seealso cref="Comparer"/>. this method uses the intro sort
        /// algorithm, but falls back to insertion sort for small arrays. </summary>
        /// <param name="fromIndex"> start index (inclusive) </param>
        /// <param name="toIndex"> end index (exclusive) </param>
        public static void IntroSort<T>(T[] a, int fromIndex, int toIndex, IComparer<T> comp)
        {
            if (toIndex - fromIndex <= 1)
            {
                return;
            }
            (new ArrayIntroSorter<T>(a, comp)).Sort(fromIndex, toIndex);
        }

        /// <summary>
        /// Sorts the given array using the <seealso cref="Comparer"/>. this method uses the intro sort
        /// algorithm, but falls back to insertion sort for small arrays.
        /// </summary>
        public static void IntroSort<T>(T[] a, IComparer<T> comp)
        {
            IntroSort(a, 0, a.Length, comp);
        }

        /// <summary>
        /// Sorts the given array slice in natural order. this method uses the intro sort
        /// algorithm, but falls back to insertion sort for small arrays. </summary>
        /// <param name="fromIndex"> start index (inclusive) </param>
        /// <param name="toIndex"> end index (exclusive) </param>
        public static void IntroSort<T>(T[] a, int fromIndex, int toIndex) where T : IComparable<T>
        {
            if (toIndex - fromIndex <= 1)
            {
                return;
            }
            IntroSort(a, fromIndex, toIndex, ArrayUtil.NaturalComparer<T>());
        }

        /// <summary>
        /// Sorts the given array in natural order. this method uses the intro sort
        /// algorithm, but falls back to insertion sort for small arrays.
        /// </summary>
        public static void IntroSort<T>(T[] a) where T : IComparable<T>
        {
            IntroSort(a, 0, a.Length);
        }

        // tim sorts:

        /// <summary>
        /// Sorts the given array slice using the <seealso cref="Comparer"/>. this method uses the Tim sort
        /// algorithm, but falls back to binary sort for small arrays. </summary>
        /// <param name="fromIndex"> start index (inclusive) </param>
        /// <param name="toIndex"> end index (exclusive) </param>
        public static void TimSort<T>(T[] a, int fromIndex, int toIndex, IComparer<T> comp)
        {
            if (toIndex - fromIndex <= 1)
            {
                return;
            }
            (new ArrayTimSorter<T>(a, comp, a.Length / 64)).Sort(fromIndex, toIndex);
        }

        /// <summary>
        /// Sorts the given array using the <seealso cref="Comparer"/>. this method uses the Tim sort
        /// algorithm, but falls back to binary sort for small arrays.
        /// </summary>
        public static void TimSort<T>(T[] a, IComparer<T> comp)
        {
            TimSort(a, 0, a.Length, comp);
        }

        /// <summary>
        /// Sorts the given array slice in natural order. this method uses the Tim sort
        /// algorithm, but falls back to binary sort for small arrays. </summary>
        /// <param name="fromIndex"> start index (inclusive) </param>
        /// <param name="toIndex"> end index (exclusive) </param>
        public static void TimSort<T>(T[] a, int fromIndex, int toIndex) where T : IComparable<T>
        {
            if (toIndex - fromIndex <= 1)
            {
                return;
            }
            TimSort(a, fromIndex, toIndex, ArrayUtil.NaturalComparer<T>());
        }

        /// <summary>
        /// Sorts the given array in natural order. this method uses the Tim sort
        /// algorithm, but falls back to binary sort for small arrays.
        /// </summary>
        public static void TimSort<T>(T[] a) where T : IComparable<T>
        {
            TimSort(a, 0, a.Length);
        }
    }
}