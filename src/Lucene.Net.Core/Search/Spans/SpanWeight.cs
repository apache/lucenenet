using System.Collections.Generic;

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
    using Bits = Lucene.Net.Util.Bits;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// Expert-only.  Public for use by other weight implementations
    /// </summary>
    public class SpanWeight : Weight
    {
        protected Similarity Similarity; // LUCENENET TODO: rename
        protected IDictionary<Term, TermContext> TermContexts; // LUCENENET TODO: rename
        protected SpanQuery query; // LUCENENET TODO: rename
        protected Similarity.SimWeight Stats; // LUCENENET TODO: rename

        public SpanWeight(SpanQuery query, IndexSearcher searcher)
        {
            this.Similarity = searcher.Similarity;
            this.query = query;

            TermContexts = new Dictionary<Term, TermContext>();
            SortedSet<Term> terms = new SortedSet<Term>();
            query.ExtractTerms(terms);
            IndexReaderContext context = searcher.TopReaderContext;
            TermStatistics[] termStats = new TermStatistics[terms.Count];
            int i = 0;
            foreach (Term term in terms)
            {
                TermContext state = TermContext.Build(context, term);
                termStats[i] = searcher.TermStatistics(term, state);
                TermContexts[term] = state;
                i++;
            }
            string field = query.Field;
            if (field != null)
            {
                Stats = Similarity.ComputeWeight(query.Boost, searcher.CollectionStatistics(query.Field), termStats);
            }
        }

        public override Query Query
        {
            get
            {
                return query;
            }
        }

        public override float GetValueForNormalization()
        {
            return Stats == null ? 1.0f : Stats.GetValueForNormalization();
        }

        public override void Normalize(float queryNorm, float topLevelBoost)
        {
            if (Stats != null)
            {
                Stats.Normalize(queryNorm, topLevelBoost);
            }
        }

        public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
        {
            if (Stats == null)
            {
                return null;
            }
            else
            {
                return new SpanScorer(query.GetSpans(context, acceptDocs, TermContexts), this, Similarity.DoSimScorer(Stats, context));
            }
        }

        public override Explanation Explain(AtomicReaderContext context, int doc)
        {
            SpanScorer scorer = (SpanScorer)Scorer(context, context.AtomicReader.LiveDocs);
            if (scorer != null)
            {
                int newDoc = scorer.Advance(doc);
                if (newDoc == doc)
                {
                    float freq = scorer.SloppyFreq;
                    Similarity.SimScorer docScorer = Similarity.DoSimScorer(Stats, context);
                    ComplexExplanation result = new ComplexExplanation();
                    result.Description = "weight(" + Query + " in " + doc + ") [" + Similarity.GetType().Name + "], result of:";
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