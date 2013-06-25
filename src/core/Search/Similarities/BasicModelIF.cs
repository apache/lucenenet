using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelIF : BasicModel
    {
        public BasicModelIF() { }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            long N = stats.NumberOfDocuments;
            long F = stats.TotalTermFreq;
            return tfn * (float)(log2(1 + (N + 1) / (F + 0.5)));
        }

        public override string ToString()
        {
            return "I(F)";
        }
    }
}
