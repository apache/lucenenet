using Lucene.Net.Facet.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Params
{
    public class FacetSearchParams
    {
        public readonly FacetIndexingParams indexingParams;
        public readonly IList<FacetRequest> facetRequests;

        public FacetSearchParams(params FacetRequest[] facetRequests)
            : this(FacetIndexingParams.DEFAULT, facetRequests)
        {
        }

        public FacetSearchParams(IList<FacetRequest> facetRequests)
            : this(FacetIndexingParams.DEFAULT, facetRequests)
        {
        }

        public FacetSearchParams(FacetIndexingParams indexingParams, params FacetRequest[] facetRequests)
            : this(indexingParams, facetRequests.ToList())
        {
        }

        public FacetSearchParams(FacetIndexingParams indexingParams, IList<FacetRequest> facetRequests)
        {
            if (facetRequests == null || facetRequests.Count == 0)
            {
                throw new ArgumentException("at least one FacetRequest must be defined");
            }

            this.facetRequests = facetRequests;
            this.indexingParams = indexingParams;
        }

        public override string ToString()
        {
            string INDENT = "  ";
            char NEWLINE = '\n';
            StringBuilder sb = new StringBuilder("IndexingParams: ");
            sb.Append(NEWLINE).Append(INDENT).Append(indexingParams);
            sb.Append(NEWLINE).Append("FacetRequests:");
            foreach (FacetRequest facetRequest in facetRequests)
            {
                sb.Append(NEWLINE).Append(INDENT).Append(facetRequest);
            }

            return sb.ToString();
        }
    }
}
