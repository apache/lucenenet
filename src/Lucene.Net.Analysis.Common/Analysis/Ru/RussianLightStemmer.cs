// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Ru
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
    /// Light Stemmer for Russian.
    /// <para>
    /// This stemmer implements the following algorithm:
    /// <c>Indexing and Searching Strategies for the Russian Language.</c>
    /// Ljiljana Dolamic and Jacques Savoy.
    /// </para>
    /// </summary>
    public class RussianLightStemmer
    {
        public virtual int Stem(char[] s, int len)
        {
            len = RemoveCase(s, len);
            return Normalize(s, len);
        }

        private static int Normalize(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 3)
            {
                switch (s[len - 1])
                {
                    case 'ь':
                    case 'и':
                        return len - 1;
                    case 'н':
                        if (s[len - 2] == 'н')
                        {
                            return len - 1;
                        }
                        break;
                }
            }
            return len;
        }

        private static int RemoveCase(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 6 && (StemmerUtil.EndsWith(s, len, "иями") || StemmerUtil.EndsWith(s, len, "оями")))
            {
                return len - 4;
            }

            if (len > 5 && (StemmerUtil.EndsWith(s, len, "иям") || StemmerUtil.EndsWith(s, len, "иях") || StemmerUtil.EndsWith(s, len, "оях") || StemmerUtil.EndsWith(s, len, "ями") || StemmerUtil.EndsWith(s, len, "оям") || StemmerUtil.EndsWith(s, len, "оьв") || StemmerUtil.EndsWith(s, len, "ами") || StemmerUtil.EndsWith(s, len, "его") || StemmerUtil.EndsWith(s, len, "ему") || StemmerUtil.EndsWith(s, len, "ери") || StemmerUtil.EndsWith(s, len, "ими") || StemmerUtil.EndsWith(s, len, "ого") || StemmerUtil.EndsWith(s, len, "ому") || StemmerUtil.EndsWith(s, len, "ыми") || StemmerUtil.EndsWith(s, len, "оев")))
            {
                return len - 3;
            }

            if (len > 4 && (StemmerUtil.EndsWith(s, len, "ая") || StemmerUtil.EndsWith(s, len, "яя") || StemmerUtil.EndsWith(s, len, "ях") || StemmerUtil.EndsWith(s, len, "юю") || StemmerUtil.EndsWith(s, len, "ах") || StemmerUtil.EndsWith(s, len, "ею") || StemmerUtil.EndsWith(s, len, "их") || StemmerUtil.EndsWith(s, len, "ия") || StemmerUtil.EndsWith(s, len, "ию") || StemmerUtil.EndsWith(s, len, "ьв") || StemmerUtil.EndsWith(s, len, "ою") || StemmerUtil.EndsWith(s, len, "ую") || StemmerUtil.EndsWith(s, len, "ям") || StemmerUtil.EndsWith(s, len, "ых") || StemmerUtil.EndsWith(s, len, "ея") || StemmerUtil.EndsWith(s, len, "ам") || StemmerUtil.EndsWith(s, len, "ем") || StemmerUtil.EndsWith(s, len, "ей") || StemmerUtil.EndsWith(s, len, "ём") || StemmerUtil.EndsWith(s, len, "ев") || StemmerUtil.EndsWith(s, len, "ий") || StemmerUtil.EndsWith(s, len, "им") || StemmerUtil.EndsWith(s, len, "ое") || StemmerUtil.EndsWith(s, len, "ой") || StemmerUtil.EndsWith(s, len, "ом") || StemmerUtil.EndsWith(s, len, "ов") || StemmerUtil.EndsWith(s, len, "ые") || StemmerUtil.EndsWith(s, len, "ый") || StemmerUtil.EndsWith(s, len, "ым") || StemmerUtil.EndsWith(s, len, "ми")))
            {
                return len - 2;
            }

            if (len > 3)
            {
                switch (s[len - 1])
                {
                    case 'а':
                    case 'е':
                    case 'и':
                    case 'о':
                    case 'у':
                    case 'й':
                    case 'ы':
                    case 'я':
                    case 'ь':
                        return len - 1;
                }
            }

            return len;
        }
    }
}