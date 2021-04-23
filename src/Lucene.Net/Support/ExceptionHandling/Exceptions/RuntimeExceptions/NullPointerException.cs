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
    /// Thrown when an application attempts to use null in a case where an object is required. These include:
    /// <list type="bullet">
    ///   <item><description>Calling the instance method of a <c>null</c> object.</description></item>
    ///   <item><description>Accessing or modifying the field of a <c>null</c> object.</description></item>
    ///   <item><description>Taking the length of <c>null</c> as if it were an array.</description></item>
    ///   <item><description>Accessing or modifying the slots of <c>null</c> as if it were an array.</description></item>
    ///   <item><description>Throwing <c>null</c> as if it were a Throwable value.</description></item>
    /// </list>
    /// <para/>
    /// Applications should throw instances of this class to indicate other illegal uses of the
    /// <c>null</c> object. NullPointerException objects may be constructed by the virtual machine
    /// as if suppression were disabled and/or the stack trace was not writable.
    /// <para/>
    /// This is a Java compatibility exception, and may be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsNullPointerException(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsNullPointerException())
    /// </code>
    /// The static <see cref="Create()"/> method overloads throw <see cref="ArgumentNullException"/>, which is
    /// what we should throw in guard clauses. However, there are edge cases where it may make sense to throw
    /// <see cref="NullReferenceException"/> instead. One example of this is when in Java an <c>Integer</c>
    /// class is set to a primitive <c>int</c> variable.
    /// <code>
    /// Integer someInt = new Integer(43);<br/>
    /// int primitiveInt = someInt; // Implicit cast by the Java compiler
    /// </code>
    /// If <c>someInt</c> in the above example were set to <c>null</c>, this would still compile, but would
    /// throw <c>NullPointerException</c> at runtime.
    /// <para/>
    /// In .NET, <c>Integer</c> is most often translated to <c>int?</c>, making it nullable but keeping it
    /// a value type. However setting a nullable int to a nullable one in .NET won't compile.
    /// <code>
    /// int? someInt = 43;<br/>
    /// int primitiveInt = someInt; // Compile error
    /// </code>
    /// So, to get the same behavior as in Java (provided the nullable cannot be factored away), the
    /// appropriate translation would be:
    /// <code>
    /// int? someInt = 43;<br/>
    /// int primitiveInt;<br/>
    /// if (someInt.HasValue)<br/>
    ///     primitiveInt = someInt.Value;<br/>
    /// else<br/>
    ///     throw new NullReferenceException();
    /// </code>
    /// However, do note in most cases it would be better to try to refactor so the nullable
    /// (and therefore the exception) isn't required.
    /// <para/>
    /// There are also other edge cases (i.e. <c>null</c> state in the middle of a method where the null value isn't being passed in)
    /// where throwing <see cref="InvalidOperationException"/> may be more sensible, but this sort of change would need to be tested thoroughly.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class NullPointerException : ArgumentNullException
    {
        [Obsolete("Use NullPointerException.Create() instead.", error: true)]
        public NullPointerException()
        {
        }

        [Obsolete("Use NullPointerException.Create() instead.", error: true)]
        public NullPointerException(string message) : base(message)
        {
        }

        [Obsolete("Use NullPointerException.Create() instead.", error: true)]
        public NullPointerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        [Obsolete("Use NullPointerException.Create() instead.", error: true)]
        public NullPointerException(Exception cause)
            : base(cause?.ToString(), cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected NullPointerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create() => new ArgumentNullException();

        /// <summary>
        /// LUCENENET: This overload is for a "direct" translation without passing the name of the argument. In cases where
        /// there is no message and there is a useful argument name, it would make more senes to call <c>new ArgumentNullExcpetion()</c> directly.
        /// Since this class is basically intended as training wheels for those who don't want to bother looking up exception types,
        /// this is probably a reasonable default.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message) => new ArgumentNullException(paramName: null, message);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string paramName, string message) => new ArgumentNullException(paramName, message);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, Exception innerException) => new ArgumentNullException(message, innerException);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(Exception cause) => new ArgumentNullException(cause.Message, cause);
    }
}
