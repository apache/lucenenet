using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelG : BasicModel
    {
        public BasicModelG() { }

        public override sealed float score(BasicStats stats, float tfn)
        {
            double F = stats.TotalTermFreq + 1;
            double N = stats.NumberOfDocuments;
            double lambda = F / (N + F);
            return (float)(log2(lambda + 1) + tfn * log2((1 + lambda) / lambda));
        }

        public override string ToString()
        {
            return "G";
        }
    }
}
