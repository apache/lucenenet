/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Search.ConstantScoreQuery">Lucene.Net.Search.ConstantScoreQuery
	/// 	</see>
	/// </summary>
	public class ConstantScoreQueryBuilder : QueryBuilder
	{
		private readonly FilterBuilderFactory filterFactory;

		public ConstantScoreQueryBuilder(FilterBuilderFactory filterFactory)
		{
			this.filterFactory = filterFactory;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			Element filterElem = DOMUtils.GetFirstChildOrFail(e);
			Query q = new ConstantScoreQuery(filterFactory.GetFilter(filterElem));
			q.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return q;
		}
	}
}
