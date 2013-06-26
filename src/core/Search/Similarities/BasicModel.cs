namespace Lucene.Net.Search.Similarities
{
    public abstract class BasicModel
    {
        public abstract float Score(BasicStats stats, float tfn);

        public virtual Explanation Explain(BasicStats stats, float tfn)
        {
            var result = new Explanation
                {
                    Description = GetType().Name + ", computed from: ",
                    Value = Score(stats, tfn)
                };
            result.AddDetail(new Explanation(tfn, "tfn"));
            result.AddDetail(
                new Explanation(stats.NumberOfDocuments, "numberOfDocuments"));
            result.AddDetail(
                new Explanation(stats.TotalTermFreq, "totalTermFreq"));
            return result;
        }

        public abstract override string ToString();
    }
}