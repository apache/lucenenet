using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class LMJelinekMercerSimilarity : LMSimilarity
    {
        private readonly float lambda;
        public float Lambda { get { return lambda; } }

        public LMJelinekMercerSimilarity(CollectionModel collectionModel, float lambda)
            : base(collectionModel)
        {
            this.lambda = lambda;
        }

        public LMJelinekMercerSimilarity(float lambda)
        {
            this.lambda = lambda;
        }

        protected override float Score(BasicStats stats, float freq, float docLen)
        {
            return stats.TotalBoost * (float)Math.Log(1 + ((1 - lambda) * freq / docLen) / (lambda * ((LMStats)stats).CollectionProbability));
        }

        protected override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            if (stats.TotalBoost != 1.0f)
            {
                expl.AddDetail(new Explanation(stats.TotalBoost, "boost"));
            }
            expl.AddDetail(new Explanation(lambda, "lambda"));
            base.Explain(expl, stats, doc, freq, docLen);
        }

        public override string GetName()
        {
            return String.Format("Jelinek-Mercer{0}", Lambda);
            //return String.format(Locale.ROOT, "Jelinek-Mercer(%f)", Lambda);
        }
    }
}
