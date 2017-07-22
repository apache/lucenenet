//STATUS: DRAFT - 4.8.0

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    /// Lucene.Experimental
    /// </remarks>
    public abstract class HttpClientBase : IDisposable
    {
        /// <summary>
        /// Default connection timeout for this client, in milliseconds.
        /// <see cref="ConnectionTimeout"/>
        /// </summary>
        public const int DEFAULT_CONNECTION_TIMEOUT = 1000;

        /**
         * Default socket timeout for this client, in milliseconds.
         * 
         * @see #setSoTimeout(int)
         */
        //TODO: This goes to the Read and Write timeouts in the request (Closest we can get to a socket timeout in .NET?), those should be controlled in the messageHandler
        //      if the choosen messagehandler provides such mechanishm (e.g. the WebRequestHandler) so this doesn't seem to make sense in .NET. 
        //public const int DEFAULT_SO_TIMEOUT = 60000;

        // TODO compression?

        /// <summary>
        /// The URL to execute requests against. 
        /// </summary>
        protected string Url { get; private set; }

        private readonly HttpClient httpc;

        //JAVA: /**
        //JAVA:  * Set the connection timeout for this client, in milliseconds. This setting
        //JAVA:  * is used to modify {@link HttpConnectionParams#setConnectionTimeout}.
        //JAVA:  * 
        //JAVA:  * @param timeout timeout to set, in millisecopnds
        //JAVA:  */
        //JAVA: public void setConnectionTimeout(int timeout) {
        //JAVA:   HttpConnectionParams.setConnectionTimeout(httpc.getParams(), timeout);
        //JAVA: }
        /// <summary>
        /// Gets or Sets the connection timeout for this client, in milliseconds. This setting
        /// is used to modify <see cref="HttpClient.Timeout"/>.
        /// </summary>
        public int ConnectionTimeout
        {
            get { return (int) httpc.Timeout.TotalMilliseconds; }
            set { httpc.Timeout = TimeSpan.FromMilliseconds(value); }
        }

        //JAVA: /**
        //JAVA:  * Set the socket timeout for this client, in milliseconds. This setting
        //JAVA:  * is used to modify {@link HttpConnectionParams#setSoTimeout}.
        //JAVA:  * 
        //JAVA:  * @param timeout timeout to set, in millisecopnds
        //JAVA:  */
        //JAVA: public void setSoTimeout(int timeout) {
        //JAVA:   HttpConnectionParams.setSoTimeout(httpc.getParams(), timeout);
        //JAVA: }
        //TODO: This goes to the Read and Write timeouts in the request (Closest we can get to a socket timeout in .NET?), those should be controlled in the messageHandler
        //      if the choosen messagehandler provides such mechanishm (e.g. the WebRequestHandler) so this doesn't seem to make sense in .NET. 
        //public int SoTimeout { get; set; }

        /// <summary>
        /// Returns true if this instance was <see cref="Dispose(bool)"/>ed, otherwise
        /// returns false. Note that if you override <see cref="Dispose(bool)"/>, you must call
        /// <see cref="Dispose(bool)"/> on the base class, in order for this instance to be properly disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        //TODO: HttpMessageHandler is not really a replacement for the ClientConnectionManager, allowing for custom message handlers will
        //      provide flexibility, this is AFAIK also where users would be able to controll the equivalent of the SO timeout.
        protected HttpClientBase(string host, int port, string path, HttpMessageHandler messageHandler)
        {
            IsDisposed = false;

            #region Java
            //JAVA: /**
            //JAVA:  * @param conMgr connection manager to use for this http client.
            //JAVA:  *        <b>NOTE:</b>The provided {@link ClientConnectionManager} will not be
            //JAVA:  *        {@link ClientConnectionManager#shutdown()} by this class.
            //JAVA:  */
            //JAVA: protected HttpClientBase(String host, int port, String path, ClientConnectionManager conMgr) {
            //JAVA:   url = normalizedURL(host, port, path);
            //JAVA:   httpc = new DefaultHttpClient(conMgr);
            //JAVA:   setConnectionTimeout(DEFAULT_CONNECTION_TIMEOUT);
            //JAVA:   setSoTimeout(DEFAULT_SO_TIMEOUT);
            //JAVA: }
            #endregion

            Url = NormalizedUrl(host, port, path);
            httpc = new HttpClient(messageHandler ?? new HttpClientHandler());
            httpc.Timeout = TimeSpan.FromMilliseconds(DEFAULT_CONNECTION_TIMEOUT);
        }

        /// <summary>
        /// Throws <see cref="ObjectDisposedException"/> if this client is already closed. 
        /// </summary>
        /// <exception cref="ObjectDisposedException">client is already closed.</exception>
        protected void EnsureOpen()
        {
            #region Java
            //JAVA: protected final void ensureOpen() throws AlreadyClosedException {
            //JAVA:   if (closed) {
            //JAVA:     throw new AlreadyClosedException("HttpClient already closed");
            //JAVA:   }
            //JAVA: }
            #endregion

            if (IsDisposed)
            {
                throw new ObjectDisposedException("HttpClient already closed");
            }
        }

        private static string NormalizedUrl(string host, int port, string path)
        {
            #region Java
            //JAVA: /**
            //JAVA:  * Create a URL out of the given parameters, translate an empty/null path to '/'
            //JAVA:  */
            //JAVA: private static String normalizedURL(String host, int port, String path) {
            //JAVA:   if (path == null || path.length() == 0) {
            //JAVA:     path = "/";
            //JAVA:   }
            //JAVA:   return "http://" + host + ":" + port + path;
            //JAVA: }
            #endregion

            if (string.IsNullOrEmpty(path))
                path = "/";
            return string.Format("http://{0}:{1}{2}", host, port, path);
        }

        /// <summary>
        /// Verifies the response status and if not successfull throws an exception.
        /// </summary>
        /// <exception cref="IOException">IO Error happened at the server, check inner exception for details.</exception>
        /// <exception cref="HttpRequestException">Unknown error received from the server.</exception>
        protected void VerifyStatus(HttpResponseMessage response)
        {
            #region Java
            //JAVA: 
            //JAVA: /**
            //JAVA:  * <b>Internal:</b> response status after invocation, and in case or error attempt to read the 
            //JAVA:  * exception sent by the server. 
            //JAVA:  */
            //JAVA: protected void verifyStatus(HttpResponse response) throws IOException {
            //JAVA:   StatusLine statusLine = response.getStatusLine();
            //JAVA:   if (statusLine.getStatusCode() != HttpStatus.SC_OK) {
            //JAVA:     throwKnownError(response, statusLine); 
            //JAVA:   }
            //JAVA: }
            #endregion

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
        protected void ThrowKnownError(HttpResponseMessage response)
        {
            #region Java
            //JAVA: protected void throwKnownError(HttpResponse response, StatusLine statusLine) throws IOException {
            //JAVA:   ObjectInputStream in = null;
            //JAVA:   try {
            //JAVA:     in = new ObjectInputStream(response.getEntity().getContent());
            //JAVA:   } catch (Exception e) {
            //JAVA:     // the response stream is not an exception - could be an error in servlet.init().
            //JAVA:     throw new RuntimeException("Uknown error: " + statusLine);
            //JAVA:   }
            //JAVA:   
            //JAVA:   Throwable t;
            //JAVA:   try {
            //JAVA:     t = (Throwable) in.readObject();
            //JAVA:   } catch (Exception e) { 
            //JAVA:     //not likely
            //JAVA:     throw new RuntimeException("Failed to read exception object: " + statusLine, e);
            //JAVA:   } finally {
            //JAVA:     in.close();
            //JAVA:   }
            //JAVA:   if (t instanceof IOException) {
            //JAVA:     throw (IOException) t;
            //JAVA:   }
            //JAVA:   if (t instanceof RuntimeException) {
            //JAVA:     throw (RuntimeException) t;
            //JAVA:   }
            //JAVA:   throw new RuntimeException("unknown exception "+statusLine,t);
            //JAVA: }
            #endregion

            Stream input;
            try
            {
                //.NET Note: Bridging from Async to Sync, this is not ideal and we could consider changing the interface to be Async or provide Async overloads
                //      and have these Sync methods with their caveats.
                input = response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // the response stream is not an exception - could be an error in servlet.init().
                //JAVA: throw new RuntimeException("Uknown error: " + statusLine);
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

        protected HttpResponseMessage ExecutePost(string request, object entity, params string[] parameters)
        {
            #region Java
            //JAVA: /**
            //JAVA:  * <b>internal:</b> execute a request and return its result
            //JAVA:  * The <code>params</code> argument is treated as: name1,value1,name2,value2,...
            //JAVA:  */
            //JAVA: protected HttpResponse executePOST(String request, HttpEntity entity, String... params) throws IOException {
            //JAVA:   ensureOpen();
            //JAVA:   HttpPost m = new HttpPost(queryString(request, params));
            //JAVA:   m.setEntity(entity);
            //JAVA:   HttpResponse response = httpc.execute(m);
            //JAVA:   verifyStatus(response);
            //JAVA:   return response;
            //JAVA: }
            #endregion

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

        protected HttpResponseMessage ExecuteGet(string request, params string[] parameters)
        {
            #region Java
            //JAVA: /**
            //JAVA:  * <b>internal:</b> execute a request and return its result
            //JAVA:  * The <code>params</code> argument is treated as: name1,value1,name2,value2,...
            //JAVA:  */
            //JAVA: protected HttpResponse executeGET(String request, String... params) throws IOException {
            //JAVA:   ensureOpen();
            //JAVA:   HttpGet m = new HttpGet(queryString(request, params));
            //JAVA:   HttpResponse response = httpc.execute(m);
            //JAVA:   verifyStatus(response);
            //JAVA:   return response;
            //JAVA: }
            #endregion

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
            #region Java
            //JAVA: private String queryString(String request, String... params) throws UnsupportedEncodingException {
            //JAVA:   StringBuilder query = new StringBuilder(url).append('/').append(request).append('?');
            //JAVA:   if (params != null) {
            //JAVA:     for (int i = 0; i < params.length; i += 2) {
            //JAVA:       query.append(params[i]).append('=').append(URLEncoder.encode(params[i+1], "UTF8")).append('&');
            //JAVA:     }
            //JAVA:   }
            //JAVA:   return query.substring(0, query.length() - 1);
            //JAVA: }
            #endregion

            return parameters == null 
                ? string.Format("{0}/{1}", Url, request) 
                : string.Format("{0}/{1}?{2}", Url, request, string
                .Join("&", parameters.Select(WebUtility.UrlEncode).InPairs((key, val) => string.Format("{0}={1}", key, val))));
        }

        /// <summary>
        /// Internal utility: input stream of the provided response
        /// </summary>
        /// <exception cref="IOException"></exception>
        public Stream ResponseInputStream(HttpResponseMessage response)// throws IOException
        {
            #region Java
            //JAVA: /** Internal utility: input stream of the provided response */
            //JAVA: public InputStream responseInputStream(HttpResponse response) throws IOException {
            //JAVA:   return responseInputStream(response, false);
            //JAVA: }
            #endregion

            return ResponseInputStream(response, false);
        }

        /// <summary>
        /// Internal utility: input stream of the provided response
        /// </summary>
        /// <exception cref="IOException"></exception>
        public Stream ResponseInputStream(HttpResponseMessage response, bool consume)// throws IOException
        {
            #region Java
            //JAVA: TODO: can we simplify this Consuming !?!?!?
            //JAVA: /**
            //JAVA:  * Internal utility: input stream of the provided response, which optionally 
            //JAVA:  * consumes the response's resources when the input stream is exhausted.
            //JAVA:  */
            //JAVA: public InputStream responseInputStream(HttpResponse response, boolean consume) throws IOException {
            //JAVA:   final HttpEntity entity = response.getEntity();
            //JAVA:   final InputStream in = entity.getContent();
            //JAVA:   if (!consume) {
            //JAVA:     return in;
            //JAVA:   }
            //JAVA:   return new InputStream() {
            //JAVA:     private boolean consumed = false;
            //JAVA:     @Override
            //JAVA:     public int read() throws IOException {
            //JAVA:       final int res = in.read();
            //JAVA:       consume(res);
            //JAVA:       return res;
            //JAVA:     }
            //JAVA:     @Override
            //JAVA:     public void close() throws IOException {
            //JAVA:       super.close();
            //JAVA:       consume(-1);
            //JAVA:     }
            //JAVA:     @Override
            //JAVA:     public int read(byte[] b) throws IOException {
            //JAVA:       final int res = super.read(b);
            //JAVA:       consume(res);
            //JAVA:       return res;
            //JAVA:     }
            //JAVA:     @Override
            //JAVA:     public int read(byte[] b, int off, int len) throws IOException {
            //JAVA:       final int res = super.read(b, off, len);
            //JAVA:       consume(res);
            //JAVA:       return res;
            //JAVA:     }
            //JAVA:     private void consume(int minusOne) {
            //JAVA:       if (!consumed && minusOne==-1) {
            //JAVA:         try {
            //JAVA:           EntityUtils.consume(entity);
            //JAVA:         } catch (Exception e) {
            //JAVA:           // ignored on purpose
            //JAVA:         }
            //JAVA:         consumed = true;
            //JAVA:       }
            //JAVA:     }
            //JAVA:   };
            //JAVA: }
            #endregion

            return response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected T DoAction<T>(HttpResponseMessage response, Func<T> call)
        {
            #region Java
            //JAVA: /**
            //JAVA:  * Same as {@link #doAction(HttpResponse, boolean, Callable)} but always do consume at the end.
            //JAVA:  */
            //JAVA: protected <T> T doAction(HttpResponse response, Callable<T> call) throws IOException {
            //JAVA:   return doAction(response, true, call);
            //JAVA: }
            #endregion

            return DoAction(response, true, call);
        }

        protected T DoAction<T>(HttpResponseMessage response, bool consume, Func<T> call)
        {
            #region Java
            //JAVA: /**
            //JAVA:  * Do a specific action and validate after the action that the status is still OK, 
            //JAVA:  * and if not, attempt to extract the actual server side exception. Optionally
            //JAVA:  * release the response at exit, depending on <code>consume</code> parameter.
            //JAVA:  */
            //JAVA: protected <T> T doAction(HttpResponse response, boolean consume, Callable<T> call) throws IOException {
            //JAVA:   IOException error = null;
            //JAVA:   try {
            //JAVA:     return call.call();
            //JAVA:   } catch (IOException e) {
            //JAVA:     error = e;
            //JAVA:   } catch (Exception e) {
            //JAVA:     error = new IOException(e);
            //JAVA:   } finally {
            //JAVA:     try {
            //JAVA:       verifyStatus(response);
            //JAVA:     } finally {
            //JAVA:       if (consume) {
            //JAVA:         try {
            //JAVA:           EntityUtils.consume(response.getEntity());
            //JAVA:         } catch (Exception e) {
            //JAVA:           // ignoring on purpose
            //JAVA:         }
            //JAVA:       }
            //JAVA:     }
            //JAVA:   }
            //JAVA:   throw error; // should not get here
            //JAVA: }
            #endregion

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
                try
                {
                    VerifyStatus(response);
                }
                finally
                {
                    //TODO: Is there any reason for this on .NET?... What are they trying to achieve?
                    //JAVA:       if (consume) {
                    //JAVA:         try {
                    //JAVA:           EntityUtils.consume(response.getEntity());
                    //JAVA:         } catch (Exception e) {
                    //JAVA:           // ignoring on purpose
                    //JAVA:         }
                    //JAVA:       }
                }
            }
            throw error; // should not get here
        }

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
