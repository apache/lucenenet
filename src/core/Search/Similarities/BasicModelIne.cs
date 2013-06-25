using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelIne : BasicModel
    {
        public BasicModelIne() { }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            long N = stats.NumberOfDocuments;
            long F = stats.TotalTermFreq;
            double ne = N * (1 - Math.pow((N - 1) / (double)N, F));
            return tfn * (float)(log2((N + 1) / (ne + 0.5)));
        }

        public override string ToString()
        {
            return "I(ne)";
        }
    }
}
