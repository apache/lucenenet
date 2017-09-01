namespace Lucene.Net.Search.Similarities
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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;

    /// <summary>
    /// Implements the CombSUM method for combining evidence from multiple
    /// similarity values described in: Joseph A. Shaw, Edward A. Fox.
    /// In Text REtrieval Conference (1993), pp. 243-252
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class MultiSimilarity : Similarity
    {
        /// <summary>
        /// the sub-similarities used to create the combined score </summary>
        protected internal readonly Similarity[] m_sims;

        /// <summary>
        /// Creates a <see cref="MultiSimilarity"/> which will sum the scores
        /// of the provided <paramref name="sims"/>.
        /// </summary>
        public MultiSimilarity(Similarity[] sims)
        {
            this.m_sims = sims;
        }

        public override long ComputeNorm(FieldInvertState state)
        {
            return m_sims[0].ComputeNorm(state);
        }

        public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
        {
            SimWeight[] subStats = new SimWeight[m_sims.Length];
            for (int i = 0; i < subStats.Length; i++)
            {
                subStats[i] = m_sims[i].ComputeWeight(queryBoost, collectionStats, termStats);
            }
            return new MultiStats(subStats);
        }

        public override SimScorer GetSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            SimScorer[] subScorers = new SimScorer[m_sims.Length];
            for (int i = 0; i < subScorers.Length; i++)
            {
                subScorers[i] = m_sims[i].GetSimScorer(((MultiStats)stats).subStats[i], context);
            }
            return new MultiSimScorer(subScorers);
        }

        internal class MultiSimScorer : SimScorer
        {
            private readonly SimScorer[] subScorers;

            internal MultiSimScorer(SimScorer[] subScorers)
            {
                this.subScorers = subScorers;
            }

            public override float Score(int doc, float freq)
            {
                float sum = 0.0f;
                foreach (SimScorer subScorer in subScorers)
                {
                    sum += subScorer.Score(doc, freq);
                }
                return sum;
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                Explanation expl = new Explanation(Score(doc, freq.Value), "sum of:");
                foreach (SimScorer subScorer in subScorers)
                {
                    expl.AddDetail(subScorer.Explain(doc, freq));
                }
                return expl;
            }

            public override float ComputeSlopFactor(int distance)
            {
                return subScorers[0].ComputeSlopFactor(distance);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return subScorers[0].ComputePayloadFactor(doc, start, end, payload);
            }
        }

        internal class MultiStats : SimWeight
        {
            internal readonly SimWeight[] subStats;

            internal MultiStats(SimWeight[] subStats)
            {
                this.subStats = subStats;
            }

            public override float GetValueForNormalization()
            {
                float sum = 0.0f;
                foreach (SimWeight stat in subStats)
                {
                    sum += stat.GetValueForNormalization();
                }
                return sum / subStats.Length;
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                foreach (SimWeight stat in subStats)
                {
                    stat.Normalize(queryNorm, topLevelBoost);
                }
            }
        }
    }
}