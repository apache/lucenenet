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
    /// Thrown by String methods to indicate that an index is either negative or greater than the size of the string.
    /// For some methods such as the charAt method, this exception also is thrown when the index is equal to the size of the string.
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsStringIndexOutOfBoundsException(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsStringIndexOutOfBoundsException())
    /// </code>
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class StringIndexOutOfBoundsException : IndexOutOfBoundsException
    {
        public StringIndexOutOfBoundsException()
        {
        }

        public StringIndexOutOfBoundsException(string message) : base(message)
        {
        }

        public StringIndexOutOfBoundsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public StringIndexOutOfBoundsException(Exception cause)
            : base(cause?.ToString(), cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected StringIndexOutOfBoundsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
