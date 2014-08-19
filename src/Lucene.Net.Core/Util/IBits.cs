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
    /// Interface for Bitset-like structures.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Java <see href="https://github.com/apache/lucene-solr/blob/trunk/lucene/core/src/test/org/apache/lucene/util/Bits.java">Source</see>
    ///         .NET does not support classes in interfaces, so the nested classes have been split off into it's own class <seealso cref="Bits"/>
    ///     </para>
    /// </remarks>
    /// <seealso cref="Bits"/>
    // ReSharper disable CSharpWarnings::CS1574
    public interface IBits
    {
        /// <summary>
        /// Returns the number of bits in the set.
        /// </summary>
	    int Length { get; }

        /// <summary>
        /// Gets the value of the bit at the specified index.
        /// </summary>
        /// <param name="index">Zero based index position.  The index value should be non-negative</param>
        /// <returns>True, if the bit is set, otherwise, false.</returns>
        /// <exception cref="System.IndexOutOfRangeException">Throws when the index is negative or exceeds the length</exception>
        bool this[int index] { get; }
    }
}