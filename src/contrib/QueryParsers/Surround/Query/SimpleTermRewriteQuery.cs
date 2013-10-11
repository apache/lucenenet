using Lucene.Net.Index;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    internal class SimpleTermRewriteQuery : RewriteQuery<SimpleTerm>
    {
        internal SimpleTermRewriteQuery(
            SimpleTerm srndQuery,
            string fieldName,
            BasicQueryFactory qf)
            : base(srndQuery, fieldName, qf)
        {
        }

        public override Search.Query Rewrite(IndexReader reader)
        {
            IList<Search.Query> luceneSubQueries = new List<Search.Query>();
            
            srndQuery.VisitMatchingTerms(reader, fieldName, new AnonymousMatchingTermVisitor(qf, luceneSubQueries));

            return (luceneSubQueries.Count == 0) 
                ? SrndQuery.theEmptyLcnQuery
                : (luceneSubQueries.Count == 1) 
                    ? luceneSubQueries[0]
                    : SrndBooleanQuery.MakeBooleanQuery(
                        /* luceneSubQueries all have default weight */
                        luceneSubQueries, Occur.SHOULD); /* OR the subquery terms */
        }

        private sealed class AnonymousMatchingTermVisitor : SimpleTerm.IMatchingTermVisitor
        {
            private readonly BasicQueryFactory qf;
            private readonly IList<Search.Query> luceneSubQueries;

            public AnonymousMatchingTermVisitor(BasicQueryFactory qf, IList<Search.Query> luceneSubQueries)
            {
                this.qf = qf;
                this.luceneSubQueries = luceneSubQueries;
            }

            public void VisitMatchingTerm(Term term)
            {
                luceneSubQueries.Add(qf.NewTermQuery(term));
            }
        }
    }
}
