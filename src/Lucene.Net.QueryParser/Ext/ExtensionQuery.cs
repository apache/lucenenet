/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Classic;
using Sharpen;

namespace Lucene.Net.Queryparser.Ext
{
	/// <summary>
	/// <see cref="ExtensionQuery">ExtensionQuery</see>
	/// holds all query components extracted from the original
	/// query string like the query field and the extension query string.
	/// </summary>
	/// <seealso cref="Extensions">Extensions</seealso>
	/// <seealso cref="ExtendableQueryParser">ExtendableQueryParser</seealso>
	/// <seealso cref="ParserExtension">ParserExtension</seealso>
	public class ExtensionQuery
	{
		private readonly string field;

		private readonly string rawQueryString;

		private readonly QueryParser topLevelParser;

		/// <summary>
		/// Creates a new
		/// <see cref="ExtensionQuery">ExtensionQuery</see>
		/// </summary>
		/// <param name="field">the query field</param>
		/// <param name="rawQueryString">the raw extension query string</param>
		public ExtensionQuery(QueryParser topLevelParser, string field, string rawQueryString
			)
		{
			this.field = field;
			this.rawQueryString = rawQueryString;
			this.topLevelParser = topLevelParser;
		}

		/// <summary>Returns the query field</summary>
		/// <returns>the query field</returns>
		public virtual string GetField()
		{
			return field;
		}

		/// <summary>Returns the raw extension query string</summary>
		/// <returns>the raw extension query string</returns>
		public virtual string GetRawQueryString()
		{
			return rawQueryString;
		}

		/// <summary>
		/// Returns the top level parser which created this
		/// <see cref="ExtensionQuery">ExtensionQuery</see>
		/// 
		/// </summary>
		/// <returns>
		/// the top level parser which created this
		/// <see cref="ExtensionQuery">ExtensionQuery</see>
		/// </returns>
		public virtual QueryParser GetTopLevelParser()
		{
			return topLevelParser;
		}
	}
}
