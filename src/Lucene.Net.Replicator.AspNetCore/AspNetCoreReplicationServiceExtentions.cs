using Lucene.Net.Replicator.Http;
using Microsoft.AspNetCore.Http;

namespace Lucene.Net.Replicator.AspNetCore
{
    //Note: LUCENENET specific
    public static class AspNetCoreReplicationServiceExtentions
    {
        /// <summary>
        /// Extensiont method that mirrors the signature of <see cref="ReplicationService.Perform"/> using AspNetCore as implementation.
        /// </summary>
        public static void Perform(this ReplicationService self, HttpRequest request, HttpResponse response)
        {
            self.Perform(new AspNetCoreReplicationRequest(request), new AspNetCoreReplicationResponse(response));
        }
    }
}