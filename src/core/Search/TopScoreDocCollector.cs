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
using System.Diagnostics;
using Lucene.Net.Index;
using IndexReader = Lucene.Net.Index.IndexReader;

namespace Lucene.Net.Search
{

    /// <summary> A <see cref="Collector" /> implementation that collects the top-scoring hits,
    /// returning them as a <see cref="TopDocs" />. This is used by <see cref="IndexSearcher" /> to
    /// implement <see cref="TopDocs" />-based search. Hits are sorted by score descending
    /// and then (when the scores are tied) docID ascending. When you create an
    /// instance of this collector you should know in advance whether documents are
    /// going to be collected in doc Id order or not.
    /// 
    /// <p/><b>NOTE</b>: The values <see cref="float.NaN" /> and
    /// <see cref="float.NegativeInfinity" /> are not valid scores.  This
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
                var score = scorer.Score();

                // This collector cannot handle these scores:
                Debug.Assert(score != float.NegativeInfinity);
                Debug.Assert(!float.IsNaN(score));

                totalHits++;
                if (score <= pqTop.Score)
                {
                    // Since docs are returned in-order (i.e., increasing doc Id), a document
                    // with equal score to pqTop.score cannot compete since HitQueue favors
                    // documents with lower doc Ids. Therefore reject those docs too.
                    return;
                }
                pqTop.Doc = doc + docBase;
                pqTop.Score = score;
                pqTop = pq.UpdateTop();
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }
        }

        private class InOrderPagingScoreDocCollector : TopScoreDocCollector
        {
            private readonly ScoreDoc after;
            // this is always after.doc - docBase, to save an add when score == after.score
            private int afterDoc;
            private int collectedHits;

            internal InOrderPagingScoreDocCollector(ScoreDoc after, int numHits)
                : base(numHits)
            {
                this.after = after;
            }

            public override void Collect(int doc)
            {
                var score = scorer.Score();

                // This collector cannot handle these scores:
                //assert score != Float.NEGATIVE_INFINITY;
                //assert !Float.isNaN(score);

                totalHits++;

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
                pqTop = pq.UpdateTop();
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return false; }
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                base.SetNextReader(context);
                afterDoc = after.Doc - docBase;
            }

            public override int TopDocsSize
            {
                get { return collectedHits < pq.Size ? collectedHits : pq.Size; }
            }

            public override TopDocs NewTopDocs(ScoreDoc[] results, int start)
            {
                return results == null ? new TopDocs(totalHits, new ScoreDoc[0], float.NaN) : new TopDocs(totalHits, results);
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
                var score = scorer.Score();

                // This collector cannot handle NaN
                Debug.Assert(!float.IsNaN(score));

                totalHits++;
                doc += docBase;
                if (score < pqTop.Score || (score == pqTop.Score && doc > pqTop.Doc))
                {
                    return;
                }
                pqTop.Doc = doc;
                pqTop.Score = score;
                pqTop = pq.UpdateTop();
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }

        private class OutOfOrderPagingScoreDocCollector : TopScoreDocCollector
        {
            private readonly ScoreDoc after;
            // this is always after.doc - docBase, to save an add when score == after.score
            private int afterDoc;
            private int collectedHits;

            internal OutOfOrderPagingScoreDocCollector(ScoreDoc after, int numHits)
                : base(numHits)
            {
                this.after = after;
            }

            public override void Collect(int doc)
            {
                var score = scorer.Score();

                // This collector cannot handle NaN
                //assert !float.isNaN(score);

                totalHits++;
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
                pqTop = pq.UpdateTop();
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                base.SetNextReader(context);
                afterDoc = after.Doc - docBase;
            }

            public override int TopDocsSize
            {
                get { return collectedHits < pq.Size ? collectedHits : pq.Size; }
            }

            public override TopDocs NewTopDocs(ScoreDoc[] results, int start)
            {
                return results == null ? new TopDocs(totalHits, new ScoreDoc[0], float.NaN) : new TopDocs(totalHits, results);
            }
        }

        public static TopScoreDocCollector Create(int numHits, bool docsScoredInOrder)
        {
            return Create(numHits, null, docsScoredInOrder);
        }

        /// <summary> Creates a new <see cref="TopScoreDocCollector" /> given the number of hits to
        /// collect and whether documents are scored in order by the input
        /// <see cref="Scorer" /> to <see cref="SetScorer(Scorer)" />.
        /// 
        /// <p/><b>NOTE</b>: The instances returned by this method
        /// pre-allocate a full array of length
        /// <c>numHits</c>, and fill the array with sentinel
        /// objects.
        /// </summary>
        public static TopScoreDocCollector Create(int numHits, ScoreDoc after, bool docsScoredInOrder)
        {
            if (docsScoredInOrder)
            {
                return after == null
                           ? new InOrderTopScoreDocCollector(numHits) 
                           : new InOrderPagingScoreDocCollector(after, numHits) as TopScoreDocCollector;
            }
            else
            {
                return after == null
                           ? new OutOfOrderTopScoreDocCollector(numHits)
                           : new OutOfOrderPagingScoreDocCollector(after, numHits) as TopScoreDocCollector;
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
            pqTop = pq.Top();
        }

        public /*protected internal*/ override TopDocs NewTopDocs(ScoreDoc[] results, int start)
        {
            if (results == null)
            {
                return EMPTY_TOPDOCS;
            }

            // We need to compute maxScore in order to set it in TopDocs. If start == 0,
            // it means the largest element is already in results, use its score as
            // maxScore. Otherwise pop everything else, until the largest element is
            // extracted and use its score as maxScore.
            var maxScore = float.NaN;
            if (start == 0)
            {
                maxScore = results[0].Score;
            }
            else
            {
                for (var i = pq.Size; i > 1; i--)
                {
                    pq.Pop();
                }
                maxScore = pq.Pop().Score;
            }

            return new TopDocs(totalHits, results, maxScore);
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
            docBase = context.docBase;
        }

        public override void SetScorer(Scorer scorer)
        {
            this.scorer = scorer;
        }
    }
}