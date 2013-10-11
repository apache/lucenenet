using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class OrQuery : ComposedQuery, IDistanceSubQuery
    {
        public OrQuery(IList<SrndQuery> queries, bool infix, string opName)
            : base(queries, infix, opName)
        {
        }

        public override Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf)
        {
            return SrndBooleanQuery.MakeBooleanQuery(
                /* subqueries can be individually boosted */
                MakeLuceneSubQueriesField(fieldName, qf), Occur.SHOULD);
        }

        public string DistanceSubQueryNotAllowed
        {
            get
            {
                IEnumerator<SrndQuery> sqi = SubQueriesIterator;
                while (sqi.MoveNext())
                {
                    SrndQuery leq = sqi.Current;
                    if (leq is IDistanceSubQuery)
                    {
                        String m = ((IDistanceSubQuery)leq).DistanceSubQueryNotAllowed;
                        if (m != null)
                        {
                            return m;
                        }
                    }
                    else
                    {
                        return "subquery not allowed: " + leq.ToString();
                    }
                }
                return null;
            }
        }

        public void AddSpanQueries(SpanNearClauseFactory sncf)
        {
            IEnumerator<SrndQuery> sqi = SubQueriesIterator;
            while (sqi.MoveNext())
            {
                ((IDistanceSubQuery)sqi.Current).AddSpanQueries(sncf);
            }
        }
    }
}
