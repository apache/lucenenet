using System;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif
#nullable enable

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
    /// </summary>
    /// <seealso cref="Nodes.IQueryNode"/>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    // LUCENENET specific: Refactored constructors to be more like a .NET type and eliminated IMessage/NLS support.
    public class QueryNodeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="QueryNodeException"/>
        /// with the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public QueryNodeException(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QueryNodeException"/>
        /// with a specified inner exception
        /// that is the cause of this exception.
        /// </summary>
        /// <param name="cause">An exception instance to wrap</param>
        public QueryNodeException(Exception? cause)
            : base(cause?.Message, cause)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QueryNodeException"/>
        /// with a specified error message and a reference to the inner exception
        /// that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">An exception instance to wrap</param>
        public QueryNodeException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }


#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected QueryNodeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
