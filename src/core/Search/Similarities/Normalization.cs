namespace Lucene.Net.Search.Similarities
{
    public abstract class Normalization
    {
        public abstract float Tfn(BasicStats stats, float tf, float len);

        public virtual Explanation Explain(BasicStats stats, float tf, float len)
        {
            var result = new Explanation
                {
                    Description = GetType().Name + ", computed from: ",
                    Value = Tfn(stats, tf, len)
                };
            result.AddDetail(new Explanation(tf, "tf"));
            result.AddDetail(new Explanation(stats.AvgFieldLength, "avgFieldLength"));
            result.AddDetail(new Explanation(len, "len"));
            return result;
        }

        public abstract override string ToString();

        public sealed class NoNormalization : Normalization
        {
            public override float Tfn(BasicStats stats, float tf, float len)
            {
                return tf;
            }

            public override Explanation Explain(BasicStats stats, float tf, float len)
            {
                return new Explanation(1, "no normalization");
            }

            public override string ToString()
            {
                return "";
            }
        }
    }
}