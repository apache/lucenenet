using System.IO;

namespace Lucene.Net.Replicator.Http.Abstractions
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
    /// Abstraction for remote replication response, allows easy integration into any hosting frameworks.
    /// </summary>
    /// <remarks>
    /// .NET Specific Abstraction
    /// </remarks>
    //Note: LUCENENET specific
    public interface IReplicationResponse
    {
        /// <summary>
        /// Gets or sets the http status code of the response.
        /// </summary>
        int StatusCode { get; set; }

        /// <summary>
        /// The response content.
        /// </summary>
        Stream Body { get; }
    }
}
