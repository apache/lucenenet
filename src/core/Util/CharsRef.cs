using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Util
{
    public class CharsRef : IComparable<CharsRef>, ICloneable, ICharSequence
    {
        public static readonly char[] EMPTY_CHARS = new char[0];

        public char[] chars;

        public int offset;

        public int length;

        public CharsRef()
            : this(EMPTY_CHARS, 0, 0)
        {
        }

        public CharsRef(int capacity)
        {
            chars = new char[capacity];
        }

        public CharsRef(char[] chars, int offset, int length)
        {
            this.chars = chars;
            this.offset = offset;
            this.length = length;
        }

        public CharsRef(String @string)
        {
            this.chars = @string.ToCharArray();
            this.offset = 0;
            this.length = chars.Length;
        }

        public object Clone()
        {
            return new CharsRef(chars, offset, length);
        }

        public override int GetHashCode()
        {
            const int prime = 31;

            int result = 0;
            int end = offset + length;

            for (int i = offset; i < end; i++)
            {
                result = prime * result + chars[i];
            }

            return result;
        }

        public override bool Equals(object other)
        {
            if (other == null)
                return false;

            var cr = other as CharsRef;

            if (cr != null)
                return this.CharsEquals(cr);

            return false;
        }

        public bool CharsEquals(CharsRef other)
        {
            if (length == other.length)
            {
                int otherUpto = other.offset;
                char[] otherChars = other.chars;
                int end = offset + length;
                for (int upto = offset; upto < end; upto++, otherUpto++)
                {
                    if (chars[upto] != otherChars[otherUpto])
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

        public int CompareTo(CharsRef other)
        {
            if (this == other)
                return 0;

            char[] aChars = this.chars;
            int aUpto = this.offset;
            char[] bChars = other.chars;
            int bUpto = other.offset;

            int aStop = aUpto + Math.Min(this.length, other.length);

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
            return this.length - other.length;
        }

        public void CopyChars(CharsRef other)
        {
            CopyChars(other.chars, other.offset, other.length);
        }

        public void Grow(int newLength)
        {
            Trace.Assert(offset == 0);

            if (chars.Length < newLength)
            {
                chars = ArrayUtil.Grow(chars, newLength);
            }
        }

        public void CopyChars(char[] otherChars, int otherOffset, int otherLength)
        {
            if (chars.Length - offset < otherLength)
            {
                chars = new char[otherLength];
                offset = 0;
            }

            Array.Copy(otherChars, otherOffset, chars, offset, otherLength);
            length = otherLength;
        }

        public void Append(char[] otherChars, int otherOffset, int otherLength)
        {
            int newLen = length + otherLength;
            if (chars.Length - offset < newLen)
            {
                char[] newChars = new char[newLen];
                Array.Copy(chars, offset, newChars, 0, length);
                offset = 0;
                chars = newChars;
            }
            Array.Copy(otherChars, otherOffset, chars, length + offset, otherLength);
            length = newLen;
        }

        public override string ToString()
        {
            return new string(chars, offset, length);
        }

        /// <summary>
        /// For compatibility with CharSequence
        /// </summary>
        public int Length
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
            }
        }

        /// <summary>
        /// For compatibility with CharSequence
        /// </summary>
        public char CharAt(int index)
        {
            if (index < 0 || index >= length)
                throw new IndexOutOfRangeException();

            return chars[offset + index];
        }

        /// <summary>
        /// For compatibility with CharSequence
        /// </summary>
        public ICharSequence SubSequence(int start, int end)
        {
            if (start < 0 || end > length || start > end)
            {
                throw new IndexOutOfRangeException();
            }

            return new CharsRef(chars, offset + start, end - start);
        }

        private static readonly IComparer<CharsRef> utf16SortedAsUTF8SortOrder = new UTF16SortedAsUTF8ComparatorImpl();

        public static IComparer<CharsRef> UTF16SortedAsUTF8Comparator
        {
            get { return utf16SortedAsUTF8SortOrder; }
        }

        private class UTF16SortedAsUTF8ComparatorImpl : Comparer<CharsRef>
        {
            public UTF16SortedAsUTF8ComparatorImpl()
            {
            }

            public override int Compare(CharsRef a, CharsRef b)
            {
                if (a == b)
                    return 0;

                char[] aChars = a.chars;
                int aUpto = a.offset;
                char[] bChars = b.chars;
                int bUpto = b.offset;

                int aStop = aUpto + Math.Min(a.length, b.length);

                while (aUpto < aStop)
                {
                    char aChar = aChars[aUpto++];
                    char bChar = bChars[bUpto++];
                    if (aChar != bChar)
                    {
                        // http://icu-project.org/docs/papers/utf16_code_point_order.html

                        /* aChar != bChar, fix up each one if they're both in or above the surrogate range, then compare them */
                        if (aChar >= 0xd800 && bChar >= 0xd800)
                        {
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
                        return (int)aChar - (int)bChar; /* int must be 32 bits wide */
                    }
                }

                // One is a prefix of the other, or, they are equal:
                return a.length - b.length;
            }
        }

        [Obsolete("This comparer is only a transition mechanism")]
        private static readonly Comparer<CharsRef> utf8SortedAsUTF16SortOrder = new UTF8SortedAsUTF16ComparerImpl();

        [Obsolete("This comparer is only a transition mechanism")]
        public static Comparer<CharsRef> UTF8SortedAsUTF16Comparer
        {
            get
            {
                return utf8SortedAsUTF16SortOrder;
            }
        }

        [Obsolete("This comparer is only a transition mechanism")]
        private sealed class UTF8SortedAsUTF16ComparerImpl : Comparer<CharsRef>
        {
            public UTF8SortedAsUTF16ComparerImpl()
            {
            }

            public override int Compare(CharsRef a, CharsRef b)
            {
                if (a == b)
                    return 0;

                char[] aChars = a.chars;
                int aUpto = a.offset;
                char[] bChars = b.chars;
                int bUpto = b.offset;

                int aStop = aUpto + Math.Min(a.length, b.length);

                while (aUpto < aStop)
                {
                    char aChar = aChars[aUpto++];
                    char bChar = bChars[bUpto++];
                    if (aChar != bChar)
                    {
                        // http://icu-project.org/docs/papers/utf16_code_point_order.html

                        /* aChar != bChar, fix up each one if they're both in or above the surrogate range, then compare them */
                        if (aChar >= 0xd800 && bChar >= 0xd800)
                        {
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
                        return (int)aChar - (int)bChar; /* int must be 32 bits wide */
                    }
                }

                // One is a prefix of the other, or, they are equal:
                return a.length - b.length;
            }
        }

        public static CharsRef DeepCopyOf(CharsRef other)
        {
            CharsRef clone = new CharsRef();
            clone.CopyChars(other);
            return clone;
        }

        public bool IsValid()
        {
            if (chars == null)
            {
                throw new InvalidOperationException("chars is null");
            }
            if (length < 0)
            {
                throw new InvalidOperationException("length is negative: " + length);
            }
            if (length > chars.Length)
            {
                throw new InvalidOperationException("length is out of bounds: " + length + ",chars.length=" + chars.Length);
            }
            if (offset < 0)
            {
                throw new InvalidOperationException("offset is negative: " + offset);
            }
            if (offset > chars.Length)
            {
                throw new InvalidOperationException("offset out of bounds: " + offset + ",chars.length=" + chars.Length);
            }
            if (offset + length < 0)
            {
                throw new InvalidOperationException("offset+length is negative: offset=" + offset + ",length=" + length);
            }
            if (offset + length > chars.Length)
            {
                throw new InvalidOperationException("offset+length out of bounds: offset=" + offset + ",length=" + length + ",chars.length=" + chars.Length);
            }
            return true;
        }
    }
}
