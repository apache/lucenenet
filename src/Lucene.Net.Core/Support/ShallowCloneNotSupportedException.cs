/**
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
 */

namespace Lucene.Net.Support
{
    using System;

    /// <summary>
    /// Represents an error when a shallow clone is not supported. 
    /// </summary>
    [Serializable]
    public class ShallowCloneNotSupportedException : NotSupportedException
    {
        /// <summary>
        /// Initizalizes a new instance of <see cref="ShallowCloneNotSupportedException"/> class.
        /// </summary>        
        public ShallowCloneNotSupportedException() { }

        /// <summary>
        /// Initizalizes a new instance of <see cref="ShallowCloneNotSupportedException"/> class with the type
        /// that does not allow shallow clones.
        /// </summary>        
        /// <param name="type">The class type that does not allow shallow clones.</param>
        public ShallowCloneNotSupportedException(Type type) : 
            base(string.Format("{0} does support shallow clones. Use Clone(true) instead.", type.FullName))
        {

        }

        /// <summary>
        /// Initizalizes a new instance of <see cref="ShallowCloneNotSupportedException"/> class with a specified message.
        /// </summary>
        /// <param name="message">A string that describes the error that is meant to be understood by humans.</param>
        public ShallowCloneNotSupportedException(string message) : base(message) { }

        /// <summary>
        /// Initizalizes a new instance of <see cref="ShallowCloneNotSupportedException"/> class with a specified message and a 
        /// reference to the inner exception that caused this exception.
        /// </summary>
        /// <param name="message">A string that describes the error that is meant to be understood by humans.</param>
        /// <param name="inner">A reference to the exception that caused this one.</param>
        public ShallowCloneNotSupportedException(string message, Exception inner) : base(message, inner) { }

#if !PORTABLE && !K10
        protected ShallowCloneNotSupportedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
#endif
    }

}
