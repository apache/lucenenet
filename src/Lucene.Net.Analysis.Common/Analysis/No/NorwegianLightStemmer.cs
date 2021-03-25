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
            if (len > 7 && ((StemmerUtil.EndsWith(s, len, "heter") && useBokmaal) || (StemmerUtil.EndsWith(s, len, "heten") && useBokmaal) || (StemmerUtil.EndsWith(s, len, "heita") && useNynorsk))) // general ending (hemmeleg-heita -> hemmeleg) -  general ending (hemmelig-heten -> hemmelig) -  general ending (hemmelig-heter -> hemmelig)
            {
                return len - 5;
            }

            // Remove Nynorsk common endings, single-pass
            if (len > 8 && useNynorsk && (StemmerUtil.EndsWith(s, len, "heiter") || StemmerUtil.EndsWith(s, len, "leiken") || StemmerUtil.EndsWith(s, len, "leikar"))) // general ending (trygg-leikar -> trygg) -  general ending (trygg-leiken -> trygg) -  general ending (hemmeleg-heiter -> hemmeleg)
            {
                return len - 6;
            }

            if (len > 5 && (StemmerUtil.EndsWith(s, len, "dom") || (StemmerUtil.EndsWith(s, len, "het") && useBokmaal))) // general ending (hemmelig-het -> hemmelig) -  general ending (kristen-dom -> kristen)
            {
                return len - 3;
            }

            if (len > 6 && useNynorsk && (StemmerUtil.EndsWith(s, len, "heit") || StemmerUtil.EndsWith(s, len, "semd") || StemmerUtil.EndsWith(s, len, "leik"))) // general ending (trygg-leik -> trygg) -  general ending (verk-semd -> verk) -  general ending (hemmeleg-heit -> hemmeleg)
            {
                return len - 4;
            }

            if (len > 7 && (StemmerUtil.EndsWith(s, len, "elser") || StemmerUtil.EndsWith(s, len, "elsen"))) // general ending (føl-elsen -> føl) -  general ending (føl-elser -> føl)
            {
                return len - 5;
            }

            if (len > 6 && ((StemmerUtil.EndsWith(s, len, "ende") && useBokmaal) || (StemmerUtil.EndsWith(s, len, "ande") && useNynorsk) || StemmerUtil.EndsWith(s, len, "else") || (StemmerUtil.EndsWith(s, len, "este") && useBokmaal) || (StemmerUtil.EndsWith(s, len, "aste") && useNynorsk) || (StemmerUtil.EndsWith(s, len, "eren") && useBokmaal) || (StemmerUtil.EndsWith(s, len, "aren") && useNynorsk))) // masc -  masc -  adj (fin-aste -> fin) -  adj (fin-este -> fin) -  general ending (føl-else -> føl) -  (sov-ande -> sov) -  (sov-ende -> sov)
            {
                return len - 4;
            }

            if (len > 5 && ((StemmerUtil.EndsWith(s, len, "ere") && useBokmaal) || (StemmerUtil.EndsWith(s, len, "are") && useNynorsk) || (StemmerUtil.EndsWith(s, len, "est") && useBokmaal) || (StemmerUtil.EndsWith(s, len, "ast") && useNynorsk) || StemmerUtil.EndsWith(s, len, "ene") || (StemmerUtil.EndsWith(s, len, "ane") && useNynorsk))) // masc pl definite (gut-ane) -  masc/fem/neutr pl definite (hus-ene) -  adj (fin-ast -> fin) -  adj (fin-est -> fin) -  adj (fin-are -> fin) -  adj (fin-ere -> fin)
            {
                return len - 3;
            }

            if (len > 4 && (StemmerUtil.EndsWith(s, len, "er") || StemmerUtil.EndsWith(s, len, "en") || StemmerUtil.EndsWith(s, len, "et") || (StemmerUtil.EndsWith(s, len, "ar") && useNynorsk) || (StemmerUtil.EndsWith(s, len, "st") && useBokmaal) || StemmerUtil.EndsWith(s, len, "te"))) // adj (billig-st -> billig) -  masc pl indefinite -  neutr definite -  masc/fem definite -  masc/fem indefinite
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