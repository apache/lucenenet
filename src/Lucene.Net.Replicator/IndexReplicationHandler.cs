using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;
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
    /// A <see cref="IReplicationHandler"/> for replication of an index. Implements
    /// <see cref="RevisionReady"/> by copying the files pointed by the client resolver to
    /// the index <see cref="Store.Directory"/> and then touches the index with
    /// <see cref="IndexWriter"/> to make sure any unused files are deleted.
    /// </summary>
    /// <remarks>
    /// <b>NOTE:</b> This handler assumes that <see cref="IndexWriter"/> is not opened by
    /// another process on the index directory. In fact, opening an
    /// <see cref="IndexWriter"/> on the same directory to which files are copied can lead
    /// to undefined behavior, where some or all the files will be deleted, override
    /// other files or simply create a mess. When you replicate an index, it is best
    /// if the index is never modified by <see cref="IndexWriter"/>, except the one that is
    /// open on the source index, from which you replicate.
    /// <para/>
    /// This handler notifies the application via a provided <see cref="Action"/> when an
    /// updated index commit was made available for it.
    /// <para/>
    /// @lucene.experimental
    /// </remarks>
    public class IndexReplicationHandler : IReplicationHandler
    {
        /// <summary>
        /// The component used to log messages to the <see cref="Util.InfoStream.Default"/> 
        /// <see cref="Util.InfoStream"/>.
        /// </summary>
        public const string INFO_STREAM_COMPONENT = "IndexReplicationHandler";

        private readonly Directory indexDirectory;
        private readonly Action callback;

        private volatile IDictionary<string, IList<RevisionFile>> currentRevisionFiles;
        private volatile string currentVersion;
        private volatile InfoStream infoStream;

        //Note: LUCENENET Specific Utility Method
        private void WriteToInfoStream(params string[] messages)
        {
            if (!InfoStream.IsEnabled(INFO_STREAM_COMPONENT))
                return;

            foreach (string message in messages)
                InfoStream.Message(INFO_STREAM_COMPONENT, message);
        }

        /// <summary>
        /// Returns the last <see cref="IndexCommit"/> found in the <see cref="Directory"/>, or
        /// <c>null</c> if there are no commits.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public static IndexCommit GetLastCommit(Directory directory)
        {
            try
            {
                // IndexNotFoundException which we handle below
                if (DirectoryReader.IndexExists(directory))
                {
                    var commits = DirectoryReader.ListCommits(directory);
                    return commits[commits.Count - 1];
                }
            }
            catch (IndexNotFoundException)
            {
                // ignore the exception and return null
            }
            return null;
        }

        /// <summary>
        /// Verifies that the last file is segments_N and fails otherwise. It also
        /// removes and returns the file from the list, because it needs to be handled
        /// last, after all files. This is important in order to guarantee that if a
        /// reader sees the new segments_N, all other segment files are already on
        /// stable storage.
        /// <para/>
        /// The reason why the code fails instead of putting segments_N file last is
        /// that this indicates an error in the <see cref="IRevision"/> implementation.
        /// </summary>
        public static string GetSegmentsFile(IList<string> files, bool allowEmpty)
        {
            if (files.Count == 0)
            {
                if (allowEmpty)
                    return null;
                throw IllegalStateException.Create("empty list of files not allowed");
            }

            string segmentsFile = files[files.Count - 1];
            //NOTE: Relying on side-effects outside?
            files.RemoveAt(files.Count - 1);
            if (!segmentsFile.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal) || segmentsFile.Equals(IndexFileNames.SEGMENTS_GEN, StringComparison.Ordinal))
            {
                throw IllegalStateException.Create(
                    string.Format("last file to copy+sync must be segments_N but got {0}; check your Revision implementation!", segmentsFile));
            }
            return segmentsFile;
        }

        /// <summary>
        /// Cleanup the index directory by deleting all given files. Called when file
        /// copy or sync failed.
        /// </summary>
        public static void CleanupFilesOnFailure(Directory directory, IList<string> files)
        {
            foreach (string file in files)
            {
                try
                {
                    directory.DeleteFile(file);
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    // suppress any exception because if we're here, it means copy
                    // failed, and we must cleanup after ourselves.
                }
            }
        }

        /// <summary>
        /// Cleans up the index directory from old index files. This method uses the
        /// last commit found by <see cref="GetLastCommit(Directory)"/>. If it matches the
        /// expected <paramref name="segmentsFile"/>, then all files not referenced by this commit point
        /// are deleted.
        /// </summary>
        /// <remarks>
        /// <b>NOTE:</b> This method does a best effort attempt to clean the index
        /// directory. It suppresses any exceptions that occur, as this can be retried
        /// the next time.
        /// </remarks>
        public static void CleanupOldIndexFiles(Directory directory, string segmentsFile)
        {
            try
            {
                IndexCommit commit = GetLastCommit(directory);
                // commit is null means weird IO errors occurred, ignore them
                // if there were any IO errors reading the expected commit point (i.e.
                // segments files mismatch), then ignore that commit either.

                if (commit != null && commit.SegmentsFileName.Equals(segmentsFile, StringComparison.Ordinal))
                {
                    ISet<string> commitFiles = new JCG.HashSet<string>(commit.FileNames)
                    {
                        IndexFileNames.SEGMENTS_GEN
                    };

                    Regex matcher = IndexFileNames.CODEC_FILE_PATTERN;
                    foreach (string file in directory.ListAll())
                    {
                        if (!commitFiles.Contains(file) && (matcher.IsMatch(file) || file.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal)))
                        {
                            try
                            {
                                directory.DeleteFile(file);
                            }
                            catch (Exception t) when (t.IsThrowable())
                            {
                                // suppress, it's just a best effort
                            }
                        }
                    }

                }
            }
            catch (Exception t) when (t.IsThrowable())
            {
                // ignore any errors that happens during this state and only log it. this
                // cleanup will have a chance to succeed the next time we get a new
                // revision.
            }
        }

        /// <summary>
        /// Copies the provided list of files from the <paramref name="source"/> <see cref="Directory"/> to the 
        /// <paramref name="target"/> <see cref="Directory"/>, if they are not the same.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public static void CopyFiles(Directory source, Directory target, IList<string> files)
        {
            if (source.Equals(target))
                return;

            foreach (string file in files)
                source.Copy(target, file, file, IOContext.READ_ONCE);
        }

        /// <summary>
        /// Writes <see cref="IndexFileNames.SEGMENTS_GEN"/> file to the directory, reading
        /// the generation from the given <paramref name="segmentsFile"/>. If it is <c>null</c>,
        /// this method deletes segments.gen from the directory.
        /// </summary>
        public static void WriteSegmentsGen(string segmentsFile, Directory directory)
        {
            if (segmentsFile != null)
            {
                SegmentInfos.WriteSegmentsGen(directory, SegmentInfos.GenerationFromSegmentsFileName(segmentsFile));
                return;
            }

            try
            {
                directory.DeleteFile(IndexFileNames.SEGMENTS_GEN);
            }
            catch (Exception t) when (t.IsThrowable())
            {
                // suppress any errors while deleting this file.
            }
        }

        /// <summary>
        /// Constructor with the given index directory and callback to notify when the
        /// indexes were updated.
        /// </summary>
        public IndexReplicationHandler(Directory indexDirectory, Action callback)
        {
            this.InfoStream = InfoStream.Default;
            this.callback = callback;
            this.indexDirectory = indexDirectory;

            currentVersion = null;
            currentRevisionFiles = null;

            if (DirectoryReader.IndexExists(indexDirectory))
            {
                IList<IndexCommit> commits = DirectoryReader.ListCommits(indexDirectory);
                IndexCommit commit = commits[commits.Count - 1];

                currentVersion = IndexRevision.RevisionVersion(commit);
                currentRevisionFiles = IndexRevision.RevisionFiles(commit);

                WriteToInfoStream(
                    string.Format("constructor(): currentVersion={0} currentRevisionFiles={1}", currentVersion, currentRevisionFiles),
                    string.Format("constructor(): commit={0}", commit));
            }
        }

        public virtual string CurrentVersion => currentVersion;

        public virtual IDictionary<string, IList<RevisionFile>> CurrentRevisionFiles => currentRevisionFiles;

        public virtual void RevisionReady(string version,
            IDictionary<string, IList<RevisionFile>> revisionFiles,
            IDictionary<string, IList<string>> copiedFiles,
            IDictionary<string, Directory> sourceDirectory)
        {
            if (revisionFiles.Count > 1) throw new ArgumentException(string.Format("this handler handles only a single source; got {0}", revisionFiles.Keys));

            Directory clientDirectory = sourceDirectory.Values.First();
            IList<string> files = copiedFiles.Values.First();
            string segmentsFile = GetSegmentsFile(files, false);

            bool success = false;
            try
            {
                // copy files from the client to index directory
                CopyFiles(clientDirectory, indexDirectory, files);

                // fsync all copied files (except segmentsFile)
                indexDirectory.Sync(files);

                // now copy and fsync segmentsFile
                clientDirectory.Copy(indexDirectory, segmentsFile, segmentsFile, IOContext.READ_ONCE);
                indexDirectory.Sync(new[] { segmentsFile });

                success = true;
            }
            finally
            {
                if (!success)
                {
                    files.Add(segmentsFile); // add it back so it gets deleted too
                    CleanupFilesOnFailure(indexDirectory, files);
                }
            }

            // all files have been successfully copied + sync'd. update the handler's state
            currentRevisionFiles = revisionFiles;
            currentVersion = version;

            WriteToInfoStream(string.Format("revisionReady(): currentVersion={0} currentRevisionFiles={1}", currentVersion, currentRevisionFiles));

            // update the segments.gen file
            WriteSegmentsGen(segmentsFile, indexDirectory);

            // Cleanup the index directory from old and unused index files.
            // NOTE: we don't use IndexWriter.deleteUnusedFiles here since it may have
            // side-effects, e.g. if it hits sudden IO errors while opening the index
            // (and can end up deleting the entire index). It is not our job to protect
            // against those errors, app will probably hit them elsewhere.
            CleanupOldIndexFiles(indexDirectory, segmentsFile);

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