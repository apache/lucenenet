using Lucene.Net.Facet.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class CachedOrdsCountingFacetsAggregator : IntRollupFacetsAggregator
    {
        public override void Aggregate(FacetsCollector.MatchingDocs matchingDocs, CategoryListParams clp, FacetArrays facetArrays)
        {
            var ords = OrdinalsCache.GetCachedOrds(matchingDocs.context, clp);
            if (ords == null)
            {
                return;
            }

            int[] counts = facetArrays.GetIntArray();
            int doc = 0;
            int length = matchingDocs.bits.Length;
            while (doc < length && (doc = matchingDocs.bits.NextSetBit(doc)) != -1)
            {
                int start = ords.offsets[doc];
                int end = ords.offsets[doc + 1];
                for (int i = start; i < end; i++)
                {
                    ++counts[ords.ordinals[i]];
                }

                ++doc;
            }
        }
    }
}
