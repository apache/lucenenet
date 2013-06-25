using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public abstract class Distribution
    {
        public Distribution() { }

        public abstract float Score(BasicStats stats, float tfn, float lambda);

        public virtual Explanation Explain(BasicStats stats, float tfn, float lambda)
        {
            return new Explanation(Score(stats, tfn, lambda), this.GetType().Name);
        }

        public override abstract string ToString();
    }
}