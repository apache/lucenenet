using Lucene.Net.Replicator.AspNetCore;
using Lucene.Net.Replicator.Http.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Threading.Tasks;

#if FEATURE_ASPNETCORE_ENDPOINT_CONFIG
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
#endif

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
    // The startup class must define all middleware (app.Use) before the terminating endpoint (app.Run)

    public class ReplicationServlet
    {
        public void Configure(IApplicationBuilder app, IReplicationService service, ReplicatorTestCase.MockErrorConfig mockErrorConfig)
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
                // LUCENENET: This is to allow synchronous IO to happen for these requests.
                // LUCENENET TODO: Allow async operations from Replicator.
                var syncIoFeature = context.Features.Get<IHttpBodyControlFeature>();
                if (syncIoFeature != null)
                {
                    syncIoFeature.AllowSynchronousIO = true;
                }

                await Task.Yield();
                service.Perform(context.Request, context.Response);
            });
        }
    }

    // ********************** Option 2: Use Middleware with Endpoint Routing *******************************
    // Running ReplicationService as middleware allows registering other URL patterns so other services
    // (such as controllers or razor pages) can be served from the same application.

#if FEATURE_ASPNETCORE_ENDPOINT_CONFIG // Only available in .NET 5+
    public class ReplicationServiceMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IReplicationService service;

        public ReplicationServiceMiddleware(RequestDelegate next, IReplicationService service)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // LUCENENET: This is to allow synchronous IO to happen for these requests.
            // LUCENENET TODO: Allow async operations from Replicator.
            var syncIoFeature = context.Features.Get<IHttpBodyControlFeature>();
            if (syncIoFeature != null)
            {
                syncIoFeature.AllowSynchronousIO = true;
            }

            await Task.Yield();
            service.Perform(context.Request, context.Response);

            // This is a terminating endpoint. Do not call the next delegate/middleware in the pipeline.
        }
    }

    public static class ReplicationServiceRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapReplicator(this IEndpointRouteBuilder endpoints, string pattern)
        {
            var pipeline = endpoints.CreateApplicationBuilder()
                .UseMiddleware<ReplicationServiceMiddleware>()
                .Build();

            return endpoints
                .Map(pattern, pipeline)
                .WithDisplayName("Replication Service");
        }
    }
#endif
}