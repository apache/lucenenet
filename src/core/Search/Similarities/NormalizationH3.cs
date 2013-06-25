using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class NormalizationH3 : Normalization
    {
        private readonly float mu;
        public float Mu { get { return mu; } }

        public NormalizationH3()
        {
            this(800F);
        }

        public NormalizationH3(float mu)
        {
            this.mu = mu;
        }

        public override float Tfn(BasicStats stats, float tf, float len)
        {
            return (tf + mu * ((stats.TotalTermFreq + 1F) / (stats.NumberOfFieldTokens + 1F))) / (len + mu) * mu;
        }

        public override string ToString()
        {
            return "3(" + mu + ")";
        }
    }
}
