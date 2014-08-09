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

    using System.Diagnostics;
    using Lucene.Net.Support;

    /// <summary>
    /// Methods and constants inspired by the article
    /// "Broadword Implementation of Rank/Select Queries" by Sebastiano Vigna, January 30, 2012:
    /// </summary>
    public sealed class BroadWord
    {

        private BroadWord() // no instance
        {
        }

        /// <summary>
        ///  L8  denotes the constant of 8-byte-counts or 8k.
        ///  _L denotes that the number is an long format. 
        /// </summary>
        public const long L8_L = 0x0101010101010101L;

        /// <summary>
        ///  L9  denotes the constant of 8-byte-counts or 9k.
        ///  _L denotes that the number is an long format. 
        /// </summary>
        public const long L9_L = unchecked((long)0x8040201008040201L);

        /// <summary>
        ///  L16  denotes the constant of 16-byte-counts or 16k.
        ///  _L denotes that the number is an long format. 
        /// </summary>
        public const long L16_L = 0x0001000100010001L;

        /// <summary>
        /// H8 = L8 << (8-1) .
        ///  These contain the high bit of each group of k bits.
        ///  The suffix _L indicates the long implementation.
        /// </summary>
        public static readonly long H8_L = L8_L << 7;

        /// H16 = L16 << (16-1) .
        ///  These contain the high bit of each group of k bits.
        ///  The suffix _L indicates the long implementation.
        public static readonly long H16_L = L16_L << 15;

        /// <summary>
        /// Bit count of a long.
        /// Only here to compare the implementation with <seealso cref="Select(long,int)"/>,
        /// normally <seealso cref="Lucene.Net.Util.BitUtil.BitCount"/> is preferable. </summary>
        /// <returns> The total number of 1 bits in x. </returns>
        public static int BitCount(long value)
        {
            ulong x = (ulong)value;
            // Step 0 leaves in each pair of bits the number of ones originally contained in that pair:
            x = x - ((x & 0xAAAAAAAAAAAAAAAAL) >> 1);
            // Step 1, idem for each nibble:
            x = (x & 0x3333333333333333L) + ((x >> 2) & 0x3333333333333333L);
            // Step 2, idem for each byte:
            x = (x + (x >> 4)) & 0x0F0F0F0F0F0F0F0FL;
            // Multiply to sum them all into the high byte, and return the high byte:
            return (int)((x * L8_L) >> 56);
        }

       
    }
}