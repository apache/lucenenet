using System;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelD : BasicModel
    {
        public override sealed float Score(BasicStats stats, float tfn)
        {
            float F = stats.TotalTermFreq + 1 + tfn;
            double phi = (double) tfn/F;
            double nphi = 1 - phi;
            double p = 1.0/(stats.NumberOfDocuments + 1);
            double D = phi*SimilarityBase.Log2(phi/p) + nphi*SimilarityBase.Log2(nphi/(1 - p));
            return (float) (D*F + 0.5*SimilarityBase.Log2(1 + 2*Math.PI*tfn*nphi));
        }

        public override string ToString()
        {
            return "D";
        }
    }
}