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
            // .NET Port: we don't have to do anything funky with surrogates here. chars are always UTF-16.
            dst[dstIndex] = (char)codePoint;
            return 1; // always 1 char written in .NET
        }

        public static char[] ToChars(int codePoint)
        {
            // .NET Port: we don't have to do anything funky with surrogates here. chars are always UTF-16.
            return new[] { (char)codePoint };
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
            // .NET Port: chars are always UTF-16 in .NET
            return (int)char.ToLower((char)codePoint);
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
    }
}