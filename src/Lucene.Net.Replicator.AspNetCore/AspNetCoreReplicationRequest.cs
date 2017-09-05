using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Replicator.Http;
using Lucene.Net.Replicator.Http.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Lucene.Net.Replicator.AspNetCore
{
    /// <summary>
    /// Abstraction for remote replication requests, allows easy integration into any hosting frameworks.
    /// </summary>
    /// <remarks>
    /// .NET Specific Implementation of the Lucene Replicator using AspNetCore  
    /// </remarks>
    //Note: LUCENENET specific
    public class AspNetCoreReplicationRequest : IReplicationRequest
    {
        private readonly HttpRequest request;

        /// <summary>
        /// Creates a <see cref="IReplicationRequest"/> wrapper around the provided <see cref="HttpRequest"/>
        /// </summary>
        /// <param name="request">the request to wrap</param>
        public AspNetCoreReplicationRequest(HttpRequest request)
        {
            this.request = request;
        }

        /// <summary>
        /// Provides the requested path which mapps to a replication operation.
        /// </summary>
        public string Path { get { return request.PathBase + request.Path; } }

        /// <summary>
        /// Returns the requested query parameter or null if not present.
        /// Throws an exception if the same parameter is provided multiple times.
        /// </summary>
        /// <param name="name">the name of the requested parameter</param>
        /// <returns>the value of the requested parameter or null if not present</returns>
        /// <exception cref="InvalidOperationException">More than one parameter with the name was given.</exception>
        public string QueryParam(string name)
        {
            return request.Query[name].SingleOrDefault();
        }
    }
}
