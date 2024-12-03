﻿using J2N.Text;
using Lucene.Net.Analysis.TokenAttributes.Extensions;
using Lucene.Net.Util;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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
    /// The term text of a <see cref="Token"/>.
    /// </summary>
    public interface ICharTermAttribute : IAttribute, ICharSequence, IAppendable
    {
        /// <summary>
        /// Copies the contents of buffer, starting at offset for
        /// length characters, into the termBuffer array.
        /// </summary>
        /// <param name="buffer"> the buffer to copy </param>
        /// <param name="offset"> the index in the buffer of the first character to copy </param>
        /// <param name="length"> the number of characters to copy </param>
        void CopyBuffer(char[] buffer, int offset, int length);

        /// <summary>
        /// Returns the internal termBuffer character array which
        /// you can then directly alter.  If the array is too
        /// small for your token, use <see cref="ResizeBuffer(int)"/>
        /// to increase it.  After
        /// altering the buffer be sure to set <see cref="Length"/>
        /// to record the number of valid
        /// characters that were placed into the termBuffer.
        /// <para>
        /// <b>NOTE</b>: The returned buffer may be larger than
        /// the valid <see cref="Length"/>.
        /// </para>
        /// </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Lucene's design requires some array properties")]
        char[] Buffer { get; }

        /// <summary>
        /// Grows the termBuffer to at least size <paramref name="newSize"/>, preserving the
        /// existing content. </summary>
        /// <param name="newSize"> minimum size of the new termBuffer </param>
        /// <returns> newly created termBuffer with length >= newSize </returns>
        char[] ResizeBuffer(int newSize);

        /// <summary>
        /// Gets or sets the number of valid characters (length of the term) in
        /// the termBuffer array.
        /// Use this setter to truncate the termBuffer
        /// or to synchronize with external manipulation of the termBuffer.
        /// Note: to grow the size of the array,
        /// use <see cref="ResizeBuffer(int)"/> first.
        /// </summary>
        /// <remarks>
        /// LUCENENET: To mimic StringBuilder, we allow this to be settable.
        /// The setter may be used as an alternative to
        /// <see cref="CharTermAttributeExtensions.SetLength{T}(T, int)"/> if
        /// chaining is not required.
        /// </remarks>
        /// <seealso cref="CharTermAttributeExtensions.SetLength{T}(T, int)"/>
        new int Length { get; set; }

        // LUCENENET specific: Redefining this[] to make it settable
        new char this[int index] { get; set; }

        /// <summary>
        /// Clears the values in this attribute and resets it to its
        /// default value.
        /// </summary>
        /// <remarks>
        /// LUCENENET specific - This method is not part of the Java Lucene API.
        /// This was added to be a more consistent way to clear attributes than SetEmpty().
        /// </remarks>
        /// <seealso cref="CharTermAttributeExtensions.SetEmpty{T}(T)"/>
        void Clear();

        /// <summary>
        /// Appends the contents of the <see cref="ICharSequence"/> to this character sequence.
        /// <para/>
        /// The characters of the <see cref="ICharSequence"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If <paramref name="value"/> is <c>null</c>, this method is a no-op.
        /// <para/>
        /// IMPORTANT: This method uses .NET semantics. In Lucene, a <c>null</c> <paramref name="value"/> would append the
        /// string <c>"null"</c> to the instance, but in Lucene.NET a <c>null</c> value will be ignored.
        /// </summary>
        new ICharTermAttribute Append(ICharSequence value);

        /// <summary>
        /// Appends the a string representation of the specified <see cref="ICharSequence"/> to this instance.
        /// <para>
        /// The characters of the <see cref="ICharSequence"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of <paramref name="count"/>. If <paramref name="value"/> is <c>null</c>
        /// and <paramref name="startIndex"/> and <paramref name="count"/> are not 0, an
        /// <see cref="ArgumentNullException"/> is thrown.
        /// </para>
        /// </summary>
        /// <param name="value">The sequence of characters to append.</param>
        /// <param name="startIndex">The start index of the <see cref="ICharSequence"/> to begin copying characters.</param>
        /// <param name="count">The number of characters to append.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>, and
        /// <paramref name="startIndex"/> and <paramref name="count"/> are not zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is less than zero.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="startIndex"/> is less than zero.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="startIndex"/> + <paramref name="count"/> is greater than the length of <paramref name="value"/>.
        /// </exception>
        new ICharTermAttribute Append(ICharSequence value, int startIndex, int count); // LUCENENET: changed to startIndex/length to match .NET

        /// <summary>
        /// Appends the supplied <see cref="char"/> to this character sequence.
        /// </summary>
        /// <param name="value">The <see cref="char"/> to append.</param>
        new ICharTermAttribute Append(char value);

        /// <summary>
        /// Appends the contents of the <see cref="T:char[]"/> array to this character sequence.
        /// <para/>
        /// The characters of the <see cref="T:char[]"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the <paramref name="value"/>.
        /// If <paramref name="value"/> is <c>null</c>, this method is a no-op.
        /// <para/>
        /// This method uses .NET semantics. In Lucene, a <c>null</c> <paramref name="value"/> would append the
        /// string <c>"null"</c> to the instance, but in Lucene.NET a <c>null</c> value will be safely ignored.
        /// </summary>
        /// <param name="value">The <see cref="T:char[]"/> array to append.</param>
        /// <remarks>
        /// LUCENENET specific method, added to simulate using the CharBuffer class in Java.
        /// </remarks>
        new ICharTermAttribute Append(char[] value);

        /// <summary>
        /// Appends the string representation of the <see cref="T:char[]"/> array to this instance.
        /// <para>
        /// The characters of the <see cref="T:char[]"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the <paramref name="value"/>. If <paramref name="value"/> is <c>null</c>
        /// and <paramref name="startIndex"/> and <paramref name="count"/> are not 0, an
        /// <see cref="ArgumentNullException"/> is thrown.
        /// </para>
        /// </summary>
        /// <param name="value">The sequence of characters to append.</param>
        /// <param name="startIndex">The start index of the <see cref="T:char[]"/> to begin copying characters.</param>
        /// <param name="count">The number of characters to append.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>, and
        /// <paramref name="startIndex"/> and <paramref name="count"/> are not zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is less than zero.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="startIndex"/> is less than zero.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="startIndex"/> + <paramref name="count"/> is greater than the length of <paramref name="value"/>.
        /// </exception>
        /// <remarks>
        /// LUCENENET specific method, added to simulate using the CharBuffer class in Java. Note that
        /// the <see cref="CopyBuffer(char[], int, int)"/> method provides similar functionality.
        /// </remarks>
        new ICharTermAttribute Append(char[] value, int startIndex, int count); // LUCENENET: changed to startIndex/length to match .NET

        /// <summary>
        /// Appends the specified <see cref="string"/> to this character sequence.
        /// <para>
        /// The characters of the <see cref="string"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, this method is a no-op.
        /// <para/>
        /// This method uses .NET semantics. In Lucene, a <c>null</c> <paramref name="value"/> would append the
        /// string <c>"null"</c> to the instance, but in Lucene.NET a <c>null</c> value will be safely ignored.
        /// </para>
        /// </summary>
        /// <param name="value">The sequence of characters to append.</param>
        /// <remarks>
        /// LUCENENET specific method, added because the .NET <see cref="string"/> data type
        /// doesn't implement <see cref="ICharSequence"/>.
        /// </remarks>
        new ICharTermAttribute Append(string value);

        /// <summary>
        /// Appends the contents of the <see cref="string"/> to this character sequence.
        /// <para>
        /// The characters of the <see cref="string"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of <paramref name="value"/>. If <paramref name="value"/> is <c>null</c>
        /// and <paramref name="startIndex"/> and <paramref name="count"/> are not 0, an
        /// <see cref="ArgumentNullException"/> is thrown.
        /// </para>
        /// </summary>
        /// <param name="value">The sequence of characters to append.</param>
        /// <param name="startIndex">The start index of the <see cref="string"/> to begin copying characters.</param>
        /// <param name="count">The number of characters to append.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>, and
        /// <paramref name="startIndex"/> and <paramref name="count"/> are not zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is less than zero.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="startIndex"/> is less than zero.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="startIndex"/> + <paramref name="count"/> is greater than the length of <paramref name="value"/>.
        /// </exception>
        /// <remarks>
        /// LUCENENET specific method, added because the .NET <see cref="string"/> data type
        /// doesn't implement <see cref="ICharSequence"/>.
        /// </remarks>
        new ICharTermAttribute Append(string value, int startIndex, int count); // LUCENENET TODO: API - change to startIndex/length to match .NET


        /// <summary>
        /// Appends a string representation of the specified <see cref="StringBuilder"/> to this character sequence.
        /// <para>
        /// The characters of the <see cref="StringBuilder"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, this method is a no-op.
        /// <para/>
        /// This method uses .NET semantics. In Lucene, a <c>null</c> <paramref name="value"/> would append the
        /// string <c>"null"</c> to the instance, but in Lucene.NET a <c>null</c> value will be safely ignored.
        /// </para>
        /// </summary>
        new ICharTermAttribute Append(StringBuilder value);

        /// <summary>
        /// Appends a string representation of the specified <see cref="StringBuilder"/> to this character sequence.
        /// <para/>The characters of the <see cref="StringBuilder"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If <paramref name="value"/> is <c>null</c>
        /// and <paramref name="startIndex"/> and <paramref name="count"/> are not 0, an
        /// <see cref="ArgumentNullException"/> is thrown.
        /// </summary>
        /// <param name="value">The sequence of characters to append.</param>
        /// <param name="startIndex">The start index of the <see cref="StringBuilder"/> to begin copying characters.</param>
        /// <param name="count">The number of characters to append.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>, and
        /// <paramref name="startIndex"/> and <paramref name="count"/> are not zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is less than zero.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="startIndex"/> is less than zero.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="startIndex"/> + <paramref name="count"/> is greater than the length of <paramref name="value"/>.
        /// </exception>
        /// <remarks>
        /// LUCENENET specific method, added because the .NET <see cref="StringBuilder"/> data type
        /// doesn't implement <see cref="ICharSequence"/>.
        /// </remarks>
        new ICharTermAttribute Append(StringBuilder value, int startIndex, int count);

        /// <summary>
        /// Appends the contents of the other <see cref="ICharTermAttribute"/> to this character sequence.
        /// <para/>The characters of the <see cref="ICharTermAttribute"/> argument are appended, in order, increasing the length of
        /// this sequence by the length of the argument. If argument is <c>null</c>, this method is a no-op.
        /// <para/>
        /// This method uses .NET semantics. In Lucene, a <c>null</c> <paramref name="value"/> would append the
        /// string <c>"null"</c> to the instance, but in Lucene.NET a <c>null</c> value will be safely ignored.
        /// </summary>
        /// <param name="value">The sequence of characters to append.</param>
        ICharTermAttribute Append(ICharTermAttribute value);
    }
}
