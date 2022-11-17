// Lucene version compatibility level 4.8.1
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
    /// A <see cref="TokenFilter"/> that stems Dutch words. 
    /// <para>
    /// It supports a table of words that should
    /// not be stemmed at all. The stemmer used can be changed at runtime after the
    /// filter object is created (as long as it is a <see cref="DutchStemmer"/>).
    /// </para>
    /// <para>
    /// To prevent terms from being stemmed use an instance of
    /// <see cref="Miscellaneous.KeywordMarkerFilter"/> or a custom <see cref="TokenFilter"/> that sets
    /// the <see cref="IKeywordAttribute"/> before this <see cref="TokenStream"/>.
    /// </para> 
    /// </summary>
    /// <seealso cref="Miscellaneous.KeywordMarkerFilter"/>
    /// @deprecated (3.1) Use <see cref="Snowball.SnowballFilter"/> with 
    /// <see cref="Tartarus.Snowball.Ext.DutchStemmer"/> instead, which has the
    /// same functionality. This filter will be removed in Lucene 5.0 
    [Obsolete("(3.1) Use Snowball.SnowballFilter with Tartarus.Snowball.Ext.DutchStemmer instead, which has the same functionality. This filter will be removed in Lucene 5.0")]
    public sealed class DutchStemFilter : TokenFilter
    {
        /// <summary>
        /// The actual token in the input stream.
        /// </summary>
        private DutchStemmer stemmer = new DutchStemmer();

        private readonly ICharTermAttribute termAtt;
        private readonly IKeywordAttribute keywordAttr;

        /// <param name="in"> Input <see cref="TokenStream"/> </param>
        public DutchStemFilter(TokenStream @in)
            : base(@in)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAttr = AddAttribute<IKeywordAttribute>();
        }

        /// <param name="in"> Input <see cref="TokenStream"/> </param>
        /// <param name="stemdictionary"> Dictionary of word stem pairs, that overrule the algorithm </param>
        public DutchStemFilter(TokenStream @in, IDictionary<string, string> stemdictionary) 
            : this(@in)
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
        /// Set a alternative/custom <see cref="DutchStemmer"/> for this filter.
        /// </summary>
        public DutchStemmer Stemmer
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

        /// <summary>
        /// Set dictionary for stemming, this dictionary overrules the algorithm,
        /// so you can correct for a particular unwanted word-stem pair.
        /// </summary>
        public CharArrayDictionary<string> StemDictionary
        {
            get // LUCENENET NOTE: Added getter per MSDN guidelines
            {
                if (stemmer != null)
                {
                    return stemmer.StemDictionary as CharArrayDictionary<string>;
                }
                return null;
            }
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