using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class FacetsAccumulator
    {
        public readonly TaxonomyReader taxonomyReader;
        public readonly IndexReader indexReader;
        public readonly FacetArrays facetArrays;
        public FacetSearchParams searchParams;

        public FacetsAccumulator(FacetSearchParams searchParams, IndexReader indexReader, TaxonomyReader taxonomyReader)
            : this(searchParams, indexReader, taxonomyReader, null)
        {
        }

        public static FacetsAccumulator Create(FacetSearchParams fsp, IndexReader indexReader, TaxonomyReader taxoReader)
        {
            if (fsp.indexingParams.PartitionSize != int.MaxValue)
            {
                return new StandardFacetsAccumulator(fsp, indexReader, taxoReader);
            }

            foreach (FacetRequest fr in fsp.facetRequests)
            {
                if (!(fr is CountFacetRequest))
                {
                    return new StandardFacetsAccumulator(fsp, indexReader, taxoReader);
                }
            }

            return new FacetsAccumulator(fsp, indexReader, taxoReader);
        }

        protected static FacetResult EmptyResult(int ordinal, FacetRequest fr)
        {
            FacetResultNode root = new FacetResultNode(ordinal, 0);
            root.label = fr.categoryPath;
            return new FacetResult(fr, root, 0);
        }

        public FacetsAccumulator(FacetSearchParams searchParams, IndexReader indexReader, TaxonomyReader taxonomyReader, FacetArrays facetArrays)
        {
            if (facetArrays == null)
            {
                facetArrays = new FacetArrays(taxonomyReader.Size);
            }

            this.facetArrays = facetArrays;
            this.indexReader = indexReader;
            this.taxonomyReader = taxonomyReader;
            this.searchParams = searchParams;
        }

        public virtual IFacetsAggregator Aggregator
        {
            get
            {
                if (FastCountingFacetsAggregator.VerifySearchParams(searchParams))
                {
                    return new FastCountingFacetsAggregator();
                }
                else
                {
                    return new CountingFacetsAggregator();
                }
            }
        }

        protected virtual FacetResultsHandler CreateFacetResultsHandler(FacetRequest fr)
        {
            if (fr.Depth == 1 && fr.SortOrderValue == FacetRequest.SortOrder.DESCENDING)
            {
                FacetRequest.FacetArraysSource fas = fr.FacetArraysSourceValue;
                if (fas == FacetRequest.FacetArraysSource.INT)
                {
                    return new IntFacetResultsHandler(taxonomyReader, fr, facetArrays);
                }

                if (fas == FacetRequest.FacetArraysSource.FLOAT)
                {
                    return new FloatFacetResultsHandler(taxonomyReader, fr, facetArrays);
                }
            }

            if (fr.ResultModeValue == FacetRequest.ResultMode.PER_NODE_IN_TREE)
            {
                return new TopKInEachNodeHandler(taxonomyReader, fr, facetArrays);
            }

            return new TopKFacetResultsHandler(taxonomyReader, fr, facetArrays);
        }

        protected virtual ISet<CategoryListParams> GetCategoryLists()
        {
            if (searchParams.indexingParams.AllCategoryListParams.Count == 1)
            {
                return new HashSet<CategoryListParams>(new[] { searchParams.indexingParams.GetCategoryListParams(null) });
            }

            HashSet<CategoryListParams> clps = new HashSet<CategoryListParams>();
            foreach (FacetRequest fr in searchParams.facetRequests)
            {
                clps.Add(searchParams.indexingParams.GetCategoryListParams(fr.categoryPath));
            }

            return clps;
        }

        public virtual List<FacetResult> Accumulate(List<FacetsCollector.MatchingDocs> matchingDocs)
        {
            IFacetsAggregator aggregator = Aggregator;
            foreach (CategoryListParams clp in GetCategoryLists())
            {
                foreach (FacetsCollector.MatchingDocs md in matchingDocs)
                {
                    aggregator.Aggregate(md, clp, facetArrays);
                }
            }

            ParallelTaxonomyArrays arrays = taxonomyReader.ParallelTaxonomyArrays;
            int[] children = arrays.Children;
            int[] siblings = arrays.Siblings;
            List<FacetResult> res = new List<FacetResult>();
            foreach (FacetRequest fr in searchParams.facetRequests)
            {
                int rootOrd = taxonomyReader.GetOrdinal(fr.categoryPath);
                if (rootOrd == TaxonomyReader.INVALID_ORDINAL)
                {
                    res.Add(EmptyResult(rootOrd, fr));
                    continue;
                }

                CategoryListParams clp = searchParams.indexingParams.GetCategoryListParams(fr.categoryPath);
                if (fr.categoryPath.length > 0)
                {
                    CategoryListParams.OrdinalPolicy ordinalPolicy = clp.GetOrdinalPolicy(fr.categoryPath.components[0]);
                    if (ordinalPolicy == CategoryListParams.OrdinalPolicy.NO_PARENTS)
                    {
                        aggregator.RollupValues(fr, rootOrd, children, siblings, facetArrays);
                    }
                }

                FacetResultsHandler frh = CreateFacetResultsHandler(fr);
                res.Add(frh.Compute());
            }

            return res;
        }
    }
}
