// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using System;

namespace Lucene.Net.Analysis.Br
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
    /// A <see cref="TokenFilter"/> that applies <see cref="BrazilianStemmer"/>.
    /// <para>
    /// To prevent terms from being stemmed use an instance of
    /// <see cref="Miscellaneous.SetKeywordMarkerFilter"/> or a custom <see cref="TokenFilter"/> that sets
    /// the <see cref="IKeywordAttribute"/> before this <see cref="TokenStream"/>.
    /// </para>
    /// </summary>
    /// <seealso cref="Miscellaneous.SetKeywordMarkerFilter"/>
    public sealed class BrazilianStemFilter : TokenFilter
    {
        /// <summary>
        /// <see cref="BrazilianStemmer"/> in use by this filter.
        /// </summary>
        private readonly BrazilianStemmer stemmer = new BrazilianStemmer();
        //private JCG.HashSet<string> exclusions = null; // LUCENENET specific: Removed unusd variable
        private readonly ICharTermAttribute termAtt;
        private readonly IKeywordAttribute keywordAttr;

        /// <summary>
        /// Creates a new <see cref="BrazilianStemFilter"/> 
        /// </summary>
        /// <param name="in"> the source <see cref="TokenStream"/>  </param>
        public BrazilianStemFilter(TokenStream @in)
              : base(@in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAttr = AddAttribute<IKeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                string term = termAtt.ToString();
                // Check the exclusion table.
                if (!keywordAttr.IsKeyword /*&& (exclusions is null || !exclusions.Contains(term))*/) // LUCENENET specific - removed unused variable "exclusions"
                {
                    string s = stemmer.Stem(term);
                    // If not stemmed, don't waste the time adjusting the token.
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
    }
}