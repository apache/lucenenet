// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System;

namespace Lucene.Net.Analysis.No
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

    /*
     * This algorithm is updated based on code located at:
     * http://members.unine.ch/jacques.savoy/clef/
     *
     * Full copyright for that code follows:
     */

    /*
     * Copyright (c) 2005, Jacques Savoy
     * All rights reserved.
     *
     * Redistribution and use in source and binary forms, with or without
     * modification, are permitted provided that the following conditions are met:
     *
     * Redistributions of source code must retain the above copyright notice, this
     * list of conditions and the following disclaimer. Redistributions in binary
     * form must reproduce the above copyright notice, this list of conditions and
     * the following disclaimer in the documentation and/or other materials
     * provided with the distribution. Neither the name of the author nor the names
     * of its contributors may be used to endorse or promote products derived from
     * this software without specific prior written permission.
     *
     * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
     * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
     * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
     * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
     * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
     * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
     * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
     * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
     * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
     * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
     * POSSIBILITY OF SUCH DAMAGE.
     */

    [System.Flags]
    public enum NorwegianStandard
    {
        /// <summary>
        /// Constant to remove Bokmål-specific endings </summary>
        BOKMAAL = 1,

        /// <summary>
        /// Constant to remove Nynorsk-specific endings </summary>
        NYNORSK = 2
    }

    /// <summary>
    /// Light Stemmer for Norwegian.
    /// <para>
    /// Parts of this stemmer is adapted from <see cref="Sv.SwedishLightStemFilter"/>, except
    /// that while the Swedish one has a pre-defined rule set and a corresponding
    /// corpus to validate against whereas the Norwegian one is hand crafted.
    /// </para>
    /// </summary>
    public class NorwegianLightStemmer
    {
        // LUCENENET specific - made flags into their own [Flags] enum named NorwegianStandard and de-nested from this type

        private readonly bool useBokmaal;
        private readonly bool useNynorsk;

        /// <summary>
        /// Creates a new <see cref="NorwegianLightStemmer"/> </summary>
        /// <param name="flags"> set to <see cref="NorwegianStandard.BOKMAAL"/>, <see cref="NorwegianStandard.NYNORSK"/>, or both. </param>
        public NorwegianLightStemmer(NorwegianStandard flags)
        {
            if (flags <= 0 || flags > (int)NorwegianStandard.BOKMAAL + NorwegianStandard.NYNORSK)
            {
                throw new ArgumentException("invalid flags");
            }

            useBokmaal = (flags & NorwegianStandard.BOKMAAL) != 0;
            useNynorsk = (flags & NorwegianStandard.NYNORSK) != 0;
        }

        public virtual int Stem(char[] s, int len)
        {
            // Remove posessive -s (bilens -> bilen) and continue checking
            if (len > 4 && s[len - 1] == 's')
            {
                len--;
            }

            // Remove common endings, single-pass
            if (len > 7 &&
                ((StemmerUtil.EndsWith(s, len, "heter") &&
                useBokmaal) ||  // general ending (hemmeleg-heita -> hemmeleg)
                (StemmerUtil.EndsWith(s, len, "heten") &&
                useBokmaal) ||  // general ending (hemmelig-heten -> hemmelig)
                (StemmerUtil.EndsWith(s, len, "heita") &&
                useNynorsk)))   // general ending (hemmeleg-heita -> hemmeleg)
            {
                return len - 5;
            }

            // Remove Nynorsk common endings, single-pass
            if (len > 8 && useNynorsk &&
                (StemmerUtil.EndsWith(s, len, "heiter") || // general ending (hemmeleg-heiter -> hemmeleg)
                StemmerUtil.EndsWith(s, len, "leiken") ||  // general ending (trygg-leiken -> trygg)
                StemmerUtil.EndsWith(s, len, "leikar")))   // general ending (trygg-leikar -> trygg)
            {
                return len - 6;
            }

            if (len > 5 &&
                (StemmerUtil.EndsWith(s, len, "dom") ||  // general ending (kristen-dom -> kristen)
                (StemmerUtil.EndsWith(s, len, "het") &&
                useBokmaal)))                            // general ending (hemmelig-het -> hemmelig)
            {
                return len - 3;
            }

            if (len > 6 && useNynorsk &&
                (StemmerUtil.EndsWith(s, len, "heit") ||  // general ending (hemmeleg-heit -> hemmeleg)
                StemmerUtil.EndsWith(s, len, "semd") ||   // general ending (verk-semd -> verk)
                StemmerUtil.EndsWith(s, len,"leik")))     // general ending (trygg-leik -> trygg)
            {
                return len - 4;
            }

            if (len > 7 &&
                (StemmerUtil.EndsWith(s, len, "elser") ||   // general ending (føl-elser -> føl)
                StemmerUtil.EndsWith(s, len, "elsen")))     // general ending (føl-elsen -> føl)
            {
                return len - 5;
            }

            if (len > 6 &&
                ((StemmerUtil.EndsWith(s, len, "ende") &&
                useBokmaal) ||      // (sov-ende -> sov)
                (StemmerUtil.EndsWith(s, len, "ande") &&
                useNynorsk) ||      // (sov-ande -> sov)
                StemmerUtil.EndsWith(s, len, "else") ||  // general ending (føl-else -> føl)
                (StemmerUtil.EndsWith(s, len, "este") &&
                useBokmaal) ||      // adj (fin-este -> fin)
                (StemmerUtil.EndsWith(s, len, "aste") &&
                useNynorsk) ||      // adj (fin-aste -> fin)
                (StemmerUtil.EndsWith(s, len, "eren") &&
                useBokmaal) ||      // masc
                (StemmerUtil.EndsWith(s, len, "aren") &&
                    useNynorsk)))       // masc
            {
                return len - 4;
            }

            if (len > 5 &&
                ((StemmerUtil.EndsWith(s, len, "ere") &&
                useBokmaal) ||     // adj (fin-ere -> fin)
                (StemmerUtil.EndsWith(s, len, "are") &&
                useNynorsk) ||    // adj (fin-are -> fin)
                (StemmerUtil.EndsWith(s, len, "est") &&
                useBokmaal) ||    // adj (fin-est -> fin)
                (StemmerUtil.EndsWith(s, len, "ast") &&
                useNynorsk) ||    // adj (fin-ast -> fin)
                StemmerUtil.EndsWith(s, len, "ene") || // masc/fem/neutr pl definite (hus-ene)
                (StemmerUtil.EndsWith(s, len, "ane") &&
                useNynorsk)))     // masc pl definite (gut-ane)
            {
                return len - 3;
            }

            if (len > 4 &&
                (StemmerUtil.EndsWith(s, len, "er") ||  // masc/fem indefinite
                StemmerUtil.EndsWith(s, len, "en") ||   // masc/fem definite
                StemmerUtil.EndsWith(s, len, "et") ||   // neutr definite
                (StemmerUtil.EndsWith(s, len, "ar") &&
                useNynorsk) ||    // masc pl indefinite
                (StemmerUtil.EndsWith(s, len, "st") &&
                useBokmaal) ||    // adj (billig-st -> billig)
                StemmerUtil.EndsWith(s, len, "te")))
            {
                return len - 2;
            }

            if (len > 3)
            {
                switch (s[len - 1])
                {
                    case 'a': // fem definite
                    case 'e': // to get correct stem for nouns ending in -e (kake -> kak, kaker -> kak)
                    case 'n':
                        return len - 1;
                }
            }

            return len;
        }
    }
}
