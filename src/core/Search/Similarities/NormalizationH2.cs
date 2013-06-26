namespace Lucene.Net.Search.Similarities
{
    public class NormalizationH2 : Normalization
    {
        private readonly float c;

        public NormalizationH2(float c)
        {
            this.c = c;
        }

        public NormalizationH2() : this(1)
        {
        }

        public float C
        {
            get { return c; }
        }

        public override sealed float Tfn(BasicStats stats, float tf, float len)
        {
            return (float)(tf*SimilarityBase.Log2(1 + c*stats.AvgFieldLength/len));
        }

        public override string ToString()
        {
            return "2";
        }
    }
}