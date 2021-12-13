using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Replicator
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

    /// <summary>
    /// A <see cref="IReplicationHandler"/> for replication of an index and taxonomy pair.
    /// See <see cref="IReplicationHandler"/> for more detail. This handler ensures
    /// that the search and taxonomy indexes are replicated in a consistent way.
    /// </summary>
    /// <remarks>
    /// <b>NOTE:</b> If you intend to recreate a taxonomy index, you should make sure
    /// to reopen an IndexSearcher and TaxonomyReader pair via the provided callback,
    /// to guarantee that both indexes are in sync. This handler does not prevent
    /// replicating such index and taxonomy pairs, and if they are reopened by a
    /// different thread, unexpected errors can occur, as well as inconsistency
    /// between the taxonomy and index readers.
    /// <para/>
    /// @lucene.experimental
    /// </remarks>
    /// <seealso cref="IndexReplicationHandler"/>
    public class IndexAndTaxonomyReplicationHandler : IReplicationHandler
    {
        /// <summary>
        /// The component used to log messages to the <see cref="Util.InfoStream.Default"/> <see cref="Util.InfoStream"/>.
        /// </summary>
        public const string INFO_STREAM_COMPONENT = "IndexAndTaxonomyReplicationHandler";

        private readonly Directory indexDirectory;
        private readonly Directory taxonomyDirectory;
        private readonly Action callback;

        private volatile IDictionary<string, IList<RevisionFile>> currentRevisionFiles;
        private volatile string currentVersion;
        private volatile InfoStream infoStream = InfoStream.Default;

        /// <summary>
        /// Constructor with the given index directory and callback to notify when the indexes were updated.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public IndexAndTaxonomyReplicationHandler(Directory indexDirectory, Directory taxonomyDirectory, Action callback)
        {
            this.indexDirectory = indexDirectory;
            this.taxonomyDirectory = taxonomyDirectory;
            this.callback = callback;

            currentVersion = null;
            currentRevisionFiles = null;

            bool indexExists = DirectoryReader.IndexExists(indexDirectory);
            bool taxonomyExists = DirectoryReader.IndexExists(taxonomyDirectory);

            if (indexExists != taxonomyExists)
                throw IllegalStateException.Create(string.Format("search and taxonomy indexes must either both exist or not: index={0} taxo={1}", indexExists, taxonomyExists));

            if (indexExists)
            {
                IndexCommit indexCommit = IndexReplicationHandler.GetLastCommit(indexDirectory);
                IndexCommit taxonomyCommit = IndexReplicationHandler.GetLastCommit(taxonomyDirectory);

                currentRevisionFiles = IndexAndTaxonomyRevision.RevisionFiles(indexCommit, taxonomyCommit);
                currentVersion = IndexAndTaxonomyRevision.RevisionVersion(indexCommit, taxonomyCommit);

                WriteToInfoStream(
                    string.Format("constructor(): currentVersion={0} currentRevisionFiles={1}", currentVersion, currentRevisionFiles),
                    string.Format("constructor(): indexCommit={0} taxoCommit={1}", indexCommit, taxonomyCommit));
            }
        }

        public virtual string CurrentVersion => currentVersion;
        public virtual IDictionary<string, IList<RevisionFile>> CurrentRevisionFiles => currentRevisionFiles;

        public virtual void RevisionReady(string version,
            IDictionary<string, IList<RevisionFile>> revisionFiles,
            IDictionary<string, IList<string>> copiedFiles,
            IDictionary<string, Directory> sourceDirectory)
        {
            Directory taxonomyClientDirectory = sourceDirectory[IndexAndTaxonomyRevision.TAXONOMY_SOURCE];
            Directory indexClientDirectory = sourceDirectory[IndexAndTaxonomyRevision.INDEX_SOURCE];
            IList<string> taxonomyFiles = copiedFiles[IndexAndTaxonomyRevision.TAXONOMY_SOURCE];
            IList<string> indexFiles = copiedFiles[IndexAndTaxonomyRevision.INDEX_SOURCE];
            string taxonomySegmentsFile = IndexReplicationHandler.GetSegmentsFile(taxonomyFiles, true);
            string indexSegmentsFile = IndexReplicationHandler.GetSegmentsFile(indexFiles, false);

            bool success = false;
            try
            {
                // copy taxonomy files before index files
                IndexReplicationHandler.CopyFiles(taxonomyClientDirectory, taxonomyDirectory, taxonomyFiles);
                IndexReplicationHandler.CopyFiles(indexClientDirectory, indexDirectory, indexFiles);

                // fsync all copied files (except segmentsFile)
                if (taxonomyFiles.Count > 0)
                    taxonomyDirectory.Sync(taxonomyFiles);
                indexDirectory.Sync(indexFiles);

                // now copy and fsync segmentsFile, taxonomy first because it is ok if a
                // reader sees a more advanced taxonomy than the index.
                if (taxonomySegmentsFile != null)
                    taxonomyClientDirectory.Copy(taxonomyDirectory, taxonomySegmentsFile, taxonomySegmentsFile, IOContext.READ_ONCE);
                indexClientDirectory.Copy(indexDirectory, indexSegmentsFile, indexSegmentsFile, IOContext.READ_ONCE);

                if (taxonomySegmentsFile != null)
                    taxonomyDirectory.Sync(new[] { taxonomySegmentsFile });
                indexDirectory.Sync(new[] { indexSegmentsFile });

                success = true;
            }
            finally
            {
                if (!success)
                {
                    taxonomyFiles.Add(taxonomySegmentsFile); // add it back so it gets deleted too
                    IndexReplicationHandler.CleanupFilesOnFailure(taxonomyDirectory, taxonomyFiles);
                    indexFiles.Add(indexSegmentsFile); // add it back so it gets deleted too
                    IndexReplicationHandler.CleanupFilesOnFailure(indexDirectory, indexFiles);
                }
            }

            // all files have been successfully copied + sync'd. update the handler's state
            currentRevisionFiles = revisionFiles;
            currentVersion = version;
            
            WriteToInfoStream("revisionReady(): currentVersion=" + currentVersion + " currentRevisionFiles=" + currentRevisionFiles);
            
            // update the segments.gen file
            IndexReplicationHandler.WriteSegmentsGen(taxonomySegmentsFile, taxonomyDirectory);
            IndexReplicationHandler.WriteSegmentsGen(indexSegmentsFile, indexDirectory);
            
            // Cleanup the index directory from old and unused index files.
            // NOTE: we don't use IndexWriter.deleteUnusedFiles here since it may have
            // side-effects, e.g. if it hits sudden IO errors while opening the index
            // (and can end up deleting the entire index). It is not our job to protect
            // against those errors, app will probably hit them elsewhere.
            IndexReplicationHandler.CleanupOldIndexFiles(indexDirectory, indexSegmentsFile);
            IndexReplicationHandler.CleanupOldIndexFiles(taxonomyDirectory, taxonomySegmentsFile);

            // successfully updated the index, notify the callback that the index is
            // ready.
            if (callback != null)
            {
                try
                {
                    callback.Invoke();
                }
                catch (Exception e) when (e.IsException())
                {
                    throw new IOException(e.ToString(), e);
                }
            }
        }

        // LUCENENET specific utility method
        private void WriteToInfoStream(params string[] messages)
        {
            if (!InfoStream.IsEnabled(INFO_STREAM_COMPONENT))
                return;

            foreach (string message in messages)
                InfoStream.Message(INFO_STREAM_COMPONENT, message);
        }

        /// <summary>
        /// Gets or sets the <see cref="Util.InfoStream"/> to use for logging messages.
        /// </summary>
        public virtual InfoStream InfoStream
        {
            get => infoStream;
            set => infoStream = value ?? InfoStream.NO_OUTPUT;
        }
    }
}
