using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core
{
    public class QueryNodeParseException : QueryNodeException
    {
        private ICharSequence query;

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

        public ICharSequence Query
        {
            get { return this.query; }
            set
            {
                this.query = value;
                this.message = new Message(QueryParserMessages.INVALID_SYNTAX_CANNOT_PARSE, query, "");
            }
        }

        public string ErrorToken
        {
            get { return this.errorToken; }
            set
            {
                this.errorToken = value;
            }
        }

        public void SetNonLocalizedMessage(IMessage message)
        {
            this.message = message;
        }

        public int BeginLine
        {
            get { return this.beginLine; }
            set { this.beginLine = value; }
        }

        public int BeginColumn
        {
            get { return this.beginColumn; }
            set { this.beginColumn = value; }
        }
    }
}
