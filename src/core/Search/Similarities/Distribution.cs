namespace Lucene.Net.Search.Similarities
{
    public abstract class Distribution
    {
        public abstract float Score(BasicStats stats, float tfn, float lambda);

        public virtual Explanation Explain(BasicStats stats, float tfn, float lambda)
        {
            return new Explanation(Score(stats, tfn, lambda), GetType().Name);
        }

        public abstract override string ToString();
    }
}