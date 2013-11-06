using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public sealed class IntFacetResultsHandler : DepthOneFacetResultsHandler
    {
        private readonly int[] values;
        public IntFacetResultsHandler(TaxonomyReader taxonomyReader, FacetRequest facetRequest, FacetArrays facetArrays)
            : base(taxonomyReader, facetRequest, facetArrays)
        {
            this.values = facetArrays.GetIntArray();
        }

        protected override double ValueOf(int ordinal)
        {
            return values[ordinal];
        }

        protected override int AddSiblings(int ordinal, int[] siblings, PriorityQueue<FacetResultNode> pq)
        {
            FacetResultNode top = pq.Top();
            int numResults = 0;
            while (ordinal != TaxonomyReader.INVALID_ORDINAL)
            {
                int value = values[ordinal];
                if (value > 0)
                {
                    ++numResults;
                    if (value > top.value)
                    {
                        top.value = value;
                        top.ordinal = ordinal;
                        top = pq.UpdateTop();
                    }
                }

                ordinal = siblings[ordinal];
            }

            return numResults;
        }

        protected override void AddSiblings(int ordinal, int[] siblings, List<FacetResultNode> nodes)
        {
            while (ordinal != TaxonomyReader.INVALID_ORDINAL)
            {
                int value = values[ordinal];
                if (value > 0)
                {
                    FacetResultNode node = new FacetResultNode(ordinal, value);
                    node.label = taxonomyReader.GetPath(ordinal);
                    nodes.Add(node);
                }

                ordinal = siblings[ordinal];
            }
        }
    }
}
