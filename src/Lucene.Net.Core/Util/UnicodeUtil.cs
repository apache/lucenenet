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
    using System.Text;

    /// <summary>
    /// Utility methods for dealing with unicode.
    /// </summary>
    public static class UnicodeUtil
    {
        public const int UNI_SUR_HIGH_START = 0xD800;
        public const int UNI_SUR_HIGH_END = 0xDBFF;
        public const int UNI_SUR_LOW_START = 0xDC00;
        public const int UNI_SUR_LOW_END = 0xDFFF;
        public const int UNI_REPLACEMENT_CHAR = 0xFFFD;
        private const int MIN_SUPPLEMENTARY_CODE_POINT = 0x10000;

        private const long UNI_MAX_BMP = 0x0000FFFF;

        private const long HALF_SHIFT = 10;
        private const long HALF_MASK = 0x3FFL;

        private static readonly int SURROGATE_OFFSET = MIN_SUPPLEMENTARY_CODE_POINT - (UNI_SUR_HIGH_START << (int)HALF_SHIFT) - UNI_SUR_LOW_START;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="result"></param>
        public static void UTF16toUTF8(IEnumerable<char> chars, int offset, int length, BytesRef result)
        {
            int end = offset + length;

            byte[] @out = result.Bytes;
            result.Offset = 0;
            // Pre-allocate for worst case 4-for-1
            int maxLen = length * 4;
            if (@out.Length < maxLen)
            {
                @out = result.Bytes = new byte[maxLen];
            }

            int upto = 0;
            for (int i = offset; i < end; i++)
            {
                int code = (int)chars.ElementAt(i);

                if (code < 0x80)
                {
                    @out[upto++] = (byte)code;
                }
                else if (code < 0x800)
                {
                    @out[upto++] = unchecked((byte)(0xC0 | (code >> 6)));
                    @out[upto++] = unchecked((byte)(0x80 | (code & 0x3F)));
                }
                else if (code < 0xD800 || code > 0xDFFF)
                {
                    @out[upto++] = unchecked((byte)(0xE0 | (code >> 12)));
                    @out[upto++] = unchecked((byte)(0x80 | ((code >> 6) & 0x3F)));
                    @out[upto++] = unchecked((byte)(0x80 | (code & 0x3F)));
                }
                else
                {
                    // surrogate pair
                    // confirm valid high surrogate
                    if (code < 0xDC00 && (i < end - 1))
                    {
                        int utf32 = (int)chars.ElementAt(i + 1);
                        // confirm valid low surrogate and write pair
                        if (utf32 >= 0xDC00 && utf32 <= 0xDFFF)
                        {
                            utf32 = (code << 10) + utf32 + SURROGATE_OFFSET;
                            i++;
                            @out[upto++] = unchecked((byte)(0xF0 | (utf32 >> 18)));
                            @out[upto++] = unchecked((byte)(0x80 | ((utf32 >> 12) & 0x3F)));
                            @out[upto++] = unchecked((byte)(0x80 | ((utf32 >> 6) & 0x3F)));
                            @out[upto++] = unchecked((byte)(0x80 | (utf32 & 0x3F)));
                            continue;
                        }
                    }
                    // replace unpaired surrogate or out-of-order low surrogate
                    // with substitution character
                    @out[upto++] = (byte)0xEF;
                    @out[upto++] = (byte)0xBF;
                    @out[upto++] = (byte)0xBD;
                }
            }
            //assert matches(s, offset, length, out, upto);
            result.Length = upto;
        }



        /// <summary>
        /// Interprets the given byte array as UTF-8 and converts to UTF-16. The <seealso cref="CharsRef"/> will be extended if
        /// it doesn't provide enough space to hold the worst case of each byte becoming a UTF-16 codepoint.
        /// <p>
        /// NOTE: Full characters are read, even if this reads past the length passed (and
        /// can result in an ArrayOutOfBoundsException if invalid UTF-8 is passed).
        /// Explicit checks for valid UTF-8 are not performed.
        /// </summary>
        // TODO: broken if chars.offset != 0
        public static int UTF8toUTF16(byte[] utf8, int offset, int length, CharsRef chars)
        {
            int outOffset = chars.Offset = 0,
                limit = offset + length;

            char[] @out = chars.Chars = ArrayUtil.Grow(chars.Chars, length);
  
            while (offset < limit)
            {
                int b = utf8[offset++] & 0xff;
                if (b < 0xc0)
                {
                    Debug.Assert(b < 0x80);
                    @out[outOffset++] = (char)b;
                }
                else if (b < 0xe0)
                {
                    @out[outOffset++] = (char)(((b & 0x1f) << 6) + (utf8[offset++] & 0x3f));
                }
                else if (b < 0xf0)
                {
                    @out[outOffset++] = (char)(((b & 0xf) << 12) + ((utf8[offset] & 0x3f) << 6) + (utf8[offset + 1] & 0x3f));
                    offset += 2;
                }
                else
                {
                    Debug.Assert(b < 0xf8, "b = 0x" + b.ToString("x"));

                    int ch = ((b & 0x7) << 18) + ((utf8[offset] & 0x3f) << 12) + ((utf8[offset + 1] & 0x3f) << 6) + (utf8[offset + 2] & 0x3f);
                    offset += 3;
                    if (ch < UNI_MAX_BMP)
                    {
                        @out[outOffset++] = (char)ch;
                    }
                    else
                    {
                        int chHalf = ch - 0x0010000;
                        @out[outOffset++] = (char)((chHalf >> 10) + 0xD800);
                        @out[outOffset++] = (char)((chHalf & HALF_MASK) + 0xDC00);
                    }
                }
            }
            chars.Length = outOffset - chars.Offset;

            return chars.Length;
        }
    }
}
