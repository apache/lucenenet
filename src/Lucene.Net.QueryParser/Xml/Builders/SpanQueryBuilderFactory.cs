/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Factory for
	/// <see cref="SpanQueryBuilder">SpanQueryBuilder</see>
	/// s
	/// </summary>
	public class SpanQueryBuilderFactory : SpanQueryBuilder
	{
		private readonly IDictionary<string, SpanQueryBuilder> builders = new Dictionary<
			string, SpanQueryBuilder>();

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			return GetSpanQuery(e);
		}

		public virtual void AddBuilder(string nodeName, SpanQueryBuilder builder)
		{
			builders.Put(nodeName, builder);
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual SpanQuery GetSpanQuery(Element e)
		{
			SpanQueryBuilder builder = builders.Get(e.GetNodeName());
			if (builder == null)
			{
				throw new ParserException("No SpanQueryObjectBuilder defined for node " + e.GetNodeName
					());
			}
			return builder.GetSpanQuery(e);
		}
	}
}
