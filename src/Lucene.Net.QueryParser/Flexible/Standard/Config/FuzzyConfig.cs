using Lucene.Net.Search;

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

        public virtual int PrefixLength
        {
            get { return prefixLength; }
            set { this.prefixLength = value; }
        }

        public virtual float MinSimilarity
        {
            get { return minSimilarity; }
            set { this.minSimilarity = value; }
        }
    }
}
