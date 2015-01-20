/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Xml.Builders;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Base class for building
	/// <see cref="Org.Apache.Lucene.Search.Spans.SpanQuery">Org.Apache.Lucene.Search.Spans.SpanQuery
	/// 	</see>
	/// s
	/// </summary>
	public abstract class SpanBuilderBase : SpanQueryBuilder
	{
		// javadocs
		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			return GetSpanQuery(e);
		}

		public abstract SpanQuery GetSpanQuery(Element arg1);
	}
}
