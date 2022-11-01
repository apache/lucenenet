// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Pt
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
    /// Light Stemmer for Portuguese
    /// <para>
    /// This stemmer implements the "UniNE" algorithm in:
    /// <c>Light Stemming Approaches for the French, Portuguese, German and Hungarian Languages</c>
    /// Jacques Savoy
    /// </para>
    /// </summary>
    public class PortugueseLightStemmer
    {
        public virtual int Stem(char[] s, int len)
        {
            if (len < 4)
            {
                return len;
            }

            len = RemoveSuffix(s, len);

            if (len > 3 && s[len - 1] == 'a')
            {
                len = NormFeminine(s, len);
            }

            if (len > 4)
            {
                switch (s[len - 1])
                {
                    case 'e':
                    case 'a':
                    case 'o':
                        len--;
                        break;
                }
            }

            for (int i = 0; i < len; i++)
            {
                switch (s[i])
                {
                    case 'à':
                    case 'á':
                    case 'â':
                    case 'ä':
                    case 'ã':
                        s[i] = 'a';
                        break;
                    case 'ò':
                    case 'ó':
                    case 'ô':
                    case 'ö':
                    case 'õ':
                        s[i] = 'o';
                        break;
                    case 'è':
                    case 'é':
                    case 'ê':
                    case 'ë':
                        s[i] = 'e';
                        break;
                    case 'ù':
                    case 'ú':
                    case 'û':
                    case 'ü':
                        s[i] = 'u';
                        break;
                    case 'ì':
                    case 'í':
                    case 'î':
                    case 'ï':
                        s[i] = 'i';
                        break;
                    case 'ç':
                        s[i] = 'c';
                        break;
                }
            }

            return len;
        }

        private static int RemoveSuffix(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 4 && StemmerUtil.EndsWith(s, len, "es"))
            {
                switch (s[len - 3])
                {
                    case 'r':
                    case 's':
                    case 'l':
                    case 'z':
                        return len - 2;
                }
            }

            if (len > 3 && StemmerUtil.EndsWith(s, len, "ns"))
            {
                s[len - 2] = 'm';
                return len - 1;
            }

            if (len > 4 && (StemmerUtil.EndsWith(s, len, "eis") || StemmerUtil.EndsWith(s, len, "éis")))
            {
                s[len - 3] = 'e';
                s[len - 2] = 'l';
                return len - 1;
            }

            if (len > 4 && StemmerUtil.EndsWith(s, len, "ais"))
            {
                s[len - 2] = 'l';
                return len - 1;
            }

            if (len > 4 && StemmerUtil.EndsWith(s, len, "óis"))
            {
                s[len - 3] = 'o';
                s[len - 2] = 'l';
                return len - 1;
            }

            if (len > 4 && StemmerUtil.EndsWith(s, len, "is"))
            {
                s[len - 1] = 'l';
                return len;
            }

            if (len > 3 && (StemmerUtil.EndsWith(s, len, "ões") || StemmerUtil.EndsWith(s, len, "ães")))
            {
                len--;
                s[len - 2] = 'ã';
                s[len - 1] = 'o';
                return len;
            }

            if (len > 6 && StemmerUtil.EndsWith(s, len, "mente"))
            {
                return len - 5;
            }

            if (len > 3 && s[len - 1] == 's')
            {
                return len - 1;
            }
            return len;
        }

        private static int NormFeminine(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 7 && (StemmerUtil.EndsWith(s, len, "inha") || StemmerUtil.EndsWith(s, len, "iaca") || StemmerUtil.EndsWith(s, len, "eira")))
            {
                s[len - 1] = 'o';
                return len;
            }

            if (len > 6)
            {
                if (StemmerUtil.EndsWith(s, len, "osa") || StemmerUtil.EndsWith(s, len, "ica") || StemmerUtil.EndsWith(s, len, "ida") || StemmerUtil.EndsWith(s, len, "ada") || StemmerUtil.EndsWith(s, len, "iva") || StemmerUtil.EndsWith(s, len, "ama"))
                {
                    s[len - 1] = 'o';
                    return len;
                }

                if (StemmerUtil.EndsWith(s, len, "ona"))
                {
                    s[len - 3] = 'ã';
                    s[len - 2] = 'o';
                    return len - 1;
                }

                if (StemmerUtil.EndsWith(s, len, "ora"))
                {
                    return len - 1;
                }

                if (StemmerUtil.EndsWith(s, len, "esa"))
                {
                    s[len - 3] = 'ê';
                    return len - 1;
                }

                if (StemmerUtil.EndsWith(s, len, "na"))
                {
                    s[len - 1] = 'o';
                    return len;
                }
            }
            return len;
        }
    }
}