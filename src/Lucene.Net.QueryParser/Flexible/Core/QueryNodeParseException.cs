using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core
{
    /// <summary>
    /// This should be thrown when an exception happens during the query parsing from
    /// string to the query node tree.
    /// </summary>
    /// <seealso cref="QueryNodeException"/>
    /// <seealso cref="ISyntaxParser"/>
    /// <seealso cref="IQueryNode"/>
    [Serializable]
    public class QueryNodeParseException : QueryNodeException
    {
        private string query;

        private int beginColumn = -1;

        private int beginLine = -1;

        private string errorToken = "";

        public QueryNodeParseException(IMessage message)
            : base(message)
        {
        }

        public QueryNodeParseException(Exception throwable)
            : base(throwable)
        {
        }

        public QueryNodeParseException(IMessage message, Exception throwable)
            : base(message, throwable)
        {
        }

        public virtual void SetQuery(string query)
        {
            this.query = query;
            this.message = new MessageImpl(
                QueryParserMessages.INVALID_SYNTAX_CANNOT_PARSE, query, "");
        }

        public virtual string Query
        {
            get { return this.query; }
        }

        /// <summary>
        /// The errorToken in the query
        /// </summary>
        public virtual string ErrorToken
        {
            get { return this.errorToken; }
            protected set { this.errorToken = value; }
        }

        public virtual void SetNonLocalizedMessage(IMessage message)
        {
            this.message = message;
        }

        /**
         * For EndOfLine and EndOfFile ("&lt;EOF&gt;") parsing problems the last char in the
         * string is returned For the case where the parser is not able to figure out
         * the line and column number -1 will be returned
         * 
         * @return line where the problem was found
         */
        public virtual int BeginLine
        {
            get { return this.beginLine; }
            protected set { this.beginLine = value; }
        }

        /**
         * For EndOfLine and EndOfFile ("&lt;EOF&gt;") parsing problems the last char in the
         * string is returned For the case where the parser is not able to figure out
         * the line and column number -1 will be returned
         * 
         * @return column of the first char where the problem was found
         */
        public virtual int BeginColumn
        {
            get { return this.beginColumn; }
            protected set { this.beginColumn = value; }
        }
    }
}
