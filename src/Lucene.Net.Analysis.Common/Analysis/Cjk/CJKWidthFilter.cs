// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Cjk
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
    /// A <see cref="TokenFilter"/> that normalizes CJK width differences:
    /// <list type="bullet">
    ///   <item><description>Folds fullwidth ASCII variants into the equivalent basic latin</description></item>
    ///   <item><description>Folds halfwidth Katakana variants into the equivalent kana</description></item>
    /// </list>
    /// <para>
    /// NOTE: this filter can be viewed as a (practical) subset of NFKC/NFKD
    /// Unicode normalization. See the normalization support in the ICU package
    /// for full normalization.
    /// </para>
    /// </summary>
    public sealed class CJKWidthFilter : TokenFilter
    {
        private ICharTermAttribute termAtt;

        /// <summary>
        /// halfwidth kana mappings: 0xFF65-0xFF9D 
        /// <para/>
        /// note: 0xFF9C and 0xFF9D are only mapped to 0x3099 and 0x309A
        /// as a fallback when they cannot properly combine with a preceding 
        /// character into a composed form.
        /// </summary>
        private static readonly char[] KANA_NORM = new char[] {
            (char)0x30fb, (char)0x30f2, (char)0x30a1, (char)0x30a3, (char)0x30a5, (char)0x30a7, (char)0x30a9, (char)0x30e3, (char)0x30e5,
            (char)0x30e7, (char)0x30c3, (char)0x30fc, (char)0x30a2, (char)0x30a4, (char)0x30a6, (char)0x30a8, (char)0x30aa, (char)0x30ab,
            (char)0x30ad, (char)0x30af, (char)0x30b1, (char)0x30b3, (char)0x30b5, (char)0x30b7, (char)0x30b9, (char)0x30bb, (char)0x30bd,
            (char)0x30bf, (char)0x30c1, (char)0x30c4, (char)0x30c6, (char)0x30c8, (char)0x30ca, (char)0x30cb, (char)0x30cc, (char)0x30cd,
            (char)0x30ce, (char)0x30cf, (char)0x30d2, (char)0x30d5, (char)0x30d8, (char)0x30db, (char)0x30de, (char)0x30df, (char)0x30e0,
            (char)0x30e1, (char)0x30e2, (char)0x30e4, (char)0x30e6, (char)0x30e8, (char)0x30e9, (char)0x30ea, (char)0x30eb, (char)0x30ec,
            (char)0x30ed, (char)0x30ef, (char)0x30f3, (char)0x3099, (char)0x309A
        };

        public CJKWidthFilter(TokenStream input)
              : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                char[] text = termAtt.Buffer;
                int length = termAtt.Length;
                for (int i = 0; i < length; i++)
                {
                    char ch = text[i];
                    if (ch >= 0xFF01 && ch <= 0xFF5E)
                    {
                        // Fullwidth ASCII variants
                        text[i] = (char)(text[i] - 0xFEE0);
                    }
                    else if (ch >= 0xFF65 && ch <= 0xFF9F)
                    {
                        // Halfwidth Katakana variants
                        if ((ch == 0xFF9E || ch == 0xFF9F) && i > 0 && Combine(text, i, ch))
                        {
                            length = StemmerUtil.Delete(text, i--, length);
                        }
                        else
                        {
                            text[i] = KANA_NORM[ch - 0xFF65];
                        }
                    }
                }
                termAtt.Length = length;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>kana combining diffs: 0x30A6-0x30FD </summary>
        private static readonly sbyte[] KANA_COMBINE_VOICED = new sbyte[] {
            78, 0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1,
            0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1,
            0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 8, 8, 8, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1
        };

        private static readonly sbyte[] KANA_COMBINE_HALF_VOICED = new sbyte[] {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 2, 0, 0, 2,
            0, 0, 2, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        /// <summary>
        /// returns true if we successfully combined the voice mark </summary>
        private static bool Combine(char[] text, int pos, char ch)
        {
            char prev = text[pos - 1];
            if (prev >= 0x30A6 && prev <= 0x30FD)
            {
                text[pos - 1] += (char)((ch == 0xFF9F) ? KANA_COMBINE_HALF_VOICED[prev - 0x30A6] : KANA_COMBINE_VOICED[prev - 0x30A6]);
                return text[pos - 1] != prev;
            }
            return false;
        }
    }
}