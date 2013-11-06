using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public abstract class IntRollupFacetsAggregator : IFacetsAggregator
    {
        public abstract void Aggregate(FacetsCollector.MatchingDocs matchingDocs, CategoryListParams clp, FacetArrays facetArrays);

        private int RollupValues(int ordinal, int[] children, int[] siblings, int[] values)
        {
            int value = 0;
            while (ordinal != TaxonomyReader.INVALID_ORDINAL)
            {
                int childValue = values[ordinal];
                childValue += RollupValues(children[ordinal], children, siblings, values);
                values[ordinal] = childValue;
                value += childValue;
                ordinal = siblings[ordinal];
            }

            return value;
        }

        public void RollupValues(FacetRequest fr, int ordinal, int[] children, int[] siblings, FacetArrays facetArrays)
        {
            int[] values = facetArrays.GetIntArray();
            values[ordinal] += RollupValues(children[ordinal], children, siblings, values);
        }

        public bool RequiresDocScores
        {
            get
            {
                return false;
            }
        }
    }
}
