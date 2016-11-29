using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    /// <summary>
    /// Configuration parameters for {@link FuzzyQuery}s
    /// </summary>
    public class FuzzyConfig
    {
        private int prefixLength = FuzzyQuery.DefaultPrefixLength;

        private float minSimilarity = FuzzyQuery.DefaultMinSimilarity;

        public FuzzyConfig() { }

        public virtual int GetPrefixLength()
        {
            return prefixLength;
        }

        public virtual void SetPrefixLength(int prefixLength)
        {
            this.prefixLength = prefixLength;
        }

        public virtual float GetMinSimilarity()
        {
            return minSimilarity;
        }

        public virtual void SetMinSimilarity(float minSimilarity)
        {
            this.minSimilarity = minSimilarity;
        }
    }
}
