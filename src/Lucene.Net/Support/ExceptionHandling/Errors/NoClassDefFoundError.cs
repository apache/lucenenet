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
    /// Thrown if the Java Virtual Machine or a ClassLoader instance tries to load in the definition of a class (as part of
    /// a normal method call or as part of creating a new instance using the new expression) and no definition of the class could be found.
    /// <para/>
    /// The searched-for class definition existed when the currently executing class was compiled, but the definition can no longer be found.
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsNoClassDefFoundError(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsNoClassDefFoundError())
    /// </code>
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class NoClassDefFoundError : Exception, IError // LUCENENET: Subclassing Error is not allowed, so we identify with the IError interface and subclass Exception
    {
        [Obsolete("Use NoClassDefFoundError.Create() instead.", error: true)]
        public NoClassDefFoundError()
        {
        }

        [Obsolete("Use NoClassDefFoundError.Create() instead.", error: true)]
        public NoClassDefFoundError(string message) : base(message)
        {
        }

        [Obsolete("Use NoClassDefFoundError.Create() instead.", error: true)]
        public NoClassDefFoundError(string message, Exception innerException) : base(message, innerException)
        {
        }

        [Obsolete("Use NoClassDefFoundError.Create() instead.", error: true)]
        public NoClassDefFoundError(Exception cause) : base(cause?.ToString(), cause)
        {
        }


#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected NoClassDefFoundError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        // Static factory methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create() => new TypeLoadException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message) => new TypeLoadException(message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, Exception innerException) => new TypeLoadException(message, innerException);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(Exception cause) => new TypeLoadException(cause.Message, cause);
    }
}
