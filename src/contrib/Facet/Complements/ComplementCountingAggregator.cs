using Lucene.Net.Facet.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Complements
{
    public class ComplementCountingAggregator : CountingAggregator
    {
        public ComplementCountingAggregator(int[] counterArray)
            : base(counterArray)
        {
        }

        public override void Aggregate(int docID, float score, IntsRef ordinals)
        {
            for (int i = 0; i < ordinals.length; i++)
            {
                int ord = ordinals.ints[i];
                --counterArray[ord];
            }
        }
    }
}
