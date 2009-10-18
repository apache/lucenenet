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

using Directory = Lucene.Net.Store.Directory;
using Lock = Lucene.Net.Store.Lock;
using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;

namespace Lucene.Net.Index
{

    /// <summary> IndexReader implementation that has access to a Directory. 
    /// Instances that have a SegmentInfos object (i. e. segmentInfos != null)
    /// "own" the directory, which means that they try to acquire a write lock
    /// whenever index modifications are performed.
    /// </summary>
    abstract public class DirectoryIndexReader : IndexReader
    {
        protected internal Directory directory;
        protected internal bool closeDirectory;
        private IndexDeletionPolicy deletionPolicy;

        private SegmentInfos segmentInfos;
        private Lock writeLock;
        private bool stale;
        private readonly IDictionary<string, string> synced = new Dictionary<string, string>();

        /// <summary>Used by commit() to record pre-commit state in case
        /// rollback is necessary 
        /// </summary>
        private bool rollbackHasChanges;
        private SegmentInfos rollbackSegmentInfos;

        protected internal bool readOnly;

        internal virtual void Init(Directory directory, SegmentInfos segmentInfos, bool closeDirectory, bool readOnly)
        {
            this.directory = directory;
            this.segmentInfos = segmentInfos;
            this.closeDirectory = closeDirectory;
            this.readOnly = readOnly;

            if (!readOnly && segmentInfos != null)
            {
                // we assume that this segments_N was properly sync'd prior
                for (int i = 0; i < segmentInfos.Count; i++)
                {
                    SegmentInfo info = segmentInfos.Info(i);
                    IList<string> files = info.Files();
                    for (int j = 0; j < files.Count; j++)
                        synced[files[j]] = files[j];
                }
            }
        }

        protected internal DirectoryIndexReader()
        {
        }

        internal DirectoryIndexReader(Directory directory, SegmentInfos segmentInfos, bool closeDirectory, bool readOnly)
            : base()
        {
            Init(directory, segmentInfos, closeDirectory, readOnly);
        }

        internal static DirectoryIndexReader Open(Directory directory, bool closeDirectory, IndexDeletionPolicy deletionPolicy)
        {
            return Open(directory, closeDirectory, deletionPolicy, null, false);
        }

        internal static DirectoryIndexReader Open(Directory directory, bool closeDirectory, IndexDeletionPolicy deletionPolicy, IndexCommit commit, bool readOnly)
        {
            SegmentInfos.FindSegmentsFile finder = new AnonymousClassFindSegmentsFile(closeDirectory, deletionPolicy, directory, readOnly);

            if (commit == null)
                return (DirectoryIndexReader) finder.Run();
            else
            {
                if (directory != commit.GetDirectory())
                    throw new System.IO.IOException("the specified commit does not match the specified Directory");
                // this can and will directly throw IOException if the specified commit point has been deleted
                return (DirectoryIndexReader)finder.DoBody(commit.GetSegmentsFileName());
            }
        }

        private class AnonymousClassFindSegmentsFile : SegmentInfos.FindSegmentsFile
        {
            private bool closeDirectory;
            private Lucene.Net.Index.IndexDeletionPolicy deletionPolicy;
            private bool readOnly;

            internal AnonymousClassFindSegmentsFile(bool closeDirectory, Lucene.Net.Index.IndexDeletionPolicy deletionPolicy, Lucene.Net.Store.Directory Param1, bool readOnly)
                : base(Param1)
            {
                this.closeDirectory = closeDirectory;
                this.deletionPolicy = deletionPolicy;
                this.readOnly = readOnly;
            }

            protected internal override object DoBody(System.String segmentFileName)
            {
                SegmentInfos infos = new SegmentInfos();
                infos.Read(directory, segmentFileName);

                DirectoryIndexReader reader;
                if (infos.Count == 1)
                {
                    // index is optimized
                    reader = SegmentReader.Get(readOnly, infos, infos.Info(0), closeDirectory);
                }
                else if (readOnly)
                {
                    reader = new ReadOnlyMultiSegmentReader(directory, infos, closeDirectory);
                }
                else
                {
                    reader = new MultiSegmentReader(directory, infos, closeDirectory, false);
                }
                reader.SetDeletionPolicy(deletionPolicy);

                return reader;
            }
        }

