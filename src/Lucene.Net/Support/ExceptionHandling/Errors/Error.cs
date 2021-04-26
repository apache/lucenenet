using Lucene.Net.Diagnostics;
using System;
using System.Runtime.CompilerServices;
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
    /// The Java Error type to simulate the type in Java that all Errors inherit.
    /// <para/>
    /// NOTE: Exception and Error in Java have the same base type, Throwable. However,
    /// that is the only common type so Exceptions can be caught without catching errors.
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsError(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsError())
    /// </code>
    /// <para/>
    /// Error can be thrown, but cannot be subclassed in C# because it is internal.
    /// For all Lucene exceptions that subclass Error, implement the <see cref="IError"/>
    /// interface, then choose the most logical exception type in .NET to subclass.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class Error : Exception, IError
    {
        [Obsolete("Use Error.Create() instead.", error: true)]
        public Error()
        {
        }

        [Obsolete("Use Error.Create() instead.", error: true)]
        public Error(string message) : base(message)
        {
        }

        [Obsolete("Use Error.Create() instead.", error: true)]
        public Error(string message, Exception innerException) : base(message, innerException)
        {
        }

        [Obsolete("Use Error.Create() instead.", error: true)]
        public Error(Exception cause)
            : base(cause?.ToString(), cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected Error(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        // Static factory methods

        // LUCENENET: For now, we are just using AssertionException because it is our only public error type
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create() => new AssertionException();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message) => new AssertionException(message);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, Exception innerException) => new AssertionException(message, innerException);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(Exception cause) => new AssertionException(cause.Message, cause);
    }
}
