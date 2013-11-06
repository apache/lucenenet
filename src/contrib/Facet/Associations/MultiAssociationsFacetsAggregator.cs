using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Associations
{
    public class MultiAssociationsFacetsAggregator : IFacetsAggregator
    {
        private readonly IDictionary<CategoryPath, IFacetsAggregator> categoryAggregators;
        private readonly List<IFacetsAggregator> aggregators;

        public MultiAssociationsFacetsAggregator(IDictionary<CategoryPath, IFacetsAggregator> aggregators)
        {
            this.categoryAggregators = aggregators;
            IDictionary<Type, IFacetsAggregator> aggsClasses = new HashMap<Type, IFacetsAggregator>();
            foreach (IFacetsAggregator fa in aggregators.Values)
            {
                aggsClasses[fa.GetType()] = fa;
            }

            this.aggregators = new List<IFacetsAggregator>(aggsClasses.Values);
        }

        public void Aggregate(FacetsCollector.MatchingDocs matchingDocs, CategoryListParams clp, FacetArrays facetArrays)
        {
            foreach (IFacetsAggregator fa in aggregators)
            {
                fa.Aggregate(matchingDocs, clp, facetArrays);
            }
        }

        public void RollupValues(FacetRequest fr, int ordinal, int[] children, int[] siblings, FacetArrays facetArrays)
        {
            categoryAggregators[fr.categoryPath].RollupValues(fr, ordinal, children, siblings, facetArrays);
        }

        public bool RequiresDocScores
        {
            get
            {
                foreach (IFacetsAggregator fa in aggregators)
                {
                    if (fa.RequiresDocScores)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
