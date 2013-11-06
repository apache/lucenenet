using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Sampling;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public sealed class AdaptiveFacetsAccumulator : StandardFacetsAccumulator
    {
        private Sampler sampler = new RandomSampler();

        public AdaptiveFacetsAccumulator(FacetSearchParams searchParams, IndexReader indexReader, TaxonomyReader taxonomyReader)
            : base(searchParams, indexReader, taxonomyReader)
        {
        }

        public AdaptiveFacetsAccumulator(FacetSearchParams searchParams, IndexReader indexReader, TaxonomyReader taxonomyReader, FacetArrays facetArrays)
            : base(searchParams, indexReader, taxonomyReader, facetArrays)
        {
        }

        public Sampler Sampler
        {
            get
            {
                return this.sampler;
            }
            set
            {
                this.sampler = value;
            }
        }

        public override List<FacetResult> Accumulate(IScoredDocIDs docids)
        {
            StandardFacetsAccumulator delegee = AppropriateFacetCountingAccumulator(docids);
            if (delegee == this)
            {
                return base.Accumulate(docids);
            }

            return delegee.Accumulate(docids);
        }

        private StandardFacetsAccumulator AppropriateFacetCountingAccumulator(IScoredDocIDs docids)
        {
            if (!MayComplement())
            {
                return this;
            }

            if (sampler == null || !sampler.ShouldSample(docids))
            {
                return this;
            }

            SamplingAccumulator samplingAccumulator = new SamplingAccumulator(sampler, searchParams, indexReader, taxonomyReader);
            samplingAccumulator.ComplementThreshold = ComplementThreshold;
            return samplingAccumulator;
        }
    }
}
