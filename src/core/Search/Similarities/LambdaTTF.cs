namespace Lucene.Net.Search.Similarities
{
    public class LambdaTTF : Lambda
    {
        public override sealed float CalculateLambda(BasicStats stats)
        {
            return (stats.TotalTermFreq + 1F)/(stats.NumberOfDocuments + 1F);
        }

        public override sealed Explanation Explain(BasicStats stats)
        {
            var result = new Explanation
                {
                    Description = GetType().Name + ", computed from: ",
                    Value = CalculateLambda(stats)
                };
            result.AddDetail(new Explanation(stats.TotalTermFreq, "totalTermFreq"));
            result.AddDetail(new Explanation(stats.NumberOfDocuments, "numberOfDocuments"));
            return result;
        }

        public override string ToString()
        {
            return "L";
        }
    }
}