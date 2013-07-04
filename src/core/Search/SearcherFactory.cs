using Lucene.Net.Index;

namespace Lucene.Net.Search
{
    public class SearcherFactory
    {
        public IndexSearcher NewSearcher(IndexReader reader)
        {
            return new IndexSearcher(reader);
        }
    }
}
