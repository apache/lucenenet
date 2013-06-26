using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Similarities
{
    public abstract class SimilarityBase : Similarity
    {
        private static readonly double LOG_2 = Math.Log(2);
        private static readonly float[] NORM_TABLE = new float[256];

        static SimilarityBase()
        {
            for (int i = 0; i < 256; i++)
            {
                float floatNorm = SmallFloat.Byte315ToFloat((byte) i);
                NORM_TABLE[i] = 1.0f/(floatNorm*floatNorm);
            }
        }

        public bool DiscountOverlaps { get; set; }

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats,
                                                       TermStatistics[] termStats)
        {
            var stats = new BasicStats[termStats.Length];
            for (int i = 0; i < termStats.Length; i++)
            {
                stats[i] = NewStats(collectionStats.Field, queryBoost);
                FillBasicStats(stats[i], collectionStats, termStats[i]);
            }
            return stats.Length == 1 ? stats[0] : new MultiSimilarity.MultiStats(stats) as SimWeight;
        }

        protected virtual BasicStats NewStats(string field, float queryBoost)
        {
            return new BasicStats(field, queryBoost);
        }

        protected virtual void FillBasicStats(BasicStats stats, CollectionStatistics collectionStats,
                                              TermStatistics termStats)
        {
            // assert collectionStats.sumTotalTermFreq() == -1 || collectionStats.sumTotalTermFreq() >= termStats.totalTermFreq();
            var numberOfDocuments = collectionStats.MaxDox;

            var docFreq = termStats.DocFreq;
            var totalTermFreq = termStats.TotalTermFreq;

            if (totalTermFreq == -1)
            {
                totalTermFreq = docFreq;
            }

            long numberOfFieldTokens = 0L;
            float avgFieldLength = 0f;

            var sumTotalTermFreq = collectionStats.SumTotalTermFreq;

            if (sumTotalTermFreq <= 0)
            {
                numberOfFieldTokens = docFreq;
                avgFieldLength = 1;
            }
            else
            {
                numberOfFieldTokens = sumTotalTermFreq;
                avgFieldLength = (float) numberOfFieldTokens/numberOfDocuments;
            }

            stats.NumberOfDocuments = numberOfDocuments;
            stats.NumberOfFieldTokens = numberOfFieldTokens;
            stats.AvgFieldLength = avgFieldLength;
            stats.DocFreq = docFreq;
            stats.TotalTermFreq = totalTermFreq;
        }

        protected abstract float Score(BasicStats stats, float freq, float docLen);

        protected virtual void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
        }

        protected virtual Explanation Explain(BasicStats stats, int doc, Explanation freq, float docLen)
        {
            var result = new Explanation();

            result.Value = Score(stats, freq.Value, docLen);
            result.Description = "score(" + GetType().Name +
                                 ", doc=" + doc + ", freq=" + freq.Value + "), computed from:";
            result.AddDetail(freq);

            Explain(result, stats, doc, freq.Value, docLen);

            return result;
        }

        public override ExactSimScorer GetExactSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var multiStats = stats as MultiSimilarity.MultiStats;
            if (multiStats != null)
            {
                SimWeight[] subStats = multiStats.subStats;
                var subScorers = new ExactSimScorer[subStats.Length];
                for (int i = 0; i < subScorers.Length; i++)
                {
                    var basicstats = (BasicStats) subStats[i];
                    subScorers[i] = new BasicExactDocScorer(basicstats, context.Reader.GetNormValues(basicstats.Field),
                                                            this);
                }
                return new MultiSimilarity.MultiExactDocScorer(subScorers);
            }
            else
            {
                var basicstats = (BasicStats) stats;
                return new BasicExactDocScorer(basicstats, context.Reader.GetNormValues(basicstats.Field), this);
            }
        }

        public override SloppySimScorer GetSloppySimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var multiStats = stats as MultiSimilarity.MultiStats;
            if (multiStats != null)
            {
                SimWeight[] subStats = multiStats.subStats;
                var subScorers = new SloppySimScorer[subStats.Length];
                for (int i = 0; i < subScorers.Length; i++)
                {
                    var basicstats = (BasicStats) subStats[i];
                    subScorers[i] = new BasicSloppyDocScorer(basicstats, context.Reader.GetNormValues(basicstats.Field),
                                                             this);
                }
                return new MultiSimilarity.MultiSloppyDocScorer(subScorers);
            }
            else
            {
                var basicstats = (BasicStats) stats;
                return new BasicSloppyDocScorer(basicstats, context.Reader.GetNormValues(basicstats.Field), this);
            }
        }

        public abstract override string ToString();

        public override long ComputeNorm(FieldInvertState state)
        {
            float numTerms;
            if (DiscountOverlaps)
                numTerms = state.Length - state.NumOverlap;
            else
                numTerms = state.Length/state.Boost;
            return EncodeNormValue(state.Boost, numTerms);
        }

        protected virtual float DecodeNormValue(sbyte norm)
        {
            return NORM_TABLE[norm & 0xFF];
        }

        protected virtual sbyte EncodeNormValue(float boost, float length)
        {
            return SmallFloat.FloatToByte315((boost/(float) Math.Sqrt(length)));
        }

        public static double Log2(double x)
        {
            return Math.Log(x)/LOG_2;
        }

        private class BasicExactDocScorer : ExactSimScorer
        {
            private readonly NumericDocValues norms;
            private readonly SimilarityBase parent;
            private readonly BasicStats stats;

            public BasicExactDocScorer(BasicStats stats, NumericDocValues norms, SimilarityBase parent)
            {
                this.stats = stats;
                this.norms = norms;
                this.parent = parent;
            }

            public override float Score(int doc, int freq)
            {
                return parent.Score(stats, freq,
                                    norms == null ? 1F : parent.DecodeNormValue((sbyte) norms.Get(doc)));
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return parent.Explain(stats, doc, freq,
                                      norms == null ? 1F : parent.DecodeNormValue((sbyte) norms.Get(doc)));
            }
        }

        private class BasicSloppyDocScorer : SloppySimScorer
        {
            private readonly NumericDocValues norms;
            private readonly SimilarityBase parent;
            private readonly BasicStats stats;

            public BasicSloppyDocScorer(BasicStats stats, NumericDocValues norms, SimilarityBase parent)
            {
                this.stats = stats;
                this.norms = norms;
                this.parent = parent;
            }

            public override float Score(int doc, float freq)
            {
                return parent.Score(stats, freq,
                                    norms == null ? 1F : parent.DecodeNormValue((sbyte) norms.Get(doc)));
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return parent.Explain(stats, doc, freq,
                                      norms == null ? 1F : parent.DecodeNormValue((sbyte) norms.Get(doc)));
            }

            public override float ComputeSlopFactor(int distance)
            {
                return 1.0f/(distance + 1);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return 1f;
            }
        }
    }
}