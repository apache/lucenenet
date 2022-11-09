// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using System;

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

    /// <summary>
    /// A <see cref="TokenFilter"/> that stems french words. 
    /// <para>
    /// The used stemmer can be changed at runtime after the
    /// filter object is created (as long as it is a <see cref="FrenchStemmer"/>).
    /// </para>
    /// <para>
    /// To prevent terms from being stemmed use an instance of
    /// <see cref="Miscellaneous.KeywordMarkerFilter"/> or a custom <see cref="TokenFilter"/> that sets
    /// the <see cref="IKeywordAttribute"/> before this <see cref="TokenStream"/>.
    /// </para> </summary>
    /// <seealso cref="Miscellaneous.KeywordMarkerFilter"/>
    /// @deprecated (3.1) Use <see cref="Snowball.SnowballFilter"/> with 
    /// <see cref="Tartarus.Snowball.Ext.FrenchStemmer"/> instead, which has the
    /// same functionality. This filter will be removed in Lucene 5.0 
    [Obsolete("(3.1) Use SnowballFilter with FrenchStemmer instead, which has the same functionality. This filter will be removed in Lucene 5.0")]
    public sealed class FrenchStemFilter : TokenFilter
    {
        /// <summary>
        /// The actual token in the input stream.
        /// </summary>
        private FrenchStemmer stemmer = new FrenchStemmer();

        private readonly ICharTermAttribute termAtt;
        private readonly IKeywordAttribute keywordAttr;

        public FrenchStemFilter(TokenStream @in)
              : base(@in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAttr = AddAttribute<IKeywordAttribute>();
        }

        /// <returns>  Returns true for the next token in the stream, or false at EOS </returns>
        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                string term = termAtt.ToString();

                // Check the exclusion table
                if (!keywordAttr.IsKeyword)
                {
                    string s = stemmer.Stem(term);
                    // If not stemmed, don't waste the time  adjusting the token.
                    if ((s != null) && !s.Equals(term, StringComparison.Ordinal))
                    {
                        termAtt.SetEmpty().Append(s);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Set a alternative/custom <see cref="FrenchStemmer"/> for this filter.
        /// </summary>
        public FrenchStemmer Stemmer
        {
            get => this.stemmer; // LUCENENET NOTE: Added getter per MSDN guidelines
            set
            {
                if (value != null)
                {
                    this.stemmer = value;
                }
            }
        }
    }
}