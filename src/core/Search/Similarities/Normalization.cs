using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Similarities
{
    public abstract class Normalization
    {
        public Normalization() { }

        public abstract float Tfn(BasicStats stats, float tf, float len);

        public virtual Explanation Explain(BasicStats stats, float tf, float len)
        {
            var result = new Explanation();
            result.Description = getClass().getSimpleName() + ", computed from: ";
            result.Value = tfn(stats, tf, len);
            result.AddDetail(new Explanation(tf, "tf"));
            result.AddDetail(new Explanation(stats.getAvgFieldLength(), "avgFieldLength"));
            result.AddDetail(new Explanation(len, "len"));
            return result;
        }

        public sealed class NoNormalization : Normalization
        {
            public NoNormalization() { }

            public override sealed float Tfn(BasicStats stats, float tf, float len)
            {
                return tf;
            }

            public override sealed Explanation Explain(BasicStats stats, float tf, float len)
            {
                return new Explanation(1, "no normalization");
            }

            public override string ToString()
            {
                return "";
            }
        }

        public override abstract string ToString();
    }
}
