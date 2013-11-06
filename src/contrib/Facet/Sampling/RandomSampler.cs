using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Sampling
{
    public class RandomSampler : Sampler
    {
        private readonly Random random;

        public RandomSampler()
            : base()
        {
            this.random = new Random();
        }

        public RandomSampler(SamplingParams params_renamed, Random random)
            : base(params_renamed)
        {
            this.random = random;
        }

        protected override SampleResult CreateSample(IScoredDocIDs docids, int actualSize, int sampleSetSize)
        {
            int[] sample = new int[sampleSetSize];
            int maxStep = (actualSize * 2) / sampleSetSize;
            int remaining = actualSize;
            IScoredDocIDsIterator it = docids.Iterator();
            int i = 0;
            while (i < sample.Length && remaining > (sampleSetSize - maxStep - i))
            {
                int skipStep = 1 + random.Next(maxStep);
                for (int j = 0; j < skipStep; j++)
                {
                    it.Next();
                    --remaining;
                }

                sample[i++] = it.DocID;
            }

            while (i < sample.Length)
            {
                it.Next();
                sample[i++] = it.DocID;
            }

            IScoredDocIDs sampleRes = ScoredDocIdsUtils.CreateScoredDocIDsSubset(docids, sample);
            SampleResult res = new SampleResult(sampleRes, sampleSetSize / (double)actualSize);
            return res;
        }
    }
}
