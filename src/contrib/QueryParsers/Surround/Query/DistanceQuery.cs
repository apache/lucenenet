using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class DistanceQuery : ComposedQuery, IDistanceSubQuery
    {
        public DistanceQuery(
            IList<SrndQuery> queries,
            bool infix,
            int opDistance,
            string opName,
            bool ordered)
            : base(queries, infix, opName)
        {
            this.opDistance = opDistance; /* the distance indicated in the operator */
            this.ordered = ordered;
        }

        private int opDistance;
        public int OpDistance { get { return opDistance; } }

        private bool ordered;
        public bool SubQueriesOrdered { get { return ordered; } }

        public string DistanceSubQueryNotAllowed
        {
            get
            {
                IEnumerator<SrndQuery> sqi = SubQueriesIterator;
                while (sqi.MoveNext())
                {
                    Object leq = sqi.Current;
                    if (leq is IDistanceSubQuery)
                    {
                        IDistanceSubQuery dsq = (IDistanceSubQuery)leq;
                        string m = dsq.DistanceSubQueryNotAllowed;
                        if (m != null)
                        {
                            return m;
                        }
                    }
                    else
                    {
                        return "Operator " + OperatorName + " does not allow subquery " + leq.ToString();
                    }
                }
                return null; /* subqueries acceptable */
            }
        }

        public void AddSpanQueries(SpanNearClauseFactory sncf)
        {
            Search.Query snq = GetSpanNearQuery(sncf.IndexReader,
                                  sncf.FieldName,
                                  Weight,
                                  sncf.BasicQueryFactory);
            sncf.AddSpanQuery(snq);
        }

        public Search.Query GetSpanNearQuery(
          IndexReader reader,
          string fieldName,
          float boost,
          BasicQueryFactory qf)
        {
            SpanQuery[] spanClauses = new SpanQuery[NrSubQueries];
            IEnumerator<SrndQuery> sqi = SubQueriesIterator;
            int qi = 0;
            while (sqi.MoveNext())
            {
                SpanNearClauseFactory sncf = new SpanNearClauseFactory(reader, fieldName, qf);

                ((IDistanceSubQuery)sqi.Current).AddSpanQueries(sncf);
                if (sncf.Count == 0)
                { /* distance operator requires all sub queries */
                    while (sqi.MoveNext())
                    { /* produce evt. error messages but ignore results */
                        ((IDistanceSubQuery)sqi.Current).AddSpanQueries(sncf);
                        sncf.Clear();
                    }
                    return SrndQuery.theEmptyLcnQuery;
                }

                spanClauses[qi] = sncf.MakeSpanClause();
                qi++;
            }

            SpanNearQuery r = new SpanNearQuery(spanClauses, OpDistance - 1, SubQueriesOrdered);
            r.Boost = boost;
            return r;
        }

        public override Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf)
        {
            return new DistanceRewriteQuery(this, fieldName, qf);
        }
    }
}
