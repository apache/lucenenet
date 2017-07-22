//STATUS: DRAFT - 4.8.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
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
    /// A {@link ReplicationHandler} for replication of an index and taxonomy pair.
    /// See {@link IndexReplicationHandler} for more detail. This handler ensures
    /// that the search and taxonomy indexes are replicated in a consistent way.
    /// 
    /// <see cref="IndexReplicationHandler"/>
    /// </summary>
    /// <remarks>
    /// If you intend to recreate a taxonomy index, you should make sure
    /// to reopen an IndexSearcher and TaxonomyReader pair via the provided callback,
    /// to guarantee that both indexes are in sync. This handler does not prevent
    /// replicating such index and taxonomy pairs, and if they are reopened by a
    /// different thread, unexpected errors can occur, as well as inconsistency
    /// between the taxonomy and index readers.
    /// 
    /// Lucene.Experimental
    /// </remarks>
    public class IndexAndTaxonomyReplicationHandler : IReplicationHandler
    {
        /// <summary>
        /// The component used to log messages to the {@link InfoStream#getDefault()default} {@link InfoStream}.
        /// </summary>
        public const string INFO_STREAM_COMPONENT = "IndexAndTaxonomyReplicationHandler";

        private readonly Directory indexDirectory;
        private readonly Directory taxonomyDirectory;
        private readonly Func<bool?> callback;

        private InfoStream infoStream = InfoStream.Default;

        public string CurrentVersion { get; private set; }
        public IDictionary<string, IList<RevisionFile>> CurrentRevisionFiles { get; private set; }
        public InfoStream InfoStream
        {
            get { return infoStream; }
            set { infoStream = value ?? InfoStream.NO_OUTPUT; }
        }

        /// <summary>
        /// Constructor with the given index directory and callback to notify when the indexes were updated.
        /// </summary>
        /// <param name="indexDirectory"></param>
        /// <param name="taxonomyDirectory"></param>
        /// <param name="callback"></param>
        /// <exception cref="System.IO.IOException"></exception>
        public IndexAndTaxonomyReplicationHandler(Directory indexDirectory, Directory taxonomyDirectory, Func<bool?> callback)
        {
            this.indexDirectory = indexDirectory;
            this.taxonomyDirectory = taxonomyDirectory;
            this.callback = callback;

            CurrentVersion = null;
            CurrentRevisionFiles = null;

            bool indexExists = DirectoryReader.IndexExists(indexDirectory);
            bool taxonomyExists = DirectoryReader.IndexExists(taxonomyDirectory);

            //JAVA: IllegalStateException
            if (indexExists != taxonomyExists)
                throw new InvalidOperationException(string.Format("search and taxonomy indexes must either both exist or not: index={0} taxo={1}", indexExists, taxonomyExists));

            if (indexExists)
            {
                IndexCommit indexCommit = IndexReplicationHandler.GetLastCommit(indexDirectory);
                IndexCommit taxonomyCommit = IndexReplicationHandler.GetLastCommit(taxonomyDirectory);

                CurrentRevisionFiles = IndexAndTaxonomyRevision.RevisionFiles(indexCommit, taxonomyCommit);
                CurrentVersion = IndexAndTaxonomyRevision.RevisionVersion(indexCommit, taxonomyCommit);

                WriteToInfoStream(
                    string.Format("constructor(): currentVersion={0} currentRevisionFiles={1}", CurrentVersion, CurrentRevisionFiles),
                    string.Format("constructor(): indexCommit={0} taxoCommit={1}", indexCommit, taxonomyCommit));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        /// <param name="revisionFiles"></param>
        /// <param name="copiedFiles"></param>
        /// <param name="sourceDirectory"></param>
        /// <exception cref=""></exception>
        public void RevisionReady(string version,
            IDictionary<string, IList<RevisionFile>> revisionFiles,
            IDictionary<string, IList<string>> copiedFiles,
            IDictionary<string, Directory> sourceDirectory)
        {
            #region Java
            //JAVA: Directory taxoClientDir = sourceDirectory.get(IndexAndTaxonomyRevision.TAXONOMY_SOURCE);
            //JAVA: Directory indexClientDir = sourceDirectory.get(IndexAndTaxonomyRevision.INDEX_SOURCE);
            //JAVA: List<String> taxoFiles = copiedFiles.get(IndexAndTaxonomyRevision.TAXONOMY_SOURCE);
            //JAVA: List<String> indexFiles = copiedFiles.get(IndexAndTaxonomyRevision.INDEX_SOURCE);
            //JAVA: String taxoSegmentsFile = IndexReplicationHandler.getSegmentsFile(taxoFiles, true);
            //JAVA: String indexSegmentsFile = IndexReplicationHandler.getSegmentsFile(indexFiles, false);
            //JAVA:
            //JAVA: boolean success = false;
            //JAVA: try {
            //JAVA:   // copy taxonomy files before index files
            //JAVA:   IndexReplicationHandler.copyFiles(taxoClientDir, taxoDir, taxoFiles);
            //JAVA:   IndexReplicationHandler.copyFiles(indexClientDir, indexDir, indexFiles);
            //JAVA:
            //JAVA:   // fsync all copied files (except segmentsFile)
            //JAVA:   if (!taxoFiles.isEmpty()) {
            //JAVA:     taxoDir.sync(taxoFiles);
            //JAVA:   }
            //JAVA:   indexDir.sync(indexFiles);
            //JAVA:
            //JAVA:   // now copy and fsync segmentsFile, taxonomy first because it is ok if a
            //JAVA:   // reader sees a more advanced taxonomy than the index.
            //JAVA:   if (taxoSegmentsFile != null) {
            //JAVA:     taxoClientDir.copy(taxoDir, taxoSegmentsFile, taxoSegmentsFile, IOContext.READONCE);
            //JAVA:   }
            //JAVA:   indexClientDir.copy(indexDir, indexSegmentsFile, indexSegmentsFile, IOContext.READONCE);
            //JAVA:
            //JAVA:   if (taxoSegmentsFile != null) {
            //JAVA:     taxoDir.sync(Collections.singletonList(taxoSegmentsFile));
            //JAVA:   }
            //JAVA:   indexDir.sync(Collections.singletonList(indexSegmentsFile));
            //JAVA:
            //JAVA:   success = true;
            //JAVA: } finally {
            //JAVA:   if (!success) {
            //JAVA:     taxoFiles.add(taxoSegmentsFile); // add it back so it gets deleted too
            //JAVA:     IndexReplicationHandler.cleanupFilesOnFailure(taxoDir, taxoFiles);
            //JAVA:     indexFiles.add(indexSegmentsFile); // add it back so it gets deleted too
            //JAVA:     IndexReplicationHandler.cleanupFilesOnFailure(indexDir, indexFiles);
            //JAVA:   }
            //JAVA: }
            //JAVA:
            //JAVA: // all files have been successfully copied + sync'd. update the handler's state
            //JAVA: currentRevisionFiles = revisionFiles;
            //JAVA: currentVersion = version;
            //JAVA:
            //JAVA: if (infoStream.isEnabled(INFO_STREAM_COMPONENT)) {
            //JAVA:   infoStream.message(INFO_STREAM_COMPONENT, "revisionReady(): currentVersion=" + currentVersion
            //JAVA:       + " currentRevisionFiles=" + currentRevisionFiles);
            //JAVA: }
            //JAVA:
            //JAVA: // update the segments.gen file
            //JAVA: IndexReplicationHandler.writeSegmentsGen(taxoSegmentsFile, taxoDir);
            //JAVA: IndexReplicationHandler.writeSegmentsGen(indexSegmentsFile, indexDir);
            //JAVA:
            //JAVA: // Cleanup the index directory from old and unused index files.
            //JAVA: // NOTE: we don't use IndexWriter.deleteUnusedFiles here since it may have
            //JAVA: // side-effects, e.g. if it hits sudden IO errors while opening the index
            //JAVA: // (and can end up deleting the entire index). It is not our job to protect
            //JAVA: // against those errors, app will probably hit them elsewhere.
            //JAVA: IndexReplicationHandler.cleanupOldIndexFiles(indexDir, indexSegmentsFile);
            //JAVA: IndexReplicationHandler.cleanupOldIndexFiles(taxoDir, taxoSegmentsFile);
            //JAVA:
            //JAVA: // successfully updated the index, notify the callback that the index is
            //JAVA: // ready.
            //JAVA: if (callback != null) {
            //JAVA:   try {
            //JAVA:     callback.call();
            //JAVA:   } catch (Exception e) {
            //JAVA:     throw new IOException(e);
            //JAVA:   }
            //JAVA: } 
            #endregion

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
                if (taxonomyFiles.Any())
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
            CurrentRevisionFiles = revisionFiles;
            CurrentVersion = version;
            
            WriteToInfoStream("revisionReady(): currentVersion=" + CurrentVersion + " currentRevisionFiles=" + CurrentRevisionFiles);
            
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
            if (callback != null) {
              try {
                callback.Invoke();
              } catch (Exception e) {
                throw new IOException(e.Message, e);
              }
            } 
        }

        private void WriteToInfoStream(params string[] messages)
        {
            if (!InfoStream.IsEnabled(INFO_STREAM_COMPONENT))
                return;

            foreach (string message in messages)
                InfoStream.Message(INFO_STREAM_COMPONENT, message);
        }
    }
}
