/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Parser
{
	/// <summary>
	/// A parser needs to implement
	/// <see cref="EscapeQuerySyntax">EscapeQuerySyntax</see>
	/// to allow the QueryNode
	/// to escape the queries, when the toQueryString method is called.
	/// </summary>
	public abstract class EscapeQuerySyntax
	{
		/// <summary>
		/// Type of escaping: String for escaping syntax,
		/// NORMAL for escaping reserved words (like AND) in terms
		/// </summary>
		public enum Type
		{
			STRING,
			NORMAL
		}

		/// <param name="text">- text to be escaped</param>
		/// <param name="locale">- locale for the current query</param>
		/// <param name="type">- select the type of escape operation to use</param>
		/// <returns>escaped text</returns>
		public abstract CharSequence Escape(CharSequence text, CultureInfo locale, EscapeQuerySyntax.Type
			 type);
	}
}
