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
using Lucene.Net.Index;
using Lucene.Net.Util;
using IndexReader = Lucene.Net.Index.IndexReader;
using Entry = Lucene.Net.Search.FieldValueHitQueue.Entry;

namespace Lucene.Net.Search
{

    /// <summary> A <see cref="Collector" /> that sorts by <see cref="SortField" /> using
    /// <see cref="FieldComparator" />s.
    /// <p/>
    /// See the <see cref="Create" /> method
    /// for instantiating a TopFieldCollector.
    /// 
    /// <p/><b>NOTE:</b> This API is experimental and might change in
    /// incompatible ways in the next release.<p/>
    /// </summary>
    public abstract class TopFieldCollector : TopDocsCollector<Entry>
    {
        // TODO: one optimization we could do is to pre-fill
        // the queue with sentinel value that guaranteed to
        // always compare lower than a real hit; this would
        // save having to check queueFull on each insert

        //
        // Implements a TopFieldCollector over one SortField criteria, without
        // tracking document scores and maxScore.
        //
        private class OneComparatorNonScoringCollector : TopFieldCollector
        {
            internal FieldComparator comparator;
            internal readonly int reverseMul;
            internal readonly FieldValueHitQueue<Entry> queue;

            public OneComparatorNonScoringCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
                this.queue = queue;
                comparator = queue.Comparators[0];
                reverseMul = queue.ReverseMul[0];
            }

            internal void UpdateBottom(int doc)
            {
                // bottom.score is already set to Float.NaN in add().
                bottom.Doc = docBase + doc;
                bottom = pq.UpdateTop();
            }

            public override void Collect(int doc)
            {
                ++totalHits;
                if (queueFull)
                {
                    if ((reverseMul * comparator.CompareBottom(doc)) <= 0)
                    {
                        // since docs are visited in doc Id order, if compare is 0, it means
                        // this document is largest than anything else in the queue, and
                        // therefore not competitive.
                        return;
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    comparator.Copy(bottom.slot, doc);
                    UpdateBottom(doc);
                    comparator.SetBottom(bottom.slot);
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    comparator.Copy(slot, doc);
                    Add(slot, doc, float.NaN);
                    if (queueFull)
                    {
                        comparator.SetBottom(bottom.slot);
                    }
                }
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                this.docBase = context.docBase;
                queue.SetComparator(0, comparator.SetNextReader(context));
                comparator = queue.FirstComparator;
            }

            public override void SetScorer(Scorer scorer)
            {
                comparator.SetScorer(scorer);
            }
        }

        //
        // Implements a TopFieldCollector over one SortField criteria, without
        // tracking document scores and maxScore, and assumes out of orderness in doc
        // Ids collection.
        //
        private class OutOfOrderOneComparatorNonScoringCollector : OneComparatorNonScoringCollector
        {

            public OutOfOrderOneComparatorNonScoringCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
            }

