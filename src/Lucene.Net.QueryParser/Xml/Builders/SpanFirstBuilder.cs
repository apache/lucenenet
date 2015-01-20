/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Search.Spans.SpanFirstQuery">Lucene.Net.Search.Spans.SpanFirstQuery
	/// 	</see>
	/// </summary>
	public class SpanFirstBuilder : SpanBuilderBase
	{
		private readonly SpanQueryBuilder factory;

		public SpanFirstBuilder(SpanQueryBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public override SpanQuery GetSpanQuery(Element e)
		{
			int end = DOMUtils.GetAttribute(e, "end", 1);
			Element child = DOMUtils.GetFirstChildElement(e);
			SpanQuery q = factory.GetSpanQuery(child);
			SpanFirstQuery sfq = new SpanFirstQuery(q, end);
			sfq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return sfq;
		}
	}
}
