// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Support;
using System;
using System.Globalization;

namespace Lucene.Net.Analysis.Tr
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
    /// Normalizes Turkish token text to lower case.
    /// <para>
    /// Turkish and Azeri have unique casing behavior for some characters. This
    /// filter applies Turkish lowercase rules. For more information, see <a
    /// href="http://en.wikipedia.org/wiki/Turkish_dotted_and_dotless_I"
    /// >http://en.wikipedia.org/wiki/Turkish_dotted_and_dotless_I</a>
    /// </para>
    /// </summary>
    public sealed class TurkishLowerCaseFilter : TokenFilter
    {
        private const int LATIN_CAPITAL_LETTER_I = '\u0049';
        private const int LATIN_SMALL_LETTER_I = '\u0069';
        private const int LATIN_SMALL_LETTER_DOTLESS_I = '\u0131';
        private const int COMBINING_DOT_ABOVE = '\u0307';
        private readonly ICharTermAttribute termAtt;

        private static readonly CultureInfo culture = new CultureInfo("tr"); // LUCENENET specific - we need to do a culture-sensitive lowercase operation in Turkish

        /// <summary>
        /// Create a new <see cref="TurkishLowerCaseFilter"/>, that normalizes Turkish token text 
        /// to lower case.
        /// </summary>
        /// <param name="in"> <see cref="TokenStream"/> to filter </param>
        public TurkishLowerCaseFilter(TokenStream @in)
            : base(@in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override sealed bool IncrementToken()
        {
            bool iOrAfter = false;

            if (m_input.IncrementToken())
            {
                char[] buffer = termAtt.Buffer;
                int length = termAtt.Length;
                for (int i = 0; i < length;)
                {
                    int ch = Character.CodePointAt(buffer, i, length);

                    iOrAfter = (ch == LATIN_CAPITAL_LETTER_I || (iOrAfter && Character.GetType(ch) == UnicodeCategory.NonSpacingMark));

                    if (iOrAfter) // all the special I turkish handling happens here.
                    {
                        switch (ch)
                        {
                            // remove COMBINING_DOT_ABOVE to mimic composed lowercase
                            case COMBINING_DOT_ABOVE:
                                length = Delete(buffer, i, length);
                                continue;
                            // i itself, it depends if it is followed by COMBINING_DOT_ABOVE
                            // if it is, we will make it small i and later remove the dot
                            case LATIN_CAPITAL_LETTER_I:
                                if (IsBeforeDot(buffer, i + 1, length))
                                {
                                    buffer[i] = (char)LATIN_SMALL_LETTER_I;
                                }
                                else
                                {
                                    buffer[i] = (char)LATIN_SMALL_LETTER_DOTLESS_I;
                                    // below is an optimization. no COMBINING_DOT_ABOVE follows,
                                    // so don't waste time calculating Character.getType(), etc
                                    iOrAfter = false;
                                }
                                i++;
                                continue;
                        }
                    }

                    // LUCENENET specific - need to pass Turkish culture to get the correct lowercase results
                    i += Character.ToChars(Character.ToLower(ch, culture), buffer, i);
                }

                termAtt.Length = length;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// lookahead for a combining dot above.
        /// other NSMs may be in between.
        /// </summary>
        private static bool IsBeforeDot(char[] s, int pos, int len) // LUCENENET: CA1822: Mark members as static
        {
            for (int i = pos; i < len;)
            {
                int ch = Character.CodePointAt(s, i, len);
                if (Character.GetType(ch) != UnicodeCategory.NonSpacingMark)
                {
                    return false;
                }
                if (ch == COMBINING_DOT_ABOVE)
                {
                    return true;
                }
                i += Character.CharCount(ch);
            }

            return false;
        }

        /// <summary>
        /// delete a character in-place.
        /// rarely happens, only if <see cref="COMBINING_DOT_ABOVE"/> is found after an i
        /// </summary>
        private static int Delete(char[] s, int pos, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (pos < len)
                Arrays.Copy(s, pos + 1, s, pos, len - pos - 1);

            return len - 1;
        }
    }
}