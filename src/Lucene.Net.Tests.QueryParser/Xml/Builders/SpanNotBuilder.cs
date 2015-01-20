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
	/// <see cref="Org.Apache.Lucene.Search.Spans.SpanNotQuery">Org.Apache.Lucene.Search.Spans.SpanNotQuery
	/// 	</see>
	/// </summary>
	public class SpanNotBuilder : SpanBuilderBase
	{
		private readonly SpanQueryBuilder factory;

		public SpanNotBuilder(SpanQueryBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
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
