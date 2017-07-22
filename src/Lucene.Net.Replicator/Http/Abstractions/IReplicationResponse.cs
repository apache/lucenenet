using System.IO;

namespace Lucene.Net.Replicator.Http.Abstractions
{
    /// <summary>
    /// Abstraction for remote replication response, allows easy integration into any hosting frameworks.
    /// </summary>
    /// <remarks>
    /// .NET Specific Abstraction  
    /// </remarks>
    //Note: LUCENENET specific
    public interface IReplicationResponse
    {
        /// <summary>
        /// Gets or sets the http status code of the response.
        /// </summary>
        int StatusCode { get; set; }

        /// <summary>
        /// The response content.
        /// </summary>
        Stream Body { get; }

        /// <summary>
        /// Flushes the reponse to the underlying response stream.
        /// </summary>
        void Flush();
    }
}