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
	/// <summary>Implemented by objects that produce Lucene Query objects from XML streams.
	/// 	</summary>
	/// <remarks>
	/// Implemented by objects that produce Lucene Query objects from XML streams. Implementations are
	/// expected to be thread-safe so that they can be used to simultaneously parse multiple XML documents.
	/// </remarks>
	public interface QueryBuilder
	{
		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		Query GetQuery(Element e);
	}
}
