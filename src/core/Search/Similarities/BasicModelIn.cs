using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelIn : BasicModel
    {
        public BasicModelIn() { }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            long N = stats.NumberOfDocuments;
            long n = stats.DocFreq;
            return tfn * (float)(log2((N + 1) / (n + 0.5)));
        }

        public override sealed Explanation Explain(BasicStats stats, float tfn)
        {
            Explanation result = new Explanation();
            result.Description = this.GetType().Name + ", computed from: ";
            result.Value = Score(stats, tfn);
            result.AddDetail(new Explanation(tfn, "tfn"));
            result.AddDetail(
                new Explanation(stats.NumberOfDocuments, "numberOfDocuments"));
            result.AddDetail(
                new Explanation(stats.DocFreq, "docFreq"));
            return result;
        }

        public override string ToString()
        {
            return "I(n)";
        }
    }
}
