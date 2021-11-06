using J2N.IO;
using System;
using System.IO;
using System.Net.Http;

namespace Lucene.Net.Replicator.Http
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
    /// An HTTP implementation of <see cref="IReplicator"/>. Assumes the API supported by <see cref="ReplicationService"/>.
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    public class HttpReplicator : HttpClientBase, IReplicator
    {
        /// <summary>
        /// Creates a new <see cref="HttpReplicator"/> with the given host, port and path.
        /// <see cref="HttpClientBase(string, int, string, HttpMessageHandler)"/> for more details.
        /// </summary>
        public HttpReplicator(string host, int port, string path, HttpMessageHandler messageHandler = null)
            : base(host, port, path, messageHandler)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpReplicator"/> with the given url.
        /// <see cref="HttpClientBase(string, HttpMessageHandler)"/> for more details.
        /// </summary>
        //Note: LUCENENET Specific
        public HttpReplicator(string url, HttpMessageHandler messageHandler = null)
            : this(url, new HttpClient(messageHandler ?? new HttpClientHandler()) { Timeout = DEFAULT_TIMEOUT })
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpReplicator"/> with the given <paramref name="url"/> and <see cref="HttpClient"/>.
        /// <see cref="HttpClientBase(string, HttpClient)"/> for more details.
        /// </summary>
        //Note: LUCENENET Specific
        public HttpReplicator(string url, HttpClient client)
            : base(url, client)
        {
        }

        /// <summary>
        /// Checks for updates at the remote host.
        /// </summary>
        public virtual SessionToken CheckForUpdate(string currentVersion)
        {
            string[] parameters = null;
            if (currentVersion != null)
                parameters = new[] { ReplicationService.REPLICATE_VERSION_PARAM, currentVersion };

            HttpResponseMessage response = base.ExecuteGet(ReplicationService.ReplicationAction.UPDATE.ToString(), parameters);
            return DoAction(response, () =>
            {
                using DataInputStream inputStream = new DataInputStream(GetResponseStream(response));
                return inputStream.ReadByte() == 0 ? null : new SessionToken(inputStream);
            });
        }

        /// <summary>
        /// Obtains the given file from it's source at the remote host.
        /// </summary>
        public virtual Stream ObtainFile(string sessionId, string source, string fileName)
        {
            HttpResponseMessage response = ExecuteGet(ReplicationService.ReplicationAction.OBTAIN.ToString(),
                ReplicationService.REPLICATE_SESSION_ID_PARAM, sessionId,
                ReplicationService.REPLICATE_SOURCE_PARAM, source,
                ReplicationService.REPLICATE_FILENAME_PARAM, fileName);
            return DoAction(response, false, () => GetResponseStream(response));
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">this replicator implementation does not support remote publishing of revisions</exception>
        public virtual void Publish(IRevision revision)
        {
            throw UnsupportedOperationException.Create("this replicator implementation does not support remote publishing of revisions");
        }

        /// <summary>
        /// Releases a session obtained from the remote host.
        /// </summary>
        public virtual void Release(string sessionId)
        {
            HttpResponseMessage response = ExecuteGet(ReplicationService.ReplicationAction.RELEASE.ToString(), ReplicationService.REPLICATE_SESSION_ID_PARAM, sessionId);
            // do not remove this call: as it is still validating for us!
            DoAction<object>(response, () => null);
        }
    }
}