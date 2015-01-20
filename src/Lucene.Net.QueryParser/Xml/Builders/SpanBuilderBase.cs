/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Base class for building
	/// <see cref="Lucene.Net.Search.Spans.SpanQuery">Lucene.Net.Search.Spans.SpanQuery
	/// 	</see>
	/// s
	/// </summary>
	public abstract class SpanBuilderBase : SpanQueryBuilder
	{
		// javadocs
		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			return GetSpanQuery(e);
		}

		public abstract SpanQuery GetSpanQuery(Element arg1);
	}
}
