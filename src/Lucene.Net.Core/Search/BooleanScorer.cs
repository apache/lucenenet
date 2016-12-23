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

    /* Description from Doug Cutting (excerpted from
     * LUCENE-1483):
     *
     * BooleanScorer uses an array to score windows of
     * 2K docs. So it scores docs 0-2K first, then docs 2K-4K,
     * etc. For each window it iterates through all query terms
     * and accumulates a score in table[doc%2K]. It also stores
     * in the table a bitmask representing which terms
     * contributed to the score. Non-zero scores are chained in
     * a linked list. At the end of scoring each window it then
     * iterates through the linked list and, if the bitmask
     * matches the boolean constraints, collects a hit. For
     * boolean queries with lots of frequent terms this can be
     * much faster, since it does not need to update a priority
     * queue for each posting, instead performing constant-time
     * operations per posting. The only downside is that it
     * results in hits being delivered out-of-order within the
     * window, which means it cannot be nested within other
     * scorers. But it works well as a top-level scorer.
     *
     * The new BooleanScorer2 implementation instead works by
     * merging priority queues of postings, albeit with some
     * clever tricks. For example, a pure conjunction (all terms
     * required) does not require a priority queue. Instead it
     * sorts the posting streams at the start, then repeatedly
     * skips the first to to the last. If the first ever equals
     * the last, then there's a hit. When some terms are
     * required and some terms are optional, the conjunction can
     * be evaluated first, then the optional terms can all skip
     * to the match and be added to the score. Thus the
     * conjunction can reduce the number of priority queue
     * updates for the optional terms. */

    internal sealed class BooleanScorer : BulkScorer
    {
        private sealed class BooleanScorerCollector : Collector
        {
            private BucketTable BucketTable; // LUCENENET TODO: Rename (private)
            private int Mask; // LUCENENET TODO: Rename (private)
            private Scorer Scorer_Renamed; // LUCENENET TODO: Rename (private)

            public BooleanScorerCollector(int mask, BucketTable bucketTable)
            {
                this.Mask = mask;
                this.BucketTable = bucketTable;
            }

            public override void Collect(int doc)
            {
                BucketTable table = BucketTable;
                int i = doc & BucketTable.MASK;
                Bucket bucket = table.Buckets[i];

                if (bucket.Doc != doc) // invalid bucket
                {
                    bucket.Doc = doc; // set doc
                    bucket.Score = Scorer_Renamed.Score(); // initialize score
                    bucket.Bits = Mask; // initialize mask
                    bucket.Coord = 1; // initialize coord

                    bucket.Next = table.First; // push onto valid list
                    table.First = bucket;
                } // valid bucket
                else
                {
                    bucket.Score += Scorer_Renamed.Score(); // increment score
                    bucket.Bits |= Mask; // add bits in mask
                    bucket.Coord++; // increment coord
                }
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                    // not needed by this implementation
                }
            }

            public override Scorer Scorer
            {
                set
                {
                    this.Scorer_Renamed = value;
                }
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return true;
            }
        }

        internal sealed class Bucket
        {
            internal int Doc = -1; // tells if bucket is valid // LUCENENET TODO: Make property
            internal double Score; // incremental score // LUCENENET TODO: Make property

            // TODO: break out bool anyProhibited, int
            // numRequiredMatched; then we can remove 32 limit on
            // required clauses
            internal int Bits; // used for bool constraints // LUCENENET TODO: Make property

            internal int Coord; // count of terms in score // LUCENENET TODO: Make property
            internal Bucket Next; // next valid bucket // LUCENENET TODO: Make property
        }

        /// <summary>
        /// A simple hash table of document scores within a range. </summary>
        internal sealed class BucketTable
        {
            public static readonly int SIZE = 1 << 11;
            public static readonly int MASK = SIZE - 1;

            internal readonly Bucket[] Buckets = new Bucket[SIZE];
            internal Bucket First = null; // head of valid list

            public BucketTable()
            {
                // Pre-fill to save the lazy init when collecting
                // each sub:
                for (int idx = 0; idx < SIZE; idx++)
                {
                    Buckets[idx] = new Bucket();
                }
            }

            public Collector NewCollector(int mask)
            {
                return new BooleanScorerCollector(mask, this);
            }

            public int Size() // LUCENENET TODO: Rename Count (or Length?)
            {
                return SIZE;
            }
        }

        internal sealed class SubScorer
        {
            public BulkScorer Scorer; // LUCENENET TODO: Make property

            // TODO: re-enable this if BQ ever sends us required clauses
            //public boolean required = false;
            public bool Prohibited; // LUCENENET TODO: Make property

            public Collector Collector; // LUCENENET TODO: Make property
            public SubScorer Next; // LUCENENET TODO: Make property
            public bool More; // LUCENENET TODO: Make property

            public SubScorer(BulkScorer scorer, bool required, bool prohibited, Collector collector, SubScorer next)
            {
                if (required)
                {
                    throw new System.ArgumentException("this scorer cannot handle required=true");
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

        private SubScorer Scorers = null; // LUCENENET TODO: Rename (private)
        private BucketTable bucketTable = new BucketTable();
        private readonly float[] CoordFactors; // LUCENENET TODO: Rename (private)

        // TODO: re-enable this if BQ ever sends us required clauses
        //private int requiredMask = 0;
        private readonly int MinNrShouldMatch; // LUCENENET TODO: Rename (private)

        private int End; // LUCENENET TODO: Rename (private)
        private Bucket Current; // LUCENENET TODO: Rename (private)

        // Any time a prohibited clause matches we set bit 0:
        private const int PROHIBITED_MASK = 1;

        private readonly Weight Weight; // LUCENENET TODO: Rename (private)

        internal BooleanScorer(BooleanWeight weight, bool disableCoord, int minNrShouldMatch, IList<BulkScorer> optionalScorers, IList<BulkScorer> prohibitedScorers, int maxCoord)
        {
            this.MinNrShouldMatch = minNrShouldMatch;
            this.Weight = weight;

            foreach (BulkScorer scorer in optionalScorers)
            {
                Scorers = new SubScorer(scorer, false, false, bucketTable.NewCollector(0), Scorers);
            }

            foreach (BulkScorer scorer in prohibitedScorers)
            {
                Scorers = new SubScorer(scorer, false, true, bucketTable.NewCollector(PROHIBITED_MASK), Scorers);
            }

            CoordFactors = new float[optionalScorers.Count + 1];
            for (int i = 0; i < CoordFactors.Length; i++)
            {
                CoordFactors[i] = disableCoord ? 1.0f : weight.Coord(i, maxCoord);
            }
        }

        public override bool Score(Collector collector, int max)
        {
            bool more;
            Bucket tmp;
            FakeScorer fs = new FakeScorer();

            // The internal loop will set the score and doc before calling collect.
            collector.Scorer = fs;
            do
            {
                bucketTable.First = null;

                while (Current != null) // more queued
                {
                    // check prohibited & required
                    if ((Current.Bits & PROHIBITED_MASK) == 0)
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
                        if (Current.Doc >= max)
                        {
                            tmp = Current;
                            Current = Current.Next;
                            tmp.Next = bucketTable.First;
                            bucketTable.First = tmp;
                            continue;
                        }

                        if (Current.Coord >= MinNrShouldMatch)
                        {
                            fs.score = (float)(Current.Score * CoordFactors[Current.Coord]);
                            fs.doc = Current.Doc;
                            fs.freq = Current.Coord;
                            collector.Collect(Current.Doc);
                        }
                    }

                    Current = Current.Next; // pop the queue
                }

                if (bucketTable.First != null)
                {
                    Current = bucketTable.First;
                    bucketTable.First = Current.Next;
                    return true;
                }

                // refill the queue
                more = false;
                End += BucketTable.SIZE;
                for (SubScorer sub = Scorers; sub != null; sub = sub.Next)
                {
                    if (sub.More)
                    {
                        sub.More = sub.Scorer.Score(sub.Collector, End);
                        more |= sub.More;
                    }
                }
                Current = bucketTable.First;
            } while (Current != null || more);

            return false;
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("boolean(");
            for (SubScorer sub = Scorers; sub != null; sub = sub.Next)
            {
                buffer.Append(sub.Scorer.ToString());
                buffer.Append(" ");
            }
            buffer.Append(")");
            return buffer.ToString();
        }
    }
}