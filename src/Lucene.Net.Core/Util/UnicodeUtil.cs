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
 * WITHbytes WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
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

    /// <summary>
    ///     Utility methods for dealing with unicode.
    /// </summary>
    public static class UnicodeUtil
    {
        public const int UNI_SUR_HIGH_START = 0xD800;
        public const int UNI_SUR_HIGH_END = 0xDBFF;
        public const int UNI_SUR_LOW_START = 0xDC00;
        public const int UNI_SUR_LOW_END = 0xDFFF;
        public const int UNI_REPLACEMENT_CHAR = 0xFFFD;
        public const int MAX_UTF8_BYTES_PER_CHAR = 4;
        private const int MIN_SUPPLEMENTARY_CODE_POINT = 0x10000;

        private const long UNI_MAX_BMP = 0x0000FFFF;

        private const long HALF_SHIFT = 10;
        private const long HALF_MASK = 0x3FFL;

        private const int SURROGATE_OFFSET =
            MIN_SUPPLEMENTARY_CODE_POINT - (UNI_SUR_HIGH_START << (int)HALF_SHIFT) - UNI_SUR_LOW_START;


        /// <summary>
        /// UTF16s to UTF8.
        /// </summary>
        /// <param name="sequence">The chars.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="bytes">The bytes.</param>
        /// <returns>System.Int32.</returns>
        public static int Utf16ToUtf8(ICharSequence sequence, int offset, int length, byte[] bytes)
        {
            int position = 0,
                i = offset,
                end = offset + length;


            // cast or convert. 
            var source = sequence;

            if (bytes == null)
                bytes = new byte[length];

            while (i < end)
            {
                int code = source.CharAt(i++);

                if (code < 0x80)
                {
                    bytes[position++] = (byte)code;
                }
                else if (code < 0x800)
                {
                    bytes[position++] = (byte)(0xC0 | (code >> 6));
                    bytes[position++] = (byte)(0x80 | (code & 0x3F));
                }
                else if (code < 0xD800 || code > 0xDFFF)
                {
                    bytes[position++] = (byte)(0xE0 | (code >> 12));
                    bytes[position++] = (byte)(0x80 | ((code >> 6) & 0x3F));
                    bytes[position++] = (byte)(0x80 | (code & 0x3F));
                }
                else
                {
                    // surrogate pair
                    // confirm valid high surrogate
                    if (code < 0xDC00 && i < end)
                    {
                        int utf32 = source.CharAt(i);
                        // confirm valid low surrogate and write pair
                        if (utf32 >= 0xDC00 && utf32 <= 0xDFFF)
                        {
                            utf32 = (code << 10) + utf32 + SURROGATE_OFFSET;
                            i++;
                            bytes[position++] = (byte)(0xF0 | (utf32 >> 18));
                            bytes[position++] = (byte)(0x80 | ((utf32 >> 12) & 0x3F));
                            bytes[position++] = (byte)(0x80 | ((utf32 >> 6) & 0x3F));
                            bytes[position++] = (byte)(0x80 | (utf32 & 0x3F));
                            continue;
                        }
                    }
                    // replace unpaired surrogate or bytes-of-order low surrogate
                    // with substitution character
                    bytes[position++] = (byte)0xEF;
                    bytes[position++] = (byte)0xBF;
                    bytes[position++] = (byte)0xBD;
                }
            }
            //assert matches(source, offset, length, bytes, position);
            return position;
        }

        /// <summary>
        /// UTF16s to UTF8.
        /// </summary>
        /// <param name="chars">The chars.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="bytes">The bytes.</param>
        /// <returns>System.Int32.</returns>
        public static int Utf16ToUtf8(IEnumerable<char> chars, int offset, int length, byte[] bytes)
        {
            int position = 0,
                i = offset,
                end = offset + length;


            // cast or convert. 
            var c = chars as char[];
            var source = c ?? chars.ToArray();

            if (bytes == null)
                bytes = new byte[length];

            while (i < end)
            {
                int code = source[i++];

                if (code < 0x80)
                {
                    bytes[position++] = (byte)code;
                }
                else if (code < 0x800)
                {
                    bytes[position++] = (byte)(0xC0 | (code >> 6));
                    bytes[position++] = (byte)(0x80 | (code & 0x3F));
                }
                else if (code < 0xD800 || code > 0xDFFF)
                {
                    bytes[position++] = (byte)(0xE0 | (code >> 12));
                    bytes[position++] = (byte)(0x80 | ((code >> 6) & 0x3F));
                    bytes[position++] = (byte)(0x80 | (code & 0x3F));
                }
                else
                {
                    // surrogate pair
                    // confirm valid high surrogate
                    if (code < 0xDC00 && i < end)
                    {
                        int utf32 = (int)source[i];
                        // confirm valid low surrogate and write pair
                        if (utf32 >= 0xDC00 && utf32 <= 0xDFFF)
                        {
                            utf32 = (code << 10) + utf32 + SURROGATE_OFFSET;
                            i++;
                            bytes[position++] = (byte)(0xF0 | (utf32 >> 18));
                            bytes[position++] = (byte)(0x80 | ((utf32 >> 12) & 0x3F));
                            bytes[position++] = (byte)(0x80 | ((utf32 >> 6) & 0x3F));
                            bytes[position++] = (byte)(0x80 | (utf32 & 0x3F));
                            continue;
                        }
                    }
                    // replace unpaired surrogate or bytes-of-order low surrogate
                    // with substitution character
                    bytes[position++] = (byte)0xEF;
                    bytes[position++] = (byte)0xBF;
                    bytes[position++] = (byte)0xBD;
                }
            }
            //assert matches(source, offset, length, bytes, position);
            return position;
        }




        /// <summary>
        ///     Interprets the given byte array as UTF-8 and converts to UTF-16. The <seealso cref="CharsRef" /> will be extended
        ///     if
        ///     it doesn't provide enough space to hold the worst case of each byte becoming a UTF-16 code point.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         NOTE: Full characters are read, even if this reads past the length passed (and
        ///         can result in an ArraybytesOfBoundsException if invalid UTF-8 is passed).
        ///         Explicit checks for valid UTF-8 are not performed.
        ///     </para>
        /// </remarks>
        // TODO: broken if chars.offset != 0
        public static int Utf8ToUtf16(byte[] utf8Bytes, int offset, int length, char[] chars)
        {
            int charsOffset = 0,
               limit = offset + length;
            while (offset < limit)
            {
                int b = utf8Bytes[offset++] & 0xff;
                if (b < 0xc0)
                {
                    Debug.Assert(b < 0x80);
                    chars[charsOffset++] = (char)b;
                }
                else if (b < 0xe0)
                {
                    chars[charsOffset++] = (char)(((b & 0x1f) << 6) + (utf8Bytes[offset++] & 0x3f));
                }
                else if (b < 0xf0)
                {
                    chars[charsOffset++] = (char)(((b & 0xf) << 12) + ((utf8Bytes[offset] & 0x3f) << 6) + (utf8Bytes[offset + 1] & 0x3f));
                    offset += 2;
                }
                else
                {
                    Debug.Assert(b < 0xf8, "b = 0x" + BitConverter.ToString(new[] { (byte)b }));


                    var ch = ((b & 0x7) << 18) + ((utf8Bytes[offset] & 0x3f) << 12) + ((utf8Bytes[offset + 1] & 0x3f) << 6) + (utf8Bytes[offset + 2] & 0x3f);
                    offset += 3;
                    if (ch < UNI_MAX_BMP)
                    {
                        chars[charsOffset++] = (char)ch;
                    }
                    else
                    {
                        int chHalf = ch - 0x0010000;
                        chars[charsOffset++] = (char)((chHalf >> 10) + 0xD800);
                        chars[charsOffset++] = (char)((chHalf & HALF_MASK) + 0xDC00);
                    }
                }
            }
            return charsOffset;


        }
    }
}