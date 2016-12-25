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

    /// <summary>
    /// A Scorer for queries with a required part and an optional part.
    /// Delays skipTo() on the optional part until a score() is needed.
    /// <br>
    /// this <code>Scorer</code> implements <seealso cref="Scorer#advance(int)"/>.
    /// </summary>
    internal class ReqOptSumScorer : Scorer
    {
        /// <summary>
        /// The scorers passed from the constructor.
        /// These are set to null as soon as their next() or skipTo() returns false.
        /// </summary>
        private Scorer ReqScorer; // LUCENENET TODO: Rename (private)

        private Scorer OptScorer; // LUCENENET TODO: Rename (private)

        /// <summary>
        /// Construct a <code>ReqOptScorer</code>. </summary>
        /// <param name="reqScorer"> The required scorer. this must match. </param>
        /// <param name="optScorer"> The optional scorer. this is used for scoring only. </param>
        public ReqOptSumScorer(Scorer reqScorer, Scorer optScorer)
            : base(reqScorer.weight)
        {
            Debug.Assert(reqScorer != null);
            Debug.Assert(optScorer != null);
            this.ReqScorer = reqScorer;
            this.OptScorer = optScorer;
        }

        public override int NextDoc()
        {
            return ReqScorer.NextDoc();
        }

        public override int Advance(int target)
        {
            return ReqScorer.Advance(target);
        }

        public override int DocID
        {
            get { return ReqScorer.DocID; }
        }

        /// <summary>
        /// Returns the score of the current document matching the query.
        /// Initially invalid, until <seealso cref="#nextDoc()"/> is called the first time. </summary>
        /// <returns> The score of the required scorer, eventually increased by the score
        /// of the optional scorer when it also matches the current document. </returns>
        public override float Score()
        {
            // TODO: sum into a double and cast to float if we ever send required clauses to BS1
            int curDoc = ReqScorer.DocID;
            float reqScore = ReqScorer.Score();
            if (OptScorer == null)
            {
                return reqScore;
            }

            int optScorerDoc = OptScorer.DocID;
            if (optScorerDoc < curDoc && (optScorerDoc = OptScorer.Advance(curDoc)) == NO_MORE_DOCS)
            {
                OptScorer = null;
                return reqScore;
            }

            return optScorerDoc == curDoc ? reqScore + OptScorer.Score() : reqScore;
        }

        public override int Freq
        {
            get
            {
                // we might have deferred advance()
                Score();
                return (OptScorer != null && OptScorer.DocID == ReqScorer.DocID) ? 2 : 1;
            }
        }

        public override ICollection<ChildScorer> Children
        {
            get
            {
                List<ChildScorer> children = new List<ChildScorer>(2);
                children.Add(new ChildScorer(ReqScorer, "MUST"));
                children.Add(new ChildScorer(OptScorer, "SHOULD"));
                return children;
            }
        }

        public override long Cost()
        {
            return ReqScorer.Cost();
        }
    }
}