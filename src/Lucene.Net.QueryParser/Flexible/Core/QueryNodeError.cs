using Lucene.Net.QueryParsers.Flexible.Messages;
using System;

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
    /// Error class with NLS support
    /// </summary>
    /// <seealso cref="NLS"/>
    /// <seealso cref="IMessage"/>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class QueryNodeError : Exception, INLSException
    {
        private IMessage message;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">NLS Message Object</param>
        public QueryNodeError(IMessage message)
            : base(message.Key)
        {
            this.message = message;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="throwable">An exception instance to wrap</param>
        public QueryNodeError(Exception throwable)
            : base(throwable.Message, throwable)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message">NLS Message Object</param>
        /// <param name="throwable">An exception instance to wrap</param>
        public QueryNodeError(IMessage message, Exception throwable)
            : base(message.Key, throwable)
        {
            this.message = message;
        }

        /// <summary>
        /// <see cref="INLSException.MessageObject"/> 
        /// </summary>
        public virtual IMessage MessageObject
        {
            get { return this.message; }
        }
    }
}
