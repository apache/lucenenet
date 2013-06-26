namespace Lucene.Net.Search.Similarities
{
    public class BasicModelIn : BasicModel
    {
        public override sealed float Score(BasicStats stats, float tfn)
        {
            long N = stats.NumberOfDocuments;
            long n = stats.DocFreq;
            return tfn*(float) (SimilarityBase.Log2((N + 1)/(n + 0.5)));
        }

        public override sealed Explanation Explain(BasicStats stats, float tfn)
        {
            var result = new Explanation
                {
                    Description = GetType().Name + ", computed from: ",
                    Value = Score(stats, tfn)
                };
            result.AddDetail(new Explanation(tfn, "tfn"));
            result.AddDetail(new Explanation(stats.NumberOfDocuments, "numberOfDocuments"));
            result.AddDetail(new Explanation(stats.DocFreq, "docFreq"));
            return result;
        }

        public override string ToString()
        {
            return "I(n)";
        }
    }
}