/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml
{
	/// <summary>
	/// Interface for building
	/// <see cref="Org.Apache.Lucene.Search.Filter">Org.Apache.Lucene.Search.Filter</see>
	/// s
	/// </summary>
	public interface FilterBuilder
	{
		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		Filter GetFilter(Element e);
	}
}
