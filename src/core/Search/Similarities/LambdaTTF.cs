using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class LambdaTTF : Lambda
    {
        public LambdaTTF() { }

        public override sealed float Lambda(BasicStats stats)
        {
            return (stats.TotalTermFreq + 1F) / (stats.NumberOfDocuments + 1F);
        }

        public override sealed Explanation Explain(BasicStats stats)
        {
            var result = new Explanation();
            result.Description = this.GetType().Name + ", computed from: ";
            result.Value = Lambda(stats);
            result.AddDetail(new Explanation(stats.TotalTermFreq, "totalTermFreq"));
            result.AddDetail(new Explanation(stats.NumberOfDocuments, "numberOfDocuments"));
            return result;
        }

        public override string ToString()
        {
            return "L";
        }
    }
}
