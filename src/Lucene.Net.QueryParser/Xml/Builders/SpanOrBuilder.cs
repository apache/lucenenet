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
	/// <see cref="Lucene.Net.Search.Spans.SpanOrQuery">Lucene.Net.Search.Spans.SpanOrQuery
	/// 	</see>
	/// </summary>
	public class SpanOrBuilder : SpanBuilderBase
	{
		private readonly SpanQueryBuilder factory;

		public SpanOrBuilder(SpanQueryBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public override SpanQuery GetSpanQuery(Element e)
		{
			IList<SpanQuery> clausesList = new AList<SpanQuery>();
			for (Node kid = e.GetFirstChild(); kid != null; kid = kid.GetNextSibling())
			{
				if (kid.GetNodeType() == Node.ELEMENT_NODE)
				{
					SpanQuery clause = factory.GetSpanQuery((Element)kid);
					clausesList.AddItem(clause);
				}
			}
			SpanQuery[] clauses = Sharpen.Collections.ToArray(clausesList, new SpanQuery[clausesList
				.Count]);
			SpanOrQuery soq = new SpanOrQuery(clauses);
			soq.SetBoost(DOMUtils.GetAttribute(e, "boost", 1.0f));
			return soq;
		}
	}
}
