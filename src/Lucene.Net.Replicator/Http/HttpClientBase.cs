using Lucene.Net.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

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
        /// <summary>
        /// Default connection timeout for this client, in milliseconds.
        /// <see cref="ConnectionTimeout"/>
        /// </summary>
        public const int DEFAULT_CONNECTION_TIMEOUT = 1000;

        // TODO compression?

        /// <summary>
        /// The URL to execute requests against. 
        /// </summary>
        protected string Url { get; private set; }

        private volatile bool isDisposed = false;
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
        protected HttpClientBase(string host, int port, string path, HttpMessageHandler messageHandler = null)
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
        protected HttpClientBase(string url, HttpMessageHandler messageHandler = null)
            : this(url, new HttpClient(messageHandler ?? new HttpClientHandler()) { Timeout = TimeSpan.FromMilliseconds(DEFAULT_CONNECTION_TIMEOUT) })
        {
        }

        /// <summary>
        /// Creates a new <see cref="HttpClientBase"/> with the given <paramref name="url"/> and <see cref="HttpClient"/>.
        /// </summary>
        /// <remarks>
        /// This allows full controll over how the <see cref="HttpClient"/> is created, 
        /// prefer the <see cref="HttpClientBase(string, HttpMessageHandler)"/> over this unless you know you need the control of the <see cref="HttpClient"/>.
        /// </remarks>
        /// <param name="url"></param>
        /// <param name="client">The <see cref="HttpClient"/> to use make remote http calls.</param>
        //Note: LUCENENET Specific
        protected HttpClientBase(string url, HttpClient client)
        {
            Url = url;
            httpc = client;
            ConnectionTimeout = DEFAULT_CONNECTION_TIMEOUT;
        }

        /// <summary>
        /// Gets or Sets the connection timeout for this client, in milliseconds. This setting
        /// is used to modify <see cref="HttpClient.Timeout"/>.
        /// </summary>
        public virtual int ConnectionTimeout
        {
            get { return (int)httpc.Timeout.TotalMilliseconds; }
            set { httpc.Timeout = TimeSpan.FromMilliseconds(value); }
        }

        /// <summary>
        /// Throws <see cref="ObjectDisposedException"/> if this client is already disposed. 
        /// </summary>
        /// <exception cref="ObjectDisposedException">client is already disposed.</exception>
        protected void EnsureOpen()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("HttpClient already disposed");
            }
        }

        /// <summary>
        /// Create a URL out of the given parameters, translate an empty/null path to '/'
        /// </summary>
        private static string NormalizedUrl(string host, int port, string path)
        {
            if (string.IsNullOrEmpty(path))
                path = "/";
            return string.Format("http://{0}:{1}{2}", host, port, path);
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
                ThrowKnownError(response);
            }
        }

        /// <summary>
        /// Throws an exception for any errors.
        /// </summary>
        /// <exception cref="IOException">IO Error happened at the server, check inner exception for details.</exception>
        /// <exception cref="HttpRequestException">Unknown error received from the server.</exception>
        protected virtual void ThrowKnownError(HttpResponseMessage response)
        {
            Stream input;
            try
            {
                //.NET Note: Bridging from Async to Sync, this is not ideal and we could consider changing the interface to be Async or provide Async overloads
                //           and have these Sync methods with their caveats.
                input = response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // the response stream is not an exception - could be an error in servlet.init().
                response.EnsureSuccessStatusCode();
                //Note: This is unreachable, but the compiler and resharper cant see that EnsureSuccessStatusCode always
                //      throws an exception in this scenario. So it complains later on in the method.
                throw;
            }

            Exception exception;
            try
            {
                TextReader reader = new StreamReader(input);
                JsonSerializer serializer = JsonSerializer.Create(ReplicationService.JSON_SERIALIZER_SETTINGS);
                exception = (Exception)serializer.Deserialize(new JsonTextReader(reader));
            }
            catch (Exception e)
            {
                //not likely
                throw new HttpRequestException(string.Format("Failed to read exception object: {0} {1}", response.StatusCode, response.ReasonPhrase), e);
            }
            finally
            {
                input.Dispose();
            }

            if (exception is IOException)
            {
                //NOTE: Preserve server stacktrace, but there are probably better options.
                throw new IOException(exception.Message, exception);
            }
            throw new HttpRequestException(string.Format("unknown exception: {0} {1}", response.StatusCode, response.ReasonPhrase), exception);
        }

        /// <summary>
        /// <b>Internal:</b> Execute a request and return its result.
        /// The <paramref name="parameters"/> argument is treated as: name1,value1,name2,value2,...
        /// </summary>
        protected virtual HttpResponseMessage ExecutePost(string request, object entity, params string[] parameters)
        {
            EnsureOpen();
            //.NET Note: No headers? No ContentType?... Bad use of Http?
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, QueryString(request, parameters));
            
            req.Content = new StringContent(JToken.FromObject(entity, JsonSerializer.Create(ReplicationService.JSON_SERIALIZER_SETTINGS))
                .ToString(Formatting.None), Encoding.UTF8, "application/json");

            //.NET Note: Bridging from Async to Sync, this is not ideal and we could consider changing the interface to be Async or provide Async overloads
            //      and have these Sync methods with their caveats.
            HttpResponseMessage response = httpc.SendAsync(req).ConfigureAwait(false).GetAwaiter().GetResult();
            VerifyStatus(response);
            return response;
        }

        /// <summary>
        /// <b>Internal:</b> Execute a request and return its result.
        /// The <paramref name="parameters"/> argument is treated as: name1,value1,name2,value2,...
        /// </summary>
        protected virtual HttpResponseMessage ExecuteGet(string request, params string[] parameters)
        {
            EnsureOpen();
            //Note: No headers? No ContentType?... Bad use of Http?
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, QueryString(request, parameters));
            //.NET Note: Bridging from Async to Sync, this is not ideal and we could consider changing the interface to be Async or provide Async overloads
            //      and have these Sync methods with their caveats.
            HttpResponseMessage response = httpc.SendAsync(req).ConfigureAwait(false).GetAwaiter().GetResult();
            VerifyStatus(response);
            return response;
        }

        private string QueryString(string request, params string[] parameters)
        {
            return parameters == null 
                ? string.Format("{0}/{1}", Url, request) 
                : string.Format("{0}/{1}?{2}", Url, request, string
                .Join("&", parameters.Select(WebUtility.UrlEncode).InPairs((key, val) => string.Format("{0}={1}", key, val))));
        }

        /// <summary>
        /// Internal utility: input stream of the provided response.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public virtual Stream ResponseInputStream(HttpResponseMessage response)
        {
            return ResponseInputStream(response, false);
        }

        // TODO: can we simplify this Consuming !?!?!?
        /// <summary>
        /// Internal utility: input stream of the provided response, which optionally 
        /// consumes the response's resources when the input stream is exhausted.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public virtual Stream ResponseInputStream(HttpResponseMessage response, bool consume)
        {
            return response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns <c>true</c> if this instance was <see cref="Dispose(bool)"/>ed, otherwise
        /// returns <c>false</c>. Note that if you override <see cref="Dispose(bool)"/>, you must call
        /// <see cref="Dispose(bool)"/> on the base class, in order for this instance to be properly disposed.
        /// </summary>
        public bool IsDisposed { get { return isDisposed; } }

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
            Exception error = new NotImplementedException();
            try
            {
                return call();
            }
            catch (IOException e)
            {
                error = e;
            }
            catch (Exception e)
            {
                error = new IOException(e.Message, e);
            }
            finally
            {
                //JAVA: Had a TryCatch here and then used a EntityUtils class to consume the response,
                //JAVA: Unsure of what that was trying to achieve it was left out.
                //JAVA: This also means that right now this overload does nothing more than support the signature given by the Java ver.
                //JAVA: Overall from a .NET perspective, this method is overly suspicious.
                VerifyStatus(response);
            }
            throw error; // should not get here
        }

        /// <summary>
        /// Disposes this <see cref="HttpClientBase"/>. 
        /// When called with <code>true</code>, this disposes the underlying <see cref="HttpClient"/>.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
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
    }
}
