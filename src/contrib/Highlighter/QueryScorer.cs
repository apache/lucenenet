/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Highlight;
using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Highlight
{

    ///<summary>
    /// <see cref="IScorer"/> implementation which scores text fragments by the number of
    /// unique query terms found. This class converts appropriate <see cref="Query"/>s to
    /// <see cref="SpanQuery"/>s and attempts to score only those terms that participated in
    /// generating the 'hit' on the document.
    /// </summary>
    public class QueryScorer : IScorer
    {
        private float totalScore;
        private ISet<String> foundTerms;
        private IDictionary<String, WeightedSpanTerm> fieldWeightedSpanTerms;
        private float maxTermWeight;
        private int position = -1;
        private String defaultField;
        private TermAttribute termAtt;
        private PositionIncrementAttribute posIncAtt;
        private bool expandMultiTermQuery = true;
        private Query query;
        private String field;
        private IndexReader reader;
        private bool skipInitExtractor;
        private bool wrapToCaching = true;

        /**
         * @param query Query to use for highlighting
         */

        public QueryScorer(Query query)
        {
            init(query, null, null, true);
        }

        /**
         * @param query Query to use for highlighting
         * @param field Field to highlight - pass null to ignore fields
         */

        public QueryScorer(Query query, String field)
        {
            init(query, field, null, true);
        }

        /**
         * @param query Query to use for highlighting
         * @param field Field to highlight - pass null to ignore fields
         * @param reader {@link IndexReader} to use for quasi tf/idf scoring
         */

        public QueryScorer(Query query, IndexReader reader, String field)
        {
            init(query, field, reader, true);
        }


        /**
         * @param query to use for highlighting
         * @param reader {@link IndexReader} to use for quasi tf/idf scoring
         * @param field to highlight - pass null to ignore fields
         * @param defaultField
         */

        public QueryScorer(Query query, IndexReader reader, String field, String defaultField)
        {
            this.defaultField = StringHelper.Intern(defaultField);
            init(query, field, reader, true);
        }

        /**
         * @param defaultField - The default field for queries with the field name unspecified
         */

        public QueryScorer(Query query, String field, String defaultField)
        {
            this.defaultField = StringHelper.Intern(defaultField);
            init(query, field, null, true);
        }

        /**
         * @param weightedTerms an array of pre-created {@link WeightedSpanTerm}s
         */

        public QueryScorer(WeightedSpanTerm[] weightedTerms)
        {
            this.fieldWeightedSpanTerms = new HashMap<String, WeightedSpanTerm>(weightedTerms.Length);

            for (int i = 0; i < weightedTerms.Length; i++)
            {
                WeightedSpanTerm existingTerm = fieldWeightedSpanTerms[weightedTerms[i].term];

                if ((existingTerm == null) ||
                    (existingTerm.weight < weightedTerms[i].weight))
                {
                    // if a term is defined more than once, always use the highest
                    // scoring weight
                    fieldWeightedSpanTerms[weightedTerms[i].term] = weightedTerms[i];
                    maxTermWeight = Math.Max(maxTermWeight, weightedTerms[i].GetWeight());
                }
            }
            skipInitExtractor = true;
        }

        /*
         * (non-Javadoc)
         *
         * @see org.apache.lucene.search.highlight.Scorer#getFragmentScore()
         */

        public float getFragmentScore()
        {
            return totalScore;
        }

        /**
         *
         * @return The highest weighted term (useful for passing to
         *         GradientFormatter to set top end of coloring scale).
         */

        public float getMaxTermWeight()
        {
            return maxTermWeight;
        }

        /*
         * (non-Javadoc)
         *
         * @see org.apache.lucene.search.highlight.Scorer#getTokenScore(org.apache.lucene.analysis.Token,
         *      int)
         */

        public float getTokenScore()
        {
            position += posIncAtt.PositionIncrement;
            String termText = termAtt.Term();

            WeightedSpanTerm weightedSpanTerm;

            if ((weightedSpanTerm = fieldWeightedSpanTerms[termText]) == null)
            {
                return 0;
            }

            if (weightedSpanTerm.isPositionSensitive() &&
                !weightedSpanTerm.checkPosition(position))
            {
                return 0;
            }

            float score = weightedSpanTerm.GetWeight();

            // found a query term - is it unique in this doc?
            if (!foundTerms.Contains(termText))
            {
                totalScore += score;
                foundTerms.Add(termText);
            }

            return score;
        }

        /* (non-Javadoc)
         * @see org.apache.lucene.search.highlight.Scorer#init(org.apache.lucene.analysis.TokenStream)
         */

        public TokenStream init(TokenStream tokenStream)
        {
            position = -1;
            termAtt = tokenStream.AddAttribute<TermAttribute>();
            posIncAtt = tokenStream.AddAttribute<PositionIncrementAttribute>();
            if (!skipInitExtractor)
            {
                if (fieldWeightedSpanTerms != null)
                {
                    fieldWeightedSpanTerms.Clear();
                }
                return initExtractor(tokenStream);
            }
            return null;
        }

        /**
         * Retrieve the {@link WeightedSpanTerm} for the specified token. Useful for passing
         * Span information to a {@link Fragmenter}.
         *
         * @param token to get {@link WeightedSpanTerm} for
         * @return WeightedSpanTerm for token
         */

        public WeightedSpanTerm getWeightedSpanTerm(String token)
        {
            return fieldWeightedSpanTerms[token];
        }

        /**
         */

        private void init(Query query, String field, IndexReader reader, bool expandMultiTermQuery)
        {
            this.reader = reader;
            this.expandMultiTermQuery = expandMultiTermQuery;
            this.query = query;
            this.field = field;
        }

        private TokenStream initExtractor(TokenStream tokenStream)
        {
            WeightedSpanTermExtractor qse = defaultField == null
                                                ? new WeightedSpanTermExtractor()
                                                : new WeightedSpanTermExtractor(defaultField);

            qse.setExpandMultiTermQuery(expandMultiTermQuery);
            qse.setWrapIfNotCachingTokenFilter(wrapToCaching);
            if (reader == null)
            {
                this.fieldWeightedSpanTerms = qse.getWeightedSpanTerms(query,
                                                                       tokenStream, field);
            }
            else
            {
                this.fieldWeightedSpanTerms = qse.getWeightedSpanTermsWithScores(query,
                                                                                 tokenStream, field, reader);
            }
            if (qse.isCachedTokenStream())
            {
                return qse.getTokenStream();
            }

            return null;
        }

        /*
         * (non-Javadoc)
         *
         * @see org.apache.lucene.search.highlight.Scorer#startFragment(org.apache.lucene.search.highlight.TextFragment)
         */

        public void startFragment(TextFragment newFragment)
        {
            foundTerms = new HashSet<String>();
            totalScore = 0;
        }

        /**
         * @return true if multi-term queries should be expanded
         */

        public bool isExpandMultiTermQuery()
        {
            return expandMultiTermQuery;
        }

        /**
         * Controls whether or not multi-term queries are expanded
         * against a {@link MemoryIndex} {@link IndexReader}.
         * 
         * @param expandMultiTermQuery true if multi-term queries should be expanded
         */

        public void setExpandMultiTermQuery(bool expandMultiTermQuery)
        {
            this.expandMultiTermQuery = expandMultiTermQuery;
        }

        /**
         * By default, {@link TokenStream}s that are not of the type
         * {@link CachingTokenFilter} are wrapped in a {@link CachingTokenFilter} to
         * ensure an efficient reset - if you are already using a different caching
         * {@link TokenStream} impl and you don't want it to be wrapped, set this to
         * false.
         * 
         * @param wrap
         */

        public void setWrapIfNotCachingTokenFilter(bool wrap)
        {
            this.wrapToCaching = wrap;
        }
    }
}