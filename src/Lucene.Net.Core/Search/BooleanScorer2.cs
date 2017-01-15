using System.Collections.Generic;
using System.Linq;

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

    using BooleanWeight = Lucene.Net.Search.BooleanQuery.BooleanWeight;

    /* See the description in BooleanScorer.java, comparing
     * BooleanScorer & BooleanScorer2 */

    /// <summary>
    /// An alternative to BooleanScorer that also allows a minimum number
    /// of optional scorers that should match.
    /// <br>Implements skipTo(), and has no limitations on the numbers of added scorers.
    /// <br>Uses ConjunctionScorer, DisjunctionScorer, ReqOptScorer and ReqExclScorer.
    /// </summary>
    internal class BooleanScorer2 : Scorer
    {
        private readonly IList<Scorer> requiredScorers;
        private readonly IList<Scorer> optionalScorers;
        private readonly IList<Scorer> prohibitedScorers;

        private class Coordinator
        {
            private readonly BooleanScorer2 outerInstance;

            internal readonly float[] coordFactors;

            internal Coordinator(BooleanScorer2 outerInstance, int maxCoord, bool disableCoord)
            {
                this.outerInstance = outerInstance;
                coordFactors = new float[outerInstance.optionalScorers.Count + outerInstance.requiredScorers.Count + 1];
                for (int i = 0; i < coordFactors.Length; i++)
                {
                    coordFactors[i] = disableCoord ? 1.0f : ((BooleanWeight)outerInstance.m_weight).Coord(i, maxCoord);
                }
            }

            internal int nrMatchers; // to be increased by score() of match counting scorers.
        }

        private readonly Coordinator coordinator;

        /// <summary>
        /// The scorer to which all scoring will be delegated,
        /// except for computing and using the coordination factor.
        /// </summary>
        private readonly Scorer countingSumScorer;

        /// <summary>
        /// The number of optionalScorers that need to match (if there are any) </summary>
        private readonly int minNrShouldMatch;

        private int doc = -1;

        /// <summary>
        /// Creates a <seealso cref="Scorer"/> with the given similarity and lists of required,
        /// prohibited and optional scorers. In no required scorers are added, at least
        /// one of the optional scorers will have to match during the search.
        /// </summary>
        /// <param name="weight">
        ///          The BooleanWeight to be used. </param>
        /// <param name="disableCoord">
        ///          If this parameter is true, coordination level matching
        ///          (<seealso cref="Similarity#coord(int, int)"/>) is not used. </param>
        /// <param name="minNrShouldMatch">
        ///          The minimum number of optional added scorers that should match
        ///          during the search. In case no required scorers are added, at least
        ///          one of the optional scorers will have to match during the search. </param>
        /// <param name="required">
        ///          the list of required scorers. </param>
        /// <param name="prohibited">
        ///          the list of prohibited scorers. </param>
        /// <param name="optional">
        ///          the list of optional scorers. </param>
        public BooleanScorer2(BooleanWeight weight, bool disableCoord, int minNrShouldMatch, IList<Scorer> required, IList<Scorer> prohibited, IList<Scorer> optional, int maxCoord)
            : base(weight)
        {
            if (minNrShouldMatch < 0)
            {
                throw new System.ArgumentException("Minimum number of optional scorers should not be negative");
            }
            this.minNrShouldMatch = minNrShouldMatch;

            optionalScorers = optional;
            requiredScorers = required;
            prohibitedScorers = prohibited;
            coordinator = new Coordinator(this, maxCoord, disableCoord);

            countingSumScorer = MakeCountingSumScorer(disableCoord);
        }

        /// <summary>
        /// Count a scorer as a single match. </summary>
        private class SingleMatchScorer : Scorer
        {
            private readonly BooleanScorer2 outerInstance;

            internal Scorer scorer;
            internal int lastScoredDoc = -1;

            // Save the score of lastScoredDoc, so that we don't compute it more than
            // once in score().
            internal float lastDocScore = float.NaN;

            internal SingleMatchScorer(BooleanScorer2 outerInstance, Scorer scorer)
                : base(scorer.m_weight)
            {
                this.outerInstance = outerInstance;
                this.scorer = scorer;
            }

            public override float GetScore()
            {
                int doc = DocID;
                if (doc >= lastScoredDoc)
                {
                    if (doc > lastScoredDoc)
                    {
                        lastDocScore = scorer.GetScore();
                        lastScoredDoc = doc;
                    }
                    outerInstance.coordinator.nrMatchers++;
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

            public override long GetCost()
            {
                return scorer.GetCost();
            }
        }

        private Scorer CountingDisjunctionSumScorer(IList<Scorer> scorers, int minNrShouldMatch)
        {
            // each scorer from the list counted as a single matcher
            if (minNrShouldMatch > 1)
            {
                return new MinShouldMatchSumScorerAnonymousInnerClassHelper(this, m_weight, scorers, minNrShouldMatch);
            }
            else
            {
                // we pass null for coord[] since we coordinate ourselves and override score()
                return new DisjunctionSumScorerAnonymousInnerClassHelper(this, m_weight, scorers.ToArray(), null);
            }
        }

        private class MinShouldMatchSumScorerAnonymousInnerClassHelper : MinShouldMatchSumScorer
        {
            private readonly BooleanScorer2 outerInstance;

            public MinShouldMatchSumScorerAnonymousInnerClassHelper(BooleanScorer2 outerInstance, Lucene.Net.Search.Weight weight, IList<Scorer> scorers, int minNrShouldMatch)
                : base(weight, scorers, minNrShouldMatch)
            {
                this.outerInstance = outerInstance;
            }

            public override float GetScore()
            {
                outerInstance.coordinator.nrMatchers += base.m_nrMatchers;
                return base.GetScore();
            }
        }

        private class DisjunctionSumScorerAnonymousInnerClassHelper : DisjunctionSumScorer
        {
            private readonly BooleanScorer2 outerInstance;

            public DisjunctionSumScorerAnonymousInnerClassHelper(BooleanScorer2 outerInstance, Weight weight, Scorer[] subScorers, float[] coord)
                : base(weight, subScorers, coord)
            {
                this.outerInstance = outerInstance;
            }

            public override float GetScore()
            {
                outerInstance.coordinator.nrMatchers += base.m_nrMatchers;
                return (float)base.m_score;
            }
        }

        private Scorer CountingConjunctionSumScorer(bool disableCoord, IList<Scorer> requiredScorers)
        {
            // each scorer from the list counted as a single matcher
            int requiredNrMatchers = requiredScorers.Count;
            return new ConjunctionScorerAnonymousInnerClassHelper(this, m_weight, requiredScorers.ToArray(), requiredNrMatchers);
        }

        private class ConjunctionScorerAnonymousInnerClassHelper : ConjunctionScorer
        {
            private readonly BooleanScorer2 outerInstance;

            private int requiredNrMatchers;

            public ConjunctionScorerAnonymousInnerClassHelper(BooleanScorer2 outerInstance, Weight weight, Scorer[] scorers, int requiredNrMatchers)
                : base(weight, scorers)
            {
                this.outerInstance = outerInstance;
                this.requiredNrMatchers = requiredNrMatchers;
                lastScoredDoc = -1;
                lastDocScore = float.NaN;
            }

            private int lastScoredDoc;

            // Save the score of lastScoredDoc, so that we don't compute it more than
            // once in score().
            private float lastDocScore;

            public override float GetScore()
            {
                int doc = outerInstance.DocID;
                if (doc >= lastScoredDoc)
                {
                    if (doc > lastScoredDoc)
                    {
                        lastDocScore = base.GetScore();
                        lastScoredDoc = doc;
                    }
                    outerInstance.coordinator.nrMatchers += requiredNrMatchers;
                }
                // All scorers match, so defaultSimilarity super.score() always has 1 as
                // the coordination factor.
                // Therefore the sum of the scores of the requiredScorers
                // is used as score.
                return lastDocScore;
            }
        }

        private Scorer DualConjunctionSumScorer(bool disableCoord, Scorer req1, Scorer req2) // non counting.
        {
            return new ConjunctionScorer(m_weight, new Scorer[] { req1, req2 });
            // All scorers match, so defaultSimilarity always has 1 as
            // the coordination factor.
            // Therefore the sum of the scores of two scorers
            // is used as score.
        }

        /// <summary>
        /// Returns the scorer to be used for match counting and score summing.
        /// Uses requiredScorers, optionalScorers and prohibitedScorers.
        /// </summary>
        private Scorer MakeCountingSumScorer(bool disableCoord) // each scorer counted as a single matcher
        {
            return (requiredScorers.Count == 0) 
                ? MakeCountingSumScorerNoReq(disableCoord) 
                : MakeCountingSumScorerSomeReq(disableCoord);
        }

        private Scorer MakeCountingSumScorerNoReq(bool disableCoord) // No required scorers
        {
            // minNrShouldMatch optional scorers are required, but at least 1
            int nrOptRequired = (minNrShouldMatch < 1) ? 1 : minNrShouldMatch;
            Scorer requiredCountingSumScorer;
            if (optionalScorers.Count > nrOptRequired)
            {
                requiredCountingSumScorer = CountingDisjunctionSumScorer(optionalScorers, nrOptRequired);
            }
            else if (optionalScorers.Count == 1)
            {
                requiredCountingSumScorer = new SingleMatchScorer(this, optionalScorers[0]);
            }
            else
            {
                requiredCountingSumScorer = CountingConjunctionSumScorer(disableCoord, optionalScorers);
            }
            return AddProhibitedScorers(requiredCountingSumScorer);
        }

        private Scorer MakeCountingSumScorerSomeReq(bool disableCoord) // At least one required scorer.
        {
            if (optionalScorers.Count == minNrShouldMatch) // all optional scorers also required.
            {
                List<Scorer> allReq = new List<Scorer>(requiredScorers);
                allReq.AddRange(optionalScorers);
                return AddProhibitedScorers(CountingConjunctionSumScorer(disableCoord, allReq));
            } // optionalScorers.size() > minNrShouldMatch, and at least one required scorer
            else
            {
                Scorer requiredCountingSumScorer = requiredScorers.Count == 1 ? new SingleMatchScorer(this, requiredScorers[0]) : CountingConjunctionSumScorer(disableCoord, requiredScorers);
                if (minNrShouldMatch > 0) // use a required disjunction scorer over the optional scorers
                {
                    return AddProhibitedScorers(DualConjunctionSumScorer(disableCoord, requiredCountingSumScorer, CountingDisjunctionSumScorer(optionalScorers, minNrShouldMatch))); // non counting
                } // minNrShouldMatch == 0
                else
                {
                    return new ReqOptSumScorer(AddProhibitedScorers(requiredCountingSumScorer), optionalScorers.Count == 1 ? new SingleMatchScorer(this, optionalScorers[0])
                        // require 1 in combined, optional scorer.
                                    : CountingDisjunctionSumScorer(optionalScorers, 1));
                }
            }
        }

        /// <summary>
        /// Returns the scorer to be used for match counting and score summing.
        /// Uses the given required scorer and the prohibitedScorers. </summary>
        /// <param name="requiredCountingSumScorer"> A required scorer already built. </param>
        private Scorer AddProhibitedScorers(Scorer requiredCountingSumScorer)
        {
            return (prohibitedScorers.Count == 0) ? requiredCountingSumScorer : new ReqExclScorer(requiredCountingSumScorer, ((prohibitedScorers.Count == 1) ? prohibitedScorers[0] : new MinShouldMatchSumScorer(m_weight, prohibitedScorers))); // no prohibited
        }

        public override int DocID
        {
            get { return doc; }
        }

        public override int NextDoc()
        {
            return doc = countingSumScorer.NextDoc();
        }

        public override float GetScore()
        {
            coordinator.nrMatchers = 0;
            float sum = countingSumScorer.GetScore();
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

        public override long GetCost()
        {
            return countingSumScorer.GetCost();
        }

        public override ICollection<ChildScorer> GetChildren()
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