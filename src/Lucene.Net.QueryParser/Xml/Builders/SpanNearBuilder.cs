/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Search.Spans.SpanNearQuery">Lucene.Net.Search.Spans.SpanNearQuery
	/// 	</see>
	/// </summary>
	public class SpanNearBuilder : SpanBuilderBase
	{
		private readonly SpanQueryBuilder factory;

		public SpanNearBuilder(SpanQueryBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public override SpanQuery GetSpanQuery(Element e)
		{
			string slopString = DOMUtils.GetAttributeOrFail(e, "slop");
			int slop = System.Convert.ToInt32(slopString);
			bool inOrder = DOMUtils.GetAttribute(e, "inOrder", false);
			IList<SpanQuery> spans = new AList<SpanQuery>();
			for (Node kid = e.GetFirstChild(); kid != null; kid = kid.GetNextSibling())
			{
				if (kid.GetNodeType() == Node.ELEMENT_NODE)
				{
					spans.AddItem(factory.GetSpanQuery((Element)kid));
				}
			}
			SpanQuery[] spanQueries = Sharpen.Collections.ToArray(spans, new SpanQuery[spans.
				Count]);
			return new SpanNearQuery(spanQueries, slop, inOrder);
		}
	}
}
