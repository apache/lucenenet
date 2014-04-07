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
            return new[] {(char)codePoint};
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
            // .NET chars are always length 1
            return 1;
        }

        public static bool IsLowSurrogate(char ch)
        {
            return ch >= MIN_LOW_SURROGATE && ch <= MAX_LOW_SURROGATE;
        }

        public static bool IsHighSurrogate(char ch)
        {
            return ch >= MIN_HIGH_SURROGATE && ch <= MAX_HIGH_SURROGATE;
        }
    }
}
