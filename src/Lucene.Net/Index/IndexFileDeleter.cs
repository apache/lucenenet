using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
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

    using CollectionUtil = Lucene.Net.Util.CollectionUtil;
    using Directory = Lucene.Net.Store.Directory;
    using InfoStream = Lucene.Net.Util.InfoStream;

    /// <summary>
    /// This class keeps track of each SegmentInfos instance that
    /// is still "live", either because it corresponds to a
    /// segments_N file in the <see cref="Directory"/> (a "commit", i.e. a
    /// committed <see cref="SegmentInfos"/>) or because it's an in-memory
    /// <see cref="SegmentInfos"/> that a writer is actively updating but has
    /// not yet committed.  This class uses simple reference
    /// counting to map the live <see cref="SegmentInfos"/> instances to
    /// individual files in the <see cref="Directory"/>.
    /// <para/>
    /// The same directory file may be referenced by more than
    /// one <see cref="IndexCommit"/>, i.e. more than one <see cref="SegmentInfos"/>.
    /// Therefore we count how many commits reference each file.
    /// When all the commits referencing a certain file have been
    /// deleted, the refcount for that file becomes zero, and the
    /// file is deleted.
    /// <para/>
    /// A separate deletion policy interface
    /// (<see cref="IndexDeletionPolicy"/>) is consulted on creation (OnInit)
    /// and once per commit (OnCommit), to decide when a commit
    /// should be removed.
    /// <para/>
    /// It is the business of the <see cref="IndexDeletionPolicy"/> to choose
    /// when to delete commit points.  The actual mechanics of
    /// file deletion, retrying, etc, derived from the deletion
    /// of commit points is the business of the <see cref="IndexFileDeleter"/>.
    /// <para/>
    /// The current default deletion policy is
    /// <see cref="KeepOnlyLastCommitDeletionPolicy"/>, which removes all
    /// prior commits when a new commit has completed.  This
    /// matches the behavior before 2.2.
    /// <para/>
    /// Note that you must hold the <c>write.lock</c> before
    /// instantiating this class.  It opens segments_N file(s)
    /// directly with no retry logic.
    /// </summary>
    internal sealed class IndexFileDeleter : IDisposable
    {
        /// <summary>
        /// Files that we tried to delete but failed (likely
        /// because they are open and we are running on Windows),
        /// so we will retry them again later:
        /// </summary>
        private IList<string> deletable;

        /// <summary>
        /// Reference count for all files in the index.
        /// Counts how many existing commits reference a file.
        /// </summary>
        private readonly IDictionary<string, RefCount> refCounts = new Dictionary<string, RefCount>(); // LUCENENET: marked readonly

        /// <summary>
        /// Holds all commits (segments_N) currently in the index.
        /// this will have just 1 commit if you are using the
        /// default delete policy (KeepOnlyLastCommitDeletionPolicy).
        /// Other policies may leave commit points live for longer
        /// in which case this list would be longer than 1:
        /// </summary>
        private readonly IList<CommitPoint> commits = new JCG.List<CommitPoint>(); // LUCENENET: marked readonly

        /// <summary>
        /// Holds files we had incref'd from the previous
        /// non-commit checkpoint:
        /// </summary>
        private readonly JCG.List<string> lastFiles = new JCG.List<string>();

        /// <summary>
        /// Commits that the IndexDeletionPolicy have decided to delete:
        /// </summary>
        private readonly IList<CommitPoint> commitsToDelete = new JCG.List<CommitPoint>(); // LUCENENET: marked readonly

        private readonly InfoStream infoStream;
        private readonly Directory directory; // LUCENENET: marked readonly
        private readonly IndexDeletionPolicy policy; // LUCENENET: marked readonly

        internal readonly bool startingCommitDeleted;
        private readonly SegmentInfos lastSegmentInfos; // LUCENENET: marked readonly

        /// <summary>
        /// Change to true to see details of reference counts when
        /// infoStream is enabled
        /// </summary>
        public static bool VERBOSE_REF_COUNTS = false;

        // Used only for assert
        private readonly IndexWriter writer;

        // called only from assert
        private bool IsLocked =>
            writer is null || UninterruptableMonitor.IsEntered(writer);

        /// <summary>
        /// Initialize the deleter: find all previous commits in
        /// the <see cref="Directory"/>, incref the files they reference, call
        /// the policy to let it delete commits.  this will remove
        /// any files not referenced by any of the commits. </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public IndexFileDeleter(Directory directory, IndexDeletionPolicy policy, SegmentInfos segmentInfos, InfoStream infoStream, IndexWriter writer, bool initialIndexExists)
        {
            this.infoStream = infoStream;
            this.writer = writer;

            string currentSegmentsFile = segmentInfos.GetSegmentsFileName();

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
            string[] files/* = null*/;
            try
            {
                files = directory.ListAll();
            }
            catch (Exception e) when (e.IsNoSuchDirectoryException())
            {
                // it means the directory is empty, so ignore it.
                files = Arrays.Empty<string>();
            }

            if (currentSegmentsFile != null)
            {
                Regex r = IndexFileNames.CODEC_FILE_PATTERN;
                foreach (string fileName in files)
                {
                    if (!fileName.EndsWith("write.lock", StringComparison.Ordinal) && !fileName.Equals(IndexFileNames.SEGMENTS_GEN, StringComparison.Ordinal)
                        && (r.IsMatch(fileName) || fileName.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal)))
                    {
                        // Add this file to refCounts with initial count 0:
                        GetRefCount(fileName);

                        if (fileName.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal))
                        {
                            // this is a commit (segments or segments_N), and
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
                            catch (Exception e) when (e.IsNoSuchFileExceptionOrFileNotFoundException())
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
                            catch (Exception e) when (e.IsIOException())
                            {
                                if (SegmentInfos.GenerationFromSegmentsFileName(fileName) <= currentGen && directory.FileLength(fileName) > 0)
                                {
                                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
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
                                CommitPoint commitPoint = new CommitPoint(commitsToDelete, directory, sis);
                                if (sis.Generation == segmentInfos.Generation)
                                {
                                    currentCommitPoint = commitPoint;
                                }
                                commits.Add(commitPoint);
                                IncRef(sis, true);

                                if (lastSegmentInfos is null || sis.Generation > lastSegmentInfos.Generation)
                                {
                                    lastSegmentInfos = sis;
                                }
                            }
                        }
                    }
                }
            }

            if (currentCommitPoint is null && currentSegmentsFile != null && initialIndexExists)
            {
                // We did not in fact see the segments_N file
                // corresponding to the segmentInfos that was passed
                // in.  Yet, it must exist, because our caller holds
                // the write lock.  this can happen when the directory
                // listing was stale (eg when index accessed via NFS
                // client with stale directory listing cache).  So we
                // try now to explicitly open this commit point:
                SegmentInfos sis = new SegmentInfos();
                try
                {
                    sis.Read(directory, currentSegmentsFile);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw new CorruptIndexException("failed to locate current segments_N file \"" + currentSegmentsFile + "\"" + e.ToString(), e);
                }
                if (infoStream.IsEnabled("IFD"))
                {
                    infoStream.Message("IFD", "forced open of current segments file " + segmentInfos.GetSegmentsFileName());
                }
                currentCommitPoint = new CommitPoint(commitsToDelete, directory, sis);
                commits.Add(currentCommitPoint);
                IncRef(sis, true);
            }

            // We keep commits list in sorted order (oldest to newest):
            CollectionUtil.TimSort(commits);

            // Now delete anything with ref count at 0.  These are
            // presumably abandoned files eg due to crash of
            // IndexWriter.
            foreach (KeyValuePair<string, RefCount> entry in refCounts)
            {
                RefCount rc = entry.Value;
                string fileName = entry.Key;
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
            this.policy.OnInit(commits);

            // Always protect the incoming segmentInfos since
            // sometime it may not be the most recent commit
            Checkpoint(segmentInfos, false);

            startingCommitDeleted = currentCommitPoint is null ? false : currentCommitPoint.IsDeleted;

            DeleteCommits();
        }

        private void EnsureOpen()
        {
            if (writer is null)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this IndexWriter is disposed.");
            }
            else
            {
                writer.EnsureOpen(false);
            }
        }

        public SegmentInfos LastSegmentInfos => lastSegmentInfos;

        /// <summary>
        /// Remove the CommitPoints in the commitsToDelete List by
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

        /// <summary>
        /// Writer calls this when it has hit an error and had to
        /// roll back, to tell us that there may now be
        /// unreferenced files in the filesystem.  So we re-list
        /// the filesystem and delete such files.  If <paramref name="segmentName"/>
        /// is non-null, we will only delete files corresponding to
        /// that segment.
        /// </summary>
        public void Refresh(string segmentName)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);

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

            Regex r = IndexFileNames.CODEC_FILE_PATTERN;

            for (int i = 0; i < files.Length; i++)
            {
                string fileName = files[i];
                //m.reset(fileName);
                if ((segmentName is null || fileName.StartsWith(segmentPrefix1, StringComparison.Ordinal) || fileName.StartsWith(segmentPrefix2, StringComparison.Ordinal))
                    && !fileName.EndsWith("write.lock", StringComparison.Ordinal) && !refCounts.ContainsKey(fileName) && !fileName.Equals(IndexFileNames.SEGMENTS_GEN, StringComparison.Ordinal)
                    && (r.IsMatch(fileName) || fileName.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal)))
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
            // Set to null so that we regenerate the list of pending
            // files; else we can accumulate same file more than
            // once
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
            deletable = null;
            Refresh(null);
        }

        public void Dispose()
        {
            // DecRef old files from the last checkpoint, if any:
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);

            if (lastFiles.Count > 0)
            {
                DecRef(lastFiles);
                lastFiles.Clear();
            }

            DeletePendingFiles();
        }

        /// <summary>
        /// Revisits the <see cref="IndexDeletionPolicy"/> by calling its
        /// <see cref="IndexDeletionPolicy.OnCommit{T}(IList{T})"/> again with the known commits.
        /// this is useful in cases where a deletion policy which holds onto index
        /// commits is used. The application may know that some commits are not held by
        /// the deletion policy anymore and call
        /// <see cref="IndexWriter.DeleteUnusedFiles()"/>, which will attempt to delete the
        /// unused commits again.
        /// </summary>
        internal void RevisitPolicy()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
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

        public void DeletePendingFiles()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
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

        /// <summary>
        /// For definition of "check point" see <see cref="IndexWriter"/> comments:
        /// "Clarification: Check Points (and commits)".
        /// <para/>
        /// Writer calls this when it has made a "consistent
        /// change" to the index, meaning new files are written to
        /// the index and the in-memory <see cref="SegmentInfos"/> have been
        /// modified to point to those files.
        /// <para/>
        /// This may or may not be a commit (segments_N may or may
        /// not have been written).
        /// <para/>
        /// We simply incref the files referenced by the new
        /// <see cref="SegmentInfos"/> and decref the files we had previously
        /// seen (if any).
        /// <para/>
        /// If this is a commit, we also call the policy to give it
        /// a chance to remove other commits.  If any commits are
        /// removed, we decref their files as well.
        /// </summary>
        public void Checkpoint(SegmentInfos segmentInfos, bool isCommit)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(IsLocked);

                Debugging.Assert(UninterruptableMonitor.IsEntered(writer));
            }
            long t0 = 0;
            if (infoStream.IsEnabled("IFD"))
            {
                t0 = J2N.Time.NanoTime();
                infoStream.Message("IFD", "now checkpoint \"" + writer.SegString(writer.ToLiveInfos(segmentInfos).Segments) + "\" [" + segmentInfos.Count + " segments " + "; isCommit = " + isCommit + "]");
            }

            // Try again now to delete any previously un-deletable
            // files (because they were in use, on Windows):
            DeletePendingFiles();

            // Incref the files:
            IncRef(segmentInfos, isCommit);

            if (isCommit)
            {
                // Append to our commits list:
                commits.Add(new CommitPoint(commitsToDelete, directory, segmentInfos));

                // Tell policy so it can remove commits:
                policy.OnCommit(commits);

                // Decref files for commits that were deleted by the policy:
                DeleteCommits();
            }
            else
            {
                // DecRef old files from the last checkpoint, if any:
                DecRef(lastFiles);
                lastFiles.Clear();

                // Save files so we can decr on next checkpoint/commit:
                lastFiles.AddRange(segmentInfos.GetFiles(directory, false));
            }
            if (infoStream.IsEnabled("IFD"))
            {
                long t1 = J2N.Time.NanoTime();
                infoStream.Message("IFD", ((t1 - t0) / 1000000) + " msec to checkpoint");
            }
        }

        internal void IncRef(SegmentInfos segmentInfos, bool isCommit)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
            // If this is a commit point, also incRef the
            // segments_N file:
            foreach (string fileName in segmentInfos.GetFiles(directory, isCommit))
            {
                IncRef(fileName);
            }
        }

        internal void IncRef(ICollection<string> files)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
            foreach (string file in files)
            {
                IncRef(file);
            }
        }

        internal void IncRef(string fileName)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
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
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
            foreach (string file in files)
            {
                DecRef(file);
            }
        }

        internal void DecRef(string fileName)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
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
                // this file is no longer referenced by any past
                // commit points nor by the in-memory SegmentInfos:
                DeleteFile(fileName);
                refCounts.Remove(fileName);
            }
        }

        internal void DecRef(SegmentInfos segmentInfos)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
            foreach (string file in segmentInfos.GetFiles(directory, false))
            {
                DecRef(file);
            }
        }

        public bool Exists(string fileName)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
            // LUCENENET: Using TryGetValue to eliminate extra lookup
            return refCounts.TryGetValue(fileName, out RefCount value) && value.count > 0;
        }

        private RefCount GetRefCount(string fileName)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
            // LUCENENET: Using TryGetValue to eliminate extra lookup
            if (!refCounts.TryGetValue(fileName, out RefCount rc))
            {
                rc = new RefCount(fileName);
                refCounts[fileName] = rc;
            }
            return rc;
        }

        internal void DeleteFiles(IList<string> files)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
            foreach (string file in files)
            {
                DeleteFile(file);
            }
        }

        /// <summary>
        /// Deletes the specified files, but only if they are new
        /// (have not yet been incref'd).
        /// </summary>
        internal void DeleteNewFiles(ICollection<string> files)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
            foreach (string fileName in files)
            {
                // NOTE: it's very unusual yet possible for the
                // refCount to be present and 0: it can happen if you
                // open IW on a crashed index, and it removes a bunch
                // of unref'd files, and then you add new docs / do
                // merging, and it reuses that segment name.
                // TestCrash.testCrashAfterReopen can hit this:

                // LUCENENET: Using TryGetValue to eliminate extra lookup
                bool got = refCounts.TryGetValue(fileName, out RefCount refCount);
                if (!got || got && refCount.count == 0)
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
            if (Debugging.AssertsEnabled) Debugging.Assert(IsLocked);
            EnsureOpen();
            try
            {
                if (infoStream.IsEnabled("IFD"))
                {
                    infoStream.Message("IFD", "delete \"" + fileName + "\"");
                }
                directory.DeleteFile(fileName);
            } // if delete fails
            catch (Exception e) when (e.IsIOException())
            {
                // Some operating systems (e.g. Windows) don't
                // permit a file to be deleted while it is opened
                // for read (e.g. by another process or thread). So
                // we assume that when a delete fails it is because
                // the file is open in another process, and queue
                // the file for subsequent deletion.

                if (infoStream.IsEnabled("IFD"))
                {
                    infoStream.Message("IFD",
                        "unable to remove file \"" + fileName + "\": " + e.ToString() + "; Will re-try later.");
                }
                if (deletable is null)
                {
                    deletable = new JCG.List<string>();
                }
                deletable.Add(fileName); // add to deletable
            }
        }

        /// <summary>
        /// Tracks the reference count for a single index file:
        /// </summary>
        private sealed class RefCount
        {
            // fileName used only for better assert error messages
            internal readonly string fileName;

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
                    if (Debugging.AssertsEnabled) Debugging.Assert(count > 0, "{0}: RefCount is 0 pre-increment for file \"{1}\"", Thread.CurrentThread.Name, fileName);
                }
                return ++count;
            }

            public int DecRef()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(count > 0, "{0}: RefCount is 0 pre-decrement for file \"{1}\"", Thread.CurrentThread.Name, fileName);
                return --count;
            }
        }

        /// <summary>
        /// Holds details for each commit point.  This class is
        /// also passed to the deletion policy.  Note: this class
        /// has a natural ordering that is inconsistent with
        /// equals.
        /// </summary>

        private sealed class CommitPoint : IndexCommit
        {
            internal ICollection<string> files;
            internal string segmentsFileName;
            internal bool deleted;
            internal Directory directory;
            internal ICollection<CommitPoint> commitsToDelete;
            internal long generation;
            internal readonly IDictionary<string, string> userData;
            internal readonly int segmentCount;

            public CommitPoint(ICollection<CommitPoint> commitsToDelete, Directory directory, SegmentInfos segmentInfos)
            {
                this.directory = directory;
                this.commitsToDelete = commitsToDelete;
                userData = segmentInfos.UserData;
                segmentsFileName = segmentInfos.GetSegmentsFileName();
                generation = segmentInfos.Generation;
                files = segmentInfos.GetFiles(directory, true);
                segmentCount = segmentInfos.Count;
            }

            public override string ToString()
            {
                return "IndexFileDeleter.CommitPoint(" + segmentsFileName + ")";
            }

            public override int SegmentCount => segmentCount;

            public override string SegmentsFileName => segmentsFileName;

            public override ICollection<string> FileNames => files;

            public override Directory Directory => directory;

            public override long Generation => generation;

            public override IDictionary<string, string> UserData => userData;

            /// <summary>
            /// Called only by the deletion policy, to remove this
            /// commit point from the index.
            /// </summary>
            public override void Delete()
            {
                if (!deleted)
                {
                    deleted = true;
                    commitsToDelete.Add(this);
                }
            }

            public override bool IsDeleted => deleted;
        }
    }
}