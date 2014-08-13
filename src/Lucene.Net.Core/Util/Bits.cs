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
    /// Bits contains the classes for <see cref="MatchAllBits"/> and  <see cref="MatchNoBits"/> and the value of an empty array of <see cref="IBits"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Java <see href="https://github.com/apache/lucene-solr/blob/trunk/lucene/core/src/test/org/apache/lucene/util/Bits.java">Source</see>
    ///         .NET does not support classes in interfaces, so the interface was refactored to <seealso cref="IBits"/>
    ///     </para>
    /// </remarks>
    /// <seealso cref="IBits"/>
    public static class Bits
    {
        public static readonly IBits[] EMPTY_ARRAY = new IBits[0];

        /// <summary>
        /// Returns true for all bits.
        /// </summary>
        public class MatchAllBits : IBits
        {
            private readonly int length;

            /// <summary>
            /// Initializes a new instance of <see cref="MatchAllBits"/>
            /// </summary>
            /// <param name="length">The number of bits in the set.</param>
            public MatchAllBits(int length)
            {
                this.length = length;
            }

            /// <summary>
            /// Gets the value of the bit at the specified index.
            /// </summary>
            /// <param name="index">Zero based index position.  The index value should be non-negative</param>
            /// <returns>True, if the bit is set, otherwise, false.</returns>
            /// <exception cref="System.IndexOutOfRangeException">Throws when the index is negative or exceeds the length</exception>
            public bool this[int index]
            {
                get
                {
                    Check.InRangeOfLength("index", index, this.Length);

                    return true;
                }
            }

            /// <summary>
            /// Returns the number of bits in the set.
            /// </summary>
            public int Length
            {
                get { return this.length; }
            }
        }

        /// <summary>
        /// Returns false for all bits.
        /// </summary>
        public class MatchNoBits : IBits
        {
            private readonly int length;

            /// <summary>
            /// Initializes a new instance of <see cref="MatchAllBits"/>
            /// </summary>
            /// <param name="length">The number of bits in the set.</param>
            public MatchNoBits(int length)
            {
                this.length = length;
            }

            /// <summary>
            /// Gets the value of the bit at the specified index.
            /// </summary>
            /// <param name="index">Zero based index position.  The index value should be non-negative</param>
            /// <returns>True, if the bit is set, otherwise, false.</returns>
            /// <exception cref="System.IndexOutOfRangeException">Throws when the index is negative or exceeds the length</exception>
            public bool this[int index]
            {
                get
                {
                    Check.InRangeOfLength("index", index, this.Length);

                    return true;
                }
            }

            /// <summary>
            /// Returns the number of bits in the set.
            /// </summary>
            public int Length
            {
                get { return this.length; }
            }
        }
    }
}