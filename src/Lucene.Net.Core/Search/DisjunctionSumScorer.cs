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
    /// A Scorer for OR like queries, counterpart of <code>ConjunctionScorer</code>.
    /// this Scorer implements <seealso cref="Scorer#advance(int)"/> and uses advance() on the given Scorers.
    /// </summary>
    internal class DisjunctionSumScorer : DisjunctionScorer
    {
        /// <summary>
        /// The number of subscorers that provide the current match. </summary>
        protected internal int NrMatchers = -1;

        protected internal double score = float.NaN;
        private readonly float[] Coord;

        /// <summary>
        /// Construct a <code>DisjunctionScorer</code>. </summary>
        /// <param name="weight"> The weight to be used. </param>
        /// <param name="subScorers"> Array of at least two subscorers. </param>
        /// <param name="coord"> Table of coordination factors </param>
        internal DisjunctionSumScorer(Weight weight, Scorer[] subScorers, float[] coord)
            : base(weight, subScorers)
        {
            if (NumScorers <= 1)
            {
                throw new System.ArgumentException("There must be at least 2 subScorers");
            }
            this.Coord = coord;
        }

        protected internal override void AfterNext()
        {
            Scorer sub = SubScorers[0];
            Doc = sub.DocID();
            if (Doc != NO_MORE_DOCS)
            {
                score = sub.Score();
                NrMatchers = 1;
                CountMatches(1);
                CountMatches(2);
            }
        }

        // TODO: this currently scores, but so did the previous impl
        // TODO: remove recursion.
        // TODO: if we separate scoring, out of here,
        // then change freq() to just always compute it from scratch
        private void CountMatches(int root)
        {
            if (root < NumScorers && SubScorers[root].DocID() == Doc)
            {
                NrMatchers++;
                score += SubScorers[root].Score();
                CountMatches((root << 1) + 1);
                CountMatches((root << 1) + 2);
            }
        }

        /// <summary>
        /// Returns the score of the current document matching the query.
        /// Initially invalid, until <seealso cref="#nextDoc()"/> is called the first time.
        /// </summary>
        public override float Score()
        {
            return (float)score * Coord[NrMatchers];
        }

        public override int Freq
        {
            get { return NrMatchers; }
        }
    }
}