using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
using System.Security.Permissions;
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
    /// Error class with NLS support
    /// </summary>
    /// <seealso cref="NLS"/>
    /// <seealso cref="IMessage"/>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class QueryNodeError : Exception, INLSException
    {
        private readonly IMessage message; // LUCENENET: marked readonly

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

        // LUCENENET: For testing purposes
        internal QueryNodeError(string message)
            : base(message)
        { }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected QueryNodeError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            message = (IMessage)info.GetValue("message", typeof(IMessage));
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("message", message, typeof(IMessage));
        }
#endif

        /// <summary>
        /// <see cref="INLSException.MessageObject"/> 
        /// </summary>
        public virtual IMessage MessageObject => this.message;
    }
}
