// Lucene version compatibility level 9.2
using Lucene.Net.Analysis.Util;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Fa
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
    /// Stemmer for Persian.
    /// <para/>
    /// Stemming is done in-place for efficiency, operating on a termbuffer.
    /// <para/>
    /// Stemming is defined as:
    /// <list type="bullet">
    ///     <item><description> Removal of attached definite article, conjunction, and prepositions.</description></item>
    ///     <item><description> Stemming of common suffixes.</description></item>
    /// </list>
    /// </summary>
    public class PersianStemmer
    {
        private const char ALEF = '\u0627';
        private const char HEH = '\u0647';
        private const char TEH = '\u062A';
        private const char REH = '\u0631';
        private const char NOON = '\u0646';
        private const char YEH = '\u064A';
        private const char ZWNJ = '\u200c'; // ZERO WIDTH NON-JOINER character

        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        private static IList<char[]> Suffixes { get; } = InitializeSuffix();

        private static IList<char[]> InitializeSuffix()
        {
            return new JCG.List<char[]>(){
                ("" + ALEF + TEH).ToCharArray(),
                ("" + ALEF + NOON).ToCharArray(),
                ("" + TEH + REH + YEH + NOON).ToCharArray(),
                ("" + TEH + REH).ToCharArray(),
                ("" + YEH + YEH).ToCharArray(),
                ("" + YEH).ToCharArray(),
                ("" + HEH + ALEF).ToCharArray(),
                ("" + ZWNJ).ToCharArray()
            };
        }

        /// <summary>
        /// Stem an input buffer of Persian text.
        /// </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after normalization </returns>
        public virtual int Stem(char[] s, int len)
        {
            len = StemSuffix(s, len);

            return len;
        }

        /// <summary>
        /// Stem suffix(es) off an Persian word. </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> new length of input buffer after stemming </returns>
        private int StemSuffix(char[] s, int len)
        {
            foreach (var suffix in Suffixes)
            {
                if (EndsWithCheckLength(s, len, suffix))
                {
                    len = StemmerUtil.DeleteN(s, len - suffix.Length, len, suffix.Length);
                }
            }
            return len;
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