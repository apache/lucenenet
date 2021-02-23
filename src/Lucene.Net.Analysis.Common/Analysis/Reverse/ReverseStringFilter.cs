// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Analysis.Reverse
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

    /// <summary>
    /// Reverse token string, for example "country" => "yrtnuoc".
    /// <para>
    /// If <see cref="marker"/> is supplied, then tokens will be also prepended by
    /// that character. For example, with a marker of &#x5C;u0001, "country" =>
    /// "&#x5C;u0001yrtnuoc". This is useful when implementing efficient leading
    /// wildcards search.
    /// </para>
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="ReverseStringFilter"/>, or when using any of
    /// its static methods:
    /// <list type="bullet">
    ///     <item><description> As of 3.1, supplementary characters are handled correctly</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class ReverseStringFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;
        private readonly char marker;
        private readonly LuceneVersion matchVersion;
        private const char NOMARKER = '\uFFFF';

        /// <summary>
        /// Example marker character: U+0001 (START OF HEADING) 
        /// </summary>
        public const char START_OF_HEADING_MARKER = '\u0001';

        /// <summary>
        /// Example marker character: U+001F (INFORMATION SEPARATOR ONE)
        /// </summary>
        public const char INFORMATION_SEPARATOR_MARKER = '\u001F';

        /// <summary>
        /// Example marker character: U+EC00 (PRIVATE USE AREA: EC00) 
        /// </summary>
        public const char PUA_EC00_MARKER = '\uEC00';

        /// <summary>
        /// Example marker character: U+200F (RIGHT-TO-LEFT MARK)
        /// </summary>
        public const char RTL_DIRECTION_MARKER = '\u200F';

        /// <summary>
        /// Create a new <see cref="ReverseStringFilter"/> that reverses all tokens in the 
        /// supplied <see cref="TokenStream"/>.
        /// <para>
        /// The reversed tokens will not be marked. 
        /// </para>
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="in"> <see cref="TokenStream"/> to filter </param>
        public ReverseStringFilter(LuceneVersion matchVersion, TokenStream @in) 
            : this(matchVersion, @in, NOMARKER)
        {
        }

        /// <summary>
        /// Create a new <see cref="ReverseStringFilter"/> that reverses and marks all tokens in the
        /// supplied <see cref="TokenStream"/>.
        /// <para>
        /// The reversed tokens will be prepended (marked) by the <paramref name="marker"/>
        /// character.
        /// </para>
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="in"> <see cref="TokenStream"/> to filter </param>
        /// <param name="marker"> A character used to mark reversed tokens </param>
        public ReverseStringFilter(LuceneVersion matchVersion, TokenStream @in, char marker) 
            : base(@in)
        {
            this.matchVersion = matchVersion;
            this.marker = marker;
            this.termAtt = GetAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                int len = termAtt.Length;
                if (marker != NOMARKER)
                {
                    len++;
                    termAtt.ResizeBuffer(len);
                    termAtt.Buffer[len - 1] = marker;
                }
                Reverse(matchVersion, termAtt.Buffer, 0, len);
                termAtt.Length = len;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Reverses the given input string
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="input"> the string to reverse </param>
        /// <returns> the given input string in reversed order </returns>
        public static string Reverse(LuceneVersion matchVersion, string input)
        {
            char[] charInput = input.ToCharArray();
            Reverse(matchVersion, charInput, 0, charInput.Length);
            return new string(charInput);
        }

        /// <summary>
        /// Reverses the given input buffer in-place </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="buffer"> the input char array to reverse </param>
        public static void Reverse(LuceneVersion matchVersion, char[] buffer)
        {
            Reverse(matchVersion, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Partially reverses the given input buffer in-place from offset 0
        /// up to the given length. </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="buffer"> the input char array to reverse </param>
        /// <param name="len"> the length in the buffer up to where the
        ///        buffer should be reversed </param>
        public static void Reverse(LuceneVersion matchVersion, char[] buffer, int len)
        {
            Reverse(matchVersion, buffer, 0, len);
        }

        /// @deprecated (3.1) Remove this when support for 3.0 indexes is no longer needed. 
        [Obsolete("(3.1) Remove this when support for 3.0 indexes is no longer needed.")]
        private static void ReverseUnicode3(char[] buffer, int start, int len)
        {
            if (len <= 1)
            {
                return;
            }
            int num = len >> 1;
            for (int i = start; i < (start + num); i++)
            {
                char c = buffer[i];
                buffer[i] = buffer[start * 2 + len - i - 1];
                buffer[start * 2 + len - i - 1] = c;
            }
        }

        /// <summary>
        /// Partially reverses the given input buffer in-place from the given offset
        /// up to the given length. </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="buffer"> the input char array to reverse </param>
        /// <param name="start"> the offset from where to reverse the buffer </param>
        /// <param name="len"> the length in the buffer up to where the
        ///        buffer should be reversed </param>
        public static void Reverse(LuceneVersion matchVersion, char[] buffer, int start, int len)
        {
#pragma warning disable 612, 618
            if (!matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))

            {
                ReverseUnicode3(buffer, start, len);
#pragma warning restore 612, 618
                return;
            }
            /* modified version of Apache Harmony AbstractStringBuilder reverse0() */
            if (len < 2)
            {
                return;
            }
            int end = (start + len) - 1;
            char frontHigh = buffer[start];
            char endLow = buffer[end];
            bool allowFrontSur = true, allowEndSur = true;
            int mid = start + (len >> 1);
            for (int i = start; i < mid; ++i, --end)
            {
                char frontLow = buffer[i + 1];
                char endHigh = buffer[end - 1];
                bool surAtFront = allowFrontSur && char.IsSurrogatePair(frontHigh, frontLow);
                if (surAtFront && (len < 3))
                {
                    // nothing to do since surAtFront is allowed and 1 char left
                    return;
                }
                bool surAtEnd = allowEndSur && char.IsSurrogatePair(endHigh, endLow);
                allowFrontSur = allowEndSur = true;
                if (surAtFront == surAtEnd)
                {
                    if (surAtFront)
                    {
                        // both surrogates
                        buffer[end] = frontLow;
                        buffer[--end] = frontHigh;
                        buffer[i] = endHigh;
                        buffer[++i] = endLow;
                        frontHigh = buffer[i + 1];
                        endLow = buffer[end - 1];
                    }
                    else
                    {
                        // neither surrogates
                        buffer[end] = frontHigh;
                        buffer[i] = endLow;
                        frontHigh = frontLow;
                        endLow = endHigh;
                    }
                }
                else
                {
                    if (surAtFront)
                    {
                        // surrogate only at the front
                        buffer[end] = frontLow;
                        buffer[i] = endLow;
                        endLow = endHigh;
                        allowFrontSur = false;
                    }
                    else
                    {
                        // surrogate only at the end
                        buffer[end] = frontHigh;
                        buffer[i] = endHigh;
                        frontHigh = frontLow;
                        allowEndSur = false;
                    }
                }
            }
            if ((len & 0x01) == 1 && !(allowFrontSur && allowEndSur))
            {
                // only if odd length
                buffer[end] = allowFrontSur ? endLow : frontHigh;
            }
        }
    }
}