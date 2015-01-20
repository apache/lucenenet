/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Queryparser.Xml.Builders;
using Org.Apache.Lucene.Search.Payloads;
using Org.Apache.Lucene.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Org.Apache.Lucene.Search.Payloads.PayloadTermQuery">Org.Apache.Lucene.Search.Payloads.PayloadTermQuery
	/// 	</see>
	/// </summary>
	public class BoostingTermBuilder : SpanBuilderBase
	{
		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
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
