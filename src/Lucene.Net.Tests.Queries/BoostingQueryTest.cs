using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries
{
    public class BoostingQueryTest : LuceneTestCase
    {
        // TODO: this suite desperately needs more tests!
        // ... like ones that actually run the query

        [Test]
        public virtual void TestBoostingQueryEquals()
        {
            TermQuery q1 = new TermQuery(new Term("subject:", "java"));
            TermQuery q2 = new TermQuery(new Term("subject:", "java"));
            assertEquals("Two TermQueries with same attributes should be equal", q1, q2);
            BoostingQuery bq1 = new BoostingQuery(q1, q2, 0.1f);
            QueryUtils.Check(bq1);
            BoostingQuery bq2 = new BoostingQuery(q1, q2, 0.1f);
            assertEquals("BoostingQuery with same attributes is not equal", bq1, bq2);
        }
    }
}