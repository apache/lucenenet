// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Standard
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
    /// Normalizes tokens extracted with <see cref="StandardTokenizer"/>.
    /// </summary>
    public class StandardFilter : TokenFilter
    {
        private readonly LuceneVersion matchVersion;

        public StandardFilter(LuceneVersion matchVersion, TokenStream @in)
            : base(@in)
        {
            this.matchVersion = matchVersion;
            typeAtt = AddAttribute<ITypeAttribute>();
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        private static readonly string APOSTROPHE_TYPE = ClassicTokenizer.TOKEN_TYPES[ClassicTokenizer.APOSTROPHE];
        private static readonly string ACRONYM_TYPE = ClassicTokenizer.TOKEN_TYPES[ClassicTokenizer.ACRONYM];

        // this filters uses attribute type
        private readonly ITypeAttribute typeAtt;
        private readonly ICharTermAttribute termAtt;

        public override sealed bool IncrementToken()
        {
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                return m_input.IncrementToken(); // TODO: add some niceties for the new grammar
            }
            else
            {
                return IncrementTokenClassic();
            }
        }

        public bool IncrementTokenClassic()
        {
            if (!m_input.IncrementToken())
            {
                return false;
            }

            char[] buffer = termAtt.Buffer;
            int bufferLength = termAtt.Length;
            string type = typeAtt.Type;

            if (type == APOSTROPHE_TYPE && bufferLength >= 2 && buffer[bufferLength - 2] == '\'' && (buffer[bufferLength - 1] == 's' || buffer[bufferLength - 1] == 'S')) // remove 's
            {
                // Strip last 2 characters off
                termAtt.Length = bufferLength - 2;
            } // remove dots
            else if (type == ACRONYM_TYPE)
            {
                int upto = 0;
                for (int i = 0; i < bufferLength; i++)
                {
                    char c = buffer[i];
                    if (c != '.')
                    {
                        buffer[upto++] = c;
                    }
                }
                termAtt.Length = upto;
            }

            return true;
        }
    }
}