        public override IndexReader Reopen()
        {
            lock (this)
            {
                EnsureOpen();

                if (this.hasChanges || this.IsCurrent())
                {
                    // this has changes, therefore we have the lock and don't need to reopen
                    // OR: the index in the directory hasn't changed - nothing to do here
                    return this;
                }

                return (DirectoryIndexReader)new AnonymousClassFindSegmentsFile1(this, directory).Run();
            }
        }

        private class AnonymousClassFindSegmentsFile1 : SegmentInfos.FindSegmentsFile
        {
            private DirectoryIndexReader enclosingInstance;

            public DirectoryIndexReader Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            internal AnonymousClassFindSegmentsFile1(DirectoryIndexReader enclosingInstance, Lucene.Net.Store.Directory Param1)
                : base(Param1)
            {
                this.enclosingInstance = enclosingInstance;
            }

            protected internal override object DoBody(System.String segmentFileName)
            {
                SegmentInfos infos = new SegmentInfos();
                infos.Read(directory, segmentFileName);

                DirectoryIndexReader newReader = Enclosing_Instance.DoReopen(infos);

                if (Enclosing_Instance != newReader)
                {
                    newReader.Init(directory, infos, Enclosing_Instance.closeDirectory, Enclosing_Instance.readOnly);
                    newReader.deletionPolicy = Enclosing_Instance.deletionPolicy;
                }

                return newReader;
            }
        }

        /// <summary> Re-opens the index using the passed-in SegmentInfos </summary>
        protected internal abstract DirectoryIndexReader DoReopen(SegmentInfos infos);

        public virtual void SetDeletionPolicy(IndexDeletionPolicy deletionPolicy)
        {
            this.deletionPolicy = deletionPolicy;
        }

        /// <summary>Returns the directory this index resides in.</summary>
        public override Directory Directory()
        {
            EnsureOpen();
            return directory;
        }

        /// <summary> Version number when this IndexReader was opened.</summary>
        public override long GetVersion()
        {
            EnsureOpen();
            return segmentInfos.GetVersion();
        }

        /// <summary> Check whether this IndexReader is still using the
        /// current (i.e., most recently committed) version of the
        /// index.  If a writer has committed any changes to the
        /// index since this reader was opened, this will return
        /// <code>false</code>, in which case you must open a new
        /// IndexReader in order to see the changes.  See the
        /// description of the <a href="IndexWriter.html#autoCommit"><code>autoCommit</code></a>
        /// flag which controls when the {@link IndexWriter}
        /// actually commits changes to the index.
        /// 
        /// </summary>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  IOException if there is a low-level IO error </throws>
        public override bool IsCurrent()
        {
            EnsureOpen();
            return SegmentInfos.ReadCurrentVersion(directory) == segmentInfos.GetVersion();
        }

        /// <summary> Checks is the index is optimized (if it has a single segment and no deletions)</summary>
        /// <returns> <code>true</code> if the index is optimized; <code>false</code> otherwise
        /// </returns>
        public override bool IsOptimized()
        {
            EnsureOpen();
            return segmentInfos.Count == 1 && HasDeletions() == false;
        }

        protected internal override void DoClose()
        {
            if (closeDirectory)
                directory.Close();
        }

