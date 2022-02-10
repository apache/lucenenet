using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Highlight
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
    /// <see cref="IScorer"/> implementation which scores text fragments by the number of
    /// unique query terms found. This class uses the <see cref="QueryTermExtractor"/>
    /// class to process determine the query terms and their boosts to be used.
    /// </summary>
    // TODO: provide option to boost score of fragments near beginning of document
    // based on fragment.getFragNum()
    public class QueryTermScorer : IScorer
    {
        //private TextFragment currentTextFragment = null; // LUCENENET: Not used
        private ISet<string> uniqueTermsInFragment;

        private float totalScore = 0;
        private readonly float maxTermWeight = 0;
        private readonly IDictionary<string, WeightedTerm> termsToFind;

        private ICharTermAttribute termAtt;

        /// <param name="query">
        /// a Lucene query (ideally rewritten using <see cref="Query.Rewrite(IndexReader)"/> before
        /// being passed to this class and the searcher)
        /// </param>
        public QueryTermScorer(Query query)
            : this(QueryTermExtractor.GetTerms(query))
        {
        }

        /// <param name="query">
        /// a Lucene query (ideally rewritten using <see cref="Query.Rewrite(IndexReader)"/> before
        /// being passed to this class and the searcher)
        /// </param>
        /// <param name="fieldName">the Field name which is used to match Query terms</param>
        public QueryTermScorer(Query query, string fieldName)
            : this(QueryTermExtractor.GetTerms(query, false, fieldName))
        {
        }

        /// <param name="query">
        /// a Lucene query (ideally rewritten using <see cref="Query.Rewrite(IndexReader)"/> before
        /// being passed to this class and the searcher)
        /// </param>
        /// <param name="reader">
        /// used to compute IDF which can be used to a) score selected
        /// fragments better b) use graded highlights eg set font color
        /// intensity
        /// </param>
        /// <param name="fieldName">
        /// the field on which Inverse Document Frequency (IDF)
        /// calculations are based
        /// </param>
        public QueryTermScorer(Query query, IndexReader reader, string fieldName)
            : this(QueryTermExtractor.GetIdfWeightedTerms(query, reader, fieldName))
        {
        }

        public QueryTermScorer(WeightedTerm[] weightedTerms)
        {
            termsToFind = new Dictionary<string, WeightedTerm>();
            for (int i = 0; i < weightedTerms.Length; i++)
            {
                if (!termsToFind.TryGetValue(weightedTerms[i].Term, out WeightedTerm existingTerm)
                    || (existingTerm is null)
                    || (existingTerm.Weight < weightedTerms[i].Weight))
                {
                    // if a term is defined more than once, always use the highest scoring
                    // Weight
                    termsToFind[weightedTerms[i].Term] = weightedTerms[i];
                    maxTermWeight = Math.Max(maxTermWeight, weightedTerms[i].Weight);
                }
            }
        }

        /// <summary>
        /// <seealso cref="IScorer.Init(TokenStream)"/>
        /// </summary>
        public virtual TokenStream Init(TokenStream tokenStream)
        {
            termAtt = tokenStream.AddAttribute<ICharTermAttribute>();
            return null;
        }

        public virtual void StartFragment(TextFragment newFragment)
        {
            uniqueTermsInFragment = new JCG.HashSet<string>();
            //currentTextFragment = newFragment; // LUCENENET: Not used
            totalScore = 0;
        }

        /// <summary>
        /// <seealso cref="IScorer.GetTokenScore()"/>
        /// </summary>
        public virtual float GetTokenScore()
        {
            string termText = termAtt.ToString();

            if (!termsToFind.TryGetValue(termText, out WeightedTerm queryTerm) || queryTerm is null)
            {
                // not a query term - return
                return 0;
            }
            // found a query term - is it unique in this doc?
            if (!uniqueTermsInFragment.Contains(termText))
            {
                totalScore += queryTerm.Weight;
                uniqueTermsInFragment.Add(termText);
            }
            return queryTerm.Weight;
        }

        /// <summary>
        /// <seealso cref="IScorer.FragmentScore"/>
        /// </summary>
        public virtual float FragmentScore => totalScore;

        public virtual void AllFragmentsProcessed()
        {
            // this class has no special operations to perform at end of processing
        }

        /// <summary>
        /// The highest weighted term (useful for passing to <see cref="GradientFormatter"/> 
        /// to set top end of coloring scale.
        /// </summary>
        public virtual float MaxTermWeight => maxTermWeight;
    }
}