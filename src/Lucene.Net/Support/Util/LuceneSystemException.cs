using System;
using System.Runtime.Serialization;

namespace Lucene.Net.Util
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
    /// A general exception type thrown from Lucene.NET. This corresponds to the
    /// <a href="https://docs.oracle.com/javase/7/docs/api/java/lang/RuntimeException.html">RuntimeException</a>
    /// type in Java.
    /// <para/>
    /// In .NET, <see cref="SystemException"/> is similar, but includes types such as <see cref="System.IO.IOException"/>
    /// that Java does not include. Per the Microsoft documentation:
    /// <para/>
    /// "Because <see cref="SystemException"/> serves as the base class of a variety of exception types, your code
    /// should not throw a <see cref="SystemException"/> exception, nor should it attempt to handle a
    /// <see cref="SystemException"/> exception unless you intend to re-throw the original exception."
    /// <para/>
    /// However, since we are not throwing the original exception, we are making a best effort by wrapping it in a custom
    /// exception that derives from <see cref="SystemException"/>. This will allow for code to catch <see cref="SystemException"/>
    /// for auditing or logging purposes to continue doing so without missing these exceptions.
    /// <para/>
    /// Lucene.NET will throw this exception with an <see cref="Exception.InnerException"/> populated with the actual
    /// exception (normally a <see cref="SystemException"/> in .NET).
    /// The primary reason for throwing a wrapper exception is to eliminate the possibility that the exception will be caught
    /// in one of the numerous catch blocks in Lucene unintentionally, and this is a way to preserve the stack trace
    /// of the original exception when it is rethrown.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class LuceneSystemException : SystemException, IRuntimeException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LuceneSystemException"/>.
        /// </summary>
        public LuceneSystemException()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LuceneSystemException"/>
        /// with the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LuceneSystemException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LuceneSystemException"/>
        /// with the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The original <see cref="Exception"/>.</param>
        public LuceneSystemException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LuceneSystemException"/>
        /// with the specified <paramref name="cause"/>.
        /// </summary>
        /// <param name="cause">The original <see cref="Exception"/>.</param>
        public LuceneSystemException(Exception cause)
            : base(cause?.Message, cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected LuceneSystemException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
