using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core
{
    /// <summary>
    /// Error class with NLS support
    /// </summary>
    /// <seealso cref="Messages.NLS"/>
    /// <seealso cref="Messages.Message"/>
    [Serializable]
    public class QueryNodeError : Exception, INLSException
    {
        private IMessage message;

        /**
         * @param message
         *          - NLS Message Object
         */
        public QueryNodeError(IMessage message)
            : base(message.Key)
        {
            this.message = message;

        }

        /**
         * @param throwable
         *          - @see java.lang.Error
         */
        public QueryNodeError(Exception throwable)
            : base(throwable.Message, throwable)
        {
        }

        /**
         * @param message
         *          - NLS Message Object
         * @param throwable
         *          - @see java.lang.Error
         */
        public QueryNodeError(IMessage message, Exception throwable)
            : base(message.Key, throwable)
        {
            this.message = message;

        }

        /*
         * (non-Javadoc)
         * 
         * @see org.apache.lucene.messages.NLSException#getMessageObject()
         */

        public virtual IMessage MessageObject
        {
            get { return this.message; }
        }
    }
}
