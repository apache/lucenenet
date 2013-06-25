using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class DefaultSimilarity : TFIDFSimilarity
    {
        public DefaultSimilarity() { }

        public override float Coord(int overlap, int maxOverlap)
        {
            return overlap / (float)maxOverlap;
        }

        public override float QueryNorm(float sumOfSquaredWeights)
        {
            return (float)(1.0 / Math.sqrt(sumOfSquaredWeights));
        }

        public override float LengthNorm(FieldInvertState state)
        {
            int numTerms;
            if (discountOverlaps)
                numTerms = state.getLength() - state.getNumOverlap();
            else
                numTerms = state.getLength();
            return state.getBoost() * ((float)(1.0 / Math.sqrt(numTerms)));
        }

        public override float Tf(float freq)
        {
            return (float)Math.sqrt(freq);
        }

        public override float SloppyFreq(int distance)
        {
            return 1.0f / (distance + 1);
        }

        public override float ScorePayload(int doc, int start, int end, BytesRef payload)
        {
            return 1;
        }

        public override float Idf(long docFreq, long numDocs)
        {
            return (float)(Math.log(numDocs / (double)(docFreq + 1)) + 1.0);
        }

        protected bool discountOverlaps = true;
        public virtual bool DiscountOverlaps { get { return discountOverlaps; } set { discountOverlaps = value; } }

        public override string ToString()
        {
            return "DefaultSimilarity";
        }
    }
}
