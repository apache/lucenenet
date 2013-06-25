using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelP : BasicModel
    {
        protected static double LOG2_E = log2(Math.E);

        public BasicModelP() { }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            float lambda = (float)(stats.TotalTermFreq + 1) / (stats.NumberOfDocuments + 1);
            return (float)(tfn * log2(tfn / lambda)
                + (lambda + 1 / (12 * tfn) - tfn) * LOG2_E
                + 0.5 * log2(2 * Math.PI * tfn));
        }

        public override string ToString()
        {
            return "P";
        }
    }
}
