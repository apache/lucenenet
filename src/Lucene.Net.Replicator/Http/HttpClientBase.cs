using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.IO;
using System.Linq;
using System.Net;
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
    /// Base class for Http clients.
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    public abstract class HttpClientBase : IDisposable
    {
        // LUCENENET specific - removed DEFAULT_CONNECTION_TIMEOUT because it is irrelevant in .NET

        [Obsolete("Use DEFAULT_TIMEOUT instead.  This extension method will be removed in 4.8.0 release candidate.")]
        public const int DEFAULT_CONNECTION_TIMEOUT = 1000;

        /// <summary>
        /// Default request timeout for this client (100 seconds).
        /// <see cref="Timeout"/>.
        /// </summary>
        public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(100); // LUCENENET: This was DEFAULT_SO_TIMEOUT in Lucene, using .NET's default timeout value of 100 instead of 61 seconds

        // TODO compression?

        /// <summary>
        /// The URL to execute requests against.
        /// </summary>
        protected string Url { get; private set; }

        private volatile bool isDisposed;

        private readonly HttpClient httpc;

        /// <summary>
        /// Creates a new <see cref="HttpClientBase"/> with the given host, port and path.
        /// </summary>
        /// <remarks>
        /// The host, port and path parameters are normalized to <c>http://{host}:{port}{path}</c>,
        /// if path is <c>null</c> or <c>empty</c> it defaults to <c>/</c>.
        /// <para/>
        /// A <see cref="HttpMessageHandler"/> is taken as an optional parameter as well, if this is not provided it defaults to <c>null</c>.
        /// In this case the internal <see cref="HttpClient"/> will default to use a <see cref="HttpClientHandler"/>.
        /// </remarks>
        /// <param name="host">The host that the client should retrieve data from.</param>
        /// <param name="port">The port to be used to connect on.</param>
        /// <param name="path">The path to the replicator on the host.</param>
        /// <param name="messageHandler">Optional, The HTTP handler stack to use for sending requests, defaults to <c>null</c>.</param>
        protected HttpClientBase(string host, int port, string path, HttpMessageHandler? messageHandler = null)
            : this(NormalizedUrl(host, port, path), messageHandler)
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpClientBase"/> with the given <paramref name="url"/>.
        /// </summary>
        /// <remarks>
        /// A <see cref="HttpMessageHandler"/> is taken as an optional parameter as well, if this is not provided it defaults to <c>null</c>.
        /// In this case the internal <see cref="HttpClient"/> will default to use a <see cref="HttpClientHandler"/>.
        /// </remarks>
        /// <param name="url">The full url, including with host, port and path.</param>
        /// <param name="messageHandler">Optional, The HTTP handler stack to use for sending requests.</param>
        //Note: LUCENENET Specific
        protected HttpClientBase(string url, HttpMessageHandler? messageHandler = null)
            : this(url, new HttpClient(messageHandler ?? new HttpClientHandler()) { Timeout = DEFAULT_TIMEOUT })
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpClientBase"/> with the given <paramref name="url"/> and <see cref="HttpClient"/>.
        /// </summary>
        /// <remarks>
        /// This allows full control over how the <see cref="HttpClient"/> is created,
        /// prefer the <see cref="HttpClientBase(string, HttpMessageHandler)"/> over this unless you know you need the control of the <see cref="HttpClient"/>.
        /// </remarks>
        /// <param name="url"></param>
        /// <param name="client">The <see cref="HttpClient"/> to use make remote http calls.</param>
        //Note: LUCENENET Specific
        protected HttpClientBase(string url, HttpClient client)
        {
            Url = url;
            httpc = client;
            Timeout = DEFAULT_TIMEOUT;
        }

        /// <summary>
        /// Gets or Sets the connection timeout for this client, in milliseconds. This setting
        /// is used to modify <see cref="HttpClient.Timeout"/>.
        /// </summary>
        public virtual TimeSpan Timeout
        {
            get => httpc.Timeout;
            set => httpc.Timeout = value;
        }

        /// <summary>
        /// Throws <see cref="ObjectDisposedException"/> if this client is already disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">client is already disposed.</exception>
        protected void EnsureOpen()
        {
            if (IsDisposed)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "HttpClient already disposed.");
            }
        }

        /// <summary>
        /// Create a URL out of the given parameters, translate an empty/null path to '/'
        /// </summary>
        private static string NormalizedUrl(string host, int port, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }

            return $"http://{host}:{port}{path}";
        }

        /// <summary>
        /// <b>Internal:</b> Verifies the response status and if not successful throws an exception.
        /// </summary>
        /// <exception cref="IOException">IO Error happened at the server, check inner exception for details.</exception>
        /// <exception cref="HttpRequestException">Unknown error received from the server.</exception>
        protected virtual void VerifyStatus(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    ThrowKnownError(response);
                }
                finally
                {
                    ConsumeQuietly(response);
                }
            }
        }

        /// <summary>
        /// Throws an exception for any errors.
        /// </summary>
        protected virtual void ThrowKnownError(HttpResponseMessage response)
        {
            throw RuntimeException.Create(response.ReasonPhrase ?? $"Unknown error: {response.StatusCode}");
        }

        /// <summary>
        /// <b>Internal:</b> Execute a request and return its result.
        /// The <paramref name="parameters"/> argument is treated as: name1,value1,name2,value2,...
        /// </summary>
        protected virtual HttpResponseMessage ExecutePost(string request, HttpContent content, params string[]? parameters)
        {
            EnsureOpen();

            var req = new HttpRequestMessage(HttpMethod.Post, QueryString(request, parameters));

            req.Content = content;

            return Execute(req);
        }

        /// <summary>
        /// <b>Internal:</b> Execute a request and return its result.
        /// The <paramref name="parameters"/> argument is treated as: name1,value1,name2,value2,...
        /// </summary>
        protected virtual HttpResponseMessage ExecuteGet(string request, params string[]? parameters)
        {
            EnsureOpen();

            var req = new HttpRequestMessage(HttpMethod.Get, QueryString(request, parameters));

            return Execute(req);
        }

        /// <summary>
        /// Execute a GET request asynchronously with an array of parameters.
        /// </summary>
        protected Task<HttpResponseMessage> ExecuteGetAsync(string action, string[] parameters, CancellationToken cancellationToken)
        {
            var url = BuildUrl(action, parameters);
            return Client.GetAsync(url, cancellationToken);
        }

        /// <summary>
        /// Execute a GET request asynchronously with up to 3 name/value parameters.
        /// </summary>
        protected Task<HttpResponseMessage> ExecuteGetAsync(
            string action,
            string param1, string value1,
            string param2 = null, string value2 = null,
            string param3 = null, string value3 = null,
            CancellationToken cancellationToken = default)
        {
            var url = BuildUrl(action, param1, value1, param2, value2, param3, value3);
            return Client.GetAsync(url, cancellationToken);
        }

        private HttpResponseMessage Execute(HttpRequestMessage request)
        {
            //.NET Note: Bridging from Async to Sync, this is not ideal and we could consider changing the interface to be Async or provide Async overloads
            //      and have these Sync methods with their caveats.
            var response = httpc.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false).GetAwaiter().GetResult();
            VerifyStatus(response);
            return response;
        }

        private string QueryString(string request, params string[]? parameters)
        {
            return parameters is null || parameters.Length == 0
                ? $"{Url}/{request}"
                : $"{Url}/{request}?{string.Join("&", parameters.Select(WebUtility.UrlEncode).InPairs((key, val) => $"{key}={val}"))}";
        }

        // Add this property so subclasses can access the HttpClient instance
        protected HttpClient Client => httpc;

        // BuildUrl helpers (mirror the QueryString overloads)
        protected virtual string BuildUrl(string action, string[] parameters)
        {
            // QueryString has signature: QueryString(string request, params string[] parameters)
            return QueryString(action, parameters);
        }

        protected virtual string BuildUrl(
            string action,
            string param1, string value1,
            string param2 = null, string value2 = null,
            string param3 = null, string value3 = null)
        {
            // Forward to QueryString which accepts params string[]
            if (param2 == null && param3 == null)
            {
                return QueryString(action, param1, value1);
            }
            return QueryString(action, param1, value1, param2, value2, param3, value3);
        }


        /// <summary>
        /// Internal utility: input stream of the provided response.
        /// </summary>
        /// <exception cref="IOException"></exception>
        [Obsolete("Use GetResponseStream(HttpResponseMessage) instead.  This extension method will be removed in 4.8.0 release candidate.")]
        public virtual Stream ResponseInputStream(HttpResponseMessage response)
        {
            return GetResponseStream(response, false);
        }

        // TODO: can we simplify this Consuming !?!?!?
        /// <summary>
        /// Internal utility: input stream of the provided response, which optionally
        /// consumes the response's resources when the input stream is exhausted.
        /// </summary>
        /// <exception cref="IOException"></exception>
        [Obsolete("Use GetResponseStream(HttpResponseMessage, bool) instead.  This extension method will be removed in 4.8.0 release candidate.")]
        public virtual Stream ResponseInputStream(HttpResponseMessage response, bool consume)
        {
            return GetResponseStream(response, consume);
        }

        /// <summary>
        /// Internal utility: input stream of the provided response.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public virtual Stream GetResponseStream(HttpResponseMessage response) // LUCENENET: This was ResponseInputStream in Lucene
        {
            return GetResponseStream(response, false);
        }

        // TODO: can we simplify this Consuming !?!?!?
        /// <summary>
        /// Internal utility: input stream of the provided response, which optionally
        /// consumes the response's resources when the input stream is exhausted.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public virtual Stream GetResponseStream(HttpResponseMessage response, bool consume) // LUCENENET: This was ResponseInputStream in Lucene
        {
            Stream result = response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            if (consume)
            {
                result = new ConsumingStream(result);
            }

            return result;
        }

        /// <summary>
        /// Internal utility: input stream of the provided response asynchronously.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public virtual async Task<Stream> GetResponseStreamAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
        {
        #if NET8_0_OR_GREATER
            Stream result = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        #else
            Stream result = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        #endif
            return result;
        }

        /// <summary>
        /// Internal utility: input stream of the provided response asynchronously, which optionally
        /// consumes the response's resources when the input stream is exhausted.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public virtual async Task<Stream> GetResponseStreamAsync(HttpResponseMessage response, bool consume, CancellationToken cancellationToken = default)
        {
        #if NET8_0_OR_GREATER
            Stream result = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        #else
            Stream result = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        #endif
            if (consume)
                result = new ConsumingStream(result);
            return result;
        }

        /// <summary>
        /// Returns <c>true</c> if this instance was <see cref="Dispose(bool)"/>ed, otherwise
        /// returns <c>false</c>. Note that if you override <see cref="Dispose(bool)"/>, you must call
        /// <see cref="Dispose(bool)"/> on the base class, in order for this instance to be properly disposed.
        /// </summary>
        public bool IsDisposed => isDisposed;

        /// <summary>
        /// Calls the overload <see cref="DoAction{T}(HttpResponseMessage, bool, Func{T})"/> passing <c>true</c> to consume.
        /// </summary>
        protected virtual T DoAction<T>(HttpResponseMessage response, Func<T> call)
        {
            return DoAction(response, true, call);
        }

        /// <summary>
        /// Do a specific action and validate after the action that the status is still OK,
        /// and if not, attempt to extract the actual server side exception. Optionally
        /// release the response at exit, depending on <paramref name="consume"/> parameter.
        /// </summary>
        protected virtual T DoAction<T>(HttpResponseMessage response, bool consume, Func<T> call)
        {
            Exception? th /* = null */;
            try
            {
                return call();
            }
            catch (Exception t) when (t.IsThrowable())
            {
                th = t;
            }
            finally
            {
                try
                {
                    VerifyStatus(response);
                }
                finally
                {
                    if (consume)
                    {
                        ConsumeQuietly(response);
                    }
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(th != null); // extra safety - if we get here, it means the Func<T> failed
            Util.IOUtils.ReThrow(th);
            return default!; // silly, if we're here, IOUtils.reThrow always throws an exception
        }

        /// <summary>
        /// Do a specific async action and validate after the action that the status is still OK,
        /// and if not, attempt to extract the actual server side exception. Optionally
        /// release the response at exit, depending on <paramref name="consume"/> parameter.
        /// </summary>
        protected virtual async Task<T> DoActionAsync<T>(HttpResponseMessage response, bool consume, Func<Task<T>> call)
        {
            Exception th = null;
            try
            {
                VerifyStatus(response);
                return await call().ConfigureAwait(false);
            }
            catch (Exception t) when (t.IsThrowable())
            {
                th = t;
            }
            finally
            {
                try
                {
                    VerifyStatus(response);
                }
                finally
                {
                    if (consume)
                    {
                        try
                        {
                            ConsumeQuietly(response);
                        }
                        catch
                        {
                            // ignore on purpose
                        }
                    }
                }
            }

            if (Debugging.AssertsEnabled) Debugging.Assert(th != null);
            Util.IOUtils.ReThrow(th);
            return default!; // never reached, rethrow above always throws
        }

        /// <summary>
        /// Calls the overload <see cref="DoActionAsync{T}(HttpResponseMessage, bool, Func{Task{T}})"/> passing <c>true</c> to consume.
        /// </summary>
        protected virtual Task<T> DoActionAsync<T>(HttpResponseMessage response, Func<Task<T>> call)
        {
            return DoActionAsync(response, true, call);
        }


        /// <summary>
        /// Disposes this <see cref="HttpClientBase"/>.
        /// When called with <code>true</code>, this disposes the underlying <see cref="HttpClient"/>.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !isDisposed)
            {
                httpc.Dispose();
            }
            isDisposed = true;
        }

        /// <summary>
        /// Disposes this <see cref="HttpClientBase"/>.
        /// This disposes the underlying <see cref="HttpClient"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static void ConsumeQuietly(HttpResponseMessage response)
        {
            try
            {
                response.Content?.Dispose(); // LUCENENET: Force a flush and dispose the underlying stream
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                // Ignore
            }
        }

        /// <summary>
        /// Wraps a stream and consumes (flushes) and disposes automatically
        /// when the last call to a Read overload occurs.
        /// </summary>
        private class ConsumingStream : Stream
        {
            private readonly Stream input;
            private bool consumed = false;

            public ConsumingStream(Stream input)
            {
                this.input = input ?? throw new ArgumentNullException(nameof(input));
            }

            public override bool CanRead => input.CanRead;

            public override bool CanSeek => input.CanSeek;

            public override bool CanWrite => input.CanWrite;

            public override long Length => input.Length;

            public override long Position
            {
                get => input.Position;
                set => input.Position = value;
            }

            public override void Flush() => input.Flush();

            public override int ReadByte()
            {
                int res = input.ReadByte();
                Consume(res);
                return res;
            }
            public override int Read(byte[] buffer, int offset, int count)
            {
                int res = input.Read(buffer, offset, count);
                Consume(res);
                return res;
            }

#if FEATURE_STREAM_READ_SPAN
            public override int Read(Span<byte> buffer)
            {
                int res = input.Read(buffer);
                Consume(res);
                return res;
            }
#endif
            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                int res = await input.ReadAsync(buffer, offset, count, cancellationToken);
                Consume(res);
                return res;
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                int res = input.EndRead(asyncResult);
                Consume(res);
                return res;
            }

            public override long Seek(long offset, SeekOrigin origin) => input.Seek(offset, origin);

            public override void SetLength(long value) => input.SetLength(value);

            public override void Write(byte[] buffer, int offset, int count) => input.Write(buffer, offset, count);

            private void Consume(int zeroOrMinusOne)
            {
                if (!consumed && zeroOrMinusOne <= 0)
                {
                    try
                    {
                        try
                        {
                            input.Flush();
                        }
                        finally
                        {
                            input.Dispose();
                        }
                    }
                    catch (Exception e) when (e.IsException())
                    {
                        // ignored on purpose
                    }
                    consumed = true;
                }
            }
        }
    }
}
