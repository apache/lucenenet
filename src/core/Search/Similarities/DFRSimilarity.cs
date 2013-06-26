using System;

namespace Lucene.Net.Search.Similarities
{
    public class DFRSimilarity : SimilarityBase
    {
        protected readonly AfterEffect afterEffect;
        protected readonly BasicModel basicModel;
        protected readonly Normalization normalization;

        public DFRSimilarity(BasicModel basicModel, AfterEffect afterEffect, Normalization normalization)
        {
            if (basicModel == null || afterEffect == null || normalization == null)
            {
                throw new ArgumentNullException("null parameters not allowed.");
            }
            this.basicModel = basicModel;
            this.afterEffect = afterEffect;
            this.normalization = normalization;
        }

        public BasicModel BasicModel
        {
            get { return basicModel; }
        }

        public AfterEffect AfterEffect
        {
            get { return afterEffect; }
        }

        public Normalization Normalization
        {
            get { return normalization; }
        }

        protected override float Score(BasicStats stats, float freq, float docLen)
        {
            float tfn = normalization.Tfn(stats, freq, docLen);
            return stats.TotalBoost*basicModel.Score(stats, tfn)*afterEffect.Score(stats, tfn);
        }

        protected override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            if (stats.TotalBoost != 1.0f)
            {
                expl.AddDetail(new Explanation(stats.TotalBoost, "boost"));
            }

            Explanation normExpl = normalization.Explain(stats, freq, docLen);
            float tfn = normExpl.Value;
            expl.AddDetail(normExpl);
            expl.AddDetail(basicModel.Explain(stats, tfn));
            expl.AddDetail(afterEffect.Explain(stats, tfn));
        }

        public override string ToString()
        {
            return "DFR " + basicModel.ToString() + afterEffect.ToString()
                   + normalization.ToString();
        }
    }
}