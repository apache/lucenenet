using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public abstract class SimpleTerm : SrndQuery, IDistanceSubQuery, IComparable<SimpleTerm>
    {
        public SimpleTerm(bool q) { quoted = q; }

        private bool quoted;
        bool IsQuoted { get { return quoted; } }

        public string Quote { get { return "\""; } }
        public string FieldOperator { get { return "/"; } }

        public abstract string ToStringUnquoted();

        public int CompareTo(SimpleTerm ost)
        {
            /* for ordering terms and prefixes before using an index, not used */
            return this.ToStringUnquoted().CompareTo(ost.ToStringUnquoted());
        }

        protected virtual void SuffixToString(StringBuilder r) { } /* override for prefix query */

        public override string ToString()
        {
            StringBuilder r = new StringBuilder();
            if (IsQuoted)
            {
                r.Append(Quote);
            }
            r.Append(ToStringUnquoted());
            if (IsQuoted)
            {
                r.Append(Quote);
            }
            SuffixToString(r);
            WeightToString(r);
            return r.ToString();
        }

        public abstract void VisitMatchingTerms(
                            IndexReader reader,
                            String fieldName,
                            IMatchingTermVisitor mtv);

        public interface IMatchingTermVisitor
        {
            void VisitMatchingTerm(Term t);
        }

        public virtual string DistanceSubQueryNotAllowed
        {
            get { return null; }
        }

        public virtual void AddSpanQueries(SpanNearClauseFactory sncf)
        {
            VisitMatchingTerms(
                sncf.IndexReader,
                sncf.FieldName,
                new AnonymousAddSpanQueriesMatchingTermVisitor(this, sncf));
        }

        private sealed class AnonymousAddSpanQueriesMatchingTermVisitor : IMatchingTermVisitor
        {
            private readonly SpanNearClauseFactory sncf;
            private readonly SimpleTerm parent;

            public AnonymousAddSpanQueriesMatchingTermVisitor(SimpleTerm parent, SpanNearClauseFactory sncf)
            {
                this.parent = parent;
                this.sncf = sncf;
            }

            public void VisitMatchingTerm(Term term)
            {
                sncf.AddTermWeighted(term, parent.Weight);
            }
        }

        public override Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf)
        {
            return new SimpleTermRewriteQuery(this, fieldName, qf);
        }
    }
}
