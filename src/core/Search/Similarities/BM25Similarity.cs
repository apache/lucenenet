using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class BM25Similarity : Similarity
    {
        private readonly float k1;
        private readonly float b;

        public BM25Similarity(float k1, float b)
        {
            this.k1 = k1;
            this.b = b;
        }

        public BM25Similarity()
        {
            this.k1 = 1.2f;
            this.b = 0.75f;
        }

        protected virtual float Idf(long docFreq, long numDocs)
        {
            return (float)Math.Log(1 + (numDocs - docFreq + 0.5D) / (docFreq + 0.5D));
        }

        protected virtual float SloppyFreq(int distance)
        {
            return 1.0f / (distance + 1);
        }

        protected virtual float ScorePayload(int doc, int start, int end, BytesRef payload)
        {
            return 1;
        }

        protected virtual float AvgFieldLength(CollectionStatistics collectionStats)
        {
            var sumTotalTermFreq = collectionStats.SumTotalTermFreq;
            if (sumTotalTermFreq <= 0)
                return 1f;
            else
                return (float)(sumTotalTermFreq / (double)collectionStats.MaxDoc);
        }

        protected virtual sbyte EncodeNormValue(float boost, int fieldLength)
        {
            return SmallFloat.FloatToByte315(boost / (float)Math.Sqrt(fieldLength));
        }

        protected virtual float DecodeNormValue(sbyte b)
        {
            return NORM_TABLE[b & 0xFF];
        }

        protected bool discountOverlaps = true;
        public virtual bool DiscountOverlaps { get { return discountOverlaps; } set { discountOverlaps = value; } }

        private static readonly float[] NORM_TABLE = new float[256];

        static BM25Similarity()
        {
            for (var i = 0; i < 256; i++)
            {
                var f = SmallFloat.Byte315ToFloat((sbyte)i);
                NORM_TABLE[i] = 1.0f / (f * f);
            }
        }

        public sealed override long ComputeNorm(FieldInvertState state)
        {
            var numTerms = discountOverlaps ? state.Length - state.NumOverlap : state.Length;
            return EncodeNormValue(state.Boost, numTerms);
        }

        public virtual Explanation IdfExplain(CollectionStatistic collectionStats, TermStatistics termStats)
        {
            long df = termStats.DocFreq;
            long max = collectionStats.MaxDoc;
            float idf = Idf(df, max);
            return new Explanation(idf, "idf(docFreq=" + df + ", maxDocs=" + max + ")");
        }

        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
        {
            long max = collectionStats.MaxDoc;
            float idf = 0.0f;
            Explanation exp = new Explanation();
            exp.Description = "idf(), sum of:";
            foreach (var stat in termStats)
            {
                long df = stat.DocFreq;
                float termIdf = Idf(df, max);
                exp.AddDetail(new Explanation(termIdf, "idf(docFreq=" + df + ", maxDocs=" + max + ")"));
                idf += termIdf;
            }
            exp.Value = idf;
            return exp;
        }

        public sealed override Similarity.SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
        {
            Explanation idf = termStats.Length == 1 ? IdfExplain(collectionStats, termStats[0]) : IdfExplain(collectionStats, termStats);

            float avgdl = AvgFieldLength(collectionStats);

            float[] cache = new float[256];
            for (var i = 0; i < cache.Length; i++) {
              cache[i] = k1 * ((1 - b) + b * DecodeNormValue((sbyte)i) / avgdl);
            }
            return new BM25Stats(collectionStats.Field, idf, queryBoost, avgdl, cache);
        }

        public sealed override Similarity.ExactSimScorer ExactSimScorer(Similarity.SimWeight stats, AtomicReaderContext context)
        {
            var bm25stats = (BM25Stats)stats;
            var norms = context.Reader.GetNormValues(bm25stats.Field);
            return norms == null
              ? new ExactBM25DocScorerNoNorms(bm25stats)
              : new ExactBM25DocScorer(bm25stats, norms);
        }

        public sealed override Similarity.SloppySimScorer SloppySimScorer(Similarity.SimWeight stats, AtomicReaderContext context)
        {
            var bm25stats = (BM25Stats)stats;
            return new SloppyBM25DocScorer(bm25stats, context.Reader.GetNormValues(bm25stats.Field));
        }

        private class ExactBM25DocScorer : ExactSimScorer
        {
            private readonly BM25Stats stats;
            private readonly float weightValue;
            private readonly NumericDocValues norms;
            private readonly float[] cache;

            ExactBM25DocScorer(BM25Stats stats, NumericDocValues norms)
            {
                //assert norms != null;
                this.stats = stats;
                this.weightValue = stats.weight * (k1 + 1); // boost * idf * (k1 + 1)
                this.cache = stats.cache;
                this.norms = norms;
            }

            public override float Score(int doc, int freq)
            {
                return weightValue * freq / (freq + cache[(byte)norms.get(doc) & 0xFF]);
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return ExplainScore(doc, freq, stats, norms);
            }
        }

        private class ExactBM25DocScorerNoNorms : ExactSimScorer
        {
            private readonly BM25Stats stats;
            private readonly float weightValue;
            private const int SCORE_CACHE_SIZE = 32;
            private float[] scoreCache = new float[SCORE_CACHE_SIZE];

            ExactBM25DocScorerNoNorms(BM25Stats stats)
            {
                this.stats = stats;
                this.weightValue = stats.weight * (k1 + 1); // boost * idf * (k1 + 1)
                for (int i = 0; i < SCORE_CACHE_SIZE; i++)
                    scoreCache[i] = weightValue * i / (i + k1);
            }

            public override float Score(int doc, int freq)
            {
                // TODO: maybe score cache is more trouble than its worth?
                return freq < SCORE_CACHE_SIZE        // check cache
                  ? scoreCache[freq]                  // cache hit
                  : weightValue * freq / (freq + k1); // cache miss
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return ExplainScore(doc, freq, stats, null);
            }
        }

        private class SloppyBM25DocScorer : SloppySimScorer
        {
            private readonly BM25Stats stats;
            private readonly float weightValue; // boost * idf * (k1 + 1)
            private readonly NumericDocValues norms;
            private readonly float[] cache;

            SloppyBM25DocScorer(BM25Stats stats, NumericDocValues norms)
            {
                this.stats = stats;
                this.weightValue = stats.weight * (k1 + 1);
                this.cache = stats.cache;
                this.norms = norms;
            }

            public override float Score(int doc, float freq)
            {
                // if there are no norms, we act as if b=0
                float norm = norms == null ? k1 : cache[(byte)norms.get(doc) & 0xFF];
                return weightValue * freq / (freq + norm);
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return ExplainScore(doc, freq, stats, norms);
            }

            public override float ComputeSlopFactor(int distance)
            {
                return SloppyFreq(distance);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return ScorePayload(doc, start, end, payload);
            }
        }

        private class BM25Stats : SimWeight
        {
            /** BM25's idf */
            private readonly Explanation idf;
            /** The average document length. */
            private readonly float avgdl;
            /** query's inner boost */
            private readonly float queryBoost;
            /** query's outer boost (only for explain) */
            private float topLevelBoost;
            /** weight (idf * boost) */
            private float weight;
            /** field name, for pulling norms */
            private readonly string field;
            /** precomputed norm[256] with k1 * ((1 - b) + b * dl / avgdl) */
            private readonly float[] cache;

            BM25Stats(String field, Explanation idf, float queryBoost, float avgdl, float[] cache)
            {
                this.field = field;
                this.idf = idf;
                this.queryBoost = queryBoost;
                this.avgdl = avgdl;
                this.cache = cache;
            }

            public override float GetValueForNormalization()
            {
                // we return a TF-IDF like normalization to be nice, but we don't actually normalize ourselves.
                float queryWeight = idf.Value * queryBoost;
                return queryWeight * queryWeight;
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                // we don't normalize with queryNorm at all, we just capture the top-level boost
                this.topLevelBoost = topLevelBoost;
                this.weight = idf.Value * queryBoost * topLevelBoost;
            }
        }

        private Explanation ExplainScore(int doc, Explanation freq, BM25Stats stats, NumericDocValues norms)
        {
            Explanation result = new Explanation();
            result.Description = "score(doc=" + doc + ",freq=" + freq + "), product of:";

            Explanation boostExpl = new Explanation(stats.QueryBoost * stats.TopLevelBoost, "boost");
            if (boostExpl.Value != 1.0f)
                result.AddDetail(boostExpl);

            result.AddDetail(stats.Idf);

            Explanation tfNormExpl = new Explanation();
            tfNormExpl.Description = "tfNorm, computed from:";
            tfNormExpl.AddDetail(freq);
            tfNormExpl.AddDetail(new Explanation(k1, "parameter k1"));
            if (norms == null)
            {
                tfNormExpl.AddDetail(new Explanation(0, "parameter b (norms omitted for field)"));
                tfNormExpl.Value = (freq.Value * (k1 + 1)) / (freq.Value + k1);
            }
            else
            {
                var doclen = DecodeNormValue((sbyte)norms.Get(doc));
                tfNormExpl.AddDetail(new Explanation(b, "parameter b"));
                tfNormExpl.AddDetail(new Explanation(stats.avgdl, "avgFieldLength"));
                tfNormExpl.AddDetail(new Explanation(doclen, "fieldLength"));
                tfNormExpl.Value = (freq.Value * (k1 + 1)) / (freq.Value + k1 * (1 - b + b * doclen / stats.avgdl));
            }
            result.AddDetail(tfNormExpl);
            result.Value = boostExpl.Value * stats.Idf.Value * tfNormExpl.Value;
            return result;
        }

        public override string ToString()
        {
            return "BM25(k1=" + k1 + ",b=" + b + ")";
        }

        public float K1 { get { return k1; } }

        public float B { get { return b; } }
    }
}
