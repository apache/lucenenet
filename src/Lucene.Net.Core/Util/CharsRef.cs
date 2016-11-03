using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Util
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
    /// Represents char[], as a slice (offset + Length) into an existing char[].
    /// The <seealso cref="#chars"/> member should never be null; use
    /// <seealso cref="#EMPTY_CHARS"/> if necessary.
    /// @lucene.internal
    /// </summary>
    public sealed class CharsRef : IComparable<CharsRef>, ICharSequence
    {
        /// <summary>
        /// An empty character array for convenience </summary>
        public static readonly char[] EMPTY_CHARS = new char[0];

        /// <summary>
        /// The contents of the CharsRef. Should never be {@code null}.
        /// </summary>
        public char[] Chars { get; set; }

        /// <summary>
        /// Offset of first valid character. </summary>
        public int Offset { get; internal set; }

        /// <summary>
        /// Length of used characters. </summary>
        public int Length { get; set; }

        /// <summary>
        /// Creates a new <seealso cref="CharsRef"/> initialized an empty array zero-Length
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
            Chars = new char[capacity];
        }

        /// <summary>
        /// Creates a new <seealso cref="CharsRef"/> initialized with the given array, offset and
        /// Length
        /// </summary>
        public CharsRef(char[] chars, int offset, int Length)
        {
            this.Chars = chars;
            this.Offset = offset;
            this.Length = Length;
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
        public object Clone()
        {
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

            var @ref = other as CharsRef;
            if (@ref != null)
            {
                return this.CharsEquals(@ref);
            }
            return false;
        }

        public bool CharsEquals(CharsRef other)
        {
            if (Length == other.Length)
            {
                int otherUpto = other.Offset;
                char[] otherChars = other.Chars;
                int end = Offset + Length;
                for (int upto = Offset; upto < end; upto++, otherUpto++)
                {
                    if (Chars[upto] != otherChars[otherUpto])
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
            if (Chars.Length < newLength)
            {
                Chars = ArrayUtil.Grow(Chars, newLength);
            }
        }

        /// <summary>
        /// Copies the given array into this CharsRef.
        /// </summary>
        public void CopyChars(char[] otherChars, int otherOffset, int otherLength)
        {
            if (Chars.Length - Offset < otherLength)
            {
                Chars = new char[otherLength];
                Offset = 0;
            }
            Array.Copy(otherChars, otherOffset, Chars, Offset, otherLength);
            Length = otherLength;
        }

        /// <summary>
        /// Appends the given array to this CharsRef
        /// </summary>
        public void Append(char[] otherChars, int otherOffset, int otherLength)
        {
            int newLen = Length + otherLength;
            if (Chars.Length - Offset < newLen)
            {
                var newChars = new char[newLen];
                Array.Copy(Chars, Offset, newChars, 0, Length);
                Offset = 0;
                Chars = newChars;
            }
            Array.Copy(otherChars, otherOffset, Chars, Length + Offset, otherLength);
            Length = newLen;
        }

        public override string ToString()
        {
            return new string(Chars, Offset, Length);
        }

        public char CharAt(int index)
        {
            // NOTE: must do a real check here to meet the specs of CharSequence
            if (index < 0 || index >= Length)
            {
                throw new System.IndexOutOfRangeException();
            }
            return Chars[Offset + index];
        }

        // LUCENENET specific - added to .NETify
        public char this[int index]
        {
            get
            {
                // NOTE: must do a real check here to meet the specs of CharSequence
                if (index < 0 || index >= Length)
                {
                    throw new IndexOutOfRangeException();
                }
                return Chars[Offset + index];
            }
        }

        public ICharSequence SubSequence(int start, int end)
        {
            // NOTE: must do a real check here to meet the specs of CharSequence
            if (start < 0 || end > Length || start > end)
            {
                throw new System.IndexOutOfRangeException();
            }
            return new CharsRef(Chars, Offset + start, end - start);
        }

        /// @deprecated this comparator is only a transition mechanism
        [Obsolete("this comparator is only a transition mechanism")]
        private static readonly IComparer<CharsRef> Utf16SortedAsUTF8SortOrder = new UTF16SortedAsUTF8Comparator();

        /// @deprecated this comparator is only a transition mechanism
        [Obsolete("this comparator is only a transition mechanism")]
        public static IComparer<CharsRef> UTF16SortedAsUTF8Comparer
        {
            get
            {
                return Utf16SortedAsUTF8SortOrder;
            }
        }

        /// @deprecated this comparator is only a transition mechanism
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
        /// The returned CharsRef will have a Length of other.Length
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
                if (Chars == null)
                {
                    throw new InvalidOperationException("chars is null");
                }
                if (Length < 0)
                {
                    throw new InvalidOperationException("Length is negative: " + Length);
                }
                if (Length > Chars.Length)
                {
                    throw new InvalidOperationException("Length is out of bounds: " + Length + ",chars.Length=" + Chars.Length);
                }
                if (Offset < 0)
                {
                    throw new InvalidOperationException("offset is negative: " + Offset);
                }
                if (Offset > Chars.Length)
                {
                    throw new InvalidOperationException("offset out of bounds: " + Offset + ",chars.Length=" + Chars.Length);
                }
                if (Offset + Length < 0)
                {
                    throw new InvalidOperationException("offset+Length is negative: offset=" + Offset + ",Length=" + Length);
                }
                if (Offset + Length > Chars.Length)
                {
                    throw new InvalidOperationException("offset+Length out of bounds: offset=" + Offset + ",Length=" + Length + ",chars.Length=" + Chars.Length);
                }
                return true;
            }
        }
    }
}