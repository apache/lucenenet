// -----------------------------------------------------------------------
// <copyright company="Apache" file="BytesRef.cs">
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

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Lucene.Net.Support;

    /// <summary>
    /// TODO: port
    /// Still missing methods that have to do with CharRef and ArrayUtil.Grow
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class might be transformed into a immutable value type depending on its use
    ///         in the Lucene code base.
    ///     </para>
    /// </remarks>
    public sealed class BytesRef : IComparable<BytesRef>,
        IEquatable<BytesRef>,
        ICloneable<BytesRef>
    {
        /// <summary>
        /// Returns an array of empty bytes. 
        /// </summary>
        public static readonly byte[] EmptyBytes = new byte[0];

        private static readonly UTF16Comparer utf16Comparer = new UTF16Comparer();
        private static readonly UnicodeComparer unicodeComparer = new UnicodeComparer();
 
        /// <summary>
        /// Initializes a new instance of the <see cref="BytesRef"/> class.
        /// </summary>
        public BytesRef()
        {
            this.Bytes = EmptyBytes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BytesRef"/> class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public BytesRef(byte[] bytes, int offset = 0, int length = 0)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");

            if (length == 0)
                length = bytes.Length;

            this.Bytes = bytes;
            this.Offset = offset;
            this.Length = length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BytesRef"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public BytesRef(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentException("capacity can not be less than 0", "capacity");

            this.Bytes = new byte[capacity];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BytesRef"/> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public BytesRef(char[] text, int offset = 0, int length = 0)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            if (length == 0)
                length = text.Length;

            this.Bytes = new byte[length];

            this.Copy(text, offset, length);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BytesRef"/> class.
        /// </summary>
        /// <param name="text">The text.</param>
        public BytesRef(string text)
            : this()
        {
            if (text == null)
                throw new ArgumentNullException("text");

            this.Copy(text);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BytesRef"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public BytesRef(BytesRef target)
            : this()
        {
            if (target == null)
                throw new ArgumentNullException("target");

            this.Copy(target);
        }

        /// <summary>
        /// Gets the UTF8 sorted as UTF16 comparer.
        /// </summary>
        /// <value>The UTF8 sorted as UTF16 comparer.</value>
        public static IComparer<BytesRef> Utf8SortedAsUtf16Comparer
        {
            get { return utf16Comparer; }
        }

        /// <summary>
        /// Gets the UTF8 sorted as Unicode comparer.
        /// </summary>
        /// <value>The UTF8 sorted as Unicode comparer.</value>
        public static IComparer<BytesRef> Utf8SortedAsUnicodeComparer
        {
            get { return unicodeComparer; }
        }

        /// <summary>
        /// Gets or sets the bytes.
        /// </summary>
        /// <value>The bytes.</value>
        public byte[] Bytes { get; set; }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length { get; set; }

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>The offset.</value>
        public int Offset { get; set; }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(BytesRef left, BytesRef right)
        {
            if (left == null)
                return right != null;

            return !left.Equals(right);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(BytesRef left, BytesRef right)
        {
            if (left == null)
                return right == null;

            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(BytesRef left, BytesRef right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(BytesRef left, BytesRef right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Byteses the equal.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>An instance of <see cref="Boolean"/>.</returns>
        public bool Equals(BytesRef target)
        {
            if (this.Length != target.Length)
                return false;

            int targetOffset = target.Offset, 
                end = this.Offset + this.Length;

            byte[] targetBytes = target.Bytes;

            for (int i = this.Offset; i < end; i++, targetOffset++)
            {
                if (this.Bytes[i] != targetBytes[targetOffset])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Copies the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public void Copy(BytesRef source)
        {
            if (this.Bytes.Length < source.Length)
                this.Bytes = new byte[source.Length];

            Array.Copy(source.Bytes, source.Offset, this.Bytes, 0, source.Length);

            this.Length = source.Length;
            this.Offset = 0;
        }

        /// <summary>
        /// Copies the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public void Copy(string source)
        {
            UnicodeUtil.UTF16toUTF8(source, 0, source.Length, this);   
        }

        /// <summary>
        /// Copies the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void Copy(char[] source, int offset = 0, int length = 0)
        {
            if (length == 0)
                length = source.Length;

            UnicodeUtil.UTF16toUTF8(source, offset, length, this);
        }

    

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>a <see cref="BytesRef"/> clone.</returns>
        public BytesRef Clone()
        {
            return new BytesRef(this);
        }

        /// <summary>
        /// Compares this instance to the other <see cref="BytesRef"/> instance.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>An instance of <see cref="Int32"/>.</returns>
        public int CompareTo(BytesRef other)
        {
            if (this == other)
                return 0;

            byte[] leftBytes = this.Bytes,
                    rightBytes = other.Bytes;

            int leftOffset = this.Offset,
                rightOffset = other.Offset,
                leftStop;

            leftStop = leftOffset + Math.Min(this.Length, other.Length);

            while (leftOffset < leftStop)
            {
                int leftByte = leftBytes[leftOffset++] & 0xff;
                int rightByte = rightBytes[rightOffset++] & 0xff;
                int difference = leftByte - rightByte;

                if (difference != 0)
                    return difference;
            }

            return this.Length - other.Length;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int hashCode = 0, end = this.Offset + this.Length;

            for (int i = this.Offset; i < end; i++) 
            {
               hashCode = (31 * hashCode) + this.Bytes[i];
            }

            return hashCode;
        }

        /// <summary>
        /// Grows the <see cref="BytesRef"/> to the specified length.
        /// </summary>
        /// <param name="length">The length.</param>
        public void Grow(int length)
        {
            this.Bytes = ArrayUtil.Grow(this.Bytes, length);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            BytesRef bytesRef = obj as BytesRef;
            
            if (bytesRef == null)
                return false;

            return this.Equals(bytesRef);
        }

        /// <summary>
        /// Starts with the specified <see cref="BytesRef"/>.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>An instance of <see cref="Boolean"/>.</returns>
        public bool StartsWith(BytesRef other)
        {
            return this.SliceEquals(other, 0);
        }

        /// <summary>
        /// Ends with the specified <see cref="BytesRef"/>.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>An instance of <see cref="Boolean"/>.</returns>
        public bool EndsWith(BytesRef other)
        {
            return this.SliceEquals(other, this.Length - other.Length);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance. Should return
        /// hex encoded bytes that looks something like: [0x6c 0x75 0x63 0x65 0x6e 0x65]
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            int end = this.Offset + this.Length;

            sb.Append('[');

            for (int i = this.Offset; i < end; i++)
            {
                if (i > this.Offset)
                    sb.Append(' ');

                sb.Append((this.Bytes[i] & 0xf).ToString("X"));
            }

            sb.Append(']');

            return sb.ToString();
        }


        object ICloneable.Clone()
        {
            return this.Clone();
        }

        private bool SliceEquals(BytesRef other, int position)
        {
            if (position < 0 || this.Length - position < other.Length)
                return false;

            int leftOffset = this.Offset + position;
            int rightOffset = other.Offset;
            int rightStop = other.Offset + other.Length;

            while (rightOffset < rightStop)
            {
                if (this.Bytes[leftOffset++] != other.Bytes[rightOffset++])
                    return false;
            }

            return true;
        }

        private class UnicodeComparer : IComparer<BytesRef>
        {
            /// <summary>
            /// Compares the two <see cref="BytesRef"/> objects to determine if they
            /// are equal or to see if the <paramref name="left"/> is greater than or less than the <paramref name="right"/>.
            /// </summary>
            /// <param name="left">The left.</param>
            /// <param name="right">The right.</param>
            /// <returns>a int the represents the comparison.</returns>
            public int Compare(BytesRef left, BytesRef right)
            {
                byte[] leftBytes = left.Bytes, 
                       rightBytes = right.Bytes;

                int leftOffset = left.Offset, 
                    rightOffset = right.Offset, 
                    leftStop;

                leftStop = leftOffset + (left.Length < right.Length ? left.Length : right.Length);

                while (leftOffset < leftStop)
                {
                    int leftByte = leftBytes[leftOffset++] & 0xff;
                    int rightByte = rightBytes[rightOffset++] & 0xff;

                    int difference = leftByte - rightByte;

                    if (difference != 0)
                        return difference;
                }

                return left.Length - right.Length;
            }
        }

        

        private class UTF16Comparer : IComparer<BytesRef>
        {
            /// <summary>
            /// Compares the two <see cref="BytesRef"/> parameters using a UTF16Sorted Comparison. 
            /// </summary>
            /// <remarks>
            ///     <para>
            ///         See http://icu-project.org/docs/papers/utf16_code_point_order.html#utf-8-in-utf-16-order
            ///         We know the terms are not equal, but, we may have to carefully 
            ///         fixup the bytes at the difference to match UTF16's sort order.
            ///      </para>
            ///      <para>
            ///         NOTE: instead of moving supplementary code points (0xee and 0xef) to the unused 0xfe and 0xff, 
            ///         we move them to the unused 0xfc and 0xfd [reserved for future 6-byte character sequences]
            ///         this reserves 0xff for preflex's term reordering (surrogate dance), and if Unicode grows such
            ///         that 6-byte sequences are needed we have much bigger problems anyway.
            ///     </para>
            /// </remarks>
            /// <param name="left">The left value that is being compared.</param>
            /// <param name="right">The right value that is being compared.</param>
            /// <returns>the comparison int.</returns>
            public int Compare(BytesRef left, BytesRef right)
            {
                byte[] leftBytes = left.Bytes,
                       rightBytes = right.Bytes;

                int leftOffset = left.Offset,
                    rightOffset = right.Offset,
                    leftStop = 0;

                leftStop = leftOffset + (left.Length < right.Length ? left.Length : right.Length);

                while (leftOffset < leftStop)
                {
                    int leftByte = leftBytes[leftOffset++] & 0xff;
                    int rightByte = rightBytes[rightOffset++] & 0xff;

                    if (leftByte != rightByte)
                    {
                        if (leftByte >= 0xee & rightByte >= 0xee)
                        {
                            if ((leftByte & 0xfe) == 0xee)
                                leftByte += 0xe;

                            if ((rightByte & 0xfe) == 0xee)
                                rightByte += 0xe;

                            return leftByte - rightByte;
                        }
                    }
                }

                return right.Length - left.Length;
            }
        }
    }
}