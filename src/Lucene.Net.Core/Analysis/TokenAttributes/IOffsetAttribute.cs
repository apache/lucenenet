namespace Lucene.Net.Analysis.TokenAttributes
{
    using Lucene.Net.Util;

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
    /// The start and end character offset of a Token.
    /// </summary>
    public interface IOffsetAttribute : IAttribute
    {
        /// <summary>
        /// Returns this Token's starting offset, the position of the first character
        /// corresponding to this token in the source text.
        /// <p>
        /// Note that the difference between <seealso cref="#EndOffset()"/> and <code>StartOffset()</code>
        /// may not be equal to termText.length(), as the term text may have been altered by a
        /// stemmer or some other filter. </summary>
        /// <seealso cref= #SetOffset(int, int)  </seealso>
        int StartOffset { get; }

        /// <summary>
        /// Set the starting and ending offset. </summary>
        /// <exception cref="IllegalArgumentException"> If <code>startOffset</code> or <code>endOffset</code>
        ///         are negative, or if <code>startOffset</code> is greater than
        ///         <code>endOffset</code> </exception>
        /// <seealso cref= #StartOffset() </seealso>
        /// <seealso cref= #EndOffset() </seealso>
        void SetOffset(int startOffset, int endOffset);

        /// <summary>
        /// Returns this Token's ending offset, one greater than the position of the
        /// last character corresponding to this token in the source text. The length
        /// of the token in the source text is (<code>EndOffset()</code> - <seealso cref="#StartOffset()"/>). </summary>
        /// <seealso cref= #SetOffset(int, int) </seealso>
        int EndOffset { get; }
    }
}