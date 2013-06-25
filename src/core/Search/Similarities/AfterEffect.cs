using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public class AfterEffect
    {
        public AfterEffect() { }

        public abstract float Score(BasicStats stats, float tfn);

        public abstract Explanation Explain(BasicStats stats, float tfn);

        public sealed class NoAfterEffect : AfterEffect
        {

            public NoAfterEffect() { }

            public override sealed float Score(BasicStats stats, float tfn)
            {
                return 1f;
            }

            public override sealed Explanation Explain(BasicStats stats, float tfn)
            {
                return new Explanation(1, "no aftereffect");
            }

            public string ToString()
            {
                return "";
            }
        }

        public abstract override string ToString();
    }
}
