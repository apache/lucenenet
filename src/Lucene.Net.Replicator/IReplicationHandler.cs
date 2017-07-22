//STATUS: DRAFT - 4.8.0
using System;
using System.Collections.Generic;
using System.IO;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Replicator
{
    /// <summary>Handler for revisions obtained by the client.</summary>
    //Note: LUCENENET specific denesting of interface
    public interface IReplicationHandler
    {
        /// <summary>Returns the current revision files held by the handler.</summary>
        string CurrentVersion { get; }

        /// <summary>Returns the current revision version held by the handler.</summary>
        IDictionary<string, IList<RevisionFile>> CurrentRevisionFiles { get; }

        /// <summary>
        /// Called when a new revision was obtained and is available (i.e. all needed files were successfully copied).
        /// </summary>
        /// <param name="version">The version of the <see cref="IRevision"/> that was copied</param>
        /// <param name="revisionFiles"> the files contained by this <see cref="IRevision"/></param>
        /// <param name="copiedFiles">the files that were actually copied</param>
        /// <param name="sourceDirectory">a mapping from a source of files to the <see cref="Directory"/> they were copied into</param>
        /// <see cref="IOException"/>
        void RevisionReady(string version,
            IDictionary<string, IList<RevisionFile>> revisionFiles,
            IDictionary<string, IList<string>> copiedFiles,
            IDictionary<string, Directory> sourceDirectory);
    }
}