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
using System.IO;
using System.Linq;
using Lucene.Net.Support;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using Document = Lucene.Net.Documents.Document;
using IndexingChain = Lucene.Net.Index.DocumentsWriterPerThread.IndexingChain;
using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;
using Directory = Lucene.Net.Store.Directory;
using Lock = Lucene.Net.Store.Lock;
using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
using Constants = Lucene.Net.Util.Constants;
using Query = Lucene.Net.Search.Query;
using Similarity = Lucene.Net.Search.Similarity;
using FieldNumbers = Lucene.Net.Index.FieldInfos.FieldNumbers;
using System.Threading;
using Lucene.Net.Util;
using Lucene.Net.Codecs;
using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
using Lucene.Net.Store;

namespace Lucene.Net.Index
{

    /// <summary>An <c>IndexWriter</c> creates and maintains an index.
    /// <p/>The <c>create</c> argument to the 
    /// <see cref="IndexWriter(Directory, Analyzer, bool, MaxFieldLength)">constructor</see> determines 
    /// whether a new index is created, or whether an existing index is
    /// opened.  Note that you can open an index with <c>create=true</c>
    /// even while readers are using the index.  The old readers will 
    /// continue to search the "point in time" snapshot they had opened, 
    /// and won't see the newly created index until they re-open.  There are
    /// also <see cref="IndexWriter(Directory, Analyzer, MaxFieldLength)">constructors</see>
    /// with no <c>create</c> argument which will create a new index
    /// if there is not already an index at the provided path and otherwise 
    /// open the existing index.<p/>
    /// <p/>In either case, documents are added with <see cref="AddDocument(Document)" />
    /// and removed with <see cref="DeleteDocuments(Term)" /> or
    /// <see cref="DeleteDocuments(Query)" />. A document can be updated with
    /// <see cref="UpdateDocument(Term, Document)" /> (which just deletes
    /// and then adds the entire document). When finished adding, deleting 
    /// and updating documents, <see cref="Close()" /> should be called.<p/>
    /// <a name="flush"></a>
    /// <p/>These changes are buffered in memory and periodically
    /// flushed to the <see cref="Directory" /> (during the above method
    /// calls).  A flush is triggered when there are enough
    /// buffered deletes (see <see cref="SetMaxBufferedDeleteTerms" />)
    /// or enough added documents since the last flush, whichever
    /// is sooner.  For the added documents, flushing is triggered
    /// either by RAM usage of the documents (see 
    /// <see cref="SetRAMBufferSizeMB" />) or the number of added documents.
    /// The default is to flush when RAM usage hits 16 MB.  For
    /// best indexing speed you should flush by RAM usage with a
    /// large RAM buffer.  Note that flushing just moves the
    /// internal buffered state in IndexWriter into the index, but
    /// these changes are not visible to IndexReader until either
    /// <see cref="Commit()" /> or <see cref="Close()" /> is called.  A flush may
    /// also trigger one or more segment merges which by default
    /// run with a background thread so as not to block the
    /// addDocument calls (see <a href="#mergePolicy">below</a>
    /// for changing the <see cref="MergeScheduler" />).
    /// <p/>
    /// If an index will not have more documents added for a while and optimal search
    /// performance is desired, then either the full <see cref="Optimize()" />
    /// method or partial <see cref="Optimize(int)" /> method should be
    /// called before the index is closed.
    /// <p/>
    /// Opening an <c>IndexWriter</c> creates a lock file for the directory in use. Trying to open
    /// another <c>IndexWriter</c> on the same directory will lead to a
    /// <see cref="LockObtainFailedException" />. The <see cref="LockObtainFailedException" />
    /// is also thrown if an IndexReader on the same directory is used to delete documents
    /// from the index.<p/>
    /// </summary>
    /// <summary><a name="deletionPolicy"></a>
    /// <p/>Expert: <c>IndexWriter</c> allows an optional
    /// <see cref="IndexDeletionPolicy" /> implementation to be
    /// specified.  You can use this to control when prior commits
    /// are deleted from the index.  The default policy is <see cref="KeepOnlyLastCommitDeletionPolicy" />
    /// which removes all prior
    /// commits as soon as a new commit is done (this matches
    /// behavior before 2.2).  Creating your own policy can allow
    /// you to explicitly keep previous "point in time" commits
    /// alive in the index for some time, to allow readers to
    /// refresh to the new commit without having the old commit
    /// deleted out from under them.  This is necessary on
    /// filesystems like NFS that do not support "delete on last
    /// close" semantics, which Lucene's "point in time" search
    /// normally relies on. <p/>
    /// <a name="mergePolicy"></a> <p/>Expert:
    /// <c>IndexWriter</c> allows you to separately change
    /// the <see cref="MergePolicy" /> and the <see cref="MergeScheduler" />.
    /// The <see cref="MergePolicy" /> is invoked whenever there are
    /// changes to the segments in the index.  Its role is to
    /// select which merges to do, if any, and return a <see cref="Index.MergePolicy.MergeSpecification" />
    /// describing the merges.  It
    /// also selects merges to do for optimize().  (The default is
    /// <see cref="LogByteSizeMergePolicy" />.  Then, the <see cref="MergeScheduler" />
    /// is invoked with the requested merges and
    /// it decides when and how to run the merges.  The default is
    /// <see cref="ConcurrentMergeScheduler" />. <p/>
    /// <a name="OOME"></a><p/><b>NOTE</b>: if you hit an
    /// OutOfMemoryError then IndexWriter will quietly record this
    /// fact and block all future segment commits.  This is a
    /// defensive measure in case any internal state (buffered
    /// documents and deletions) were corrupted.  Any subsequent
    /// calls to <see cref="Commit()" /> will throw an
    /// IllegalStateException.  The only course of action is to
    /// call <see cref="Close()" />, which internally will call <see cref="Rollback()" />
    ///, to undo any changes to the index since the
    /// last commit.  You can also just call <see cref="Rollback()" />
    /// directly.<p/>
    /// <a name="thread-safety"></a><p/><b>NOTE</b>: 
    /// <see cref="IndexWriter" /> instances are completely thread
    /// safe, meaning multiple threads can call any of its
    /// methods, concurrently.  If your application requires
    /// external synchronization, you should <b>not</b>
    /// synchronize on the <c>IndexWriter</c> instance as
    /// this may cause deadlock; use your own (non-Lucene) objects
    /// instead. <p/>
    /// <b>NOTE:</b> if you call
    /// <c>Thread.Interrupt()</c> on a thread that's within
    /// IndexWriter, IndexWriter will try to catch this (eg, if
    /// it's in a Wait() or Thread.Sleep()), and will then throw
    /// the unchecked exception <see cref="System.Threading.ThreadInterruptedException"/>
    /// and <b>clear</b> the interrupt status on the thread<p/>
    /// </summary>

