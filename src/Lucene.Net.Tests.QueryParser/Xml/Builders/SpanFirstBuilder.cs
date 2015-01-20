/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Queryparser.Xml.Builders;
using Org.Apache.Lucene.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Org.Apache.Lucene.Search.Spans.SpanFirstQuery">Org.Apache.Lucene.Search.Spans.SpanFirstQuery
	/// 	</see>
	/// </summary>
	public class SpanFirstBuilder : SpanBuilderBase
	{
		private readonly SpanQueryBuilder factory;

		public SpanFirstBuilder(SpanQueryBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
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
