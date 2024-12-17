using Lucene.Net.Support;
using System;

#nullable enable

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

    /*
     * Some of this code came from the excellent Unicode
     * conversion examples from:
     *
     *   http://www.unicode.org/Public/PROGRAMS/CVTUTF
     *
     * Full Copyright for that code follows:
    */

    /*
     * Copyright 2001-2004 Unicode, Inc.
     *
     * Disclaimer
     *
     * this source code is provided as is by Unicode, Inc. No claims are
     * made as to fitness for any particular purpose. No warranties of any
     * kind are expressed or implied. The recipient agrees to determine
     * applicability of information provided. If this file has been
     * purchased on magnetic or optical media from Unicode, Inc., the
     * sole remedy for any claim will be exchange of defective media
     * within 90 days of receipt.
     *
     * Limitations on Rights to Redistribute this Code
     *
     * Unicode, Inc. hereby grants the right to freely use the information
     * supplied in this file in the creation of products supporting the
     * Unicode Standard, and to make copies of this file in any form
     * for internal or external distribution as long as this notice
     * remains attached.
     */

    /*
     * Additional code came from the IBM ICU library.
     *
     *  http://www.icu-project.org
     *
     * Full Copyright for that code follows.
     */

    /*
     * Copyright (C) 1999-2010, International Business Machines
     * Corporation and others.  All Rights Reserved.
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to deal
     * in the Software without restriction, including without limitation the rights
     * to use, copy, modify, merge, publish, distribute, and/or sell copies of the
     * Software, and to permit persons to whom the Software is furnished to do so,
     * provided that the above copyright notice(s) and this permission notice appear
     * in all copies of the Software and that both the above copyright notice(s) and
     * this permission notice appear in supporting documentation.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF THIRD PARTY RIGHTS.
     * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN this NOTICE BE
     * LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES, OR
     * ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER
     * IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT
     * OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF this SOFTWARE.
     *
     * Except as contained in this notice, the name of a copyright holder shall not
     * be used in advertising or otherwise to promote the sale, use or other
     * dealings in this Software without prior written authorization of the
     * copyright holder.
     */

    public static partial class UnicodeUtil
    {
        /// <summary>
        /// Generates char array that represents the provided input code points.
        /// <para/>
        /// LUCENENET specific.
        /// </summary>
        /// <param name="codePoints"> The code array. </param>
        /// <param name="offset"> The start of the text in the code point array. </param>
        /// <param name="count"> The number of code points. </param>
        /// <returns> a char array representing the code points between offset and count. </returns>
        // LUCENENET NOTE: This code was originally in the NewString() method.
        // It has been refactored from the original to remove the exception throw/catch and
        // instead proactively resizes the array instead of relying on exceptions + copy operations
        [Obsolete("Use NewString method instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static char[] ToCharArray(int[] codePoints, int offset, int count)
        {
            return ToCharArray(codePoints.AsSpan(offset), count);
        }

        /// <summary>
        /// Generates char array that represents the provided input code points.
        /// <para/>
        /// LUCENENET specific.
        /// </summary>
        /// <param name="codePoints"> The code span. </param>
        /// <param name="count"> The number of code points. </param>
        /// <returns> a char array representing the code points between offset and count. </returns>
        // LUCENENET NOTE: This code was originally in the NewString() method.
        // It has been refactored from the original to remove the exception throw/catch and
        // instead proactively resizes the array instead of relying on exceptions + copy operations
        [Obsolete("Use NewString method instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static char[] ToCharArray(ReadOnlySpan<int> codePoints, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count must be >= 0"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            const int countThreshold = 1024; // If the number of chars exceeds this, we count them instead of allocating count * 2
            // LUCENENET: as a first approximation, assume each codepoint
            // is 2 characters (since it cannot be longer than this)
            int arrayLength = count * 2;
            // LUCENENET: if we go over the threshold, count the number of
            // chars we will need so we can allocate the precise amount of memory
            if (count > countThreshold)
            {
                arrayLength = 0;
                for (int r = 0; r < count; ++r)
                {
                    arrayLength += codePoints[r] < 0x010000 ? 1 : 2;
                }
                if (arrayLength < 1)
                {
                    arrayLength = count * 2;
                }
            }
            // Initialize our array to our exact or oversized length.
            // It is now safe to assume we have enough space for all of the characters.
            char[] chars = new char[arrayLength];
            int w = 0;
            for (int r = 0; r < count; ++r)
            {
                int cp = codePoints[r];
                if (cp < 0 || cp > 0x10ffff)
                {
                    throw new ArgumentException($"Invalid code point: {cp}", nameof(codePoints));
                }
                if (cp < 0x010000)
                {
                    chars[w++] = (char)cp;
                }
                else
                {
                    chars[w++] = (char)(LEAD_SURROGATE_OFFSET_ + (cp >> LEAD_SURROGATE_SHIFT_));
                    chars[w++] = (char)(TRAIL_SURROGATE_MIN_VALUE + (cp & TRAIL_SURROGATE_MASK_));
                }
            }

            var result = new char[w];
            Arrays.Copy(chars, result, w);
            return result;
        }
    }
}
