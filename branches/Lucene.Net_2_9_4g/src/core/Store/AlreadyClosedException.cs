/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Runtime.Serialization;

namespace Lucene.Net.Store
{

    // JAVA: src/java/org/apache/lucene/store/AlreadyClosedException.java
	
	/// <summary> 
    /// This exception is thrown when there is an attempt to access a resource 
    /// that has already been closed.
	/// </summary>
    /// <remarks>
    ///     <para>
    ///         An example would be when a <see cref="Lucene.Net.Analysis.TokenStream"/> has already been closed. 
    ///     </para>
    /// </remarks>
	[Serializable]
	public class AlreadyClosedException : System.SystemException
	{
        /// <summary>
        /// Initializes a new instance of <see cref="AlreadyClosedException"/> with a message and <c>null</c> inner exception.
        /// </summary>
        /// <param name="message">
        ///     A <c>String</c> that describes the error. The content of message is intended to be understood 
        ///     by humans. The caller of this constructor is required to ensure that this string has been 
        ///     localized for the current system culture. 
        /// </param>
        /// <remarks>
        ///     <para>
        ///         The constructor initializes the <see cref="System.Exception.Message"/> property of the new instance using message.
        ///     </para>
        /// </remarks>
		public AlreadyClosedException(System.String message):base(message)
		{
		}

        /// <summary>
        /// Initializes a new instance of <see cref="AlreadyClosedException"/> with a message and inner exception.
        /// </summary>
        /// <param name="message">
        ///     A <c>String</c> that describes the error. The content of message is intended to be understood 
        ///     by humans. The caller of this constructor is required to ensure that this string has been 
        ///     localized for the current system culture. 
        /// </param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception. If the <paramref name="innerException"/> parameter is not null, the 
        ///     current exception is raised in a catch block that handles the inner exception. 
        /// </param>
        /// <remarks>
        ///     <para>
        ///         An exception that is thrown as a direct result of a previous exception should include a reference to the 
        ///         previous exception in the <see cref="System.Exception.InnerException"/> property. The <see cref="System.Exception.InnerException"/> property 
        ///         returns the same value that is passed into the constructor, or <c>null</c> if 
        ///         the <see cref="System.Exception.InnerException"/> property does not supply the inner 
        ///         exception value to the constructor.
        ///     </para>
        /// </remarks>
        public AlreadyClosedException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlreadyClosedException"/> class with the specified serialization and context information.
        /// </summary>
        /// <param name="info">The data for serializing or deserializing the object. </param>
        /// <param name="context">The source and destination for the object. </param>
        // REFACTOR: add build conditional to only compile in the client and full versions of the .NET framework.
        protected AlreadyClosedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
	}
}