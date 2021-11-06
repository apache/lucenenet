using Lucene.Net.Replicator.Http;
using Lucene.Net.Replicator.Http.Abstractions;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

#if FEATURE_ASPNETCORE_ENDPOINT_CONFIG
using Microsoft.AspNetCore.Builder;
#endif

namespace Lucene.Net.Replicator
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

    [SuppressCodecs("Lucene3x")]
    public class ReplicatorTestCase : LuceneTestCase
    {

#if FEATURE_ASPNETCORE_ENDPOINT_CONFIG
        /// <summary>
        /// Call this overload to use <see cref="ReplicationServiceMiddleware"/> to host <see cref="ReplicationService"/>.
        /// </summary>
        /// <param name="service">The <see cref="ReplicationService"/> that will be registered as singleton.</param>
        /// <param name="mockErrorConfig">The <see cref="MockErrorConfig"/> that will be registered as singleton.</param>
        /// <returns>A configured <see cref="TestServer"/> instance.</returns>
        public static TestServer NewHttpServer(IReplicationService service, MockErrorConfig mockErrorConfig)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(container =>
                {
                    container.AddRouting();
                    container.AddSingleton(service);
                    container.AddSingleton(mockErrorConfig);
                    container.AddSingleton<ReplicationServiceMiddleware>();
                    container.AddSingleton<MockErrorMiddleware>();
                })
                .Configure(app =>
                {
                    app.UseRouting();

                    // Middleware so we can mock a server exception and toggle the exception on and off.
                    app.UseMiddleware<MockErrorMiddleware>();

                    app.UseEndpoints(endpoints =>
                    {
                        // This is to define the endpoint for Replicator.
                        // All URLs with the pattern /replicate/{shard?}/{action?} terminate here and any middleware that
                        // is expected to run for Replicator must be registered before this call.
                        endpoints.MapReplicator(ReplicationService.REPLICATION_CONTEXT + "/{shard?}/{action?}");

                        endpoints.MapGet("/{controller?}/{action?}/{id?}", async context =>
                        {
                            // This is just to demonstrate allowing requests to other services/controllers in the same
                            // application. This isn't required, but is allowed.
                            await context.Response.WriteAsync("Hello World!");
                        });
                    });
                });
            var server = new TestServer(builder);
            return server;
        }
#else
        /// <summary>
        /// Call this overload to use <typeparamref name="TStartUp"/> as the Startup Class.
        /// </summary>
        /// <typeparam name="TStartUp">The type of startup class.</typeparam>
        /// <param name="service">The <see cref="ReplicationService"/> that will be registered as singleton.</param>
        /// <param name="mockErrorConfig">The <see cref="MockErrorConfig"/> that will be registered as singleton.</param>
        /// <returns>A configured <see cref="TestServer"/> instance.</returns>
        public static TestServer NewHttpServer<TStartUp>(IReplicationService service, MockErrorConfig mockErrorConfig) where TStartUp : class
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(container =>
                {
                    container.AddSingleton(service);
                    container.AddSingleton(mockErrorConfig);
                })
                .UseStartup<TStartUp>();

            var server = new TestServer(builder);
            server.BaseAddress = new Uri("http://localhost" + ReplicationService.REPLICATION_CONTEXT);
            return server;
        }
#endif

        /// <summary>
        /// Returns a <see cref="server"/>'s port. 
        /// </summary>
        public static int ServerPort(TestServer server)
        {
            return server.BaseAddress.Port;
        }

        /// <summary>
        /// Returns a <see cref="server"/>'s host. 
        /// </summary>
        public static string ServerHost(TestServer server)
        {
            return server.BaseAddress.Host;
        }

        /// <summary>
        /// Stops the given HTTP Server instance.
        /// </summary>
        public static void StopHttpServer(TestServer server)
        {
            server.Dispose();
        }

        public class HttpResponseException : Exception
        {
            public int Status { get; set; } = 500;

            public object Value { get; set; }
        }

        public class MockErrorMiddleware
        {
            private readonly RequestDelegate next;
            private readonly MockErrorConfig mockErrorConfig;

            public MockErrorMiddleware(RequestDelegate next, MockErrorConfig mockErrorConfig)
            {
                this.next = next ?? throw new ArgumentNullException(nameof(next));
                this.mockErrorConfig = mockErrorConfig ?? throw new ArgumentNullException(nameof(mockErrorConfig));
            }

            public async Task InvokeAsync(HttpContext context)
            {
                var path = context.Request.Path;
                if (path.StartsWithSegments(ReplicationService.REPLICATION_CONTEXT))
                {
                    if (mockErrorConfig.RespondWithError)
                    {
                        throw new HttpResponseException();
                    }
                }

                // Call the next delegate/middleware in the pipeline
                await next(context);
            }
        }

        public class MockErrorConfig
        {
            public bool RespondWithError { get; set; } = false;
        }
    }
}