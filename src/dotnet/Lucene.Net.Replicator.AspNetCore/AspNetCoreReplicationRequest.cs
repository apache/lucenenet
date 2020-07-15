using Lucene.Net.Replicator.Http.Abstractions;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

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
    /// Abstraction for remote replication requests, allows easy integration into any hosting frameworks.
    /// </summary>
    /// <remarks>
    /// .NET Specific Implementation of the Lucene Replicator using AspNetCore  
    /// </remarks>
    //Note: LUCENENET specific
    public class AspNetCoreReplicationRequest : IReplicationRequest
    {
        private readonly HttpRequest request;

        /// <summary>
        /// Creates a <see cref="IReplicationRequest"/> wrapper around the provided <see cref="HttpRequest"/>
        /// </summary>
        /// <param name="request">the request to wrap</param>
        public AspNetCoreReplicationRequest(HttpRequest request)
        {
            this.request = request;
        }

        /// <summary>
        /// Provides the requested path which mapps to a replication operation.
        /// </summary>
        public string Path => request.PathBase + request.Path;

        /// <summary>
        /// Returns the requested query parameter or null if not present.
        /// Throws an exception if the same parameter is provided multiple times.
        /// </summary>
        /// <param name="name">the name of the requested parameter</param>
        /// <returns>the value of the requested parameter or null if not present</returns>
        /// <exception cref="InvalidOperationException">More than one parameter with the name was given.</exception>
        public string QueryParam(string name)
        {
            return request.Query[name].SingleOrDefault();
        }
    }
}
