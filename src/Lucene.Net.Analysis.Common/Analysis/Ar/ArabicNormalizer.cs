// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Ar
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
    /// Normalizer for Arabic.
    /// <para/>
    /// Normalization is done in-place for efficiency, operating on a termbuffer.
    /// <para/>
    /// Normalization is defined as:
    /// <list type="bullet">
    ///     <item><description> Normalization of hamza with alef seat to a bare alef.</description></item>
    ///     <item><description> Normalization of teh marbuta to heh</description></item>
    ///     <item><description> Normalization of dotless yeh (alef maksura) to yeh.</description></item>
    ///     <item><description> Removal of Arabic diacritics (the harakat)</description></item>
    ///     <item><description> Removal of tatweel (stretching character).</description></item>
    /// </list>
    /// </summary>
    public class ArabicNormalizer
    {
        public const char ALEF = '\u0627';
        public const char ALEF_MADDA = '\u0622';
        public const char ALEF_HAMZA_ABOVE = '\u0623';
        public const char ALEF_HAMZA_BELOW = '\u0625';

        public const char YEH = '\u064A';
        public const char DOTLESS_YEH = '\u0649';

        public const char TEH_MARBUTA = '\u0629';
        public const char HEH = '\u0647';

        public const char TATWEEL = '\u0640';

        public const char FATHATAN = '\u064B';
        public const char DAMMATAN = '\u064C';
        public const char KASRATAN = '\u064D';
        public const char FATHA = '\u064E';
        public const char DAMMA = '\u064F';
        public const char KASRA = '\u0650';
        public const char SHADDA = '\u0651';
        public const char SUKUN = '\u0652';

        /// <summary>
        /// Normalize an input buffer of Arabic text
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
                    case ALEF_MADDA:
                    case ALEF_HAMZA_ABOVE:
                    case ALEF_HAMZA_BELOW:
                        s[i] = ALEF;
                        break;
                    case DOTLESS_YEH:
                        s[i] = YEH;
                        break;
                    case TEH_MARBUTA:
                        s[i] = HEH;
                        break;
                    case TATWEEL:
                    case KASRATAN:
                    case DAMMATAN:
                    case FATHATAN:
                    case FATHA:
                    case DAMMA:
                    case KASRA:
                    case SHADDA:
                    case SUKUN:
                        len = StemmerUtil.Delete(s, i, len);
                        i--;
                        break;
                    default:
                        break;
                }
            }

            return len;
        }
    }
}