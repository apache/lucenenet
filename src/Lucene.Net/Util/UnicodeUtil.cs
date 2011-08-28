// -----------------------------------------------------------------------
// <copyright company="Apache" file="UnicodeUtil.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
//      Some of this code came from the excellent Unicode
//      conversion examples from:   http://www.unicode.org/Public/PROGRAMS/CVTUTF
//
//      Full Copyright for that code follows:
//      Copyright 2001-2004 Unicode, Inc.
//      
//      Disclaimer
//      
//      This source code is provided as is by Unicode, Inc. No claims are
//      made as to fitness for any particular purpose. No warranties of any
//      kind are expressed or implied. The recipient agrees to determine
//      applicability of information provided. If this file has been
//      purchased on magnetic or optical media from Unicode, Inc., the
//      sole remedy for any claim will be exchange of defective media
//      within 90 days of receipt.
//      
//      Limitations on Rights to Redistribute This Code
//      
//      Unicode, Inc. hereby grants the right to freely use the information
//      supplied in this file in the creation of products supporting the
//      Unicode Standard, and to make copies of this file in any form
//      for internal or external distribution as long as this notice
//      remains attached.
//
//      Additional code came from the IBM ICU library.
//     
//       http://www.icu-project.org
//     
//      Full Copyright for that code follows.
//
//
//      Copyright (C) 1999-2010, International Business Machines
//      Corporation and others.  All Rights Reserved.
//     
//      Permission is hereby granted, free of charge, to any person obtaining a copy
//      of this software and associated documentation files (the "Software"), to deal
//      in the Software without restriction, including without limitation the rights
//      to use, copy, modify, merge, publish, distribute, and/or sell copies of the
//      Software, and to permit persons to whom the Software is furnished to do so,
//      provided that the above copyright notice(s) and this permission notice appear
//      in all copies of the Software and that both the above copyright notice(s) and
//      this permission notice appear in supporting documentation.
//     
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//      IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//      FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF THIRD PARTY RIGHTS.
//      IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS NOTICE BE
//      LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES, OR
//      ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER
//      IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT
//      OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
//     
//      Except as contained in this notice, the name of a copyright holder shall not
//      be used in advertising or otherwise to promote the sale, use or other
//      dealings in this Software without prior written authorization of the
//      copyright holder.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Util
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Lucene.Net.Support;

    /// <summary>
    /// Class to encode .NET's UTF16 <c>char[]</c> into UTF8 <c>byte[]</c> without always 
    /// allocating a new byte[] as <see cref="Encoding.GetBytes(char[])"/> does.
    /// </summary>
    public class UnicodeUtil
    {
        /// <summary>
        /// Unicode Surrogate High Start (0xD800)
        /// </summary>
        public static readonly int UnicodeSurrogateHighStart = 0xD800;

        /// <summary>
        /// Unicode Surrogate High End (0xDBFF)
        /// </summary>
        public static readonly int UnicodeSurrogateHighEnd = 0xDBFF;

        /// <summary>
        /// Unicode Surrogate Low Start (0xDC00)
        /// </summary>
        public static readonly int UnicodeSurrogateLowStart = 0xDC00;


        /// <summary>
        /// Unicode Surrogate Low End (0xDFF)
        /// </summary>
        public static readonly int UnicodeSurrogateLowEnd = 0xDFFF;


        /// <summary>
        /// Unicode Replacement Character (0xFFFD)
        /// </summary>
        public static readonly int UnicodeReplacementCharacter = 0xFFFD;


        /// <summary>
        /// Character Minimum Supplementary Code Point (0x10000) equivalent to java's
        /// <c>Character.MIN_SUPPLEMENTARY_CODE_POINT </c>
        /// </summary>
        public static readonly int CharacterMinimumSupplementaryCodePoint = 0x10000;

        //// private static readonly long unicodeMaxBmp = 0x0000FFFF;

        private static readonly long halfShift = 10;
        //// private static readonly long halfMask = 0x3FFL;

        /*
        /// <summary>
        /// Shift value for lead surrogate to form a supplementary character.
        /// </summary>
        private static readonly int leadSurrogateShift = 10;

        /// <summary>
        /// Mask to retrieve the significant value from a trail surrogate.
        /// </summary>
        private static readonly int trailSurrogateMask = 0x3FF;

        /// <summary>
        /// Trail surrogate minimum value (0xDC00)
        /// </summary>
        private static readonly int trailSurrogateMinValue = 0xDC00;
        
        /// <summary>
        ///  Lead surrogate minimum value (0xD800)
        /// </summary>
        private static readonly int leadSurrogateMinValue = 0xD800;
        
        /// <summary>
        /// The minimum value for Supplementary code points
        /// </summary>
        private static readonly int supplementaryMinValue = 0x10000;
        
        /// <summary>
        ///  Value that all lead surrogate starts with
        /// </summary>
        private static readonly int LeadSurrogateOffset = leadSurrogateMinValue
                - (supplementaryMinValue >> leadSurrogateShift);
         */

        private static readonly int surrogateOffset = CharacterMinimumSupplementaryCodePoint -
                                                      (UnicodeSurrogateHighStart << (int)halfShift) -
                                                      UnicodeSurrogateLowStart;


        /// <summary>
        /// UTs the f16to UT f8.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="destination">The destination.</param>
        internal static void UTF16toUTF8(char[] source, int offset, int length, BytesRef destination)
        {
            UTF16toUTF8(source.ToCharSequence(), offset, length, destination);
        }

        /// <summary>
        /// UTs the f16to U t8.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="destination">The destination.</param>
        internal static void UTF16toUTF8(string source, int offset, int length, BytesRef destination)
        {
            UTF16toUTF8(source.ToCharSequence(), offset, length, destination);
        }

        // TODO: change source to IEnumerable<char> once Portable Class Libraries support IEnumerable<char> on string.
        private static void UTF16toUTF8(ICharSequence sequence, int offset, int length, BytesRef destination)
        {
            int position = 0;
            int i = offset, end = offset + length, maxLength = length * 4;
            byte[] bytes = destination.Bytes;

            if (bytes.Length < maxLength)
                bytes = destination.Bytes = new byte[maxLength];

            destination.Offset = 0;

            while (i < end)
            {
                int currentByte = sequence.CharAt(i++);

                //// 0x80 = 128
                //// 0x800 = 2048
                //// 0xD800 & 0xDFFF are code point ranges in UTF16 U+D800..U+DFFF
                //// last else takes care of UTF-16 surrogate pairs

                if (currentByte < 0x80) 
                {
                    bytes[position++] = (byte)currentByte;
                }
                else if (currentByte < 0x800) 
                {
                    bytes[position++] = (byte)(0xC0 | (currentByte >> 6));
                    bytes[position++] = (byte)(0x80 | (currentByte & 0x3F));
                }
                else if (currentByte < 0xD800 && currentByte > 0xDFFF)  
                {
                    bytes[position++] = (byte)(0xE0 | (currentByte >> 12));
                    bytes[position++] = (byte)(0x80 | ((currentByte >> 6) & 0x3F));
                    bytes[position++] = (byte)(0x80 | (currentByte & 0x3F));
                }
                else
                {
                    // UTF-16 surrogate pairs
                    // confirm valid high surrogate
                    if (currentByte < 0xDC00 && i < end)
                    {
                        int utf32 = sequence.CharAt(i);

                        // confirm valid low surrogate and write pair
                        if (utf32 >= 0xDC00 && utf32 <= 0xDFFF)
                        {
                            utf32 = (currentByte << 10) + utf32 + surrogateOffset;
                            i++;
                            bytes[position++] = (byte)(0xF0 | (utf32 >> 18));
                            bytes[position++] = (byte)(0x80 | ((utf32 >> 12) & 0x3F));
                            bytes[position++] = (byte)(0x80 | ((utf32 >> 6) & 0x3F));
                            bytes[position++] = (byte)(0x80 | (utf32 & 0x3F));
                            continue;
                        }
                    }
                    //// replace unpaired surrogate or out-of-order low surrogate
                    //// with substitution character
                    bytes[position++] = 0xEF;
                    bytes[position++] = 0xBF;
                    bytes[position++] = 0xBD;
                }
            }

            destination.Length = position;
        }
    }
}