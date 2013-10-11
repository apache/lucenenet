using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class NotQuery : ComposedQuery
    {
        public NotQuery(IList<SrndQuery> queries, string opName)
            : base(queries, true /* infix */, opName)
        {
        }

        public override Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf)
        {
            IList<Search.Query> luceneSubQueries = MakeLuceneSubQueriesField(fieldName, qf);
            BooleanQuery bq = new BooleanQuery();
            bq.Add(luceneSubQueries[0], Occur.MUST);
            SrndBooleanQuery.AddQueriesToBoolean(bq,
                // FIXME: do not allow weights on prohibited subqueries.
                    luceneSubQueries.SubList(1, luceneSubQueries.Count),
                // later subqueries: not required, prohibited
                    Occur.MUST_NOT);
            return bq;
        }
    }
}
