/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Queryparser.Surround.Query;
using Lucene.Net.Search.Spans;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	/// <summary>Factory for NEAR queries</summary>
	public class DistanceQuery : ComposedQuery, DistanceSubQuery
	{
		public DistanceQuery(IList<SrndQuery> queries, bool infix, int opDistance, string
			 opName, bool ordered) : base(queries, infix, opName)
		{
			this.opDistance = opDistance;
			this.ordered = ordered;
		}

		private int opDistance;

		public virtual int GetOpDistance()
		{
			return opDistance;
		}

		private bool ordered;

		public virtual bool SubQueriesOrdered()
		{
			return ordered;
		}

		public virtual string DistanceSubQueryNotAllowed()
		{
			Iterator<object> sqi = GetSubQueriesIterator();
			while (sqi.HasNext())
			{
				object leq = sqi.Next();
				if (leq is DistanceSubQuery)
				{
					DistanceSubQuery dsq = (DistanceSubQuery)leq;
					string m = dsq.DistanceSubQueryNotAllowed();
					if (m != null)
					{
						return m;
					}
				}
				else
				{
					return "Operator " + GetOperatorName() + " does not allow subquery " + leq.ToString
						();
				}
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AddSpanQueries(SpanNearClauseFactory sncf)
		{
			Lucene.Net.Search.Query snq = GetSpanNearQuery(sncf.GetIndexReader(), sncf
				.GetFieldName(), GetWeight(), sncf.GetBasicQueryFactory());
			sncf.AddSpanQuery(snq);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual Lucene.Net.Search.Query GetSpanNearQuery(IndexReader reader
			, string fieldName, float boost, BasicQueryFactory qf)
		{
			SpanQuery[] spanClauses = new SpanQuery[GetNrSubQueries()];
			Iterator<object> sqi = GetSubQueriesIterator();
			int qi = 0;
			while (sqi.HasNext())
			{
				SpanNearClauseFactory sncf = new SpanNearClauseFactory(reader, fieldName, qf);
				((DistanceSubQuery)sqi.Next()).AddSpanQueries(sncf);
				if (sncf.Size() == 0)
				{
					while (sqi.HasNext())
					{
						((DistanceSubQuery)sqi.Next()).AddSpanQueries(sncf);
						sncf.Clear();
					}
					return SrndQuery.theEmptyLcnQuery;
				}
				spanClauses[qi] = sncf.MakeSpanClause();
				qi++;
			}
			SpanNearQuery r = new SpanNearQuery(spanClauses, GetOpDistance() - 1, SubQueriesOrdered
				());
			r.SetBoost(boost);
			return r;
		}

		public override Lucene.Net.Search.Query MakeLuceneQueryFieldNoBoost(string
			 fieldName, BasicQueryFactory qf)
		{
			return new DistanceRewriteQuery(this, fieldName, qf);
		}
	}
}
