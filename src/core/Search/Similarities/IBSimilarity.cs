using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class IBSimilarity : SimilarityBase
    {
        protected readonly Distribution distribution;
        public Distribution Distribution { get { return distribution; } }
        protected readonly Lambda lambda;
        public Lambda Lambda { get { return lambda; } }
        protected readonly Normalization normalization;
        public Lambda Lambda { get { return lambda; } }

        public IBSimilarity(Distribution distribution, Lambda lambda, Normalization normalization)
        {
            this.distribution = distribution;
            this.lambda = lambda;
            this.normalization = normalization;
        }

        protected override float Score(BasicStats stats, float freq, float docLen)
        {
            return stats.getTotalBoost() *
                distribution.score(stats, normalization.tfn(stats, freq, docLen), lambda.lambda(stats));
        }

        protected override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            if (stats.TotalBoost != 1.0f)
            {
                expl.AddDetail(new Explanation(stats.TotalBoost, "boost"));
            }
            Explanation normExpl = normalization.Explain(stats, freq, docLen);
            Explanation lambdaExpl = lambda.Explain(stats);
            expl.AddDetail(normExpl);
            expl.AddDetail(lambdaExpl);
            expl.AddDetail(distribution.Explain(stats, normExpl.Value, lambdaExpl.Value));
        }

        public override string ToString()
        {
            return "IB " + distribution.ToString() + "-" + lambda.ToString()
                         + normalization.ToString();
        }
    }
}
