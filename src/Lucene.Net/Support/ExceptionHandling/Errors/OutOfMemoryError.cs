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
    /// Thrown when the Java Virtual Machine cannot allocate an object because
    /// it is out of memory, and no more memory could be made available by the
    /// garbage collector. OutOfMemoryError objects may be constructed by the virtual
    /// machine as if <a href="https://docs.oracle.com/javase/7/docs/api/java/lang/Throwable.html#Throwable(java.lang.String,%20java.lang.Throwable,%20boolean,%20boolean)">
    /// suppression were disabled and/or the stack trace was not writable.</a>
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsOutOfMemoryError(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsOutOfMemoryError())
    /// </code>
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class OutOfMemoryError : OutOfMemoryException, IError
    {
        [Obsolete("Use OutOfMemoryError.Create() instead.", error: true)]
        public OutOfMemoryError()
        {
        }

        [Obsolete("Use OutOfMemoryError.Create() instead.", error: true)]
        public OutOfMemoryError(string message) : base(message)
        {
        }

        [Obsolete("Use OutOfMemoryError.Create() instead.", error: true)]
        public OutOfMemoryError(string message, Exception innerException) : base(message, innerException)
        {
        }

        [Obsolete("Use OutOfMemoryError.Create() instead.", error: true)]
        public OutOfMemoryError(Exception cause) : base(cause?.ToString(), cause)
        {
        }


#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected OutOfMemoryError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        // Static factory methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create() => new OutOfMemoryException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message) => new OutOfMemoryException(message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, Exception innerException) => new OutOfMemoryException(message, innerException);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(Exception cause) => new OutOfMemoryException(cause.Message, cause);
    }
}
