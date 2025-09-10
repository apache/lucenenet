using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.Replicator
{
    /// <summary>
    /// Async version of <see cref="IReplicator"/> for non-blocking replication operations.
    /// </summary>
    public interface IAsyncReplicator
    {
        /// <summary>
        /// Check whether the given version is up-to-date and returns a
        /// <see cref="SessionToken"/> which can be used for fetching the revision files.
        /// </summary>
        /// <param name="currentVersion">Current version of the index.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<SessionToken?> CheckForUpdateAsync(string currentVersion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a stream for the requested file and source.
        /// </summary>
        Task<Stream> ObtainFileAsync(string sessionId, string source, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notify that the specified session is no longer needed.
        /// </summary>
        Task ReleaseAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishing revisions is not supported in HttpReplicator; throw if called.
        /// </summary>
        Task PublishAsync(IRevision revision, CancellationToken cancellationToken = default);
    }
}
