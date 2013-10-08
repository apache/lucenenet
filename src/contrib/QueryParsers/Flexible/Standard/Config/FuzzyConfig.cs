using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    public class FuzzyConfig
    {
        private int prefixLength = FuzzyQuery.defaultPrefixLength;

        private float minSimilarity = FuzzyQuery.defaultMinSimilarity;

        public FuzzyConfig() { }

        public int PrefixLength
        {
            get { return prefixLength; }
            set { prefixLength = value; }
        }

        public float MinSimilarity
        {
            get { return minSimilarity; }
            set { minSimilarity = value; }
        }
    }
}
