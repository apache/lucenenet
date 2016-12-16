using Lucene.Net.Analysis.TokenAttributes;
using System.Collections.Generic;

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
    /// A <seealso cref="TokenFilter"/> that applies <seealso cref="BrazilianStemmer"/>.
    /// <para>
    /// To prevent terms from being stemmed use an instance of
    /// <seealso cref="SetKeywordMarkerFilter"/> or a custom <seealso cref="TokenFilter"/> that sets
    /// the <seealso cref="KeywordAttribute"/> before this <seealso cref="TokenStream"/>.
    /// </para> </summary>
    /// <seealso cref= SetKeywordMarkerFilter
    ///  </seealso>
    public sealed class BrazilianStemFilter : TokenFilter
    {

        /// <summary>
        /// <seealso cref="BrazilianStemmer"/> in use by this filter.
        /// </summary>
        private BrazilianStemmer stemmer = new BrazilianStemmer();
        private HashSet<string> exclusions = null; // LUCENENET TODO: This is odd. No way to set it at all, so it cannot possibly have any values.
        private readonly ICharTermAttribute termAtt;
        private readonly IKeywordAttribute keywordAttr;

        /// <summary>
        /// Creates a new BrazilianStemFilter 
        /// </summary>
        /// <param name="in"> the source <seealso cref="TokenStream"/>  </param>
        public BrazilianStemFilter(TokenStream @in)
              : base(@in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAttr = AddAttribute<IKeywordAttribute>();
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                string term = termAtt.ToString();
                // Check the exclusion table.
                if (!keywordAttr.Keyword && (exclusions == null || !exclusions.Contains(term)))
                {
                    string s = stemmer.Stem(term);
                    // If not stemmed, don't waste the time adjusting the token.
                    if ((s != null) && !s.Equals(term))
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