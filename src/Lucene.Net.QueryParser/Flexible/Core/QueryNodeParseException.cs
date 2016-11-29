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

        public virtual string GetQuery()
        {
            return this.query;
        }

        /**
         * @param errorToken
         *          the errorToken in the query
         */
        protected virtual void SetErrorToken(string errorToken)
        {
            this.errorToken = errorToken;
        }

        public virtual string GetErrorToken()
        {
            return this.errorToken;
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
        public virtual int GetBeginLine()
        {
            return this.beginLine;
        }

        /**
         * For EndOfLine and EndOfFile ("&lt;EOF&gt;") parsing problems the last char in the
         * string is returned For the case where the parser is not able to figure out
         * the line and column number -1 will be returned
         * 
         * @return column of the first char where the problem was found
         */
        public virtual int GetBeginColumn()
        {
            return this.beginColumn;
        }

        /**
         * @param beginLine
         *          the beginLine to set
         */
        protected virtual void SetBeginLine(int beginLine)
        {
            this.beginLine = beginLine;
        }

        /**
         * @param beginColumn
         *          the beginColumn to set
         */
        protected virtual void SetBeginColumn(int beginColumn)
        {
            this.beginColumn = beginColumn;
        }
    }
}
