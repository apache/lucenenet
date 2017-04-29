// This interface was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

namespace Lucene.Net.Support
{
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

    /// <summary>
    /// This interface represents an ordered set of characters and defines the
    /// methods to probe them.
    /// </summary>
    public interface ICharSequence
    {
        /// <summary>
        /// Returns the number of characters in this sequence.
        /// </summary>
        int Length { get; }

        // LUCENENET specific - removed CharAt() and replaced with this[int] to .NETify
        //char CharAt(int index);

        /// <summary>
        /// Returns the character at the specified index, with the first character
        /// having index zero.
        /// </summary>
        /// <param name="index">the index of the character to return.</param>
        /// <returns>the requested character.</returns>
        /// <exception cref="System.IndexOutOfRangeException">
        /// if <c>index &lt; 0</c> or <c>index</c> is greater than the
        /// length of this sequence.
        /// </exception>
        char this[int index] { get; }

        /// <summary>
        /// Returns a <see cref="ICharSequence"/> from the <paramref name="start"/> index (inclusive)
        /// to the <paramref name="end"/> index (exclusive) of this sequence.
        /// </summary>
        /// <param name="start">
        /// the start offset of the sub-sequence. It is inclusive, that
        /// is, the index of the first character that is included in the
        /// sub-sequence.
        /// </param>
        /// <param name="end">
        /// the end offset of the sub-sequence. It is exclusive, that is,
        /// the index of the first character after those that are included
        /// in the sub-sequence
        /// </param>
        /// <returns>the requested sub-sequence.</returns>
        /// <exception cref="System.IndexOutOfRangeException">
        /// if <c>start &lt; 0</c>, <c>end &lt; 0</c>, <c>start &gt; end</c>,
        /// or if <paramref name="start"/> or <paramref name="end"/> are greater than the
        /// length of this sequence.
        /// </exception>
        ICharSequence SubSequence(int start, int end);

        /// <summary>
        /// Returns a string with the same characters in the same order as in this
        /// sequence.
        /// </summary>
        /// <returns>a string based on this sequence.</returns>
        string ToString();
    }
}