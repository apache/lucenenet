using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Partitions;
using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Sampling
{
    public class SamplingAccumulator : StandardFacetsAccumulator
    {
        private double samplingRatio = -1.0;
        private readonly Sampler sampler;

        public SamplingAccumulator(Sampler sampler, FacetSearchParams searchParams, IndexReader indexReader, TaxonomyReader taxonomyReader, FacetArrays facetArrays)
            : base(searchParams, indexReader, taxonomyReader, facetArrays)
        {
            this.sampler = sampler;
        }

        public SamplingAccumulator(Sampler sampler, FacetSearchParams searchParams, IndexReader indexReader, TaxonomyReader taxonomyReader)
            : base(searchParams, indexReader, taxonomyReader)
        {
            this.sampler = sampler;
        }

        public override List<FacetResult> Accumulate(IScoredDocIDs docids)
        {
            FacetSearchParams original = searchParams;
            searchParams = sampler.OverSampledSearchParams(original);
            List<FacetResult> sampleRes = base.Accumulate(docids);
            List<FacetResult> fixedRes = new List<FacetResult>();
            foreach (FacetResult fres in sampleRes)
            {
                var freswritable = fres;

                PartitionsFacetResultsHandler frh = (PartitionsFacetResultsHandler)CreateFacetResultsHandler(freswritable.FacetRequest);
                sampler.GetSampleFixer(indexReader, taxonomyReader, searchParams).FixResult(docids, freswritable);
                freswritable = frh.RearrangeFacetResult(freswritable);
                freswritable = sampler.TrimResult(freswritable);
                frh.LabelResult(freswritable);
                fixedRes.Add(freswritable);
            }

            searchParams = original;
            return fixedRes;
        }

        protected override IScoredDocIDs ActualDocsToAccumulate(IScoredDocIDs docids)
        {
            Sampler.SampleResult sampleRes = sampler.GetSampleSet(docids);
            samplingRatio = sampleRes.actualSampleRatio;
            return sampleRes.docids;
        }

        protected override double TotalCountsFactor
        {
            get
            {
                if (samplingRatio < 0)
                {
                    throw new InvalidOperationException(@"Total counts ratio unavailable because actualDocsToAccumulate() was not invoked");
                }

                return samplingRatio;
            }
        }
    }
}
