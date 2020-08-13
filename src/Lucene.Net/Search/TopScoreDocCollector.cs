using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;

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
    /// A <see cref="ICollector"/> implementation that collects the top-scoring hits,
    /// returning them as a <see cref="TopDocs"/>. this is used by <see cref="IndexSearcher"/> to
    /// implement <see cref="TopDocs"/>-based search. Hits are sorted by score descending
    /// and then (when the scores are tied) docID ascending. When you create an
    /// instance of this collector you should know in advance whether documents are
    /// going to be collected in doc Id order or not.
    ///
    /// <para/><b>NOTE</b>: The values <see cref="float.NaN"/> and
    /// <see cref="float.NegativeInfinity"/> are not valid scores.  This
    /// collector will not properly collect hits with such
    /// scores.
    /// </summary>
    public abstract class TopScoreDocCollector : TopDocsCollector<ScoreDoc>
    {
        // Assumes docs are scored in order.
        private class InOrderTopScoreDocCollector : TopScoreDocCollector
        {
            internal InOrderTopScoreDocCollector(int numHits)
                : base(numHits)
            {
            }

            public override void Collect(int doc)
            {
                float score = scorer.GetScore();

                // this collector cannot handle these scores:
                Debugging.Assert(() => !float.IsNegativeInfinity(score));
                Debugging.Assert(() => !float.IsNaN(score));

                m_totalHits++;
                if (score <= pqTop.Score)
                {
                    // Since docs are returned in-order (i.e., increasing doc Id), a document
                    // with equal score to pqTop.score cannot compete since HitQueue favors
                    // documents with lower doc Ids. Therefore reject those docs too.
                    return;
                }
                pqTop.Doc = doc + docBase;
                pqTop.Score = score;
                pqTop = m_pq.UpdateTop();
            }

            public override bool AcceptsDocsOutOfOrder => false;
        }

        // Assumes docs are scored in order.
        private class InOrderPagingScoreDocCollector : TopScoreDocCollector
        {
            internal readonly ScoreDoc after;

            // this is always after.doc - docBase, to save an add when score == after.score
            internal int afterDoc;

            internal int collectedHits;

            internal InOrderPagingScoreDocCollector(ScoreDoc after, int numHits)
                : base(numHits)
            {
                this.after = after;
            }

            public override void Collect(int doc)
            {
                float score = scorer.GetScore();

                // this collector cannot handle these scores:
                Debugging.Assert(() => !float.IsNegativeInfinity(score));
                Debugging.Assert(() => !float.IsNaN(score));

                m_totalHits++;

                if (score > after.Score || (score == after.Score && doc <= afterDoc))
                {
                    // hit was collected on a previous page
                    return;
                }

                if (score <= pqTop.Score)
                {
                    // Since docs are returned in-order (i.e., increasing doc Id), a document
                    // with equal score to pqTop.score cannot compete since HitQueue favors
                    // documents with lower doc Ids. Therefore reject those docs too.
                    return;
                }
                collectedHits++;
                pqTop.Doc = doc + docBase;
                pqTop.Score = score;
                pqTop = m_pq.UpdateTop();
            }

            public override bool AcceptsDocsOutOfOrder => false;

            public override void SetNextReader(AtomicReaderContext context)
            {
                base.SetNextReader(context);
                afterDoc = after.Doc - docBase;
            }

            protected override int TopDocsCount => collectedHits < m_pq.Count ? collectedHits : m_pq.Count;

            protected override TopDocs NewTopDocs(ScoreDoc[] results, int start)
            {
                // LUCENENET specific - optimized empty array creation
                return results == null ? new TopDocs(m_totalHits, Arrays.Empty<ScoreDoc>(), float.NaN) : new TopDocs(m_totalHits, results);
            }
        }

        // Assumes docs are scored out of order.
        private class OutOfOrderTopScoreDocCollector : TopScoreDocCollector
        {
            internal OutOfOrderTopScoreDocCollector(int numHits)
                : base(numHits)
            {
            }

            public override void Collect(int doc)
            {
                float score = scorer.GetScore();

                // this collector cannot handle NaN
                Debugging.Assert(() => !float.IsNaN(score));

                m_totalHits++;
                if (score < pqTop.Score)
                {
                    // Doesn't compete w/ bottom entry in queue
                    return;
                }
                doc += docBase;
                if (score == pqTop.Score && doc > pqTop.Doc)
                {
                    // Break tie in score by doc ID:
                    return;
                }
                pqTop.Doc = doc;
                pqTop.Score = score;
                pqTop = m_pq.UpdateTop();
            }

            public override bool AcceptsDocsOutOfOrder => true;
        }

        // Assumes docs are scored out of order.
        private class OutOfOrderPagingScoreDocCollector : TopScoreDocCollector
        {
            internal readonly ScoreDoc after;

            // this is always after.doc - docBase, to save an add when score == after.score
            internal int afterDoc;

            internal int collectedHits;

            internal OutOfOrderPagingScoreDocCollector(ScoreDoc after, int numHits)
                : base(numHits)
            {
                this.after = after;
            }

            public override void Collect(int doc)
            {
                float score = scorer.GetScore();

                // this collector cannot handle NaN
                Debugging.Assert(() => !float.IsNaN(score));

                m_totalHits++;
                if (score > after.Score || (score == after.Score && doc <= afterDoc))
                {
                    // hit was collected on a previous page
                    return;
                }
                if (score < pqTop.Score)
                {
                    // Doesn't compete w/ bottom entry in queue
                    return;
                }
                doc += docBase;
                if (score == pqTop.Score && doc > pqTop.Doc)
                {
                    // Break tie in score by doc ID:
                    return;
                }
                collectedHits++;
                pqTop.Doc = doc;
                pqTop.Score = score;
                pqTop = m_pq.UpdateTop();
            }

            public override bool AcceptsDocsOutOfOrder => true;

            public override void SetNextReader(AtomicReaderContext context)
            {
                base.SetNextReader(context);
                afterDoc = after.Doc - docBase;
            }

            protected override int TopDocsCount => collectedHits < m_pq.Count ? collectedHits : m_pq.Count;

            protected override TopDocs NewTopDocs(ScoreDoc[] results, int start)
            {
                // LUCENENET specific - optimized empty array creation
                return results == null ? new TopDocs(m_totalHits, Arrays.Empty<ScoreDoc>(), float.NaN) : new TopDocs(m_totalHits, results);
            }
        }

        /// <summary>
        /// Creates a new <see cref="TopScoreDocCollector"/> given the number of hits to
        /// collect and whether documents are scored in order by the input
        /// <see cref="Scorer"/> to <see cref="SetScorer(Scorer)"/>.
        ///
        /// <para/><b>NOTE</b>: The instances returned by this method
        /// pre-allocate a full array of length
        /// <paramref name="numHits"/>, and fill the array with sentinel
        /// objects.
        /// </summary>
        public static TopScoreDocCollector Create(int numHits, bool docsScoredInOrder)
        {
            return Create(numHits, null, docsScoredInOrder);
        }

        /// <summary>
        /// Creates a new <see cref="TopScoreDocCollector"/> given the number of hits to
        /// collect, the bottom of the previous page, and whether documents are scored in order by the input
        /// <see cref="Scorer"/> to <see cref="SetScorer(Scorer)"/>.
        ///
        /// <para/><b>NOTE</b>: The instances returned by this method
        /// pre-allocate a full array of length
        /// <paramref name="numHits"/>, and fill the array with sentinel
        /// objects.
        /// </summary>
        public static TopScoreDocCollector Create(int numHits, ScoreDoc after, bool docsScoredInOrder)
        {
            if (numHits <= 0)
            {
                throw new ArgumentException("numHits must be > 0; please use TotalHitCountCollector if you just need the total hit count");
            }

            if (docsScoredInOrder)
            {
                return after == null ? (TopScoreDocCollector)new InOrderTopScoreDocCollector(numHits) : new InOrderPagingScoreDocCollector(after, numHits);
            }
            else
            {
                return after == null ? (TopScoreDocCollector)new OutOfOrderTopScoreDocCollector(numHits) : new OutOfOrderPagingScoreDocCollector(after, numHits);
            }
        }

        internal ScoreDoc pqTop;
        internal int docBase = 0;
        internal Scorer scorer;

        // prevents instantiation
        private TopScoreDocCollector(int numHits)
            : base(new HitQueue(numHits, true))
        {
            // HitQueue implements getSentinelObject to return a ScoreDoc, so we know
            // that at this point top() is already initialized.
            pqTop = m_pq.Top;
        }

        protected override TopDocs NewTopDocs(ScoreDoc[] results, int start)
        {
            if (results == null)
            {
                return EMPTY_TOPDOCS;
            }

            // We need to compute maxScore in order to set it in TopDocs. If start == 0,
            // it means the largest element is already in results, use its score as
            // maxScore. Otherwise pop everything else, until the largest element is
            // extracted and use its score as maxScore.
            float maxScore = float.NaN;
            if (start == 0)
            {
                maxScore = results[0].Score;
            }
            else
            {
                for (int i = m_pq.Count; i > 1; i--)
                {
                    m_pq.Pop();
                }
                maxScore = m_pq.Pop().Score;
            }

            return new TopDocs(m_totalHits, results, maxScore);
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
            docBase = context.DocBase;
        }

        public override void SetScorer(Scorer scorer)
        {
            this.scorer = scorer;
        }
    }
}