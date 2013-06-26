using System;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelIne : BasicModel
    {
        public override sealed float Score(BasicStats stats, float tfn)
        {
            long N = stats.NumberOfDocuments;
            long F = stats.TotalTermFreq;
            double ne = N*(1 - Math.Pow((N - 1)/(double) N, F));
            return tfn*(float) (SimilarityBase.Log2((N + 1)/(ne + 0.5)));
        }

        public override string ToString()
        {
            return "I(ne)";
        }
    }
}