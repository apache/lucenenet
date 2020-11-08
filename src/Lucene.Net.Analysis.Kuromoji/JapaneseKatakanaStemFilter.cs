using Lucene.Net.Analysis.TokenAttributes;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Ja
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
    /// A <see cref="TokenFilter"/> that normalizes common katakana spelling variations
    /// ending in a long sound character by removing this character (U+30FC).  Only
    /// katakana words longer than a minimum length are stemmed (default is four).
    /// </summary>
    /// <remarks>
    /// Note that only full-width katakana characters are supported.  Please use a
    /// <see cref="Cjk.CJKWidthFilter"/> to convert half-width
    /// katakana to full-width before using this filter.
    /// <para/>
    /// In order to prevent terms from being stemmed, use an instance of
    /// <see cref="Miscellaneous.SetKeywordMarkerFilter"/>
    /// or a custom <see cref="TokenFilter"/> that sets the <see cref="IKeywordAttribute"/>
    /// before this <see cref="TokenStream"/>.
    /// </remarks>
    public sealed class JapaneseKatakanaStemFilter : TokenFilter
    {
        public const int DEFAULT_MINIMUM_LENGTH = 4;
        private const char HIRAGANA_KATAKANA_PROLONGED_SOUND_MARK = '\u30fc';

        private readonly ICharTermAttribute termAttr;
        private readonly IKeywordAttribute keywordAttr;
        private readonly int minimumKatakanaLength;

        private readonly static Regex katakanaPattern = new Regex(@"\p{IsKatakana}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public JapaneseKatakanaStemFilter(TokenStream input, int minimumLength)
            : base(input)
        {
            this.minimumKatakanaLength = minimumLength;
            this.termAttr = AddAttribute<ICharTermAttribute>();
            this.keywordAttr = AddAttribute<IKeywordAttribute>();
        }

        public JapaneseKatakanaStemFilter(TokenStream input)
            : this(input, DEFAULT_MINIMUM_LENGTH)
        {
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (!keywordAttr.IsKeyword)
                {
                    termAttr.SetLength(Stem(termAttr.Buffer, termAttr.Length));
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private int Stem(char[] term, int length)
        {
            if (length < minimumKatakanaLength)
            {
                return length;
            }

            if (!IsKatakana(term, length))
            {
                return length;
            }

            if (term[length - 1] == HIRAGANA_KATAKANA_PROLONGED_SOUND_MARK)
            {
                return length - 1;
            }

            return length;
        }

        private static bool IsKatakana(char[] term, int length) // LUCENENET: CA1822: Mark members as static
        {
            for (int i = 0; i < length; i++)
            {
                // NOTE: Test only identifies full-width characters -- half-widths are supported
                if (!katakanaPattern.IsMatch(term[i].ToString()))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
