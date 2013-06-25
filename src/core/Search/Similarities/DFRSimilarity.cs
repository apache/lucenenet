using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class DFRSimilarity : SimilarityBase
    {
        protected readonly BasicModel basicModel;
        public BasicModel BasicModel { get { return basicModel; } }
        protected readonly AfterEffect afterEffect;
        public AfterEffect AfterEffect { get { return afterEffect; } }
        protected readonly Normalization normalization;
        public Normalization Normalization { get { return normalization; } }

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

        protected override float Score(BasicStats stats, float freq, float docLen)
        {
            var tfn = normalization.Tfn(stats, freq, docLen);
            return stats.TotalBoost * basicModel.Score(stats, tfn) * afterEffect.Score(stats, tfn);
        }

        protected override void Explain(Explanation expl, BasicStats stats, int doc, float freq, float docLen)
        {
            if (stats.TotalBoost != 1.0f)
            {
                expl.addDetail(new Explanation(stats.getTotalBoost(), "boost"));
            }

            var normExpl = normalization.Explain(stats, freq, docLen);
            var tfn = normExpl.Value;
            expl.AddDetail(normExpl);
            expl.AddDetail(basicModel.Explain(stats, tfn));
            expl.AddDetail(afterEffect.Explain(stats, tfn));
        }

        public override string ToString()
        {
            return "DFR " + basicModel.toString() + afterEffect.toString()
                          + normalization.toString();
        }
    }
}
