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

    /// <summary>
    /// A <see cref="Scorer"/> for OR like queries, counterpart of <see cref="ConjunctionScorer"/>.
    /// This <see cref="Scorer"/> implements <see cref="DocIdSetIterator.Advance(int)"/> and uses Advance() on the given <see cref="Scorer"/>s.
    /// </summary>
    internal class DisjunctionSumScorer : DisjunctionScorer
    {
        /// <summary>
        /// The number of subscorers that provide the current match. </summary>
        protected internal int m_nrMatchers = -1;

        protected internal double m_score = float.NaN;
        private readonly float[] coord;

        /// <summary>
        /// Construct a <see cref="DisjunctionScorer"/>. </summary>
        /// <param name="weight"> The weight to be used. </param>
        /// <param name="subScorers"> Array of at least two subscorers. </param>
        /// <param name="coord"> Table of coordination factors </param>
        internal DisjunctionSumScorer(Weight weight, Scorer[] subScorers, float[] coord)
            : base(weight, subScorers)
        {
            if (m_numScorers <= 1)
            {
                throw new ArgumentException("There must be at least 2 subScorers");
            }
            this.coord = coord;
        }

        protected override void AfterNext()
        {
            Scorer sub = m_subScorers[0];
            m_doc = sub.DocID;
            if (m_doc != NO_MORE_DOCS)
            {
                m_score = sub.GetScore();
                m_nrMatchers = 1;
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
            if (root < m_numScorers && m_subScorers[root].DocID == m_doc)
            {
                m_nrMatchers++;
                m_score += m_subScorers[root].GetScore();
                CountMatches((root << 1) + 1);
                CountMatches((root << 1) + 2);
            }
        }

        /// <summary>
        /// Returns the score of the current document matching the query.
        /// Initially invalid, until <see cref="DisjunctionScorer.NextDoc()"/> is called the first time.
        /// </summary>
        public override float GetScore()
        {
            return (float)m_score * coord[m_nrMatchers];
        }

        public override int Freq => m_nrMatchers;
    }
}