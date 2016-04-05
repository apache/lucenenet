using System;
using System.Text;

namespace Lucene.Net.Analysis.Tokenattributes
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
    /// The term text of a Token.
    /// </summary>
    public interface ICharTermAttribute : IAttribute
    {
        /// <summary>
        /// Copies the contents of buffer, starting at offset for
        ///  length characters, into the termBuffer array. </summary>
        ///  <param name="buffer"> the buffer to copy </param>
        ///  <param name="offset"> the index in the buffer of the first character to copy </param>
        ///  <param name="length"> the number of characters to copy </param>
        void CopyBuffer(char[] buffer, int offset, int length);

        /// <summary>
        /// Returns the internal termBuffer character array which
        ///  you can then directly alter.  If the array is too
        ///  small for your token, use {@link
        ///  #resizeBuffer(int)} to increase it.  After
        ///  altering the buffer be sure to call {@link
        ///  #setLength} to record the number of valid
        ///  characters that were placed into the termBuffer.
        ///  <p>
        ///  <b>NOTE</b>: The returned buffer may be larger than
        ///  the valid <seealso cref="#length()"/>.
        /// </summary>
        char[] Buffer();

        /// <summary>
        /// Grows the termBuffer to at least size newSize, preserving the
        ///  existing content. </summary>
        ///  <param name="newSize"> minimum size of the new termBuffer </param>
        ///  <returns> newly created termBuffer with length >= newSize </returns>
        char[] ResizeBuffer(int newSize);

        int Length { get; set; }

        /// <summary>
        /// Set number of valid characters (length of the term) in
        ///  the termBuffer array. Use this to truncate the termBuffer
        ///  or to synchronize with external manipulation of the termBuffer.
        ///  Note: to grow the size of the array,
        ///  use <seealso cref="#resizeBuffer(int)"/> first. </summary>
        ///  <param name="length"> the truncated length </param>
        ICharTermAttribute SetLength(int length);

        /// <summary>
        /// Sets the length of the termBuffer to zero.
        /// Use this method before appending contents
        /// using the <seealso cref="Appendable"/> interface.
        /// </summary>
        ICharTermAttribute SetEmpty();

        // the following methods are redefined to get rid of IOException declaration:
        ICharTermAttribute Append(string csq, int start, int end);

        ICharTermAttribute Append(char c);

        /// <summary>
        /// Appends the specified {@code String} to this character sequence.
        /// <p>The characters of the {@code String} argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is {@code null}, then the four
        /// characters {@code "null"} are appended.
        /// </summary>
        ICharTermAttribute Append(string s);

        /// <summary>
        /// Appends the specified {@code StringBuilder} to this character sequence.
        /// <p>The characters of the {@code StringBuilder} argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is {@code null}, then the four
        /// characters {@code "null"} are appended.
        /// </summary>
        ICharTermAttribute Append(StringBuilder sb);

        /// <summary>
        /// Appends the contents of the other {@code CharTermAttribute} to this character sequence.
        /// <p>The characters of the {@code CharTermAttribute} argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is {@code null}, then the four
        /// characters {@code "null"} are appended.
        /// </summary>
        ICharTermAttribute Append(ICharTermAttribute termAtt);
    }
}