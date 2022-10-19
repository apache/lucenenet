using System;
using System.Collections.Generic;
using System.Text;

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
    using BooleanWeight = Lucene.Net.Search.BooleanQuery.BooleanWeight;

    /// <summary>
    /// Description from Doug Cutting (excerpted from
    /// LUCENE-1483):
    /// <para/>
    /// <see cref="BooleanScorer"/> uses an array to score windows of
    /// 2K docs. So it scores docs 0-2K first, then docs 2K-4K,
    /// etc. For each window it iterates through all query terms
    /// and accumulates a score in table[doc%2K]. It also stores
    /// in the table a bitmask representing which terms
    /// contributed to the score. Non-zero scores are chained in
    /// a linked list. At the end of scoring each window it then
    /// iterates through the linked list and, if the bitmask
    /// matches the boolean constraints, collects a hit. For
    /// boolean queries with lots of frequent terms this can be
    /// much faster, since it does not need to update a priority
    /// queue for each posting, instead performing constant-time
    /// operations per posting. The only downside is that it
    /// results in hits being delivered out-of-order within the
    /// window, which means it cannot be nested within other
    /// scorers. But it works well as a top-level scorer.
    /// <para/>
    /// The new BooleanScorer2 implementation instead works by
    /// merging priority queues of postings, albeit with some
    /// clever tricks. For example, a pure conjunction (all terms
    /// required) does not require a priority queue. Instead it
    /// sorts the posting streams at the start, then repeatedly
    /// skips the first to to the last. If the first ever equals
    /// the last, then there's a hit. When some terms are
    /// required and some terms are optional, the conjunction can
    /// be evaluated first, then the optional terms can all skip
    /// to the match and be added to the score. Thus the
    /// conjunction can reduce the number of priority queue
    /// updates for the optional terms.
    /// </summary>
    internal sealed class BooleanScorer : BulkScorer
    {
        private sealed class BooleanScorerCollector : ICollector
        {
            private readonly BucketTable bucketTable; // LUCENENET: marked readonly
            private readonly int mask; // LUCENENET: marked readonly
            private Scorer scorer;

            public BooleanScorerCollector(int mask, BucketTable bucketTable)
            {
                this.mask = mask;
                this.bucketTable = bucketTable;
            }

            public void Collect(int doc)
            {
                BucketTable table = bucketTable;
                int i = doc & BucketTable.MASK;
                Bucket bucket = table.buckets[i];

                if (bucket.Doc != doc) // invalid bucket
                {
                    bucket.Doc = doc; // set doc
                    bucket.Score = scorer.GetScore(); // initialize score
                    bucket.Bits = mask; // initialize mask
                    bucket.Coord = 1; // initialize coord

                    bucket.Next = table.first; // push onto valid list
                    table.first = bucket;
                } // valid bucket
                else
                {
                    bucket.Score += scorer.GetScore(); // increment score
                    bucket.Bits |= mask; // add bits in mask
                    bucket.Coord++; // increment coord
                }
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                // not needed by this implementation
            }

            public void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
            }

            public bool AcceptsDocsOutOfOrder => true;
        }

        internal sealed class Bucket
        {
            internal int Doc { get; set; } // tells if bucket is valid
            internal double Score { get; set; } // incremental score

            // TODO: break out bool anyProhibited, int
            // numRequiredMatched; then we can remove 32 limit on
            // required clauses
            internal int Bits { get; set; } // used for bool constraints

            internal int Coord { get; set; } // count of terms in score
            internal Bucket Next { get; set; } // next valid bucket

            public Bucket()
            {
                // Initialize properties
                Doc = -1;
            }
        }

        /// <summary>
        /// A simple hash table of document scores within a range. </summary>
        internal sealed class BucketTable
        {
            public const int SIZE = 1 << 11;
            public const int MASK = SIZE - 1;

            internal readonly Bucket[] buckets = new Bucket[SIZE];
            internal Bucket first = null; // head of valid list

            public BucketTable()
            {
                // Pre-fill to save the lazy init when collecting
                // each sub:
                for (int idx = 0; idx < SIZE; idx++)
                {
                    buckets[idx] = new Bucket();
                }
            }

            public ICollector NewCollector(int mask)
            {
                return new BooleanScorerCollector(mask, this);
            }

            public static int Count => SIZE; // LUCENENET NOTE: This was size() in Lucene. // LUCENENET: CA1822: Mark members as static
        }

        internal sealed class SubScorer
        {
            public BulkScorer Scorer { get; set; }

            // TODO: re-enable this if BQ ever sends us required clauses
            //public boolean required = false;
            public bool Prohibited { get; set; }

            public ICollector Collector { get; set; }
            public SubScorer Next { get; set; }
            public bool More { get; set; }

            public SubScorer(BulkScorer scorer, bool required, bool prohibited, ICollector collector, SubScorer next)
            {
                if (required)
                {
                    throw new ArgumentException("this scorer cannot handle required=true");
                }
                this.Scorer = scorer;
                this.More = true;
                // TODO: re-enable this if BQ ever sends us required clauses
                //this.required = required;
                this.Prohibited = prohibited;
                this.Collector = collector;
                this.Next = next;
            }
        }

        private readonly SubScorer scorers = null; // LUCENENET: marked readonly
        private readonly BucketTable bucketTable = new BucketTable(); // LUCENENET: marked readonly
        private readonly float[] coordFactors;

        // TODO: re-enable this if BQ ever sends us required clauses
        //private int requiredMask = 0;
        private readonly int minNrShouldMatch;

        private int end;
        private Bucket current;

        // Any time a prohibited clause matches we set bit 0:
        private const int PROHIBITED_MASK = 1;

        //private readonly Weight weight; // LUCENENET: Never read

        internal BooleanScorer(BooleanWeight weight, bool disableCoord, int minNrShouldMatch, IList<BulkScorer> optionalScorers, IList<BulkScorer> prohibitedScorers, int maxCoord)
        {
            this.minNrShouldMatch = minNrShouldMatch;
            //this.weight = weight; // LUCENENET: Never read

            foreach (BulkScorer scorer in optionalScorers)
            {
                scorers = new SubScorer(scorer, false, false, bucketTable.NewCollector(0), scorers);
            }

            foreach (BulkScorer scorer in prohibitedScorers)
            {
                scorers = new SubScorer(scorer, false, true, bucketTable.NewCollector(PROHIBITED_MASK), scorers);
            }

            coordFactors = new float[optionalScorers.Count + 1];
            for (int i = 0; i < coordFactors.Length; i++)
            {
                coordFactors[i] = disableCoord ? 1.0f : weight.Coord(i, maxCoord);
            }
        }

        public override bool Score(ICollector collector, int max)
        {
            bool more;
            Bucket tmp;
            FakeScorer fs = new FakeScorer();

            // The internal loop will set the score and doc before calling collect.
            collector.SetScorer(fs);
            do
            {
                bucketTable.first = null;

                while (current != null) // more queued
                {
                    // check prohibited & required
                    if ((current.Bits & PROHIBITED_MASK) == 0)
                    {
                        // TODO: re-enable this if BQ ever sends us required
                        // clauses
                        //&& (current.bits & requiredMask) == requiredMask) {
                        // NOTE: Lucene always passes max =
                        // Integer.MAX_VALUE today, because we never embed
                        // a BooleanScorer inside another (even though
                        // that should work)... but in theory an outside
                        // app could pass a different max so we must check
                        // it:
                        if (current.Doc >= max)
                        {
                            tmp = current;
                            current = current.Next;
                            tmp.Next = bucketTable.first;
                            bucketTable.first = tmp;
                            continue;
                        }

                        if (current.Coord >= minNrShouldMatch)
                        {
                            fs.score = (float)(current.Score * coordFactors[current.Coord]);
                            fs.doc = current.Doc;
                            fs.freq = current.Coord;
                            collector.Collect(current.Doc);
                        }
                    }

                    current = current.Next; // pop the queue
                }

                if (bucketTable.first != null)
                {
                    current = bucketTable.first;
                    bucketTable.first = current.Next;
                    return true;
                }

                // refill the queue
                more = false;
                end += BucketTable.SIZE;
                for (SubScorer sub = scorers; sub != null; sub = sub.Next)
                {
                    if (sub.More)
                    {
                        sub.More = sub.Scorer.Score(sub.Collector, end);
                        more |= sub.More;
                    }
                }
                current = bucketTable.first;
            } while (current != null || more);

            return false;
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("boolean(");
            for (SubScorer sub = scorers; sub != null; sub = sub.Next)
            {
                buffer.Append(sub.Scorer.ToString());
                buffer.Append(' ');
            }
            buffer.Append(')');
            return buffer.ToString();
        }
    }
}