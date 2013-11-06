using Lucene.Net.Facet.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public interface IFacetsAggregator
    {
        void Aggregate(FacetsCollector.MatchingDocs matchingDocs, CategoryListParams clp, FacetArrays facetArrays);

        void RollupValues(FacetRequest fr, int ordinal, int[] children, int[] siblings, FacetArrays facetArrays);

        bool RequiresDocScores { get; }
    }
}
