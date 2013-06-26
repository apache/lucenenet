using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class MultiSimilarity : Similarity
    {
        protected readonly Similarity[] sims;

        public MultiSimilarity(Similarity[] sims)
        {
            this.sims = sims;
        }

        public override long ComputeNorm(FieldInvertState state)
        {
            return sims[0].ComputeNorm(state);
        }

        public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, TermStatistics[] termStats)
        {
            var subStats = new SimWeight[sims.Length];
            for (var i = 0; i < subStats.Length; i++)
            {
                subStats[i] = sims[i].ComputeWeight(queryBoost, collectionStats, termStats);
            }
            return new MultiStats(subStats);
        }

        public override ExactSimScorer ExactSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var subScorers = new ExactSimScorer[sims.Length];
            for (var i = 0; i < subScorers.Length; i++)
            {
                subScorers[i] = sims[i].ExactSimScorer(((MultiStats)stats).subStats[i], context);
            }
            return new MultiExactDocScorer(subScorers);
        }

        public override SloppySimScorer SloppySimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var subScorers = new SloppySimScorer[sims.Length];
            for (var i = 0; i < subScorers.Length; i++)
            {
                subScorers[i] = sims[i].SloppySimScorer(((MultiStats)stats).subStats[i], context);
            }
            return new MultiSloppyDocScorer(subScorers);
        }

        internal class MultiExactDocScorer : ExactSimScorer
        {
            private readonly ExactSimScorer[] subScorers;

            MultiExactDocScorer(ExactSimScorer[] subScorers)
            {
                this.subScorers = subScorers;
            }

            public override float Score(int doc, int freq)
            {
                float sum = 0.0f;
                foreach (var subScorer in subScorers)
                {
                    sum += subScorer.Score(doc, freq);
                }
                return sum;
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                var expl = new Explanation(Score(doc, (int)freq.Value), "sum of:");
                foreach (var subScorer in subScorers)
                {
                    expl.AddDetail(subScorer.Explain(doc, freq));
                }
                return expl;
            }
        }

        internal class MultiSloppyDocScorer : SloppySimScorer
        {
            private readonly SloppySimScorer[] subScorers;

            MultiSloppyDocScorer(SloppySimScorer[] subScorers)
            {
                this.subScorers = subScorers;
            }

            public override float Score(int doc, float freq)
            {
                float sum = 0.0f;
                foreach (var subScorer in subScorers)
                {
                    sum += subScorer.Score(doc, freq);
                }
                return sum;
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                var expl = new Explanation(Score(doc, freq.Value), "sum of:");
                foreach (var subScorer in subScorers)
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

            MultiStats(SimWeight[] subStats)
            {
                this.subStats = subStats;
            }

            public override float GetValueForNormalization()
            {
                float sum = 0.0f;
                foreach (var stat in subStats)
                {
                    sum += stat.GetValueForNormalization();
                }
                return sum / subStats.Length;
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                foreach (var stat in subStats)
                {
                    stat.Normalize(queryNorm, topLevelBoost);
                }
            }
        }
    }
}
