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
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsNullPointerException(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsNullPointerException())
    /// </code>
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class NullPointerException : ArgumentNullException
    {
        public NullPointerException()
        {
        }

        public NullPointerException(string message) : base(message)
        {
        }

        public NullPointerException(string message, Exception innerException) : base(message, innerException)
        {
        }

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
    }
}
