using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public abstract class BasicModel
    {

        public BasicModel() { }

        public abstract float Score(BasicStats stats, float tfn);

        public virtual Explanation Explain(BasicStats stats, float tfn)
        {
            Explanation result = new Explanation();
            result.Description = this.GetType().Name + ", computed from: ";
            result.Value = Score(stats, tfn);
            result.AddDetail(new Explanation(tfn, "tfn"));
            result.AddDetail(
                new Explanation(stats.NumberOfDocuments, "numberOfDocuments"));
            result.AddDetail(
                new Explanation(stats.TotalTermFreq, "totalTermFreq"));
            return result;
        }

        public override abstract string ToString();
    }
}
