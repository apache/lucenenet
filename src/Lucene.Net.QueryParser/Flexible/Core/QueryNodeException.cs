using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Globalization;

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
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class QueryNodeException : Exception, INLSException
    {
        protected IMessage message = new Message(QueryParserMessages.EMPTY_MESSAGE);

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
