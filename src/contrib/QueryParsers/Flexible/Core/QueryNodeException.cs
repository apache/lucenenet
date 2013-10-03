using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core
{
    public class QueryNodeException : Exception, INLSException
    {
        protected IMessage message = new Message(QueryParserMessages.EMPTY_MESSAGE);

        public QueryNodeException(IMessage message)
            : base(message.Key)
        {
            this.message = message;
        }

        public QueryNodeException(Exception throwable)
            : base("", throwable)
        {
        }

        public QueryNodeException(IMessage message, Exception throwable)
            : base(message.Key, throwable)
        {
            this.message = message;
        }

        public IMessage MessageObject
        {
            get { return this.message; }
        }

        public override string Message
        {
            get
            {
                return GetLocalizedMessage();
            }
        }

        public string GetLocalizedMessage()
        {
            return GetLocalizedMessage(CultureInfo.CurrentCulture);
        }

        public string GetLocalizedMessage(CultureInfo locale)
        {
            return this.message.GetLocalizedMessage(locale);
        }

        public override string ToString()
        {
            return this.message.Key + ": " + GetLocalizedMessage();
        }
    }
}
