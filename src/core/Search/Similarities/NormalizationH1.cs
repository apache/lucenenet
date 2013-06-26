namespace Lucene.Net.Search.Similarities
{
    public class NormalizationH1 : Normalization
    {
        private readonly float c;

        public NormalizationH1(float c)
        {
            this.c = c;
        }

        public NormalizationH1() : this(1)
        {
        }

        public float C
        {
            get { return c; }
        }

        public override sealed float Tfn(BasicStats stats, float tf, float len)
        {
            return tf*stats.AvgFieldLength/len;
        }

        public override string ToString()
        {
            return "1";
        }
    }
}