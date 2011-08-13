// -----------------------------------------------------------------------
// <copyright company="Apache" file="ICharTermAttribute.cs">
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


namespace Lucene.Net.Analysis.TokenAttributes
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using Util;


    /// <summary>
    /// The term text of a Token.
    /// </summary>
    /// <remarks>
    ///     <note>
    ///         <para>
    ///             <b>Java File: </b> <a href="https://github.com/apache/lucene-solr/blob/trunk/lucene/src/java/org/apache/lucene/analysis/tokenattributes/CharTermAttribute.java">
    ///             lucene/src/java/org/apache/lucene/analysis/tokenattributes/CharTermAttribute.java
    ///         </a>
    ///         </para>
    ///         <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Analysis/TokenAttributes/ICharTermAttribute.cs">
    ///              src/Lucene.Net/Analysis/TokenAttributes/ICharTermAttribute.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "The class was called Attribute in Java. It would be fun to call it Annotation. However, " +
        "its probably best to try to honor the correlating names when possible.")]
    public interface ICharTermAttribute : IAttribute
    {
        /// <summary>
        /// Gets the internal termBuffer character array which you can then
        /// directly alter.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If the array is too small for the token, use <see cref="SetLength(int)"/>
        ///         to increase the size. After altering the buffer be sure to call
        ///         <see cref="SetLength(int)"/> to record the valid characters that 
        ///         were placed into the termBuffer.
        ///     </para>
        /// </remarks>
        /// <value>The buffer.</value>
        IEnumerable<char> Buffer { get; }

        /// <summary>
        /// Gets or sets the number of valid characters, the length of the term, in
        /// the termBuffer array.  
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Use this to truncate the termBuffer or to synchronize any external 
        ///         manipulation of the termBuffer.
        ///     </para>
        ///     <note>
        ///         To grow the size of the array, use <see cref="ResizeBuffer(int)"/> first.
        ///     </note>
        /// </remarks>
        int Length { get; set; }

        /// <summary>
        /// Appends the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        ICharTermAttribute Append(char value);

        /// <summary>
        /// Appends the specified <see cref="string"/> to internal buffer or character sequence.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        ICharTermAttribute Append(string value);


        /// <summary>
        ///     Appends the specified <see cref="string"/> to internal buffer or character sequence.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="startingIndex">the index of string to start the copy.</param>
        /// <param name="length">The length of the string that is to be copied.</param>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        ICharTermAttribute Append(string value, int startingIndex, int length);

        /// <summary>
        /// Appends the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        ICharTermAttribute Append(StringBuilder value);

        /// <summary>
        /// Appends the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        ICharTermAttribute Append(ICharTermAttribute value);

        /// <summary>
        /// Copies the contents of the buffer, starting at the offset to the specified length.
        /// </summary>
        /// <param name="buffer">The buffer to copy.</param>
        /// <param name="offset">The index of the first character to copy inside the buffer.</param>
        /// <param name="length">The number of characters to copy, if -1 the length will default to the buffer length.</param>
        void CopyBuffer(char[] buffer, int offset = 0, int length = -1);


        /// <summary>
        /// Sets the length of the internal buffer to zero. User this 
        /// method before appending content using <c>Append</c> methods.
        /// </summary>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        ICharTermAttribute Empty();


        /// <summary>
        /// Resizes the buffer.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>An instance of <see cref="IEnumerable{T}"/>.</returns>
        IEnumerable<char> ResizeBuffer(int length);


        /// <summary>
        /// Gets or sets the number of valid characters, the length of the term, in
        /// the termBuffer array.  
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Use this to truncate the termBuffer or to synchronize any external 
        ///         manipulation of the termBuffer.
        ///     </para>
        ///     <note>
        ///         To grow the size of the array, use <see cref="ResizeBuffer(int)"/> first.
        ///     </note>
        /// </remarks>
        /// <param name="length">The length.</param>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        ICharTermAttribute SetLength(int length);
    }
}