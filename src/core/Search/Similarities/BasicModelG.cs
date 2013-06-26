namespace Lucene.Net.Search.Similarities
{
    public class BasicModelG : BasicModel
    {
        public override sealed float Score(BasicStats stats, float tfn)
        {
            long F = stats.TotalTermFreq + 1;
            long N = stats.NumberOfDocuments;
            long lambda = F/(N + F);
            return (float) (SimilarityBase.Log2(lambda + 1) + tfn*SimilarityBase.Log2((1 + lambda)/lambda));
        }

        public override string ToString()
        {
            return "G";
        }
    }
}