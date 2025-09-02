using Lucene.Net.Replicator.Http.Abstractions;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
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
    /// A simple <see cref="HttpListener"/>-based test server with an API modeled
    /// after Microsoft.AspNetCore.TestHost.TestServer so we can easily swap this
    /// implementation in its place.
    /// </summary>
    public class TestServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly IReplicationService _service;
        private readonly ReplicatorTestCase.MockErrorConfig _mockErrorConfig;
        private readonly bool _useSynchronousIO;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Task _serverTask;

        public Uri BaseAddress { get; }

        public TestServer(
            IReplicationService service,
            ReplicatorTestCase.MockErrorConfig mockErrorConfig,
            bool useSynchronousIO,
            string prefix = "http://localhost:0/")
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _mockErrorConfig = mockErrorConfig ?? throw new ArgumentNullException(nameof(mockErrorConfig));
            _useSynchronousIO = useSynchronousIO;

            // Auto-select a free port if prefix ends with 0
            if (prefix.EndsWith("0/", StringComparison.Ordinal))
            {
                int port = GetFreePort();
                prefix = prefix.Replace("0/", port + "/");
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            BaseAddress = new Uri(prefix);
            _listener.Start();

            // Start listening loop
            _serverTask = Task.Run(() => ListenLoopAsync(_cancellationTokenSource.Token));
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();

                    // Handle each request in background
                    _ = Task.Run(async () =>
                    {
                        var request = new HttpListenerReplicationRequest(context.Request);
                        var response = new HttpListenerReplicationResponse(context.Response);

                        try
                        {
                            // Simulate test error condition
                            if (_mockErrorConfig.RespondWithError)
                            {
                                throw new ReplicatorTestCase.HttpResponseException();
                            }

                            if (_useSynchronousIO)
                            {
                                _service.Perform(request, response);
                            }
                            else
                            {
                                await _service.PerformAsync(request, response, token);
                            }
                        }
                        catch
                        {
                            response.StatusCode = 500;
                            byte[] errorBytes = Encoding.UTF8.GetBytes("Internal Server Error");
                            await response.Body.WriteAsync(errorBytes, 0, errorBytes.Length, token);
                        }
                        finally
                        {
                            await response.Body.FlushAsync(token);
                            context.Response.Close();
                        }
                    }, token);
                }
            }
            catch (ObjectDisposedException) { }
            catch (HttpListenerException) { }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _listener.Close();
            try { _serverTask.Wait(); } catch { }
            _cancellationTokenSource.Dispose();
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public HttpMessageHandler CreateHandler() => new HttpClientHandler();
    }
}
