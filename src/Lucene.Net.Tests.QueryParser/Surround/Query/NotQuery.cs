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
	/// <summary>Factory for prohibited clauses</summary>
	public class NotQuery : ComposedQuery
	{
		public NotQuery(IList<SrndQuery> queries, string opName) : base(queries, true, opName
			)
		{
		}

		public override Org.Apache.Lucene.Search.Query MakeLuceneQueryFieldNoBoost(string
			 fieldName, BasicQueryFactory qf)
		{
			IList<Org.Apache.Lucene.Search.Query> luceneSubQueries = MakeLuceneSubQueriesField
				(fieldName, qf);
			BooleanQuery bq = new BooleanQuery();
			bq.Add(luceneSubQueries[0], BooleanClause.Occur.MUST);
			SrndBooleanQuery.AddQueriesToBoolean(bq, luceneSubQueries.SubList(1, luceneSubQueries
				.Count), BooleanClause.Occur.MUST_NOT);
			// FIXME: do not allow weights on prohibited subqueries.
			// later subqueries: not required, prohibited
			return bq;
		}
	}
}
