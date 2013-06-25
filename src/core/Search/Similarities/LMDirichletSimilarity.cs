using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class LMDirichletSimilarity : LMSimilarity
    {
        private readonly float mu;
        public float Mu { get { return mu; } }

        public LMDirichletSimilarity(CollectionModel collectionModel, float mu)
            : base(collectionModel)
        {
            this.mu = mu;
        }

        public LMDirichletSimilarity(float mu)
        {
            this.mu = mu;
        }

        public LMDirichletSimilarity(CollectionModel collectionModel) : this(collectionModel, 2000) { }

        public LMDirichletSimilarity() : this(2000) { }

        protected override float Score(BasicStats stats, float freq, float docLen)
        {
            var score = stats.TotalBoost * (float)(Math.Log(1 + freq /
                (mu * ((LMStats)stats).CollectionProbability)) +
                Math.Log(mu / (docLen + mu)));
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
            var weightExpl = new Explanation();
            weightExpl.Value = (float)Math.Log(1 + freq / (mu * ((LMStats)stats).CollectionProbability));
            weightExpl.Description = "term weight";
            expl.AddDetail(weightExpl);
            expl.AddDetail(new Explanation((float)Math.Log(mu / (docLen + mu)), "document norm"));
            base.Explain(expl, stats, doc, freq, docLen);
        }

        public override string GetName()
        {
            return String.Format("Dirichlet{0}", Mu);
            //return String.format(Locale.ROOT, "Dirichlet(%f)", getMu());
        }
    }
}
