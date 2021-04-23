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
    /// Thrown to indicate that a method has been passed an illegal or inappropriate argument.
    /// <para/>
    /// This is a Java compatibility exception, and can be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsIllegalArgumentException(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsIllegalArgumentException())
    /// </code>
    /// <para/>
    /// Note that in .NET we should aim to provide the specialized <see cref="ArgumentNullException"/>
    /// and <see cref="ArgumentOutOfRangeException"/> when appropriate, and since both of them subclass
    /// <see cref="ArgumentException"/> these are not breaking changes. Unlike in Java, .NET <see cref="ArgumentException"/>
    /// types also accept a <c>paramName</c> argument to provide more information about the nature of the exception.
    /// <para/>
    /// Note also that in Java it is not common practice to use guard clauses. For this reason, we can improve the code
    /// by adding them when we are sure that, for example, <c>null</c> is not a valid argument. <see cref="NullReferenceException"/>
    /// is always a bug, <see cref="ArgumentNullException"/> is a fail-fast way of avoiding <see cref="NullReferenceException"/>.
    /// That said, care must be taken not to disallow <c>null</c> when it is a valid value. The appropriate way to translate is
    /// usually to add an additional method overload without the nullable argument and to ensure that the one with the argument is
    /// never passed a <c>null</c> value.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class IllegalArgumentException : ArgumentException
    {
        public IllegalArgumentException()
        {
        }

        public IllegalArgumentException(string message) : base(message)
        {
        }

        public IllegalArgumentException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public IllegalArgumentException(Exception cause)
            : base(cause?.ToString(), cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected IllegalArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create() => new ArgumentException();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message) => new ArgumentException(message);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, Exception innerException) => new ArgumentException(message, innerException);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, string paramName) => new ArgumentException(message, paramName);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, string paramName, Exception innerException) => new ArgumentException(message, paramName, innerException);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(Exception cause) => new ArgumentException(cause.Message, cause);
    }
}
