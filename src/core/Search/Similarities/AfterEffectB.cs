using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class AfterEffectB : AfterEffect
    {
        public AfterEffectB() { }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            long F = stats.TotalTermFreq + 1;
            long n = stats.DocFreq + 1;
            return (F + 1) / (n * (tfn + 1));
        }

        public override sealed Explanation Explain(BasicStats stats, float tfn)
        {
            Explanation result = new Explanation();
            result.setDescription(this.GetType().Name + ", computed from: ");
            result.setValue(score(stats, tfn));
            result.addDetail(new Explanation(tfn, "tfn"));
            result.addDetail(new Explanation(stats.getTotalTermFreq(), "totalTermFreq"));
            result.addDetail(new Explanation(stats.getDocFreq(), "docFreq"));
            return result;
        }

        public override string ToString()
        {
            return "B";
        }
    }
}
