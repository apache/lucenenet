/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Queryparser.Xml.Builders;
using Org.Apache.Lucene.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Org.Apache.Lucene.Search.Spans.SpanTermQuery">Org.Apache.Lucene.Search.Spans.SpanTermQuery
	/// 	</see>
	/// </summary>
	public class SpanTermBuilder : SpanBuilderBase
	{
		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public override SpanQuery GetSpanQuery(Element e)
		{
			string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
			string value = DOMUtils.GetNonBlankTextOrFail(e);
			SpanTermQuery stq = new SpanTermQuery(new Term(fieldName, value));
			stq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return stq;
		}
	}
}
