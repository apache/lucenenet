using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class AfterEffectL : AfterEffect
    {
        public AfterEffectL() { }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            return 1 / (tfn + 1);
        }

        public override sealed Explanation Explain(BasicStats stats, float tfn)
        {
            Explanation result = new Explanation();
            result.setDescription(this.GetType().Name + ", computed from: ");
            result.setValue(score(stats, tfn));
            result.addDetail(new Explanation(tfn, "tfn"));
            return result;
        }

        public override string ToString()
        {
            return "L";
        }
    }
}