    /*
    * Clarification: Check Points (and commits)
    * IndexWriter writes new index files to the directory without writing a new segments_N
    * file which references these new files. It also means that the state of 
    * the in memory SegmentInfos object is different than the most recent
    * segments_N file written to the directory.
    * 
    * Each time the SegmentInfos is changed, and matches the (possibly 
    * modified) directory files, we have a new "check point". 
    * If the modified/new SegmentInfos is written to disk - as a new 
    * (generation of) segments_N file - this check point is also an 
    * IndexCommit.
    * 
    * A new checkpoint always replaces the previous checkpoint and 
    * becomes the new "front" of the index. This allows the IndexFileDeleter 
    * to delete files that are referenced only by stale checkpoints.
    * (files that were created since the last commit, but are no longer
    * referenced by the "front" of the index). For this, IndexFileDeleter 
    * keeps track of the last non commit checkpoint.
    */
    public class IndexWriter : IDisposable, ITwoPhaseCommit
    {
        private const int UNBOUNDED_MAX_MERGE_SEGMENTS = -1;

        public const string WRITE_LOCK_NAME = "write.lock";

        public const string SOURCE = "source";

        public const string SOURCE_MERGE = "merge";

        public const string SOURCE_FLUSH = "flush";

        public const string SOURCE_ADDINDEXES_READERS = "addIndexes(IndexReader...)";

        public const int MAX_TERM_LENGTH = DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8;
        private volatile bool hitOOM;

        private readonly Directory directory;  // where this index resides
        private readonly Analyzer analyzer;    // how to analyze text

        private volatile long changeCount; // increments every time a change is completed
        private long lastCommitChangeCount; // last changeCount that was committed

        private IList<SegmentInfoPerCommit> rollbackSegments;      // list of segmentInfo we will fallback to if the commit fails

        internal volatile SegmentInfos pendingCommit;            // set when a commit is pending (after prepareCommit() & before commit())
        internal volatile long pendingCommitChangeCount;

        private ICollection<String> filesToCommit;

        internal readonly SegmentInfos segmentInfos;       // the segments
        internal readonly FieldNumbers globalFieldNumberMap;

        private DocumentsWriter docWriter;
        internal readonly IndexFileDeleter deleter;

        // used by forceMerge to note those needing merging
        private IDictionary<SegmentInfoPerCommit, bool> segmentsToMerge = new HashMap<SegmentInfoPerCommit, bool>();
        private int mergeMaxNumSegments;

        private Lock writeLock;

        private volatile bool closed;
        private volatile bool closing;

        // Holds all SegmentInfo instances currently involved in
        // merges
        private HashSet<SegmentInfoPerCommit> mergingSegments = new HashSet<SegmentInfoPerCommit>();

        private MergePolicy mergePolicy;
        private readonly MergeScheduler mergeScheduler;
        private LinkedList<MergePolicy.OneMerge> pendingMerges = new LinkedList<MergePolicy.OneMerge>();
        private ISet<MergePolicy.OneMerge> runningMerges = new HashSet<MergePolicy.OneMerge>();
        private List<MergePolicy.OneMerge> mergeExceptions = new List<MergePolicy.OneMerge>();
        private long mergeGen;
        private bool stopMerges;

        internal int flushCount = 0;
        internal int flushDeletesCount = 0;

        internal readonly ReaderPool readerPool;
        internal readonly BufferedDeletesStream bufferedDeletesStream;

        // This is a "write once" variable (like the organic dye
        // on a DVD-R that may or may not be heated by a laser and
        // then cooled to permanently record the event): it's
        // false, until getReader() is called for the first time,
        // at which point it's switched to true and never changes
        // back to false.  Once this is true, we hold open and
        // reuse SegmentReader instances internally for applying
        // deletes, doing merges, and reopening near real-time
        // readers.
        private volatile bool poolReaders;

        // The instance that was passed to the constructor. It is saved only in order
        // to allow users to query an IndexWriter settings.
        private readonly LiveIndexWriterConfig config;

        internal DirectoryReader Reader
        {
            get
            {
                return GetReader(true);
            }
        }

