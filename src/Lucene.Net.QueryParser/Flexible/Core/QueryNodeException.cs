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
    /// <summary>
    /// This exception should be thrown if something wrong happens when dealing with
    /// {@link QueryNode}s.
    /// <para>
    /// It also supports NLS messages.
    /// </para>
    /// </summary>
    /// <seealso cref="Message"/>
    /// <seealso cref="NLS"/>
    /// <seealso cref="NLSException"/>
    /// <seealso cref="IQueryNode"/>
    [Serializable]
    public class QueryNodeException : Exception, INLSException
    {
        protected IMessage message = new MessageImpl(QueryParserMessages.EMPTY_MESSAGE);

        public QueryNodeException(IMessage message)
            : base(message.Key)
        {
            this.message = message;
        }

        public QueryNodeException(Exception throwable)
            : base(throwable.Message, throwable)
        {
        }

        public QueryNodeException(IMessage message, Exception throwable)
            : base(message.Key, throwable)
        {
            this.message = message;
        }

        public virtual IMessage MessageObject
        {
            get { return this.message; }
        }

        public override string Message
        {
            get { return GetLocalizedMessage(); }
        }

        public virtual string GetLocalizedMessage()
        {
            return GetLocalizedMessage(CultureInfo.InvariantCulture);
        }

        public virtual string GetLocalizedMessage(CultureInfo locale)
        {
            return this.message.GetLocalizedMessage(locale);
        }

        public override string ToString()
        {
            return this.message.Key + ": " + GetLocalizedMessage();
        }
    }
}
