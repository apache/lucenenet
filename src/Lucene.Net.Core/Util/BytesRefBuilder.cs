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
    using System.Linq;
    using Support;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;


    /// <summary>
    /// Class ByteRefBuilder.
    /// </summary>
    //
    // Notes
    //
    // ReSharper disable CSharpWarnings::CS1574
    public class BytesRefBuilder
    {
        private readonly BytesRef bytesRef;

        /// <summary>
        /// Initializes a new instance of the <see cref="BytesRefBuilder"/> class.
        /// </summary>
        public BytesRefBuilder()
        {
            this.bytesRef = new BytesRef();
        }

        /// <summary>
        /// Gets the bytes.
        /// </summary>
        /// <value>The bytes.</value>
        public byte[] Bytes
        {
            get { return this.bytesRef.Bytes; }
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length
        {
            get { return this.bytesRef.Length; }
            set
            {
                if(this.bytesRef.Bytes.Length < value)
                    this.Grow(value);

                this.bytesRef.Length = value;
            }
        }

        /// <summary>
        /// Appends the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Append(byte value)
        {
            var next = this.bytesRef.Length + 1;
            this.Length = next;
            this.Bytes[next] = value;
        }

        /// <summary>
        /// Appends the specified bytes.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void Append(byte[] bytes, int offset, int length)
        {
            this.Length += length;
            Array.Copy(bytes, offset, this.bytesRef.Bytes, this.bytesRef.Length, length);
        }

        /// <summary>
        /// Appends the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Append(BytesRef value)
        {
            this.Append(value.Bytes, value.Offset, value.Length);
        }

        /// <summary>
        /// Appends the specified builder.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public void Append(BytesRefBuilder builder)
        {
            this.Append(builder.bytesRef);
        }

        /// <summary>
        /// Bytes at.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>System.Byte.</returns>
        public byte ByteAt(int offset)
        {
            return this.Bytes[offset];
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this.Length = 0;
        }

        /// <summary>
        /// Clears and replaces the internal bytes. Its a shorthand method for calling 
        /// <see cref="Clear"/> and <see cref="Append(byte[],int, int)"/>.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void CopyBytes(byte[] bytes, int offset, int length)
        {
            this.Clear();
            this.Append(bytes, offset, length);
        }

        /// <summary>
        /// Clears and replaces the internal bytes. Its a shorthand method for calling 
        /// <see cref="Clear"/> and <see cref="Append(BytesRef)"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        public void CopyBytes(BytesRef value)
        {
            this.Clear();
            this.Append(value);
        }

        /// <summary>
        /// Copies the bytes.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public void CopyBytes(BytesRefBuilder builder)
        {
            this.Clear();
            this.Append(builder);
        }

        /// <summary>
        /// Copies the chars.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void CopyChars(ICharSequence text, int offset = 0, int length = -1)
        {
            if(length == -1)
                length = text.Length;

            this.Length = length * UnicodeUtil.MAX_UTF8_BYTES_PER_CHAR;
            this.Length = UnicodeUtil.Utf16ToUtf8(text, offset, length, this.bytesRef.Bytes);
        }

        /// <summary>
        /// Copies the chars.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void CopyChars(string text, int offset = 0, int length = -1)
        {
            if (length == -1)
                length = text.Length;

            this.Length = length * UnicodeUtil.MAX_UTF8_BYTES_PER_CHAR;
            this.Length = UnicodeUtil.Utf16ToUtf8(text.ToCharArray(), offset, length, this.bytesRef.Bytes);
        }

        /// <summary>
        /// Copies the chars.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void CopyChars(char[] text, int offset = 0, int length = -1)
        {
            if (length == -1)
                length = text.Length;

            this.Length = length * UnicodeUtil.MAX_UTF8_BYTES_PER_CHAR;
            this.Length = UnicodeUtil.Utf16ToUtf8(text, offset, length, this.bytesRef.Bytes);
        }

        /// <summary>
        /// Copies the chars.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void CopyChars(IEnumerable<char> text, int offset = 0, int length = -1)
        {
            // ReSharper disable PossibleMultipleEnumeration
            if (length == -1)

                length = text.Count();

            this.Length = length * UnicodeUtil.MAX_UTF8_BYTES_PER_CHAR;
            this.Length = UnicodeUtil.Utf16ToUtf8(text, offset, length, this.bytesRef.Bytes);
        }

        /// <inherits />
        /// <exception cref="NotSupportedException">Throws when called.</exception>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "Java Port Consistency")]
        public override bool Equals(object obj)
        {
            throw new NotSupportedException();
        }

          /// <inherits />
        /// <exception cref="NotSupportedException">Throws when called.</exception>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "Java Port Consistency")]
        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Grows the specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        protected void Grow(int capacity)
        {
            this.bytesRef.Bytes = this.bytesRef.Bytes.Grow(capacity);
        }



        /// <summary>
        /// To the bytes reference.
        /// </summary>
        /// <returns>BytesRef.</returns>
        public BytesRef ToBytesRef()
        {
            var copy = this.Bytes.Copy(this.Length);
            return new BytesRef(copy); 
        }
    }
}
