using Lucene.Net.Replicator.Http;
using Lucene.Net.Replicator.Http.Abstractions;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

#if FEATURE_ASPNETCORE_ENDPOINT_CONFIG
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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
        public static TestServer NewHttpServer(IReplicationService service, MockErrorConfig mockErrorConfig, bool useSynchronousIO, bool useStartupClass)
        {
            if (useStartupClass)
            {
                var builder = new WebHostBuilder()
                    .ConfigureServices(container =>
                    {
                        container.AddSingleton(service);
                        container.AddSingleton(mockErrorConfig);
                    });

                if (useSynchronousIO)
                {
                    builder.UseStartup<SynchronousReplicationServlet>();
                }
                else
                {
                    builder.UseStartup<ReplicationServlet>();
                }

                var server = new TestServer(builder);
                server.BaseAddress = new Uri("http://localhost" + ReplicationService.REPLICATION_CONTEXT);
                return server;
            }
            else
            {
#if FEATURE_ASPNETCORE_ENDPOINT_CONFIG
                var builder = new WebHostBuilder()
                    .ConfigureServices(container =>
                    {
                        container.AddRouting();
                        container.AddSingleton(service);
                        container.AddSingleton(mockErrorConfig);
                        if (useSynchronousIO)
                        {
                            container.AddSingleton<SynchronousReplicationServiceMiddleware>();
                            container.AddSingleton<EnableSynchronousIOMiddleware>();
                        }
                        else
                        {
                            container.AddSingleton<ReplicationServiceMiddleware>();
                        }
                        container.AddSingleton<MockErrorMiddleware>();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();

                        // Middleware so we can mock a server exception and toggle the exception on and off.
                        app.UseMiddleware<MockErrorMiddleware>();

                        if (useSynchronousIO)
                        {
                            app.UseMiddleware<EnableSynchronousIOMiddleware>();
                        }

                        app.UseEndpoints(endpoints =>
                        {
                            // This is to define the endpoint for Replicator.
                            // All URLs with the pattern /replicate/{shard?}/{action?} terminate here and any middleware that
                            // is expected to run for Replicator must be registered before this call.
                            string pattern = ReplicationService.REPLICATION_CONTEXT + "/{shard?}/{action?}";
                            if (useSynchronousIO)
                                endpoints.MapReplicator<SynchronousReplicationServiceMiddleware>(pattern);
                            else
                                endpoints.MapReplicator<ReplicationServiceMiddleware>(pattern);

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
#else
                throw new PlatformNotSupportedException("Endpoint configuration is not supported prior to .NET 5.0");
#endif
            }
        }

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

        public class EnableSynchronousIOMiddleware
        {
            private readonly RequestDelegate next;

            public EnableSynchronousIOMiddleware(RequestDelegate next)
            {
                this.next = next ?? throw new ArgumentNullException(nameof(next));
            }

            public async Task InvokeAsync(HttpContext context)
            {
                // LUCENENET: This is to allow synchronous IO to happen for these requests.
                // Note that in a real-world app this would be set in the configuration, not
                // per HTTP request. However, this setting is not recommended in modern production
                // applications.
                var syncIoFeature = context.Features.Get<IHttpBodyControlFeature>();
                if (syncIoFeature != null)
                {
                    syncIoFeature.AllowSynchronousIO = true;
                }

                await next(context);
            }
        }
    }
}
