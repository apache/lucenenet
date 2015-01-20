/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Queryparser.Xml.Builders;
using Org.Apache.Lucene.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Org.Apache.Lucene.Search.Spans.SpanNearQuery">Org.Apache.Lucene.Search.Spans.SpanNearQuery
	/// 	</see>
	/// </summary>
	public class SpanNearBuilder : SpanBuilderBase
	{
		private readonly SpanQueryBuilder factory;

		public SpanNearBuilder(SpanQueryBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
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
