// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Pt
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
    /// Portuguese stemmer implementing the RSLP (Removedor de Sufixos da Lingua Portuguesa)
    /// algorithm. This is sometimes also referred to as the Orengo stemmer.
    /// </summary>
    /// <seealso cref="RSLPStemmerBase"/>
    public class PortugueseStemmer : RSLPStemmerBase
    {
        private static readonly Step plural, feminine, adverb, augmentative, noun, verb, vowel;

        static PortugueseStemmer()
        {
            IDictionary<string, Step> steps = Parse(typeof(PortugueseStemmer), "portuguese.rslp");
            plural = steps["Plural"];
            feminine = steps["Feminine"];
            adverb = steps["Adverb"];
            augmentative = steps["Augmentative"];
            noun = steps["Noun"];
            verb = steps["Verb"];
            vowel = steps["Vowel"];
        }

        /// <param name="s"> buffer, oversized to at least <code>len+1</code> </param>
        /// <param name="len"> initial valid length of buffer </param>
        /// <returns> new valid length, stemmed </returns>
        public virtual int Stem(char[] s, int len)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(s.Length >= len + 1, "this stemmer requires an oversized array of at least 1");

            len = plural.Apply(s, len);
            len = adverb.Apply(s, len);
            len = feminine.Apply(s, len);
            len = augmentative.Apply(s, len);

            int oldlen = len;
            len = noun.Apply(s, len);

            if (len == oldlen) // suffix not removed
            {
                oldlen = len;

                len = verb.Apply(s, len);

                if (len == oldlen) // suffix not removed
                {
                    len = vowel.Apply(s, len);
                }
            }

            // rslp accent removal
            for (int i = 0; i < len; i++)
            {
                switch (s[i])
                {
                    case 'à':
                    case 'á':
                    case 'â':
                    case 'ã':
                    case 'ä':
                    case 'å':
                        s[i] = 'a';
                        break;
                    case 'ç':
                        s[i] = 'c';
                        break;
                    case 'è':
                    case 'é':
                    case 'ê':
                    case 'ë':
                        s[i] = 'e';
                        break;
                    case 'ì':
                    case 'í':
                    case 'î':
                    case 'ï':
                        s[i] = 'i';
                        break;
                    case 'ñ':
                        s[i] = 'n';
                        break;
                    case 'ò':
                    case 'ó':
                    case 'ô':
                    case 'õ':
                    case 'ö':
                        s[i] = 'o';
                        break;
                    case 'ù':
                    case 'ú':
                    case 'û':
                    case 'ü':
                        s[i] = 'u';
                        break;
                    case 'ý':
                    case 'ÿ':
                        s[i] = 'y';
                        break;
                }
            }
            return len;
        }
    }
}