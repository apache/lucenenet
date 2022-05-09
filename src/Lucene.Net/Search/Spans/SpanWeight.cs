using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Spans
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// Expert-only.  Public for use by other weight implementations
    /// </summary>
    public class SpanWeight : Weight
    {
        protected Similarity m_similarity;
        protected IDictionary<Term, TermContext> m_termContexts;
        protected SpanQuery m_query;
        protected Similarity.SimWeight m_stats;

        public SpanWeight(SpanQuery query, IndexSearcher searcher)
        {
            this.m_similarity = searcher.Similarity;
            this.m_query = query;

            m_termContexts = new Dictionary<Term, TermContext>();
            ISet<Term> terms = new JCG.SortedSet<Term>();
            query.ExtractTerms(terms);
            IndexReaderContext context = searcher.TopReaderContext;
            TermStatistics[] termStats = new TermStatistics[terms.Count];
            int i = 0;
            foreach (Term term in terms)
            {
                TermContext state = TermContext.Build(context, term);
                termStats[i] = searcher.TermStatistics(term, state);
                m_termContexts[term] = state;
                i++;
            }
            string field = query.Field;
            if (field != null)
            {
                m_stats = m_similarity.ComputeWeight(query.Boost, searcher.CollectionStatistics(query.Field), termStats);
            }
        }

        public override Query Query => m_query;

        public override float GetValueForNormalization()
        {
            return m_stats is null ? 1.0f : m_stats.GetValueForNormalization();
        }

        public override void Normalize(float queryNorm, float topLevelBoost)
        {
            if (m_stats != null)
            {
                m_stats.Normalize(queryNorm, topLevelBoost);
            }
        }

        public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
        {
            if (m_stats is null)
            {
                return null;
            }
            else
            {
                return new SpanScorer(m_query.GetSpans(context, acceptDocs, m_termContexts), this, m_similarity.GetSimScorer(m_stats, context));
            }
        }

        public override Explanation Explain(AtomicReaderContext context, int doc)
        {
            SpanScorer scorer = (SpanScorer)GetScorer(context, context.AtomicReader.LiveDocs);
            if (scorer != null)
            {
                int newDoc = scorer.Advance(doc);
                if (newDoc == doc)
                {
                    float freq = scorer.SloppyFreq;
                    Similarity.SimScorer docScorer = m_similarity.GetSimScorer(m_stats, context);
                    ComplexExplanation result = new ComplexExplanation();
                    result.Description = "weight(" + Query + " in " + doc + ") [" + m_similarity.GetType().Name + "], result of:";
                    Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
                    result.AddDetail(scoreExplanation);
                    result.Value = scoreExplanation.Value;
                    result.Match = true;
                    return result;
                }
            }

            return new ComplexExplanation(false, 0.0f, "no matching term");
        }
    }
}