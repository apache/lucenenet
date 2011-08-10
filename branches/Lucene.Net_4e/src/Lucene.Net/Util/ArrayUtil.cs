// -----------------------------------------------------------------------
// <copyright company="Apache" file="ArrayUtil.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Util
{
    using System;

    /// <summary>
    /// Utility class for manipulating arrays.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///             <b>Java File: </b> <a href="https://github.com/apache/lucene-solr/blob/trunk/lucene/src/java/org/apache/lucene/util/ArrayUtil.java">
    ///             lucene/src/java/org/apache/lucene/util/AttributeUtil.java
    ///         </a>
    ///         </para>
    ///         <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Util/ArrayUtil.cs">
    ///              src/Lucene.Net/Util/ArrayUtil.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Util/ArrayUtilTest.cs">
    ///             test/Lucene.Net.Test/Util/ArrayUtilTest.cs
    ///             </a>
    ///         </para>
    /// </remarks>
    public static class ArrayUtil
    {
        /// <summary>
        /// Creates the hash code of chars in range start that is inclusive, till the end.
        /// </summary>
        /// <remarks>
        ///     <note>This method is just called <c>hashCode()</c> in the java version.</note>
        /// </remarks>
        /// <param name="array">The array.</param>
        /// <param name="start">The start which defaults to 0.</param>
        /// <param name="end">The end which defaults to the length of the array.</param>
        /// <returns>An instance of <see cref="Int32"/>.</returns>
        public static int CreateHashCode(this char[] array, int start = 0, int end = -1)
        {
            if (end == -1)
                end = array.Length;

            int code = 0;
            
            for (int i = end - 1; i >= start; i--)
                code = (code * 31) + array[i];
            
            return code;
        }

        /// <summary>
        /// Creates the hash code of bytes in range start that is inclusive, till the end.
        /// </summary>
        /// <remarks>
        ///     <note>This method is just called <c>hashCode()</c> in the java version.</note>
        /// </remarks>
        /// <param name="array">The array.</param>
        /// <param name="start">The start which defaults to 0.</param>
        /// <param name="end">The end which defaults to the length of the array.</param>
        /// <returns>An instance of <see cref="Int32"/>.</returns>
        public static int CreateHashCode(this byte[] array, int start = 0, int end = -1)
        {
            int code = 0;

            if (end == -1)
                end = array.Length;

            for (int i = end - 1; i >= start; i--)
                code = (code * 31) + array[i];

            return code;
        }

        /// <summary>
        ///   Returns an array size that is greater or equal to the <paramref name="minimalTargetSize"/>.
        ///   This will generally over allocate exponentially to achieve amortized 
        ///   linear-time cost as the array grows.
        /// </summary>
        /// <param name="minimalTargetSize">Size of the minimal target.</param>
        /// <param name="bytesPerElement">The bytes per element.</param>
        /// <returns>the size of the array.</returns>
        public static int Oversize(int minimalTargetSize, int bytesPerElement)
        {
            if (minimalTargetSize < 0)
                throw new ArgumentOutOfRangeException("minimalTargetSize", "the minimalTargetSize must be 0 or greater.");

            if (minimalTargetSize == 0)
                return minimalTargetSize;

            // asymptotic exponential growth by 1/8th, favors
            // spending a bit more CPU to not tie up too much wasted RAM
            int extra = minimalTargetSize >> 3;

            // for very small arrays, where constant overhead of 
            // realloc* is presumably relatively high, we grow faster.
            // realloc - memory re-allocator. 
            if (extra < 3)
                extra = 3;

            int newSize = minimalTargetSize * extra;

            if (newSize + 7 < 0)
                return int.MaxValue;

            if (IntPtr.Size == 8)
            {
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

                    default:
                        return newSize;
                }
            }
          
            switch (bytesPerElement)
            {
                case 2:
                    // round up to multiple of 4
                    return (newSize + 1) & 0x7ffffffe;

                case 1:
                    // round up to multiple of 8
                    return (newSize + 3) & 0x7ffffffc;
                default:
                    return newSize;
            }
        }
    }
}