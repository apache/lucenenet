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
using System.Linq;

namespace Lucene.Net.Search
{
    /* See the description in BooleanScorer.java, comparing
    * BooleanScorer & BooleanScorer2 */

    /// <summary>An alternative to BooleanScorer that also allows a minimum number
    /// of optional scorers that should match.
    /// <br/>Implements skipTo(), and has no limitations on the numbers of added scorers.
    /// <br/>Uses ConjunctionScorer, DisjunctionScorer, ReqOptScorer and ReqExclScorer.
    /// </summary>
    class BooleanScorer2 : Scorer
    {
        private readonly IList<Scorer> requiredScorers;
        private readonly IList<Scorer> optionalScorers;
        private readonly IList<Scorer> prohibitedScorers;

        private class Coordinator
        {
            private readonly BooleanScorer2 enclosingInstance;

            internal readonly float[] coordFactors = null;

            public Coordinator(BooleanScorer2 enclosingInstance, int maxCoord, bool disableCoord)
            {
                this.enclosingInstance = enclosingInstance;
                coordFactors = new float[enclosingInstance.optionalScorers.Count + enclosingInstance.requiredScorers.Count + 1];
                for (int i = 0; i < coordFactors.Length; i++)
                {
                    coordFactors[i] = disableCoord ? 1.0f : ((BooleanQuery.BooleanWeight)enclosingInstance.weight).Coord(i, maxCoord);
                }
            }

            internal int nrMatchers; // to be increased by Score() of match counting scorers.
        }

        private readonly Coordinator coordinator;

        /// <summary>The scorer to which all scoring will be delegated,
        /// except for computing and using the coordination factor.
        /// </summary>
        private readonly Scorer countingSumScorer;

        /// <summary>The number of optionalScorers that need to match (if there are any) </summary>
        private readonly int minNrShouldMatch;

        private int doc = -1;

        /// <summary> Creates a <see cref="Scorer" /> with the given similarity and lists of required,
        /// prohibited and optional scorers. In no required scorers are added, at least
        /// one of the optional scorers will have to match during the search.
        /// 
        /// </summary>
        public BooleanScorer2(BooleanQuery.BooleanWeight weight, bool disableCoord, int minNrShouldMatch,
            IList<Scorer> required, IList<Scorer> prohibited, IList<Scorer> optional, int maxCoord)
            : base(weight)
        {
            if (minNrShouldMatch < 0)
            {
                throw new ArgumentException("Minimum number of optional scorers should not be negative");
            }
            this.minNrShouldMatch = minNrShouldMatch;

            optionalScorers = optional;
            requiredScorers = required;
            prohibitedScorers = prohibited;
            coordinator = new Coordinator(this, maxCoord, disableCoord);

            countingSumScorer = MakeCountingSumScorer(disableCoord);
        }

        /// <summary>Count a scorer as a single match. </summary>
        private class SingleMatchScorer : Scorer
        {
            private readonly BooleanScorer2 enclosingInstance;

            private Scorer scorer;
            private int lastScoredDoc = -1;
            // Save the score of lastScoredDoc, so that we don't compute it more than
            // once in score().
            private float lastDocScore = float.NaN;

            internal SingleMatchScorer(BooleanScorer2 enclosingInstance, Scorer scorer)
                : base(scorer.Weight)
            {
                this.enclosingInstance = enclosingInstance;
                this.scorer = scorer;
            }

            public override float Score()
            {
                int doc = DocID;
                if (doc >= lastScoredDoc)
                {
                    if (doc > lastScoredDoc)
                    {
                        lastDocScore = scorer.Score();
                        lastScoredDoc = doc;
                    }
                    enclosingInstance.coordinator.nrMatchers++;
                }
                return lastDocScore;
            }

            public override int Freq
            {
                get { return 1; }
            }

            public override int DocID
            {
                get { return scorer.DocID; }
            }

            public override int NextDoc()
            {
                return scorer.NextDoc();
            }

            public override int Advance(int target)
            {
                return scorer.Advance(target);
            }

            public override long Cost
            {
                get { return scorer.Cost; }
            }
        }

