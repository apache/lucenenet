using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public abstract class ComposedQuery : SrndQuery
    {
        public ComposedQuery(IList<SrndQuery> qs, bool operatorInfix, string opName)
        {
            Recompose(qs);
            this.operatorInfix = operatorInfix;
            this.opName = opName;
        }

        protected void Recompose(IList<SrndQuery> queries)
        {
            if (queries.Count < 2) throw new InvalidOperationException("Too few subqueries");
            this.queries = queries;
        }

        protected string opName;
        public string OperatorName { get { return opName; } }

        protected IList<SrndQuery> queries;

        public IEnumerator<SrndQuery> SubQueriesIterator { get { return queries.GetEnumerator(); } }

        public int NrSubQueries { get { return queries.Count; } }

        public SrndQuery GetSubQuery(int qn) { return queries[qn]; }

        private bool operatorInfix;
        public bool IsOperatorInfix { get { return operatorInfix; } } /* else prefix operator */

        public IList<Search.Query> MakeLuceneSubQueriesField(string fn, BasicQueryFactory qf)
        {
            IList<Search.Query> luceneSubQueries = new List<Search.Query>();
            IEnumerator<SrndQuery> sqi = SubQueriesIterator;
            while (sqi.MoveNext())
            {
                luceneSubQueries.Add((sqi.Current).MakeLuceneQueryField(fn, qf));
            }
            return luceneSubQueries;
        }

        public override string ToString()
        {
            StringBuilder r = new StringBuilder();
            if (IsOperatorInfix)
            {
                InfixToString(r);
            }
            else
            {
                PrefixToString(r);
            }
            WeightToString(r);
            return r.ToString();
        }

        /* Override for different spacing */
        protected string PrefixSeparator { get { return ", "; } }
        protected string BracketOpen { get { return "("; } }
        protected string BracketClose { get { return ")"; } }

        protected void InfixToString(StringBuilder r)
        {
            /* Brackets are possibly redundant in the result. */
            IEnumerator<SrndQuery> sqi = SubQueriesIterator;
            r.Append(BracketOpen);
            if (sqi.MoveNext())
            {
                r.Append(sqi.Current.ToString());
                while (sqi.MoveNext())
                {
                    r.Append(" ");
                    r.Append(OperatorName); /* infix operator */
                    r.Append(" ");
                    r.Append(sqi.Current.ToString());
                }
            }
            r.Append(BracketClose);
        }

        protected void PrefixToString(StringBuilder r)
        {
            IEnumerator<SrndQuery> sqi = SubQueriesIterator;
            r.Append(OperatorName); /* prefix operator */
            r.Append(BracketOpen);
            if (sqi.MoveNext())
            {
                r.Append(sqi.Current.ToString());
                while (sqi.MoveNext())
                {
                    r.Append(PrefixSeparator);
                    r.Append(sqi.Current.ToString());
                }
            }
            r.Append(BracketClose);
        }

        public override bool IsFieldsSubQueryAcceptable
        {
            get
            {
                /* at least one subquery should be acceptable */
                IEnumerator<SrndQuery> sqi = SubQueriesIterator;
                while (sqi.MoveNext())
                {
                    if ((sqi.Current).IsFieldsSubQueryAcceptable)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public abstract override Search.Query MakeLuceneQueryFieldNoBoost(string fieldName, BasicQueryFactory qf);
    }
}
