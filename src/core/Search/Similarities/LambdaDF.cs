using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class LambdaDF : Lambda
    {
        public LambdaDF() { }

        public override sealed float Lambda(BasicStats stats)
        {
            return (stats.DocFreq + 1F) / (stats.NumberOfDocuments + 1F);
        }

        public override sealed Explanation Explain(BasicStats stats)
        {
            var result = new Explanation();
            result.Description = this.GetType().Name + ", computed from: ";
            result.Value = Lambda(stats);
            result.AddDetail(new Explanation(stats.DocFreq, "docFreq"));
            result.AddDetail(new Explanation(stats.NumberOfDocuments, "numberOfDocuments"));
            return result;
        }

        public override string ToString()
        {
            return "D";
        }
    }
}
