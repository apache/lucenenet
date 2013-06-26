namespace Lucene.Net.Search.Similarities
{
    public abstract class AfterEffect
    {
        public abstract float Score(BasicStats stats, float tfn);

        public abstract Explanation Explain(BasicStats stats, float tfn);

        public abstract override string ToString();

        public sealed class NoAfterEffect : AfterEffect
        {
            public override sealed float Score(BasicStats stats, float tfn)
            {
                return 1f;
            }

            public override sealed Explanation Explain(BasicStats stats, float tfn)
            {
                return new Explanation(1, "no aftereffect");
            }

            public override string ToString()
            {
                return "";
            }
        }
    }
}