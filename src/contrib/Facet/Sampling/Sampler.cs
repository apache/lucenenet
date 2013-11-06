using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Sampling
{
    public abstract class Sampler
    {
        protected readonly SamplingParams samplingParams;
        public Sampler()
            : this(new SamplingParams())
        {
        }

        public Sampler(SamplingParams params_renamed)
        {
            if (!params_renamed.Validate())
            {
                throw new ArgumentException(@"The provided SamplingParams are not valid!!");
            }

            this.samplingParams = params_renamed;
        }

        public virtual bool ShouldSample(IScoredDocIDs docIds)
        {
            return docIds.Size > samplingParams.SamplingThreshold;
        }

        public virtual SampleResult GetSampleSet(IScoredDocIDs docids)
        {
            if (!ShouldSample(docids))
            {
                return new SampleResult(docids, 1.0);
            }

            int actualSize = docids.Size;
            int sampleSetSize = (int)(actualSize * samplingParams.SampleRatio);
            sampleSetSize = Math.Max(sampleSetSize, samplingParams.MinSampleSize);
            sampleSetSize = Math.Min(sampleSetSize, samplingParams.MaxSampleSize);
            return CreateSample(docids, actualSize, sampleSetSize);
        }

        protected abstract SampleResult CreateSample(IScoredDocIDs docids, int actualSize, int sampleSetSize);

        public virtual ISampleFixer GetSampleFixer(IndexReader indexReader, TaxonomyReader taxonomyReader, FacetSearchParams searchParams)
        {
            return new TakmiSampleFixer(indexReader, taxonomyReader, searchParams);
        }

        public sealed class SampleResult
        {
            public readonly IScoredDocIDs docids;
            public readonly double actualSampleRatio;

            internal SampleResult(IScoredDocIDs docids, double actualSampleRatio)
            {
                this.docids = docids;
                this.actualSampleRatio = actualSampleRatio;
            }
        }

        public SamplingParams SamplingParams
        {
            get
            {
                return samplingParams;
            }
        }

        public virtual FacetResult TrimResult(FacetResult facetResult)
        {
            double overSampleFactor = SamplingParams.OversampleFactor;
            if (overSampleFactor <= 1)
            {
                return facetResult;
            }

            OverSampledFacetRequest sampledFreq = null;
            try
            {
                sampledFreq = (OverSampledFacetRequest)facetResult.FacetRequest;
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException(@"It is only valid to call this method with result obtained for a " + @"facet request created through sampler.overSamlpingSearchParams()", e);
            }

            FacetRequest origFrq = sampledFreq.orig;
            FacetResultNode trimmedRootNode = facetResult.FacetResultNode;
            TrimSubResults(trimmedRootNode, origFrq.numResults);
            return new FacetResult(origFrq, trimmedRootNode, facetResult.NumValidDescendants);
        }

        private void TrimSubResults(FacetResultNode node, int size)
        {
            if (node.subResults == FacetResultNode.EMPTY_SUB_RESULTS || node.subResults.Count == 0)
            {
                return;
            }

            List<FacetResultNode> trimmed = new List<FacetResultNode>(size);
            for (int i = 0; i < node.subResults.Count && i < size; i++)
            {
                FacetResultNode trimmedNode = node.subResults[i];
                TrimSubResults(trimmedNode, size);
                trimmed.Add(trimmedNode);
            }

            node.subResults = trimmed;
        }

        public virtual FacetSearchParams OverSampledSearchParams(FacetSearchParams original)
        {
            FacetSearchParams res = original;
            double overSampleFactor = SamplingParams.OversampleFactor;
            if (overSampleFactor > 1)
            {
                List<FacetRequest> facetRequests = new List<FacetRequest>();
                foreach (FacetRequest frq in original.facetRequests)
                {
                    int overSampledNumResults = (int)Math.Ceiling(frq.numResults * overSampleFactor);
                    facetRequests.Add(new OverSampledFacetRequest(frq, overSampledNumResults));
                }

                res = new FacetSearchParams(original.indexingParams, facetRequests);
            }

            return res;
        }

        private class OverSampledFacetRequest : FacetRequest
        {
            internal readonly FacetRequest orig;

            public OverSampledFacetRequest(FacetRequest orig, int num)
                : base(orig.categoryPath, num)
            {
                this.orig = orig;
                Depth = orig.Depth;
                NumLabel = orig.NumLabel;
                ResultModeValue = orig.ResultModeValue;
                SortOrderValue = orig.SortOrderValue;
            }

            public override IAggregator CreateAggregator(bool useComplements, FacetArrays arrays, TaxonomyReader taxonomy)
            {
                return orig.CreateAggregator(useComplements, arrays, taxonomy);
            }

            public override FacetArraysSource FacetArraysSourceValue
            {
                get
                {
                    return orig.FacetArraysSourceValue;
                }
            }

            public override double GetValueOf(FacetArrays arrays, int idx)
            {
                return orig.GetValueOf(arrays, idx);
            }
        }
    }
}
