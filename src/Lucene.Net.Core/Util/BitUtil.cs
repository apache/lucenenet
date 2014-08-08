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

    /// <summary>
    ///  A variety of high efficiency bit twiddling routines.
    ///  </summary>
    public static class BitUtil
    {
        /// <summary>
        /// table of bytes
        /// </summary>
        private static readonly byte[] BYTE_COUNTS = new byte[] {
            0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4, 
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 
            4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8 }; 

        // 
        /// <summary>
        ///The General Idea: instead of having an array per byte that has
        /// the offsets of the next set bit, that array could be
        /// packed inside a 32 bit integer (8 4 bit numbers).  That
        /// should be faster than accessing an array for each index, and
        /// the total array size is kept smaller (256*sizeof(int))=1K
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <code language="python">
        ///             *** the python code that generated bitlist
        /// def bits2int(val):
        ///     arr=0
        ///     for shift in range(8,0,-1):
        ///     if val & 0x80:
        ///         arr = (arr << 4) | shift
        ///     val = val << 1
        ///     return arr
        ///
        /// def int_table():
        ///     tbl = [ hex(bits2int(val)).strip('L') for val in range(256) ]
        ///     return ','.join(tbl)
        /// 
        ///         </code>
        ///     </para>
        ///     <para>
        ///         0x87654321 converts to unint, so an unchecked conversion to int is required. 
        ///     </para>
        /// </remarks>
        private static readonly int[] BIT_LISTS = new int[] { 
          0x0, 0x1, 0x2, 0x21, 0x3, 0x31, 0x32, 0x321, 0x4, 0x41, 0x42, 0x421, 0x43, 
            0x431, 0x432, 0x4321, 0x5, 0x51, 0x52, 0x521, 0x53, 0x531, 0x532, 0x5321, 
            0x54, 0x541, 0x542, 0x5421, 0x543, 0x5431, 0x5432, 0x54321, 0x6, 0x61, 0x62, 
            0x621, 0x63, 0x631, 0x632, 0x6321, 0x64, 0x641, 0x642, 0x6421, 0x643, 
            0x6431, 0x6432, 0x64321, 0x65, 0x651, 0x652, 0x6521, 0x653, 0x6531, 0x6532, 
            0x65321, 0x654, 0x6541, 0x6542, 0x65421, 0x6543, 0x65431, 0x65432, 0x654321, 
            0x7, 0x71, 0x72, 0x721, 0x73, 0x731, 0x732, 0x7321, 0x74, 0x741, 0x742,
            0x7421, 0x743, 0x7431, 0x7432, 0x74321, 0x75, 0x751, 0x752, 0x7521, 0x753, 
            0x7531, 0x7532, 0x75321, 0x754, 0x7541, 0x7542, 0x75421, 0x7543, 0x75431, 
            0x75432, 0x754321, 0x76, 0x761, 0x762, 0x7621, 0x763, 0x7631, 0x7632, 
            0x76321, 0x764, 0x7641, 0x7642, 0x76421, 0x7643, 0x76431, 0x76432, 0x764321, 
            0x765, 0x7651, 0x7652, 0x76521, 0x7653, 0x76531, 0x76532, 0x765321, 0x7654, 
            0x76541, 0x76542, 0x765421, 0x76543, 0x765431, 0x765432, 0x7654321, 0x8, 
            0x81, 0x82, 0x821, 0x83, 0x831, 0x832, 0x8321, 0x84, 0x841, 0x842, 0x8421, 
            0x843, 0x8431, 0x8432, 0x84321, 0x85, 0x851, 0x852, 0x8521, 0x853, 0x8531, 
            0x8532, 0x85321, 0x854, 0x8541, 0x8542, 0x85421, 0x8543, 0x85431, 0x85432, 
            0x854321, 0x86, 0x861, 0x862, 0x8621, 0x863, 0x8631, 0x8632, 0x86321, 0x864, 
            0x8641, 0x8642, 0x86421, 0x8643, 0x86431, 0x86432, 0x864321, 0x865, 0x8651, 
            0x8652, 0x86521, 0x8653, 0x86531, 0x86532, 0x865321, 0x8654, 0x86541, 
            0x86542, 0x865421, 0x86543, 0x865431, 0x865432, 0x8654321, 0x87, 0x871, 
            0x872, 0x8721, 0x873, 0x8731, 0x8732, 0x87321, 0x874, 0x8741, 0x8742, 
            0x87421, 0x8743, 0x87431, 0x87432, 0x874321, 0x875, 0x8751, 0x8752, 0x87521, 
            0x8753, 0x87531, 0x87532, 0x875321, 0x8754, 0x87541, 0x87542, 0x875421, 
            0x87543, 0x875431, 0x875432, 0x8754321, 0x876, 0x8761, 0x8762, 0x87621, 
            0x8763, 0x87631, 0x87632, 0x876321, 0x8764, 0x87641, 0x87642, 0x876421, 
            0x87643, 0x876431, 0x876432, 0x8764321, 0x8765, 0x87651, 0x87652, 0x876521, 
            0x87653, 0x876531, 0x876532, 0x8765321, 0x87654, 0x876541, 0x876542, 
            0x8765421, 0x876543, 0x8765431, 0x8765432, unchecked((int)0x87654321)
       };

        /// <summary>
        /// Gets the number of bits sets in the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">A byte.</param>
        /// <returns>The number of bits.</returns>
        public static int BitCount(byte value)
        {
            return BYTE_COUNTS[value & 0xFF];
        }


        /// <summary>
        /// Gets the number of bits which are encoded in <paramref name="value"/> .
        /// </summary>
        /// <remarks>
        ///     <list>
        ///     <item>
        ///         <code>(i >>> (4 * n)) & 0x0F</code> is the offset of the n-th set bit of the given 
        ///         byte. For example <see cref="GetBiteList"/>(12) returns 0x43.
        ///     </item>
        ///     <item>
        ///         <code>0x43 & 0x0F</code> is 3, meaning the the first bit set is at offset 3-1 = 2.
        ///     </item>
        ///     <item>
        ///         <code>(0x43 >>> 4) & 0x0F</code> is 4, meaning there is a second bit set at offset 4-1=3.
        ///     </item>
        ///     <item>
        ///         <code>(0x43 >>> 8) & 0x0F</code> is 0, meaning there is no more bit set in this byte.
        ///     </item>
        ///     </para>
        /// </remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int BitList(byte value)
        {
            return BIT_LISTS[value & 0xFF];
        }


       
        /// <summary>
        /// Returns the number of set bits in an array of <see cref="System.Int34"/>.
        /// </summary>
        /// <param name="array">An array of <see cref="System.Int64"/></param>
        /// <param name="wordOffset">The offset position.</param>
        /// <param name="numberOfWords">The number of words, usually the length of the first array.</param>
        /// <returns>The number of set bits.</returns>
        public static long PopArray(long[] array, int wordOffset, int numberOfWords)
        {
            long count = 0;
            int length = wordOffset + numberOfWords;
            for (int i = wordOffset; i < length; ++i)
            {
                count += BitCount(array[i]);
            }
            return count;
        }

        /// <summary>
        /// Returns the popcount or cardinality of the two sets, <paramref name="left"/> &amp; <paramref name="right" />,
        /// after an intersection.
        /// Neither array is modified.
        /// </summary>
        /// <param name="left">The array of <see cref="System.Int64"/> on the left side of the statement.</param>
        /// <param name="right">The array of <see cref="System.Int64"/> on the right side of the statement.</param>
        /// <param name="wordOffset">The offset position.</param>
        /// <param name="numberOfWords">The number of words, usually the length of the first array.</param>
        /// <returns>The number of set bits.</returns>
        public static long PopIntersect(long[] left, long[] right, int wordOffset, int numWords)
        {
            long popCount = 0;
            for (int i = wordOffset, end = wordOffset + numWords; i < end; ++i)
            {
                popCount += BitCount(left[i] & right[i]);
            }
            return popCount;
        }

        /// <summary>
        /// Returns the popcount or cardinality of the union of two sets, <paramref name="left"/> | <paramref name="right" />.
        /// Neither array is modified.
        /// </summary>
        /// <param name="left">The array of <see cref="System.Int64"/> on the left side of the statement.</param>
        /// <param name="right">The array of <see cref="System.Int64"/> on the right side of the statement.</param>
        /// <param name="wordOffset">The offset position.</param>
        /// <param name="numberOfWords">The number of words, usually the length of the first array.</param>
        /// <returns>The number of set bits.</returns>
        public static long PopUnion(long[] left, long[] right, int wordOffset, int numWords)
        {
            long popCount = 0;
            for (int i = wordOffset, end = wordOffset + numWords; i < end; ++i)
            {
                popCount += BitCount(left[i] | right[i]);
            }
            return popCount;
        }

        /// <summary>
        /// Returns the popcount or cardinality of <paramref name="left" /> &amp; ~<paramref name="right" />
        ///  Neither array is modified.
        /// </summary>
        /// <param name="left">The array of <see cref="System.Int64"/> on the left side of the statement.</param>
        /// <param name="right">The array of <see cref="System.Int64"/> on the right side of the statement.</param>
        /// <param name="wordOffset">The offset position.</param>
        /// <param name="numberOfWords">The number of words, usually the length of the first array.</param>
        /// <returns>The number of set bits.</returns>
        public static long PopAndNot(long[] left, long[] right, int wordOffset, int numWords)
        {
            long popCount = 0;
            for (int i = wordOffset, end = wordOffset + numWords; i < end; ++i)
            {
                popCount += BitCount(left[i] & ~right[i]);
            }
            return popCount;
        }

        /// <summary>
        /// Returns the popcount or cardinality of <paramref name="left" /> ^ <paramref name="right" />. 
        /// Neither array is modified.
        /// </summary>
        /// <param name="left">The array of <see cref="System.Int64"/> on the left side of the statement.</param>
        /// <param name="right">The array of <see cref="System.Int64"/> on the right side of the statement.</param>
        /// <param name="wordOffset">The offset position.</param>
        /// <param name="numberOfWords">The number of words, usually the length of the first array.</param>
        /// <returns>The number of set bits.</returns>
        public static long PopXor(long[] left, long[] right, int wordOffset, int numWords)
        {
         
            long popCount = 0;
            for (int i = wordOffset, end = wordOffset + numWords; i < end; ++i)
            {
                popCount += BitCount(left[i] ^ right[i]);
            }
            return popCount;
        }

        /// <summary>
        /// Returns the next highest power of two or the 
        /// current value if it's already a power of two or zero.
        /// </summary>
        /// <param name="value">The value used to find the next power of two.</param>
        /// <returns>The next hightest power of two or current value.</returns>
        public static int NextHighestPowerOfTwo(int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;
            return value;
        }

        /// <summary>
        /// Returns the next highest power of two or the current value if it's already a power of two or zero. 
        /// </summary>
        /// <param name="value">The value used to find the next power of two.</param>
        /// <returns>The next hightest power of two or current value.</returns>
        public static long NextHighestPowerOfTwo(long value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value |= value >> 32;
            value++;
            return value;
        }

        /** Decode an int previously encoded with {@link #zigZagEncode(int)}. */
        public static int ZigZagDecode(int value) 
        {
            return (int)(((uint)value >> 1) ^ -(value & 1));
        }

        /** Decode a long previously encoded with {@link #zigZagEncode(long)}. */
        public static long zigZagDecode(long value) 
        {
            return ((value >>> 1) ^ -(value & 1));
        }

        /// <summary>
        /// Zig-zag encode the value.  
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     <ahref="https://developers.google.com/protocol-buffers/docs/encoding#types">Zig-zag</a>
        ///     encode the provided long. Assuming the input is a signed long whose
        ///     absolute value can be stored on <tt>n</tt> bits, the returned value will
        ///     be an unsigned long that can be stored on <tt>n+1</tt> bits.
        ///     </para>
        /// </remarks>
        /// <param name="value">The value that will be encoded.</param>
        /// <returns>The encoded value.</returns>
        public static int ZigZagEncode(int value)
        {
            return (value >> 31) ^ (value << 1);
        }

        /// <summary>
        /// Zig-zag encode the value.  
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     <ahref="https://developers.google.com/protocol-buffers/docs/encoding#types">Zig-zag</a>
        ///     encode the provided long. Assuming the input is a signed long whose
        ///     absolute value can be stored on <tt>n</tt> bits, the returned value will
        ///     be an unsigned long that can be stored on <tt>n+1</tt> bits.
        ///     </para>
        /// </remarks>
        /// <param name="value">The value that will be encoded.</param>
        /// <returns>The encoded value.</returns>
        public static long zigZagEncode(long value)
        {
            return (value >> 63) ^ (value << 1);
        }

        /// <summary>
        /// Alternative for Java's Long.BitCount
        /// </summary>
        /// <param name="value">The value to be inspected.</param>
        /// <returns></returns>
        /// <seealso href="http://docs.oracle.com/javase/6/docs/api/java/lang/Long.html#bitCount(long)"/>
        private static long BitCount(long value)
        {
            return SparseBitCount(value);
        }
        

        private static long SparseBitCount(long value)
        {
            long count = 0;
            while(value != 0)
            {
                count++;
                value &= (value - 1);
            }

            return count;
        }
       
    }
}
