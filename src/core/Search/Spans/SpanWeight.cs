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
using Lucene.Net.Index;
using IndexReader = Lucene.Net.Index.IndexReader;
using Lucene.Net.Search;
using IDFExplanation = Lucene.Net.Search.Explanation.IDFExplanation;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Spans
{

    /// <summary> Expert-only.  Public for use by other weight implementations</summary>
    [Serializable]
    public class SpanWeight : Weight
    {
        protected Similarity similarity;
        protected IDictionary<Term, TermContext> termContexts;
        protected SpanQuery query;
        protected Similarity.SimWeight stats;

        public SpanWeight(SpanQuery query, Searcher searcher)
        {
            this.similarity = searcher.Similarity;
            this.query = query;

            termContexts = new HashMap<Term, TermContext>();
            HashSet<Term> terms = new HashSet<Term>();
            query.ExtractTerms(terms);
            IndexReaderContext context = searcher.TopReaderContext;
            TermStatistics[] termStats = new TermStatistics[terms.Count];
            int i = 0;
            foreach (Term term in terms)
            {
                TermContext state = TermContext.build(context, term, true);
                termStats[i] = searcher.TermStatistics(term, state);
                termContexts[term] = state;
                i++;
            }
            String field = query.Field;
            if (field != null)
            {
                stats = similarity.ComputeWeight(query.Boost,
                                                 searcher.CollectionStatistics(query.Field),
                                                 termStats);
            }
        }

        public override Query Query
        {
            get { return query; }
        }

        public override float GetValueForNormalization()
        {
            return stats == null ? 1.0f : stats.GetValueForNormalization();
        }

        public override void Normalize(float queryNorm, float topLevelBoost)
        {
            if (stats != null)
            {
                stats.Normalize(queryNorm, topLevelBoost);
            }
        }

        public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, IBits acceptDocs)
        {
            if (stats == null)
            {
                return null;
            }
            else
            {
                return new SpanScorer(query.GetSpans(context, acceptDocs, termContexts), this, similarity.SloppySimScorer(stats, context));
            }
        }

        public override Explanation Explain(AtomicReaderContext context, int doc)
        {
            SpanScorer scorer = (SpanScorer)scorer(context, true, false, context.Reader.GetLiveDocs());
            if (scorer != null)
            {
                int newDoc = scorer.Advance(doc);
                if (newDoc == doc)
                {
                    float freq = scorer.SloppyFreq();
                    SloppySimScorer docScorer = similarity.SloppySimScorer(stats, context);
                    ComplexExplanation result = new ComplexExplanation();
                    result.Description = "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name + "], result of:";
                    Explanation scoreExplanation = docScorer.explain(doc, new Explanation(freq, "phraseFreq=" + freq));
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