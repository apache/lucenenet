namespace Lucene.Net.Search.Similarities
{
    public abstract class Lambda
    {
        public abstract float Lambda(BasicStats stats);
        public abstract Explanation Explain(BasicStats stats);

        public abstract override string ToString();
    }
}