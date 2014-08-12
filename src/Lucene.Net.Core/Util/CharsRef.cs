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
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Represents char[], as a slice (offset + length) into an existing char[].
    /// The <seealso cref="#chars"/> member should never be null; use
    /// <seealso cref="#EMPTY_CHARS"/> if necessary.
    /// @lucene.internal
    /// </summary>
    public sealed class CharsRef : IComparable<CharsRef>, 
        ICharSequence, 
        Lucene.Net.Support.ICloneable, 
        IEnumerable<char>
    {
       

        /// <summary>
        /// An empty character array for convenience </summary>
        public static readonly char[] EMPTY_CHARS = new char[0];

        /// <summary>
        /// The contents of the CharsRef. Should never be <see cref="Null"/>. </summary>
        public char[] Chars { get; set; }

        /// <summary>
        /// Offset of first valid character. </summary>
        public int Offset {get; set;}

        /// <summary>
        /// Length of used characters. </summary>
        public int Length { get; internal set; }

     
        /// <summary>
        /// Creates a new <seealso cref="CharsRef"/> initialized an empty array zero-length
        /// </summary>
        public CharsRef()
            : this(EMPTY_CHARS, 0, 0)
        {
        }

        /// <summary>
        /// Creates a new <seealso cref="CharsRef"/> initialized with an array of the given
        /// capacity
        /// </summary>
        public CharsRef(int capacity)
        {
            this.Chars = new char[capacity];
        }

        /// <summary>
        /// Creates a new <seealso cref="CharsRef"/> initialized with the given array, offset and
        /// length
        /// </summary>
        public CharsRef(char[] chars, int offset, int length)
        {
            this.Chars = chars;
            this.Offset = offset;
            this.Length = length;
            Debug.Assert(Valid);
        }

        /// <summary>
        /// Creates a new <seealso cref="CharsRef"/> initialized with the given Strings character
        /// array
        /// </summary>
        public CharsRef(string @string)
        {
            this.Chars = @string.ToCharArray();
            this.Offset = 0;
            this.Length = Chars.Length;
        }

        /// <summary>
        /// Returns a shallow clone of this instance (the underlying characters are
        /// <b>not</b> copied and will be shared by both the returned object and this
        /// object.
        /// </summary>
        /// <seealso cref= #deepCopyOf </seealso>
        public object Clone(bool deepClone = false)
        {
            if (deepClone)
                throw new DeepCloneNotSupportedException(typeof(CharsRef));

            return new CharsRef(Chars, Offset, Length);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 0;
            int end = Offset + Length;
            for (int i = Offset; i < end; i++)
            {
                result = prime * result + Chars[i];
            }
            return result;
        }

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }
            if (other is CharsRef)
            {
                return this.CharsEquals((CharsRef)other);
            }
            return false;
        }

        public bool CharsEquals(CharsRef other)
        {
            if (Length == other.Length)
            {
                int otherUpto = other.Offset;
                char[] otherChars = other.Chars;
                int end = this.Offset + this.Length;
                for (int upto = Offset; upto < end; upto++, otherUpto++)
                {
                    if (this.Chars[upto] != otherChars[otherUpto])
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Signed int order comparison </summary>
        public int CompareTo(CharsRef other)
        {
            if (this == other)
            {
                return 0;
            }

            char[] aChars = this.Chars;
            int aUpto = this.Offset;
            char[] bChars = other.Chars;
            int bUpto = other.Offset;

            int aStop = aUpto + Math.Min(this.Length, other.Length);

            while (aUpto < aStop)
            {
                int aInt = aChars[aUpto++];
                int bInt = bChars[bUpto++];
                if (aInt > bInt)
                {
                    return 1;
                }
                else if (aInt < bInt)
                {
                    return -1;
                }
            }

            // One is a prefix of the other, or, they are equal:
            return this.Length - other.Length;
        }

        /// <summary>
        /// Copies the given <seealso cref="CharsRef"/> referenced content into this instance.
        /// </summary>
        /// <param name="other">
        ///          the <seealso cref="CharsRef"/> to copy </param>
        public void CopyChars(CharsRef other)
        {
            CopyChars(other.Chars, other.Offset, other.Length);
        }

        /// <summary>
        /// Used to grow the reference array.
        ///
        /// In general this should not be used as it does not take the offset into account.
        /// @lucene.internal
        /// </summary>
        public void Grow(int newLength)
        {
            Debug.Assert(Offset == 0);
            if (this.Chars.Length < newLength)
            {
                this.Chars = ArrayUtil.Grow(this.Chars, newLength);
            }
        }

        /// <summary>
        /// Copies the given array into this CharsRef.
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
        /// Appends the given array to this CharsRef
        /// </summary>
        public void Append(char[] otherChars, int otherOffset, int otherLength)
        {
            int newLen = Length + otherLength;
            if (this.Chars.Length - this.Offset < newLen)
            {
                char[] newChars = new char[newLen];
                Array.Copy(this.Chars, this.Offset, newChars, 0, Length);
                this.Offset = 0;
                this.Chars = newChars;
            }
            Array.Copy(otherChars, otherOffset, this.Chars, this.Length + this.Offset, otherLength);
            Length = newLen;
        }

        public override string ToString()
        {
            return new string(this.Chars, this.Offset, this.Length);
        }

        public char CharAt(int index)
        {
            // NOTE: must do a real check here to meet the specs of CharSequence
            if (index < 0 || index >= Length)
            {
                throw new System.IndexOutOfRangeException();
            }
            return this.Chars[this.Offset + index];
        }

        public ICharSequence SubSequence(int start, int end)
        {
            // NOTE: must do a real check here to meet the specs of CharSequence
            if (start < 0 || end > Length || start > end)
            {
                throw new System.IndexOutOfRangeException();
            }
            return new CharsRef(this.Chars, this.Offset + start, end - start);
        }

        /// @deprecated this comparator is only a transition mechanism
#pragma warning disable 0612, 0618
        private static readonly IComparer<CharsRef> Utf16SortedAsUTF8SortOrder = new UTF16SortedAsUTF8Comparator();
#pragma warning restore 0612, 0618

        /// @deprecated this comparator is only a transition mechanism
        [Obsolete("this comparator is only a transition mechanism")]
        public static IComparer<CharsRef> UTF16SortedAsUTF8Comparer
        {
            get
            {
                return Utf16SortedAsUTF8SortOrder;
            }
        }

        
        [Obsolete("this comparator is only a transition mechanism")]
        private class UTF16SortedAsUTF8Comparator : IComparer<CharsRef>
        {
            // Only singleton
            internal UTF16SortedAsUTF8Comparator()
            {
            }

            public virtual int Compare(CharsRef a, CharsRef b)
            {
                if (a == b)
                {
                    return 0;
                }

                char[] aChars = a.Chars;
                int aUpto = a.Offset;
                char[] bChars = b.Chars;
                int bUpto = b.Offset;

                int aStop = aUpto + Math.Min(a.Length, b.Length);

                while (aUpto < aStop)
                {
                    char aChar = aChars[aUpto++];
                    char bChar = bChars[bUpto++];
                    if (aChar != bChar)
                    {
                        // http://icu-project.org/docs/papers/utf16_code_point_order.html

                        /* aChar != bChar, fix up each one if they're both in or above the surrogate range, then compare them */
                        if (aChar >= 0xd800 && bChar >= 0xd800)
                        {//LUCENE TO-DO possible truncation or is char 16bit?
                            if (aChar >= 0xe000)
                            {
                                aChar -= (char)0x800;
                            }
                            else
                            {
                                aChar += (char)0x2000;
                            }

                            if (bChar >= 0xe000)
                            {
                                bChar -= (char)0x800;
                            }
                            else
                            {
                                bChar += (char)0x2000;
                            }
                        }

                        /* now aChar and bChar are in code point order */
                        return (int)aChar - (int)bChar; // int must be 32 bits wide
                    }
                }

                // One is a prefix of the other, or, they are equal:
                return a.Length - b.Length;
            }
        }

        /// <summary>
        /// Creates a new CharsRef that points to a copy of the chars from
        /// <code>other</code>
        /// <p>
        /// The returned CharsRef will have a length of other.length
        /// and an offset of zero.
        /// </summary>
        public static CharsRef DeepCopyOf(CharsRef other)
        {
            CharsRef clone = new CharsRef();
            clone.CopyChars(other);
            return clone;
        }

        /// <summary>
        /// Performs internal consistency checks.
        /// Always returns true (or throws InvalidOperationException)
        /// </summary>
        public bool Valid
        {
            get
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
                    throw new InvalidOperationException("length is out of bounds: " + Length + ",chars.length=" + Chars.Length);
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
                    throw new InvalidOperationException("offset+length out of bounds: offset=" + Offset + ",length=" + Length + ",chars.length=" + Chars.Length);
                }
                return true;
            }
        }

        public IEnumerator<char> GetEnumerator()
        {
            return ((IEnumerable<char>)this.Chars).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.Chars.GetEnumerator();
        }
    }
}