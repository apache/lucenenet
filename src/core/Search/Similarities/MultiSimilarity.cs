using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Util;

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

        public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats,
                                                TermStatistics[] termStats)
        {
            var subStats = new SimWeight[sims.Length];
            for (int i = 0; i < subStats.Length; i++)
            {
                subStats[i] = sims[i].ComputeWeight(queryBoost, collectionStats, termStats);
            }
            return new MultiStats(subStats);
        }

        public override ExactSimScorer GetExactSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var subScorers = new ExactSimScorer[sims.Length];
            for (int i = 0; i < subScorers.Length; i++)
            {
                subScorers[i] = sims[i].GetExactSimScorer(((MultiStats) stats).subStats[i], context);
            }
            return new MultiExactDocScorer(subScorers);
        }

        public override SloppySimScorer GetSloppySimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var subScorers = new SloppySimScorer[sims.Length];
            for (int i = 0; i < subScorers.Length; i++)
            {
                subScorers[i] = sims[i].GetSloppySimScorer(((MultiStats) stats).subStats[i], context);
            }
            return new MultiSloppyDocScorer(subScorers);
        }

        internal class MultiExactDocScorer : ExactSimScorer
        {
            private readonly ExactSimScorer[] subScorers;

            public MultiExactDocScorer(ExactSimScorer[] subScorers)
            {
                this.subScorers = subScorers;
            }

            public override float Score(int doc, int freq)
            {
                return subScorers.Sum(subScorer => subScorer.Score(doc, freq));
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                var expl = new Explanation(Score(doc, (int) freq.Value), "sum of:");
                foreach (ExactSimScorer subScorer in subScorers)
                {
                    expl.AddDetail(subScorer.Explain(doc, freq));
                }
                return expl;
            }
        }

        internal class MultiSloppyDocScorer : SloppySimScorer
        {
            private readonly SloppySimScorer[] subScorers;

            public MultiSloppyDocScorer(SloppySimScorer[] subScorers)
            {
                this.subScorers = subScorers;
            }

            public override float Score(int doc, float freq)
            {
                return subScorers.Sum(subScorer => subScorer.Score(doc, freq));
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                var expl = new Explanation(Score(doc, freq.Value), "sum of:");
                foreach (SloppySimScorer subScorer in subScorers)
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

            public MultiStats(SimWeight[] subStats)
            {
                this.subStats = subStats;
            }

            public override float GetValueForNormalization()
            {
                float sum = subStats.Sum(stat => stat.GetValueForNormalization());
                return sum/subStats.Length;
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