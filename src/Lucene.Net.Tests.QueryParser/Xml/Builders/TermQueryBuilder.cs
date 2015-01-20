/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Queryparser.Xml.Builders;
using Org.Apache.Lucene.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Org.Apache.Lucene.Search.TermQuery">Org.Apache.Lucene.Search.TermQuery
	/// 	</see>
	/// </summary>
	public class TermQueryBuilder : QueryBuilder
	{
		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			string field = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
			string value = DOMUtils.GetNonBlankTextOrFail(e);
			TermQuery tq = new TermQuery(new Term(field, value));
			tq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return tq;
		}
	}
}
