namespace Lucene.Net.Search.Similarities
{
    public class AfterEffectL : AfterEffect
    {
        public override sealed float Score(BasicStats stats, float tfn)
        {
            return 1/(tfn + 1);
        }

        public override sealed Explanation Explain(BasicStats stats, float tfn)
        {
            var result = new Explanation();
            result.Description = GetType().Name + ", computed from: ";
            result.Value = Score(stats, tfn);
            result.AddDetail(new Explanation(tfn, "tfn"));
            return result;
        }

        public override string ToString()
        {
            return "L";
        }
    }
}