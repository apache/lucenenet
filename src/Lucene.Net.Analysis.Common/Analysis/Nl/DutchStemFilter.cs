using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Nl
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
    /// A <seealso cref="TokenFilter"/> that stems Dutch words. 
    /// <para>
    /// It supports a table of words that should
    /// not be stemmed at all. The stemmer used can be changed at runtime after the
    /// filter object is created (as long as it is a <seealso cref="DutchStemmer"/>).
    /// </para>
    /// <para>
    /// To prevent terms from being stemmed use an instance of
    /// <seealso cref="KeywordMarkerFilter"/> or a custom <seealso cref="TokenFilter"/> that sets
    /// the <seealso cref="KeywordAttribute"/> before this <seealso cref="TokenStream"/>.
    /// </para> </summary>
    /// <seealso cref= KeywordMarkerFilter </seealso>
    /// @deprecated (3.1) Use <seealso cref="SnowballFilter"/> with 
    /// <seealso cref="org.tartarus.snowball.ext.DutchStemmer"/> instead, which has the
    /// same functionality. This filter will be removed in Lucene 5.0 
    [Obsolete("(3.1) Use SnowballFilter with DutchStemmer instead, which has the same functionality. This filter will be removed in Lucene 5.0")]
    public sealed class DutchStemFilter : TokenFilter
    {
        /// <summary>
        /// The actual token in the input stream.
        /// </summary>
        private DutchStemmer stemmer = new DutchStemmer();

        private readonly ICharTermAttribute termAtt;
        private readonly IKeywordAttribute keywordAttr;

        public DutchStemFilter(TokenStream _in)
              : base(_in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAttr = AddAttribute<IKeywordAttribute>();
        }

        /// <param name="stemdictionary"> Dictionary of word stem pairs, that overrule the algorithm </param>
        public DutchStemFilter(TokenStream _in, IDictionary<string, string> stemdictionary) : this(_in)
        {
            stemmer.StemDictionary = stemdictionary;
        }

        /// <summary>
        /// Returns the next token in the stream, or null at EOS
        /// </summary>
        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                string term = termAtt.ToString();

                // Check the exclusion table.
                if (!keywordAttr.IsKeyword)
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

        /// <summary>
        /// Set a alternative/custom <seealso cref="DutchStemmer"/> for this filter.
        /// </summary>
        public DutchStemmer Stemmer
        {
            set
            {
                if (value != null)
                {
                    this.stemmer = value;
                }
            }
        }

        /// <summary>
        /// Set dictionary for stemming, this dictionary overrules the algorithm,
        /// so you can correct for a particular unwanted word-stem pair.
        /// </summary>
        public CharArrayMap<string> StemDictionary
        {
            set
            {
                if (stemmer != null)
                {
                    stemmer.StemDictionary = value;
                }
            }
        }
    }
}