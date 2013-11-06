using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public abstract class FacetResultsHandler
    {
        public readonly TaxonomyReader taxonomyReader;
        public readonly FacetRequest facetRequest;
        protected readonly FacetArrays facetArrays;

        public FacetResultsHandler(TaxonomyReader taxonomyReader, FacetRequest facetRequest, FacetArrays facetArrays)
        {
            this.taxonomyReader = taxonomyReader;
            this.facetRequest = facetRequest;
            this.facetArrays = facetArrays;
        }

        public abstract FacetResult Compute();
    }
}
