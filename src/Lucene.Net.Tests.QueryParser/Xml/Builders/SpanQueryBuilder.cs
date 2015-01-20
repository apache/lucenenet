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
	/// Interface for retrieving a
	/// <see cref="Org.Apache.Lucene.Search.Spans.SpanQuery">Org.Apache.Lucene.Search.Spans.SpanQuery
	/// 	</see>
	/// .
	/// </summary>
	public interface SpanQueryBuilder : QueryBuilder
	{
		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		SpanQuery GetSpanQuery(Element e);
	}
}
