namespace Lucene.Net.Search.Similarities
{
    public class AfterEffectB : AfterEffect
    {
        public override sealed float Score(BasicStats stats, float tfn)
        {
            long F = stats.TotalTermFreq + 1;
            long n = stats.DocFreq + 1;
            return (F + 1)/(n*(tfn + 1));
        }

        public override sealed Explanation Explain(BasicStats stats, float tfn)
        {
            var result = new Explanation
                {
                    Description = GetType().Name + ", computed from: ",
                    Value = Score(stats, tfn)
                };
            result.AddDetail(new Explanation(tfn, "tfn"));
            result.AddDetail(new Explanation(stats.TotalTermFreq, "totalTermFreq"));
            result.AddDetail(new Explanation(stats.DocFreq, "docFreq"));
            return result;
        }

        public override string ToString()
        {
            return "B";
        }
    }
}