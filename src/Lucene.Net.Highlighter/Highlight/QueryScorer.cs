using J2N.Text;
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

    ///<summary>
    /// <see cref="IScorer"/> implementation which scores text fragments by the number of
    /// unique query terms found. This class converts appropriate <see cref="Query"/>s to
    /// <see cref="Search.Spans.SpanQuery"/>s and attempts to score only those terms that participated in
    /// generating the 'hit' on the document.
    /// </summary>
    public class QueryScorer : IScorer
    {
        private float totalScore;
        private ISet<string> foundTerms;
        private IDictionary<string, WeightedSpanTerm> fieldWeightedSpanTerms;
        private readonly float maxTermWeight;
        private int position = -1;
        private readonly string defaultField;
        private ICharTermAttribute termAtt;
        private IPositionIncrementAttribute posIncAtt;
        private bool expandMultiTermQuery = true;
        private Query query;
        private string field;
        private IndexReader reader;
        private readonly bool skipInitExtractor;
        private bool wrapToCaching = true;
        private int maxCharsToAnalyze;

        /// <summary>
        /// Constructs a new <see cref="QueryScorer"/> instance
        /// </summary>
        /// <param name="query"><see cref="Query"/> to use for highlighting</param>
        public QueryScorer(Query query)
        {
            Init(query, null, null, true);
        }

        /// <summary>
        /// Constructs a new <see cref="QueryScorer"/> instance
        /// </summary>
        /// <param name="query"><see cref="Query"/> to use for highlighting</param>
        /// <param name="field">Field to highlight - pass null to ignore fields</param>
        public QueryScorer(Query query, string field)
        {
            Init(query, field, null, true);
        }

        /// <summary>
        /// Constructs a new <see cref="QueryScorer"/> instance
        /// </summary>
        /// <param name="query"><see cref="Query"/> to use for highlighting</param>
        /// <param name="reader"><see cref="IndexReader"/> to use for quasi tf/idf scoring</param>
        /// <param name="field">Field to highlight - pass null to ignore fields</param>
        public QueryScorer(Query query, IndexReader reader, string field)
        {
            Init(query, field, reader, true);
        }

        /// <summary>
        /// Constructs a new <see cref="QueryScorer"/> instance
        /// </summary>
        /// <param name="query"><see cref="Query"/> to use for highlighting</param>
        /// <param name="reader"><see cref="IndexReader"/> to use for quasi tf/idf scoring</param>
        /// <param name="field">Field to highlight - pass null to ignore fields</param>
        /// <param name="defaultField">The default field for queries with the field name unspecified</param>
        public QueryScorer(Query query, IndexReader reader, string field, string defaultField)
        {
            this.defaultField = defaultField.Intern();
            Init(query, field, reader, true);
        }

        /// <summary>
        /// Constructs a new <see cref="QueryScorer"/> instance
        /// </summary>
        /// <param name="query"><see cref="Query"/> to use for highlighting</param>
        /// <param name="field">Field to highlight - pass null to ignore fields</param>
        /// <param name="defaultField">The default field for queries with the field name unspecified</param>
        public QueryScorer(Query query, string field, string defaultField)
        {
            this.defaultField = defaultField.Intern();
            Init(query, field, null, true);
        }

        /// <summary>
        /// Constructs a new <see cref="QueryScorer"/> instance
        /// </summary>
        /// <param name="weightedTerms">an array of pre-created <see cref="WeightedSpanTerm"/>s</param>
        public QueryScorer(WeightedSpanTerm[] weightedTerms)
        {
            this.fieldWeightedSpanTerms = new JCG.Dictionary<string, WeightedSpanTerm>(weightedTerms.Length);

            foreach (WeightedSpanTerm t in weightedTerms)
            {
                if (!fieldWeightedSpanTerms.TryGetValue(t.Term, out WeightedSpanTerm existingTerm) ||
                    (existingTerm is null) ||
                    (existingTerm.Weight < t.Weight))
                {
                    // if a term is defined more than once, always use the highest
                    // scoring Weight
                    fieldWeightedSpanTerms[t.Term] = t;
                    maxTermWeight = Math.Max(maxTermWeight, t.Weight);
                }
            }
            skipInitExtractor = true;
        }

        /// <seealso cref="IScorer.FragmentScore"/>
        public virtual float FragmentScore => totalScore;

        /// <summary>
        /// The highest weighted term (useful for passing to <see cref="GradientFormatter"/> to set top end of coloring scale).
        /// </summary>
        public virtual float MaxTermWeight => maxTermWeight;

        /// <seealso cref="IScorer.GetTokenScore()"/>
        public virtual float GetTokenScore()
        {
            position += posIncAtt.PositionIncrement;
            string termText = termAtt.ToString();

            if (!fieldWeightedSpanTerms.TryGetValue(termText, out WeightedSpanTerm weightedSpanTerm) || weightedSpanTerm is null)
            {
                return 0;
            }

            if (weightedSpanTerm.IsPositionSensitive &&
                !weightedSpanTerm.CheckPosition(position))
            {
                return 0;
            }

            float score = weightedSpanTerm.Weight;

            // found a query term - is it unique in this doc?
            if (!foundTerms.Contains(termText))
            {
                totalScore += score;
                foundTerms.Add(termText);
            }

            return score;
        }

        /// <seealso cref="IScorer.Init"/>
        public virtual TokenStream Init(TokenStream tokenStream)
        {
            position = -1;
            termAtt = tokenStream.AddAttribute<ICharTermAttribute>();
            posIncAtt = tokenStream.AddAttribute<IPositionIncrementAttribute>();
            if (!skipInitExtractor)
            {
                fieldWeightedSpanTerms?.Clear();
                return InitExtractor(tokenStream);
            }
            return null;
        }

        /// <summary>
        /// Retrieve the <see cref="WeightedSpanTerm"/> for the specified token. Useful for passing
        /// Span information to a <see cref="IFragmenter"/>.
        /// </summary>
        /// <param name="token">token to get <see cref="WeightedSpanTerm"/> for</param>
        /// <returns><see cref="WeightedSpanTerm"/> for token</returns>
        public virtual WeightedSpanTerm GetWeightedSpanTerm(string token)
        {
            fieldWeightedSpanTerms.TryGetValue(token, out WeightedSpanTerm result);
            return result;
        }

        private void Init(Query query, string field, IndexReader reader, bool expandMultiTermQuery)
        {
            this.reader = reader;
            this.expandMultiTermQuery = expandMultiTermQuery;
            this.query = query;
            this.field = field;
        }

        private TokenStream InitExtractor(TokenStream tokenStream)
        {
            WeightedSpanTermExtractor qse = NewTermExtractor(defaultField);

            qse.SetMaxDocCharsToAnalyze(maxCharsToAnalyze);
            qse.ExpandMultiTermQuery = expandMultiTermQuery;
            qse.SetWrapIfNotCachingTokenFilter(wrapToCaching);
            if (reader is null)
            {
                this.fieldWeightedSpanTerms = qse.GetWeightedSpanTerms(query,
                                                                       tokenStream, field);
            }
            else
            {
                this.fieldWeightedSpanTerms = qse.GetWeightedSpanTermsWithScores(query,
                                                             tokenStream, field, reader);
            }
            if (qse.IsCachedTokenStream)
            {
                return qse.TokenStream;
            }

            return null;
        }

        protected virtual WeightedSpanTermExtractor NewTermExtractor(string defaultField)
        {
            return defaultField is null ? new WeightedSpanTermExtractor()
                : new WeightedSpanTermExtractor(defaultField);
        }

        /// <seealso cref="IScorer.StartFragment"/>
        public virtual void StartFragment(TextFragment newFragment)
        {
            foundTerms = new JCG.HashSet<string>();
            totalScore = 0;
        }

        /// <summary>
        /// Controls whether or not multi-term queries are expanded
        /// against a <see cref="Index.Memory.MemoryIndex"/> <see cref="IndexReader"/>.
        /// <c>true</c> if multi-term queries should be expanded
        /// </summary>
        public virtual bool ExpandMultiTermQuery
        {
            get => expandMultiTermQuery;
            set => this.expandMultiTermQuery = value;
        }

        /// <summary>
        /// By default, <see cref="TokenStream"/>s that are not of the type
        /// <see cref="CachingTokenFilter"/> are wrapped in a <see cref="CachingTokenFilter"/> to
        /// ensure an efficient reset - if you are already using a different caching
        /// <see cref="TokenStream"/> impl and you don't want it to be wrapped, set this to
        /// false.
        /// </summary>
        public virtual void SetWrapIfNotCachingTokenFilter(bool wrap)
        {
            this.wrapToCaching = wrap;
        }

        public virtual void SetMaxDocCharsToAnalyze(int maxDocCharsToAnalyze)
        {
            this.maxCharsToAnalyze = maxDocCharsToAnalyze;
        }
    }
}