        /// <summary> Commit changes resulting from delete, undeleteAll, or
        /// setNorm operations
        /// 
        /// If an exception is hit, then either no changes or all
        /// changes will have been committed to the index
        /// (transactional semantics).
        /// </summary>
        /// <throws>  IOException if there is a low-level IO error </throws>
        protected internal override void DoCommit()
        {
            if (hasChanges)
            {
                if (segmentInfos != null)
                {

                    // Default deleter (for backwards compatibility) is
                    // KeepOnlyLastCommitDeleter:
                    IndexFileDeleter deleter = new IndexFileDeleter(directory, deletionPolicy == null ? new KeepOnlyLastCommitDeletionPolicy() : deletionPolicy, segmentInfos, null, null);

                    // Checkpoint the state we are about to change, in
                    // case we have to roll back:
                    StartCommit();

                    bool success = false;
                    try
                    {
                        CommitChanges();

                        // sync all the files we just wrote
                        for (int i = 0; i < segmentInfos.Count; i++)
                        {
                            SegmentInfo info = segmentInfos.Info(i);
                            IList<string> files = info.Files();
                            for (int j = 0; j < files.Count; j++)
                            {
                                string fileName = files[j];
                                if (!synced.ContainsKey(fileName))
                                {
                                    System.Diagnostics.Debug.Assert(directory.FileExists(fileName));
                                    directory.Sync(fileName);
                                    synced[fileName] = fileName;
                                }
                            }
                        }

                        segmentInfos.Commit(directory);
                        success = true;
                    }
                    finally
                    {

                        if (!success)
                        {

                            // Rollback changes that were made to
                            // SegmentInfos but failed to get [fully]
                            // committed.  This way this reader instance
                            // remains consistent (matched to what's
                            // actually in the index):
                            RollbackCommit();

                            // Recompute deletable files & remove them (so
                            // partially written .del files, etc, are
                            // removed):
                            deleter.Refresh();
                        }
                    }

                    // Have the deleter remove any now unreferenced
                    // files due to this commit:
                    deleter.Checkpoint(segmentInfos, true);

                    if (writeLock != null)
                    {
                        writeLock.Release(); // release write lock
                        writeLock = null;
                    }
                }
                else
                    CommitChanges();
            }
            hasChanges = false;
        }

        protected internal abstract void CommitChanges();

        /// <summary> Tries to acquire the WriteLock on this directory.
        /// this method is only valid if this IndexReader is directory owner.
        /// 
        /// </summary>
        /// <throws>  StaleReaderException if the index has changed </throws>
        /// <summary> since this reader was opened
        /// </summary>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  LockObtainFailedException if another writer </throws>
        /// <summary>  has this index open (<code>write.lock</code> could not
        /// be obtained)
        /// </summary>
        /// <throws>  IOException if there is a low-level IO error </throws>
        protected internal override void AcquireWriteLock()
        {
            if (segmentInfos != null)
            {
                EnsureOpen();
                if (stale)
                    throw new StaleReaderException("IndexReader out of date and no longer valid for delete, undelete, or setNorm operations");

                if (this.writeLock == null)
                {
                    Lock writeLock = directory.MakeLock(IndexWriter.WRITE_LOCK_NAME);
                    if (!writeLock.Obtain(IndexWriter.WRITE_LOCK_TIMEOUT))
                    // obtain write lock
                    {
                        throw new LockObtainFailedException("Index locked for write: " + writeLock);
                    }
                    this.writeLock = writeLock;

                    // we have to check whether index has changed since this reader was opened.
                    // if so, this reader is no longer valid for deletion
                    if (SegmentInfos.ReadCurrentVersion(directory) > segmentInfos.GetVersion())
                    {
                        stale = true;
                        this.writeLock.Release();
                        this.writeLock = null;
                        throw new StaleReaderException("IndexReader out of date and no longer valid for delete, undelete, or setNorm operations");
                    }
                }
            }
        }

        /// <summary> Should internally checkpoint state that will change
        /// during commit so that we can rollback if necessary.
        /// </summary>
        internal virtual void StartCommit()
        {
            if (segmentInfos != null)
            {
                rollbackSegmentInfos = (SegmentInfos)segmentInfos.Clone();
            }
            rollbackHasChanges = hasChanges;
        }

