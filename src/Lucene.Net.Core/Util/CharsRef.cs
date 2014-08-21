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
    using System.Diagnostics;
    using System.Linq;
    using Lucene.Net.Support;
    using ICloneable = Lucene.Net.Support.ICloneable; 

    /// <summary>
    ///     <see cref="CharsRef" /> represents a <see cref="char" /> array as a slice from an existing array.
    ///     This class is for internal use only.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <seealso cref="Chars" /> member should never be null; use
    ///         <seealso cref="CharsRef.EMPTY_CHARS" /> if necessary.
    ///     </para>
    /// </remarks>
    // ReSharper disable CSharpWarnings::CS1574
    public sealed class CharsRef : IComparable<CharsRef>,
        ICharSequence,
        // ReSharper disable once RedundantNameQualifier
        ICloneable,
        IEnumerable<char>
    {
        private int length;

        /// <summary>
        ///     An empty character array for convenience
        /// </summary>
        public static readonly char[] EMPTY_CHARS = new char[0];

        /// <summary>
        ///     The contents of the CharsRef. Should never be <c>null</c>.
        /// </summary>
        public char[] Chars { get; internal set; }

        /// <summary>
        ///     Offset of first valid character.
        /// </summary>
        public int Offset { get; internal set; }

        /// <summary>
        ///     Length of used characters.
        /// </summary>
        public int Length
        {
            get { return this.length; }
            set
            {
                this.length = value;
                if(this.Chars.Length < value)
                    this.Grow(value);
            }
        }


        /// <summary>
        ///     Initializes a new instance of <seealso cref="CharsRef" /> with an empty array.
        /// </summary>
        public CharsRef()
            : this(EMPTY_CHARS, 0, 0)
        {
        }

        /// <summary>
        ///     Initializes a new instance of <seealso cref="CharsRef" /> with an empty array with the
        ///     specified <paramref name="capacity" />.
        /// </summary>
        /// <param name="capacity">The size of the internal array.</param>
        public CharsRef(int capacity)
        {
            this.Chars = new char[capacity];
            
        }

        /// <summary>
        ///     Initializes a new instance of <seealso cref="CharsRef" /> that
        ///     references the <paramref name="chars" /> instead of makinga copy.
        /// </summary>
        /// <param name="chars">The array of chars to reference.</param>
        /// <param name="offset">The starting position of the first valid byte.</param>
        /// <param name="length">The number of bytes to use.</param>
        public CharsRef(char[] chars, int offset, int length)
        {
            this.Chars = chars;
            this.Offset = offset;
            this.Length = length;
            Debug.Assert(this.Valid());
        }

        /// <summary>
        ///     Initializes a new instance of <seealso cref="CharsRef" /> with the specified <paramref name="value" />.
        /// </summary>
        /// <param name="value">The string that be referenced.</param>
        public CharsRef(string value)
        {
            this.Chars = value.ToCharArray();
            this.Offset = 0;
            this.Length = Chars.Length;
        }

        /// <summary>
        ///     Returns a shallow clone of this instance (the underlying characters are
        ///     <b>not</b> copied and will be shared by both the returned object and this
        ///     object.
        /// </summary>
        /// <seealso cref="Lucene.Net.Support.ICloneable" />
        public object Clone(bool deepClone = false)
        {
            if (deepClone)
            {
                var charsRef = new CharsRef();
                charsRef.CopyChars(this);

                return charsRef;
            }

            return new CharsRef(Chars, Offset, Length);
        }

        /// <inherited />
        public override int GetHashCode()
        {
            const int prime = 31;
            var result = 0;
            var end = Offset + Length;
            for (var i = Offset; i < end; i++)
            {
                result = prime*result + Chars[i];
            }
            return result;
        }

        /// <inherited />
        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }
            if (other is CharsRef)
            {
                return this.CharsEquals((CharsRef) other);
            }
            return false;
        }

        /// <summary>
        ///     Determines if the other <see cref="CharsRef" /> is equal to the current instance.
        /// </summary>
        /// <param name="other">The instance to compare.</param>
        /// <returns>True, if the other instance is equal to the current instance, otherwise false.</returns>
        public bool CharsEquals(CharsRef other)
        {
            if (Length == other.Length)
            {
                // upto = Offset;
                var otherOffset = other.Offset;
                var otherChars = other.Chars;
                var end = this.Offset + this.Length;
                for (var offset = Offset; offset < end; offset++, otherOffset++)
                {
                    if (this.Chars[offset] != otherChars[otherOffset])
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Signed int order comparison
        /// </summary>
        /// <param name="other">The reference that will be compared to this instance.</param>
        public int CompareTo(CharsRef other)
        {
            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (this == other)
            {
                return 0;
            }

            var aChars = this.Chars;
            var aOffset = this.Offset;
            var bChars = other.Chars;
            var bOffset = other.Offset;

            var aStop = aOffset + Math.Min(this.Length, other.Length);

            while (aOffset < aStop)
            {
                int aInt = aChars[aOffset++];
                int bInt = bChars[bOffset++];
                if (aInt > bInt)
                {
                    return 1;
                }
                
                if (aInt < bInt)
                {
                    return -1;
                }
            }

            // One is a prefix of the other, or, they are equal:
            return this.Length - other.Length;
        }

        /// <summary>
        ///     Copies the given <seealso cref="CharsRef" /> referenced content into this instance.
        /// </summary>
        /// <param name="other">the <seealso cref="CharsRef" /> to copy </param>
        public void CopyChars(CharsRef other)
        {
            CopyChars(other.Chars, other.Offset, other.Length);
        }

        /// <summary>
        ///     Used to grow the reference array.
        /// </summary>
        /// <param name="capacity">The minimum length to grow the internal array.</param>
        internal void Grow(int capacity)
        {
            Debug.Assert(Offset == 0);
            if (this.Chars.Length < capacity)
            {
                this.Chars = ArrayUtil.Grow(this.Chars, capacity);
            }
        }

        /// <summary>
        ///     Copies the given array into this CharsRef.
        /// </summary>
        public void CopyChars(char[] otherChars, int otherOffset, int otherLength)
        {
            if (this.Chars.Length - this.Offset < otherLength)
            {
                this.Chars = new char[otherLength];
                this.Offset = 0;
            }
            Array.Copy(otherChars, otherOffset, this.Chars, this.Offset, otherLength);
            this.Length = otherLength;
        }

        /// <summary>
        ///     Appends the given array to this instance.
        /// </summary>
        public void Append(char[] otherChars, int otherOffset, int otherLength)
        {
            var newLen = Length + otherLength;
            if (this.Chars.Length - this.Offset < newLen)
            {
                var newChars = new char[newLen];
                Array.Copy(this.Chars, this.Offset, newChars, 0, Length);
                this.Offset = 0;
                this.Chars = newChars;
            }
            Array.Copy(otherChars, otherOffset, this.Chars, this.Length + this.Offset, otherLength);
            Length = newLen;
        }

        /// <summary>
        ///     Returns the string representation of the <see cref="char" /> array.
        /// </summary>
        /// <returns>
        ///     <see cref="string" />
        /// </returns>
        public override string ToString()
        {
            return new string(this.Chars, this.Offset, this.Length);
        }

        /// <summary>
        ///     Returns the char at specified index.
        /// </summary>
        /// <param name="index">The index of the char to be returned.</param>
        /// <returns>A char</returns>
        public char CharAt(int index)
        {
            // NOTE: must do a real check here to meet the specs of CharSequence
            if (index < 0 || index >= Length)
            {
                throw new IndexOutOfRangeException();
            }
            return this.Chars[this.Offset + index];
        }

        /// <summary>
        ///     Returns a new <see cref="ICharSequence" /> of the specified range of start and end.
        /// </summary>
        /// <param name="start">The position to start the new sequence.</param>
        /// <param name="end">The position to end the new sequence.</param>
        /// <returns>A new <see cref="ICharSequence" />.</returns>
        public ICharSequence SubSequence(int start, int end)
        {
            // NOTE: must do a real check here to meet the specs of CharSequence
            if (start < 0 || end > Length || start > end)
            {
                throw new IndexOutOfRangeException();
            }
            return new CharsRef(this.Chars, this.Offset + start, end - start);
        }

        /// @deprecated this comparator is only a transition mechanism
#pragma warning disable 0612, 0618
        private static readonly IComparer<CharsRef> UTF16_SORTED_AS_UTF8_SORT_ORDER = new Utf16SortedAsUtf8Comparator();
#pragma warning restore 0612, 0618

        /// @deprecated this comparator is only a transition mechanism
        [Obsolete("this comparator is only a transition mechanism")]
        public static IComparer<CharsRef> Utf16SortedAsUtf8Comparer
        {
            get { return UTF16_SORTED_AS_UTF8_SORT_ORDER; }
        }


        [Obsolete("this comparator is only a transition mechanism")]
        private sealed class Utf16SortedAsUtf8Comparator : IComparer<CharsRef>
        {
           

            public int Compare(CharsRef a, CharsRef b)
            {
                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (a == b)
                {
                    return 0;
                }

                var aChars = a.Chars;
                var aOffset = a.Offset;
                var bChars = b.Chars;
                var bOffset = b.Offset;

                var aStop = aOffset + Math.Min(a.Length, b.Length);

                while (aOffset < aStop)
                {
                    var aChar = aChars[aOffset++];
                    var bChar = bChars[bOffset++];

                    if (aChar == bChar)
                        continue;

                    
                    // http://icu-project.org/docs/papers/utf16_code_point_order.html

                    /* aChar != bChar, fix up each one if they're both in or above the surrogate range, then compare them */
                    if (aChar < 0xd800 || bChar < 0xd800) 
                        return aChar - bChar; // int must be 32 bits wide

                    //LUCENE TO-DO possible truncation or is char 16bit?
                    if (aChar >= 0xe000)
                    {
                        aChar -= (char) 0x800;
                    }
                    else
                    {
                        aChar += (char) 0x2000;
                    }

                    if (bChar >= 0xe000)
                    {
                        bChar -= (char) 0x800;
                    }
                    else
                    {
                        bChar += (char) 0x2000;
                    }

                    /* now aChar and bChar are in code point order */
                    return aChar - bChar; // int must be 32 bits wide
                    
                }

                // One is a prefix of the other, or, they are equal:
                return a.Length - b.Length;
            }
        }


        /// <summary>
        ///     Performs internal consistency checks.
        /// </summary>
        /// <returns>True</returns>
        /// <exception cref="System.InvalidOperationException">
        ///     <list type="bullet">
        ///         <item>Thrown when <see cref="BytesRef.Bytes" /> is null.</item>
        ///         <item>Thrown when <see cref="BytesRef.Length" /> is less than zero.</item>
        ///         <item>Thrown when <see cref="BytesRef.Length" /> is greater than <see cref="BytesRef.Bytes" />.Length.</item>
        ///         <item>Thrown when <see cref="BytesRef.Offset" /> is less than zero.</item>
        ///         <item>Thrown when <see cref="BytesRef.Offset" /> is greater than <see cref="BytesRef.Bytes" />.Length.</item>
        ///         <item>Thrown when <see cref="BytesRef.Offset" /> and <see cref="BytesRef.Length" /> is less than zero.</item>
        ///         <item>
        ///             Thrown when <see cref="BytesRef.Offset" /> and <see cref="BytesRef.Length" /> is greater than
        ///             <see cref="BytesRef.Bytes" />.Length.
        ///         </item>
        ///     </list>
        /// </exception>
        // this should be a method instead of a property due to the exceptions thrown. 
        public bool Valid()
        {
            if (this.Chars == null)
            {
                throw new InvalidOperationException("chars is null");
            }
            if (this.Length < 0)
            {
                throw new InvalidOperationException("length is negative: " + Length);
            }
            if (this.Length > this.Chars.Length)
            {
                throw new InvalidOperationException("length is out of bounds: " + Length + ",chars.length=" +
                                                    Chars.Length);
            }
            if (this.Offset < 0)
            {
                throw new InvalidOperationException("offset is negative: " + Offset);
            }
            if (this.Offset > this.Chars.Length)
            {
                throw new InvalidOperationException("offset out of bounds: " + Offset + ",chars.length=" + Chars.Length);
            }
            if (this.Offset + this.Length < 0)
            {
                throw new InvalidOperationException("offset+length is negative: offset=" + Offset + ",length=" + Length);
            }
            if (this.Offset + this.Length > this.Chars.Length)
            {
                throw new InvalidOperationException("offset+length out of bounds: offset=" + Offset + ",length=" +
                                                    Length + ",chars.length=" + Chars.Length);
            }
            return true;
        }

        public IEnumerator<char> GetEnumerator()
        {
            return new CharEnumerator(this.Chars, this.Offset, this.Offset + this.Length);
        }

        public class CharEnumerator : IEnumerator<char>, ICharSequence
        {
            private char[] chars;
            private int length;
            private int offset;
            private int position = -1;

            public CharEnumerator(char[] chars, int offset = 0, int length = 0)
            {
                if (length == 0 || length > chars.Length)
                    length = chars.Length;

                this.chars = chars;
                this.length = length;
                this.offset = offset;
                this.position = (this.offset - 1);
            }

            public int Length
            {
                get { return this.length; }
            }

            public char CharAt(int index)
            {
                return this.chars[index];
            }

            public ICharSequence SubSequence(int start, int end)
            {
                return new CharEnumerator(this.chars, start, end - start);
            }


            public char Current
            {
                get { return this.chars[this.position]; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                this.position = this.position + 1;
                return this.position < this.length;
            }

            public void Reset()
            {
                this.position = this.offset - 1;
            }

            public void Dispose()
            {
                this.Dispose(true);
            }

            protected virtual void Dispose(bool dispose)
            {
                if (dispose)
                {
                    this.chars = null;
                    this.offset = 0;
                    this.length = 0;
                    this.position = -1;
                }
            }

            ~CharEnumerator()
            {
                this.Dispose(false);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}