using System;
using System.Threading;
using System.Threading.Tasks;


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
    /// Contract for a replication service.
    /// </summary>
    public interface IReplicationService
    {
        /// <summary>
        /// Executes the replication task.
        /// </summary>
        /// <exception cref="InvalidOperationException">required parameters are missing</exception>
        void Perform(IReplicationRequest request, IReplicationResponse response);

        /// <summary>
        /// Executes the replication task asynchronously.
        /// </summary>
        /// <param name="request">The replication request.</param>
        /// <param name="response">The replication response.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PerformAsync(IReplicationRequest request, IReplicationResponse response, CancellationToken cancellationToken = default);

    }
}
