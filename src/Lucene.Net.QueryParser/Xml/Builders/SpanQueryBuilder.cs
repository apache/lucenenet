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
	/// Interface for retrieving a
	/// <see cref="Lucene.Net.Search.Spans.SpanQuery">Lucene.Net.Search.Spans.SpanQuery
	/// 	</see>
	/// .
	/// </summary>
	public interface SpanQueryBuilder : QueryBuilder
	{
		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		SpanQuery GetSpanQuery(Element e);
	}
}
