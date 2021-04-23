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
    /// The Java description is:
    /// <para/>
    /// Thrown when an application tries to load in a class through its string name using:
    /// <list type="bullet">
    ///     <item><description>The <c>forName</c> method in class <c>Class</c>. </description></item>
    ///     <item><description>The <c>findSystemClass</c> method in class <c>ClassLoader</c>.</description></item>
    ///     <item><description>The <c>loadClass</c> method in class <c>ClassLoader</c>.</description></item>
    /// </list>
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsClassNotFoundException(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsClassNotFoundException())
    /// </code>
    /// <para/>
    /// IMPORTANT: .NET doesn't behave the same way as Java in this regard. The <see cref="Type.GetType(string)"/> method
    /// may throw <see cref="TypeLoadException"/> if a static initializer fails, but usually returns <c>null</c> if
    /// the type is not resolved. So, for compatibility the logic should be adjusted to treat <c>null</c> like a
    /// <see cref="ClassNotFoundException"/> in Java. If the method is expected to throw <see cref="ClassNotFoundException"/>
    /// when the type cannot be resolved, then we must explictly throw it when <see cref="Type.GetType(string)"/> returns <c>null</c>.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class ClassNotFoundException : Exception
    {
        [Obsolete("Use ClassNotFoundException.Create() instead.", error: true)]
        public ClassNotFoundException()
        {
        }

        [Obsolete("Use ClassNotFoundException.Create() instead.", error: true)]
        public ClassNotFoundException(string message) : base(message)
        {
        }

        [Obsolete("Use ClassNotFoundException.Create() instead.", error: true)]
        public ClassNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        [Obsolete("Use ClassNotFoundException.Create() instead.", error: true)]
        public ClassNotFoundException(Exception cause)
            : base(cause?.ToString(), cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ClassNotFoundException(SerializationInfo info, StreamingContext context)
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
