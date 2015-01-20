/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml
{
	/// <summary>
	/// Factory for
	/// <see cref="QueryBuilder">QueryBuilder</see>
	/// </summary>
	public class QueryBuilderFactory : QueryBuilder
	{
		internal Dictionary<string, QueryBuilder> builders = new Dictionary<string, QueryBuilder
			>();

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element n)
		{
			QueryBuilder builder = builders.Get(n.GetNodeName());
			if (builder == null)
			{
				throw new ParserException("No QueryObjectBuilder defined for node " + n.GetNodeName
					());
			}
			return builder.GetQuery(n);
		}

		public virtual void AddBuilder(string nodeName, QueryBuilder builder)
		{
			builders.Put(nodeName, builder);
		}

		public virtual QueryBuilder GetQueryBuilder(string nodeName)
		{
			return builders.Get(nodeName);
		}
	}
}
