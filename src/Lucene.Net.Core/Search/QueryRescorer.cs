using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Search
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

    /// <summary>
    /// A <seealso cref="Rescorer"/> that uses a provided Query to assign
    ///  scores to the first-pass hits.
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class QueryRescorer : Rescorer
    {
        private readonly Query Query; // LUCENENET TODO: Rename (private)

        /// <summary>
        /// Sole constructor, passing the 2nd pass query to
        ///  assign scores to the 1st pass hits.
        /// </summary>
        public QueryRescorer(Query query)
        {
            this.Query = query;
        }

        /// <summary>
        /// Implement this in a subclass to combine the first pass and
        /// second pass scores.  If secondPassMatches is false then
        /// the second pass query failed to match a hit from the
        /// first pass query, and you should ignore the
        /// secondPassScore.
        /// </summary>
        protected abstract float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore);

        public override TopDocs Rescore(IndexSearcher searcher, TopDocs firstPassTopDocs, int topN)
        {
            ScoreDoc[] hits = (ScoreDoc[])firstPassTopDocs.ScoreDocs.Clone();
            Array.Sort(hits, new ComparatorAnonymousInnerClassHelper(this));

            IList<AtomicReaderContext> leaves = searcher.IndexReader.Leaves;

            Weight weight = searcher.CreateNormalizedWeight(Query);

            // Now merge sort docIDs from hits, with reader's leaves:
            int hitUpto = 0;
            int readerUpto = -1;
            int endDoc = 0;
            int docBase = 0;
            Scorer scorer = null;

            while (hitUpto < hits.Length)
            {
                ScoreDoc hit = hits[hitUpto];
                int docID = hit.Doc;
                AtomicReaderContext readerContext = null;
                while (docID >= endDoc)
                {
                    readerUpto++;
                    readerContext = leaves[readerUpto];
                    endDoc = readerContext.DocBase + readerContext.Reader.MaxDoc;
                }

                if (readerContext != null)
                {
                    // We advanced to another segment:
                    docBase = readerContext.DocBase;
                    scorer = weight.Scorer(readerContext, null);
                }

                int targetDoc = docID - docBase;
                int actualDoc = scorer.DocID;
                if (actualDoc < targetDoc)
                {
                    actualDoc = scorer.Advance(targetDoc);
                }

                if (actualDoc == targetDoc)
                {
                    // Query did match this doc:
                    hit.Score = Combine(hit.Score, true, scorer.Score());
                }
                else
                {
                    // Query did not match this doc:
                    Debug.Assert(actualDoc > targetDoc);
                    hit.Score = Combine(hit.Score, false, 0.0f);
                }

                hitUpto++;
            }

            // TODO: we should do a partial sort (of only topN)
            // instead, but typically the number of hits is
            // smallish:
            Array.Sort(hits, new ComparatorAnonymousInnerClassHelper2(this));

            if (topN < hits.Length)
            {
                ScoreDoc[] subset = new ScoreDoc[topN];
                Array.Copy(hits, 0, subset, 0, topN);
                hits = subset;
            }

            return new TopDocs(firstPassTopDocs.TotalHits, hits, hits[0].Score);
        }

        private class ComparatorAnonymousInnerClassHelper : IComparer<ScoreDoc>
        {
            private readonly QueryRescorer OuterInstance; // LUCENENET TODO: Rename (private)

            public ComparatorAnonymousInnerClassHelper(QueryRescorer outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public virtual int Compare(ScoreDoc a, ScoreDoc b)
            {
                return a.Doc - b.Doc;
            }
        }

        private class ComparatorAnonymousInnerClassHelper2 : IComparer<ScoreDoc>
        {
            private readonly QueryRescorer OuterInstance; // LUCENENET TODO: Rename (private)

            public ComparatorAnonymousInnerClassHelper2(QueryRescorer outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public virtual int Compare(ScoreDoc a, ScoreDoc b)
            {
                // Sort by score descending, then docID ascending:
                if (a.Score > b.Score)
                {
                    return -1;
                }
                else if (a.Score < b.Score)
                {
                    return 1;
                }
                else
                {
                    // this subtraction can't overflow int
                    // because docIDs are >= 0:
                    return a.Doc - b.Doc;
                }
            }
        }

        public override Explanation Explain(IndexSearcher searcher, Explanation firstPassExplanation, int docID)
        {
            Explanation secondPassExplanation = searcher.Explain(Query, docID);

            float? secondPassScore = secondPassExplanation.IsMatch ? (float?)secondPassExplanation.Value : null;

            float score;
            if (secondPassScore == null)
            {
                score = Combine(firstPassExplanation.Value, false, 0.0f);
            }
            else
            {
                score = Combine(firstPassExplanation.Value, true, (float)secondPassScore);
            }

            Explanation result = new Explanation(score, "combined first and second pass score using " + this.GetType());

            Explanation first = new Explanation(firstPassExplanation.Value, "first pass score");
            first.AddDetail(firstPassExplanation);
            result.AddDetail(first);

            Explanation second;
            if (secondPassScore == null)
            {
                second = new Explanation(0.0f, "no second pass score");
            }
            else
            {
                second = new Explanation((float)secondPassScore, "second pass score");
            }
            second.AddDetail(secondPassExplanation);
            result.AddDetail(second);

            return result;
        }

        /// <summary>
        /// Sugar API, calling {#rescore} using a simple linear
        ///  combination of firstPassScore + weight * secondPassScore
        /// </summary>
        public static TopDocs Rescore(IndexSearcher searcher, TopDocs topDocs, Query query, double weight, int topN)
        {
            return new QueryRescorerAnonymousInnerClassHelper(query, weight).Rescore(searcher, topDocs, topN);
        }

        private class QueryRescorerAnonymousInnerClassHelper : QueryRescorer
        {
            private double Weight;

            public QueryRescorerAnonymousInnerClassHelper(Lucene.Net.Search.Query query, double weight)
                : base(query)
            {
                this.Weight = weight;
            }

            protected override float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore)
            {
                float score = firstPassScore;
                if (secondPassMatches)
                {
                    score += (float)(Weight * secondPassScore);
                }
                return score;
            }
        }
    }
}