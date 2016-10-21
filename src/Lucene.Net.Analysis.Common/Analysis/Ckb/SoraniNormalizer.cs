using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using System.Globalization;

namespace Lucene.Net.Analysis.Ckb
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
    /// Normalizes the Unicode representation of Sorani text.
    /// <para>
    /// Normalization consists of:
    /// <ul>
    ///   <li>Alternate forms of 'y' (0064, 0649) are converted to 06CC (FARSI YEH)
    ///   <li>Alternate form of 'k' (0643) is converted to 06A9 (KEHEH)
    ///   <li>Alternate forms of vowel 'e' (0647+200C, word-final 0647, 0629) are converted to 06D5 (AE)
    ///   <li>Alternate (joining) form of 'h' (06BE) is converted to 0647
    ///   <li>Alternate forms of 'rr' (0692, word-initial 0631) are converted to 0695 (REH WITH SMALL V BELOW)
    ///   <li>Harakat, tatweel, and formatting characters such as directional controls are removed.
    /// </ul>
    /// </para>
    /// </summary>
    public class SoraniNormalizer
    {

        internal const char YEH = '\u064A';
        internal const char DOTLESS_YEH = '\u0649';
        internal const char FARSI_YEH = '\u06CC';

        internal const char KAF = '\u0643';
        internal const char KEHEH = '\u06A9';

        internal const char HEH = '\u0647';
        internal const char AE = '\u06D5';
        internal const char ZWNJ = '\u200C';
        internal const char HEH_DOACHASHMEE = '\u06BE';
        internal const char TEH_MARBUTA = '\u0629';

        internal const char REH = '\u0631';
        internal const char RREH = '\u0695';
        internal const char RREH_ABOVE = '\u0692';

        internal const char TATWEEL = '\u0640';
        internal const char FATHATAN = '\u064B';
        internal const char DAMMATAN = '\u064C';
        internal const char KASRATAN = '\u064D';
        internal const char FATHA = '\u064E';
        internal const char DAMMA = '\u064F';
        internal const char KASRA = '\u0650';
        internal const char SHADDA = '\u0651';
        internal const char SUKUN = '\u0652';

        /// <summary>
        /// Normalize an input buffer of Sorani text
        /// </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after normalization </returns>
        public virtual int normalize(char[] s, int len)
        {
            for (int i = 0; i < len; i++)
            {
                switch (s[i])
                {
                    case YEH:
                    case DOTLESS_YEH:
                        s[i] = FARSI_YEH;
                        break;
                    case KAF:
                        s[i] = KEHEH;
                        break;
                    case ZWNJ:
                        if (i > 0 && s[i - 1] == HEH)
                        {
                            s[i - 1] = AE;
                        }
                        len = StemmerUtil.Delete(s, i, len);
                        i--;
                        break;
                    case HEH:
                        if (i == len - 1)
                        {
                            s[i] = AE;
                        }
                        break;
                    case TEH_MARBUTA:
                        s[i] = AE;
                        break;
                    case HEH_DOACHASHMEE:
                        s[i] = HEH;
                        break;
                    case REH:
                        if (i == 0)
                        {
                            s[i] = RREH;
                        }
                        break;
                    case RREH_ABOVE:
                        s[i] = RREH;
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
                        if (CharUnicodeInfo.GetUnicodeCategory(s[i]) == UnicodeCategory.Format)
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