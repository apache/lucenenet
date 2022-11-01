// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Bg
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
    /// Light Stemmer for Bulgarian.
    /// <para>
    /// Implements the algorithm described in:  
    /// <c>
    /// Searching Strategies for the Bulgarian Language
    /// </c>
    /// http://members.unine.ch/jacques.savoy/Papers/BUIR.pdf
    /// </para>
    /// </summary>
    public class BulgarianStemmer
    {

        /// <summary>
        /// Stem an input buffer of Bulgarian text.
        /// </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after normalization </returns>
        public virtual int Stem(char[] s, int len)
        {
            if (len < 4) // do not stem
            {
                return len;
            }

            if (len > 5 && StemmerUtil.EndsWith(s, len, "ища"))
            {
                return len - 3;
            }

            len = RemoveArticle(s, len);
            len = RemovePlural(s, len);

            if (len > 3)
            {
                if (StemmerUtil.EndsWith(s, len, "я"))
                {
                    len--;
                }
                if (StemmerUtil.EndsWith(s, len, "а") || StemmerUtil.EndsWith(s, len, "о") || StemmerUtil.EndsWith(s, len, "е"))
                {
                    len--;
                }
            }

            // the rule to rewrite ен -> н is duplicated in the paper.
            // in the perl implementation referenced by the paper, this is fixed.
            // (it is fixed here as well)
            if (len > 4 && StemmerUtil.EndsWith(s, len, "ен"))
            {
                s[len - 2] = 'н'; // replace with н
                len--;
            }

            if (len > 5 && s[len - 2] == 'ъ')
            {
                s[len - 2] = s[len - 1]; // replace ъN with N
                len--;
            }

            return len;
        }

        /// <summary>
        /// Mainly remove the definite article </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> new stemmed length </returns>
        private static int RemoveArticle(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 6 && StemmerUtil.EndsWith(s, len, "ият"))
            {
                return len - 3;
            }

            if (len > 5)
            {
                if (StemmerUtil.EndsWith(s, len, "ът") || StemmerUtil.EndsWith(s, len, "то") || StemmerUtil.EndsWith(s, len, "те") || StemmerUtil.EndsWith(s, len, "та") || StemmerUtil.EndsWith(s, len, "ия"))
                {
                    return len - 2;
                }
            }

            if (len > 4 && StemmerUtil.EndsWith(s, len, "ят"))
            {
                return len - 2;
            }

            return len;
        }

        private static int RemovePlural(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 6)
            {
                if (StemmerUtil.EndsWith(s, len, "овци"))
                {
                    return len - 3; // replace with о
                }
                if (StemmerUtil.EndsWith(s, len, "ове"))
                {
                    return len - 3;
                }
                if (StemmerUtil.EndsWith(s, len, "еве"))
                {
                    s[len - 3] = 'й'; // replace with й
                    return len - 2;
                }
            }

            if (len > 5)
            {
                if (StemmerUtil.EndsWith(s, len, "ища"))
                {
                    return len - 3;
                }
                if (StemmerUtil.EndsWith(s, len, "та"))
                {
                    return len - 2;
                }
                if (StemmerUtil.EndsWith(s, len, "ци"))
                {
                    s[len - 2] = 'к'; // replace with к
                    return len - 1;
                }
                if (StemmerUtil.EndsWith(s, len, "зи"))
                {
                    s[len - 2] = 'г'; // replace with г
                    return len - 1;
                }

                if (s[len - 3] == 'е' && s[len - 1] == 'и')
                {
                    s[len - 3] = 'я'; // replace е with я, remove и
                    return len - 1;
                }
            }

            if (len > 4)
            {
                if (StemmerUtil.EndsWith(s, len, "си"))
                {
                    s[len - 2] = 'х'; // replace with х
                    return len - 1;
                }
                if (StemmerUtil.EndsWith(s, len, "и"))
                {
                    return len - 1;
                }
            }

            return len;
        }
    }
}