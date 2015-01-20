/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Surround.Query;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Surround.Query
{
	internal class SrndBooleanQuery
	{
		public static void AddQueriesToBoolean(BooleanQuery bq, IList<Org.Apache.Lucene.Search.Query
			> queries, BooleanClause.Occur occur)
		{
			for (int i = 0; i < queries.Count; i++)
			{
				bq.Add(queries[i], occur);
			}
		}

		public static Org.Apache.Lucene.Search.Query MakeBooleanQuery(IList<Org.Apache.Lucene.Search.Query
			> queries, BooleanClause.Occur occur)
		{
			if (queries.Count <= 1)
			{
				throw new Exception("Too few subqueries: " + queries.Count);
			}
			BooleanQuery bq = new BooleanQuery();
			AddQueriesToBoolean(bq, queries.SubList(0, queries.Count), occur);
			return bq;
		}
	}
}
