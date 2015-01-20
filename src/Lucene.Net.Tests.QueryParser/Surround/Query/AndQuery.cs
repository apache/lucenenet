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
	/// <summary>Factory for conjunctions</summary>
	public class AndQuery : ComposedQuery
	{
		public AndQuery(IList<SrndQuery> queries, bool inf, string opName) : base(queries
			, inf, opName)
		{
		}

		public override Org.Apache.Lucene.Search.Query MakeLuceneQueryFieldNoBoost(string
			 fieldName, BasicQueryFactory qf)
		{
			return SrndBooleanQuery.MakeBooleanQuery(MakeLuceneSubQueriesField(fieldName, qf)
				, BooleanClause.Occur.MUST);
		}
	}
}
