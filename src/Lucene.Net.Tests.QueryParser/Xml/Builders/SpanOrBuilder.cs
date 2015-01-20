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
	/// <see cref="Org.Apache.Lucene.Search.Spans.SpanOrQuery">Org.Apache.Lucene.Search.Spans.SpanOrQuery
	/// 	</see>
	/// </summary>
	public class SpanOrBuilder : SpanBuilderBase
	{
		private readonly SpanQueryBuilder factory;

		public SpanOrBuilder(SpanQueryBuilder factory)
		{
			this.factory = factory;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
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
