/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.De
{
    /// <summary>
    /// A filter that stems German words. It supports a table of words that should
    /// not be stemmed at all. The stemmer used can be changed at runtime after the
    /// filter object is created (as long as it is a GermanStemmer).
    /// </summary>
    public sealed class GermanStemFilter : TokenFilter
    {
        /// <summary>
        /// The actual token in the input stream.
        /// </summary>
        private GermanStemmer stemmer = null;
        private ISet<string> exclusionSet = null;

        private TermAttribute termAtt;

        public GermanStemFilter(TokenStream _in)
            : this(_in, false)
        { }

        public GermanStemFilter(TokenStream _in, bool useDin2Stemmer)
            : this(_in, null, useDin2Stemmer)
        { }

        /// <summary>
        /// Builds a GermanStemFilter that uses an exclusiontable. 
        /// </summary>
        /// <param name="_in"></param>
        /// <param name="exclusiontable"></param>
        public GermanStemFilter(TokenStream _in, ISet<string> exclusiontable)
            : this(_in, exclusiontable, false)
        { }

        /// <summary>
        /// Builds a GermanStemFilter that uses an exclusiontable. 
        /// </summary>
        /// <param name="_in"></param>
        /// <param name="exclusiontable"></param>
        /// <param name="useDin2Stemmer">Specifies where to use the DIN-5007-2 (names) 
        /// stemmer instead of the default DIN-5007-1 (words) stemmer</param>
        public GermanStemFilter(TokenStream _in, ISet<string> exclusiontable, bool useDin2Stemmer)
            : base(_in)
        {
            exclusionSet = exclusiontable;
            stemmer = useDin2Stemmer ? new GermanStemmerDIN2() : new GermanStemmer();
            termAtt = AddAttribute<TermAttribute>();
        }

        /// <returns>
        /// Returns true for next token in the stream, or false at EOS
        /// </returns>
        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                String term = termAtt.Term();
                // Check the exclusion table.
                if (exclusionSet == null || !exclusionSet.Contains(term))
                {
                    String s = stemmer.Stem(term);
                    // If not stemmed, don't waste the time adjusting the token.
                    if ((s != null) && !s.Equals(term))
                        termAtt.SetTermBuffer(s);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Set a alternative/custom GermanStemmer for this filter. 
        /// </summary>
        /// <param name="stemmer"></param>
        public void SetStemmer(GermanStemmer stemmer)
        {
            if (stemmer != null)
            {
                this.stemmer = stemmer;
            }
        }

        /// <summary>
        /// Set an alternative exclusion list for this filter. 
        /// </summary>
        /// <param name="exclusiontable"></param>
        public void SetExclusionTable(ISet<string> exclusiontable)
        {
            exclusionSet = exclusiontable;
        }
    }
}