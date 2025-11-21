using J2N.IO;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

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
    public class HttpReplicator : HttpClientBase, IReplicator, IAsyncReplicator
    {
        /// <summary>
        /// Creates a new <see cref="HttpReplicator"/> with the given host, port and path.
        /// <see cref="HttpClientBase(string, int, string, HttpMessageHandler)"/> for more details.
        /// </summary>
        public HttpReplicator(string host, int port, string path, HttpMessageHandler? messageHandler = null)
            : base(host, port, path, messageHandler ?? new HttpClientHandler())
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpReplicator"/> with the given url.
        /// <see cref="HttpClientBase(string, HttpMessageHandler)"/> for more details.
        /// </summary>
        //Note: LUCENENET Specific
        public HttpReplicator(string url, HttpMessageHandler? messageHandler = null)
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
        public virtual SessionToken? CheckForUpdate(string? currentVersion)
        {
            string[]? parameters = null;

            if (!string.IsNullOrEmpty(currentVersion))
            {
                parameters = new[] { ReplicationService.REPLICATE_VERSION_PARAM, currentVersion! }; // [!]: verified above
            }

            var response = base.ExecuteGet(nameof(ReplicationService.ReplicationAction.UPDATE), parameters);

            return DoAction(response, () =>
            {
                using var inputStream = new DataInputStream(GetResponseStream(response));
                return inputStream.ReadByte() == 0 ? null : new SessionToken(inputStream);
            });
        }

        /// <summary>
        /// Obtains the given file from its source at the remote host.
        /// </summary>
        public virtual Stream ObtainFile(string sessionId, string source, string fileName)
        {
            var response = ExecuteGet(nameof(ReplicationService.ReplicationAction.OBTAIN),
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
            var response = ExecuteGet(nameof(ReplicationService.ReplicationAction.RELEASE), ReplicationService.REPLICATE_SESSION_ID_PARAM, sessionId);
            // do not remove this call: as it is still validating for us!
            DoAction<object?>(response, () => null);
        }

        #region Async methods (IAsyncReplicator)

        /// <summary>
        /// Checks for updates at the remote host asynchronously.
        /// </summary>
        /// <param name="currentVersion">The current index version.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// A <see cref="SessionToken"/> if updates are available; otherwise, <c>null</c>.
        /// </returns>
        public async Task<SessionToken?> CheckForUpdateAsync(string? currentVersion, CancellationToken cancellationToken = default)
        {
            string[]? parameters = !string.IsNullOrEmpty(currentVersion)
                ? new[] { ReplicationService.REPLICATE_VERSION_PARAM, currentVersion! } // [!]: verified above
                : null;

            using var response = await ExecuteGetAsync(
                nameof(ReplicationService.ReplicationAction.UPDATE),
                parameters,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return await DoActionAsync(response, async () =>
            {
                // ReSharper disable once AccessToDisposedClosure - DoActionAsync definitively returns after this lambda is invoked
                using var inputStream = new DataInputStream(
                    await GetResponseStreamAsync(response, cancellationToken).ConfigureAwait(false));

                return inputStream.ReadByte() == 0 ? null : new SessionToken(inputStream);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Obtains the given file from the remote host asynchronously.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="source">The source of the file.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Stream"/> of the requested file.</returns>
        public async Task<Stream> ObtainFileAsync(string sessionId, string source, string fileName, CancellationToken cancellationToken = default)
        {
            using var response = await ExecuteGetAsync(
                nameof(ReplicationService.ReplicationAction.OBTAIN),
                ReplicationService.REPLICATE_SESSION_ID_PARAM, sessionId,
                ReplicationService.REPLICATE_SOURCE_PARAM, source,
                ReplicationService.REPLICATE_FILENAME_PARAM, fileName,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return await DoActionAsync(response,
                    // ReSharper disable once AccessToDisposedClosure - DoActionAsync definitively returns after this lambda is invoked
                    async () => await GetResponseStreamAsync(response, cancellationToken).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Publishes a new <see cref="IRevision"/> asynchronously.
        /// Not supported in this implementation.
        /// </summary>
        /// <param name="revision">The revision to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        /// <exception cref="NotSupportedException">Always thrown.</exception>
        public Task PublishAsync(IRevision revision, CancellationToken cancellationToken = default)
        {
            throw UnsupportedOperationException.Create(
                "this replicator implementation does not support remote publishing of revisions");
        }

        /// <summary>
        /// Releases the session at the remote host asynchronously.
        /// </summary>
        /// <param name="sessionId">The session ID to release.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        public async Task ReleaseAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            using var response = await ExecuteGetAsync(
                nameof(ReplicationService.ReplicationAction.RELEASE),
                ReplicationService.REPLICATE_SESSION_ID_PARAM, sessionId,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            await DoActionAsync(response, () =>
            {
                // No actual response content needed — just verification
                return Task.FromResult<object?>(null);
            }).ConfigureAwait(false);
        }

        #endregion

    }
}
