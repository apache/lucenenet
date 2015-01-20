/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml
{
	/// <summary>
	/// Interface for building
	/// <see cref="Lucene.Net.Search.Filter">Lucene.Net.Search.Filter</see>
	/// s
	/// </summary>
	public interface FilterBuilder
	{
		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		Filter GetFilter(Element e);
	}
}