        internal DirectoryReader GetReader(bool applyAllDeletes)
        {
            EnsureOpen();

            long tStart = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            if (infoStream.isEnabled("IW"))
            {
                infoStream.message("IW", "flush at getReader");
            }
            // Do this up front before flushing so that the readers
            // obtained during this flush are pooled, the first time
            // this method is called:
            poolReaders = true;
            DirectoryReader r = null;
            DoBeforeFlush();
            bool anySegmentFlushed = false;
            /*
             * for releasing a NRT reader we must ensure that 
             * DW doesn't add any segments or deletes until we are
             * done with creating the NRT DirectoryReader. 
             * We release the two stage full flush after we are done opening the
             * directory reader!
             */
            bool success2 = false;
            try
            {
                lock (fullFlushLock)
                {
                    bool success = false;
                    try
                    {
                        anySegmentFlushed = docWriter.FlushAllThreads();
                        if (!anySegmentFlushed)
                        {
                            // prevent double increment since docWriter#doFlush increments the flushcount
                            // if we flushed anything.
                            Interlocked.Increment(ref flushCount);
                        }
                        success = true;
                        // Prevent segmentInfos from changing while opening the
                        // reader; in theory we could instead do similar retry logic,
                        // just like we do when loading segments_N
                        lock (this)
                        {
                            MaybeApplyDeletes(applyAllDeletes);
                            r = StandardDirectoryReader.Open(this, segmentInfos, applyAllDeletes);
                            if (infoStream.isEnabled("IW"))
                            {
                                infoStream.message("IW", "return reader version=" + r.Version + " reader=" + r);
                            }
                        }
                    }
                    catch (OutOfMemoryException oom)
                    {
                        HandleOOM(oom, "getReader");
                        // never reached but javac disagrees:
                        return null;
                    }
                    finally
                    {
                        if (!success)
                        {
                            if (infoStream.isEnabled("IW"))
                            {
                                infoStream.message("IW", "hit exception during NRT reader");
                            }
                        }
                        // Done: finish the full flush!
                        docWriter.FinishFullFlush(success);
                        DoAfterFlush();
                    }
                }
                if (anySegmentFlushed)
                {
                    MaybeMerge(MergeTrigger.FULL_FLUSH, UNBOUNDED_MAX_MERGE_SEGMENTS);
                }
                if (infoStream.isEnabled("IW"))
                {
                    infoStream.message("IW", "getReader took " + ((DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) - tStart) + " msec");
                }
                success2 = true;
            }
            finally
            {
                if (!success2)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)r);
                }
            }
            return r;
        }

        internal class ReaderPool
        {
            private readonly IndexWriter parent;

            public ReaderPool(IndexWriter parent)
            {
                this.parent = parent;
            }

            private readonly IDictionary<SegmentInfoPerCommit, ReadersAndLiveDocs> readerMap = new HashMap<SegmentInfoPerCommit, ReadersAndLiveDocs>();

            // used only by asserts
            public bool InfoIsLive(SegmentInfoPerCommit info)
            {
                lock (this)
                {
                    int idx = parent.segmentInfos.IndexOf(info);
                    //assert idx != -1: "info=" + info + " isn't live";
                    //assert segmentInfos.info(idx) == info: "info=" + info + " doesn't match live info in segmentInfos";
                    return true;
                }
            }

            public void Drop(SegmentInfoPerCommit info)
            {
                lock (this)
                {
                    ReadersAndLiveDocs rld = readerMap[info];
                    if (rld != null)
                    {
                        //assert info == rld.info;
                        readerMap.Remove(info);
                        rld.DropReaders();
                    }
                }
            }

            public bool AnyPendingDeletes
            {
                get
                {
                    lock (this)
                    {
                        foreach (ReadersAndLiveDocs rld in readerMap.Values)
                        {
                            if (rld.PendingDeleteCount != 0)
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }

            public void Release(ReadersAndLiveDocs rld)
            {
                lock (this)
                {
                    // Matches incRef in get:
                    rld.DecRef();

                    // Pool still holds a ref:
                    //assert rld.refCount() >= 1;

                    if (!parent.poolReaders && rld.RefCount == 1)
                    {
                        // This is the last ref to this RLD, and we're not
                        // pooling, so remove it:
                        if (rld.WriteLiveDocs(parent.directory))
                        {
                            // Make sure we only write del docs for a live segment:
                            //assert infoIsLive(rld.info);
                            // Must checkpoint w/ deleter, because we just
                            // created created new _X_N.del file.
                            parent.deleter.Checkpoint(parent.segmentInfos, false);
                        }

                        rld.DropReaders();
                        readerMap.Remove(rld.Info);
                    }
                }
            }

            internal void DropAll(bool doSave)
            {
                lock (this)
                {
                    Exception priorE = null;
                    IEnumerator<KeyValuePair<SegmentInfoPerCommit, ReadersAndLiveDocs>> it = readerMap.ToList().GetEnumerator();
                    while (it.MoveNext())
                    {
                        ReadersAndLiveDocs rld = it.Current.Value;

                        try
                        {
                            if (doSave && rld.WriteLiveDocs(parent.directory))
                            {
                                // Make sure we only write del docs for a live segment:
                                //assert infoIsLive(rld.info);
                                // Must checkpoint w/ deleter, because we just
                                // created created new _X_N.del file.
                                parent.deleter.Checkpoint(parent.segmentInfos, false);
                            }
                        }
                        catch (Exception t)
                        {
                            if (priorE != null)
                            {
                                priorE = t;
                            }
                        }

                        // Important to remove as-we-go, not with .clear()
                        // in the end, in case we hit an exception;
                        // otherwise we could over-decref if close() is
                        // called again:
                        readerMap.Remove(it.Current);

                        // NOTE: it is allowed that these decRefs do not
                        // actually close the SRs; this happens when a
                        // near real-time reader is kept open after the
                        // IndexWriter instance is closed:
                        try
                        {
                            rld.DropReaders();
                        }
                        catch (Exception t)
                        {
                            if (priorE != null)
                            {
                                priorE = t;
                            }
                        }
                    }
                    //assert readerMap.size() == 0;
                    if (priorE != null)
                    {
                        throw priorE;
                    }
                }
            }

            public void Commit(SegmentInfos infos)
            {
                lock (this)
                {
                    foreach (SegmentInfoPerCommit info in infos)
                    {
                        ReadersAndLiveDocs rld = readerMap[info];
                        if (rld != null)
                        {
                            //assert rld.info == info;
                            if (rld.WriteLiveDocs(parent.directory))
                            {
                                // Make sure we only write del docs for a live segment:
                                //assert infoIsLive(info);
                                // Must checkpoint w/ deleter, because we just
                                // created created new _X_N.del file.
                                parent.deleter.Checkpoint(parent.segmentInfos, false);
                            }
                        }
                    }
                }
            }

            public ReadersAndLiveDocs Get(SegmentInfoPerCommit info, bool create)
            {
                lock (this)
                {

                    //assert info.info.dir == directory: "info.dir=" + info.info.dir + " vs " + directory;

                    ReadersAndLiveDocs rld = readerMap[info];
                    if (rld == null)
                    {
                        if (!create)
                        {
                            return null;
                        }
                        rld = new ReadersAndLiveDocs(parent, info);
                        // Steal initial reference:
                        readerMap[info] = rld;
                    }
                    else
                    {
                        //assert rld.info == info: "rld.info=" + rld.info + " info=" + info + " isLive?=" + infoIsLive(rld.info) + " vs " + infoIsLive(info);
                    }

                    if (create)
                    {
                        // Return ref to caller:
                        rld.IncRef();
                    }

                    //assert noDups();

                    return rld;
                }
            }

            private bool NoDups()
            {
                ISet<String> seen = new HashSet<String>();
                foreach (SegmentInfoPerCommit info in readerMap.Keys)
                {
                    //assert !seen.contains(info.info.name);
                    seen.Add(info.info.name);
                }
                return true;
            }
        }

        public virtual int NumDeletedDocs(SegmentInfoPerCommit info)
        {
            EnsureOpen(false);
            int delCount = info.DelCount;

            ReadersAndLiveDocs rld = readerPool.Get(info, false);
            if (rld != null)
            {
                delCount += rld.PendingDeleteCount;
            }
            return delCount;
        }

        protected void EnsureOpen(bool failIfClosing)
        {
            if (closed || (failIfClosing && closing))
            {
                throw new AlreadyClosedException("this IndexWriter is closed");
            }
        }

        protected void EnsureOpen()
        {
            EnsureOpen(true);
        }

        internal readonly Codec codec; // for writing new segments

        public IndexWriter(Directory d, IndexWriterConfig conf)
        {
            readerPool = new ReaderPool(this); // .NET Port: can't reference "this" in-line
            config = new LiveIndexWriterConfig((IndexWriterConfig)conf.Clone());
            directory = d;
            analyzer = config.Analyzer;
            infoStream = config.InfoStream;
            mergePolicy = config.MergePolicy;
            mergePolicy.IndexWriter = this;
            mergeScheduler = config.MergeScheduler;
            codec = config.Codec;

            bufferedDeletesStream = new BufferedDeletesStream(infoStream);
            poolReaders = config.ReaderPooling;

            writeLock = directory.MakeLock(WRITE_LOCK_NAME);

            if (!writeLock.Obtain(config.WriteLockTimeout)) // obtain write lock
                throw new LockObtainFailedException("Index locked for write: " + writeLock);

            bool success = false;
            try
            {
                OpenMode mode = config.OpenMode;
                bool create;
                if (mode == OpenMode.CREATE)
                {
                    create = true;
                }
                else if (mode == OpenMode.APPEND)
                {
                    create = false;
                }
                else
                {
                    // CREATE_OR_APPEND - create only if an index does not exist
                    create = !DirectoryReader.IndexExists(directory);
                }

                // If index is too old, reading the segments will throw
                // IndexFormatTooOldException.
                segmentInfos = new SegmentInfos();

                bool initialIndexExists = true;

                if (create)
                {
                    // Try to read first.  This is to allow create
                    // against an index that's currently open for
                    // searching.  In this case we write the next
                    // segments_N file with no segments:
                    try
                    {
                        segmentInfos.Read(directory);
                        segmentInfos.Clear();
                    }
                    catch (IOException e)
                    {
                        // Likely this means it's a fresh directory
                        initialIndexExists = false;
                    }

                    // Record that we have a change (zero out all
                    // segments) pending:
                    Changed();
                }
                else
                {
                    segmentInfos.Read(directory);

                    IndexCommit commit = config.IndexCommit;
                    if (commit != null)
                    {
                        // Swap out all segments, but, keep metadata in
                        // SegmentInfos, like version & generation, to
                        // preserve write-once.  This is important if
                        // readers are open against the future commit
                        // points.
                        if (commit.Directory != directory)
                            throw new ArgumentException("IndexCommit's directory doesn't match my directory");
                        SegmentInfos oldInfos = new SegmentInfos();
                        oldInfos.Read(directory, commit.SegmentsFileName);
                        segmentInfos.Replace(oldInfos);
                        Changed();
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "init: loaded commit \"" + commit.SegmentsFileName + "\"");
                        }
                    }
                }

                rollbackSegments = segmentInfos.CreateBackupSegmentInfos();

                // start with previous field numbers, but new FieldInfos
                globalFieldNumberMap = FieldNumberMap;
                docWriter = new DocumentsWriter(codec, config, directory, this, globalFieldNumberMap, bufferedDeletesStream);

                // Default deleter (for backwards compatibility) is
                // KeepOnlyLastCommitDeleter:
                lock (this)
                {
                    deleter = new IndexFileDeleter(directory,
                                                   config.IndexDeletionPolicy,
                                                   segmentInfos, infoStream, this,
                                                   initialIndexExists);
                }

                if (deleter.startingCommitDeleted)
                {
                    // Deletion policy deleted the "head" commit point.
                    // We have to mark ourself as changed so that if we
                    // are closed w/o any further changes we write a new
                    // segments_N file.
                    Changed();
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "init: create=" + create);
                    MessageState();
                }

                success = true;

            }
            finally
            {
                if (!success)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "init: hit exception on init; releasing write lock");
                    }
                    try
                    {
                        writeLock.Release();
                    }
                    catch (Exception t)
                    {
                        // don't mask the original exception
                    }
                    writeLock = null;
                }
            }
        }

        private FieldInfos GetFieldInfos(SegmentInfo info)
        {
            Directory cfsDir = null;
            try
            {
                if (info.UseCompoundFile)
                {
                    cfsDir = new CompoundFileDirectory(info.dir,
                                                       IndexFileNames.SegmentFileName(info.name, "", IndexFileNames.COMPOUND_FILE_EXTENSION),
                                                       IOContext.READONCE,
                                                       false);
                }
                else
                {
                    cfsDir = info.dir;
                }
                return info.Codec.FieldInfosFormat().FieldInfosReader.Read(cfsDir, info.name, IOContext.READONCE);
            }
            finally
            {
                if (info.UseCompoundFile && cfsDir != null)
                {
                    cfsDir.Dispose();
                }
            }
        }

        private FieldNumbers FieldNumberMap
        {
            get
            {
                FieldNumbers map = new FieldNumbers();

                foreach (SegmentInfoPerCommit info in segmentInfos)
                {
                    foreach (FieldInfo fi in GetFieldInfos(info.info))
                    {
                        map.AddOrGet(fi.name, fi.number, fi.DocValuesTypeValue.GetValueOrDefault());
                    }
                }

                return map;
            }
        }

        public LiveIndexWriterConfig Config
        {
            get
            {
                EnsureOpen(false);
                return config;
            }
        }

        private void MessageState()
        {
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "\ndir=" + directory + "\n" +
                      "index=" + SegString() + "\n" +
                      "version=" + Constants.LUCENE_VERSION + "\n" +
                      config.ToString());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool waitForMerges)
        {
            // Ensure that only one thread actually gets to do the
            // closing, and make sure no commit is also in progress:
            lock (commitLock)
            {
                if (ShouldClose())
                {
                    // If any methods have hit OutOfMemoryError, then abort
                    // on close, in case the internal state of IndexWriter
                    // or DocumentsWriter is corrupt
                    if (hitOOM)
                    {
                        RollbackInternal();
                    }
                    else
                    {
                        CloseInternal(waitForMerges, true);
                    }
                }
            }
        }

        private bool ShouldClose()
        {
            lock (this)
            {
                while (true)
                {
                    if (!closed)
                    {
                        if (!closing)
                        {
                            closing = true;
                            return true;
                        }
                        else
                        {
                            // Another thread is presently trying to close;
                            // wait until it finishes one way (closes
                            // successfully) or another (fails to close)
                            DoWait();
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        private void CloseInternal(bool waitForMerges, bool doFlush)
        {
            bool interrupted = false;
            try
            {

                if (pendingCommit != null)
                {
                    throw new InvalidOperationException("cannot close: prepareCommit was already called with no corresponding call to commit");
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "now flush at close waitForMerges=" + waitForMerges);
                }

                docWriter.Dispose();

                try
                {
                    // Only allow a new merge to be triggered if we are
                    // going to wait for merges:
                    if (doFlush)
                    {
                        Flush(waitForMerges, true);
                    }
                    else
                    {
                        docWriter.Abort(); // already closed -- never sync on IW 
                    }

                }
                finally
                {
                    try
                    {
                        // clean up merge scheduler in all cases, although flushing may have failed:
                        interrupted = false;

                        if (waitForMerges)
                        {
                            try
                            {
                                // Give merge scheduler last chance to run, in case
                                // any pending merges are waiting:
                                mergeScheduler.Merge(this);
                            }
                            catch (ThreadInterruptedException tie)
                            {
                                // ignore any interruption, does not matter
                                interrupted = true;
                                if (infoStream.IsEnabled("IW"))
                                {
                                    infoStream.Message("IW", "interrupted while waiting for final merges");
                                }
                            }
                        }

                        lock (this)
                        {
                            for (; ; )
                            {
                                try
                                {
                                    FinishMerges(waitForMerges && !interrupted);
                                    break;
                                }
                                catch (ThreadInterruptedException tie)
                                {
                                    // by setting the interrupted status, the
                                    // next call to finishMerges will pass false,
                                    // so it will not wait
                                    interrupted = true;
                                    if (infoStream.IsEnabled("IW"))
                                    {
                                        infoStream.Message("IW", "interrupted while waiting for merges to finish");
                                    }
                                }
                            }
                            stopMerges = true;
                        }

                    }
                    finally
                    {
                        // shutdown policy, scheduler and all threads (this call is not interruptible):
                        IOUtils.CloseWhileHandlingException((IDisposable)mergePolicy, mergeScheduler);
                    }
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "now call final commit()");
                }

                if (doFlush)
                {
                    CommitInternal();
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "at close: " + SegString());
                }
                // used by assert below
                DocumentsWriter oldWriter = docWriter;
                lock (this)
                {
                    readerPool.DropAll(true);
                    docWriter = null;
                    deleter.Dispose();
                }

                if (writeLock != null)
                {
                    writeLock.Release();                          // release write lock
                    writeLock = null;
                }
                lock (this)
                {
                    closed = true;
                }
                //assert oldWriter.perThreadPool.numDeactivatedThreadStates() == oldWriter.perThreadPool.getMaxThreadStates();
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "closeInternal");
            }
            finally
            {
                lock (this)
                {
                    closing = false;
                    Monitor.PulseAll(this);
                    if (!closed)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "hit exception while closing");
                        }
                    }
                }
                // finally, restore interrupt status:
                if (interrupted) Thread.CurrentThread.Interrupt();
            }
        }

        public virtual Directory Directory
        {
            get { return directory; }
        }

        public virtual Analyzer Analyzer
        {
            get
            {
                EnsureOpen();
                return analyzer;
            }
        }

        public int MaxDoc
        {
            get
            {
                lock (this)
                {
                    EnsureOpen();
                    int count;
                    if (docWriter != null)
                        count = docWriter.NumDocs;
                    else
                        count = 0;

                    count += segmentInfos.TotalDocCount;
                    return count;
                }
            }
        }

        public int NumDocs
        {
            get
            {
                lock (this)
                {
                    EnsureOpen();
                    int count;
                    if (docWriter != null)
                        count = docWriter.NumDocs;
                    else
                        count = 0;

                    foreach (SegmentInfoPerCommit info in segmentInfos)
                    {
                        count += info.info.DocCount - NumDeletedDocs(info);
                    }
                    return count;
                }
            }
        }

        public bool HasDeletions
        {
            get
            {
                lock (this)
                {
                    EnsureOpen();
                    if (bufferedDeletesStream.Any())
                    {
                        return true;
                    }
                    if (docWriter.AnyDeletions)
                    {
                        return true;
                    }
                    if (readerPool.AnyPendingDeletes)
                    {
                        return true;
                    }
                    foreach (SegmentInfoPerCommit info in segmentInfos)
                    {
                        if (info.HasDeletions)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        public void AddDocument(IEnumerable<IIndexableField> doc)
        {
            AddDocument(doc, analyzer);
        }

        public void AddDocument(IEnumerable<IIndexableField> doc, Analyzer analyzer)
        {
            UpdateDocument(null, doc, analyzer);
        }

        public void AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            AddDocuments(docs, analyzer);
        }

        public void AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer analyzer)
        {
            UpdateDocuments(null, docs, analyzer);
        }

        public void UpdateDocuments(Term delTerm, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            UpdateDocuments(delTerm, docs, analyzer);
        }

        public void UpdateDocuments(Term delTerm, IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer analyzer)
        {
            EnsureOpen();
            try
            {
                bool success = false;
                bool anySegmentFlushed = false;
                try
                {
                    anySegmentFlushed = docWriter.UpdateDocuments(docs, analyzer, delTerm);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "hit exception updating document");
                        }
                    }
                }
                if (anySegmentFlushed)
                {
                    MaybeMerge(MergeTrigger.SEGMENT_FLUSH, UNBOUNDED_MAX_MERGE_SEGMENTS);
                }
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "updateDocuments");
            }
        }

        public void DeleteDocuments(Term term)
        {
            EnsureOpen();
            try
            {
                docWriter.DeleteTerms(term);
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "deleteDocuments(Term)");
            }
        }

        public bool TryDeleteDocument(IndexReader readerIn, int docID)
        {
            lock (this)
            {
                AtomicReader reader;
                if (readerIn is AtomicReader)
                {
                    // Reader is already atomic: use the incoming docID:
                    reader = (AtomicReader)readerIn;
                }
                else
                {
                    // Composite reader: lookup sub-reader and re-base docID:
                    IList<AtomicReaderContext> leaves = readerIn.Leaves;
                    int subIndex = ReaderUtil.SubIndex(docID, leaves);
                    reader = leaves[subIndex].Reader;
                    docID -= leaves[subIndex].docBase;
                    //assert docID >= 0;
                    //assert docID < reader.maxDoc();
                }

                if (!(reader is SegmentReader))
                {
                    throw new ArgumentException("the reader must be a SegmentReader or composite reader containing only SegmentReaders");
                }

                SegmentInfoPerCommit info = ((SegmentReader)reader).SegmentInfo;

                // TODO: this is a slow linear search, but, number of
                // segments should be contained unless something is
                // seriously wrong w/ the index, so it should be a minor
                // cost:

                if (segmentInfos.IndexOf(info) != -1)
                {
                    ReadersAndLiveDocs rld = readerPool.Get(info, false);
                    if (rld != null)
                    {
                        lock (bufferedDeletesStream)
                        {
                            rld.InitWritableLiveDocs();
                            if (rld.Delete(docID))
                            {
                                int fullDelCount = rld.Info.DelCount + rld.PendingDeleteCount;
                                if (fullDelCount == rld.Info.info.DocCount)
                                {
                                    // If a merge has already registered for this
                                    // segment, we leave it in the readerPool; the
                                    // merge will skip merging it and will then drop
                                    // it once it's done:
                                    if (!mergingSegments.Contains(rld.Info))
                                    {
                                        segmentInfos.Remove(rld.Info);
                                        readerPool.Drop(rld.Info);
                                        Checkpoint();
                                    }
                                }

                                // Must bump changeCount so if no other changes
                                // happened, we still commit this change:
                                Changed();
                            }
                            //System.out.println("  yes " + info.info.name + " " + docID);
                            return true;
                        }
                    }
                    else
                    {
                        //System.out.println("  no rld " + info.info.name + " " + docID);
                    }
                }
                else
                {
                    //System.out.println("  no seg " + info.info.name + " " + docID);
                }
                return false;
            }
        }

        public void DeleteDocuments(params Term[] terms)
        {
            EnsureOpen();
            try
            {
                docWriter.DeleteTerms(terms);
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "deleteDocuments(Term..)");
            }
        }

        public void DeleteDocuments(Query query)
        {
            EnsureOpen();
            try
            {
                docWriter.DeleteQueries(query);
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "deleteDocuments(Query)");
            }
        }

        public void DeleteDocuments(params Query[] queries)
        {
            EnsureOpen();
            try
            {
                docWriter.DeleteQueries(queries);
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "deleteDocuments(Query..)");
            }
        }

        public void UpdateDocument(Term term, IEnumerable<IIndexableField> doc)
        {
            EnsureOpen();
            UpdateDocument(term, doc, analyzer);
        }

        public void UpdateDocument(Term term, IEnumerable<IIndexableField> doc, Analyzer analyzer)
        {
            EnsureOpen();
            try
            {
                bool success = false;
                bool anySegmentFlushed = false;
                try
                {
                    anySegmentFlushed = docWriter.UpdateDocument(doc, analyzer, term);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "hit exception updating document");
                        }
                    }
                }

                if (anySegmentFlushed)
                {
                    MaybeMerge(MergeTrigger.SEGMENT_FLUSH, UNBOUNDED_MAX_MERGE_SEGMENTS);
                }
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "updateDocument");
            }
        }

        internal int SegmentCount
        {
            get
            {
                lock (this)
                {
                    return segmentInfos.Count;
                }
            }
        }

        internal int NumBufferedDocuments
        {
            get
            {
                lock (this)
                {
                    return docWriter.NumDocs;
                }
            }
        }

        internal ICollection<String> IndexFileNames
        {
            get
            {
                lock (this)
                {
                    return segmentInfos.Files(directory, true);
                }
            }
        }

        internal int GetDocCount(int i)
        {
            if (i >= 0 && i < segmentInfos.Count)
            {
                return segmentInfos.Info(i).info.DocCount;
            }
            else
            {
                return -1;
            }
        }

        internal int FlushCount
        {
            get
            {
                return flushCount;
            }
        }

        internal int FlushDeletesCount
        {
            get
            {
                return flushDeletesCount;
            }
        }

        internal string NewSegmentName()
        {
            // Cannot synchronize on IndexWriter because that causes
            // deadlock
            lock (segmentInfos)
            {
                // Important to increment changeCount so that the
                // segmentInfos is written on close.  Otherwise we
                // could close, re-open and re-return the same segment
                // name that was previously returned which can cause
                // problems at least with ConcurrentMergeScheduler.
                changeCount++;
                segmentInfos.Changed();
                return "_" + Number.ToString(segmentInfos.counter++, Character.MAX_RADIX);
            }
        }

        internal readonly InfoStream infoStream;

        public void ForceMerge(int maxNumSegments)
        {
            ForceMerge(maxNumSegments, true);
        }

        public void ForceMerge(int maxNumSegments, bool doWait)
        {
            EnsureOpen();

            if (maxNumSegments < 1)
                throw new ArgumentException("maxNumSegments must be >= 1; got " + maxNumSegments);

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "forceMerge: index now " + SegString());
                infoStream.Message("IW", "now flush at forceMerge");
            }

            Flush(true, true);

            lock (this)
            {
                ResetMergeExceptions();
                segmentsToMerge.Clear();
                foreach (SegmentInfoPerCommit info in segmentInfos)
                {
                    segmentsToMerge[info] = true;
                }
                mergeMaxNumSegments = maxNumSegments;

                // Now mark all pending & running merges for forced
                // merge:
                foreach (MergePolicy.OneMerge merge in pendingMerges)
                {
                    merge.maxNumSegments = maxNumSegments;
                    segmentsToMerge[merge.info] = true;
                }

                foreach (MergePolicy.OneMerge merge in runningMerges)
                {
                    merge.maxNumSegments = maxNumSegments;
                    segmentsToMerge[merge.info] = true;
                }
            }

            MaybeMerge(MergeTrigger.EXPLICIT, maxNumSegments);

            if (doWait)
            {
                lock (this)
                {
                    while (true)
                    {

                        if (hitOOM)
                        {
                            throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot complete forceMerge");
                        }

                        if (mergeExceptions.Count > 0)
                        {
                            // Forward any exceptions in background merge
                            // threads to the current thread:
                            int size = mergeExceptions.Count;
                            for (int i = 0; i < size; i++)
                            {
                                MergePolicy.OneMerge merge = mergeExceptions[i];
                                if (merge.maxNumSegments != -1)
                                {
                                    IOException err = new IOException("background merge hit exception: " + merge.SegString(directory), merge.GetException() ?? new Exception());

                                    throw err;
                                }
                            }
                        }

                        if (MaxNumSegmentsMergesPending())
                            DoWait();
                        else
                            break;
                    }
                }

                // If close is called while we are still
                // running, throw an exception so the calling
                // thread will know merging did not
                // complete
                EnsureOpen();
            }

            // NOTE: in the ConcurrentMergeScheduler case, when
            // doWait is false, we can return immediately while
            // background threads accomplish the merging
        }

        private bool MaxNumSegmentsMergesPending
        {
            get
            {
                lock (this)
                {
                    foreach (MergePolicy.OneMerge merge in pendingMerges)
                    {
                        if (merge.maxNumSegments != -1)
                            return true;
                    }

                    foreach (MergePolicy.OneMerge merge in runningMerges)
                    {
                        if (merge.maxNumSegments != -1)
                            return true;
                    }

                    return false;
                }
            }
        }

        public void ForceMergeDeletes(bool doWait)
        {
            EnsureOpen();

            Flush(true, true);

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "forceMergeDeletes: index now " + SegString());
            }

            MergePolicy.MergeSpecification spec;

            lock (this)
            {
                spec = mergePolicy.FindForcedDeletesMerges(segmentInfos);
                if (spec != null)
                {
                    int numMerges = spec.merges.Count;
                    for (int i = 0; i < numMerges; i++)
                        RegisterMerge(spec.merges[i]);
                }
            }

            mergeScheduler.Merge(this);

            if (spec != null && doWait)
            {
                int numMerges = spec.merges.Count;
                lock (this)
                {
                    bool running = true;
                    while (running)
                    {

                        if (hitOOM)
                        {
                            throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot complete forceMergeDeletes");
                        }

                        // Check each merge that MergePolicy asked us to
                        // do, to see if any of them are still running and
                        // if any of them have hit an exception.
                        running = false;
                        for (int i = 0; i < numMerges; i++)
                        {
                            MergePolicy.OneMerge merge = spec.merges[i];
                            if (pendingMerges.Contains(merge) || runningMerges.Contains(merge))
                            {
                                running = true;
                            }
                            Exception t = merge.GetException();
                            if (t != null)
                            {
                                IOException ioe = new IOException("background merge hit exception: " + merge.SegString(directory), t);

                                throw ioe;
                            }
                        }

                        // If any of our merges are still running, wait:
                        if (running)
                            DoWait();
                    }
                }
            }

            // NOTE: in the ConcurrentMergeScheduler case, when
            // doWait is false, we can return immediately while
            // background threads accomplish the merging
        }

        public void ForceMergeDeletes()
        {
            ForceMergeDeletes(true);
        }

        public void MaybeMerge()
        {
            MaybeMerge(MergeTrigger.EXPLICIT, UNBOUNDED_MAX_MERGE_SEGMENTS);
        }

        private void MaybeMerge(MergeTrigger trigger, int maxNumSegments)
        {
            EnsureOpen(false);
            UpdatePendingMerges(trigger, maxNumSegments);
            mergeScheduler.Merge(this);
        }

        private void UpdatePendingMerges(MergeTrigger trigger, int maxNumSegments)
        {
            lock (this)
            {
                //assert maxNumSegments == -1 || maxNumSegments > 0;
                //assert trigger != null;
                if (stopMerges)
                {
                    return;
                }

                // Do not start new merges if we've hit OOME
                if (hitOOM)
                {
                    return;
                }

                MergePolicy.MergeSpecification spec;
                if (maxNumSegments != UNBOUNDED_MAX_MERGE_SEGMENTS)
                {
                    //assert trigger == MergeTrigger.EXPLICIT || trigger == MergeTrigger.MERGE_FINISHED :
                    //  "Expected EXPLICT or MERGE_FINISHED as trigger even with maxNumSegments set but was: " + trigger.name();
                    spec = mergePolicy.FindForcedMerges(segmentInfos, maxNumSegments, segmentsToMerge);
                    if (spec != null)
                    {
                        int numMerges = spec.merges.Count;
                        for (int i = 0; i < numMerges; i++)
                        {
                            MergePolicy.OneMerge merge = spec.merges[i];
                            merge.maxNumSegments = maxNumSegments;
                        }
                    }

                }
                else
                {
                    spec = mergePolicy.FindMerges(trigger, segmentInfos);
                }

                if (spec != null)
                {
                    int numMerges = spec.merges.Count;
                    for (int i = 0; i < numMerges; i++)
                    {
                        RegisterMerge(spec.merges[i]);
                    }
                }
            }
        }

        public ICollection<SegmentInfoPerCommit> MergingSegments
        {
            get
            {
                lock (this)
                {
                    return mergingSegments;
                }
            }
        }

        public MergePolicy.OneMerge NextMerge
        {
            get
            {
                lock (this)
                {
                    if (pendingMerges.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        // Advance the merge from pending to running
                        MergePolicy.OneMerge merge = pendingMerges.First.Value;
                        pendingMerges.RemoveFirst();
                        runningMerges.Add(merge);
                        return merge;
                    }
                }
            }
        }

        public bool HasPendingMerges
        {
            get
            {
                lock (this)
                {
                    return pendingMerges.Count != 0;
                }
            }
        }
    }
}