namespace Lucene.Net.Search.Similarities
{
    public class IBSimilarity : SimilarityBase
    {
        protected readonly Distribution distribution;

        protected readonly Lambda lambda;

        protected readonly Normalization normalization;

        public IBSimilarity(Distribution distribution, Lambda lambda, Normalization normalization)
        {
            this.distribution = distribution;
            this.lambda = lambda;
            this.normalization = normalization;
        }

        public Distribution Distribution
        {
            get { return distribution; }
        }

        public Lambda Lambda
        {
            get { return lambda; }
        }

        public Normalization Normalization
        {
            get { return normalization; }
        }

        protected override float Score(BasicStats stats, float freq, float docLen)
        {
            return stats.TotalBoost*
                   distribution.Score(stats, normalization.Tfn(stats, freq, docLen), lambda.CalculateLambda(stats));
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
            return "IB " + distribution + "-" + lambda
                   + normalization;
        }
    }
}