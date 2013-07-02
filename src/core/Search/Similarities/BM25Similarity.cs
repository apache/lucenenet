using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Similarities
{
    public class BM25Similarity : Similarity
    {
        private static readonly float[] NORM_TABLE = new float[256];
        private readonly float b;
        private readonly float k1;
        protected bool discountOverlaps = true;

        static BM25Similarity()
        {
            for (int i = 0; i < 256; i++)
            {
                float f = SmallFloat.Byte315ToFloat((sbyte) i);
                NORM_TABLE[i] = 1.0f/(f*f);
            }
        }

        public BM25Similarity(float k1, float b)
        {
            this.k1 = k1;
            this.b = b;
        }

        public BM25Similarity()
        {
            k1 = 1.2f;
            b = 0.75f;
        }

        public virtual bool DiscountOverlaps
        {
            get { return discountOverlaps; }
            set { discountOverlaps = value; }
        }

        public float K1
        {
            get { return k1; }
        }

        public float B
        {
            get { return b; }
        }

        protected virtual float Idf(long docFreq, long numDocs)
        {
            return (float) Math.Log(1 + (numDocs - docFreq + 0.5D)/(docFreq + 0.5D));
        }

        protected virtual float SloppyFreq(int distance)
        {
            return 1.0f/(distance + 1);
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
                return (float) (sumTotalTermFreq/(double) collectionStats.MaxDoc);
        }

        protected virtual sbyte EncodeNormValue(float boost, int fieldLength)
        {
            return SmallFloat.FloatToByte315(boost/(float) Math.Sqrt(fieldLength));
        }

        protected virtual float DecodeNormValue(sbyte b)
        {
            return NORM_TABLE[b & 0xFF];
        }

        public override sealed long ComputeNorm(FieldInvertState state)
        {
            int numTerms = discountOverlaps ? state.Length - state.NumOverlap : state.Length;
            return EncodeNormValue(state.Boost, numTerms);
        }

        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics termStats)
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
            var exp = new Explanation();
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

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats,
                                                       params TermStatistics[] termStats)
        {
            Explanation idf = termStats.Length == 1
                                  ? IdfExplain(collectionStats, termStats[0])
                                  : IdfExplain(collectionStats, termStats);

            float avgdl = AvgFieldLength(collectionStats);

            var cache = new float[256];
            for (int i = 0; i < cache.Length; i++)
            {
                cache[i] = k1*((1 - b) + b*DecodeNormValue((sbyte) i)/avgdl);
            }
            return new BM25Stats(collectionStats.Field, idf, queryBoost, avgdl, cache);
        }

        public override sealed ExactSimScorer GetExactSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var bm25stats = (BM25Stats) stats;
            NumericDocValues norms = context.Reader.GetNormValues(bm25stats.Field);
            return norms == null
                       ? new ExactBM25DocScorerNoNorms(bm25stats, this)
                       : new ExactBM25DocScorer(bm25stats, norms, this) as ExactSimScorer;
        }

        public override sealed SloppySimScorer GetSloppySimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var bm25stats = (BM25Stats) stats;
            return new SloppyBM25DocScorer(bm25stats, context.Reader.GetNormValues(bm25stats.Field), this);
        }

        private Explanation ExplainScore(int doc, Explanation freq, BM25Stats stats, NumericDocValues norms)
        {
            var result = new Explanation {Description = "score(doc=" + doc + ",freq=" + freq + "), product of:"};

            var boostExpl = new Explanation(stats.QueryBoost*stats.TopLevelBoost, "boost");
            if (boostExpl.Value != 1.0f)
                result.AddDetail(boostExpl);

            result.AddDetail(stats.Idf);

            var tfNormExpl = new Explanation {Description = "tfNorm, computed from:"};
            tfNormExpl.AddDetail(freq);
            tfNormExpl.AddDetail(new Explanation(k1, "parameter k1"));
            if (norms == null)
            {
                tfNormExpl.AddDetail(new Explanation(0, "parameter b (norms omitted for field)"));
                tfNormExpl.Value = (freq.Value*(k1 + 1))/(freq.Value + k1);
            }
            else
            {
                float doclen = DecodeNormValue((sbyte) norms.Get(doc));
                tfNormExpl.AddDetail(new Explanation(b, "parameter b"));
                tfNormExpl.AddDetail(new Explanation(stats.Avgdl, "avgFieldLength"));
                tfNormExpl.AddDetail(new Explanation(doclen, "fieldLength"));
                tfNormExpl.Value = (freq.Value*(k1 + 1))/(freq.Value + k1*(1 - b + b*doclen/stats.Avgdl));
            }
            result.AddDetail(tfNormExpl);
            result.Value = boostExpl.Value*stats.Idf.Value*tfNormExpl.Value;
            return result;
        }

        public override string ToString()
        {
            return "BM25(k1=" + k1 + ",b=" + b + ")";
        }

        private class BM25Stats : SimWeight
        {
            /** BM25's idf */
            private readonly float avgdl;
            private readonly float[] cache;
            private readonly string field;
            private readonly Explanation idf;
            private readonly float queryBoost;
            private float topLevelBoost;
            private float weight;

            public BM25Stats(String field, Explanation idf, float queryBoost, float avgdl, float[] cache)
            {
                this.field = field;
                this.idf = idf;
                this.queryBoost = queryBoost;
                this.avgdl = avgdl;
                this.cache = cache;
            }

            public Explanation Idf
            {
                get { return idf; }
            }

            /** The average document length. */

            public float Avgdl
            {
                get { return avgdl; }
            }

            /** query's inner boost */

            public float QueryBoost
            {
                get { return queryBoost; }
            }

            /** query's outer boost (only for explain) */

            public float TopLevelBoost
            {
                get { return topLevelBoost; }
            }

            /** weight (idf * boost) */

            public float Weight
            {
                get { return weight; }
            }

            /** field name, for pulling norms */

            public string Field
            {
                get { return field; }
            }

            /** precomputed norm[256] with k1 * ((1 - b) + b * dl / avgdl) */

            public float[] Cache
            {
                get { return cache; }
            }

            public override float GetValueForNormalization()
            {
                // we return a TF-IDF like normalization to be nice, but we don't actually normalize ourselves.
                float queryWeight = idf.Value*queryBoost;
                return queryWeight*queryWeight;
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                // we don't normalize with queryNorm at all, we just capture the top-level boost
                this.topLevelBoost = topLevelBoost;
                weight = idf.Value*queryBoost*topLevelBoost;
            }
        }

        private class ExactBM25DocScorer : ExactSimScorer
        {
            private readonly float[] cache;
            private readonly NumericDocValues norms;
            private readonly BM25Similarity parent;
            private readonly BM25Stats stats;
            private readonly float weightValue;

            public ExactBM25DocScorer(BM25Stats stats, NumericDocValues norms, BM25Similarity parent)
            {
                //assert norms != null;
                this.stats = stats;
                weightValue = stats.Weight*(parent.k1 + 1); // boost * idf * (k1 + 1)
                cache = stats.Cache;
                this.norms = norms;
                this.parent = parent;
            }

            public override float Score(int doc, int freq)
            {
                return weightValue*freq/(freq + cache[(byte) norms.Get(doc) & 0xFF]);
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return parent.ExplainScore(doc, freq, stats, norms);
            }
        }

        private class ExactBM25DocScorerNoNorms : ExactSimScorer
        {
            private const int SCORE_CACHE_SIZE = 32;
            private readonly BM25Similarity parent;
            private readonly float[] scoreCache = new float[SCORE_CACHE_SIZE];
            private readonly BM25Stats stats;
            private readonly float weightValue;

            public ExactBM25DocScorerNoNorms(BM25Stats stats, BM25Similarity parent)
            {
                this.stats = stats;
                weightValue = stats.Weight*(parent.k1 + 1); // boost * idf * (k1 + 1)
                for (int i = 0; i < SCORE_CACHE_SIZE; i++)
                    scoreCache[i] = weightValue*i/(i + parent.k1);
                this.parent = parent;
            }

            public override float Score(int doc, int freq)
            {
                // TODO: maybe score cache is more trouble than its worth?
                return freq < SCORE_CACHE_SIZE // check cache
                           ? scoreCache[freq] // cache hit
                           : weightValue*freq/(freq + parent.k1); // cache miss
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return parent.ExplainScore(doc, freq, stats, null);
            }
        }

        private class SloppyBM25DocScorer : SloppySimScorer
        {
            private readonly float[] cache;
            private readonly NumericDocValues norms;
            private readonly BM25Similarity parent;
            private readonly BM25Stats stats;
            private readonly float weightValue; // boost * idf * (k1 + 1)

            public SloppyBM25DocScorer(BM25Stats stats, NumericDocValues norms, BM25Similarity parent)
            {
                this.stats = stats;
                weightValue = stats.Weight*(parent.k1 + 1);
                cache = stats.Cache;
                this.norms = norms;
                this.parent = parent;
            }

            public override float Score(int doc, float freq)
            {
                // if there are no norms, we act as if b=0
                float norm = norms == null ? parent.k1 : cache[(byte) norms.Get(doc) & 0xFF];
                return weightValue*freq/(freq + norm);
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return parent.ExplainScore(doc, freq, stats, norms);
            }

            public override float ComputeSlopFactor(int distance)
            {
                return parent.SloppyFreq(distance);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return parent.ScorePayload(doc, start, end, payload);
            }
        }
    }
}