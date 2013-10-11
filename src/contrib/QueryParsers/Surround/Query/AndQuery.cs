using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class AndQuery : ComposedQuery
    {
        public AndQuery(IList<SrndQuery> queries, bool inf, string opName)
            : base(queries, inf, opName)
        {
        }

        public override Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf)
        {
            return SrndBooleanQuery.MakeBooleanQuery( /* subqueries can be individually boosted */
                MakeLuceneSubQueriesField(fieldName, qf), Occur.MUST);
        }
    }
}
