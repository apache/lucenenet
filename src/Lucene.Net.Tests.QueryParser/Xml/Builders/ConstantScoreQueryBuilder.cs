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
	/// <see cref="Org.Apache.Lucene.Search.ConstantScoreQuery">Org.Apache.Lucene.Search.ConstantScoreQuery
	/// 	</see>
	/// </summary>
	public class ConstantScoreQueryBuilder : QueryBuilder
	{
		private readonly FilterBuilderFactory filterFactory;

		public ConstantScoreQueryBuilder(FilterBuilderFactory filterFactory)
		{
			this.filterFactory = filterFactory;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			Element filterElem = DOMUtils.GetFirstChildOrFail(e);
			Query q = new ConstantScoreQuery(filterFactory.GetFilter(filterElem));
			q.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return q;
		}
	}
}
