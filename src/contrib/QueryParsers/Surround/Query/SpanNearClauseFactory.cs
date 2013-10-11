using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    public class SpanNearClauseFactory
    {
        public SpanNearClauseFactory(IndexReader reader, string fieldName, BasicQueryFactory qf)
        {
            this.reader = reader;
            this.fieldName = fieldName;
            this.weightBySpanQuery = new HashMap<SpanQuery, float?>();
            this.qf = qf;
        }

        private IndexReader reader;
        private string fieldName;
        private HashMap<SpanQuery, float?> weightBySpanQuery;
        private BasicQueryFactory qf;

        public IndexReader IndexReader { get { return reader; } }

        public string FieldName { get { return fieldName; } }

        public BasicQueryFactory BasicQueryFactory { get { return qf; } }

        public int Count { get { return weightBySpanQuery.Count; } }

        public void Clear() { weightBySpanQuery.Clear(); }

        protected void AddSpanQueryWeighted(SpanQuery sq, float weight)
        {
            float? w = weightBySpanQuery[sq];
            if (w != null)
                w = w.Value + weight;
            else
                w = weight;
            weightBySpanQuery[sq] = w;
        }

        public void AddTermWeighted(Term t, float weight)
        {
            SpanTermQuery stq = qf.NewSpanTermQuery(t);
            /* CHECKME: wrap in Hashable...? */
            AddSpanQueryWeighted(stq, weight);
        }

        public void AddSpanQuery(Search.Query q)
        {
            if (q == SrndQuery.theEmptyLcnQuery)
                return;
            if (!(q is SpanQuery))
                throw new InvalidOperationException("Expected SpanQuery: " + q.ToString(FieldName));
            AddSpanQueryWeighted((SpanQuery)q, q.Boost);
        }

        public SpanQuery MakeSpanClause()
        {
            SpanQuery[] spanQueries = new SpanQuery[Count];
            IEnumerator<SpanQuery> sqi = weightBySpanQuery.Keys.GetEnumerator();
            int i = 0;
            while (sqi.MoveNext())
            {
                SpanQuery sq = sqi.Current;
                sq.Boost = weightBySpanQuery[sq].GetValueOrDefault();
                spanQueries[i++] = sq;
            }

            if (spanQueries.Length == 1)
                return spanQueries[0];
            else
                return new SpanOrQuery(spanQueries);
        }
    }
}
