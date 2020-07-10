using Lucene.Net.Analysis.TokenAttributes;
using System.Text;

namespace Lucene.Net.Analysis.Stempel
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
    /// Transforms the token stream as per the stemming algorithm.
    /// <para>
    /// Note: the input to the stemming filter must already be in lower case, so you
    /// will need to use <see cref="Analysis.Core.LowerCaseFilter"/> or <see cref="Analysis.Core.LowerCaseTokenizer"/> farther down the
    /// <see cref="Tokenizer"/> chain in order for this to work properly!
    /// </para>
    /// </summary>
    public sealed class StempelFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;
        private readonly IKeywordAttribute keywordAtt;
        private readonly StempelStemmer stemmer;
        private readonly int minLength;

        /// <summary>
        /// Minimum length of input words to be processed. Shorter words are returned
        /// unchanged.
        /// </summary>
        public static readonly int DEFAULT_MIN_LENGTH = 3;

        /// <summary>
        /// Create filter using the supplied stemming table.
        /// </summary>
        /// <param name="in">input token stream</param>
        /// <param name="stemmer">stemmer</param>
        public StempelFilter(TokenStream @in, StempelStemmer stemmer)
            : this(@in, stemmer, DEFAULT_MIN_LENGTH)
        {
        }

        /// <summary>
        /// Create filter using the supplied stemming table.
        /// </summary>
        /// <param name="in">input token stream</param>
        /// <param name="stemmer">stemmer</param>
        /// <param name="minLength">For performance reasons words shorter than minLength 
        /// characters are not processed, but simply returned.</param>
        public StempelFilter(TokenStream @in, StempelStemmer stemmer, int minLength)
            : base(@in)
        {
            this.stemmer = stemmer;
            this.minLength = minLength;
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.keywordAtt = AddAttribute<IKeywordAttribute>();
        }

        /// <summary>
        /// Returns the next input <see cref="Token"/>, after being stemmed
        /// </summary>
        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                if (!keywordAtt.IsKeyword && termAtt.Length > minLength)
                {
                    StringBuilder sb = stemmer.Stem(termAtt.ToString());
                    if (sb != null) // if we can't stem it, return unchanged
                        termAtt.SetEmpty().Append(sb);
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
