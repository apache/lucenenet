/*
* Licensed to the Apache Software Foundation (ASF) under one or more
* contributor license agreements.  See the NOTICE file distributed with
* this work for additional information regarding copyright ownership.
* The ASF licenses this file to You under the Apache License, Version 2.0
* (the "License"); you may not use this file except in compliance with
* the License.  You may obtain a copy of the License at
* 
* http://www.apache.org/licenses/LICENSE-2.0
* 
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using Lucene.Net.Support;
using Directory = Lucene.Net.Store.Directory;
using Lucene.Net.Util;
using Lucene.Net.Store;
using System.Text.RegularExpressions;
using System.IO;

namespace Lucene.Net.Index
{

    /// <summary>
    /// <para>This class keeps track of each SegmentInfos instance that
    /// is still "live", either because it corresponds to a
    /// segments_N file in the Directory (a "commit", i.e. a
    /// committed SegmentInfos) or because it's an in-memory
    /// SegmentInfos that a writer is actively updating but has
    /// not yet committed.  This class uses simple reference
    /// counting to map the live SegmentInfos instances to
    /// individual files in the Directory.</para>
    /// 
    /// <para>The same directory file may be referenced by more than
    /// one IndexCommit, i.e. more than one SegmentInfos.
    /// Therefore we count how many commits reference each file.
    /// When all the commits referencing a certain file have been
    /// deleted, the refcount for that file becomes zero, and the
    /// file is deleted.</para>
    ///
    /// <para>A separate deletion policy interface
    /// (IndexDeletionPolicy) is consulted on creation (onInit)
    /// and once per commit (onCommit), to decide when a commit
    /// should be removed.</para>
    /// 
    /// <para>It is the business of the IndexDeletionPolicy to choose
    /// when to delete commit points.  The actual mechanics of
    /// file deletion, retrying, etc, derived from the deletion
    /// of commit points is the business of the IndexFileDeleter.</para>
    /// 
    /// <para>The current default deletion policy is
    /// <see cref="KeepOnlyLastCommitDeletionPolicy"/>, which removes all
    /// prior commits when a new commit has completed.  This
    /// matches the behavior before 2.2.</para>
    ///
    /// <para>Note that you must hold the write.lock before
    /// instantiating this class.  It opens segments_N file(s)
    /// directly with no retry logic.</para>
    /// </summary>

    public sealed class IndexFileDeleter : IDisposable
    {

        //// Files that we tried to delete but failed (likely
        /// because they are open and we are running on Windows),
        /// so we will retry them again later: ////
        private IList<string> deletable;

        //// Reference count for all files in the index.  
        /// Counts how many existing commits reference a file.
        /// Maps String to RefCount (class below) instances: ////
        private IDictionary<string, RefCount> refCounts = new HashMap<string, RefCount>();

        //// Holds all commits (segments_N) currently in the index.
        /// This will have just 1 commit if you are using the
        /// default delete policy (KeepOnlyLastCommitDeletionPolicy).
        /// Other policies may leave commit points live for longer
        /// in which case this list would be longer than 1: ////
        private IList<CommitPoint> commits = new List<CommitPoint>();

        //// Holds files we had incref'd from the previous
        /// non-commit checkpoint: ////
        private IList<ICollection<string>> lastFiles = new List<ICollection<string>>();

        //// Commits that the IndexDeletionPolicy have decided to delete: ////
        private IList<CommitPoint> commitsToDelete = new List<CommitPoint>();

        private readonly InfoStream infoStream;
        private Directory directory;
        private IndexDeletionPolicy policy;

        internal readonly bool startingCommitDeleted;
        private SegmentInfos lastSegmentInfos;

        /// <summary>Change to true to see details of reference counts when
        /// infoStream != null 
        /// </summary>
        public static bool VERBOSE_REF_COUNTS = false;

        // Used only for assert
        private readonly IndexWriter writer;

        // called only from assert
        private bool Locked
        {
            get
            {
                // .NET TODO: When we can upgrade to .NET 4.5, we can check Monitor.IsEntered(writer)
                return writer == null; // || Thread.holdsLock(writer);
            }
        }

        /// <summary> Initialize the deleter: find all previous commits in
        /// the Directory, incref the files they reference, call
        /// the policy to let it delete commits.  This will remove
        /// any files not referenced by any of the commits.
        /// </summary>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  IOException if there is a low-level IO error </throws>
        public IndexFileDeleter(Directory directory, IndexDeletionPolicy policy, SegmentInfos segmentInfos,
                          InfoStream infoStream, IndexWriter writer, bool initialIndexExists)
        {
            this.infoStream = infoStream;
            this.writer = writer;

            String currentSegmentsFile = segmentInfos.SegmentsFileName;

            if (infoStream.IsEnabled("IFD"))
            {
                infoStream.Message("IFD", "init: current segments file is \"" + currentSegmentsFile + "\"; deletionPolicy=" + policy);
            }

            this.policy = policy;
            this.directory = directory;

            // First pass: walk the files and initialize our ref
            // counts:
            long currentGen = segmentInfos.Generation;

            CommitPoint currentCommitPoint = null;
            String[] files = null;
            try
            {
                files = directory.ListAll();
            }
            catch (NoSuchDirectoryException e)
            {
                // it means the directory is empty, so ignore it.
                files = new String[0];
            }

            if (currentSegmentsFile != null)
            {
                Regex m = IndexFileNames.CODEC_FILE_PATTERN;
                foreach (String fileName in files)
                {
                    if (!fileName.EndsWith("write.lock") && !fileName.Equals(IndexFileNames.SEGMENTS_GEN)
                        && (m.IsMatch(fileName) || fileName.StartsWith(IndexFileNames.SEGMENTS)))
                    {

                        // Add this file to refCounts with initial count 0:
                        GetRefCount(fileName);

                        if (fileName.StartsWith(IndexFileNames.SEGMENTS))
                        {

                            // This is a commit (segments or segments_N), and
                            // it's valid (<= the max gen).  Load it, then
                            // incref all files it refers to:
                            if (infoStream.IsEnabled("IFD"))
                            {
                                infoStream.Message("IFD", "init: load commit \"" + fileName + "\"");
                            }
                            SegmentInfos sis = new SegmentInfos();
                            try
                            {
                                sis.Read(directory, fileName);
                            }
                            catch (FileNotFoundException e)
                            {
                                // LUCENE-948: on NFS (and maybe others), if
                                // you have writers switching back and forth
                                // between machines, it's very likely that the
                                // dir listing will be stale and will claim a
                                // file segments_X exists when in fact it
                                // doesn't.  So, we catch this and handle it
                                // as if the file does not exist
                                if (infoStream.IsEnabled("IFD"))
                                {
                                    infoStream.Message("IFD", "init: hit FileNotFoundException when loading commit \"" + fileName + "\"; skipping this commit point");
                                }
                                sis = null;
                            }
                            catch (IOException e)
                            {
                                if (SegmentInfos.GenerationFromSegmentsFileName(fileName) <= currentGen && directory.FileLength(fileName) > 0)
                                {
                                    throw e;
                                }
                                else
                                {
                                    // Most likely we are opening an index that
                                    // has an aborted "future" commit, so suppress
                                    // exc in this case
                                    sis = null;
                                }
                            }
                            if (sis != null)
                            {
                                CommitPoint commitPoint = new CommitPoint(this, commitsToDelete, directory, sis);
                                if (sis.Generation == segmentInfos.Generation)
                                {
                                    currentCommitPoint = commitPoint;
                                }
                                commits.Add(commitPoint);
                                IncRef(sis, true);

                                if (lastSegmentInfos == null || sis.Generation > lastSegmentInfos.Generation)
                                {
                                    lastSegmentInfos = sis;
                                }
                            }
                        }
                    }
                }
            }

            if (currentCommitPoint == null && currentSegmentsFile != null && initialIndexExists)
            {
                // We did not in fact see the segments_N file
                // corresponding to the segmentInfos that was passed
                // in.  Yet, it must exist, because our caller holds
                // the write lock.  This can happen when the directory
                // listing was stale (eg when index accessed via NFS
                // client with stale directory listing cache).  So we
                // try now to explicitly open this commit point:
                SegmentInfos sis = new SegmentInfos();
                try
                {
                    sis.Read(directory, currentSegmentsFile);
                }
                catch (IOException e)
                {
                    throw new CorruptIndexException("failed to locate current segments_N file \"" + currentSegmentsFile + "\"");
                }
                if (infoStream.IsEnabled("IFD"))
                {
                    infoStream.Message("IFD", "forced open of current segments file " + segmentInfos.SegmentsFileName);
                }
                currentCommitPoint = new CommitPoint(this, commitsToDelete, directory, sis);
                commits.Add(currentCommitPoint);
                IncRef(sis, true);
            }

            // We keep commits list in sorted order (oldest to newest):
            CollectionUtil.MergeSort(commits);

            // Now delete anything with ref count at 0.  These are
            // presumably abandoned files eg due to crash of
            // IndexWriter.
            foreach (KeyValuePair<String, RefCount> entry in refCounts)
            {
                RefCount rc = entry.Value;
                String fileName = entry.Key;
                if (0 == rc.count)
                {
                    if (infoStream.IsEnabled("IFD"))
                    {
                        infoStream.Message("IFD", "init: removing unreferenced file \"" + fileName + "\"");
                    }
                    DeleteFile(fileName);
                }
            }

            // Finally, give policy a chance to remove things on
            // startup:
            if (currentSegmentsFile != null)
            {
                policy.OnInit(commits);
            }

            // Always protect the incoming segmentInfos since
            // sometime it may not be the most recent commit
            Checkpoint(segmentInfos, false);

            startingCommitDeleted = currentCommitPoint == null ? false : currentCommitPoint.IsDeleted;

            DeleteCommits();
        }

        public SegmentInfos LastSegmentInfos
        {
            get { return lastSegmentInfos; }
        }

        /// <summary> Remove the CommitPoints in the commitsToDelete List by
        /// DecRef'ing all files from each SegmentInfos.
        /// </summary>
        private void DeleteCommits()
        {
            int size = commitsToDelete.Count;

            if (size > 0)
            {
                // First decref all files that had been referred to by
                // the now-deleted commits:
                for (int i = 0; i < size; i++)
                {
                    CommitPoint commit = commitsToDelete[i];
                    if (infoStream.IsEnabled("IFD"))
                    {
                        infoStream.Message("IFD", "deleteCommits: now decRef commit \"" + commit.SegmentsFileName + "\"");
                    }
                    foreach (string file in commit.files)
                    {
                        DecRef(file);
                    }
                }
                commitsToDelete.Clear();

                // Now compact commits to remove deleted ones (preserving the sort):
                size = commits.Count;
                int readFrom = 0;
                int writeTo = 0;
                while (readFrom < size)
                {
                    CommitPoint commit = commits[readFrom];
                    if (!commit.deleted)
                    {
                        if (writeTo != readFrom)
                        {
                            commits[writeTo] = commits[readFrom];
                        }
                        writeTo++;
                    }
                    readFrom++;
                }

                while (size > writeTo)
                {
                    commits.RemoveAt(size - 1);
                    size--;
                }
            }
        }

        /// <summary> Writer calls this when it has hit an error and had to
        /// roll back, to tell us that there may now be
        /// unreferenced files in the filesystem.  So we re-list
        /// the filesystem and delete such files.  If segmentName
        /// is non-null, we will only delete files corresponding to
        /// that segment.
        /// </summary>
        public void Refresh(string segmentName)
        {
            string[] files = directory.ListAll();
            string segmentPrefix1;
            string segmentPrefix2;
            if (segmentName != null)
            {
                segmentPrefix1 = segmentName + ".";
                segmentPrefix2 = segmentName + "_";
            }
            else
            {
                segmentPrefix1 = null;
                segmentPrefix2 = null;
            }

            Regex m = IndexFileNames.CODEC_FILE_PATTERN;

            for (int i = 0; i < files.Length; i++)
            {
                string fileName = files[i];
                if ((segmentName == null || fileName.StartsWith(segmentPrefix1) || fileName.StartsWith(segmentPrefix2)) &&
                  !fileName.EndsWith("write.lock") &&
                  !refCounts.ContainsKey(fileName) &&
                  !fileName.Equals(IndexFileNames.SEGMENTS_GEN) &&
                  (m.IsMatch(fileName) || fileName.StartsWith(IndexFileNames.SEGMENTS)))
                {
                    // Unreferenced file, so remove it
                    if (infoStream.IsEnabled("IFD"))
                    {
                        infoStream.Message("IFD", "refresh [prefix=" + segmentName + "]: removing newly created unreferenced file \"" + fileName + "\"");
                    }
                    DeleteFile(fileName);
                }
            }
        }

        public void Refresh()
        {
            deletable = null;
            Refresh(null);
        }

        public void Dispose()
        {
            // Move to protected method if class becomes unsealed
            // DecRef old files from the last checkpoint, if any:
            int size = lastFiles.Count;
            if (size > 0)
            {
                for (int i = 0; i < size; i++)
                    DecRef(lastFiles[i]);
                lastFiles.Clear();
            }

            DeletePendingFiles();
        }

        internal void RevisitPolicy()
        {
            //assert locked();
            if (infoStream.IsEnabled("IFD"))
            {
                infoStream.Message("IFD", "now revisitPolicy");
            }

            if (commits.Count > 0)
            {
                policy.OnCommit(commits);
                DeleteCommits();
            }
        }

        internal void DeletePendingFiles()
        {
            if (deletable != null)
            {
                IList<string> oldDeletable = deletable;
                deletable = null;
                int size = oldDeletable.Count;
                for (int i = 0; i < size; i++)
                {
                    if (infoStream.IsEnabled("IFD"))
                    {
                        infoStream.Message("IFD", "delete pending file " + oldDeletable[i]);
                    }
                    DeleteFile(oldDeletable[i]);
                }
            }
        }

        /// <summary> For definition of "check point" see IndexWriter comments:
        /// "Clarification: Check Points (and commits)".
        /// 
        /// Writer calls this when it has made a "consistent
        /// change" to the index, meaning new files are written to
        /// the index and the in-memory SegmentInfos have been
        /// modified to point to those files.
        /// 
        /// This may or may not be a commit (segments_N may or may
        /// not have been written).
        /// 
        /// We simply incref the files referenced by the new
        /// SegmentInfos and decref the files we had previously
        /// seen (if any).
        /// 
        /// If this is a commit, we also call the policy to give it
        /// a chance to remove other commits.  If any commits are
        /// removed, we decref their files as well.
        /// </summary>
        public void Checkpoint(SegmentInfos segmentInfos, bool isCommit)
        {
            long t0 = 0;
            if (infoStream.IsEnabled("IFD"))
            {
                t0 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                infoStream.Message("IFD", "now checkpoint \"" + writer.SegString(writer.ToLiveInfos(segmentInfos)) + "\" [" + segmentInfos.Count + " segments " + "; isCommit = " + isCommit + "]");
            }

            // Try again now to delete any previously un-deletable
            // files (because they were in use, on Windows):
            DeletePendingFiles();

            // Incref the files:
            IncRef(segmentInfos, isCommit);

            if (isCommit)
            {
                // Append to our commits list:
                commits.Add(new CommitPoint(this, commitsToDelete, directory, segmentInfos));

                // Tell policy so it can remove commits:
                policy.OnCommit(commits);

                // Decref files for commits that were deleted by the policy:
                DeleteCommits();
            }
            else
            {
                // DecRef old files from the last checkpoint, if any:
                foreach (ICollection<String> lastFile in lastFiles)
                {
                    DecRef(lastFile);
                }
                lastFiles.Clear();

                // Save files so we can decr on next checkpoint/commit:
                lastFiles.Add(segmentInfos.Files(directory, false));
            }

            if (infoStream.IsEnabled("IFD"))
            {
                long t1 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                infoStream.Message("IFD", (t1 - t0) + " msec to checkpoint");
            }
        }

        internal void IncRef(SegmentInfos segmentInfos, bool isCommit)
        {
            // If this is a commit point, also incRef the
            // segments_N file:
            foreach (string fileName in segmentInfos.Files(directory, isCommit))
            {
                IncRef(fileName);
            }
        }

        internal void IncRef(ICollection<string> files)
        {
            foreach (string file in files)
            {
                IncRef(file);
            }
        }

        internal void IncRef(string fileName)
        {
            RefCount rc = GetRefCount(fileName);
            if (infoStream.IsEnabled("IFD"))
            {
                if (VERBOSE_REF_COUNTS)
                {
                    infoStream.Message("IFD", "  IncRef \"" + fileName + "\": pre-incr count is " + rc.count);
                }
            }
            rc.IncRef();
        }

        internal void DecRef(ICollection<string> files)
        {
            foreach (string file in files)
            {
                DecRef(file);
            }
        }

        internal void DecRef(System.String fileName)
        {
            RefCount rc = GetRefCount(fileName);
            if (infoStream.IsEnabled("IFD"))
            {
                if (VERBOSE_REF_COUNTS)
                {
                    infoStream.Message("IFD", "  DecRef \"" + fileName + "\": pre-decr count is " + rc.count);
                }
            }
            if (0 == rc.DecRef())
            {
                // This file is no longer referenced by any past
                // commit points nor by the in-memory SegmentInfos:
                DeleteFile(fileName);
                refCounts.Remove(fileName);
            }
        }

        internal void DecRef(SegmentInfos segmentInfos)
        {
            foreach (string file in segmentInfos.Files(directory, false))
            {
                DecRef(file);
            }
        }

        public bool Exists(string fileName)
        {
            if (!refCounts.ContainsKey(fileName))
            {
                return false;
            }
            else
            {
                return GetRefCount(fileName).count > 0;
            }
        }

        private RefCount GetRefCount(string fileName)
        {
            RefCount rc;
            if (!refCounts.ContainsKey(fileName))
            {
                rc = new RefCount(fileName);
                refCounts[fileName] = rc;
            }
            else
            {
                rc = refCounts[fileName];
            }
            return rc;
        }

        internal void DeleteFiles(IList<string> files)
        {
            foreach (string file in files)
                DeleteFile(file);
        }

        /// <summary>Deletes the specified files, but only if they are new
        /// (have not yet been incref'd). 
        /// </summary>
        internal void DeleteNewFiles(ICollection<string> files)
        {
            foreach (string fileName in files)
            {
                // NOTE: it's very unusual yet possible for the
                // refCount to be present and 0: it can happen if you
                // open IW on a crashed index, and it removes a bunch
                // of unref'd files, and then you add new docs / do
                // merging, and it reuses that segment name.
                // TestCrash.testCrashAfterReopen can hit this:
                if (!refCounts.ContainsKey(fileName) || refCounts[fileName].count == 0)
                {
                    if (infoStream.IsEnabled("IFD"))
                    {
                        infoStream.Message("IFD", "delete new file \"" + fileName + "\"");
                    }
                    DeleteFile(fileName);
                }
            }
        }

        internal void DeleteFile(string fileName)
        {
            try
            {
                if (infoStream.IsEnabled("IFD"))
                {
                    infoStream.Message("IFD", "delete \"" + fileName + "\"");
                }
                directory.DeleteFile(fileName);
            }
            catch (System.IO.IOException e)
            {
                // if delete fails
                if (directory.FileExists(fileName))
                {

                    // Some operating systems (e.g. Windows) don't
                    // permit a file to be deleted while it is opened
                    // for read (e.g. by another process or thread). So
                    // we assume that when a delete fails it is because
                    // the file is open in another process, and queue
                    // the file for subsequent deletion.

                    if (infoStream.IsEnabled("IFD"))
                    {
                        infoStream.Message("IFD", "unable to remove file \"" + fileName + "\": " + e.ToString() + "; Will re-try later.");
                    }
                    if (deletable == null)
                    {
                        deletable = new List<string>();
                    }
                    deletable.Add(fileName); // add to deletable
                }
            }
        }

        /// <summary> Tracks the reference count for a single index file:</summary>
        private sealed class RefCount
        {
            // fileName used only for better assert error messages
            internal string fileName;
            internal bool initDone;

            internal RefCount(string fileName)
            {
                this.fileName = fileName;
            }

            internal int count;

            public int IncRef()
            {
                if (!initDone)
                {
                    initDone = true;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(count > 0, "RefCount is 0 pre-increment for file " + fileName);
                }
                return ++count;
            }

            public int DecRef()
            {
                System.Diagnostics.Debug.Assert(count > 0, "RefCount is 0 pre-decrement for file " + fileName);
                return --count;
            }
        }

        /// <summary> Holds details for each commit point.  This class is
        /// also passed to the deletion policy.  Note: this class
        /// has a natural ordering that is inconsistent with
        /// equals.
        /// </summary>

        private sealed class CommitPoint : IndexCommit
        {
            private readonly IndexFileDeleter enclosingInstance;
           
            internal long gen;
            internal ICollection<string> files;
            internal string segmentsFileName;
            internal bool deleted;
            internal Directory directory;
            internal ICollection<CommitPoint> commitsToDelete;
            internal long generation;
            internal IDictionary<string, string> userData;
            private readonly int segmentCount;

            public CommitPoint(IndexFileDeleter enclosingInstance, ICollection<CommitPoint> commitsToDelete, Directory directory, SegmentInfos segmentInfos)
            {
                this.enclosingInstance = enclosingInstance;
                this.directory = directory;
                this.commitsToDelete = commitsToDelete;
                userData = segmentInfos.UserData;
                segmentsFileName = segmentInfos.SegmentsFileName;
                generation = segmentInfos.Generation;
                files = segmentInfos.Files(directory, true);
                segmentCount = segmentInfos.Count;
            }

            public override string ToString()
            {
                return "IndexFileDeleter.CommitPoint(" + segmentsFileName + ")";
            }

            public override int SegmentCount
            {
                get { return segmentCount; }
            }

            public override string SegmentsFileName
            {
                get { return segmentsFileName; }
            }

            public override ICollection<string> FileNames
            {
                get { return files; }
            }

            public override Directory Directory
            {
                get { return directory; }
            }

            public override long Generation
            {
                get { return generation; }
            }

            public override IDictionary<string, string> UserData
            {
                get { return userData; }
            }

            /// <summary> Called only be the deletion policy, to remove this
            /// commit point from the index.
            /// </summary>
            public override void Delete()
            {
                if (!deleted)
                {
                    deleted = true;
                    enclosingInstance.commitsToDelete.Add(this);
                }
            }

            public override bool IsDeleted
            {
                get { return deleted; }
            }
        }
    }
}