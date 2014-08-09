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
        /// <summary>
        /// FFS bit positions with De Bruijn sequences.
        /// </summary>
        static class DeBruijn32
        {
            static int[] s_positions = new int[32]
	        {
	            0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
	            31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
	        };

            /// <summary>
            /// Returns the first set bit (FFS), or 0 if no bits are set.
            /// </summary>
            public static int Position(int number)
            {
                uint res = unchecked((uint)(number & -number) * 0x077CB531U) >> 27;
                return s_positions[res];
            }
        }


        static class DeBruijn64
        {


            static int[] s_positions = new int[64] {
                0,  1,  2, 53,  3,  7, 54, 27, 4, 38, 41,  8, 34, 55, 48, 28,
                62,  5, 39, 46, 44, 42, 22,  9, 24, 35, 59, 56, 49, 18, 29, 11,
                63, 52,  6, 26, 37, 40, 33, 47, 61, 45, 43, 21, 23, 58, 17, 10,
                51, 25, 36, 32, 60, 20, 57, 16, 50, 31, 19, 15, 30, 14, 13, 12,
            };

            public static int Position(long value)
            {

                var result = unchecked((uint)(value & -value) * 0x022fdd63cc95386d) >> 58;
                return s_positions[result];
            }
        }


        /// <summary>
        /// Returns the number of trailing zeros. i.e 100 has two trailing zeros.
        /// </summary>
        /// <param name="value">The value to be inspected for trailing zeros.</param>
        /// <remarks>
        ///     <para>
        ///         We're using the De Bruijn sequences based upon the various bit twiddling hacks found in 
        ///         an online paper stanford <see cref="https://graphics.stanford.edu/~seander/bithacks.html#ZerosOnRightMultLookup" />
        ///     </para>
        ///     <para>
        ///          It should be faster than Java's native 
        ///          <see href="http://grepcode.com/file/repository.grepcode.com/java/root/jdk/openjdk/7u40-b43/java/lang/Integer.java#Integer.numberOfTrailingZeros%28int%29">
        ///          Long.numberOfTrailingZeros
        ///          </see> which uses the binary search method of finding trailing zeros.  
        ///     </para>
        /// </remarks>
        /// <returns>The number of trailing zeros.</returns>
        public static int NumberOfTrailingZeros(this int value)
        {
            return DeBruijn32.Position(value);
        }

        /// <summary>
        /// Returns the number of trailing zeros. i.e 100 has two trailing zeros.
        /// </summary>
        /// <param name="value">The value to be inspected for trailing zeros.</param>
        /// <remarks>
        ///     <para>
        ///         We're using the De Bruijn sequences based upon the various bit twiddling hacks found in 
        ///         an online paper stanford <see cref="https://graphics.stanford.edu/~seander/bithacks.html#ZerosOnRightMultLookup" />
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
    }
}
