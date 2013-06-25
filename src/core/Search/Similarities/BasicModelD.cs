using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelD : BasicModel
    {
        public BasicModelD() { }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            double F = stats.TotalTermFreq + 1 + tfn;
            double phi = (double)tfn / F;
            double nphi = 1 - phi;
            double p = 1.0 / (stats.NumberOfDocuments + 1);
            double D = phi * log2(phi / p) + nphi * log2(nphi / (1 - p));
            return (float)(D * F + 0.5 * log2(1 + 2 * Math.PI * tfn * nphi));
        }

        public string ToString()
        {
            return "D";
        }
    }
}
