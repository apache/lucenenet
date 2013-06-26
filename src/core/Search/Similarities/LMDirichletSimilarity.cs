using System;

namespace Lucene.Net.Search.Similarities
{
    public class LMDirichletSimilarity : LMSimilarity
    {
        private readonly float mu;

        public LMDirichletSimilarity(ICollectionModel collectionModel, float mu)
            : base(collectionModel)
        {
            this.mu = mu;
        }

        public LMDirichletSimilarity(float mu)
        {
            this.mu = mu;
        }

        public LMDirichletSimilarity(ICollectionModel collectionModel) : this(collectionModel, 2000)
        {
        }

        public LMDirichletSimilarity() : this(2000)
        {
        }

        public float Mu
        {
            get { return mu; }
        }

        protected override float Score(BasicStats stats, float freq, float docLen)
        {
            float score = stats.TotalBoost*(float) (Math.Log(1 + freq/
                                                             (mu*((LMStats) stats).CollectionProbability)) +
                                                    Math.Log(mu/(docLen + mu)));
            return score > 0.0f ? score : 0.0f;
        }

        protected override void Explain(Explanation expl, BasicStats stats, int doc,
                                        float freq, float docLen)
        {
            if (stats.TotalBoost != 1.0f)
            {
                expl.AddDetail(new Explanation(stats.TotalBoost, "boost"));
            }

            expl.AddDetail(new Explanation(mu, "mu"));
            var weightExpl = new Explanation
                {
                    Value = (float) Math.Log(1 + freq/(mu*((LMStats) stats).CollectionProbability)),
                    Description = "term weight"
                };
            expl.AddDetail(weightExpl);
            expl.AddDetail(new Explanation((float) Math.Log(mu/(docLen + mu)), "document norm"));
            base.Explain(expl, stats, doc, freq, docLen);
        }

        public override string GetName()
        {
            return string.Format("Dirichlet{0}", Mu);
            //return String.format(Locale.ROOT, "Dirichlet(%f)", getMu());
        }
    }
}