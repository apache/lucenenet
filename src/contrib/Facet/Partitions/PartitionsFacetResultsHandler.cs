using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Partitions
{
    public abstract class PartitionsFacetResultsHandler : FacetResultsHandler
    {
        public PartitionsFacetResultsHandler(TaxonomyReader taxonomyReader, FacetRequest facetRequest, FacetArrays facetArrays)
            : base(taxonomyReader, facetRequest, facetArrays)
        {
        }

        public abstract IIntermediateFacetResult FetchPartitionResult(int offset);
        public abstract IIntermediateFacetResult MergeResults(params IIntermediateFacetResult[] tmpResults);
        public abstract FacetResult RenderFacetResult(IIntermediateFacetResult tmpResult);
        public abstract FacetResult RearrangeFacetResult(FacetResult facetResult);
        public abstract void LabelResult(FacetResult facetResult);

        protected virtual bool IsSelfPartition(int ordinal, FacetArrays facetArrays, int offset)
        {
            int partitionSize = facetArrays.arrayLength;
            return ordinal / partitionSize == offset / partitionSize;
        }

        public override FacetResult Compute()
        {
            FacetResult res = RenderFacetResult(FetchPartitionResult(0));
            LabelResult(res);
            return res;
        }
    }
}
