using Lucene.Net.Replicator.Http;
using Lucene.Net.Replicator.Http.Abstractions;
using Microsoft.AspNetCore.Http;

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

    //Note: LUCENENET specific
    public static class AspNetCoreReplicationServiceExtentions
    {
        /// <summary>
        /// Extension method that mirrors the signature of <see cref="IReplicationService.Perform"/> using AspNetCore as implementation.
        /// </summary>
        public static void Perform(this IReplicationService self, HttpRequest request, HttpResponse response)
        {
            self.Perform(new AspNetCoreReplicationRequest(request), new AspNetCoreReplicationResponse(response));
        }
    }
}