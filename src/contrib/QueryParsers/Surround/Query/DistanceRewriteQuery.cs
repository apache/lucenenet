using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class DistanceRewriteQuery : RewriteQuery<DistanceQuery>
    {
        internal DistanceRewriteQuery(
            DistanceQuery srndQuery,
            string fieldName,
            BasicQueryFactory qf)
            : base(srndQuery, fieldName, qf)
        {
        }

        public override Search.Query Rewrite(IndexReader reader)
        {
            return srndQuery.GetSpanNearQuery(reader, fieldName, Boost, qf);
        }
    }
}
