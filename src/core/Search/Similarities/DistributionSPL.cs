using System;

namespace Lucene.Net.Search.Similarities
{
    public class DistributionSPL : Distribution
    {
        public override sealed float Score(BasicStats stats, float tfn, float lambda)
        {
            if (lambda == 1f)
            {
                lambda = 0.99f;
            }
            return (float) -Math.Log((Math.Pow(lambda, (tfn/(tfn + 1))) - lambda)/(1 - lambda));
        }

        public override string ToString()
        {
            return "SPL";
        }
    }
}