// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Fr
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
    /// Light Stemmer for French.
    /// <para>
    /// This stemmer implements the "UniNE" algorithm in:
    /// <c>Light Stemming Approaches for the French, Portuguese, German and Hungarian Languages</c>
    /// Jacques Savoy
    /// </para>
    /// </summary>
    public class FrenchLightStemmer
    {
        public virtual int Stem(char[] s, int len)
        {
            if (len > 5 && s[len - 1] == 'x')
            {
                if (s[len - 3] == 'a' && s[len - 2] == 'u' && s[len - 4] != 'e')
                {
                    s[len - 2] = 'l';
                }
                len--;
            }

            if (len > 3 && s[len - 1] == 'x')
            {
                len--;
            }

            if (len > 3 && s[len - 1] == 's')
            {
                len--;
            }

            if (len > 9 && StemmerUtil.EndsWith(s, len, "issement"))
            {
                len -= 6;
                s[len - 1] = 'r';
                return Norm(s, len);
            }

            if (len > 8 && StemmerUtil.EndsWith(s, len, "issant"))
            {
                len -= 4;
                s[len - 1] = 'r';
                return Norm(s, len);
            }

            if (len > 6 && StemmerUtil.EndsWith(s, len, "ement"))
            {
                len -= 4;
                if (len > 3 && StemmerUtil.EndsWith(s, len, "ive"))
                {
                    len--;
                    s[len - 1] = 'f';
                }
                return Norm(s, len);
            }

            if (len > 11 && StemmerUtil.EndsWith(s, len, "ficatrice"))
            {
                len -= 5;
                s[len - 2] = 'e';
                s[len - 1] = 'r';
                return Norm(s, len);
            }

            if (len > 10 && StemmerUtil.EndsWith(s, len, "ficateur"))
            {
                len -= 4;
                s[len - 2] = 'e';
                s[len - 1] = 'r';
                return Norm(s, len);
            }

            if (len > 9 && StemmerUtil.EndsWith(s, len, "catrice"))
            {
                len -= 3;
                s[len - 4] = 'q';
                s[len - 3] = 'u';
                s[len - 2] = 'e';
                //s[len-1] = 'r' <-- unnecessary, already 'r'.
                return Norm(s, len);
            }

            if (len > 8 && StemmerUtil.EndsWith(s, len, "cateur"))
            {
                len -= 2;
                s[len - 4] = 'q';
                s[len - 3] = 'u';
                s[len - 2] = 'e';
                s[len - 1] = 'r';
                return Norm(s, len);
            }

            if (len > 8 && StemmerUtil.EndsWith(s, len, "atrice"))
            {
                len -= 4;
                s[len - 2] = 'e';
                s[len - 1] = 'r';
                return Norm(s, len);
            }

            if (len > 7 && StemmerUtil.EndsWith(s, len, "ateur"))
            {
                len -= 3;
                s[len - 2] = 'e';
                s[len - 1] = 'r';
                return Norm(s, len);
            }

            if (len > 6 && StemmerUtil.EndsWith(s, len, "trice"))
            {
                len--;
                s[len - 3] = 'e';
                s[len - 2] = 'u';
                s[len - 1] = 'r';
            }

            if (len > 5 && StemmerUtil.EndsWith(s, len, "ième"))
            {
                return Norm(s, len - 4);
            }

            if (len > 7 && StemmerUtil.EndsWith(s, len, "teuse"))
            {
                len -= 2;
                s[len - 1] = 'r';
                return Norm(s, len);
            }

            if (len > 6 && StemmerUtil.EndsWith(s, len, "teur"))
            {
                len--;
                s[len - 1] = 'r';
                return Norm(s, len);
            }

            if (len > 5 && StemmerUtil.EndsWith(s, len, "euse"))
            {
                return Norm(s, len - 2);
            }

            if (len > 8 && StemmerUtil.EndsWith(s, len, "ère"))
            {
                len--;
                s[len - 2] = 'e';
                return Norm(s, len);
            }

            if (len > 7 && StemmerUtil.EndsWith(s, len, "ive"))
            {
                len--;
                s[len - 1] = 'f';
                return Norm(s, len);
            }

            if (len > 4 && (StemmerUtil.EndsWith(s, len, "folle") || StemmerUtil.EndsWith(s, len, "molle")))
            {
                len -= 2;
                s[len - 1] = 'u';
                return Norm(s, len);
            }

            if (len > 9 && StemmerUtil.EndsWith(s, len, "nnelle"))
            {
                return Norm(s, len - 5);
            }

            if (len > 9 && StemmerUtil.EndsWith(s, len, "nnel"))
            {
                return Norm(s, len - 3);
            }

            if (len > 4 && StemmerUtil.EndsWith(s, len, "ète"))
            {
                len--;
                s[len - 2] = 'e';
            }

            if (len > 8 && StemmerUtil.EndsWith(s, len, "ique"))
            {
                len -= 4;
            }

            if (len > 8 && StemmerUtil.EndsWith(s, len, "esse"))
            {
                return Norm(s, len - 3);
            }

            if (len > 7 && StemmerUtil.EndsWith(s, len, "inage"))
            {
                return Norm(s, len - 3);
            }

            if (len > 9 && StemmerUtil.EndsWith(s, len, "isation"))
            {
                len -= 7;
                if (len > 5 && StemmerUtil.EndsWith(s, len, "ual"))
                {
                    s[len - 2] = 'e';
                }
                return Norm(s, len);
            }

            if (len > 9 && StemmerUtil.EndsWith(s, len, "isateur"))
            {
                return Norm(s, len - 7);
            }

            if (len > 8 && StemmerUtil.EndsWith(s, len, "ation"))
            {
                return Norm(s, len - 5);
            }

            if (len > 8 && StemmerUtil.EndsWith(s, len, "ition"))
            {
                return Norm(s, len - 5);
            }

            return Norm(s, len);
        }

        private static int Norm(char[] s, int len) // LUCENENET: CA1822: Mark members as static
        {
            if (len > 4)
            {
                for (int i = 0; i < len; i++)
                {
                    switch (s[i])
                    {
                        case 'à':
                        case 'á':
                        case 'â':
                            s[i] = 'a';
                            break;
                        case 'ô':
                            s[i] = 'o';
                            break;
                        case 'è':
                        case 'é':
                        case 'ê':
                            s[i] = 'e';
                            break;
                        case 'ù':
                        case 'û':
                            s[i] = 'u';
                            break;
                        case 'î':
                            s[i] = 'i';
                            break;
                        case 'ç':
                            s[i] = 'c';
                            break;
                    }
                }

                char ch = s[0];
                for (int i = 1; i < len; i++)
                {
                    if (s[i] == ch && char.IsLetter(ch))
                    {
                        len = StemmerUtil.Delete(s, i--, len);
                    }
                    else
                    {
                        ch = s[i];
                    }
                }
            }

            if (len > 4 && StemmerUtil.EndsWith(s, len, "ie"))
            {
                len -= 2;
            }

            if (len > 4)
            {
                if (s[len - 1] == 'r')
                {
                    len--;
                }
                if (s[len - 1] == 'e')
                {
                    len--;
                }
                if (s[len - 1] == 'e')
                {
                    len--;
                }
                if (s[len - 1] == s[len - 2] && char.IsLetter(s[len - 1]))
                {
                    len--;
                }
            }
            return len;
        }
    }
}