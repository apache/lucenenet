using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelBE : BasicModel
    {
        public BasicModelBE() { }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            double F = stats.TotalTermFreq + 1 + tfn;
            double N = F + stats.NumberOfDocuments;
            return (float)(-log2((N - 1) * Math.E)
                + f(N + F - 1, N + F - tfn - 2) - f(F, F - tfn));
        }

        private sealed double f(double n, double m)
        {
            return (m + 0.5) * log2(n / m) + (n - m) * log2(n);
        }

        public override string ToString()
        {
            return "Be";
        }
    }
}
