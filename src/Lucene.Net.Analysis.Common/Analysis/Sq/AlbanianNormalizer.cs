// LUCENENET specific: Albanian is not supported by Apache Lucene 4.8; this analyzer is a .NET-only addition.
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Sq
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
    /// Normalizer for Albanian.
    /// <para>
    /// Standard Albanian orthography has two letters outside ASCII, <c>ë</c> and <c>ç</c>.
    /// Both are very frequently written as plain <c>e</c> and <c>c</c> in informal text,
    /// on keyboards without those keys, and in search queries, so the two spellings must
    /// index to the same term. This normalizer folds:
    /// <list type="bullet">
    ///     <item><description><c>ë</c> → <c>e</c>, <c>ç</c> → <c>c</c>, whether precomposed or
    ///     written as a base letter followed by a combining diaeresis / cedilla
    ///     (text copied from PDFs and some web pages comes in that decomposed form)</description></item>
    ///     <item><description>the circumflex and acute vowels of Gheg writing (<c>â ê î ô û</c>
    ///     for nasal vowels, <c>á é í ó ú</c> for long ones, as in <c>âsht</c>, <c>nânë</c>,
    ///     <c>Malcís</c>) → plain vowels</description></item>
    ///     <item><description>elided clitics written with an apostrophe are dropped
    ///     (<c>ç'është</c> → <c>është</c>, <c>s'ka</c> → <c>ka</c>, <c>t'i</c> → <c>i</c>),
    ///     so the remaining word can be matched against the stop list</description></item>
    ///     <item><description>other apostrophes inside a word are removed (<c>Kur'an</c> → <c>kuran</c>),
    ///     covering U+0027, U+2019 and U+02BC which all occur in Albanian text</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The folding is applied to both indexed text and queries, so it also lets the
    /// <see cref="AlbanianLightStemmer"/> work on a single spelling of every suffix.
    /// </para>
    /// </summary>
    public class AlbanianNormalizer
    {
        /// <summary>
        /// Normalize an input buffer of Albanian text.
        /// </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after normalization </returns>
        public virtual int Normalize(char[] s, int len)
        {
            for (int i = 0; i < len; i++)
            {
                switch (s[i])
                {
                    case 'ë':
                    case 'ê':
                    case 'é':
                        s[i] = 'e';
                        break;
                    case 'Ë':
                    case 'Ê':
                    case 'É':
                        s[i] = 'E';
                        break;
                    case 'ç':
                        s[i] = 'c';
                        break;
                    case 'Ç':
                        s[i] = 'C';
                        break;
                    case 'â':
                    case 'á':
                        s[i] = 'a';
                        break;
                    case 'Â':
                    case 'Á':
                        s[i] = 'A';
                        break;
                    case 'î':
                    case 'í':
                        s[i] = 'i';
                        break;
                    case 'Î':
                    case 'Í':
                        s[i] = 'I';
                        break;
                    case 'ô':
                    case 'ó':
                        s[i] = 'o';
                        break;
                    case 'Ô':
                    case 'Ó':
                        s[i] = 'O';
                        break;
                    case 'û':
                    case 'ú':
                        s[i] = 'u';
                        break;
                    case 'Û':
                    case 'Ú':
                        s[i] = 'U';
                        break;
                    case '̈': // combining diaeresis (e + ̈ = ë)
                    case '̧': // combining cedilla (c + ̧ = ç)
                    case '̂': // combining circumflex (Gheg â ê î ô û)
                    case '́': // combining acute (Gheg á é í ó ú)
                        len = StemmerUtil.Delete(s, i, len);
                        i--;
                        break;
                    case '\'':      // apostrophe
                    case '’':  // right single quotation mark
                    case 'ʼ':  // modifier letter apostrophe
                        if (i == 1 && len > 2)
                        {
                            // a one-letter elided clitic: ç'është, s'ka, t'i, m'u, q'u
                            len = StemmerUtil.DeleteN(s, 0, len, 2);
                            i = -1;
                        }
                        else
                        {
                            len = StemmerUtil.Delete(s, i, len);
                            i--;
                        }
                        break;
                }
            }
            return len;
        }
    }
}