        private Scorer CountingDisjunctionSumScorer(IList<Scorer> scorers, int minNrShouldMatch)
        {
            // each scorer from the list counted as a single matcher
            if (minNrShouldMatch > 1)
            {
                return new AnonymousMinShouldMatchSumScorer(this, weight, scorers, minNrShouldMatch);
            }
            else
            {
                // we pass null for coord[] since we coordinate ourselves and override score()
                return new AnonymousDisjunctionSumScorer(this, weight, scorers.ToArray(), null);
            }
        }

        private sealed class AnonymousMinShouldMatchSumScorer : MinShouldMatchSumScorer
        {
            private readonly BooleanScorer2 enclosingInstance;

            public AnonymousMinShouldMatchSumScorer(BooleanScorer2 enclosingInstance, Weight weight, IList<Scorer> subScorers, int minimumNrMatchers)
                : base(weight, subScorers, minimumNrMatchers)
            {
                this.enclosingInstance = enclosingInstance;
            }

            public override float Score()
            {
                enclosingInstance.coordinator.nrMatchers += base.nrMatchers;
                return base.Score();
            }
        }

        private sealed class AnonymousDisjunctionSumScorer : DisjunctionSumScorer
        {
            private readonly BooleanScorer2 enclosingInstance;

            public AnonymousDisjunctionSumScorer(BooleanScorer2 enclosingInstance, Weight weight, Scorer[] subScorers, float[] coord)
                : base(weight, subScorers, coord)
            {
                this.enclosingInstance = enclosingInstance;
            }

            public override float Score()
            {
                enclosingInstance.coordinator.nrMatchers += base.nrMatchers;
                return (float)base.score;
            }
        }

        private Scorer CountingConjunctionSumScorer(bool disableCoord, IList<Scorer> requiredScorers)
        {
            // each scorer from the list counted as a single matcher
            int requiredNrMatchers = requiredScorers.Count;
            return new AnonymousConjunctionScorer(this, requiredNrMatchers, weight, requiredScorers.ToArray());
        }

        private sealed class AnonymousConjunctionScorer : ConjunctionScorer
        {
            private readonly BooleanScorer2 enclosingInstance;
            private readonly int requiredNrMatchers;
            private int lastScoredDoc = -1;
            private float lastDocScore = float.NaN;

            public AnonymousConjunctionScorer(BooleanScorer2 enclosingInstance, int requiredNrMatchers, Weight weight, Scorer[] scorers)
                : base(weight, scorers)
            {
                this.enclosingInstance = enclosingInstance;
                this.requiredNrMatchers = requiredNrMatchers;
            }

            public override float Score()
            {
                int doc = DocID;
                if (doc >= lastScoredDoc)
                {
                    if (doc > lastScoredDoc)
                    {
                        lastDocScore = base.Score();
                        lastScoredDoc = doc;
                    }
                    enclosingInstance.coordinator.nrMatchers += requiredNrMatchers;
                }
                // All scorers match, so defaultSimilarity super.score() always has 1 as
                // the coordination factor.
                // Therefore the sum of the scores of the requiredScorers
                // is used as score.
                return lastDocScore;
            }
        }

        private Scorer DualConjunctionSumScorer(bool disableCoord, Scorer req1, Scorer req2)
        {
            // non counting.
            return new ConjunctionScorer(weight, new Scorer[] { req1, req2 });
            // All scorers match, so defaultSimilarity always has 1 as
            // the coordination factor.
            // Therefore the sum of the scores of two scorers
            // is used as score.
        }

        /// <summary>Returns the scorer to be used for match counting and score summing.
        /// Uses requiredScorers, optionalScorers and prohibitedScorers.
        /// </summary>
        private Scorer MakeCountingSumScorer(bool disableCoord)
        {
            // each scorer counted as a single matcher
            return (requiredScorers.Count == 0) ? MakeCountingSumScorerNoReq(disableCoord) : MakeCountingSumScorerSomeReq(disableCoord);
        }

        private Scorer MakeCountingSumScorerNoReq(bool disableCoord)
        {
            // No required scorers
            // minNrShouldMatch optional scorers are required, but at least 1
            int nrOptRequired = (minNrShouldMatch < 1) ? 1 : minNrShouldMatch;
            Scorer requiredCountingSumScorer;
            if (optionalScorers.Count > nrOptRequired)
                requiredCountingSumScorer = CountingDisjunctionSumScorer(optionalScorers, nrOptRequired);
            else if (optionalScorers.Count == 1)
                requiredCountingSumScorer = new SingleMatchScorer(this, optionalScorers[0]);
            else
                requiredCountingSumScorer = CountingConjunctionSumScorer(disableCoord, optionalScorers);
            return AddProhibitedScorers(requiredCountingSumScorer);
        }

