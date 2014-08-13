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
    using System.Linq;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Utility methods for manipulating arrays.
    /// </summary>
    public static class ArrayUtil
    {


        /// <summary>
        /// Returns an array that has been resized to a length greater than
        /// the original array.
        /// </summary>
        /// <typeparam name="T">The element type for the array.</typeparam>
        /// <param name="array">The array to base the resize on.</param>
        /// <param name="minSize">The minimum size to grow the array.</param>
        /// <returns>The resized array.</returns>
        /// <exception cref="ArgumentException">Throws when <paramref name="minTargetSize"/> is less than zero.</exception>
        public static T[] Grow<T>(this T[] array, int minSize = 1)
        {
            Debug.Assert(typeof(T).GetTypeInfo().IsPrimitive, "Type T must be primitive");
            Debug.Assert(minSize >= 0, "targetSize must be positive");

            if (array.Length < minSize)
            {
                int capacity = Oversize(minSize, RamUsageEstimator.PrimitiveSizes[typeof(T)]);
                var oversizedArray = new T[capacity];
                Array.Copy(array, 0, oversizedArray, 0, array.Length);

                return oversizedArray;
            }
            else
            {
                return array;
            }
        }


        /// <summary>
        /// Returns a hashcode for an array.
        /// </summary>
        /// <typeparam name="T">The element type for the array.</typeparam>
        /// <param name="array">The target array to create a hashcode.</param>
        /// <param name="start">The starting position for a loop.</param>
        /// <param name="end">The number of iterations for the loop to perform.</param>
        /// <returns>The hashcode.</returns>
        public static int CreateHashCode<T>(this T[] array, int start, int end) where T : struct
        {
            var code = 0;
            var elements = array.Cast<int>().ToArray();
            for (var i = end - 1; i >= start; i--)
            {
                code = code * 31 + elements[i];
            }

            return code;
        }


        /// <summary>
        /// Returns a new capacity number to resize an array.
        /// </summary>
        /// <param name="minTargetSize">The minium size for the oversize.</param>
        /// <param name="bytesPerElement">The number of bytes an element of the array will allocate in memory.</param>
        /// <returns>The new capacity size.</returns>
        /// <exception cref="ArgumentException">Throws when <paramref name="minTargetSize"/> is less than zero.</exception>
        public static int Oversize(int minTargetSize, int bytesPerElement)
        {
            // catch usage that accidentally overflows int
            Check.Condition(minTargetSize < 0, "minTargetSize", "invalid array size {0}", minTargetSize);

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

            if (Constants.KRE_IS_64BIT)
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

        /// <summary>
        /// Parses the string argument as if it was an int value and returns the result.
        /// </summary>
        /// <param name="chars">A string representation of an int quantity. </param>
        /// <param name="offset">The position in the <paramref name="chars"/> array to start parsing.</param>
        /// <param name="limit">The number of characters to parse after the <paramref name="offset"/>.</param>
        /// <param name="radix"> The base to use for conversion. </param>
        /// <returns> int the value represented by the argument </returns>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="chars"/> is null.</exception>
        /// <exception cref="ArgumentException">Throws when <paramref name="chars"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <list type="bullet">
        ///         <item>Throws when <paramref name="offset"/> is less than zero or greater than <paramref name="chars"/>.Length.</item>
        ///         <item>Throws when <paramref name="limit"/> is greater than <paramref name="chars"/>.Length.</item>
        ///         <item>Throws when <paramref name="radix"/> is less than 2 or greater than 36.</item>
        ///     </list>
        /// </exception>
        /// <exception cref="FormatException">Throws when a character cannot be translated into a integer.</exception>
        public static int ParseInt(this string chars, int offset = 0, int limit = -1, int radix = 10)
        {
            Check.NotNull("chars", chars);

            return ParseInt(chars.ToCharArray(), offset, limit, radix);
        }

        /// <summary>
        /// Parses the string argument as if it was an int value and returns the result.
        /// </summary>
        /// <param name="chars">A string representation of an int quantity. </param>
        /// <param name="offset">The position in the <paramref name="chars"/> array to start parsing.</param>
        /// <param name="limit">The number of characters to parse after the <paramref name="offset"/>.</param>
        /// <param name="radix"> The base to use for conversion. </param>
        /// <returns>The integer value that was parsed from <pararef name="chars" />.</returns>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="chars"/> is null.</exception>
        /// <exception cref="ArgumentException">Throws when <paramref name="chars"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <list type="bullet">
        ///         <item>Throws when <paramref name="offset"/> is less than zero or greater than <paramref name="chars"/>.Length.</item>
        ///         <item>Throws when <paramref name="limit"/> is greater than <paramref name="chars"/>.Length.</item>
        ///         <item>Throws when <paramref name="radix"/> is less than 2 or greater than 36.</item>
        ///     </list>
        /// </exception>
        /// <exception cref="FormatException">Throws when a character cannot be translated into a integer.</exception>
        public static int ParseInt(this char[] chars, int offset = 0, int limit = -1, int radix = 10)
        {
            Check.NotNull("chars", chars, false);
            Check.Condition(chars.Length == 0, "chars", "The parameter, chars, must not be an empty array.");
            Check.Condition<ArgumentOutOfRangeException>(
                offset < 0 || offset > chars.Length,  
                "The parameter, offset ({0}), must be greater than -1 and less than parameter, chars.Length ({1}).", offset, chars.Length);

            if (limit < 0)
                limit = chars.Length;

            Check.Condition<ArgumentOutOfRangeException>(
                limit > chars.Length,
                "The parameter, limit ({0}), must be less than or equal to the length of the parameter, chars ({1}).", 
                 limit, chars.Length);

            const int minRadix = 2;
            const int maxRadix = 36;

            Check.Condition<ArgumentOutOfRangeException>(
                radix < minRadix || radix > maxRadix,
                "The parameter, radix ({0}), must be greater than {1} and less than {2}.", radix, minRadix, maxRadix);

            bool negative = chars[offset] == '-';

            Check.Condition<FormatException>(negative && limit == 1, "A negative sign, '-', cannot be converted into an integer.");

            if (negative == true)
            {
                offset++;
                limit--;
            }

            return ParseInt(chars, offset, limit, radix, negative);
        }

        // parse() in Java
        private static int ParseInt(char[] chars, int offset, int len, int radix, bool negative)
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


        private class NaturalComparator<T> : IComparer<T> where T : IComparable<T>
        {
            public int Compare(T x, T y)
            {
                return x.CompareTo(y);
            }
        }

        public static IComparer<T> NaturalComparer<T>() where T : IComparable<T>
        {
            return new NaturalComparator<T>();
        }

        public static T[] CopyOf<T>(this T[] array, int length)
        {
            var copy = new T[length];
            Array.Copy(array, copy, length);

            return copy;
        }

        public static T[] CopyOfRange<T>(this T[] array, int start, int end)
        {
            //Check.SliceInRangeOfLength(start, end, array.Length);

            var length = end - start;
            var copy = new T[length];
            Array.Copy(array, start, copy, 0, length);

            return copy;
        }

        public static void Swap<T>(this T[] array, int x, int y)
        {
            T tmp = array[x];
            array[x] = array[y];
            array[y] = tmp;
        }
    }
}