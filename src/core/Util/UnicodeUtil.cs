/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Support;
using System;
using System.Text;

namespace Lucene.Net.Util
{


    /*
    * Some of this code came from the excellent Unicode
    * conversion examples from:
    *
    *   http://www.unicode.org/Public/PROGRAMS/CVTUTF
    *
    * Full Copyright for that code follows:*/

    /*
    * Copyright 2001-2004 Unicode, Inc.
    * 
    * Disclaimer
    * 
    * This source code is provided as is by Unicode, Inc. No claims are
    * made as to fitness for any particular purpose. No warranties of any
    * kind are expressed or implied. The recipient agrees to determine
    * applicability of information provided. If this file has been
    * purchased on magnetic or optical media from Unicode, Inc., the
    * sole remedy for any claim will be exchange of defective media
    * within 90 days of receipt.
    * 
    * Limitations on Rights to Redistribute This Code
    * 
    * Unicode, Inc. hereby grants the right to freely use the information
    * supplied in this file in the creation of products supporting the
    * Unicode Standard, and to make copies of this file in any form
    * for internal or external distribution as long as this notice
    * remains attached.
    */

    /// <summary> Class to encode java's UTF16 char[] into UTF8 byte[]
    /// without always allocating a new byte[] as
    /// String.getBytes("UTF-8") does.
    /// 
    /// <p/><b>WARNING</b>: This API is a new and experimental and
    /// may suddenly change. <p/>
    /// </summary>

    public static class UnicodeUtil
    {
        public static readonly BytesRef BIG_TERM = new BytesRef(
            new sbyte[] { (sbyte)-1, (sbyte)-1, (sbyte)-1, (sbyte)-1, (sbyte)-1, (sbyte)-1, (sbyte)-1, (sbyte)-1, (sbyte)-1, (sbyte)-1 }
        ); // TODO this is unrelated here find a better place for it

        public const int UNI_SUR_HIGH_START = 0xD800;
        public const int UNI_SUR_HIGH_END = 0xDBFF;
        public const int UNI_SUR_LOW_START = 0xDC00;
        public const int UNI_SUR_LOW_END = 0xDFFF;
        public const int UNI_REPLACEMENT_CHAR = 0xFFFD;

        private const long UNI_MAX_BMP = 0x0000FFFF;

        private const long HALF_SHIFT = 10;
        private const long HALF_MASK = 0x3FFL;

        private const int SURROGATE_OFFSET = 0x010000 - (UNI_SUR_HIGH_START << (int)HALF_SHIFT) - UNI_SUR_LOW_START;

