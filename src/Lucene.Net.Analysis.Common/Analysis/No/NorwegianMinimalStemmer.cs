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

    /// <summary>
    /// Minimal Stemmer for Norwegian Bokmål (no-nb) and Nynorsk (no-nn)
    /// <para>
    /// Stems known plural forms for Norwegian nouns only, together with genitiv -s
    /// </para>
    /// </summary>
    public class NorwegianMinimalStemmer
    {
        //private readonly bool useBokmaal; // LUCENENET: Never read
        private readonly bool useNynorsk;

        /// <summary>
        /// Creates a new <see cref="NorwegianMinimalStemmer"/> </summary>
        /// <param name="flags"> set to <see cref="NorwegianStandard.BOKMAAL"/>, 
        ///                     <see cref="NorwegianStandard.NYNORSK"/>, or both. </param>
        public NorwegianMinimalStemmer(NorwegianStandard flags)
        {
            if (flags <= 0 || flags > (int)NorwegianStandard.BOKMAAL + NorwegianStandard.NYNORSK)
            {
                throw new ArgumentException("invalid flags");
            }
            //useBokmaal = (flags & NorwegianStandard.BOKMAAL) != 0; // LUCENENET: Never read
            useNynorsk = (flags & NorwegianStandard.NYNORSK) != 0;
        }

        public virtual int Stem(char[] s, int len)
        {
            // Remove genitiv s
            if (len > 4 && s[len - 1] == 's')
            {
                len--;
            }

            if (len > 5 && (StemmerUtil.EndsWith(s, len, "ene") || (StemmerUtil.EndsWith(s, len, "ane") && useNynorsk))) // masc pl definite (gut-ane) -  masc/fem/neutr pl definite (hus-ene)
            {
                return len - 3;
            }

            if (len > 4 && (StemmerUtil.EndsWith(s, len, "er") || StemmerUtil.EndsWith(s, len, "en") || StemmerUtil.EndsWith(s, len, "et") || (StemmerUtil.EndsWith(s, len, "ar") && useNynorsk))) // masc pl indefinite -  neutr definite -  masc/fem definite -  masc/fem indefinite
            {
                return len - 2;
            }

            if (len > 3)
            {
                switch (s[len - 1])
                {
                    case 'a': // fem definite
                    case 'e': // to get correct stem for nouns ending in -e (kake -> kak, kaker -> kak)
                        return len - 1;
                }
            }

            return len;
        }
    }
}