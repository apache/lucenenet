/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Surround.Query;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Surround.Query
{
	internal class SimpleTermRewriteQuery : RewriteQuery<SimpleTerm>
	{
		internal SimpleTermRewriteQuery(SimpleTerm srndQuery, string fieldName, BasicQueryFactory
			 qf) : base(srndQuery, fieldName, qf)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Org.Apache.Lucene.Search.Query Rewrite(IndexReader reader)
		{
			IList<Org.Apache.Lucene.Search.Query> luceneSubQueries = new AList<Org.Apache.Lucene.Search.Query
				>();
			srndQuery.VisitMatchingTerms(reader, fieldName, new _MatchingTermVisitor_40(this, 
				luceneSubQueries));
			return (luceneSubQueries.Count == 0) ? SrndQuery.theEmptyLcnQuery : (luceneSubQueries
				.Count == 1) ? luceneSubQueries[0] : SrndBooleanQuery.MakeBooleanQuery(luceneSubQueries
				, BooleanClause.Occur.SHOULD);
		}

		private sealed class _MatchingTermVisitor_40 : SimpleTerm.MatchingTermVisitor
		{
			public _MatchingTermVisitor_40(SimpleTermRewriteQuery _enclosing, IList<Org.Apache.Lucene.Search.Query
				> luceneSubQueries)
			{
				this._enclosing = _enclosing;
				this.luceneSubQueries = luceneSubQueries;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public void VisitMatchingTerm(Term term)
			{
				luceneSubQueries.AddItem(this._enclosing.qf.NewTermQuery(term));
			}

			private readonly SimpleTermRewriteQuery _enclosing;

			private readonly IList<Org.Apache.Lucene.Search.Query> luceneSubQueries;
		}
	}
}
