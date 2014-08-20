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

namespace Lucene.Net.Support
{
    using System;

    /// <summary>
    /// Extension methods for numeric types to enable the same functionality that
    /// currently exists in the standard java libraries. 
    /// </summary>
    public static class NumberExtensionMethods
    {

        private static class DeBruijn32Leading
        {

        }

        /// <summary>
        /// FFS bit positions with De Bruijn sequences.
        /// </summary>
        private static class DeBruijn32
        {
            private static readonly int[] POSITIONS =
            {
                0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
                31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
            };

            /// <summary>
            /// Returns the first set bit (FFS), or 0 if no bits are set.
            /// </summary>
            public static int Position(int number)
            {
                var res = unchecked((uint) (number & -number)*0x077CB531U) >> 27;
                return POSITIONS[res];
            }
        }


        private static class DeBruijn64
        {


            private static readonly int[] POSITIONS =
            {
                0, 1, 2, 53, 3, 7, 54, 27, 4, 38, 41, 8, 34, 55, 48, 28,
                62, 5, 39, 46, 44, 42, 22, 9, 24, 35, 59, 56, 49, 18, 29, 11,
                63, 52, 6, 26, 37, 40, 33, 47, 61, 45, 43, 21, 23, 58, 17, 10,
                51, 25, 36, 32, 60, 20, 57, 16, 50, 31, 19, 15, 30, 14, 13, 12
            };

            public static int Position(long value)
            {

                var result = unchecked((uint) (value & -value)*0x022fdd63cc95386d) >> 58;
                return POSITIONS[result];
            }
        }


        public static int NumberOfLeadingZeros(this long value)
        {
            return (int) NumberOfLeadingZeros((ulong) value);
        }

         [CLSCompliant(false)]
        public static uint NumberOfLeadingZeros(this ulong value)
        {
            if (value == 0)
                return 64;
            uint number = 1;
            var test = (uint)value >> 32;


            if (test == 0)
            {
                number += 32;
                test = (uint)value;
            }

            if (test >> 16 == 0)
            {
                number += 16;
                test <<= 16;
            }

            if (test >> 24 == 0)
            {
                number += 8;
                test <<= 8;
            }

            if (test >> 28 == 0)
            {
                number += 4;
                test <<= 4;
            }

            if (test >> 30 == 0)
            {
                number += 2;
                test <<= 2;
            }
            number -= test >> 31;

            return number;
        }

        public static int NumberOfLeadingZeros(this int value)
        {
            return (int) NumberOfLeadingZeros((uint) value);
        }

        [CLSCompliant(false)]
        public static uint NumberOfLeadingZeros(this uint value)
        {
            // from hacker's delight
            http: //www.hackersdelight.org/permissions.htm
            uint test, number;
            uint x = value;

            number = 32;
            test = x >> 16;
            if (test != 0)
            {
                number = number - 16;
                x = test;
            }
            test = x >> 8;
            if (test != 0)
            {
                number = number - 8;
                x = test;
            }
            test = x >> 4;
            if (test != 0)
            {
                number = number - 4;
                x = test;
            }
            test = x >> 2;
            if (test != 0)
            {
                number = number - 2;
                x = test;
            }
            test = x >> 1;
            
            if (test != 0)
                return number - 2;

            return number - x;
        }

        /// <summary>
        /// Returns the number of trailing zeros. i.e 100 has two trailing zeros.
        /// </summary>
        /// <param name="value">The value to be inspected for trailing zeros.</param>
        /// <remarks>
        ///     <para>
        ///         We're using the De Bruijn sequences based upon the various bit twiddling hacks found in 
        ///         an <a href="https://graphics.stanford.edu/~seander/bithacks.html#ZerosOnRightMultLookup">online paper at stanford</a>
        ///     </para>
        ///     <para>
        ///          It should be faster than Java's native 
        ///          <a href="http://grepcode.com/file/repository.grepcode.com/java/root/jdk/openjdk/7u40-b43/java/lang/Integer.java#Integer.numberOfTrailingZeros%28int%29">
        ///          Long.numberOfTrailingZeros
        ///          </a> which uses the binary search method of finding trailing zeros.  
        ///     </para>
        /// </remarks>
        /// <returns>The number of trailing zeros.</returns>
        public static int NumberOfTrailingZeros(this int value)
        {
            return DeBruijn32.Position(value);
        }

        // ReSharper disable once CSharpWarnings::CS1584
        /// <summary>
        /// Returns the number of trailing zeros. i.e 100 has two trailing zeros.
        /// </summary>
        /// <param name="value">The value to be inspected for trailing zeros.</param>
        /// <remarks>
        ///     <para>
        ///         We're using the De Bruijn sequences based upon the various bit twiddling hacks found in 

        ///         an online paper stanford <see href="https://graphics.stanford.edu/~seander/bithacks.html#ZerosOnRightMultLookup" />
        ///     </para>
        ///     <para>
        ///          It should be faster than Java's native 
        ///          <see href="http://grepcode.com/file/repository.grepcode.com/java/root/jdk/openjdk/7u40-b43/java/lang/Integer.java#Integer.numberOfTrailingZeros%28int%29">
        ///          Long.numberOfTrailingZeros
        ///          </see> which uses the binary search method of finding trailing zeros.  
        ///     </para>
        /// </remarks>
        /// <returns>The number of trailing zeros.</returns>
        public static int NumberOfTrailingZeros(this long value)
        {
            return DeBruijn64.Position(value);
        }

        /// <summary>
        /// Rotates bits to the left. Similar to _rotl in c++ or Java's Integer.rotateLeft.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="shift">The number of bits to shift.</param>
        /// <returns>The rotated value.</returns>
        internal static uint RotateLeft(this uint value, int shift)
        {
            var v = value;
            return (v << shift) | (v >> (32 - shift));
        }

        /// <summary>
        /// Rotates bits to the right. Similar to _rotr in c++ or Java's Integer.rotateRight.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="shift">The number of bits to shift.</param>
        /// <returns>The rotated value.</returns>
        internal static uint RotateRight(this uint value, int shift)
        {
            var v = value;
            return (v >> shift) | (v << (32 - shift));
        }

        /// <summary>
        /// Rotates bits to the left. Similar to _rotl in c++ or Java's Integer.rotateLeft.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="shift">The number of bits to shift.</param>
        /// <returns>The rotated value.</returns>
        public static int RotateLeft(this int value, int shift)
        {
            return (int)RotateLeft((uint)value, shift);
        }

        /// <summary>
        /// Rotates bits to the right. Similar to _rotr in c++ or Java's Integer.rotateRight.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="shift">The number of bits to shift.</param>
        /// <returns>The rotated value.</returns>
        public static int RotateRight(this int value, int shift)
        {
            return (int)RotateRight((uint)value, shift);
        }
    }
}
