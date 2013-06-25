using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public abstract class Similarity
    {
        public Similarity() { }

        public virtual float Coord(int overlap, int maxOverlap)
        {
            return 1f;
        }

        public virtual float QueryNorm(float valueForNormalization)
        {
            return 1f;
        }

        public abstract long ComputeNorm(FieldInvertState state);

        public abstract SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, TermStatistics[] termStats);

        public abstract ExactSimScorer ExactSimScorer(SimWeight weight, AtomicReaderContext context);

        public abstract SloppySimScorer SloppySimScorer(SimWeight weight, AtomicReaderContext context);

        public abstract class ExactSimScorer
        {
            public ExactSimScorer() { }

            public abstract float Score(int doc, int freq);

            public virtual Explanation Explain(int doc, Explanation freq)
            {
                var result = new Explanation(Score(doc, (int)freq.Value),
                    "score(doc=" + doc + ",freq=" + freq.Value + "), with freq of:");
                result.AddDetail(freq);
                return result;
            }
        }

        public abstract class SloppySimScorer
        {
            public SloppySimScorer() { }

            public abstract float ComputSlopFactor(int distance);

            public abstract float ComputePayloadFactor(int doc, int start, int end, BytesRef payload);

            public virtual Explanation Explain(int doc, Explanation freq)
            {
                var result = new Explanation(Score(doc, freq.Value),
                    "score(doc=" + doc + ",freq=" + freq.Value + "), with freq of:");
                result.AddDetail(freq);
                return result;
            }
        }

        public abstract class SimWeight
        {
            public SimWeight() { }

            public abstract float GetValueForNormalization();

            public abstract void Normalize(float queryNorm, float topLevelBoost);
        }
    }
}
