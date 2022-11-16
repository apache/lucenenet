// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using System;

namespace Lucene.Net.Analysis.De
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
    /// Normalizes German characters according to the heuristics
    /// of the <c>http://snowball.tartarus.org/algorithms/german2/stemmer.html
    /// German2 snowball algorithm</c>.
    /// It allows for the fact that ä, ö and ü are sometimes written as ae, oe and ue.
    /// <para>
    /// <list>
    ///     <item><description> 'ß' is replaced by 'ss'</description></item>
    ///     <item><description> 'ä', 'ö', 'ü' are replaced by 'a', 'o', 'u', respectively.</description></item>
    ///     <item><description> 'ae' and 'oe' are replaced by 'a', and 'o', respectively.</description></item>
    ///     <item><description> 'ue' is replaced by 'u', when not following a vowel or q.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This is useful if you want this normalization without using
    /// the German2 stemmer, or perhaps no stemming at all.
    /// </para>
    /// </summary>
    public sealed class GermanNormalizationFilter : TokenFilter
    {
        // FSM with 3 states:
        private const int N = 0; // ordinary state
        private const int V = 1; // stops 'u' from entering umlaut state
        private const int U = 2; // umlaut state, allows e-deletion

        private readonly ICharTermAttribute termAtt;

        public GermanNormalizationFilter(TokenStream input)
              : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                int state = N;
                char[] buffer = termAtt.Buffer;
                int length = termAtt.Length;
                for (int i = 0; i < length; i++)
                {
                    char c = buffer[i];
                    switch (c)
                    {
                        case 'a':
                        case 'o':
                            state = U;
                            break;
                        case 'u':
                            state = (state == N) ? U : V;
                            break;
                        case 'e':
                            if (state == U)
                            {
                                length = StemmerUtil.Delete(buffer, i--, length);
                            }
                            state = V;
                            break;
                        case 'i':
                        case 'q':
                        case 'y':
                            state = V;
                            break;
                        case 'ä':
                            buffer[i] = 'a';
                            state = V;
                            break;
                        case 'ö':
                            buffer[i] = 'o';
                            state = V;
                            break;
                        case 'ü':
                            buffer[i] = 'u';
                            state = V;
                            break;
                        case 'ß':
                            buffer[i++] = 's';
                            buffer = termAtt.ResizeBuffer(1 + length);
                            if (i < length)
                            {
                                Arrays.Copy(buffer, i, buffer, i + 1, (length - i));
                            }
                            buffer[i] = 's';
                            length++;
                            state = N;
                            break;
                        default:
                            state = N;
                            break;
                    }
                }
                termAtt.Length = length;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}