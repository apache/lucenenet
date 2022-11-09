using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis.TokenAttributes
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
    /// The start and end character offset of a <see cref="Token"/>.
    /// </summary>
    public interface IOffsetAttribute : IAttribute
    {
        /// <summary>
        /// Returns this <see cref="Token"/>'s starting offset, the position of the first character
        /// corresponding to this token in the source text.
        /// <para/>
        /// Note that the difference between <see cref="EndOffset"/> and <see cref="StartOffset"/>
        /// may not be equal to termText.Length, as the term text may have been altered by a
        /// stemmer or some other filter.
        /// </summary>
        /// <seealso cref="SetOffset(int, int)"/>
        int StartOffset { get; } // LUCENENET TODO: API - add a setter ? It seems the SetOffset only sets two properties at once...

        /// <summary>
        /// Set the starting and ending offset.
        /// </summary>
        /// <exception cref="ArgumentException"> If <paramref name="startOffset"/> or <paramref name="endOffset"/>
        ///         are negative, or if <paramref name="startOffset"/> is greater than
        ///         <paramref name="endOffset"/> </exception>
        /// <seealso cref="StartOffset"/>
        /// <seealso cref="EndOffset"/>
        void SetOffset(int startOffset, int endOffset);

        /// <summary>
        /// Returns this <see cref="Token"/>'s ending offset, one greater than the position of the
        /// last character corresponding to this token in the source text. The length
        /// of the token in the source text is (<code>EndOffset</code> - <see cref="StartOffset"/>).
        /// </summary>
        /// <seealso cref="SetOffset(int, int)"/>
        int EndOffset { get; } // LUCENENET TODO: API - add a setter ? It seems the SetOffset only sets two properties at once...
    }
}