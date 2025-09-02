using Lucene.Net.Replicator.Http.Abstractions;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.Replicator.Net
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
    /// A concrete implementation of <see cref="IReplicationResponse"/> for supporting
    /// <see cref="HttpListener"/>.
    /// </summary>
    public class HttpListenerReplicationResponse : IReplicationResponse
    {
        private readonly HttpListenerResponse _response;

        public HttpListenerReplicationResponse(HttpListenerResponse response) => _response = response;

        public int StatusCode
        {
            get => _response.StatusCode;
            set => _response.StatusCode = value;
        }

        public Stream Body => _response.OutputStream;

        public void Flush() => _response.OutputStream.Flush();

        public Task FlushAsync(CancellationToken cancellationToken = default) =>
            _response.OutputStream.FlushAsync(cancellationToken);
    }
}
