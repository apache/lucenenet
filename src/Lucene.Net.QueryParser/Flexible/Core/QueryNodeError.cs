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
    /// A query node error.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    // LUCENENET specific: Added IError for identification of the Java superclass in .NET.
    // LUCENENET specific: Subclassed ArgumentException, since in all cases, we are validating arguments. However, this exception is also filtered using the IsException() extension method in catch blocks.
    // LUCENENET specific: Refactored constructors to be more like a .NET type and eliminated IMessage/NLS support.
    public class QueryNodeError : ArgumentException, IError
    {
        /// <summary>
        /// Initializes a new instance of <see cref="QueryNodeError"/>
        /// with the specified <paramref name="message"/> and <paramref name="paramName"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the current error.</param>
        /// <param name="message">The message that describes the error.</param>
        public QueryNodeError(string? message, string? paramName)
            : base(message, paramName)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QueryNodeError"/>
        /// with the specified <paramref name="message"/>, <paramref name="paramName"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the current error.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.
        /// If the <paramref name="innerException"/> parameter is not a <c>null</c> reference, the
        /// current exception is raised in a catch block that handles the inner exception.</param>
        public QueryNodeError(string? message, string? paramName, Exception? innerException)
            : base(message, paramName, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QueryNodeError"/>
        /// with the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public QueryNodeError(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QueryNodeError"/>
        /// with a specified inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.
        /// If the <paramref name="innerException"/> parameter is not a <c>null</c> reference, the
        /// current exception is raised in a catch block that handles the inner exception.</param>
        public QueryNodeError(Exception? innerException)
            : base(innerException?.Message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="QueryNodeError"/>
        /// with a specified error message and a reference to the inner exception
        /// that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.
        /// If the <paramref name="innerException"/> parameter is not a <c>null</c> reference, the
        /// current exception is raised in a catch block that handles the inner exception.</param>
        public QueryNodeError(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected QueryNodeError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
