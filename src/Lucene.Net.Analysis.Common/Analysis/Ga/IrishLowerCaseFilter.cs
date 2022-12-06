// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using System.Globalization;

namespace Lucene.Net.Analysis.Ga
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
    /// Normalises token text to lower case, handling t-prothesis
    /// and n-eclipsis (i.e., that 'nAthair' should become 'n-athair')
    /// </summary>
    public sealed class IrishLowerCaseFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;

        private static readonly CultureInfo culture = new CultureInfo("ga"); // LUCENENET specific - use Irish culture when lowercasing.

        /// <summary>
        /// Create an <see cref="IrishLowerCaseFilter"/> that normalises Irish token text.
        /// </summary>
        public IrishLowerCaseFilter(TokenStream @in)
            : base(@in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                char[] chArray = termAtt.Buffer;
                int chLen = termAtt.Length;
                int idx = 0;

                if (chLen > 1 && (chArray[0] == 'n' || chArray[0] == 't') && IsUpperVowel(chArray[1]))
                {
                    chArray = termAtt.ResizeBuffer(chLen + 1);
                    for (int i = chLen; i > 1; i--)
                    {
                        chArray[i] = chArray[i - 1];
                    }
                    chArray[1] = '-';
                    termAtt.Length = chLen + 1;
                    idx = 2;
                    chLen = chLen + 1;
                }

                // LUCENENET: Reduce allocations by using the stack and spans
                var source = new ReadOnlySpan<char>(chArray, idx, chLen);
                var destination = chArray.AsSpan(idx, chLen);
                var spare = chLen * sizeof(char) <= Constants.MaxStackByteLimit ? stackalloc char[chLen] : new char[chLen];
                source.ToLower(spare, culture); // LUCENENET specific - use Irish culture when lowercasing
                spare.CopyTo(destination);

                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsUpperVowel(int v) // LUCENENET: CA1822: Mark members as static
        {
            switch (v)
            {
                case 'A':
                case 'E':
                case 'I':
                case 'O':
                case 'U':
                // vowels with acute accent (fada)
                case '\u00c1':
                case '\u00c9':
                case '\u00cd':
                case '\u00d3':
                case '\u00da':
                    return true;
                default:
                    return false;
            }
        }
    }
}