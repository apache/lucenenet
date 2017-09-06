using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Globalization;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

namespace Lucene.Net.QueryParsers.Flexible.Core
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// This exception should be thrown if something wrong happens when dealing with
    /// <see cref="Nodes.IQueryNode"/>s.
    /// <para>
    /// It also supports NLS messages.
    /// </para>
    /// </summary>
    /// <seealso cref="IMessage"/>
    /// <seealso cref="NLS"/>
    /// <seealso cref="INLSException"/>
    /// <seealso cref="Nodes.IQueryNode"/>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class QueryNodeException : Exception, INLSException
    {
        protected IMessage m_message = new Message(QueryParserMessages.EMPTY_MESSAGE);

        public QueryNodeException(IMessage message)
            : base(message.Key)
        {
            this.m_message = message;
        }

        public QueryNodeException(Exception throwable)
            : base(throwable.Message, throwable)
        {
        }

        public QueryNodeException(IMessage message, Exception throwable)
            : base(message.Key, throwable)
        {
            this.m_message = message;
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        // For testing
        public QueryNodeException(string message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public QueryNodeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        public virtual IMessage MessageObject
        {
            get { return this.m_message; }
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
            return this.m_message.GetLocalizedMessage(locale);
        }

        public override string ToString()
        {
            return this.m_message.Key + ": " + GetLocalizedMessage();
        }
    }
}
