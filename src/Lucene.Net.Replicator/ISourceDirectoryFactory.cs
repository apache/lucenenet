using Lucene.Net.Store;

namespace Lucene.Net.Replicator
{
    /// <summary>
    /// Resolves a session and source into a <see cref="Directory"/> to use for copying
    /// the session files to.
    /// </summary>
    //Note: LUCENENET specific denesting of interface
    public interface ISourceDirectoryFactory
    {
        /// <summary>
        /// Returns the <see cref="Directory"/> to use for the given session and source.
        /// Implementations may e.g. return different directories for different
        /// sessions, or the same directory for all sessions. In that case, it is
        /// advised to clean the directory before it is used for a new session.
        /// </summary>
        /// <seealso cref="CleanupSession"/>
        Directory GetDirectory(string sessionId, string source); //throws IOException;

        /// <summary>
        /// Called to denote that the replication actions for this session were finished and the directory is no longer needed. 
        /// </summary>
        /// <exception cref="System.IO.IOException"></exception>
        void CleanupSession(string sessionId);
    }
}