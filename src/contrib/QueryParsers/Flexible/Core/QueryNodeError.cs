using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core
{
    public class QueryNodeError : Exception, INLSException
    {
        private IMessage message;

        public QueryNodeError(IMessage message)
            : base(message.Key)
        {
            this.message = message;
        }

        public QueryNodeError(Exception throwable)
            : base("", throwable)
        {
        }

        public QueryNodeError(IMessage message, Exception throwable)
            : base(message.Key, throwable)
        {
            this.message = message;
        }

        public IMessage MessageObject
        {
            get { return this.message; }
        }
    }
}
