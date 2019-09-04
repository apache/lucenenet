/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Globalization;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Mimics Java's Character class.
    /// </summary>
    public class Character
    {
        private const char charNull = '\0';
        private const char charZero = '0';
        private const char charA = 'a';

        public const int MAX_RADIX = 36;
        public const int MIN_RADIX = 2;

        public const int MAX_CODE_POINT = 0x10FFFF;
        public const int MIN_CODE_POINT = 0x000000;

        public const char MAX_SURROGATE = '\uDFFF';
        public const char MIN_SURROGATE = '\uD800';

        public const char MIN_LOW_SURROGATE = '\uDC00';
        public const char MAX_LOW_SURROGATE = '\uDFFF';

        public const char MIN_HIGH_SURROGATE = '\uD800';
        public const char MAX_HIGH_SURROGATE = '\uDBFF';

        public const int MIN_SUPPLEMENTARY_CODE_POINT = 0x010000;

        private static readonly string digitKeys = "0Aa\u0660\u06f0\u0966\u09e6\u0a66\u0ae6\u0b66\u0be7\u0c66\u0ce6\u0d66\u0e50\u0ed0\u0f20\u1040\u1369\u17e0\u1810\uff10\uff21\uff41";

        private static readonly char[] digitValues = "90Z7zW\u0669\u0660\u06f9\u06f0\u096f\u0966\u09ef\u09e6\u0a6f\u0a66\u0aef\u0ae6\u0b6f\u0b66\u0bef\u0be6\u0c6f\u0c66\u0cef\u0ce6\u0d6f\u0d66\u0e59\u0e50\u0ed9\u0ed0\u0f29\u0f20\u1049\u1040\u1371\u1368\u17e9\u17e0\u1819\u1810\uff19\uff10\uff3a\uff17\uff5a\uff37"
            .ToCharArray();

        /// <summary>
        /// Convenience method to determine the value of the specified character
        /// <paramref name="c"/> in the supplied radix. The value of <paramref name="radix"/> must be
        /// between <see cref="MIN_RADIX"/> and <see cref="MAX_RADIX"/>.
        /// </summary>
        /// <param name="c">The character to determine the value of.</param>
        /// <param name="radix">The radix.</param>
        /// <returns>
        /// The value of <paramref name="c"/> in <paramref name="radix"/> if <paramref name="radix"/> lies
        /// between <see cref="MIN_RADIX"/> and <see cref="MAX_RADIX"/>; -1 otherwise.
        /// </returns>
        public static int Digit(char c, int radix)
        {
            int result = -1;
            if (radix >= MIN_RADIX && radix <= MAX_RADIX)
            {
                if (c < 128)
                {
                    // Optimized for ASCII
                    if ('0' <= c && c <= '9')
                    {
                        result = c - '0';
                    }
                    else if ('a' <= c && c <= 'z')
                    {
                        result = c - ('a' - 10);
                    }
                    else if ('A' <= c && c <= 'Z')
                    {
                        result = c - ('A' - 10);
                    }
                    return result < radix ? result : -1;
                }
                result = BinarySearchRange(digitKeys, c);
                if (result >= 0 && c <= digitValues[result * 2])
                {
                    int value = (char)(c - digitValues[result * 2 + 1]);
                    if (value >= radix)
                    {
                        return -1;
                    }
                    return value;
                }
            }
            return -1;
        }

        /// <summary>
        /// Search the sorted characters in the string and return the nearest index.
        /// </summary>
        /// <param name="data">The String to search.</param>
        /// <param name="c">The character to search for.</param>
        /// <returns>The nearest index.</returns>
        private static int BinarySearchRange(string data, char c)
        {
            char value = (char)0;
            int low = 0, mid = -1, high = data.Length - 1;
            while (low <= high)
            {
                mid = (low + high) >> 1;
                value = data[mid];
                if (c > value)
                    low = mid + 1;
                else if (c == value)
                    return mid;
                else
                    high = mid - 1;
            }
            return mid - (c < value ? 1 : 0);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="digit"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        public static char ForDigit(int digit, int radix)
        {
            // if radix or digit is out of range,
            // return the null character.
            if (radix < Character.MIN_RADIX)
                return charNull;
            if (radix > Character.MAX_RADIX)
                return charNull;
            if (digit < 0)
                return charNull;
            if (digit >= radix)
                return charNull;

            // if digit is less than 10,
            // return '0' plus digit
            if (digit < 10)
                return (char)((int)charZero + digit);

            // otherwise, return 'a' plus digit.
            return (char)((int)charA + digit - 10);
        }

        public static int ToChars(int codePoint, char[] dst, int dstIndex)
        {
            var converted = UnicodeUtil.ToCharArray(new[] {codePoint}, 0, 1);

            Array.Copy(converted, 0, dst, dstIndex, converted.Length);

            return converted.Length;
        }

        public static char[] ToChars(int codePoint)
        {
            return UnicodeUtil.ToCharArray(new[] {codePoint}, 0, 1);
        }

        public static int ToCodePoint(char high, char low)
        {
            // Optimized form of:
            // return ((high - MIN_HIGH_SURROGATE) << 10)
            //         + (low - MIN_LOW_SURROGATE)
            //         + MIN_SUPPLEMENTARY_CODE_POINT;
            return ((high << 10) + low) + (MIN_SUPPLEMENTARY_CODE_POINT
                                           - (MIN_HIGH_SURROGATE << 10)
                                           - MIN_LOW_SURROGATE);
        }

        public static int CodePointBefore(char[] seq, int index)
        {
            if (seq == null)
            {
                throw new ArgumentNullException(nameof(seq));
            }
            int len = seq.Length;
            if (index < 1 || index > len)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }

            char low = seq[--index];
            if (--index < 0)
            {
                return low;
            }
            char high = seq[index];
            if (char.IsSurrogatePair(high, low))
            {
                return ToCodePoint(high, low);
            }
            return low;
        }

        public static int ToLower(int codePoint)
        {
            var str = UnicodeUtil.NewString(new[] {codePoint}, 0, 1);

            str = str.ToLowerInvariant();

            return CodePointAt(str, 0);
        }

        public static int ToUpper(int codePoint)
        {
            var str = UnicodeUtil.NewString(new[] { codePoint }, 0, 1);

            str = str.ToUpperInvariant();

            return CodePointAt(str, 0);
        }

        public static int CharCount(int codePoint)
        {
            // A given codepoint can be represented in .NET either by 1 char (up to UTF16),
            // or by if it's a UTF32 codepoint, in which case the current char will be a surrogate
            return codePoint >= MIN_SUPPLEMENTARY_CODE_POINT ? 2 : 1;
        }

        /// <summary>
        /// Returns the number of Unicode code points in the text range of the specified char sequence. 
        /// The text range begins at the specified <paramref name="beginIndex"/> and extends to the char at index <c>endIndex - 1</c>. 
        /// Thus the length (in <see cref="char"/>s) of the text range is <c>endIndex-beginIndex</c>. 
        /// Unpaired surrogates within the text range count as one code point each.
        /// </summary>
        /// <param name="seq">the char sequence</param>
        /// <param name="beginIndex">the index to the first char of the text range.</param>
        /// <param name="endIndex">the index after the last char of the text range.</param>
        /// <returns>the number of Unicode code points in the specified text range</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// if the <paramref name="beginIndex"/> is negative, or <paramref name="endIndex"/> 
        /// is larger than the length of the given sequence, or <paramref name="beginIndex"/> 
        /// is larger than <paramref name="endIndex"/>.
        /// </exception>
        public static int CodePointCount(string seq, int beginIndex, int endIndex)
        {
            int length = seq.Length;
            if (beginIndex < 0 || endIndex > length || beginIndex > endIndex)
            {
                throw new IndexOutOfRangeException();
            }
            int n = endIndex - beginIndex;
            for (int i = beginIndex; i < endIndex;)
            {
                if (char.IsHighSurrogate(seq[i++]) && i < endIndex &&
                    char.IsLowSurrogate(seq[i]))
                {
                    n--;
                    i++;
                }
            }
            return n;
        }

        public static int CodePointCount(char[] a, int offset, int count)
        {
            if (count > a.Length - offset || offset < 0 || count < 0)
            {
                throw new IndexOutOfRangeException();
            }
            return CodePointCountImpl(a, offset, count);
        }

        internal static int CodePointCountImpl(char[] a, int offset, int count)
        {
            int endIndex = offset + count;
            int n = count;
            for (int i = offset; i < endIndex;)
            {
                if (char.IsHighSurrogate(a[i++]) && i < endIndex 
                    && char.IsLowSurrogate(a[i]))
                {
                    n--;
                    i++;
                }
            }
            return n;
        }

        public static int CodePointAt(string seq, int index)
        {
            char c1 = seq[index++];
            if (char.IsHighSurrogate(c1))
            {
                if (index < seq.Length)
                {
                    char c2 = seq[index];
                    if (char.IsLowSurrogate(c2))
                    {
                        return ToCodePoint(c1, c2);
                    }
                }
            }
            return c1;
        }

        public static int CodePointAt(char high, char low)
        {
            return ((high << 10) + low) + (MIN_SUPPLEMENTARY_CODE_POINT
                                       - (MIN_HIGH_SURROGATE << 10)
                                       - MIN_LOW_SURROGATE);
        }

        public static int CodePointAt(StringBuilder seq, int index)
        {
            char c1 = seq[index++];
            if (char.IsHighSurrogate(c1))
            {
                if (index < seq.Length)
                {
                    char c2 = seq[index];
                    if (char.IsLowSurrogate(c2))
                    {
                        return ToCodePoint(c1, c2);
                    }
                }
            }
            return c1;
        }

        public static int CodePointAt(ICharSequence seq, int index)
        {
            char c1 = seq[index++];
            if (char.IsHighSurrogate(c1))
            {
                if (index < seq.Length)
                {
                    char c2 = seq[index];
                    if (char.IsLowSurrogate(c2))
                    {
                        return ToCodePoint(c1, c2);
                    }
                }
            }
            return c1;
        }

        public static int CodePointAt(char[] a, int index, int limit)
        {
            if (index >= limit || limit < 0 || limit > a.Length)
            {
                throw new IndexOutOfRangeException();
            }
            return CodePointAtImpl(a, index, limit);
        }

        // throws ArrayIndexOutofBoundsException if index out of bounds
        static int CodePointAtImpl(char[] a, int index, int limit)
        {
            char c1 = a[index++];
            if (char.IsHighSurrogate(c1))
            {
                if (index < limit)
                {
                    char c2 = a[index];
                    if (char.IsLowSurrogate(c2))
                    {
                        return ToCodePoint(c1, c2);
                    }
                }
            }
            return c1;
        }

        /// <summary>
        /// Copy of the implementation from Character class in Java
        /// 
        /// http://grepcode.com/file/repository.grepcode.com/java/root/jdk/openjdk/6-b27/java/lang/Character.java
        /// </summary>
        public static int OffsetByCodePoints(string seq, int index,
                                         int codePointOffset)
        {
            int length = seq.Length;
            if (index < 0 || index > length)
            {
                throw new IndexOutOfRangeException();
            }

            int x = index;
            if (codePointOffset >= 0)
            {
                int i;
                for (i = 0; x < length && i < codePointOffset; i++)
                {
                    if (char.IsHighSurrogate(seq[x++]))
                    {
                        if (x < length && char.IsLowSurrogate(seq[x]))
                        {
                            x++;
                        }
                    }
                }
                if (i < codePointOffset)
                {
                    throw new IndexOutOfRangeException();
                }
            }
            else
            {
                int i;
                for (i = codePointOffset; x > 0 && i < 0; i++)
                {
                    if (char.IsLowSurrogate(seq[--x]))
                    {
                        if (x > 0 && char.IsHighSurrogate(seq[x - 1]))
                        {
                            x--;
                        }
                    }
                }
                if (i < 0)
                {
                    throw new IndexOutOfRangeException();
                }
            }
            return x;
        }

        /// <summary>
        /// Copy of the implementation from Character class in Java
        /// 
        /// http://grepcode.com/file/repository.grepcode.com/java/root/jdk/openjdk/6-b27/java/lang/Character.java
        /// </summary>
        public static int OffsetByCodePoints(char[] a, int start, int count,
                                         int index, int codePointOffset)
        {
            if (count > a.Length - start || start < 0 || count < 0
                || index < start || index > start + count)
            {
                throw new IndexOutOfRangeException();
            }
            return OffsetByCodePointsImpl(a, start, count, index, codePointOffset);
        }

        static int OffsetByCodePointsImpl(char[] a, int start, int count,
                                          int index, int codePointOffset)
        {
            int x = index;
            if (codePointOffset >= 0)
            {
                int limit = start + count;
                int i;
                for (i = 0; x < limit && i < codePointOffset; i++)
                {
                    if (Char.IsHighSurrogate(a[x++]) && x < limit && Char.IsLowSurrogate(a[x]))
                    {
                        x++;
                    }
                }
                if (i < codePointOffset)
                {
                    throw new IndexOutOfRangeException();
                }
            }
            else
            {
                int i;
                for (i = codePointOffset; x > start && i < 0; i++)
                {
                    if (Char.IsLowSurrogate(a[--x]) && x > start &&
                        Char.IsHighSurrogate(a[x - 1]))
                    {
                        x--;
                    }
                }
                if (i < 0)
                {
                    throw new IndexOutOfRangeException();
                }
            }
            return x;
        }

        public static bool IsLetter(int c)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory((char)c);

            return unicodeCategory == UnicodeCategory.LowercaseLetter ||
                   unicodeCategory == UnicodeCategory.UppercaseLetter ||
                   unicodeCategory == UnicodeCategory.TitlecaseLetter ||
                   unicodeCategory == UnicodeCategory.ModifierLetter ||
                   unicodeCategory == UnicodeCategory.OtherLetter;
        }

        /// <summary>
        /// LUCENENET safe way to get unicode category. The .NET <see cref="char.ConvertFromUtf32(int)"/>
        /// method should be used first to be safe for surrogate pairs. However, if the value falls between
        /// 0x00d800 and 0x00dfff, that method throws an exception. So this is a wrapper that converts the
        /// codepoint to a char in those cases.
        /// 
        /// This mimics the behavior of the Java Character.GetType class, but returns the .NET UnicodeCategory
        /// enumeration for easy consumption.
        /// </summary>
        /// <param name="codePoint"></param>
        /// <returns> A <see cref="UnicodeCategory"/> representing the <paramref name="codePoint"/>. </returns>
        public static UnicodeCategory GetType(int codePoint)
        {
            if ((codePoint >= 0x00d800) && (codePoint <= 0x00dfff))
                return CharUnicodeInfo.GetUnicodeCategory((char)codePoint);
            else
                return CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codePoint), 0);
        }
    }
}