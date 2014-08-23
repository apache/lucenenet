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

                return POSITIONS[(v & -v) * 0x022fdd63cc95386d >> 58];
            }
        }


      


        /// <summary>
        /// Returns the leading zeros from the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Int32.</returns>
        public static int NumberOfLeadingZeros(long value)
        {
            return (int)NumberOfLeadingZeros((ulong)value);
        }

        /// <summary>
        /// Returns the leading zeros from the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.UInt32.</returns>
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
        public static int NumberOfTrailingZeros(long value)
        {
            return DeBruijn64.Position(value);
        }

        public static long RotateLeft(long value, int shift)
        {
            return (long)RotateLeft((ulong) value, shift);
        }

        [CLSCompliant(false)]
        public static ulong RotateLeft(ulong value, int shift)
        {
            return (value << shift) | (value >> -shift);
        }


        public static long RotateRight(long value, int shift)
        {
            return (long)RotateRight((ulong) value, shift);
        }

        [CLSCompliant(false)]
        public static ulong RotateRight(ulong value, int shift)
        {
            return (value >> shift) | (value << -shift);
        }
    }
}
