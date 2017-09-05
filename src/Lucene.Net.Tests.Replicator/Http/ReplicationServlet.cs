//STATUS: DRAFT - 4.8.0

using System.Threading.Tasks;
using Lucene.Net.Replicator.AspNetCore;
using Lucene.Net.Replicator.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Lucene.Net.Tests.Replicator.Http
{
    public class ReplicationServlet
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ReplicationService service)
        {
            app.Run(async context =>
            {
                await Task.Yield();
                service.Perform(context.Request, context.Response);
            });
        }
    }
}