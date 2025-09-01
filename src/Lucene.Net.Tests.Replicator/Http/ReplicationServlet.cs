#if FEATURE_ASPNETCORE_TESTHOST
using Lucene.Net.Replicator.AspNetCore;
using Lucene.Net.Replicator.Http.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

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

    // ********************** Option 1: Use a Startup Class ********************************************
    // The startup class must define all middleware (app.Use) before the terminating endpoint (app.Run).

    public class ReplicationServlet
    {
        public void Configure(IApplicationBuilder app, IAsyncReplicationService service, ReplicatorTestCase.MockErrorConfig mockErrorConfig)
        {
            // Middleware to throw an exception conditionally from our test server.
            app.Use(async (context, next) =>
            {
                if (mockErrorConfig.RespondWithError)
                {
                    throw new ReplicatorTestCase.HttpResponseException();
                }

                await next();
            });

            app.Run(async (context) =>
            {
                // LUCENENET: Although the async/await pattern doesn't exist in Java Lucene, this is the recommended
                // approach for modern .NET development.
                await service.PerformAsync(context.Request, context.Response, context.RequestAborted);

                // This is a terminating endpoint. Do not call the next delegate/middleware in the pipeline.
            });
        }
    }

    // ********************** Option 2: Use Middleware with Endpoint Routing *******************************
    // Running ReplicationService as middleware allows registering other URL patterns so other services
    // (such as controllers or razor pages) can be served from the same application.

    public class ReplicationServiceMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IAsyncReplicationService  service;

        public ReplicationServiceMiddleware(RequestDelegate next, IAsyncReplicationService  service)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // LUCENENET: Although the async/await pattern doesn't exist in Java Lucene, this is the recommended
            // approach for modern .NET development.
            await service.PerformAsync(context.Request, context.Response, context.RequestAborted);

            // This is a terminating endpoint. Do not call the next delegate/middleware in the pipeline.
        }
    }

    public static partial class ReplicationServiceRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapReplicator<TReplicationServiceMiddleware>(this IEndpointRouteBuilder endpoints, string pattern) where TReplicationServiceMiddleware : class
        {
            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<TReplicationServiceMiddleware>()
                .Build();

            return endpoints
                .Map(pattern, pipeline)
                .WithDisplayName("Replication Service");
        }
    }
}
#endif
