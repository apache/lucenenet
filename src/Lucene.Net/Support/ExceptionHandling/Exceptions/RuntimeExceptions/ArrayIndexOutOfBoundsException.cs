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
    /// Thrown to indicate that an array has been accessed with an illegal index.
    /// The index is either negative or greater than or equal to the size of the array.
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsArrayIndexOutOfBoundsException(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsArrayIndexOutOfBoundsException())
    /// </code>
    /// <para/>
    /// Note that when an array type is translated to .NET that uses an indexer property <c>this[index]</c>,
    /// we should instead throw <see cref="IndexOutOfRangeException"/> for that property only.
    /// In all other cases, use an overload of <see cref="Create()"/>.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class ArrayIndexOutOfBoundsException : ArgumentOutOfRangeException
    {
        [Obsolete("Use ArrayIndexOutOfBoundsException.Create() instead.", error: true)]
        public ArrayIndexOutOfBoundsException()
        {
        }

        [Obsolete("Use ArrayIndexOutOfBoundsException.Create() instead.", error: true)]
        public ArrayIndexOutOfBoundsException(string message) : base(message)
        {
        }

        [Obsolete("Use ArrayIndexOutOfBoundsException.Create() instead.", error: true)]
        public ArrayIndexOutOfBoundsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        [Obsolete("Use ArrayIndexOutOfBoundsException.Create() instead.", error: true)]
        public ArrayIndexOutOfBoundsException(Exception cause)
            : base(cause?.ToString(), cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ArrayIndexOutOfBoundsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        // Static factory methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create() => new ArgumentOutOfRangeException();

        /// <summary>
        /// LUCENENET: This overload is for a "direct" translation without passing the name of the argument. In cases where
        /// there is no message and there is a useful argument name, it would make more senes to call <c>new ArgumentOutOfRangeException()</c> directly.
        /// Since this class is basically intended as training wheels for those who don't want to bother looking up exception types,
        /// this is probably a reasonable default.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message) => new ArgumentOutOfRangeException(paramName: null, message);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string paramName, string message) => new ArgumentOutOfRangeException(paramName, message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string paramName, object actualValue, string message) => new ArgumentOutOfRangeException(paramName, actualValue, message);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, Exception innerException) => new ArgumentOutOfRangeException(message, innerException);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(Exception cause) => new ArgumentOutOfRangeException(cause.Message, cause);
    }
}

