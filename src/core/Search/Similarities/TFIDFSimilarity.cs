using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public abstract class TFIDFSimilarity : Similarity
    {
        public TFIDFSimilarity() { }

        public override abstract float Coord(int overlap, int maxOverlap);

        public override abstract float QueryNorm(float sumOfSquaredWeights);

        public float Tf(int freq)
        {
            return Tf((float)freq);
        }

        public abstract float Tf(float freq);

        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics termStats)
        {
            long df = termStats.DocFreq;
            long max = collectionStats.MaxDoc;
            float idf = Idf(df, max);
            return new Explanation(idf, "idf(docFreq=" + df + ", maxDocs=" + max + ")");
        }

        public virtual Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
        {
            var max = collectionStats.MaxDoc;
            var idf = 0.0f;
            var exp = new Explanation();
            exp.Description = "idf(), sum of:";
            foreach (var stat in termStats)
            {
                var df = stat.DocFreq;
                var termIdf = Idf(df, max);
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
            var normValue = LengthNorm(state);
            return EncodeNormValue(normValue);
        }

        private static readonly float[] NORM_TABLE = new float[256];

        static TFIDFSimilarity()
        {
            for (var i = 0; i < 256; i++)
            {
                NORM_TABLE[i] = SmallFloat.Byte315ToFloat((byte)i);
            }
        }

        public float DecodeNormValue(byte b)
        {
            return NORM_TABLE[b & 0xFF];  // & 0xFF maps negative bytes to positive above 127
        }

        public byte EncodeNormValue(float f)
        {
            return (byte)SmallFloat.FloatToByte315(f);
        }

        public abstract float SloppyFreq(int distance);

        public abstract float ScorePayload(int doc, int start, int end, BytesRef payload);

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
        {
            var idf = termStats.Length == 1
                ? IdfExplain(collectionStats, termStats[0])
                : IdfExplain(collectionStats, termStats);
            return new IDFStats(collectionStats.Field, idf, queryBoost);
        }

        public override sealed ExactSimScorer ExactSimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var idfstats = (IDFStats)stats;
            return new ExactTFIDFDocScorer(idfstats, context.Reader.GetNormValues(idfstats.field));
        }

        public override sealed SloppySimScorer SloppySimScorer(SimWeight stats, AtomicReaderContext context)
        {
            var idfstats = (IDFStats)stats;
            return new SloppyTFIDFDocScorer(idfstats, context.Reader.GetNormValues(idfstats.field));
        }

        // TODO: we can specialize these for omitNorms up front, but we should test that it doesn't confuse stupid hotspot.

        private sealed class ExactTFIDFDocScorer : ExactSimScorer
        {
            private readonly IDFStats stats;
            private readonly float weightValue;
            private readonly NumericDocValues norms;

            ExactTFIDFDocScorer(IDFStats stats, NumericDocValues norms)
            {
                this.stats = stats;
                this.weightValue = stats.value;
                this.norms = norms;
            }

            public override float Score(int doc, int freq)
            {
                float raw = Tf(freq) * weightValue;  // compute tf(f)*weight

                return norms == null ? raw : raw * DecodeNormValue((sbyte)norms.Get(doc)); // normalize for field
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return ExplainScore(doc, freq, stats, norms);
            }
        }

        private sealed class SloppyTFIDFDocScorer : SloppySimScorer
        {
            private readonly IDFStats stats;
            private readonly float weightValue;
            private readonly NumericDocValues norms;

            SloppyTFIDFDocScorer(IDFStats stats, NumericDocValues norms)
            {
                this.stats = stats;
                this.weightValue = stats.value;
                this.norms = norms;
            }

            public override float Score(int doc, float freq)
            {
                var raw = Tf(freq) * weightValue; // compute tf(f)*weight

                return norms == null ? raw : raw * DecodeNormValue((sbyte)norms.Get(doc));  // normalize for field
            }

            public override float ComputeSlopFactor(int distance)
            {
                return SloppyFreq(distance);
            }

            public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
            {
                return ScorePayload(doc, start, end, payload);
            }

            public override Explanation Explain(int doc, Explanation freq)
            {
                return ExplainScore(doc, freq, stats, norms);
            }
        }

        private class IDFStats : SimWeight
        {
            private readonly string field;
            /** The idf and its explanation */
            private readonly Explanation idf;
            private float queryNorm;
            private float queryWeight;
            private readonly float queryBoost;
            internal float value;

            public IDFStats(String field, Explanation idf, float queryBoost)
            {
                // TODO: Validate?
                this.field = field;
                this.idf = idf;
                this.queryBoost = queryBoost;
                this.queryWeight = idf.Value * queryBoost; // compute query weight
            }

            public override float GetValueForNormalization()
            {
                // TODO: (sorta LUCENE-1907) make non-static class and expose this squaring via a nice method to subclasses?
                return queryWeight * queryWeight;  // sum of squared weights
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                this.queryNorm = queryNorm * topLevelBoost;
                queryWeight *= this.queryNorm;              // normalize query weight
                value = queryWeight * idf.Value;         // idf for document
            }
        }

        private Explanation ExplainScore(int doc, Explanation freq, IDFStats stats, NumericDocValues norms)
        {
            var result = new Explanation();
            result.Description = "score(doc=" + doc + ",freq=" + freq + "), product of:";

            // explain query weight
            var queryExpl = new Explanation();
            queryExpl.Description = "queryWeight, product of:";

            var boostExpl = new Explanation(stats.queryBoost, "boost");
            if (stats.queryBoost != 1.0f)
                queryExpl.AddDetail(boostExpl);
            queryExpl.AddDetail(stats.idf);

            var queryNormExpl = new Explanation(stats.queryNorm, "queryNorm");
            queryExpl.AddDetail(queryNormExpl);

            queryExpl.Value = boostExpl.Value * stats.idf.Value * queryNormExpl.Value;

            result.AddDetail(queryExpl);

            // explain field weight
            var fieldExpl = new Explanation();
            fieldExpl.Description = "fieldWeight in " + doc + ", product of:";

            var tfExplanation = new Explanation();
            tfExplanation.Value = Tf(freq.Value);
            tfExplanation.Description = "tf(freq=" + freq.Value + "), with freq of:";
            tfExplanation.AddDetail(freq);
            fieldExpl.AddDetail(tfExplanation);
            fieldExpl.AddDetail(stats.idf);

            var fieldNormExpl = new Explanation();
            float fieldNorm = norms != null ? DecodeNormValue((sbyte)norms.Get(doc)) : 1.0f;
            fieldNormExpl.Value = fieldNorm;
            fieldNormExpl.Description = "fieldNorm(doc=" + doc + ")";
            fieldExpl.AddDetail(fieldNormExpl);

            fieldExpl.Value = tfExplanation.Value * stats.Idf.Value * fieldNormExpl.Value;

            result.AddDetail(fieldExpl);

            // combine them
            result.Value = queryExpl.Value * fieldExpl.Value;

            if (queryExpl.Value == 1.0f)
                return fieldExpl;

            return result;
        }
    }
}
