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
	/// <see cref="Lucene.Net.Search.Spans.SpanNotQuery">Lucene.Net.Search.Spans.SpanNotQuery
	/// 	</see>
	/// </summary>
	public class SpanNotBuilder : SpanBuilderBase
	{
		private readonly SpanQueryBuilder factory;

		public SpanNotBuilder(SpanQueryBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public override SpanQuery GetSpanQuery(Element e)
		{
			Element includeElem = DOMUtils.GetChildByTagOrFail(e, "Include");
			includeElem = DOMUtils.GetFirstChildOrFail(includeElem);
			Element excludeElem = DOMUtils.GetChildByTagOrFail(e, "Exclude");
			excludeElem = DOMUtils.GetFirstChildOrFail(excludeElem);
			SpanQuery include = factory.GetSpanQuery(includeElem);
			SpanQuery exclude = factory.GetSpanQuery(excludeElem);
			SpanNotQuery snq = new SpanNotQuery(include, exclude);
			snq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return snq;
		}
	}
}
