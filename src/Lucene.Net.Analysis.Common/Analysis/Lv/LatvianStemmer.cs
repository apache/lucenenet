// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Lv
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
    /// Light stemmer for Latvian.
    /// <para>
    /// This is a light version of the algorithm in Karlis Kreslin's PhD thesis
    /// <c>A stemming algorithm for Latvian</c> with the following modifications:
    /// <list type="bullet">
    ///   <item><description>Only explicitly stems noun and adjective morphology</description></item>
    ///   <item><description>Stricter length/vowel checks for the resulting stems (verb etc suffix stripping is removed)</description></item>
    ///   <item><description>Removes only the primary inflectional suffixes: case and number for nouns ; 
    ///       case, number, gender, and definitiveness for adjectives.</description></item>
    ///   <item><description>Palatalization is only handled when a declension II,V,VI noun suffix is removed.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class LatvianStemmer
    {
        /// <summary>
        /// Stem a latvian word. returns the new adjusted length.
        /// </summary>
        public virtual int Stem(char[] s, int len)
        {
            int numVowels = NumVowels(s, len);

            for (int i = 0; i < affixes.Length; i++)
            {
                Affix affix = affixes[i];
                if (numVowels > affix.vc && len >= affix.affix.Length + 3 && StemmerUtil.EndsWith(s, len, affix.affix))
                {
                    len -= affix.affix.Length;
                    return affix.palatalizes ? Unpalatalize(s, len) : len;
                }
            }

            return len;
        }

        internal static readonly Affix[] affixes = {
            new Affix("ajiem", 3, false), new Affix("ajai",  3, false),
            new Affix("ajam",  2, false), new Affix("ajām",  2, false),
            new Affix("ajos",  2, false), new Affix("ajās",  2, false),
            new Affix("iem",   2, true),  new Affix("ajā",   2, false),
            new Affix("ais",   2, false), new Affix("ai",    2, false),
            new Affix("ei",    2, false), new Affix("ām",    1, false),
            new Affix("am",    1, false), new Affix("ēm",    1, false),
            new Affix("īm",    1, false), new Affix("im",    1, false),
            new Affix("um",    1, false), new Affix("us",    1, true),
            new Affix("as",    1, false), new Affix("ās",    1, false),
            new Affix("es",    1, false), new Affix("os",    1, true),
            new Affix("ij",    1, false), new Affix("īs",    1, false),
            new Affix("ēs",    1, false), new Affix("is",    1, false),
            new Affix("ie",    1, false), new Affix("u",     1, true),
            new Affix("a",     1, true),  new Affix("i",     1, true),
            new Affix("e",     1, false), new Affix("ā",     1, false),
            new Affix("ē",     1, false), new Affix("ī",     1, false),
            new Affix("ū",     1, false), new Affix("o",     1, false),
            new Affix("s",     0, false), new Affix("š",     0, false),
        };

        internal class Affix
        {
            internal char[] affix; // suffix
            internal int vc; // vowel count of the suffix
            internal bool palatalizes; // true if we should fire palatalization rules.

            internal Affix(string affix, int vc, bool palatalizes)
            {
                this.affix = affix.ToCharArray();
                this.vc = vc;
                this.palatalizes = palatalizes;
            }
        }

        /// <summary>
        /// Most cases are handled except for the ambiguous ones:
        /// <list type="bullet">
        ///     <item><description> s -> š</description></item>
        ///     <item><description> t -> š</description></item>
        ///     <item><description> d -> ž</description></item>
        ///     <item><description> z -> ž</description></item>
        /// </list>
        /// </summary>
        private static int Unpalatalize(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            // we check the character removed: if its -u then 
            // its 2,5, or 6 gen pl., and these two can only apply then.
            if (s[len] == 'u')
            {
                // kš -> kst
                if (StemmerUtil.EndsWith(s, len, "kš"))
                {
                    len++;
                    s[len - 2] = 's';
                    s[len - 1] = 't';
                    return len;
                }
                // ņņ -> nn
                if (StemmerUtil.EndsWith(s, len, "ņņ"))
                {
                    s[len - 2] = 'n';
                    s[len - 1] = 'n';
                    return len;
                }
            }

            // otherwise all other rules
            if (StemmerUtil.EndsWith(s, len, "pj") || StemmerUtil.EndsWith(s, len, "bj") || StemmerUtil.EndsWith(s, len, "mj") || StemmerUtil.EndsWith(s, len, "vj"))
            {
                // labial consonant
                return len - 1;
            }
            else if (StemmerUtil.EndsWith(s, len, "šņ"))
            {
                s[len - 2] = 's';
                s[len - 1] = 'n';
                return len;
            }
            else if (StemmerUtil.EndsWith(s, len, "žņ"))
            {
                s[len - 2] = 'z';
                s[len - 1] = 'n';
                return len;
            }
            else if (StemmerUtil.EndsWith(s, len, "šļ"))
            {
                s[len - 2] = 's';
                s[len - 1] = 'l';
                return len;
            }
            else if (StemmerUtil.EndsWith(s, len, "žļ"))
            {
                s[len - 2] = 'z';
                s[len - 1] = 'l';
                return len;
            }
            else if (StemmerUtil.EndsWith(s, len, "ļņ"))
            {
                s[len - 2] = 'l';
                s[len - 1] = 'n';
                return len;
            }
            else if (StemmerUtil.EndsWith(s, len, "ļļ"))
            {
                s[len - 2] = 'l';
                s[len - 1] = 'l';
                return len;
            }
            else if (s[len - 1] == 'č')
            {
                s[len - 1] = 'c';
                return len;
            }
            else if (s[len - 1] == 'ļ')
            {
                s[len - 1] = 'l';
                return len;
            }
            else if (s[len - 1] == 'ņ')
            {
                s[len - 1] = 'n';
                return len;
            }

            return len;
        }

        /// <summary>
        /// Count the vowels in the string, we always require at least
        /// one in the remaining stem to accept it.
        /// </summary>
        private static int NumVowels(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            int n = 0;
            for (int i = 0; i < len; i++)
            {
                switch (s[i])
                {
                    case 'a':
                    case 'e':
                    case 'i':
                    case 'o':
                    case 'u':
                    case 'ā':
                    case 'ī':
                    case 'ē':
                    case 'ū':
                        n++;
                        break;
                }
            }
            return n;
        }
    }
}