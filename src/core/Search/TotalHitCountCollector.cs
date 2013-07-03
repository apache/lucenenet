using Lucene.Net.Index;

namespace Lucene.Net.Search
{
    /// <summary>
    /// Just counts the total number of hits.
    /// </summary>
    public class TotalHitCountCollector : Collector
    {
        public int TotalHits { get; private set; }

        public override void SetScorer(Scorer scorer)
        {
        }

        public override void Collect(int doc)
        {
            TotalHits++;
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return true; }
        }
    }
}