        /// <summary> Rolls back state to just before the commit (this is
        /// called by commit() if there is some exception while
        /// committing).
        /// </summary>
        internal virtual void RollbackCommit()
        {
            if (segmentInfos != null)
            {
                for (int i = 0; i < segmentInfos.Count; i++)
                {
                    // Rollback each segmentInfo.  Because the
                    // SegmentReader holds a reference to the
                    // SegmentInfo we can't [easily] just replace
                    // segmentInfos, so we reset it in place instead:
                    segmentInfos.Info(i).Reset(rollbackSegmentInfos.Info(i));
                }
                rollbackSegmentInfos = null;
            }

            hasChanges = rollbackHasChanges;
        }

        /// <summary>Release the write lock, if needed. </summary>
        ~DirectoryIndexReader()
        {
            try
            {
                if (writeLock != null)
                {
                    writeLock.Release(); // release write lock
                    writeLock = null;
                }
            }
            finally
            {
                // {{Aroush-2.3.1}} do we need to call Finalize() here?
            }
        }

        private class ReaderCommit : IndexCommit
        {
            private string segmentsFileName;
            internal ICollection<string> files;
            internal Directory dir;
            internal long generation;
            internal long version;
            internal readonly bool isOptimized;

            internal ReaderCommit(SegmentInfos infos, Directory dir)
            {
                segmentsFileName = infos.GetCurrentSegmentFileName();
                this.dir = dir;
                int size = infos.Count;
                files = new List<string>(size);
                files.Add(segmentsFileName);
                for (int i = 0; i < size; i++)
                {
                    SegmentInfo info = infos.Info(i);
                    if (info.dir == dir)
                        SupportClass.CollectionsSupport.AddAll(info.Files(), files);
                }
                version = infos.GetVersion();
                generation = infos.GetGeneration();
                isOptimized = infos.Count == 1 && !infos.Info(0).HasDeletions();
            }

            public override bool IsOptimized()
            {
                return isOptimized;
            }
            public override string GetSegmentsFileName()
            {
                return segmentsFileName;
            }
            public override ICollection<string> GetFileNames()
            {
                return files;
            }
            public override Directory GetDirectory()
            {
                return dir;
            }
            public override long GetVersion()
            {
                return version;
            }
            public override long GetGeneration()
            {
                return generation;
            }
            public override bool IsDeleted()
            {
                return false;
            }
        }

        /**
         * Expert: return the IndexCommit that this reader has
         * opened.
         *
         * <p><b>WARNING</b>: this API is new and experimental and
         * may suddenly change.</p>
         */
        public override IndexCommit GetIndexCommit()
        {
            return new ReaderCommit(segmentInfos, directory);
        }

        /** @see IndexReader#listCommits */
        public new static ICollection<IndexCommitPoint> ListCommits(Directory dir)
        {

            string[] files = dir.List();
            if (files == null)
                throw new System.IO.IOException("cannot read directory " + dir + ": list() returned null");

            ICollection<IndexCommitPoint> commits = new List<IndexCommitPoint>();

            SegmentInfos latest = new SegmentInfos();
            latest.Read(dir);
            long currentGen = latest.GetGeneration();

            commits.Add(new ReaderCommit(latest, dir));

            for (int i = 0; i < files.Length; i++)
            {

                String fileName = files[i];

                if (fileName.StartsWith(IndexFileNames.SEGMENTS) &&
                    !fileName.Equals(IndexFileNames.SEGMENTS_GEN) &&
                    SegmentInfos.GenerationFromSegmentsFileName(fileName) < currentGen)
                {
                    SegmentInfos sis = new SegmentInfos();
                    try
                    {
                        // IOException allowed to throw there, in case
                        // segments_N is corrupt
                        sis.Read(dir, fileName);
                    }
                    catch (System.Exception)
                    {
                        // LUCENE-948: on NFS (and maybe others), if
                        // you have writers switching back and forth
                        // between machines, it's very likely that the
                        // dir listing will be stale and will claim a
                        // file segments_X exists when in fact it
                        // doesn't.  So, we catch this and handle it
                        // as if the file does not exist
                        sis = null;
                    }

                    if (sis != null)
                        commits.Add(new ReaderCommit(sis, dir));
                }
            }

            return commits;
        }
    }
}