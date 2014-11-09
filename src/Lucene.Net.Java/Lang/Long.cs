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

namespace Java.Lang
{
    using System;

    public class Long
    {
        public const int SIZE = 64;

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
                var v = value;
                // TODO: find a better way of handling negative indices for DeBruijn64.Position
                var index =  ((v & -v)*0x022fdd63cc95386d >> 58);
                if (0 > index)
                    index = 64 + index;

                return POSITIONS[index]; 
            }
        }


      


        /// <summary>
        /// Returns the leading zeros from the binary expression of the value.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///      A long is 64 bits. Each bit is either a one or a zero.
        ///     <see cref="NumberOfLeadingZeros(long)"/> will count zeros from the left most bit towards the right
        ///     till it reaches the first bit that has a value of one.  Binary is often
        ///     written as 
        ///     <a href="http://simple.wikipedia.org/wiki/Hexadecimal_numeral_system">hexadecimal literal or hex digit</a> in code.    
        ///     </para>
        ///     <example>
        ///     <code lang="csharp"> 
        ///     // The counting stops at the first bit that has a one value starting from the left side.
        ///     // In the case below, the first bit with a value of one is in the 16th position.
        ///     //
        ///     // hex value, long value
        ///     // 0x1F00F0f00F111L = (Long)545422443606289;
        ///     //
        ///     // this is the binary form of the long value being tested:
        ///     // |                 | 15 zeros 
        ///     // 0000 0000 0000 0001 1111 0000 0000 1111 0000 1111 0000 0000 1111 0001 0001 0001
        ///     const long value2 = 0x1F00F0f00F111L;
        ///     Assert.Equal(15, Long.NumberOfLeadingZeros(value2), "The number of leading zeros must be 15");
        ///     </code>
        ///     </example>
        /// </remarks>
        /// <param name="value">The value.</param>
        /// <returns>The number of leading zeros.</returns>
        public static int NumberOfLeadingZeros(long value)
        {
            return (int)NumberOfLeadingZeros((ulong)value);
        }

        /// <summary>
        /// Returns the leading zeros from the binary expression of the value.  
        /// </summary>
        /// <seealso cref="NumberOfLeadingZeros(long)"/>
        /// <param name="value">The value.</param>
        /// <returns>The number of leading zeros.</returns>
        [CLSCompliant(false)]
        public static uint NumberOfLeadingZeros(ulong value)
        {
            if (value == 0)
                return 64;
            uint number = 1;
            var test = value >> 32;


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
            number -= (uint)test >> 31;

            return number;
        }

        // ReSharper disable once CSharpWarnings::CS1584
        /// <summary>
        /// Returns the number of trailing zeros from the binary value.
        /// </summary>
        /// <param name="value">The value to be inspected for trailing zeros.</param>
        /// <remarks>
        ///     <para>
        ///      A long is 64 bits. Each bit is either a one or a zero.
        ///     <see cref="NumberOfTrailingZeros(long)"/> will count zeros from the right most bit towards the left
        ///     till it reaches the first bit that has a value of one.  Binary is often
        ///     written as 
        ///     <a href="http://simple.wikipedia.org/wiki/Hexadecimal_numeral_system">hexadecimal literal or hex digit</a> in code.    
        ///     </para>
        ///     <example>
        ///     <code lang="csharp"> 
        ///     // The counting stops at the first bit that has a one value starting from the right side.
        ///     // In the case below, the first bit with a value of one is in the 16th position.
        ///     //
        ///     // hex value, long value
        ///     // 0x80000L = (Long) 524288;
        ///     //
        ///     // this is the binary form of the long value being tested:
        ///     //                                                        |                      | 19 zeros 
        ///     // 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 1000 0000 0000 0000 0000
        ///     const long value3 = 0x80000L;
        ///     Assert.Equal(19, Long.NumberOfTrailingZeros(value3), "The number of trailing zeros must be 19");
        ///     </code>
        ///     </example>
        ///     <para>
        ///         We're using the De Bruijn sequences based upon the various bit twiddling hacks found in
        ///         an online paper stanford <see href="https://graphics.stanford.edu/~seander/bithacks.html#ZerosOnRightMultLookup" />
        ///     </para>
        ///     <para>
        ///          It should be faster than Java's native 
        ///          <see href="http://docs.oracle.com/javase/7/docs/api/java/lang/Long.html#numberOfTrailingZeros(long)">
        ///          Long.numberOfTrailingZeros
        ///          </see> which uses the binary search method of finding trailing zeros.  
        ///     </para>
        /// </remarks>
        /// <returns>The number of trailing zeros.</returns>
        public static int NumberOfTrailingZeros(long value)
        {
            return DeBruijn64.Position(value);
        }

        /// <summary>
        /// Rotates the left.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="shift">The shift.</param>
        /// <returns>System.Int64.</returns>
        public static long RotateLeft(long value, int shift)
        {
            return (long)RotateLeft((ulong) value, shift);
        }

        /// <summary>
        /// Rotates the left.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="shift">The shift.</param>
        /// <returns>System.UInt64.</returns>
        [CLSCompliant(false)]
        public static ulong RotateLeft(ulong value, int shift)
        {
            return (value << shift) | (value >> -shift);
        }


        /// <summary>
        /// Rotates the right.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="shift">The shift.</param>
        /// <returns>System.Int64.</returns>
        public static long RotateRight(long value, int shift)
        {
            return (long)RotateRight((ulong) value, shift);
        }

        /// <summary>
        /// Rotates the right.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="shift">The shift.</param>
        /// <returns>System.UInt64.</returns>
        [CLSCompliant(false)]
        public static ulong RotateRight(ulong value, int shift)
        {
            return (value >> shift) | (value << -shift);
        }
    }
}
