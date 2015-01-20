/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Surround.Query;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Surround.Query
{
	/// <summary>Factory for disjunctions</summary>
	public class OrQuery : ComposedQuery, DistanceSubQuery
	{
		public OrQuery(IList<SrndQuery> queries, bool infix, string opName) : base(queries
			, infix, opName)
		{
		}

		public override Org.Apache.Lucene.Search.Query MakeLuceneQueryFieldNoBoost(string
			 fieldName, BasicQueryFactory qf)
		{
			return SrndBooleanQuery.MakeBooleanQuery(MakeLuceneSubQueriesField(fieldName, qf)
				, BooleanClause.Occur.SHOULD);
		}

		public virtual string DistanceSubQueryNotAllowed()
		{
			Iterator sqi = GetSubQueriesIterator();
			while (sqi.HasNext())
			{
				SrndQuery leq = (SrndQuery)sqi.Next();
				if (leq is DistanceSubQuery)
				{
					string m = ((DistanceSubQuery)leq).DistanceSubQueryNotAllowed();
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

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AddSpanQueries(SpanNearClauseFactory sncf)
		{
			Iterator sqi = GetSubQueriesIterator();
			while (sqi.HasNext())
			{
				((DistanceSubQuery)sqi.Next()).AddSpanQueries(sncf);
			}
		}
	}
}
