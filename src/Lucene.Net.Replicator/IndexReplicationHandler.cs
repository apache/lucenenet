//STATUS: DRAFT - 4.8.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    /// A <see cref="IReplicationHandler"/> for replication of an index. Implements
    /// <see cref="RevisionReady"/> by copying the files pointed by the client resolver to
    /// the index <see cref="Store.Directory"/> and then touches the index with
    /// <see cref="IndexWriter"/> to make sure any unused files are deleted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler assumes that <see cref="IndexWriter"/> is not opened by
    /// another process on the index directory. In fact, opening an
    /// <see cref="IndexWriter"/> on the same directory to which files are copied can lead
    /// to undefined behavior, where some or all the files will be deleted, override
    /// other files or simply create a mess. When you replicate an index, it is best
    /// if the index is never modified by <see cref="IndexWriter"/>, except the one that is
    /// open on the source index, from which you replicate.
    /// </para>
    /// <para>
    /// This handler notifies the application via a provided <see cref="Callable"/> when an
    /// updated index commit was made available for it.
    /// </para>
    /// 
    /// Lucene.Experimental
    /// </remarks>
    public class IndexReplicationHandler : IReplicationHandler
    {
        /// <summary>
        /// The component used to log messages to the {@link InfoStream#getDefault()
        /// default} <seealso cref="InfoStream"/>.
        /// </summary>
        public const string INFO_STREAM_COMPONENT = "IndexReplicationHandler";

        private readonly Directory indexDirectory;
        private readonly Func<bool?> callback;
        private InfoStream infoStream;

        public string CurrentVersion { get; private set; }
        public IDictionary<string, IList<RevisionFile>> CurrentRevisionFiles { get; private set; }

        public InfoStream InfoStream
        {
            get { return infoStream; }
            set { infoStream = value ?? InfoStream.NO_OUTPUT; }
        }

        /// <summary>
        /// Constructor with the given index directory and callback to notify when the
        /// indexes were updated.
        /// </summary>
        /// <param name="indexDirectory"></param>
        /// <param name="callback"></param>
        // .NET NOTE: Java uses a Callable<Boolean>, however it is never uses the returned value?
        public IndexReplicationHandler(Directory indexDirectory, Func<bool?> callback)
        {
            #region JAVA
            //JAVA: this.callback = callback;
            //JAVA: this.indexDir = indexDir;
            //JAVA: currentRevisionFiles = null;
            //JAVA: currentVersion = null;
            //JAVA: if (DirectoryReader.indexExists(indexDir))
            //JAVA: {
            //JAVA:     final List<IndexCommit> commits = DirectoryReader.listCommits(indexDir);
            //JAVA:     final IndexCommit commit = commits.get(commits.size() - 1);
            //JAVA:     currentRevisionFiles = IndexRevision.revisionFiles(commit);
            //JAVA:     currentVersion = IndexRevision.revisionVersion(commit);
            //JAVA:     final InfoStream infoStream = InfoStream.getDefault();
            //JAVA:     if (infoStream.isEnabled(INFO_STREAM_COMPONENT))
            //JAVA:     {
            //JAVA:         infoStream.message(INFO_STREAM_COMPONENT, "constructor(): currentVersion=" + currentVersion
            //JAVA:                                                   + " currentRevisionFiles=" + currentRevisionFiles);
            //JAVA:         infoStream.message(INFO_STREAM_COMPONENT, "constructor(): commit=" + commit);
            //JAVA:     }
            //JAVA: }
            #endregion

            this.InfoStream = InfoStream.Default;
            this.callback = callback;
            this.indexDirectory = indexDirectory;

            CurrentVersion = null;
            CurrentRevisionFiles = null;

            if (DirectoryReader.IndexExists(indexDirectory))
            {
                IList<IndexCommit> commits = DirectoryReader.ListCommits(indexDirectory);
                IndexCommit commit = commits.Last();

                CurrentVersion = IndexRevision.RevisionVersion(commit);
                CurrentRevisionFiles = IndexRevision.RevisionFiles(commit);

                WriteToInfoStream(
                    string.Format("constructor(): currentVersion={0} currentRevisionFiles={1}", CurrentVersion, CurrentRevisionFiles),
                    string.Format("constructor(): commit={0}", commit));
            }
        }

        public void RevisionReady(string version, 
            IDictionary<string, IList<RevisionFile>> revisionFiles, 
            IDictionary<string, IList<string>> copiedFiles, 
            IDictionary<string, Directory> sourceDirectory)
        {
            #region Java
            //JAVA: if (revisionFiles.size() > 1) {
            //JAVA:   throw new IllegalArgumentException("this handler handles only a single source; got " + revisionFiles.keySet());
            //JAVA: }
            //JAVA: 
            //JAVA: Directory clientDir = sourceDirectory.values().iterator().next();
            //JAVA: List<String> files = copiedFiles.values().iterator().next();
            //JAVA: String segmentsFile = getSegmentsFile(files, false);
            //JAVA: 
            //JAVA: boolean success = false;
            //JAVA: try {
            //JAVA:   // copy files from the client to index directory
            //JAVA:   copyFiles(clientDir, indexDir, files);
            //JAVA:   
            //JAVA:   // fsync all copied files (except segmentsFile)
            //JAVA:   indexDir.sync(files);
            //JAVA:   
            //JAVA:   // now copy and fsync segmentsFile
            //JAVA:   clientDir.copy(indexDir, segmentsFile, segmentsFile, IOContext.READONCE);
            //JAVA:   indexDir.sync(Collections.singletonList(segmentsFile));
            //JAVA:   
            //JAVA:   success = true;
            //JAVA: } finally {
            //JAVA:   if (!success) {
            //JAVA:     files.add(segmentsFile); // add it back so it gets deleted too
            //JAVA:     cleanupFilesOnFailure(indexDir, files);
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
            //JAVA: writeSegmentsGen(segmentsFile, indexDir);
            //JAVA: 
            //JAVA: // Cleanup the index directory from old and unused index files.
            //JAVA: // NOTE: we don't use IndexWriter.deleteUnusedFiles here since it may have
            //JAVA: // side-effects, e.g. if it hits sudden IO errors while opening the index
            //JAVA: // (and can end up deleting the entire index). It is not our job to protect
            //JAVA: // against those errors, app will probably hit them elsewhere.
            //JAVA: cleanupOldIndexFiles(indexDir, segmentsFile);
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
            //TODO: ArgumentOutOfRangeException more suited?
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
            CurrentRevisionFiles = revisionFiles;
            CurrentVersion = version;
            
            WriteToInfoStream(string.Format("revisionReady(): currentVersion={0} currentRevisionFiles={1}", CurrentVersion, CurrentRevisionFiles));

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
            if (callback != null) {
              try {
                callback.Invoke();
              } catch (Exception e) {
                throw new IOException(e.Message, e);
              }
            }           
        }

        // .NET NOTE: Utility Method
        private void WriteToInfoStream(params string[] messages)
        {
            if (!InfoStream.IsEnabled(INFO_STREAM_COMPONENT))
                return;

            foreach (string message in messages)
                InfoStream.Message(INFO_STREAM_COMPONENT, message);
        }

        /// <summary>
        /// Returns the last <see cref="IndexCommit"/> found in the <see cref="Directory"/>, or
        /// <code>null</code> if there are no commits.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        /// <exception cref="System.IO.IOException"></exception>
        public static IndexCommit GetLastCommit(Directory directory)
        {
            #region Java
            //JAVA: try {
            //JAVA:   if (DirectoryReader.indexExists(dir)) {
            //JAVA:     List<IndexCommit> commits = DirectoryReader.listCommits(dir);
            //JAVA:     // listCommits guarantees that we get at least one commit back, or
            //JAVA:     // IndexNotFoundException which we handle below
            //JAVA:     return commits.get(commits.size() - 1);
            //JAVA:   }
            //JAVA: } catch (IndexNotFoundException e) {
            //JAVA:   // ignore the exception and return null
            //JAVA: }
            //JAVA: return null;       
            #endregion

            try
            {
                // IndexNotFoundException which we handle below
                return DirectoryReader.IndexExists(directory) 
                    ? DirectoryReader.ListCommits(directory).Last() 
                    : null;
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
        /// <para>
        /// The reason why the code fails instead of putting segments_N file last is
        /// that this indicates an error in the Revision implementation.
        /// </para>
        /// </summary>
        /// <param name="files"></param>
        /// <param name="allowEmpty"></param>
        /// <returns></returns>
        public static string GetSegmentsFile(IList<string> files, bool allowEmpty)
        {
            #region Java
            //JAVA: if (files.isEmpty()) {
            //JAVA:   if (allowEmpty) {
            //JAVA:     return null;
            //JAVA:   } else {
            //JAVA:     throw new IllegalStateException("empty list of files not allowed");
            //JAVA:   }
            //JAVA: }
            //JAVA: 
            //JAVA: String segmentsFile = files.remove(files.size() - 1);
            //JAVA: if (!segmentsFile.startsWith(IndexFileNames.SEGMENTS) || segmentsFile.equals(IndexFileNames.SEGMENTS_GEN)) {
            //JAVA:   throw new IllegalStateException("last file to copy+sync must be segments_N but got " + segmentsFile
            //JAVA:       + "; check your Revision implementation!");
            //JAVA: }
            //JAVA: return segmentsFile;    
            #endregion

            if (!files.Any())
            {
                if (allowEmpty)
                    return null;
                throw new InvalidOperationException("empty list of files not allowed");
            }

            string segmentsFile = files.Last();
            //NOTE: Relying on side-effects outside?
            files.RemoveAt(files.Count - 1);
            if (!segmentsFile.StartsWith(IndexFileNames.SEGMENTS) || segmentsFile.Equals(IndexFileNames.SEGMENTS_GEN))
            {
                throw new InvalidOperationException(
                    string.Format("last file to copy+sync must be segments_N but got {0}; check your Revision implementation!", segmentsFile));
            }
            return segmentsFile;
        }

        /// <summary>
        /// Cleanup the index directory by deleting all given files. Called when file
        /// copy or sync failed.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="files"></param>
        public static void CleanupFilesOnFailure(Directory directory, IList<string> files)
        {
            #region Java
            //JAVA: for (String file : files) {
            //JAVA:     try {
            //JAVA:         dir.deleteFile(file);
            //JAVA:     } catch (Throwable t) {
            //JAVA:         // suppress any exception because if we're here, it means copy
            //JAVA:         // failed, and we must cleanup after ourselves.
            //JAVA:     }
            //JAVA: }
            #endregion

            foreach (string file in files)
            {
                try
                {
                    directory.DeleteFile(file);
                }
                catch
                {
                    // suppress any exception because if we're here, it means copy
                    // failed, and we must cleanup after ourselves.
                }
            }
        }

        public static void CleanupOldIndexFiles(Directory directory, string segmentsFile)
        {
            #region Java
            //JAVA: try {
            //JAVA:   IndexCommit commit = getLastCommit(dir);
            //JAVA:   // commit == null means weird IO errors occurred, ignore them
            //JAVA:   // if there were any IO errors reading the expected commit point (i.e.
            //JAVA:   // segments files mismatch), then ignore that commit either.
            //JAVA:   if (commit != null && commit.getSegmentsFileName().equals(segmentsFile)) {
            //JAVA:     Set<String> commitFiles = new HashSet<>();
            //JAVA:     commitFiles.addAll(commit.getFileNames());
            //JAVA:     commitFiles.add(IndexFileNames.SEGMENTS_GEN);
            //JAVA:     Matcher matcher = IndexFileNames.CODEC_FILE_PATTERN.matcher("");
            //JAVA:     for (String file : dir.listAll()) {
            //JAVA:       if (!commitFiles.contains(file)
            //JAVA:           && (matcher.reset(file).matches() || file.startsWith(IndexFileNames.SEGMENTS))) {
            //JAVA:         try {
            //JAVA:           dir.deleteFile(file);
            //JAVA:         } catch (Throwable t) {
            //JAVA:           // suppress, it's just a best effort
            //JAVA:         }
            //JAVA:       }
            //JAVA:     }
            //JAVA:   }
            //JAVA: } catch (Throwable t) {
            //JAVA:   // ignore any errors that happens during this state and only log it. this
            //JAVA:   // cleanup will have a chance to succeed the next time we get a new
            //JAVA:   // revision.
            //JAVA: }             
            #endregion

            try
            {
                IndexCommit commit = GetLastCommit(directory);
                // commit == null means weird IO errors occurred, ignore them
                // if there were any IO errors reading the expected commit point (i.e.
                // segments files mismatch), then ignore that commit either.

                if (commit != null && commit.SegmentsFileName.Equals(segmentsFile))
                {
                    HashSet<string> commitFiles = new HashSet<string>( commit.FileNames
                        .Union(new[] {IndexFileNames.SEGMENTS_GEN}));

                    Regex matcher = IndexFileNames.CODEC_FILE_PATTERN;
                    foreach (string file in directory.ListAll()
                        .Where(file => !commitFiles.Contains(file) && (matcher.IsMatch(file) || file.StartsWith(IndexFileNames.SEGMENTS))))
                    {
                        try
                        {
                            directory.DeleteFile(file);
                        }
                        catch
                        {
                            // suppress, it's just a best effort
                        }
                    }

                }
            }
            catch 
            {
                // ignore any errors that happens during this state and only log it. this
                // cleanup will have a chance to succeed the next time we get a new
                // revision.
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="files"></param>
        /// <exception cref="System.IO.IOException"></exception>
        public static void CopyFiles(Directory source, Directory target, IList<string> files)
        {
            #region Java
            //JAVA: if (!source.equals(target)) {
            //JAVA:     for (String file : files) {
            //JAVA:         source.copy(target, file, file, IOContext.READONCE);
            //JAVA:     }
            //JAVA: }
            #endregion

            if (source.Equals(target))
                return;

            foreach (string file in files)
                source.Copy(target, file, file, IOContext.READ_ONCE);
        }

        /// <summary>
        /// Writes <see cref="IndexFileNames.SEGMENTS_GEN"/> file to the directory, reading
        /// the generation from the given <code>segmentsFile</code>. If it is <code>null</code>,
        /// this method deletes segments.gen from the directory.
        /// </summary>
        /// <param name="segmentsFile"></param>
        /// <param name="directory"></param>
        public static void WriteSegmentsGen(string segmentsFile, Directory directory)
        {
            #region Java
            //JAVA: public static void writeSegmentsGen(String segmentsFile, Directory dir) {
            //JAVA:   if (segmentsFile != null) {
            //JAVA:     SegmentInfos.writeSegmentsGen(dir, SegmentInfos.generationFromSegmentsFileName(segmentsFile));
            //JAVA:   } else {
            //JAVA:     try {
            //JAVA:       dir.deleteFile(IndexFileNames.SEGMENTS_GEN);
            //JAVA:     } catch (Throwable t) {
            //JAVA:       // suppress any errors while deleting this file.
            //JAVA:     }
            //JAVA:   }
            //JAVA: }
            #endregion

            if (segmentsFile != null)
            {
                SegmentInfos.WriteSegmentsGen(directory, SegmentInfos.GenerationFromSegmentsFileName(segmentsFile));
                return;
            }

            try
            {
                directory.DeleteFile(IndexFileNames.SEGMENTS_GEN);
            }
            catch
            {
                // suppress any errors while deleting this file.
            }
        }
    }
}