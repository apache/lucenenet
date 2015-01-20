/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Queryparser.Flexible.Core;
using Lucene.Net.Queryparser.Flexible.Core.Messages;
using Lucene.Net.Queryparser.Flexible.Messages;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core
{
	/// <summary>
	/// This should be thrown when an exception happens during the query parsing from
	/// string to the query node tree.
	/// </summary>
	/// <remarks>
	/// This should be thrown when an exception happens during the query parsing from
	/// string to the query node tree.
	/// </remarks>
	/// <seealso cref="QueryNodeException">QueryNodeException</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Parser.SyntaxParser">Lucene.Net.Queryparser.Flexible.Core.Parser.SyntaxParser
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</seealso>
	[System.Serializable]
	public class QueryNodeParseException : QueryNodeException
	{
		private CharSequence query;

		private int beginColumn = -1;

		private int beginLine = -1;

		private string errorToken = string.Empty;

		public QueryNodeParseException(Message message) : base(message)
		{
		}

		public QueryNodeParseException(Exception throwable) : base(throwable)
		{
		}

		public QueryNodeParseException(Message message, Exception throwable) : base(message
			, throwable)
		{
		}

		public virtual void SetQuery(CharSequence query)
		{
			this.query = query;
			this.message = new MessageImpl(QueryParserMessages.INVALID_SYNTAX_CANNOT_PARSE, query
				, string.Empty);
		}

		public virtual CharSequence GetQuery()
		{
			return this.query;
		}

		/// <param name="errorToken">the errorToken in the query</param>
		protected internal virtual void SetErrorToken(string errorToken)
		{
			this.errorToken = errorToken;
		}

		public virtual string GetErrorToken()
		{
			return this.errorToken;
		}

		public virtual void SetNonLocalizedMessage(Message message)
		{
			this.message = message;
		}

		/// <summary>
		/// For EndOfLine and EndOfFile ("&lt;EOF&gt;") parsing problems the last char in the
		/// string is returned For the case where the parser is not able to figure out
		/// the line and column number -1 will be returned
		/// </summary>
		/// <returns>line where the problem was found</returns>
		public virtual int GetBeginLine()
		{
			return this.beginLine;
		}

		/// <summary>
		/// For EndOfLine and EndOfFile ("&lt;EOF&gt;") parsing problems the last char in the
		/// string is returned For the case where the parser is not able to figure out
		/// the line and column number -1 will be returned
		/// </summary>
		/// <returns>column of the first char where the problem was found</returns>
		public virtual int GetBeginColumn()
		{
			return this.beginColumn;
		}

		/// <param name="beginLine">the beginLine to set</param>
		protected internal virtual void SetBeginLine(int beginLine)
		{
			this.beginLine = beginLine;
		}

		/// <param name="beginColumn">the beginColumn to set</param>
		protected internal virtual void SetBeginColumn(int beginColumn)
		{
			this.beginColumn = beginColumn;
		}
	}
}
