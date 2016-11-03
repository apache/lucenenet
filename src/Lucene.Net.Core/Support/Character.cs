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

        public static int MIN_SUPPLEMENTARY_CODE_POINT = 0x010000;

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

        public static int ToLowerCase(int codePoint)
        {
            var str = UnicodeUtil.NewString(new[] {codePoint}, 0, 1);

            str = str.ToLowerInvariant();

            return CodePointAt(str, 0);
        }

        public static int ToUpperCase(int codePoint)
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
            int n = 0;
            for (int i = offset; i < endIndex;)
            {
                n++;
                if (char.IsHighSurrogate(a[i++]))
                {
                    if (i < endIndex && char.IsLowSurrogate(a[i]))
                    {
                        i++;
                    }
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

        public static int CodePointAt(ICharSequence seq, int index)
        {
            char c1 = seq.CharAt(index++);
            if (char.IsHighSurrogate(c1))
            {
                if (index < seq.Length)
                {
                    char c2 = seq.CharAt(index);
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
        /// LUCENENET safe way to get unicode category. The .NET <seealso cref="char.ConvertFromUtf32(int)"/>
        /// method should be used first to be safe for surrogate pairs. However, if the value falls between
        /// 0x00d800 and 0x00dfff, that method throws an exception. So this is a wrapper that converts the
        /// codepoint to a char in those cases.
        /// 
        /// This mimics the behavior of the Java Character.GetType class, but returns the .NET UnicodeCategory
        /// enumeration for easy consumption.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static UnicodeCategory GetType(int codePoint)
        {
            if ((codePoint >= 0x00d800) && (codePoint <= 0x00dfff))
                return CharUnicodeInfo.GetUnicodeCategory((char)codePoint);
            else
                return CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codePoint), 0);
        }
    }
}