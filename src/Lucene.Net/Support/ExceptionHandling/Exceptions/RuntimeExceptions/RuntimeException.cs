using Lucene.Net.Util;
using System;
using System.Runtime.Serialization;

namespace Lucene
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
    /// RuntimeException is the superclass of those exceptions that can be thrown during the normal
    /// operation of the Java Virtual Machine.
    /// <para/>
    /// RuntimeException and its subclasses are unchecked exceptions.Unchecked exceptions do not
    /// need to be declared in a method or constructor's throws clause if they can be thrown by the
    /// execution of the method or constructor and propagate outside the method or constructor boundary.
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsRuntimeException(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsRuntimeException())
    /// </code>
    /// <para/>
    /// RuntimeException can be thrown, but cannot be subclassed in C# because it is internal.
    /// For all Lucene exceptions that subclass RuntimeException, implement the <see cref="IRuntimeException"/>
    /// interface, then choose the most logical exception type in .NET to subclass.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class RuntimeException : Exception, IRuntimeException
    {
        [Obsolete("Use RuntimeException.Create() instead.", error: true)]
        public RuntimeException()
        {
        }

        [Obsolete("Use RuntimeException.Create() instead.", error: true)]
        public RuntimeException(string message) : base(message)
        {
        }

        [Obsolete("Use RuntimeException.Create() instead.", error: true)]
        public RuntimeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        [Obsolete("Use RuntimeException.Create() instead.", error: true)]
        public RuntimeException(Exception cause)
            : base(cause?.ToString(), cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected RuntimeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        // Static factory methods

        public static Exception Create() => new LuceneSystemException();

        public static Exception Create(string message) => new LuceneSystemException(message);

        public static Exception Create(string message, Exception innerException) => new LuceneSystemException(message, innerException);

        public static Exception Create(Exception cause) => new LuceneSystemException(cause.Message, cause);
    }
}
