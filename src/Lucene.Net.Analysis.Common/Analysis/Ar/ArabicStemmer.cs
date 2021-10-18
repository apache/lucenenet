// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// Stemmer for Arabic.
    /// <para/>
    /// Stemming is done in-place for efficiency, operating on a termbuffer.
    /// <para/>
    /// Stemming is defined as:
    /// <list type="bullet">
    ///     <item><description> Removal of attached definite article, conjunction, and prepositions.</description></item>
    ///     <item><description> Stemming of common suffixes.</description></item>
    /// </list>
    /// </summary>
    public class ArabicStemmer
    {
        public const char ALEF = '\u0627';
        public const char BEH = '\u0628';
        public const char TEH_MARBUTA = '\u0629';
        public const char TEH = '\u062A';
        public const char FEH = '\u0641';
        public const char KAF = '\u0643';
        public const char LAM = '\u0644';
        public const char NOON = '\u0646';
        public const char HEH = '\u0647';
        public const char WAW = '\u0648';
        public const char YEH = '\u064A';

        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static IList<char[]> Prefixes { get; } = InitializePrefix();
        public static IList<char[]> Suffixes { get; } = InitializeSuffix();

        private static IList<char[]> InitializePrefix()
        {
            return new JCG.List<char[]>(){ ("" + ALEF + LAM).ToCharArray(),
            ("" + WAW + ALEF + LAM).ToCharArray(),
            ("" + BEH + ALEF + LAM).ToCharArray(),
            ("" + KAF + ALEF + LAM).ToCharArray(),
            ("" + FEH + ALEF + LAM).ToCharArray(),
            ("" + LAM + LAM).ToCharArray(),
            ("" + WAW).ToCharArray() };
        }
        private static IList<char[]> InitializeSuffix()
        {
            return new JCG.List<char[]>(){ ("" + HEH + ALEF).ToCharArray(),
            ("" + ALEF + NOON).ToCharArray(),
            ("" + ALEF + TEH).ToCharArray(),
            ("" + WAW + NOON).ToCharArray(),
            ("" + YEH + NOON).ToCharArray(),
            ("" + YEH + HEH).ToCharArray(),
            ("" + YEH + TEH_MARBUTA).ToCharArray(),
            ("" + HEH).ToCharArray(),
            ("" + TEH_MARBUTA).ToCharArray(),
            ("" + YEH).ToCharArray() };
        }
        
        /// <summary>
        /// Stem an input buffer of Arabic text.
        /// </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after normalization </returns>
        public virtual int Stem(char[] s, int len)
        {
            len = StemPrefix(s, len);
            len = StemSuffix(s, len);

            return len;
        }

        /// <summary>
        /// Stem a prefix off an Arabic word. </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> new length of input buffer after stemming. </returns>
        public virtual int StemPrefix(char[] s, int len)
        {
            for (int i = 0; i < Prefixes.Count; i++)
            {
                if (StartsWithCheckLength(s, len, Prefixes[i]))
                {
                    return StemmerUtil.DeleteN(s, 0, len, Prefixes[i].Length);
                }
            }
            return len;
        }

        /// <summary>
        /// Stem suffix(es) off an Arabic word. </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> new length of input buffer after stemming </returns>
        public virtual int StemSuffix(char[] s, int len)
        {
            for (int i = 0; i < Suffixes.Count; i++)
            {
                if (EndsWithCheckLength(s, len, Suffixes[i]))
                {
                    len = StemmerUtil.DeleteN(s, len - Suffixes[i].Length, len, Suffixes[i].Length);
                }
            }
            return len;
        }

        /// <summary>
        /// Returns true if the prefix matches and can be stemmed </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <param name="prefix"> prefix to check </param>
        /// <returns> true if the prefix matches and can be stemmed </returns>
        internal virtual bool StartsWithCheckLength(char[] s, int len, char[] prefix)
        {
            if (prefix.Length == 1 && len < 4) // wa- prefix requires at least 3 characters
            {
                return false;
            } // other prefixes require only 2.
            else if (len < prefix.Length + 2)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < prefix.Length; i++)
                {
                    if (s[i] != prefix[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Returns true if the suffix matches and can be stemmed </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <param name="suffix"> suffix to check </param>
        /// <returns> true if the suffix matches and can be stemmed </returns>
        internal virtual bool EndsWithCheckLength(char[] s, int len, char[] suffix)
        {
            if (len < suffix.Length + 2) // all suffixes require at least 2 characters after stemming
            {
                return false;
            }
            else
            {
                for (int i = 0; i < suffix.Length; i++)
                {
                    if (s[len - suffix.Length + i] != suffix[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}