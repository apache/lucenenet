using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public interface IRewriteQuery
    {
        // .NET Port: non-generic marker interface
    }

    public abstract class RewriteQuery<SQ> : Search.Query, IRewriteQuery
        where SQ : SrndQuery
    {
        protected readonly SQ srndQuery;
        protected readonly string fieldName;
        protected readonly BasicQueryFactory qf;

        internal RewriteQuery(
          SQ srndQuery,
          string fieldName,
          BasicQueryFactory qf)
        {
            this.srndQuery = srndQuery;
            this.fieldName = fieldName;
            this.qf = qf;
        }

        public abstract override Search.Query Rewrite(IndexReader reader);

        public override string ToString()
        {
            return ToString(null);
        }

        public override string ToString(string field)
        {
            return GetType().FullName
                + (field == null ? "" : "(unused: " + field + ")")
                + "(" + fieldName
                + ", " + srndQuery.ToString()
                + ", " + qf.ToString()
                + ")";
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode()
                ^ fieldName.GetHashCode()
                ^ qf.GetHashCode()
                ^ srndQuery.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!GetType().Equals(obj.GetType()))
                return false;
            RewriteQuery<SQ> other = (RewriteQuery<SQ>)obj;
            return fieldName.Equals(other.fieldName)
                && qf.Equals(other.qf)
                && srndQuery.Equals(other.srndQuery);
        }

        public override object Clone()
        {
            throw new NotSupportedException();
        }
    }
}
