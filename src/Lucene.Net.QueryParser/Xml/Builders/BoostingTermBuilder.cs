/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Search.Payloads;
using Lucene.Net.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Search.Payloads.PayloadTermQuery">Lucene.Net.Search.Payloads.PayloadTermQuery
	/// 	</see>
	/// </summary>
	public class BoostingTermBuilder : SpanBuilderBase
	{
		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public override SpanQuery GetSpanQuery(Element e)
		{
			string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
			string value = DOMUtils.GetNonBlankTextOrFail(e);
			PayloadTermQuery btq = new PayloadTermQuery(new Term(fieldName, value), new AveragePayloadFunction
				());
			btq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return btq;
		}
	}
}