        private Scorer MakeCountingSumScorerSomeReq(bool disableCoord)
        {
            // At least one required scorer.
            if (optionalScorers.Count == minNrShouldMatch)
            {
                // all optional scorers also required.
                var allReq = new List<Scorer>(requiredScorers);
                allReq.AddRange(optionalScorers);
                return AddProhibitedScorers(CountingConjunctionSumScorer(disableCoord, allReq));
            }
            else
            {
                // optionalScorers.size() > minNrShouldMatch, and at least one required scorer
                Scorer requiredCountingSumScorer =
                                    requiredScorers.Count == 1
                                    ? new SingleMatchScorer(this, requiredScorers[0])
                                    : CountingConjunctionSumScorer(disableCoord, requiredScorers);
                if (minNrShouldMatch > 0)
                {
                    // use a required disjunction scorer over the optional scorers
                    return AddProhibitedScorers(
                        DualConjunctionSumScorer(
                            disableCoord,
                            requiredCountingSumScorer,
                            CountingDisjunctionSumScorer(optionalScorers, minNrShouldMatch)));
                }
                else
                {
                    // minNrShouldMatch == 0
                    return new ReqOptSumScorer(AddProhibitedScorers(requiredCountingSumScorer),
                                               optionalScorers.Count == 1
                                               ? new SingleMatchScorer(this, optionalScorers[0])
                                               : CountingDisjunctionSumScorer(optionalScorers, 1));
                }
            }
        }

        /// <summary>Returns the scorer to be used for match counting and score summing.
        /// Uses the given required scorer and the prohibitedScorers.
        /// </summary>
        /// <param name="requiredCountingSumScorer">A required scorer already built.
        /// </param>
        private Scorer AddProhibitedScorers(Scorer requiredCountingSumScorer)
        {
            return (prohibitedScorers.Count == 0)
                   ? requiredCountingSumScorer
                   : new ReqExclScorer(requiredCountingSumScorer,
                                       ((prohibitedScorers.Count == 1)
                                        ? prohibitedScorers[0]
                                        : new MinShouldMatchSumScorer(weight, prohibitedScorers)));
        }

        /// <summary>Scores and collects all matching documents.</summary>
        /// <param name="collector">The collector to which all matching documents are passed through.
        /// </param>
        public override void Score(Collector collector)
        {
            collector.SetScorer(this);
            while ((doc = countingSumScorer.NextDoc()) != NO_MORE_DOCS)
            {
                collector.Collect(doc);
            }
        }

        public override bool Score(Collector collector, int max, int firstDocID)
        {
            doc = firstDocID;
            collector.SetScorer(this);
            while (doc < max)
            {
                collector.Collect(doc);
                doc = countingSumScorer.NextDoc();
            }
            return doc != NO_MORE_DOCS;
        }

        public override int DocID
        {
            get { return doc; }
        }

        public override int NextDoc()
        {
            return doc = countingSumScorer.NextDoc();
        }

        public override float Score()
        {
            coordinator.nrMatchers = 0;
            float sum = countingSumScorer.Score();
            return sum * coordinator.coordFactors[coordinator.nrMatchers];
        }

        public override int Freq
        {
            get { return countingSumScorer.Freq; }
        }

        public override int Advance(int target)
        {
            return doc = countingSumScorer.Advance(target);
        }

        public override long Cost
        {
            get { return countingSumScorer.Cost; }
        }

        public override ICollection<ChildScorer> Children
        {
            get
            {
                List<ChildScorer> children = new List<ChildScorer>();
                foreach (Scorer s in optionalScorers)
                {
                    children.Add(new ChildScorer(s, "SHOULD"));
                }
                foreach (Scorer s in prohibitedScorers)
                {
                    children.Add(new ChildScorer(s, "MUST_NOT"));
                }
                foreach (Scorer s in requiredScorers)
                {
                    children.Add(new ChildScorer(s, "MUST"));
                }
                return children;
            }
        }
    }
}