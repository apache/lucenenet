using System.IO;
using Lucene.Net.Replicator.Http;
using Lucene.Net.Replicator.Http.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Lucene.Net.Replicator.AspNetCore
{
    /// <summary>
    /// Implementation of the <see cref="IReplicationResponse"/> abstraction for the AspNetCore framework.
    /// </summary>
    /// <remarks>
    /// .NET Specific Implementation of the Lucene Replicator using AspNetCore  
    /// </remarks>
    //Note: LUCENENET specific
    public class AspNetCoreReplicationResponse : IReplicationResponse
    {
        private readonly HttpResponse response;

        /// <summary>
        /// Creates a <see cref="IReplicationResponse"/> wrapper around the provided <see cref="HttpResponse"/>
        /// </summary>
        /// <param name="response">the response to wrap</param>
        public AspNetCoreReplicationResponse(HttpResponse response)
        {
            this.response = response;
        }

        /// <summary>
        /// Gets or sets the http status code of the response.
        /// </summary>
        public int StatusCode
        {
            get { return response.StatusCode; }
            set { response.StatusCode = value; }
        }

        /// <summary>
        /// The response content.
        /// </summary>
        /// <remarks>
        /// This simply returns the <see cref="HttpResponse.Body"/>.
        /// </remarks>
        public Stream Body { get { return response.Body; } }

        /// <summary>
        /// Flushes the reponse to the underlying response stream.
        /// </summary>
        /// <remarks>
        /// This simply calls <see cref="Stream.Flush"/> on the <see cref="HttpResponse.Body"/>.
        /// </remarks>
        public void Flush()
        {
            response.Body.Flush();
        }
    }
}