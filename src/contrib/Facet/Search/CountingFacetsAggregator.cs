using Lucene.Net.Facet.Params;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class CountingFacetsAggregator : IntRollupFacetsAggregator
    {
        private readonly IntsRef ordinals = new IntsRef(32);

        public override void Aggregate(FacetsCollector.MatchingDocs matchingDocs, CategoryListParams clp, FacetArrays facetArrays)
        {
            ICategoryListIterator cli = clp.CreateCategoryListIterator(0);
            if (!cli.SetNextReader(matchingDocs.context))
            {
                return;
            }

            int length = matchingDocs.bits.Length;
            int[] counts = facetArrays.GetIntArray();
            int doc = 0;
            while (doc < length && (doc = matchingDocs.bits.NextSetBit(doc)) != -1)
            {
                cli.GetOrdinals(doc, ordinals);
                int upto = ordinals.offset + ordinals.length;
                for (int i = ordinals.offset; i < upto; i++)
                {
                    ++counts[ordinals.ints[i]];
                }

                ++doc;
            }
        }
    }
}
