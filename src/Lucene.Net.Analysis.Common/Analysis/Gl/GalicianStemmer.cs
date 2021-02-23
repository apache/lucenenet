// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Pt;
using Lucene.Net.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Gl
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
    /// Galician stemmer implementing "Regras do lematizador para o galego".
    /// </summary>
    /// <seealso cref="RSLPStemmerBase"/>
    /// <a href="http://bvg.udc.es/recursos_lingua/stemming.jsp">Description of rules</a>
    public class GalicianStemmer : RSLPStemmerBase
    {
        private static readonly Step plural, unification, adverb, augmentative, noun, verb, vowel;

        static GalicianStemmer()
        {
            IDictionary<string, Step> steps = Parse(typeof(GalicianStemmer), "galician.rslp");
            plural = steps["Plural"];
            unification = steps["Unification"];
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
            len = unification.Apply(s, len);
            len = adverb.Apply(s, len);

            int oldlen;
            do
            {
                oldlen = len;
                len = augmentative.Apply(s, len);
            } while (len != oldlen);

            oldlen = len;
            len = noun.Apply(s, len);
            if (len == oldlen) // suffix not removed
            {
                len = verb.Apply(s, len);
            }

            len = vowel.Apply(s, len);

            // RSLG accent removal
            for (int i = 0; i < len; i++)
            {
                switch (s[i])
                {
                    case 'á':
                        s[i] = 'a';
                        break;
                    case 'é':
                    case 'ê':
                        s[i] = 'e';
                        break;
                    case 'í':
                        s[i] = 'i';
                        break;
                    case 'ó':
                        s[i] = 'o';
                        break;
                    case 'ú':
                        s[i] = 'u';
                        break;
                }
            }

            return len;
        }
    }
}