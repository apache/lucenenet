using Lucene.Net.Diagnostics;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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

    /// <summary>
    /// A <see cref="Scorer"/> for queries with a required part and an optional part.
    /// Delays SkipTo() on the optional part until a GetScore() is needed.
    /// <para/>
    /// This <see cref="Scorer"/> implements <see cref="DocIdSetIterator.Advance(int)"/>.
    /// </summary>
    internal class ReqOptSumScorer : Scorer
    {
        /// <summary>
        /// The scorers passed from the constructor.
        /// These are set to <c>null</c> as soon as their Next() or SkipTo() returns <c>false</c>.
        /// </summary>
        private readonly Scorer reqScorer; // LUCENENET: marked readonly

        private Scorer optScorer;

        /// <summary>
        /// Construct a <see cref="ReqOptSumScorer"/>. </summary>
        /// <param name="reqScorer"> The required scorer. This must match. </param>
        /// <param name="optScorer"> The optional scorer. This is used for scoring only. </param>
        public ReqOptSumScorer(Scorer reqScorer, Scorer optScorer)
            : base(reqScorer.m_weight)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(reqScorer != null);
                Debugging.Assert(optScorer != null);
            }
            this.reqScorer = reqScorer;
            this.optScorer = optScorer;
        }

        public override int NextDoc()
        {
            return reqScorer.NextDoc();
        }

        public override int Advance(int target)
        {
            return reqScorer.Advance(target);
        }

        public override int DocID => reqScorer.DocID;

        /// <summary>
        /// Returns the score of the current document matching the query.
        /// Initially invalid, until <see cref="NextDoc()"/> is called the first time. </summary>
        /// <returns> The score of the required scorer, eventually increased by the score
        /// of the optional scorer when it also matches the current document. </returns>
        public override float GetScore()
        {
            // TODO: sum into a double and cast to float if we ever send required clauses to BS1
            int curDoc = reqScorer.DocID;
            float reqScore = reqScorer.GetScore();
            if (optScorer is null)
            {
                return reqScore;
            }

            int optScorerDoc = optScorer.DocID;
            if (optScorerDoc < curDoc && (optScorerDoc = optScorer.Advance(curDoc)) == NO_MORE_DOCS)
            {
                optScorer = null;
                return reqScore;
            }

            return optScorerDoc == curDoc ? reqScore + optScorer.GetScore() : reqScore;
        }

        public override int Freq
        {
            get
            {
                // we might have deferred advance()
                GetScore();
                return (optScorer != null && optScorer.DocID == reqScorer.DocID) ? 2 : 1;
            }
        }

        public override ICollection<ChildScorer> GetChildren()
        {
            return new JCG.List<ChildScorer>(2)
            {
                new ChildScorer(reqScorer, "MUST"),
                new ChildScorer(optScorer, "SHOULD")
            };
        }

        public override long GetCost()
        {
            return reqScorer.GetCost();
        }
    }
}