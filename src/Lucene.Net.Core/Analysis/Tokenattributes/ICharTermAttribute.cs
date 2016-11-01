using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Text;

namespace Lucene.Net.Analysis.Tokenattributes
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
    /// The term text of a Token.
    /// </summary>
    public interface ICharTermAttribute : IAttribute, ICharSequence, ICloneable
    {
        /// <summary>
        /// Copies the contents of buffer, starting at offset for
        /// length characters, into the termBuffer array. </summary>
        /// <param name="buffer"> the buffer to copy </param>
        /// <param name="offset"> the index in the buffer of the first character to copy </param>
        /// <param name="length"> the number of characters to copy </param>
        void CopyBuffer(char[] buffer, int offset, int length);

        /// <summary>
        /// Returns the internal termBuffer character array which
        /// you can then directly alter.  If the array is too
        /// small for your token, use <see cref="ResizeBuffer(int)"/>
        /// to increase it.  After
        /// altering the buffer be sure to call <see cref="SetLength(int)"/> 
        /// to record the number of valid
        /// characters that were placed into the termBuffer.
        /// <para>
        /// <b>NOTE</b>: The returned buffer may be larger than
        /// the valid <see cref="Length"/>.
        /// </para>
        /// </summary>
        char[] Buffer();

        /// <summary>
        /// Grows the termBuffer to at least size newSize, preserving the
        /// existing content. </summary>
        /// <param name="newSize"> minimum size of the new termBuffer </param>
        /// <returns> newly created termBuffer with length >= newSize </returns>
        char[] ResizeBuffer(int newSize);

        /// <summary>
        /// Gets or Sets the number of valid characters (in
        /// the termBuffer array.
        /// <seealso cref="SetLength(int)"/>
        /// </summary>
        new int Length { get; set; } // LUCENENET: To mimic StringBuilder, we allow this to be settable.

        // LUCENENET specific: Redefining this[] to make it settable
        new char this[int index] { get; set; }

        /// <summary>
        /// Set number of valid characters (length of the term) in
        /// the termBuffer array. Use this to truncate the termBuffer
        /// or to synchronize with external manipulation of the termBuffer.
        /// Note: to grow the size of the array,
        /// use <see cref="ResizeBuffer(int)"/> first. </summary>
        /// <param name="length"> the truncated length </param>
        ICharTermAttribute SetLength(int length); 

        /// <summary>
        /// Sets the length of the termBuffer to zero.
        /// Use this method before appending contents.
        /// </summary>
        ICharTermAttribute SetEmpty();

        // the following methods are redefined to get rid of IOException declaration:

        /// <summary>
        /// Appends the contents of the <see cref="ICharSequence"/> to this character sequence.
        /// <para>
        /// The characters of the <see cref="ICharSequence"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, then the four
        /// characters <c>"null"</c> are appended.
        /// </para>
        /// </summary>
        ICharTermAttribute Append(ICharSequence csq);

        /// <summary>
        /// Appends the contents of the <see cref="ICharSequence"/> to this character sequence, beginning and ending
        /// at the specified indices.
        /// <para>
        /// The characters of the <see cref="ICharSequence"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, then the four
        /// characters <c>"null"</c> are appended.
        /// </para>
        /// </summary>
        /// <param name="csq">The index of the first character in the subsequence.</param>
        /// <param name="start">The start index of the <see cref="ICharSequence"/> to begin copying characters.</param>
        /// <param name="end">The index of the character following the last character in the subsequence.</param>
        ICharTermAttribute Append(ICharSequence csq, int start, int end);

        /// <summary>
        /// Appends the supplied <see cref="char"/> to this character sequence.
        /// </summary>
        /// <param name="c">The <see cref="char"/> to append.</param>
        ICharTermAttribute Append(char c);

        /// <summary>
        /// Appends the contents of the <see cref="char[]"/> array to this character sequence.
        /// <para>
        /// The characters of the <see cref="char[]"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, then the four
        /// characters <c>"null"</c> are appended.
        /// </para>
        /// </summary>
        /// <param name="csq">The <see cref="char[]"/> array to append.</param>
        /// <remarks>
        /// LUCENENET specific method, added to simulate using the CharBuffer class in Java.
        /// </remarks>
        ICharTermAttribute Append(char[] csq);

        /// <summary>
        /// Appends the contents of the <see cref="char[]"/> array to this character sequence, beginning and ending
        /// at the specified indices.
        /// <para>
        /// The characters of the <see cref="char[]"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, then the four
        /// characters <c>"null"</c> are appended.
        /// </para>
        /// </summary>
        /// <param name="csq">The <see cref="char[]"/> array to append.</param>
        /// <param name="start">The start index of the <see cref="char[]"/> to begin copying characters.</param>
        /// <param name="end">The index of the character following the last character in the subsequence.</param>
        /// <remarks>
        /// LUCENENET specific method, added to simulate using the CharBuffer class in Java. Note that
        /// the <see cref="CopyBuffer(char[], int, int)"/> method provides similar functionality, except for
        /// the last argument of this method is an index of the array rather than the length of characters to copy.
        /// </remarks>
        ICharTermAttribute Append(char[] csq, int start, int end);

        /// <summary>
        /// Appends the specified <see cref="string"/> to this character sequence.
        /// <para>
        /// The characters of the <see cref="string"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, then the four
        /// characters <c>"null"</c> are appended.
        /// </para>
        /// </summary>
        /// <remarks>
        /// LUCENENET specific method, added because the .NET <see cref="string"/> data type 
        /// doesn't implement <see cref="ICharSequence"/>. 
        /// </remarks>
        ICharTermAttribute Append(string s);

        /// <summary>
        /// Appends the contents of the <see cref="string"/> to this character sequence, beginning and ending
        /// at the specified indices.
        /// <para>
        /// The characters of the <see cref="string"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, then the four
        /// characters <c>"null"</c> are appended.
        /// </para>
        /// </summary>
        /// <param name="csq">The index of the first character in the subsequence.</param>
        /// <param name="start">The start index of the <see cref="string"/> to begin copying characters.</param>
        /// <param name="end">The index of the character following the last character in the subsequence.</param>
        /// <remarks>
        /// LUCENENET specific method, added because the .NET <see cref="string"/> data type 
        /// doesn't implement <see cref="ICharSequence"/>. 
        /// </remarks>
        ICharTermAttribute Append(string csq, int start, int end);


        /// <summary>
        /// Appends the specified <see cref="StringBuilder"/> to this character sequence.
        /// <para>
        /// The characters of the <see cref="StringBuilder"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, then the four
        /// characters <c>"null"</c> are appended.
        /// </para>
        /// </summary>
        ICharTermAttribute Append(StringBuilder sb);

        /// <summary>
        /// Appends the specified <see cref="StringBuilder"/> to this character sequence.
        /// <p>The characters of the <see cref="StringBuilder"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, then the four
        /// characters <c>"null"</c> are appended.
        /// </summary>
        /// <remarks>
        /// LUCENENET specific method, added because the .NET <see cref="StringBuilder"/> data type 
        /// doesn't implement <see cref="ICharSequence"/>.
        /// </remarks>
        ICharTermAttribute Append(StringBuilder s, int start, int end);

        /// <summary>
        /// Appends the contents of the other <see cref="ICharTermAttribute"/> to this character sequence.
        /// <p>The characters of the <see cref="ICharTermAttribute"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, then the four
        /// characters <c>"null"</c> are appended.
        /// </summary>
        ICharTermAttribute Append(ICharTermAttribute termAtt);
    }
}