using System;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelBE : BasicModel
    {
        public override sealed float Score(BasicStats stats, float tfn)
        {
            float F = stats.TotalTermFreq + 1 + tfn;
            float N = F + stats.NumberOfDocuments;
            return (float) (-SimilarityBase.Log2((N - 1)*Math.E)
                            + f(N + F - 1, N + F - tfn - 2) - f(F, F - tfn));
        }

        private double f(double n, double m)
        {
            return (m + 0.5)*SimilarityBase.Log2(n/m) + (n - m)*SimilarityBase.Log2(n);
        }

        public override string ToString()
        {
            return "Be";
        }
    }
}