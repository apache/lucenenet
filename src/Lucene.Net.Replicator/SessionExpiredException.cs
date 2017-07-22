//STATUS: DRAFT - 4.8.0

using System;
using System.IO;
using System.Runtime.Serialization;

namespace Lucene.Net.Replicator
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
    /// Exception indicating that a revision update session was expired due to lack of activity.
    /// </summary>
    /// <remarks>
    /// <see cref="LocalReplicator.DEFAULT_SESSION_EXPIRATION_THRESHOLD"/>
    /// <see cref="LocalReplicator.ExpirationThreshold"/>
    /// 
    /// Lucene.Experimental
    /// </remarks>
    public class SessionExpiredException : IOException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SessionExpiredException()
        {
        }

        public SessionExpiredException(string message) 
            : base(message)
        {
        }

        public SessionExpiredException(string message, Exception inner) 
            : base(message, inner)
        {
        }
    }
}