            public override void Collect(int doc)
            {
                ++totalHits;
                if (queueFull)
                {
                    // Fastmatch: return if this hit is not competitive
                    var cmp = reverseMul * comparator.CompareBottom(doc);
                    if (cmp < 0 || (cmp == 0 && doc + docBase > bottom.Doc))
                    {
                        return;
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    comparator.Copy(bottom.slot, doc);
                    UpdateBottom(doc);
                    comparator.SetBottom(bottom.slot);
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    comparator.Copy(slot, doc);
                    Add(slot, doc, float.NaN);
                    if (queueFull)
                    {
                        comparator.SetBottom(bottom.slot);
                    }
                }
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }

        /*
        * Implements a TopFieldCollector over one SortField criteria, while tracking
        * document scores but no maxScore.
        */
        private class OneComparatorScoringNoMaxScoreCollector : OneComparatorNonScoringCollector
        {
            internal Scorer scorer;

            public OneComparatorScoringNoMaxScoreCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
            }

            internal void UpdateBottom(int doc, float score)
            {
                bottom.Doc = docBase + doc;
                bottom.Score = score;
                bottom = pq.UpdateTop();
            }

            public override void Collect(int doc)
            {
                ++totalHits;
                if (queueFull)
                {
                    if ((reverseMul * comparator.CompareBottom(doc)) <= 0)
                    {
                        // since docs are visited in doc Id order, if compare is 0, it means
                        // this document is largest than anything else in the queue, and
                        // therefore not competitive.
                        return;
                    }

                    // Compute the score only if the hit is competitive.
                    var score = scorer.Score();

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    comparator.Copy(bottom.slot, doc);
                    UpdateBottom(doc, score);
                    comparator.SetBottom(bottom.slot);
                }
                else
                {
                    // Compute the score only if the hit is competitive.
                    var score = scorer.Score();

                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    comparator.Copy(slot, doc);
                    Add(slot, doc, score);
                    if (queueFull)
                    {
                        comparator.SetBottom(bottom.slot);
                    }
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
                comparator.SetScorer(scorer);
            }
        }

        /*
        * Implements a TopFieldCollector over one SortField criteria, while tracking
        * document scores but no maxScore, and assumes out of orderness in doc Ids
        * collection.
        */
        private class OutOfOrderOneComparatorScoringNoMaxScoreCollector : OneComparatorScoringNoMaxScoreCollector
        {

            public OutOfOrderOneComparatorScoringNoMaxScoreCollector(FieldValueHitQueue queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
            }

            public override void Collect(int doc)
            {
                ++totalHits;
                if (queueFull)
                {
                    // Fastmatch: return if this hit is not competitive
                    var cmp = reverseMul * comparator.CompareBottom(doc);
                    if (cmp < 0 || (cmp == 0 && doc + docBase > bottom.Doc))
                    {
                        return;
                    }

                    // Compute the score only if the hit is competitive.
                    var score = scorer.Score();

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    comparator.Copy(bottom.slot, doc);
                    UpdateBottom(doc, score);
                    comparator.SetBottom(bottom.slot);
                }
                else
                {
                    // Compute the score only if the hit is competitive.
                    var score = scorer.Score();

                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    comparator.Copy(slot, doc);
                    Add(slot, doc, score);
                    if (queueFull)
                    {
                        comparator.SetBottom(bottom.slot);
                    }
                }
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }

        //
        // Implements a TopFieldCollector over one SortField criteria, with tracking
        // document scores and maxScore.
        //
        private class OneComparatorScoringMaxScoreCollector : OneComparatorNonScoringCollector
        {
            internal Scorer scorer;

            public OneComparatorScoringMaxScoreCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
                // Must set maxScore to NEG_INF, or otherwise Math.max always returns NaN.
                maxScore = float.NegativeInfinity;
            }

            internal void UpdateBottom(int doc, float score)
            {
                bottom.Doc = docBase + doc;
                bottom.Score = score;
                bottom = pq.UpdateTop();
            }

            public override void Collect(int doc)
            {
                var score = scorer.Score();
                if (score > maxScore)
                {
                    maxScore = score;
                }
                ++totalHits;
                if (queueFull)
                {
                    if ((reverseMul * comparator.CompareBottom(doc)) <= 0)
                    {
                        // since docs are visited in doc Id order, if compare is 0, it means
                        // this document is largest than anything else in the queue, and
                        // therefore not competitive.
                        return;
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    comparator.Copy(bottom.slot, doc);
                    UpdateBottom(doc, score);
                    comparator.SetBottom(bottom.slot);
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    comparator.Copy(slot, doc);
                    Add(slot, doc, score);
                    if (queueFull)
                    {
                        comparator.SetBottom(bottom.slot);
                    }
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
                base.SetScorer(scorer);
            }
        }

        //
        // Implements a TopFieldCollector over one SortField criteria, with tracking
        // document scores and maxScore, and assumes out of orderness in doc Ids
        // collection.
        //
        private class OutOfOrderOneComparatorScoringMaxScoreCollector : OneComparatorScoringMaxScoreCollector
        {

            public OutOfOrderOneComparatorScoringMaxScoreCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
            }

            public override void Collect(int doc)
            {
                var score = scorer.Score();
                if (score > maxScore)
                {
                    maxScore = score;
                }
                ++totalHits;
                if (queueFull)
                {
                    // Fastmatch: return if this hit is not competitive
                    var cmp = reverseMul * comparator.CompareBottom(doc);
                    if (cmp < 0 || (cmp == 0 && doc + docBase > bottom.Doc))
                    {
                        return;
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    comparator.Copy(bottom.slot, doc);
                    UpdateBottom(doc, score);
                    comparator.SetBottom(bottom.slot);
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    comparator.Copy(slot, doc);
                    Add(slot, doc, score);
                    if (queueFull)
                    {
                        comparator.SetBottom(bottom.slot);
                    }
                }
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }

        /*
        * Implements a TopFieldCollector over multiple SortField criteria, without
        * tracking document scores and maxScore.
        */
        private class MultiComparatorNonScoringCollector : TopFieldCollector
        {
            internal readonly FieldComparator[] comparators;
            internal readonly int[] reverseMul;
            internal readonly FieldValueHitQueue<Entry> queue;

            public MultiComparatorNonScoringCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
                this.queue = queue;
                comparators = queue.Comparators;
                reverseMul = queue.ReverseMul;
            }

            internal void UpdateBottom(int doc)
            {
                // bottom.score is already set to Float.NaN in add().
                bottom.Doc = docBase + doc;
                bottom = pq.UpdateTop();
            }

            public override void Collect(int doc)
            {
                ++totalHits;
                if (queueFull)
                {
                    // Fastmatch: return if this hit is not competitive
                    for (var i = 0; ; i++)
                    {
                        var c = reverseMul[i] * comparators[i].CompareBottom(doc);
                        if (c < 0)
                        {
                            // Definitely not competitive.
                            return;
                        }
                        else if (c > 0)
                        {
                            // Definitely competitive.
                            break;
                        }
                        else if (i == comparators.Length - 1)
                        {
                            // Here c=0. If we're at the last comparator, this doc is not
                            // competitive, since docs are visited in doc Id order, which means
                            // this doc cannot compete with any other document in the queue.
                            return;
                        }
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    foreach (var t in comparators)
                    {
                        t.Copy(bottom.slot, doc);
                    }

                    UpdateBottom(doc);

                    foreach (var t in comparators)
                    {
                        t.SetBottom(bottom.slot);
                    }
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    foreach (var t in comparators)
                    {
                        t.Copy(slot, doc);
                    }
                    Add(slot, doc, float.NaN);
                    if (queueFull)
                    {
                        foreach (var t in comparators)
                        {
                            t.SetBottom(bottom.slot);
                        }
                    }
                }
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                this.docBase = docBase;
                for (var i = 0; i < comparators.Length; i++)
                {
                    queue.SetComparators(i, comparators[i].SetNextReader(context));
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                // set the scorer on all comparators
                foreach (var t in comparators)
                {
                    t.SetScorer(scorer);
                }
            }
        }

        /*
        * Implements a TopFieldCollector over multiple SortField criteria, without
        * tracking document scores and maxScore, and assumes out of orderness in doc
        * Ids collection.
        */
        private class OutOfOrderMultiComparatorNonScoringCollector : MultiComparatorNonScoringCollector
        {

            public OutOfOrderMultiComparatorNonScoringCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
            }

            public override void Collect(int doc)
            {
                ++totalHits;
                if (queueFull)
                {
                    // Fastmatch: return if this hit is not competitive
                    for (int i = 0; ; i++)
                    {
                        int c = reverseMul[i] * comparators[i].CompareBottom(doc);
                        if (c < 0)
                        {
                            // Definitely not competitive.
                            return;
                        }
                        else if (c > 0)
                        {
                            // Definitely competitive.
                            break;
                        }
                        else if (i == comparators.Length - 1)
                        {
                            // This is the equals case.
                            if (doc + docBase > bottom.Doc)
                            {
                                // Definitely not competitive
                                return;
                            }
                            break;
                        }
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    foreach (var t in comparators)
                    {
                        t.Copy(bottom.slot, doc);
                    }

                    UpdateBottom(doc);

                    foreach (var t in comparators)
                    {
                        t.SetBottom(bottom.slot);
                    }
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    foreach (var t in comparators)
                    {
                        t.Copy(slot, doc);
                    }
                    Add(slot, doc, float.NaN);
                    if (queueFull)
                    {
                        foreach (var t in comparators)
                        {
                            t.SetBottom(bottom.slot);
                        }
                    }
                }
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }

        /*
        * Implements a TopFieldCollector over multiple SortField criteria, with
        * tracking document scores and maxScore.
        */
        private class MultiComparatorScoringMaxScoreCollector : MultiComparatorNonScoringCollector
        {

            internal Scorer scorer;

            public MultiComparatorScoringMaxScoreCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
                // Must set maxScore to NEG_INF, or otherwise Math.max always returns NaN.
                maxScore = float.NegativeInfinity;
            }

            internal void UpdateBottom(int doc, float score)
            {
                bottom.Doc = docBase + doc;
                bottom.Score = score;
                bottom = pq.UpdateTop();
            }

            public override void Collect(int doc)
            {
                var score = scorer.Score();
                if (score > maxScore)
                {
                    maxScore = score;
                }
                ++totalHits;
                if (queueFull)
                {
                    // Fastmatch: return if this hit is not competitive
                    for (var i = 0; ; i++)
                    {
                        var c = reverseMul[i] * comparators[i].CompareBottom(doc);
                        if (c < 0)
                        {
                            // Definitely not competitive.
                            return;
                        }
                        else if (c > 0)
                        {
                            // Definitely competitive.
                            break;
                        }
                        else if (i == comparators.Length - 1)
                        {
                            // Here c=0. If we're at the last comparator, this doc is not
                            // competitive, since docs are visited in doc Id order, which means
                            // this doc cannot compete with any other document in the queue.
                            return;
                        }
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    foreach (var t in comparators)
                    {
                        t.Copy(bottom.slot, doc);
                    }

                    UpdateBottom(doc, score);

                    foreach (var t in comparators)
                    {
                        t.SetBottom(bottom.slot);
                    }
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    foreach (var t in comparators)
                    {
                        t.Copy(slot, doc);
                    }
                    Add(slot, doc, score);
                    if (queueFull)
                    {
                        foreach (var t in comparators)
                        {
                            t.SetBottom(bottom.slot);
                        }
                    }
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
                base.SetScorer(scorer);
            }
        }

        /*
        * Implements a TopFieldCollector over multiple SortField criteria, with
        * tracking document scores and maxScore, and assumes out of orderness in doc
        * Ids collection.
        */
        private sealed class OutOfOrderMultiComparatorScoringMaxScoreCollector : MultiComparatorScoringMaxScoreCollector
        {

            public OutOfOrderMultiComparatorScoringMaxScoreCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
            }

            public override void Collect(int doc)
            {
                var score = scorer.Score();
                if (score > maxScore)
                {
                    maxScore = score;
                }
                ++totalHits;
                if (queueFull)
                {
                    // Fastmatch: return if this hit is not competitive
                    for (var i = 0; ; i++)
                    {
                        var c = reverseMul[i] * comparators[i].CompareBottom(doc);
                        if (c < 0)
                        {
                            // Definitely not competitive.
                            return;
                        }
                        else if (c > 0)
                        {
                            // Definitely competitive.
                            break;
                        }
                        else if (i == comparators.Length - 1)
                        {
                            // This is the equals case.
                            if (doc + docBase > bottom.Doc)
                            {
                                // Definitely not competitive
                                return;
                            }
                            break;
                        }
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    foreach (var t in comparators)
                    {
                        t.Copy(bottom.slot, doc);
                    }

                    UpdateBottom(doc, score);

                    foreach (var t in comparators)
                    {
                        t.SetBottom(bottom.slot);
                    }
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    foreach (var t in comparators)
                    {
                        t.Copy(slot, doc);
                    }
                    Add(slot, doc, score);
                    if (queueFull)
                    {
                        foreach (var t in comparators)
                        {
                            t.SetBottom(bottom.slot);
                        }
                    }
                }
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }

        /*
        * Implements a TopFieldCollector over multiple SortField criteria, with
        * tracking document scores and maxScore.
        */
        private class MultiComparatorScoringNoMaxScoreCollector : MultiComparatorNonScoringCollector
        {
            internal Scorer scorer;

            public MultiComparatorScoringNoMaxScoreCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
            }

            internal void UpdateBottom(int doc, float score)
            {
                bottom.Doc = docBase + doc;
                bottom.Score = score;
                bottom = pq.UpdateTop();
            }

            public override void Collect(int doc)
            {
                ++totalHits;
                if (queueFull)
                {
                    // Fastmatch: return if this hit is not competitive
                    for (var i = 0; ; i++)
                    {
                        var c = reverseMul[i] * comparators[i].CompareBottom(doc);
                        if (c < 0)
                        {
                            // Definitely not competitive.
                            return;
                        }
                        else if (c > 0)
                        {
                            // Definitely competitive.
                            break;
                        }
                        else if (i == comparators.Length - 1)
                        {
                            // Here c=0. If we're at the last comparator, this doc is not
                            // competitive, since docs are visited in doc Id order, which means
                            // this doc cannot compete with any other document in the queue.
                            return;
                        }
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    foreach (var t in comparators)
                    {
                        t.Copy(bottom.slot, doc);
                    }

                    // Compute score only if it is competitive.
                    var score = scorer.Score();
                    UpdateBottom(doc, score);

                    foreach (var t in comparators)
                    {
                        t.SetBottom(bottom.slot);
                    }
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    foreach (var t in comparators)
                    {
                        t.Copy(slot, doc);
                    }

                    // Compute score only if it is competitive.
                    var score = scorer.Score();
                    Add(slot, doc, score);
                    if (queueFull)
                    {
                        foreach (var t in comparators)
                        {
                            t.SetBottom(bottom.slot);
                        }
                    }
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
                base.SetScorer(scorer);
            }
        }

        /*
        * Implements a TopFieldCollector over multiple SortField criteria, with
        * tracking document scores and maxScore, and assumes out of orderness in doc
        * Ids collection.
        */
        private sealed class OutOfOrderMultiComparatorScoringNoMaxScoreCollector : MultiComparatorScoringNoMaxScoreCollector
        {

            public OutOfOrderMultiComparatorScoringNoMaxScoreCollector(FieldValueHitQueue<Entry> queue, int numHits, bool fillFields)
                : base(queue, numHits, fillFields)
            {
            }

            public override void Collect(int doc)
            {
                ++totalHits;
                if (queueFull)
                {
                    // Fastmatch: return if this hit is not competitive
                    for (var i = 0; ; i++)
                    {
                        var c = reverseMul[i] * comparators[i].CompareBottom(doc);
                        if (c < 0)
                        {
                            // Definitely not competitive.
                            return;
                        }
                        else if (c > 0)
                        {
                            // Definitely competitive.
                            break;
                        }
                        else if (i == comparators.Length - 1)
                        {
                            // This is the equals case.
                            if (doc + docBase > bottom.Doc)
                            {
                                // Definitely not competitive
                                return;
                            }
                            break;
                        }
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    foreach (var t in comparators)
                    {
                        t.Copy(bottom.slot, doc);
                    }

                    // Compute score only if it is competitive.
                    float score = scorer.Score();
                    UpdateBottom(doc, score);

                    foreach (var t in comparators)
                    {
                        t.SetBottom(bottom.slot);
                    }
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = totalHits - 1;
                    // Copy hit into queue
                    foreach (var t in comparators)
                    {
                        t.Copy(slot, doc);
                    }

                    // Compute score only if it is competitive.
                    var score = scorer.Score();
                    Add(slot, doc, score);
                    if (queueFull)
                    {
                        foreach (var t in comparators)
                        {
                            t.SetBottom(bottom.slot);
                        }
                    }
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
                base.SetScorer(scorer);
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }
        }

        private sealed class PagingFieldCollector : TopFieldCollector
        {
            Scorer scorer;
            int collectedHits;
            readonly FieldComparator[] comparators;
            readonly int[] reverseMul;
            readonly FieldValueHitQueue<Entry> queue;
            readonly bool trackDocScores;
            readonly bool trackMaxScore;
            readonly FieldDoc after;
            int afterDoc;

            public PagingFieldCollector(
                                FieldValueHitQueue<Entry> queue, FieldDoc after, int numHits, bool fillFields,
                                bool trackDocScores, bool trackMaxScore)
                : base(queue, numHits, fillFields)
            {
                this.queue = queue;
                this.trackDocScores = trackDocScores;
                this.trackMaxScore = trackMaxScore;
                this.after = after;
                comparators = queue.Comparators;
                reverseMul = queue.ReverseMul;

                // Must set maxScore to NEG_INF, or otherwise Math.max always returns NaN.
                maxScore = float.NegativeInfinity;
            }

            internal void UpdateBottom(int doc, float score)
            {
                bottom.Doc = docBase + doc;
                bottom.Score = score;
                bottom = pq.UpdateTop();
            }

            public override void Collect(int doc)
            {
                totalHits++;

                var sameValues = true;
                for (var compIDX = 0; compIDX < comparators.Length; compIDX++)
                {
                    var comp = comparators[compIDX];

                    var cmp = reverseMul[compIDX] * comp.CompareDocToValue(doc, after.fields[compIDX]);
                    if (cmp < 0)
                    {
                        return;
                    }
                    else if (cmp > 0)
                    {
                        sameValues = false;
                        break;
                    }
                }

                if (sameValues && doc <= afterDoc)
                {
                    return;
                }

                collectedHits++;

                var score = float.NaN;
                if (trackMaxScore)
                {
                    score = scorer.Score();
                    if (score > maxScore)
                    {
                        maxScore = score;
                    }
                }

                if (queueFull)
                {
                    // Fastmatch: return if this hit is not competitive
                    for (var i = 0; ; i++)
                    {
                        var c = reverseMul[i] * comparators[i].CompareBottom(doc);
                        if (c < 0)
                        {
                            // Definitely not competitive.
                            return;
                        }
                        else if (c > 0)
                        {
                            // Definitely competitive.
                            break;
                        }
                        else if (i == comparators.Length - 1)
                        {
                            // This is the equals case.
                            if (doc + docBase > bottom.Doc)
                            {
                                // Definitely not competitive
                                return;
                            }
                            break;
                        }
                    }

                    // This hit is competitive - replace bottom element in queue & adjustTop
                    foreach (var t in comparators)
                    {
                        t.Copy(bottom.slot, doc);
                    }

                    // Compute score only if it is competitive.
                    if (trackDocScores && !trackMaxScore)
                    {
                        score = scorer.Score();
                    }
                    UpdateBottom(doc, score);

                    foreach (var t in comparators)
                    {
                        t.SetBottom(bottom.slot);
                    }
                }
                else
                {
                    // Startup transient: queue hasn't gathered numHits yet
                    var slot = collectedHits - 1;
                    //System.out.println("    slot=" + slot);
                    // Copy hit into queue
                    foreach (var t in comparators)
                    {
                        t.Copy(slot, doc);
                    }

                    // Compute score only if it is competitive.
                    if (trackDocScores && !trackMaxScore)
                    {
                        score = scorer.Score();
                    }
                    bottom = pq.Add(new Entry(slot, docBase + doc, score));
                    queueFull = collectedHits == numHits;
                    if (queueFull)
                    {
                        foreach (var t in comparators)
                        {
                            t.SetBottom(bottom.slot);
                        }
                    }
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
                foreach (var t in comparators)
                {
                    t.SetScorer(scorer);
                }
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return true; }
            }

            public override void SetNextReader(AtomicReaderContext context)
            {
                docBase = context.docBase;
                afterDoc = after.Doc - docBase;
                for (var i = 0; i < comparators.Length; i++)
                {
                    queue.SetComparator(i, comparators[i].SetNextReader(context));
                }
            }
        }

        private static readonly ScoreDoc[] EMPTY_SCOREDOCS = new ScoreDoc[0];

        private bool fillFields;

        /*
        * Stores the maximum score value encountered, needed for normalizing. If
        * document scores are not tracked, this value is initialized to NaN.
        */
        internal float maxScore = float.NaN;

        internal int numHits;
        internal FieldValueHitQueue.Entry bottom = null;
        internal bool queueFull;
        internal int docBase;

        // Declaring the constructor private prevents extending this class by anyone
        // else. Note that the class cannot be final since it's extended by the
        // internal versions. If someone will define a constructor with any other
        // visibility, then anyone will be able to extend the class, which is not what
        // we want.
        private TopFieldCollector(PriorityQueue<Entry> pq, int numHits, bool fillFields)
            : base(pq)
        {
            this.numHits = numHits;
            this.fillFields = fillFields;
        }

        public static TopFieldCollector Create(Sort sort, int numHits, bool fillFields, bool trackDocScores,
                                               bool trackMaxScore, bool docsScoredInOrder)
        {
            return Create(sort, numHits, null, fillFields, trackDocScores, trackMaxScore, docsScoredInOrder);
        }

        /// <summary> Creates a new <see cref="TopFieldCollector" /> from the given
        /// arguments.
        /// 
        /// <p/><b>NOTE</b>: The instances returned by this method
        /// pre-allocate a full array of length
        /// <c>numHits</c>.
        /// 
        /// </summary>
        /// <param name="sort">the sort criteria (SortFields).
        /// </param>
        /// <param name="numHits">the number of results to collect.
        /// </param>
        /// <param name="fillFields">specifies whether the actual field values should be returned on
        /// the results (FieldDoc).
        /// </param>
        /// <param name="trackDocScores">specifies whether document scores should be tracked and set on the
        /// results. Note that if set to false, then the results' scores will
        /// be set to Float.NaN. Setting this to true affects performance, as
        /// it incurs the score computation on each competitive result.
        /// Therefore if document scores are not required by the application,
        /// it is recommended to set it to false.
        /// </param>
        /// <param name="trackMaxScore">specifies whether the query's maxScore should be tracked and set
        /// on the resulting <see cref="TopDocs" />. Note that if set to false,
        /// <see cref="TopDocs.MaxScore" /> returns Float.NaN. Setting this to
        /// true affects performance as it incurs the score computation on
        /// each result. Also, setting this true automatically sets
        /// <c>trackDocScores</c> to true as well.
        /// </param>
        /// <param name="docsScoredInOrder">specifies whether documents are scored in doc Id order or not by
        /// the given <see cref="Scorer" /> in <see cref="Collector.SetScorer(Scorer)" />.
        /// </param>
        /// <returns> a <see cref="TopFieldCollector" /> instance which will sort the results by
        /// the sort criteria.
        /// </returns>
        /// <throws>  IOException </throws>
        public static TopFieldCollector Create(Sort sort, int numHits, FieldDoc after, bool fillFields, bool trackDocScores, bool trackMaxScore, bool docsScoredInOrder)
        {
            if (sort.fields.Length == 0) throw new ArgumentException("Sort must contain at least one field");
            if (numHits <= 0) throw new ArgumentException("numHits must be > 0; please use TotalHitCountCollector if you just need the total hit count");


            FieldValueHitQueue<Entry> queue = FieldValueHitQueue<Entry>.Create(sort.fields, numHits);
            if (after == null)
            {
                if (queue.Comparators.Length == 1)
                {
                    if (docsScoredInOrder)
                    {
                        if (trackMaxScore)
                        {
                            return new OneComparatorScoringMaxScoreCollector(queue, numHits, fillFields);
                        }
                        else if (trackDocScores)
                        {
                            return new OneComparatorScoringNoMaxScoreCollector(queue, numHits, fillFields);
                        }
                        else
                        {
                            return new OneComparatorNonScoringCollector(queue, numHits, fillFields);
                        }
                    }
                    else
                    {
                        if (trackMaxScore)
                        {
                            return new OutOfOrderOneComparatorScoringMaxScoreCollector(queue, numHits, fillFields);
                        }
                        else if (trackDocScores)
                        {
                            return new OutOfOrderOneComparatorScoringNoMaxScoreCollector(queue, numHits, fillFields);
                        }
                        else
                        {
                            return new OutOfOrderOneComparatorNonScoringCollector(queue, numHits, fillFields);
                        }
                    }
                }

                // multiple comparators.
                if (docsScoredInOrder)
                {
                    if (trackMaxScore)
                    {
                        return new MultiComparatorScoringMaxScoreCollector(queue, numHits, fillFields);
                    }
                    else if (trackDocScores)
                    {
                        return new MultiComparatorScoringNoMaxScoreCollector(queue, numHits, fillFields);
                    }
                    else
                    {
                        return new MultiComparatorNonScoringCollector(queue, numHits, fillFields);
                    }
                }
                else
                {
                    if (trackMaxScore)
                    {
                        return new OutOfOrderMultiComparatorScoringMaxScoreCollector(queue, numHits, fillFields);
                    }
                    else if (trackDocScores)
                    {
                        return new OutOfOrderMultiComparatorScoringNoMaxScoreCollector(queue, numHits, fillFields);
                    }
                    else
                    {
                        return new OutOfOrderMultiComparatorNonScoringCollector(queue, numHits, fillFields);
                    }
                }
            }
            else
            {
                if (after.fields == null)
                {
                    throw new ArgumentException("after.fields wasn't set; you must pass fillFields=true for the previous search");
                }

                if (after.fields.Length != sort.GetSort().Length)
                {
                    throw new ArgumentException("after.fields has " + after.fields.Length + " values but sort has " + sort.GetSort().Length);
                }

                return new PagingFieldCollector(queue, after, numHits, fillFields, trackDocScores, trackMaxScore);
            }
        }

        internal void Add(int slot, int doc, float score)
        {
            bottom = pq.Add(new Entry(slot, docBase + doc, score));
            queueFull = totalHits == numHits;
        }

        /*
        * Only the following callback methods need to be overridden since
        * topDocs(int, int) calls them to return the results.
        */

        protected internal override void PopulateResults(ScoreDoc[] results, int howMany)
        {
            if (fillFields)
            {
                // avoid casting if unnecessary.
                FieldValueHitQueue<Entry> queue = (FieldValueHitQueue<Entry>)pq;
                for (var i = howMany - 1; i >= 0; i--)
                {
                    results[i] = queue.FillFields(queue.Pop());
                }
            }
            else
            {
                for (var i = howMany - 1; i >= 0; i--)
                {
                    var entry = pq.Pop();
                    results[i] = new FieldDoc(entry.Doc, entry.Score);
                }
            }
        }

        public /*protected internal*/ override TopDocs NewTopDocs(ScoreDoc[] results, int start)
        {
            if (results == null)
            {
                results = EMPTY_SCOREDOCS;
                // Set maxScore to NaN, in case this is a maxScore tracking collector.
                maxScore = float.NaN;
            }

            // If this is a maxScoring tracking collector and there were no results, 
            return new TopFieldDocs(totalHits, results, ((FieldValueHitQueue<Entry>)pq).GetFields(), maxScore);
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return false; }
        }
    }
}