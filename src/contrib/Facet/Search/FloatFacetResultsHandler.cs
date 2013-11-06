using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public sealed class FloatFacetResultsHandler : DepthOneFacetResultsHandler
    {
        private readonly float[] values;

        public FloatFacetResultsHandler(TaxonomyReader taxonomyReader, FacetRequest facetRequest, FacetArrays facetArrays)
            : base(taxonomyReader, facetRequest, facetArrays)
        {
            this.values = facetArrays.GetFloatArray();
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
                float value = values[ordinal];
                if (value > 0F)
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
                float value = values[ordinal];
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
