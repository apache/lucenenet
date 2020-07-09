using Lucene.Net.Replicator.Http.Abstractions;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Lucene.Net.Replicator.AspNetCore
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
    /// Implementation of the <see cref="IReplicationResponse"/> abstraction for the AspNetCore framework.
    /// </summary>
    /// <remarks>
    /// .NET Specific Implementation of the Lucene Replicator using AspNetCore  
    /// </remarks>
    //Note: LUCENENET specific
    public class AspNetCoreReplicationResponse : IReplicationResponse
    {
        private readonly HttpResponse response;

        /// <summary>
        /// Creates a <see cref="IReplicationResponse"/> wrapper around the provided <see cref="HttpResponse"/>
        /// </summary>
        /// <param name="response">the response to wrap</param>
        public AspNetCoreReplicationResponse(HttpResponse response)
        {
            this.response = response;
        }

        /// <summary>
        /// Gets or sets the http status code of the response.
        /// </summary>
        public int StatusCode
        {
            get => response.StatusCode;
            set => response.StatusCode = value;
        }

        /// <summary>
        /// The response content.
        /// </summary>
        /// <remarks>
        /// This simply returns the <see cref="HttpResponse.Body"/>.
        /// </remarks>
        public Stream Body => response.Body;

        /// <summary>
        /// Flushes the reponse to the underlying response stream.
        /// </summary>
        /// <remarks>
        /// This simply calls <see cref="Stream.Flush"/> on the <see cref="HttpResponse.Body"/>.
        /// </remarks>
        public void Flush()
        {
            response.Body.Flush();
        }
    }
}