using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Partitions;
using Lucene.Net.Facet.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Sampling
{
    public class SamplingWrapper : StandardFacetsAccumulator
    {
        private StandardFacetsAccumulator delegee;
        private Sampler sampler;

        public SamplingWrapper(StandardFacetsAccumulator delegee, Sampler sampler)
            : base(delegee.searchParams, delegee.indexReader, delegee.taxonomyReader)
        {
            this.delegee = delegee;
            this.sampler = sampler;
        }

        public override List<FacetResult> Accumulate(IScoredDocIDs docids)
        {
            FacetSearchParams original = delegee.searchParams;
            delegee.searchParams = sampler.OverSampledSearchParams(original);
            Sampler.SampleResult sampleSet = sampler.GetSampleSet(docids);
            List<FacetResult> sampleRes = delegee.Accumulate(sampleSet.docids);
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

            delegee.searchParams = original;
            return fixedRes;
        }

        public override double ComplementThreshold
        {
            get
            {
                return delegee.ComplementThreshold;
            }
            set
            {
                delegee.ComplementThreshold = value;
            }
        }
    }
}
