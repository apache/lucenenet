using System;

namespace Lucene.Net.Search.Similarities
{
    public class DistributionLL : Distribution
    {
        public override sealed float Score(BasicStats stats, float tfn, float lambda)
        {
            return (float) -Math.Log(lambda/(tfn + lambda));
        }

        public override string ToString()
        {
            return "LL";
        }
    }
}