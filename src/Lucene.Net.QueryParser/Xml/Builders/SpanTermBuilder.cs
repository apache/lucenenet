/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Search.Spans.SpanTermQuery">Lucene.Net.Search.Spans.SpanTermQuery
	/// 	</see>
	/// </summary>
	public class SpanTermBuilder : SpanBuilderBase
	{
		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
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
