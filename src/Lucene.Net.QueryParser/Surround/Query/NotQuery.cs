/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Surround.Query;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	/// <summary>Factory for prohibited clauses</summary>
	public class NotQuery : ComposedQuery
	{
		public NotQuery(IList<SrndQuery> queries, string opName) : base(queries, true, opName
			)
		{
		}

		public override Lucene.Net.Search.Query MakeLuceneQueryFieldNoBoost(string
			 fieldName, BasicQueryFactory qf)
		{
			IList<Lucene.Net.Search.Query> luceneSubQueries = MakeLuceneSubQueriesField
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
