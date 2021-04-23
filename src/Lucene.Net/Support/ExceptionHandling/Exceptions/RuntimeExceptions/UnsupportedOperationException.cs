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
    /// Thrown to indicate that the requested operation is not supported.
    /// <para/>
    /// This class is a member of the <a href="https://docs.oracle.com/javase/7/docs/technotes/guides/collections/index.html">Java Collections Framework</a>.
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsUnsupportedOperationException(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsUnsupportedOperationException())
    /// </code>
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class UnsupportedOperationException : NotSupportedException
    {
        [Obsolete("Use UnsupportedOperationException.Create() instead.", error: true)]
        public UnsupportedOperationException()
        {
        }

        [Obsolete("Use UnsupportedOperationException.Create() instead.", error: true)]
        public UnsupportedOperationException(string message) : base(message)
        {
        }

        [Obsolete("Use UnsupportedOperationException.Create() instead.", error: true)]
        public UnsupportedOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        [Obsolete("Use UnsupportedOperationException.Create() instead.", error: true)]
        public UnsupportedOperationException(Exception cause)
            : base(cause?.ToString(), cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected UnsupportedOperationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        // Static factory methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create() => new NotSupportedException();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message) => new NotSupportedException(message);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, Exception innerException) => new NotSupportedException(message, innerException);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(Exception cause) => new NotSupportedException(cause.Message, cause);
    }
}
