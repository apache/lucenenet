using System;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

namespace Lucene.Net.Util
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
    /// Thrown by Lucene on detecing that <see cref="System.Threading.Thread.Interrupt()"/> had been
    /// called. This exception has the specific purpose of being allowed to pass through to the
    /// calling thread of <see cref="J2N.Threading.ThreadJob"/> so it reaches the appropriate handler.
    /// </summary>
    // LUCENENET: In Lucene, this exception was so it could be re-thrown unchecked. It has been
    // re-purposed in .NET but used in all the same scenerios.
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class ThreadInterruptedException : Exception, IRuntimeException
    {
        public ThreadInterruptedException(Exception interruptedException)
            : base(interruptedException.Message, interruptedException)
        {
        }

        public ThreadInterruptedException(string message) : base(message)
        {
        }

        public ThreadInterruptedException(string message, Exception innerException) : base(message, innerException)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ThreadInterruptedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
