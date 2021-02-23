// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis.Id
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
    /// A <see cref="TokenFilter"/> that applies <see cref="IndonesianStemmer"/> to stem Indonesian words.
    /// </summary>
    public sealed class IndonesianStemFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;
        private readonly IKeywordAttribute keywordAtt;
        private readonly IndonesianStemmer stemmer = new IndonesianStemmer();
        private readonly bool stemDerivational;

        /// <summary>
        /// Calls <see cref="IndonesianStemFilter(TokenStream, bool)">IndonesianStemFilter(input, true)</see>
        /// </summary>
        public IndonesianStemFilter(TokenStream input)
              : this(input, true)
        {
        }

        /// <summary>
        /// Create a new <see cref="IndonesianStemFilter"/>.
        /// <para>
        /// If <paramref name="stemDerivational"/> is false, 
        /// only inflectional suffixes (particles and possessive pronouns) are stemmed.
        /// </para>
        /// </summary>
        public IndonesianStemFilter(TokenStream input, bool stemDerivational)
              : base(input)
        {
            this.stemDerivational = stemDerivational;
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAtt = AddAttribute<IKeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (!keywordAtt.IsKeyword)
                {
                    int newlen = stemmer.Stem(termAtt.Buffer, termAtt.Length, stemDerivational);
                    termAtt.Length = newlen;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}