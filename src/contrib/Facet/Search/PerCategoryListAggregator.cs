using Lucene.Net.Facet.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class PerCategoryListAggregator : IFacetsAggregator
    {
        private readonly IDictionary<CategoryListParams, IFacetsAggregator> aggregators;
        private readonly FacetIndexingParams fip;

        public PerCategoryListAggregator(IDictionary<CategoryListParams, IFacetsAggregator> aggregators, FacetIndexingParams fip)
        {
            this.aggregators = aggregators;
            this.fip = fip;
        }

        public void Aggregate(FacetsCollector.MatchingDocs matchingDocs, CategoryListParams clp, FacetArrays facetArrays)
        {
            aggregators[clp].Aggregate(matchingDocs, clp, facetArrays);
        }

        public void RollupValues(FacetRequest fr, int ordinal, int[] children, int[] siblings, FacetArrays facetArrays)
        {
            CategoryListParams clp = fip.GetCategoryListParams(fr.categoryPath);
            aggregators[clp].RollupValues(fr, ordinal, children, siblings, facetArrays);
        }

        public bool RequiresDocScores
        {
            get
            {
                foreach (IFacetsAggregator aggregator in aggregators.Values)
                {
                    if (aggregator.RequiresDocScores)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