        public static int UTF16toUTF8WithHash(char[] source, int offset, int length, BytesRef result)
        {
            int hash = 0;
            int upto = 0;
            int i = offset;
            int end = offset + length;
            sbyte[] output = result.bytes;
            // Pre-allocate for worst case 4-for-1
            int maxLen = length * 4;
            if (output.Length < maxLen)
                output = result.bytes = new sbyte[ArrayUtil.Oversize(maxLen, 1)];
            result.offset = 0;

            while (i < end)
            {

                int code = (int)source[i++];

                if (code < 0x80)
                {
                    hash = 31 * hash + (output[upto++] = (sbyte)code);
                }
                else if (code < 0x800)
                {
                    hash = 31 * hash + (output[upto++] = (sbyte)(0xC0 | (code >> 6)));
                    hash = 31 * hash + (output[upto++] = (sbyte)(0x80 | (code & 0x3F)));
                }
                else if (code < 0xD800 || code > 0xDFFF)
                {
                    hash = 31 * hash + (output[upto++] = (sbyte)(0xE0 | (code >> 12)));
                    hash = 31 * hash + (output[upto++] = (sbyte)(0x80 | ((code >> 6) & 0x3F)));
                    hash = 31 * hash + (output[upto++] = (sbyte)(0x80 | (code & 0x3F)));
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
                            hash = 31 * hash + (output[upto++] = (sbyte)(0xF0 | (utf32 >> 18)));
                            hash = 31 * hash + (output[upto++] = (sbyte)(0x80 | ((utf32 >> 12) & 0x3F)));
                            hash = 31 * hash + (output[upto++] = (sbyte)(0x80 | ((utf32 >> 6) & 0x3F)));
                            hash = 31 * hash + (output[upto++] = (sbyte)(0x80 | (utf32 & 0x3F)));
                            continue;
                        }
                    }
                    // replace unpaired surrogate or out-of-order low surrogate
                    // with substitution character
                    hash = 31 * hash + (output[upto++] = unchecked((sbyte)0xEF));
                    hash = 31 * hash + (output[upto++] = unchecked((sbyte)0xBF));
                    hash = 31 * hash + (output[upto++] = unchecked((sbyte)0xBD));
                }
            }
            //assert matches(source, offset, length, out, upto);
            result.length = upto;
            return hash;
        }

        /// <summary>Encode characters from a char[] source, starting at
        /// offset and stopping when the character 0xffff is seen.
        /// Returns the number of bytes written to bytesOut. 
        /// </summary>
        public static void UTF16toUTF8(char[] source, int offset, int length, BytesRef result)
        {
            int upto = 0;
            int i = offset;
            int end = offset + length;
            sbyte[] output = result.bytes;
            // Pre-allocate for worst case 4-for-1
            int maxLen = length * 4;
            if (output.Length < maxLen)
                output = result.bytes = new sbyte[maxLen];
            result.offset = 0;

            while (i < end)
            {

                int code = (int)source[i++];

                if (code < 0x80)
                    output[upto++] = (sbyte)code;
                else if (code < 0x800)
                {
                    output[upto++] = (sbyte)(0xC0 | (code >> 6));
                    output[upto++] = (sbyte)(0x80 | (code & 0x3F));
                }
                else if (code < 0xD800 || code > 0xDFFF)
                {
                    output[upto++] = (sbyte)(0xE0 | (code >> 12));
                    output[upto++] = (sbyte)(0x80 | ((code >> 6) & 0x3F));
                    output[upto++] = (sbyte)(0x80 | (code & 0x3F));
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
                            output[upto++] = (sbyte)(0xF0 | (utf32 >> 18));
                            output[upto++] = (sbyte)(0x80 | ((utf32 >> 12) & 0x3F));
                            output[upto++] = (sbyte)(0x80 | ((utf32 >> 6) & 0x3F));
                            output[upto++] = (sbyte)(0x80 | (utf32 & 0x3F));
                            continue;
                        }
                    }
                    // replace unpaired surrogate or out-of-order low surrogate
                    // with substitution character
                    output[upto++] = unchecked((sbyte)0xEF);
                    output[upto++] = unchecked((sbyte)0xBF);
                    output[upto++] = unchecked((sbyte)0xBD);
                }
            }
            //assert matches(source, offset, length, out, upto);
            result.length = upto;
        }

        public static void UTF16toUTF8(string source, int offset, int length, BytesRef result)
        {
            UTF16toUTF8(source.ToCharArray(), offset, length, result);
        }

        public static void UTF16toUTF8(ICharSequence s, int offset, int length, BytesRef result)
        {
            int end = offset + length;

            sbyte[] output = result.bytes;
            result.offset = 0;
            // Pre-allocate for worst case 4-for-1
            int maxLen = length * 4;
            if (output.Length < maxLen)
                output = result.bytes = new sbyte[maxLen];

            int upto = 0;
            for (int i = offset; i < end; i++)
            {
                int code = (int)s.CharAt(i);

                if (code < 0x80)
                    output[upto++] = (sbyte)code;
                else if (code < 0x800)
                {
                    output[upto++] = (sbyte)(0xC0 | (code >> 6));
                    output[upto++] = (sbyte)(0x80 | (code & 0x3F));
                }
                else if (code < 0xD800 || code > 0xDFFF)
                {
                    output[upto++] = (sbyte)(0xE0 | (code >> 12));
                    output[upto++] = (sbyte)(0x80 | ((code >> 6) & 0x3F));
                    output[upto++] = (sbyte)(0x80 | (code & 0x3F));
                }
                else
                {
                    // surrogate pair
                    // confirm valid high surrogate
                    if (code < 0xDC00 && (i < end - 1))
                    {
                        int utf32 = (int)s.CharAt(i + 1);
                        // confirm valid low surrogate and write pair
                        if (utf32 >= 0xDC00 && utf32 <= 0xDFFF)
                        {
                            utf32 = (code << 10) + utf32 + SURROGATE_OFFSET;
                            i++;
                            output[upto++] = (sbyte)(0xF0 | (utf32 >> 18));
                            output[upto++] = (sbyte)(0x80 | ((utf32 >> 12) & 0x3F));
                            output[upto++] = (sbyte)(0x80 | ((utf32 >> 6) & 0x3F));
                            output[upto++] = (sbyte)(0x80 | (utf32 & 0x3F));
                            continue;
                        }
                    }
                    // replace unpaired surrogate or out-of-order low surrogate
                    // with substitution character
                    output[upto++] = unchecked((sbyte)0xEF);
                    output[upto++] = unchecked((sbyte)0xBF);
                    output[upto++] = unchecked((sbyte)0xBD);
                }
            }
            //assert matches(s, offset, length, out, upto);
            result.length = upto;
        }

        public static bool ValidUTF16String(ICharSequence s)
        {
            int size = s.Length;
            for (int i = 0; i < size; i++)
            {
                char ch = s.CharAt(i);
                if (ch >= UNI_SUR_HIGH_START && ch <= UNI_SUR_HIGH_END)
                {
                    if (i < size - 1)
                    {
                        i++;
                        char nextCH = s.CharAt(i);
                        if (nextCH >= UNI_SUR_LOW_START && nextCH <= UNI_SUR_LOW_END)
                        {
                            // Valid surrogate pair
                        }
                        else
                            // Unmatched high surrogate
                            return false;
                    }
                    else
                        // Unmatched high surrogate
                        return false;
                }
                else if (ch >= UNI_SUR_LOW_START && ch <= UNI_SUR_LOW_END)
                    // Unmatched low surrogate
                    return false;
            }

            return true;
        }

        public static bool ValidUTF16String(char[] s)
        {
            int size = s.Length;
            for (int i = 0; i < size; i++)
            {
                char ch = s[i];
                if (ch >= UNI_SUR_HIGH_START && ch <= UNI_SUR_HIGH_END)
                {
                    if (i < size - 1)
                    {
                        i++;
                        char nextCH = s[i];
                        if (nextCH >= UNI_SUR_LOW_START && nextCH <= UNI_SUR_LOW_END)
                        {
                            // Valid surrogate pair
                        }
                        else
                            // Unmatched high surrogate
                            return false;
                    }
                    else
                        // Unmatched high surrogate
                        return false;
                }
                else if (ch >= UNI_SUR_LOW_START && ch <= UNI_SUR_LOW_END)
                    // Unmatched low surrogate
                    return false;
            }

            return true;
        }

        public static bool ValidUTF16String(string s)
        {
            int size = s.Length;
            for (int i = 0; i < size; i++)
            {
                char ch = s[i];
                if (ch >= UNI_SUR_HIGH_START && ch <= UNI_SUR_HIGH_END)
                {
                    if (i < size - 1)
                    {
                        i++;
                        char nextCH = s[i];
                        if (nextCH >= UNI_SUR_LOW_START && nextCH <= UNI_SUR_LOW_END)
                        {
                            // Valid surrogate pair
                        }
                        else
                            // Unmatched high surrogate
                            return false;
                    }
                    else
                        // Unmatched high surrogate
                        return false;
                }
                else if (ch >= UNI_SUR_LOW_START && ch <= UNI_SUR_LOW_END)
                    // Unmatched low surrogate
                    return false;
            }

            return true;
        }

        public static bool ValidUTF16String(char[] s, int size)
        {
            for (int i = 0; i < size; i++)
            {
                char ch = s[i];
                if (ch >= UNI_SUR_HIGH_START && ch <= UNI_SUR_HIGH_END)
                {
                    if (i < size - 1)
                    {
                        i++;
                        char nextCH = s[i];
                        if (nextCH >= UNI_SUR_LOW_START && nextCH <= UNI_SUR_LOW_END)
                        {
                            // Valid surrogate pair
                        }
                        else
                            return false;
                    }
                    else
                        return false;
                }
                else if (ch >= UNI_SUR_LOW_START && ch <= UNI_SUR_LOW_END)
                    // Unmatched low surrogate
                    return false;
            }

            return true;
        }

        internal static readonly int[] utf8CodeLength;

        static UnicodeUtil()
        {
            int v = int.MinValue;
            utf8CodeLength = new int[] {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v,
                v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v,
                v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v,
                v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
                4, 4, 4, 4, 4, 4, 4, 4 //, 5, 5, 5, 5, 6, 6, 0, 0
              };
        }

        public static int CodePointCount(BytesRef utf8)
        {
            int pos = utf8.offset;
            int limit = pos + utf8.length;
            sbyte[] bytes = utf8.bytes;

            int codePointCount = 0;
            for (; pos < limit; codePointCount++)
            {
                int v = bytes[pos] & 0xFF;
                if (v <   /* 0xxx xxxx */ 0x80) { pos += 1; continue; }
                if (v >=  /* 110x xxxx */ 0xc0)
                {
                    if (v < /* 111x xxxx */ 0xe0) { pos += 2; continue; }
                    if (v < /* 1111 xxxx */ 0xf0) { pos += 3; continue; }
                    if (v < /* 1111 1xxx */ 0xf8) { pos += 4; continue; }
                    // fallthrough, consider 5 and 6 byte sequences invalid. 
                }

                // Anything not covered above is invalid UTF8.
                throw new ArgumentException();
            }

            // Check if we didn't go over the limit on the last character.
            if (pos > limit) throw new ArgumentException();

            return codePointCount;
        }

        public static void UTF8toUTF32(BytesRef utf8, IntsRef utf32)
        {
            // TODO: broken if incoming result.offset != 0
            // pre-alloc for worst case
            // TODO: ints cannot be null, should be an assert
            if (utf32.ints == null || utf32.ints.Length < utf8.length)
            {
                utf32.ints = new int[utf8.length];
            }
            int utf32Count = 0;
            int utf8Upto = utf8.offset;
            int[] ints = utf32.ints;
            sbyte[] bytes = utf8.bytes;
            int utf8Limit = utf8.offset + utf8.length;
            while (utf8Upto < utf8Limit)
            {
                int numBytes = utf8CodeLength[bytes[utf8Upto] & 0xFF];
                int v = 0;
                switch (numBytes)
                {
                    case 1:
                        ints[utf32Count++] = bytes[utf8Upto++];
                        continue;
                    case 2:
                        // 5 useful bits
                        v = bytes[utf8Upto++] & 31;
                        break;
                    case 3:
                        // 4 useful bits
                        v = bytes[utf8Upto++] & 15;
                        break;
                    case 4:
                        // 3 useful bits
                        v = bytes[utf8Upto++] & 7;
                        break;
                    default:
                        throw new ArgumentException("invalid utf8");
                }

                // TODO: this may read past utf8's limit.
                int limit = utf8Upto + numBytes - 1;
                while (utf8Upto < limit)
                {
                    v = v << 6 | bytes[utf8Upto++] & 63;
                }
                ints[utf32Count++] = v;
            }

            utf32.offset = 0;
            utf32.length = utf32Count;
        }

        /** Shift value for lead surrogate to form a supplementary character. */
        private const int LEAD_SURROGATE_SHIFT_ = 10;
        /** Mask to retrieve the significant value from a trail surrogate.*/
        private const int TRAIL_SURROGATE_MASK_ = 0x3FF;
        /** Trail surrogate minimum value */
        private const int TRAIL_SURROGATE_MIN_VALUE = 0xDC00;
        /** Lead surrogate minimum value */
        private const int LEAD_SURROGATE_MIN_VALUE = 0xD800;
        /** The minimum value for Supplementary code points */
        private const int SUPPLEMENTARY_MIN_VALUE = 0x10000;
        /** Value that all lead surrogate starts with */
        private const int LEAD_SURROGATE_OFFSET_ = LEAD_SURROGATE_MIN_VALUE
                - (SUPPLEMENTARY_MIN_VALUE >> LEAD_SURROGATE_SHIFT_);

        public static String NewString(int[] codePoints, int offset, int count)
        {
            if (count < 0)
            {
                throw new ArgumentException();
            }
            char[] chars = new char[count];
            int w = 0;
            for (int r = offset, e = offset + count; r < e; ++r)
            {
                int cp = codePoints[r];
                if (cp < 0 || cp > 0x10ffff)
                {
                    throw new ArgumentException();
                }
                while (true)
                {
                    try
                    {
                        if (cp < 0x010000)
                        {
                            chars[w] = (char)cp;
                            w++;
                        }
                        else
                        {
                            chars[w] = (char)(LEAD_SURROGATE_OFFSET_ + (cp >> LEAD_SURROGATE_SHIFT_));
                            chars[w + 1] = (char)(TRAIL_SURROGATE_MIN_VALUE + (cp & TRAIL_SURROGATE_MASK_));
                            w += 2;
                        }
                        break;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        int newlen = (int)(Math.Ceiling((double)codePoints.Length * (w + 2)
                                / (r - offset + 1)));
                        char[] temp = new char[newlen];
                        Array.Copy(chars, 0, temp, 0, w);
                        chars = temp;
                    }
                }
            }

            return new String(chars, 0, w);
        }

        public static String ToHexString(String s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (i > 0)
                {
                    sb.Append(' ');
                }
                if (ch < 128)
                {
                    sb.Append(ch);
                }
                else
                {
                    if (ch >= UNI_SUR_HIGH_START && ch <= UNI_SUR_HIGH_END)
                    {
                        sb.Append("H:");
                    }
                    else if (ch >= UNI_SUR_LOW_START && ch <= UNI_SUR_LOW_END)
                    {
                        sb.Append("L:");
                    }
                    else if (ch > UNI_SUR_LOW_END)
                    {
                        if (ch == 0xffff)
                        {
                            sb.Append("F:");
                        }
                        else
                        {
                            sb.Append("E:");
                        }
                    }

                    sb.Append("0x" + ((short)ch).ToString("X"));
                }
            }
            return sb.ToString();
        }

        public static void UTF8toUTF16(sbyte[] utf8, int offset, int length, CharsRef chars)
        {
            int out_offset = chars.offset = 0;
            char[] output = chars.chars = ArrayUtil.Grow(chars.chars, length);
            int limit = offset + length;
            while (offset < limit)
            {
                int b = utf8[offset++] & 0xff;
                if (b < 0xc0)
                {
                    //assert b < 0x80;
                    output[out_offset++] = (char)b;
                }
                else if (b < 0xe0)
                {
                    output[out_offset++] = (char)(((b & 0x1f) << 6) + (utf8[offset++] & 0x3f));
                }
                else if (b < 0xf0)
                {
                    output[out_offset++] = (char)(((b & 0xf) << 12) + ((utf8[offset] & 0x3f) << 6) + (utf8[offset + 1] & 0x3f));
                    offset += 2;
                }
                else
                {
                    //assert b < 0xf8: "b = 0x" + Integer.toHexString(b);
                    int ch = ((b & 0x7) << 18) + ((utf8[offset] & 0x3f) << 12) + ((utf8[offset + 1] & 0x3f) << 6) + (utf8[offset + 2] & 0x3f);
                    offset += 3;
                    if (ch < UNI_MAX_BMP)
                    {
                        output[out_offset++] = (char)ch;
                    }
                    else
                    {
                        int chHalf = ch - 0x0010000;
                        output[out_offset++] = (char)((chHalf >> 10) + 0xD800);
                        output[out_offset++] = (char)((chHalf & HALF_MASK) + 0xDC00);
                    }
                }
            }
            chars.length = out_offset - chars.offset;
        }

        public static void UTF8toUTF16(BytesRef bytesRef, CharsRef chars)
        {
            UTF8toUTF16(bytesRef.bytes, bytesRef.offset, bytesRef.length, chars);
        }
    }
}