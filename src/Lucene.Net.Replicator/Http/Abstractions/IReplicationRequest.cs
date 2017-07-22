namespace Lucene.Net.Replicator.Http.Abstractions
{
    /// <summary>
    /// Abstraction for remote replication requests, allows easy integration into any hosting frameworks.
    /// </summary>
    /// <remarks>
    /// .NET Specific Abstraction  
    /// </remarks>
    //Note: LUCENENET specific
    public interface IReplicationRequest
    {
        /// <summary>
        /// Provides the requested path which mapps to a replication operation.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Returns the requested query parameter or null if not present.
        /// </summary>
        /// <remarks>
        ///  May though execeptions if the same parameter is provided multiple times, consult the documentation for the specific implementation.
        /// </remarks>
        /// <param name="name">the name of the requested parameter</param>
        /// <returns>the value of the requested parameter or null if not present</returns>
        string QueryParam(string name);
    }
}