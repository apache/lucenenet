// -----------------------------------------------------------------------
// <copyright company="Apache" file="CharsRef.cs">
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
    /// This class represents a slice of a an existing <c>char[]</c>.
    /// The <see cref="Chars"/> property should never be null, default to <see cref="emptyArray"/> if necessary.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class might be transformed into a immutable value type depending on its use
    ///         in the Lucene code base.
    ///     </para>
    /// </remarks>
    public class CharsRef : IComparable<CharsRef>, ICharSequence, ICloneable<CharsRef>, IEnumerable<char>, IEquatable<CharsRef>
    {
        private static readonly char[] emptyArray = new char[0];
        private static readonly UTF8Comparer comparer = new UTF8Comparer();

        /// <summary>
        /// Initializes a new instance of the <see cref="CharsRef"/> class.
        /// </summary>
        public CharsRef()
        {
            this.Chars = emptyArray;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharsRef"/> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public CharsRef(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentException("capacity can not be less than 0", "capacity");

            this.Chars = new char[capacity];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharsRef"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the length of <paramref name="source"/> is less than <paramref name="offset"/> and 
        ///     <paramref name="length"/> combined.
        /// </exception>
        public CharsRef(char[] source, int offset = 0, int length = 0)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (source.Length < (offset + length))
                throw new ArgumentException(
                    "The length of source, source.Length, must be equal to or greater than the offset & length combined.");

            if (length == 0)
                length = source.Length;

            this.Chars = source;
            this.Offset = offset;
            this.Length = length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharsRef"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="source"/> is null.</exception>
        public CharsRef(string source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            this.Chars = source.ToCharArray();
            this.Length = this.Chars.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharsRef"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public CharsRef(CharsRef source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            this.Chars = emptyArray;
            this.Copy(source);
        }

        /// <summary>
        /// Gets the UT F16 sorted as UT f8 comparer.
        /// </summary>
        /// <value>The UT F16 sorted as UT f8 comparer.</value>
        public static IComparer<CharsRef> UTF16SortedAsUTF8Comparer
        {
            get { return comparer; }
        }

        /// <summary>
        /// Gets or sets the chars.
        /// </summary>
        /// <value>The chars.</value>
        public char[] Chars { get; protected set; }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length { get; protected set; }

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>The offset.</value>
        public int Offset { get; protected set; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(CharsRef left, CharsRef right)
        {
            if (left == null)
                return right == null;

            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(CharsRef left, CharsRef right)
        {
            if (left == null)
                return right != null;

            return !left.Equals(right);
        }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(CharsRef left, CharsRef right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(CharsRef left, CharsRef right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Appends the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void Append(char[] value, int offset = 0, int length = 0)
        {
            if (length == 0)
                length = value.Length;

            this.Grow(this.Offset + length);

            Array.Copy(value, offset, this.Chars, this.Offset, length);

            this.Length = length;
        }

        /// <summary>
        /// Finds the <see cref="char"/> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>An instance of <see cref="Char"/>.</returns>
        public char CharAt(int index)
        {
            return this.Chars[index];
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>an cloned instance of <see cref="CharsRef"/></returns>
        public CharsRef Clone()
        {
            return new CharsRef(this);
        }

        /// <summary>
        /// Compares to.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>An instance of <see cref="Int32"/>.</returns>
        public int CompareTo(CharsRef other)
        {
            if (this.Equals(other))
                return 0;

            char[] leftChars = this.Chars,
                   rightChars = other.Chars;

            int leftOffset = this.Offset,
                rightOffset = other.Offset,
                end = leftOffset + Math.Min(this.Length, other.Length);

            while (leftOffset < end)
            {
                int leftChar = leftChars[leftOffset++];
                int rightChar = rightChars[rightOffset++];

                if (leftChar > rightChar)
                    return 1;
                if (leftChar < rightChar)
                    return -1;
            }

            return this.Length - other.Length;
        }

        /// <summary>
        /// Copies the specified <paramref name="source"/> and resets the current instance's <see cref="Offset"/> to 0.
        /// </summary>
        /// <param name="source">The source.</param>
        public void Copy(CharsRef source)
        {
            this.Chars = ArrayUtil.Grow(this.Chars, source.Length);

            Array.Copy(source.Chars, source.Offset, this.Chars, 0, source.Length);

            this.Length = source.Length;
            this.Offset = 0;
        }

        /// <summary>
        /// Copies the <c>char[]</c> into this instance and sets this instance's <see cref="Offset"/> to 0.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void Copy(char[] source, int offset = 0, int length = 0)
        {
            if (length == 0)
                length = source.Length;

            this.Offset = 0;
            this.Append(source, offset, length);
        }

        /// <summary>
        /// Determines whether the specified <see cref="CharsRef"/> is equal to this instance.
        /// </summary>
        /// <param name="right">The right.</param>
        /// <returns><c>true</c> if <paramref name="right"/> is equal to this instance, otherwise <c>false</c>.</returns>
        public bool Equals(CharsRef right)
        {
            if (this.Length != right.Length)
                return false;

            int rightOffset = right.Offset,
                end = this.Offset + this.Length;

            char[] rightChars = right.Chars,
                   leftChars = this.Chars;

            for (int leftOffset = this.Offset; leftOffset < end; leftOffset++, rightOffset++)
            {
                if (leftChars[leftOffset] != rightChars[rightOffset])
                    return false;
            }

            return true;
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
            CharsRef otherCharRef = obj as CharsRef;

            if (otherCharRef != null)
                return this.Equals(otherCharRef);

            if (!obj.IsCharSequence())
                return false;

            ICharSequence sequence = null;

            if (obj is string)
            {
                sequence = obj.ToString().ToCharSequence();
            }
            else if (obj is ICharSequence)
            {
                sequence = (ICharSequence)obj;
            }
            else
            {
                sequence = ((IEnumerable<char>)obj).ToCharSequence();
            }

            if (this.Length != sequence.Length)
                return false;

            int end = this.Length,
                i = this.Offset,
                j = 0;

            while (end-- != 0)
            {
                if (this.Chars[i++] != sequence.CharAt(j++))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="IEnumerator{T}"/> of <c>char</c>.
        /// </returns>
        public IEnumerator<char> GetEnumerator()
        {
            // there might be a better way to do this.
            return this.Chars.ToList().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }


        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int result = 0, end = this.Offset + this.Length;

            for (int i = this.Offset; i < end; i++)
                result = (31 * result) + this.Chars[i];

            return result;
        }

        /// <summary>
        /// Grows the specified length.
        /// </summary>
        /// <param name="length">The length.</param>
        public void Grow(int length)
        {
            if (this.Chars.Length < length)
                this.Chars = ArrayUtil.Grow(this.Chars, length);
        }

        /// <summary>
        /// Gets the subset sequence of characters from the current sequence.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>
        /// An instance of <see cref="ICharSequence"/>.
        /// </returns>
        public ICharSequence SubSequence(int start, int end)
        {
            return new CharsRef(this.Chars, this.Offset + start, this.Offset + end - 1);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> representation of this instance. This override
        /// creates a new string passing in the char array, offset, and length.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return new string(this.Chars, this.Offset, this.Length);
        }

        

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        private sealed class UTF8Comparer : IComparer<CharsRef>
        {
            /// <summary>
            /// Compares the specified left.
            /// </summary>
            /// <remarks>
            ///     <notes>
            ///     http://icu-project.org/docs/papers/utf16_code_point_order.html
            ///     </notes>
            ///     <para>
            ///     A good deal of the code was pulled from the icu-project.
            ///     </para>
            /// </remarks>
            /// <param name="left">The left.</param>
            /// <param name="right">The right.</param>
            /// <returns>
            /// <c>0</c> if <paramref name="left"/> and <paramref name="right"/> are equal. Returns an int less than 0 
            /// if the <paramref name="left"/> is less than the <paramref name="right"/>, otherwise an int greater than 0.
            /// </returns>
            public int Compare(CharsRef left, CharsRef right)
            {
                if (left == right)
                    return 0;

                char[] leftChars = left.Chars,
                       rightChars = right.Chars;
                int leftOffset = left.Offset,
                    rightOffset = right.Offset,
                    end = leftOffset + Math.Min(left.Length, right.Length);

                Func<int, int> normalize = (value) => {
                     if (value >= 0xe000)
                         value -= 0x800;
                     else
                         value += 0x2000;
                    return value;
                };
                
                
                while (leftOffset < end)
                {
                    int leftChar = leftChars[leftOffset++];
                    int rightChar = rightChars[rightOffset++];
                    if (leftChar != rightChar)
                    {
                        //// http://icu-project.org/docs/papers/utf16_code_point_order.html
                        //// fix up each value if both values are inside of or above the surrogate range. then compare.
                        if (leftChar >= 0xd800 && rightChar >= 0xd800)
                        {
                            leftChar = normalize(leftChar);
                            rightChar = normalize(rightChar);
                        }

                        return leftChar - rightChar;
                    }
                }

                return left.Length - right.Length;
            }
        }
    }
}
