// -----------------------------------------------------------------------
// <copyright company="Apache" file="CharTermAttribute.cs">
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
    using System;
    using System.Text;
    using Util;

    /// <summary>
    /// The term text of a Token
    /// </summary>
    /// <remarks>
    ///      <note>
    ///         <para>
    ///             <b>Java File: </b> <a href="https://github.com/apache/lucene-solr/blob/trunk/lucene/src/java/org/apache/lucene/analysis/tokenattributes/CharTermAttributeImpl.java">
    ///             lucene/src/java/org/apache/lucene/analysis/tokenattributes/CharTermAttributeImpl.java
    ///         </a>
    ///         </para>
    ///         <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Analysis/TokenAttributes/CharTermAttribute.cs">
    ///              src/Lucene.Net/Analysis/TokenAttributes/CharTermAttribute.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Analysis/TokenAttributes/CharTermAttributeTest.cs">
    ///             test/Lucene.Net.Test/Analysis/TokenAttributes/CharTermAttributeTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    ///     <para>
    ///         The java version has extra methods to work with Java's 
    ///         <a href="http://download.oracle.com/javase/1,5.0/docs/api/java/lang/CharSequence.html">CharSequence</a>. 
    ///         Java's version of <c>string</c>, <c>StringBuilder</c>, <c>StringBuffer</c>, and <c>CharBuffer</c>
    ///         all implement this interface.  C# does not have a known equivalent.
    ///     </para>
    /// </remarks>
    public class CharTermAttribute : AttributeBase, ICharTermAttribute
    {
        private const int MinBufferSize = 10;
        private int termLength;


        /// <summary>
        /// Initializes a new instance of the <see cref="CharTermAttribute"/> class.
        /// </summary>
        public CharTermAttribute()
        {
            this.Buffer = CreateBuffer(MinBufferSize);
        }

        /// <summary>
        /// Gets the internal termBuffer character array which you can then
        /// directly alter.
        /// </summary>
        /// <value>The buffer.</value>
        /// <remarks>
        ///     <para>
        ///         If the array is too small for the token, use <see cref="SetLength(int)"/>
        ///         to increase the size. After altering the buffer be sure to call
        ///         <see cref="SetLength(int)"/> to record the valid characters that
        ///         were placed into the termBuffer.
        ///     </para>
        /// </remarks>
        public char[] Buffer { get; private set; }

        /// <summary>
        ///     Gets or sets the number of valid characters, the length of the term, in
        ///     the termBuffer array.
        /// </summary>
        /// <value></value>
        /// <remarks>
        ///     <para>
        ///         Use this to truncate the termBuffer or to synchronize any external
        ///         manipulation of the termBuffer.
        ///     </para>
        ///     <note>
        ///         To grow the size of the array, use <see cref="ResizeBuffer(int)"/> first.
        ///     </note>
        /// </remarks>
        public int Length
        {
            get { return this.termLength; }
            set { this.SetLength(value); }
        }

        /// <summary>
        ///     Appends the specified <see cref="Char"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        public ICharTermAttribute Append(char value)
        {
            int newLength = this.termLength + 1;

            this.ResizeBuffer(newLength);
            this.Buffer[newLength] = value;
            
            return this;
        }

        /// <summary>
        ///     Appends the specified <see cref="string"/> to internal buffer or character sequence.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        public ICharTermAttribute Append(string value)
        {
            return Append(value, 0, value == null ? 0 : value.Length);
        }

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
        public ICharTermAttribute Append(string value, int startingIndex, int length)
        {
            if (value == null)
                return this.AppendNull();

            value.CopyTo(startingIndex, this.ResizeBuffer(this.termLength + length), this.termLength, length);
            this.Length += length;

            return this;
        }

        /// <summary>
        ///     Appends the specified <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        public ICharTermAttribute Append(StringBuilder value)
        {
            if (value == null)
                return this.AppendNull();

            return this.Append(value.ToString());
        }

        /// <summary>
        ///     Appends the specified <see cref="ICharTermAttribute"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        public ICharTermAttribute Append(ICharTermAttribute value)
        {
            if (value == null)
                return this.AppendNull();

            int length = value.Length;

            Array.Copy(value.Buffer, 0, this.ResizeBuffer(this.termLength + length), this.termLength, length);
            this.termLength += length;

            return this;
        }


        /// <summary>
        ///     Returns the character at the specified index index.
        /// </summary>
        /// <param name="index">The position of the character.</param>
        /// <returns>An instance of <see cref="Char"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        ///    Throws when the index is equal or greater than the current buffer length 
        /// </exception>
        public char CharAt(int index)
        {
            if (index >= this.termLength)
                throw new IndexOutOfRangeException();

            return this.Buffer[index];
        }

        /// <summary>
        /// Clears the instance.
        /// </summary>
        public override void Clear()
        {
            this.termLength = 0;
        }

        /// <summary>
        /// Creates a clone of the object, generally shallow.
        /// </summary>
        /// <returns>an the clone of the current instance.</returns>
        public override AttributeBase Clone()
        {
            CharTermAttribute clone = (CharTermAttribute)this.MemberwiseClone();

            clone.Buffer = new char[this.termLength];
            Array.Copy(this.Buffer, 0, clone.Buffer, 0, this.termLength);

            return clone;
        }

        /// <summary>
        /// Copies the contents of the buffer, starting at the offset to the specified length.
        /// </summary>
        /// <param name="buffer">The buffer to copy.</param>
        /// <param name="offset">The index of the first character to copy inside the buffer.</param>
        /// <param name="length">The number of characters to copy.</param>
        public void CopyBuffer(char[] buffer, int offset = 0, int length = -1)
        {
            if (length == -1)
                length = buffer.Length;

            this.GrowBuffer(length);
            Array.Copy(buffer, offset, this.Buffer, 0, length);
            this.termLength = length;
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="attributeBase">The attribute base.</param>
        public override void CopyTo(AttributeBase attributeBase)
        {
            ICharTermAttribute attribute = (ICharTermAttribute)attributeBase;
            attribute.CopyBuffer(this.Buffer, 0, this.termLength);
        }

        /// <summary>
        ///     Sets the length of the internal buffer to zero. User this
        ///     method before appending content using <c>Append</c> methods.
        /// </summary>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        public ICharTermAttribute Empty()
        {
            this.termLength = 0;
            return this;
        }


        /// <summary>
        ///     Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            if (obj is CharTermAttribute)
            {
                CharTermAttribute y = (CharTermAttribute)obj;

                if (this.termLength != y.termLength)
                    return false;

                for (int i = 0; i < this.termLength; i++)
                {
                    if (this.Buffer[i] != y.Buffer[i])
                        return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///     A hash code for this instance, suitable for use in 
        ///     hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int code = this.termLength;
            code = (code * 31) + this.Buffer.CreateHashCode();
            return code;
        }


        /// <summary>
        ///     Resizes the length of the internal buffer to the new value and preserves the
        ///     existing content.
        /// </summary>
        /// <param name="length">The length to re-buffer to.</param>
        /// <returns>The <see cref="T:System.Char"/> array.</returns>
        public char[] ResizeBuffer(int length)
        {
            if (this.Buffer.Length < length)
            {
                char[] newBuffer = CreateBuffer(length);
                Array.Copy(this.Buffer, 0, newBuffer, 0, this.Buffer.Length);
                this.Buffer = newBuffer;
            }

            return this.Buffer;
        }

        /// <summary>
        ///     Gets or sets the number of valid characters, the length of the term, in
        ///     the termBuffer array.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <remarks>
        ///     <para>
        ///         Use this to truncate the termBuffer or to synchronize any external
        ///         manipulation of the termBuffer.
        ///     </para>
        ///     <note>
        ///         To grow the size of the array, use <see cref="ResizeBuffer(int)"/> first.
        ///     </note>
        /// </remarks>
        /// <returns>
        ///     An instance of <see cref="ICharTermAttribute"/> for fluent interface
        ///     chaining purposes.
        /// </returns>
        public ICharTermAttribute SetLength(int length)
        {
            if (length > this.Buffer.Length)
            {
                string message = string.Format(
                    "The given length '{0}' needs to be less than current the internal buffer length '{1}'",
                    length,
                    this.Buffer.Length);

                throw new ArgumentOutOfRangeException("length", message);
            }

            this.termLength = length;

            return this;
        }


        /// <summary>
        ///     Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return new string(this.Buffer).Substring(0, this.termLength);
        }

        private static char[] CreateBuffer(int length)
        {
            return new char[ArrayUtil.Oversize(length, RamUsageEstimator.NumBytesChar)];
        }

        private CharTermAttribute AppendNull()
        {
            this.ResizeBuffer(this.termLength + 4);

            this.Buffer[this.termLength++] = 'n';
            this.Buffer[this.termLength++] = 'u';
            this.Buffer[this.termLength++] = 'l';
            this.Buffer[this.termLength++] = 'l';

            return this;
        }

        private void GrowBuffer(int length)
        {
            if (this.Buffer.Length < length)
                this.Buffer = CreateBuffer(length);
        }
    }
}