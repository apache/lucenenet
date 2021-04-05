using System;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

namespace Lucene.Net.Diagnostics
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
    /// Thrown to indicate that an assertion has failed.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class AssertionException : Exception, IError
    {
        /// <summary>
        /// Constructs an <see cref="AssertionException"/> with no detail message.
        /// </summary>
        public AssertionException() { }

        /// <summary>
        /// Constructs an <see cref="AssertionException"/> with the provided <paramref name="message"/>.
        /// </summary>
        /// <param name="message">Value to be used as the assertion message.</param>
        public AssertionException(string message)
            : base(message)
        { }

        /// <summary>
        /// Constructs an <see cref="AssertionException"/> with the provided <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">Value to be used as the assertion message.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, 
        /// or a <c>null</c> reference (<c>Nothing</c> in Visual Basic) if no inner exception is specified.</param>
        public AssertionException(string message, Exception innerException)
            : base(message, innerException)
        { }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected AssertionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
