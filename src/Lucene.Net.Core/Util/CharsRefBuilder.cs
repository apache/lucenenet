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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Builds up characters for a <see cref="Lucene.Net.Util.CharsRef"/>. 
    /// This class is meant for internal use only. 
    /// </summary>
    // ReSharper disable CSharpWarnings::CS1574
    public class CharsRefBuilder:
        IEnumerable<char>
    {
        private readonly CharsRef charsRef;
        
        /// <summary>
        /// Initializes a new instance of <see cref="CharsRefBuilder"/>.
        /// </summary>
        public CharsRefBuilder()
        {
            this.charsRef = new CharsRef();
        }

        /// <summary>
        /// Gets or sets the character value at the specified index.
        /// </summary>
        /// <param name="index">The position of the value to get or set.</param>
        /// <returns>The value at the specified index.</returns>
        public char this[int index]
        {
            get { return this.charsRef.Chars[index]; }
            set
            {
                this.charsRef.Chars[index] = value;
            }
        }

        /// <summary>
        /// Gets the internal reference of <see cref="Lucene.Net.Util.CharsRef"/> 
        /// that the builder uses.
        /// </summary>
        public CharsRef CharRef
        {
            get { return this.charsRef; }
        }

        /// <summary>
        /// Gets the array of characters.
        /// </summary>
        public Char[] Chars
        {
            get { return this.charsRef.Chars; }
        }

        /// <summary>
        /// Gets or sets the number of characters in the builder.
        /// </summary>
        public int Length
        {
            get { return this.charsRef.Length; }
            set { this.charsRef.Length = value; }
        }

        /// <summary>
        /// Appends the characters at the end of the array in this instance.
        /// </summary>
        /// <param name="chars">The array of characters to copy.</param>
        /// <param name="offset">The starting position in <paramref name="chars"/> for the copy.</param>
        /// <param name="length">The number of characters to copy from <paramref name="chars"/>.</param>
        /// <exception cref="System.ArgumentException">
        ///     <list type="bullet">
        ///         <item>Thrown when <paramref name="offset"/> is less than 0.</item>
        ///         <item>Thrown when <paramref name="length"/> is less than 0 or greater than <paramref name="chars"/>.Length.</item>
        ///     </list>
        /// </exception>
        public void Append(char[] chars, int offset = 0, int length = 0)
        {
            this.InternalCopyChars(chars, offset, length, true);
        }

        /// <summary>
        /// Reset the current instance of <see cref="Lucene.Net.Util.CharsRefBuilder"/>
        /// to an empty state.
        /// </summary>
        public void Clear()
        {
            this.charsRef.Length = 0;
        }

        /// <summary>
        /// Copies the <see cref="Lucene.Net.Util.CharsRef"/> into this instance.
        /// </summary>
        /// <param name="other">The instance to copy.</param>
        public void CopyChars(CharsRef other)
        {
            this.CopyChars(other.Chars);
        }

        /// <summary>
        /// Copies the characters into this instance.
        /// </summary>
        /// <param name="chars">The array of characters to copy.</param>
        /// <param name="offset">The starting position in <paramref name="chars"/> for the copy.</param>
        /// <param name="length">The number of characters to copy from <paramref name="chars"/>.</param>
        /// <exception cref="System.ArgumentException">
        ///     <list type="bullet">
        ///         <item>Thrown when <paramref name="offset"/> is less than 0.</item>
        ///         <item>Thrown when <paramref name="length"/> is less than 0 or greater than <paramref name="chars"/>.Length.</item>
        ///     </list>
        /// </exception>
        public void CopyChars(char[] chars, int offset = 0, int length = 0)
        {
            this.InternalCopyChars(chars, offset, length);
        }

        private void InternalCopyChars(char[] chars, int offset = 0, int length = 0, bool append = false)
        {
            Check.Condition(offset < 0, "offset", "Parameter 'offset' should be greater than or equal to 0.");
            Check.Condition(length < 0 || length > chars.Length, "length",
                    "Parameter 'length' must be greater or equal to 0 and less than or equal to chars.Length.");

            if (length == 0)
                length = chars.Length;

            int position = 0;
            int newLength = length;
            if(append)
            {
                position = this.charsRef.Length;
                newLength = this.charsRef.Length + length;
            }

            this.Grow(newLength);
            Array.Copy(chars, offset, this.charsRef.Chars, position, length);

            this.charsRef.Length = newLength;
        }


        /// <summary>
        /// Copies the <see cref="Lucene.Net.Util.BytesRef"/> into this instance.
        /// </summary>
        /// <param name="bytes">The instance to copy.</param>
        public void CopyUtf8Bytes(BytesRef bytes)
        {
            this.CopyUtf8Bytes(bytes.Bytes, bytes.Offset, bytes.Length);
        }


        /// <summary>
        /// Copies the bytes into this instance.
        /// </summary>
        /// <param name="bytes">The array of bytes to copy.</param>
        /// <param name="offset">The starting position in <paramref name="bytes"/> for the copy.</param>
        /// <param name="length">The number of characters to copy from <paramref name="bytes"/>.</param>
        /// <exception cref="System.ArgumentException">
        ///     <list type="bullet">
        ///         <item>Thrown when <paramref name="offset"/> is less than 0.</item>
        ///         <item>Thrown when <paramref name="length"/> is less than 0 or greater than <paramref name="bytes"/>.Length.</item>
        ///     </list>
        /// </exception>
        public void CopyUtf8Bytes(byte[] bytes, int offset = 0, int length = 0)
        {
            Check.Condition(offset < 0, "offset", "Parameter 'offset' should be greater than or equal to 0.");
            //Check.Condition(length < 0 || length > bytes.Length, "length",
            //    "Parameter 'length' must be greater or equal to 0 and less than or equal to bytes.Length.");

            if (length == 0)
                length = bytes.Length;

            this.Grow(length);

            this.charsRef.Length = UnicodeUtil.Utf8ToUtf16(bytes, offset, length, this.charsRef);
        }

        /// <inherits />
        /// <exception cref="NotSupportedException">Throws when called.</exception>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "Java Port Consistency")]
        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }
        
        /// <summary>
        /// Resizes and increases the length of the reference array.
        /// </summary>
        /// <param name="mininumSize">The minimum size to grow the array.</param>
        public void Grow(int mininumSize)
        {
            this.charsRef.Chars = ArrayUtil.Grow(this.charsRef.Chars, mininumSize);
        }

        /// <inherits />
        /// <exception cref="NotSupportedException">Throws when called.</exception>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "Java Port Consistency")]
        public override bool Equals(object obj)
        {
            throw new NotSupportedException();
        }


        /// <summary>
        /// Returns a new instance of <see cref="Lucene.Net.Util.CharsRef"/> that has 
        /// copy of the current state of this instance.
        /// </summary>
        /// <returns>a new instance of <see cref="Lucene.Net.Util.CharsRef"/></returns>
        public CharsRef ToCharRef()
        {
            var copy = this.charsRef.Chars.CopyOf(this.charsRef.Length);
            return new CharsRef(copy, 0, this.charsRef.Length);
        }

        /// <inherited />
        public override string ToString()
        {
            return this.charsRef.ToString();
        }

        #region IEnumerable<char>
        public IEnumerator<char> GetEnumerator()
        {
            return this.charsRef.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.charsRef.GetEnumerator();
        }
        #endregion
    }
}