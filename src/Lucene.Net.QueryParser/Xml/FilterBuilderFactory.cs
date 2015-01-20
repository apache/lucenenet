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
	/// <see cref="FilterBuilder">FilterBuilder</see>
	/// </summary>
	public class FilterBuilderFactory : FilterBuilder
	{
		internal Dictionary<string, FilterBuilder> builders = new Dictionary<string, FilterBuilder
			>();

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Filter GetFilter(Element n)
		{
			FilterBuilder builder = builders.Get(n.GetNodeName());
			if (builder == null)
			{
				throw new ParserException("No FilterBuilder defined for node " + n.GetNodeName());
			}
			return builder.GetFilter(n);
		}

		public virtual void AddBuilder(string nodeName, FilterBuilder builder)
		{
			builders.Put(nodeName, builder);
		}

		public virtual FilterBuilder GetFilterBuilder(string nodeName)
		{
			return builders.Get(nodeName);
		}
	}
}
