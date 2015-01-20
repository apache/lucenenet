/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Org.Apache.Lucene.Search.FilteredQuery">Org.Apache.Lucene.Search.FilteredQuery
	/// 	</see>
	/// </summary>
	public class FilteredQueryBuilder : QueryBuilder
	{
		private readonly FilterBuilder filterFactory;

		private readonly QueryBuilder queryFactory;

		public FilteredQueryBuilder(FilterBuilder filterFactory, QueryBuilder queryFactory
			)
		{
			this.filterFactory = filterFactory;
			this.queryFactory = queryFactory;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			Element filterElement = DOMUtils.GetChildByTagOrFail(e, "Filter");
			filterElement = DOMUtils.GetFirstChildOrFail(filterElement);
			Filter f = filterFactory.GetFilter(filterElement);
			Element queryElement = DOMUtils.GetChildByTagOrFail(e, "Query");
			queryElement = DOMUtils.GetFirstChildOrFail(queryElement);
			Query q = queryFactory.GetQuery(queryElement);
			FilteredQuery fq = new FilteredQuery(q, f);
			fq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return fq;
		}
	}
}
