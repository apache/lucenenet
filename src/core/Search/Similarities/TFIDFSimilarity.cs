using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Similarities
{
    public abstract class TFIDFSimilarity : Similarity
    {
        private static readonly float[] NORM_TABLE = new float[256];

        static TFIDFSimilarity()
        {
            for (int i = 0; i < 256; i++)
            {
                NORM_TABLE[i] = SmallFloat.Byte315ToFloat((byte) i);
            }
        }

        public abstract override float Coord(int overlap, int maxOverlap);

        public abstract override float QueryNorm(float sumOfSquaredWeights);

        public float Tf(int freq)
        {
            return Tf((float) freq);
        }

        public abstract float Tf(float freq);

        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics termStats)
        {
            var df = termStats.DocFreq;
            var max = collectionStats.MaxDoc;
            float idf = Idf(df, max);
            return new Explanation(idf, "idf(docFreq=" + df + ", maxDocs=" + max + ")");
        }

        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
        {
            var max = collectionStats.MaxDoc;
            float idf = 0.0f;
            var exp = new Explanation {Description = "idf(), sum of:"};
            foreach (var stat in termStats)
            {
                var df = stat.DocFreq;
                float termIdf = Idf(df, max);
                exp.AddDetail(new Explanation(termIdf, "idf(docFreq=" + df + ", maxDocs=" + max + ")"));
                idf += termIdf;
            }
            exp.Value = idf;
            return exp;
        }

        public abstract float Idf(long docFreq, long numDocs);

        public abstract float LengthNorm(FieldInvertState state);

        public override sealed long ComputeNorm(FieldInvertState state)
        {
            float normValue = LengthNorm(state);
            return EncodeNormValue(normValue);
        }

        public float DecodeNormValue(sbyte b)
        {
            return NORM_TABLE[b & 0xFF]; // & 0xFF maps negative bytes to positive above 127
        }

        public byte EncodeNormValue(float f)
        {
            return (byte) SmallFloat.FloatToByte315(f);
        }

        public abstract float SloppyFreq(int distance);

        public abstract float ScorePayload(int doc, int start, int end, BytesRef payload);

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats,
                                                       params TermStatistics[] termStats)
        {
            Explanation idf = termStats.Length == 1
                                  ? IdfExplain(collectionStats, termStats[0])
                                  : IdfExplain(collectionStats, termStats);
            return new IDFStats(collectionStats.Field, idf, queryBoost);
        }

        public override sealed ExactSimScorer GetExactSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var idfstats = (IDFStats) stats;
            return new ExactTFIDFDocScorer(idfstats, context.Reader.GetNormValues(idfstats.Field), this);
        }

        public override sealed SloppySimScorer GetSloppySimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var idfstats = (IDFStats) stats;
            return new SloppyTFIDFDocScorer(idfstats, context.Reader.GetNormValues(idfstats.Field), this);
        }

        private Explanation ExplainScore(int doc, Explanation freq, IDFStats stats, NumericDocValues norms)
        {
            var result = new Explanation {Description = "score(doc=" + doc + ",freq=" + freq + "), product of:"};

            // explain query weight
            var queryExpl = new Explanation {Description = "queryWeight, product of:"};

            var boostExpl = new Explanation(stats.QueryBoost, "boost");
            if (stats.QueryBoost != 1.0f)
                queryExpl.AddDetail(boostExpl);
            queryExpl.AddDetail(stats.Idf);

            var queryNormExpl = new Explanation(stats.QueryNorm, "queryNorm");
            queryExpl.AddDetail(queryNormExpl);

            queryExpl.Value = boostExpl.Value*stats.Idf.Value*queryNormExpl.Value;

            result.AddDetail(queryExpl);

            // explain field weight
            var fieldExpl = new Explanation {Description = "fieldWeight in " + doc + ", product of:"};

            var tfExplanation = new Explanation
                {
                    Value = Tf(freq.Value),
                    Description = "tf(freq=" + freq.Value + "), with freq of:"
                };
            tfExplanation.AddDetail(freq);
            fieldExpl.AddDetail(tfExplanation);
            fieldExpl.AddDetail(stats.Idf);

            var fieldNormExpl = new Explanation();
            float fieldNorm = norms != null ? DecodeNormValue((sbyte) norms.Get(doc)) : 1.0f;
            fieldNormExpl.Value = fieldNorm;
            fieldNormExpl.Description = "fieldNorm(doc=" + doc + ")";
            fieldExpl.AddDetail(fieldNormExpl);

            fieldExpl.Value = tfExplanation.Value*stats.Idf.Value*fieldNormExpl.Value;

            result.AddDetail(fieldExpl);

            // combine them
            result.Value = queryExpl.Value*fieldExpl.Value;

            return queryExpl.Value == 1.0f ? fieldExpl : result;
        }

        // TODO: we can specialize these for omitNorms up front, but we should test that it doesn't confuse stupid hotspot.

        private sealed class ExactTFIDFDocScorer : ExactSimScorer
        {
            private readonly NumericDocValues norms;
            private readonly TFIDFSimilarity parent;
            private readonly IDFStats stats;
            private readonly float weightValue;

            public ExactTFIDFDocScorer(IDFStats stats, NumericDocValues norms, TFIDFSimilarity parent)
            {
                this.stats = stats;
                weightValue = stats.value;
                this.norms = norms;
                this.parent = parent;
            }

            public override float Score(int doc, int freq)
            {
                float raw = parent.Tf(freq)*weightValue; // compute tf(f)*weight

                return norms == null ? raw : raw*parent.DecodeNormValue((sbyte) norms.Get(doc)); // normalize for field
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return parent.ExplainScore(doc, freq, stats, norms);
            }
        }

        private class IDFStats : SimWeight
        {
            private readonly string field;

            /** The idf and its explanation */
            private readonly Explanation idf;
            private readonly float queryBoost;


            private float queryNorm;

            private float queryWeight;
            internal float value;

            public IDFStats(String field, Explanation idf, float queryBoost)
            {
                // TODO: Validate?
                this.field = field;
                this.idf = idf;
                this.queryBoost = queryBoost;
                queryWeight = idf.Value*queryBoost; // compute query weight
            }

            public string Field
            {
                get { return field; }
            }

            public Explanation Idf
            {
                get { return idf; }
            }

            public float QueryNorm
            {
                get { return queryNorm; }
            }

            public float QueryWeight
            {
                get { return queryWeight; }
            }


            public float QueryBoost
            {
                get { return queryBoost; }
            }

            public float Value
            {
                get { return value; }
            }

            public override float GetValueForNormalization()
            {
                // TODO: (sorta LUCENE-1907) make non-static class and expose this squaring via a nice method to subclasses?
                return queryWeight*queryWeight; // sum of squared weights
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                this.queryNorm = queryNorm*topLevelBoost;
                queryWeight *= this.queryNorm; // normalize query weight
                value = queryWeight*idf.Value; // idf for document
            }
        }

        private sealed class SloppyTFIDFDocScorer : SloppySimScorer
        {
            private readonly NumericDocValues norms;

            private readonly TFIDFSimilarity parent;
            private readonly IDFStats stats;
            private readonly float weightValue;

            public SloppyTFIDFDocScorer(IDFStats stats, NumericDocValues norms, TFIDFSimilarity parent)
            {
                this.stats = stats;
                weightValue = stats.value;
                this.norms = norms;
                this.parent = parent;
            }

            public override float Score(int doc, float freq)
            {
                float raw = parent.Tf(freq)*weightValue; // compute tf(f)*weight

                return norms == null ? raw : raw*parent.DecodeNormValue((sbyte) norms.Get(doc)); // normalize for field
            }

            public override float ComputeSlopFactor(int distance)
            {
                return parent.SloppyFreq(distance);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return parent.ScorePayload(doc, start, end, payload);
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return parent.ExplainScore(doc, freq, stats, norms);
            }
        }
    }
}