/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Queryparser.Surround.Query;
using Sharpen;

namespace Lucene.Net.Queryparser.Surround.Query
{
	internal class DistanceRewriteQuery : RewriteQuery<DistanceQuery>
	{
		internal DistanceRewriteQuery(DistanceQuery srndQuery, string fieldName, BasicQueryFactory
			 qf) : base(srndQuery, fieldName, qf)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Search.Query Rewrite(IndexReader reader)
		{
			return srndQuery.GetSpanNearQuery(reader, fieldName, GetBoost(), qf);
		}
	}
}
