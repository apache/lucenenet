// LUCENENET specific - commented this because we already have an ObjectDisposedException in .NET.
// This is just reinventing the wheel. However ObjectDisposedException subclasses InvalidOperationException,
// while AlreadyClosedException, subclasses IOException, so we patch this behavior in ExceptionExtensions.
// There is a duplicate type in the Lucene namespace that acts as a static factory to create ObjectDisposedException,
// which is intended to make porting efforts easier.
//using System;
//#if FEATURE_SERIALIZABLE_EXCEPTIONS
//using System.Runtime.Serialization;
//#endif

//namespace Lucene.Net.Store
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    /// <summary>
//    /// this exception is thrown when there is an attempt to
//    /// access something that has already been closed.
//    /// </summary>
//    // LUCENENET: It is no longer good practice to use binary serialization. 
//    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
//#if FEATURE_SERIALIZABLE_EXCEPTIONS
//    [Serializable]
//#endif
//    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "ObjectDisposedExcption already violates this rule by not having a default constructor.")]
//    internal class AlreadyClosedException : ObjectDisposedException // LUCENENET specific: marked internal, as we only want users to catch ObjectDisposedException
//    {
//        public AlreadyClosedException(string message)
//            : base(message)
//        {
//        }

//        public AlreadyClosedException(string message, Exception innerException) : base(message, innerException) // LUCENENET: CA1032: Implement standard exception constructors
//        {
//        }

//#if FEATURE_SERIALIZABLE_EXCEPTIONS
//        /// <summary>
//        /// Initializes a new instance of this class with serialized data.
//        /// </summary>
//        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
//        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
//        protected AlreadyClosedException(SerializationInfo info, StreamingContext context)
//            : base(info, context)
//        {
//        }
//#endif
//    }
//}