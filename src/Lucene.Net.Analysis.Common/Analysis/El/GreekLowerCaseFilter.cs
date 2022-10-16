// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.Globalization;

namespace Lucene.Net.Analysis.El
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
    /// Normalizes token text to lower case, removes some Greek diacritics,
    /// and standardizes final sigma to sigma. 
    /// <para>You must specify the required <see cref="LuceneVersion"/>
    /// compatibility when creating <see cref="GreekLowerCaseFilter"/>:
    /// <list type="bullet">
    ///     <item><description> As of 3.1, supplementary characters are properly lowercased.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class GreekLowerCaseFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;
        private readonly CharacterUtils charUtils;

        private static readonly CultureInfo culture = new CultureInfo("el"); // LUCENENET specific - use Greek culture when lowercasing.

        /// <summary>
        /// Create a <see cref="GreekLowerCaseFilter"/> that normalizes Greek token text.
        /// </summary>
        /// <param name="matchVersion"> Lucene compatibility version, 
        ///   See <see cref="LuceneVersion"/> </param>
        /// <param name="in"> <see cref="TokenStream"/> to filter </param>
        public GreekLowerCaseFilter(LuceneVersion matchVersion, TokenStream @in)
              : base(@in)
        {
            this.charUtils = CharacterUtils.GetInstance(matchVersion);
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                char[] chArray = termAtt.Buffer;
                int chLen = termAtt.Length;
                for (int i = 0; i < chLen;)
                {
                    i += Character.ToChars(LowerCase(charUtils.CodePointAt(chArray, i, chLen)), chArray, i);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private static int LowerCase(int codepoint) // LUCENENET: CA1822: Mark members as static
        {
            switch (codepoint)
            {
                /* There are two lowercase forms of sigma:
                 *   U+03C2: small final sigma (end of word)
                 *   U+03C3: small sigma (otherwise)
                 *   
                 * Standardize both to U+03C3
                 */
                case '\u03C2': // small final sigma
                    return '\u03C3'; // small sigma

                /* Some greek characters contain diacritics.
                 * This filter removes these, converting to the lowercase base form.
                 */

                case '\u0386': // capital alpha with tonos
                case '\u03AC': // small alpha with tonos
                    return '\u03B1'; // small alpha

                case '\u0388': // capital epsilon with tonos
                case '\u03AD': // small epsilon with tonos
                    return '\u03B5'; // small epsilon

                case '\u0389': // capital eta with tonos
                case '\u03AE': // small eta with tonos
                    return '\u03B7'; // small eta

                case '\u038A': // capital iota with tonos
                case '\u03AA': // capital iota with dialytika
                case '\u03AF': // small iota with tonos
                case '\u03CA': // small iota with dialytika
                case '\u0390': // small iota with dialytika and tonos
                    return '\u03B9'; // small iota

                case '\u038E': // capital upsilon with tonos
                case '\u03AB': // capital upsilon with dialytika
                case '\u03CD': // small upsilon with tonos
                case '\u03CB': // small upsilon with dialytika
                case '\u03B0': // small upsilon with dialytika and tonos
                    return '\u03C5'; // small upsilon

                case '\u038C': // capital omicron with tonos
                case '\u03CC': // small omicron with tonos
                    return '\u03BF'; // small omicron

                case '\u038F': // capital omega with tonos
                case '\u03CE': // small omega with tonos
                    return '\u03C9'; // small omega

                /* The previous implementation did the conversion below.
                 * Only implemented for backwards compatibility with old indexes.
                 */

                case '\u03A2': // reserved
                    return '\u03C2'; // small final sigma

                default:
                    return Character.ToLower(codepoint, culture); // LUCENENET specific - need to use specific culture to override current thread
            }
        }
    }
}