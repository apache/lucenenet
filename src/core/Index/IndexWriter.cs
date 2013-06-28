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

using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;
using Constants = Lucene.Net.Util.Constants;
using Directory = Lucene.Net.Store.Directory;
using Document = Lucene.Net.Documents.Document;
using FieldNumbers = Lucene.Net.Index.FieldInfos.FieldNumbers;
using IndexingChain = Lucene.Net.Index.DocumentsWriterPerThread.IndexingChain;
using Lock = Lucene.Net.Store.Lock;
using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
using MergeTrigger = Lucene.Net.Index.MergePolicy.MergeTrigger;
using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
using Query = Lucene.Net.Search.Query;
using Similarity = Lucene.Net.Search.Similarity;

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

        private long changeCount; // increments every time a change is completed
        private long lastCommitChangeCount; // last changeCount that was committed

        private IList<SegmentInfoPerCommit> rollbackSegments;      // list of segmentInfo we will fallback to if the commit fails

        internal volatile SegmentInfos pendingCommit;            // set when a commit is pending (after prepareCommit() & before commit())
        internal long pendingCommitChangeCount;

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

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "flush at getReader");
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
                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "return reader version=" + r.Version + " reader=" + r);
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
                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "hit exception during NRT reader");
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
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "getReader took " + ((DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) - tStart) + " msec");
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
            mergePolicy.SetIndexWriter(this);
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
                OpenMode mode = config.OpenModeValue;
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
                                                       Lucene.Net.Index.IndexFileNames.SegmentFileName(info.name, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_EXTENSION),
                                                       IOContext.READONCE,
                                                       false);
                }
                else
                {
                    cfsDir = info.dir;
                }
                return info.Codec.FieldInfosFormat.FieldInfosReader.Read(cfsDir, info.name, IOContext.READONCE);
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

                        if (MaxNumSegmentsMergesPending)
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
                            Exception t = merge.Exception;
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

        // .NET Port: Not override, part of ITwoPhaseCommit
        public void Rollback()
        {
            EnsureOpen();

            lock (commitLock)
            {
                if (ShouldClose())
                    RollbackInternal();
            }
        }

        private void RollbackInternal()
        {

            bool success = false;

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "rollback");
            }

            try
            {
                lock (this)
                {
                    FinishMerges(false);
                    stopMerges = true;
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "rollback: done finish merges");
                }

                // Must pre-close these two, in case they increment
                // changeCount so that we can then set it to false
                // before calling closeInternal
                mergePolicy.Dispose();
                mergeScheduler.Dispose();

                bufferedDeletesStream.Clear();
                docWriter.Close(); // mark it as closed first to prevent subsequent indexing actions/flushes 
                docWriter.Abort(); // don't sync on IW here
                lock (this)
                {

                    if (pendingCommit != null)
                    {
                        pendingCommit.RollbackCommit(directory);
                        deleter.DecRef(pendingCommit);
                        pendingCommit = null;
                        Monitor.PulseAll(this);
                    }

                    // Don't bother saving any changes in our segmentInfos
                    readerPool.DropAll(false);

                    // Keep the same segmentInfos instance but replace all
                    // of its SegmentInfo instances.  This is so the next
                    // attempt to commit using this instance of IndexWriter
                    // will always write to a new generation ("write
                    // once").
                    segmentInfos.RollbackSegmentInfos(rollbackSegments);
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "rollback: infos=" + SegString(segmentInfos));
                    }


                    //assert testPoint("rollback before checkpoint");

                    // Ask deleter to locate unreferenced files & remove
                    // them:
                    deleter.Checkpoint(segmentInfos, false);
                    deleter.Refresh();

                    lastCommitChangeCount = changeCount;
                }

                success = true;
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "rollbackInternal");
            }
            finally
            {
                lock (this)
                {
                    if (!success)
                    {
                        closing = false;
                        Monitor.PulseAll(this);
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "hit exception during rollback");
                        }
                    }
                }
            }

            CloseInternal(false, false);
        }

        public void DeleteAll()
        {
            EnsureOpen();
            // Remove any buffered docs
            bool success = false;
            /* hold the full flush lock to prevent concurrency commits / NRT reopens to
             * get in our way and do unnecessary work. -- if we don't lock this here we might
             * get in trouble if */
            lock (fullFlushLock)
            {
                /*
                 * We first abort and trash everything we have in-memory
                 * and keep the thread-states locked, the lockAndAbortAll operation
                 * also guarantees "point in time semantics" ie. the checkpoint that we need in terms
                 * of logical happens-before relationship in the DW. So we do
                 * abort all in memory structures 
                 * We also drop global field numbering before during abort to make
                 * sure it's just like a fresh index.
                 */
                try
                {
                    docWriter.LockAndAbortAll();
                    lock (this)
                    {
                        try
                        {
                            // Abort any running merges
                            FinishMerges(false);
                            // Remove all segments
                            segmentInfos.Clear();
                            // Ask deleter to locate unreferenced files & remove them:
                            deleter.Checkpoint(segmentInfos, false);
                            /* don't refresh the deleter here since there might
                             * be concurrent indexing requests coming in opening
                             * files on the directory after we called DW#abort()
                             * if we do so these indexing requests might hit FNF exceptions.
                             * We will remove the files incrementally as we go...
                             */
                            // Don't bother saving any changes in our segmentInfos
                            readerPool.DropAll(false);
                            // Mark that the index has changed
                            ++changeCount;
                            segmentInfos.Changed();
                            globalFieldNumberMap.Clear();
                            success = true;
                        }
                        catch (OutOfMemoryException oom)
                        {
                            HandleOOM(oom, "deleteAll");
                        }
                        finally
                        {
                            if (!success)
                            {
                                if (infoStream.IsEnabled("IW"))
                                {
                                    infoStream.Message("IW", "hit exception during deleteAll");
                                }
                            }
                        }
                    }
                }
                finally
                {
                    docWriter.UnlockAllAfterAbortAll();
                }
            }
        }

        private void FinishMerges(bool waitForMerges)
        {
            lock (this)
            {
                if (!waitForMerges)
                {

                    stopMerges = true;

                    // Abort all pending & running merges:
                    foreach (MergePolicy.OneMerge merge in pendingMerges)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "now abort pending merge " + SegString(merge.segments));
                        }
                        merge.Abort();
                        MergeFinish(merge);
                    }
                    pendingMerges.Clear();

                    foreach (MergePolicy.OneMerge merge in runningMerges)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "now abort running merge " + SegString(merge.segments));
                        }
                        merge.Abort();
                    }

                    // These merges periodically check whether they have
                    // been aborted, and stop if so.  We wait here to make
                    // sure they all stop.  It should not take very long
                    // because the merge threads periodically check if
                    // they are aborted.
                    while (runningMerges.Count > 0)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "now wait for " + runningMerges.Count + " running merge/s to abort");
                        }
                        DoWait();
                    }

                    stopMerges = false;
                    Monitor.PulseAll(this);

                    //assert 0 == mergingSegments.size();

                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "all running merges have aborted");
                    }

                }
                else
                {
                    // waitForMerges() will ensure any running addIndexes finishes.
                    // It's fine if a new one attempts to start because from our
                    // caller above the call will see that we are in the
                    // process of closing, and will throw an
                    // AlreadyClosedException.
                    WaitForMerges();
                }
            }
        }

        public void WaitForMerges()
        {
            lock (this)
            {
                EnsureOpen(false);
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "waitForMerges");
                }
                while (pendingMerges.Count > 0 || runningMerges.Count > 0)
                {
                    DoWait();
                }

                // sanity check
                //assert 0 == mergingSegments.size();

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "waitForMerges done");
                }
            }
        }

        internal void Checkpoint()
        {
            lock (this)
            {
                Changed();
                deleter.Checkpoint(segmentInfos, false);
            }
        }

        internal void Changed()
        {
            lock (this)
            {
                changeCount++;
                segmentInfos.Changed();
            }
        }

        internal void PublishFrozenDeletes(FrozenBufferedDeletes packet)
        {
            lock (this)
            {
                //assert packet != null && packet.any();
                lock (bufferedDeletesStream)
                {
                    bufferedDeletesStream.Push(packet);
                }
            }
        }

        internal void PublishFlushedSegment(SegmentInfoPerCommit newSegment,
            FrozenBufferedDeletes packet, FrozenBufferedDeletes globalPacket)
        {
            lock (this)
            {
                // Lock order IW -> BDS
                lock (bufferedDeletesStream)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "publishFlushedSegment");
                    }

                    if (globalPacket != null && globalPacket.Any())
                    {
                        bufferedDeletesStream.Push(globalPacket);
                    }
                    // Publishing the segment must be synched on IW -> BDS to make the sure
                    // that no merge prunes away the seg. private delete packet
                    long nextGen;
                    if (packet != null && packet.Any())
                    {
                        nextGen = bufferedDeletesStream.Push(packet);
                    }
                    else
                    {
                        // Since we don't have a delete packet to apply we can get a new
                        // generation right away
                        nextGen = bufferedDeletesStream.NextGen;
                    }
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "publish sets newSegment delGen=" + nextGen + " seg=" + SegString(newSegment));
                    }
                    newSegment.BufferedDeletesGen = nextGen;
                    segmentInfos.Add(newSegment);
                    Checkpoint();
                }
            }
        }

        internal bool UseCompoundFile(SegmentInfoPerCommit segmentInfo)
        {
            lock (this)
            {
                return mergePolicy.UseCompoundFile(segmentInfos, segmentInfo);
            }
        }

        private void ResetMergeExceptions()
        {
            lock (this)
            {
                mergeExceptions = new List<MergePolicy.OneMerge>();
                mergeGen++;
            }
        }

        private void NoDupDirs(params Directory[] dirs)
        {
            HashSet<Directory> dups = new HashSet<Directory>();
            for (int i = 0; i < dirs.Length; i++)
            {
                if (dups.Contains(dirs[i]))
                    throw new ArgumentException("Directory " + dirs[i] + " appears more than once");
                if (dirs[i] == directory)
                    throw new ArgumentException("Cannot add directory to itself");
                dups.Add(dirs[i]);
            }
        }

        public void AddIndexes(params Directory[] dirs)
        {
            EnsureOpen();

            NoDupDirs(dirs);

            try
            {
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "flush at addIndexes(Directory...)");
                }

                Flush(false, true);

                List<SegmentInfoPerCommit> infos = new List<SegmentInfoPerCommit>();
                bool success = false;
                try
                {
                    foreach (Directory dir in dirs)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "addIndexes: process directory " + dir);
                        }
                        SegmentInfos sis = new SegmentInfos(); // read infos from dir
                        sis.Read(dir);
                        ISet<String> dsFilesCopied = new HashSet<String>();
                        IDictionary<String, String> dsNames = new HashMap<String, String>();
                        ISet<String> copiedFiles = new HashSet<String>();
                        foreach (SegmentInfoPerCommit info in sis)
                        {
                            //assert !infos.contains(info): "dup info dir=" + info.info.dir + " name=" + info.info.name;

                            String newSegName = NewSegmentName();

                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "addIndexes: process segment origName=" + info.info.name + " newName=" + newSegName + " info=" + info);
                            }

                            IOContext context = new IOContext(new MergeInfo(info.info.DocCount, info.SizeInBytes, true, -1));

                            foreach (FieldInfo fi in GetFieldInfos(info.info))
                            {
                                globalFieldNumberMap.AddOrGet(fi.name, fi.number, fi.DocValuesTypeValue.GetValueOrDefault());
                            }
                            infos.Add(CopySegmentAsIs(info, newSegName, dsNames, dsFilesCopied, context, copiedFiles));
                        }
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        foreach (SegmentInfoPerCommit sipc in infos)
                        {
                            foreach (String file in sipc.Files)
                            {
                                try
                                {
                                    directory.DeleteFile(file);
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }

                lock (this)
                {
                    success = false;
                    try
                    {
                        EnsureOpen();
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            foreach (SegmentInfoPerCommit sipc in infos)
                            {
                                foreach (String file in sipc.Files)
                                {
                                    try
                                    {
                                        directory.DeleteFile(file);
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }
                    }
                    segmentInfos.AddRange(infos);
                    Checkpoint();
                }

            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "addIndexes(Directory...)");
            }
        }

        public void AddIndexes(params IndexReader[] readers)
        {
            EnsureOpen();
            int numDocs = 0;

            try
            {
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "flush at addIndexes(IndexReader...)");
                }
                Flush(false, true);

                String mergedName = NewSegmentName();
                List<AtomicReader> mergeReaders = new List<AtomicReader>();
                foreach (IndexReader indexReader in readers)
                {
                    numDocs += indexReader.NumDocs;
                    foreach (AtomicReaderContext ctx in indexReader.Leaves)
                    {
                        mergeReaders.Add(ctx.Reader);
                    }
                }
                IOContext context = new IOContext(new MergeInfo(numDocs, -1, true, -1));

                // TODO: somehow we should fix this merge so it's
                // abortable so that IW.close(false) is able to stop it
                TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(directory);

                SegmentInfo info = new SegmentInfo(directory, Constants.LUCENE_MAIN_VERSION, mergedName, -1,
                                                   false, codec, null, null);

                SegmentMerger merger = new SegmentMerger(mergeReaders, info, infoStream, trackingDir, config.TermIndexInterval,
                                                         MergeState.CheckAbort.NONE, globalFieldNumberMap, context);

                MergeState mergeState;
                bool success = false;
                try
                {
                    mergeState = merger.Merge();                // merge 'em
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        lock (this)
                        {
                            deleter.Refresh(info.name);
                        }
                    }
                }

                SegmentInfoPerCommit infoPerCommit = new SegmentInfoPerCommit(info, 0, -1L);

                info.Files = new HashSet<String>(trackingDir.CreatedFiles);
                trackingDir.CreatedFiles.Clear();

                SetDiagnostics(info, SOURCE_ADDINDEXES_READERS);

                bool useCompoundFile;
                lock (this)
                { // Guard segmentInfos
                    if (stopMerges)
                    {
                        deleter.DeleteNewFiles(infoPerCommit.Files);
                        return;
                    }
                    EnsureOpen();
                    useCompoundFile = mergePolicy.UseCompoundFile(segmentInfos, infoPerCommit);
                }

                // Now create the compound file if needed
                if (useCompoundFile)
                {
                    ICollection<String> filesToDelete = infoPerCommit.Files;
                    try
                    {
                        CreateCompoundFile(infoStream, directory, MergeState.CheckAbort.NONE, info, context);
                    }
                    finally
                    {
                        // delete new non cfs files directly: they were never
                        // registered with IFD
                        lock (this)
                        {
                            deleter.DeleteNewFiles(filesToDelete);
                        }
                    }
                    info.UseCompoundFile = true;
                }

                // Have codec write SegmentInfo.  Must do this after
                // creating CFS so that 1) .si isn't slurped into CFS,
                // and 2) .si reflects useCompoundFile=true change
                // above:
                success = false;
                try
                {
                    codec.SegmentInfoFormat.SegmentInfoWriter.Write(trackingDir, info, mergeState.fieldInfos, context);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        lock (this)
                        {
                            deleter.Refresh(info.name);
                        }
                    }
                }

                info.AddFiles(trackingDir.CreatedFiles);

                // Register the new segment
                lock (this)
                {
                    if (stopMerges)
                    {
                        deleter.DeleteNewFiles(info.Files);
                        return;
                    }
                    EnsureOpen();
                    segmentInfos.Add(infoPerCommit);
                    Checkpoint();
                }
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "addIndexes(IndexReader...)");
            }
        }

        private SegmentInfoPerCommit CopySegmentAsIs(SegmentInfoPerCommit info, String segName,
                                               IDictionary<String, String> dsNames, ISet<String> dsFilesCopied, IOContext context,
                                               ISet<String> copiedFiles)
        {
            // Determine if the doc store of this segment needs to be copied. It's
            // only relevant for segments that share doc store with others,
            // because the DS might have been copied already, in which case we
            // just want to update the DS name of this SegmentInfo.
            String dsName = Lucene3xSegmentInfoFormat.GetDocStoreSegment(info.info);
            //assert dsName != null;
            String newDsName;
            if (dsNames.ContainsKey(dsName))
            {
                newDsName = dsNames[dsName];
            }
            else
            {
                dsNames[dsName] = segName;
                newDsName = segName;
            }

            // note: we don't really need this fis (its copied), but we load it up
            // so we don't pass a null value to the si writer
            FieldInfos fis = GetFieldInfos(info.info);

            ISet<String> docStoreFiles3xOnly = Lucene3xCodec.GetDocStoreFiles(info.info);

            IDictionary<String, String> attributes;
            // copy the attributes map, we might modify it below.
            // also we need to ensure its read-write, since we will invoke the SIwriter (which might want to set something).
            if (info.info.Attributes == null)
            {
                attributes = new HashMap<String, String>();
            }
            else
            {
                attributes = new HashMap<String, String>(info.info.Attributes);
            }
            if (docStoreFiles3xOnly != null)
            {
                // only violate the codec this way if it's preflex &
                // shares doc stores
                // change docStoreSegment to newDsName
                attributes[Lucene3xSegmentInfoFormat.DS_NAME_KEY] = newDsName;
            }

            //System.out.println("copy seg=" + info.info.name + " version=" + info.info.getVersion());
            // Same SI as before but we change directory, name and docStoreSegment:
            SegmentInfo newInfo = new SegmentInfo(directory, info.info.Version, segName, info.info.DocCount,
                                                  info.info.UseCompoundFile,
                                                  info.info.Codec, info.info.Diagnostics, attributes);
            SegmentInfoPerCommit newInfoPerCommit = new SegmentInfoPerCommit(newInfo, info.DelCount, info.DelGen);

            ISet<String> segFiles = new HashSet<String>();

            // Build up new segment's file names.  Must do this
            // before writing SegmentInfo:
            foreach (String file in info.Files)
            {
                String newFileName;
                if (docStoreFiles3xOnly != null && docStoreFiles3xOnly.Contains(file))
                {
                    newFileName = newDsName + Lucene.Net.Index.IndexFileNames.StripSegmentName(file);
                }
                else
                {
                    newFileName = segName + Lucene.Net.Index.IndexFileNames.StripSegmentName(file);
                }
                segFiles.Add(newFileName);
            }
            newInfo.Files = segFiles;

            // We must rewrite the SI file because it references
            // segment name (its own name, if its 3.x, and doc
            // store segment name):
            TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(directory);
            try
            {
                newInfo.Codec.SegmentInfoFormat.SegmentInfoWriter.Write(trackingDir, newInfo, fis, context);
            }
            catch (NotSupportedException uoe)
            {
                // OK: 3x codec cannot write a new SI file;
                // SegmentInfos will write this on commit
            }

            ICollection<String> siFiles = trackingDir.CreatedFiles;

            bool success = false;
            try
            {

                // Copy the segment's files
                foreach (String file in info.Files)
                {

                    String newFileName;
                    if (docStoreFiles3xOnly != null && docStoreFiles3xOnly.Contains(file))
                    {
                        newFileName = newDsName + Lucene.Net.Index.IndexFileNames.StripSegmentName(file);
                        if (dsFilesCopied.Contains(newFileName))
                        {
                            continue;
                        }
                        dsFilesCopied.Add(newFileName);
                    }
                    else
                    {
                        newFileName = segName + Lucene.Net.Index.IndexFileNames.StripSegmentName(file);
                    }

                    if (siFiles.Contains(newFileName))
                    {
                        // We already rewrote this above
                        continue;
                    }

                    //assert !directory.fileExists(newFileName): "file \"" + newFileName + "\" already exists; siFiles=" + siFiles;
                    //assert !copiedFiles.contains(file): "file \"" + file + "\" is being copied more than once";
                    copiedFiles.Add(file);
                    info.info.dir.Copy(directory, file, newFileName, context);
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    foreach (String file in newInfo.Files)
                    {
                        try
                        {
                            directory.DeleteFile(file);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return newInfoPerCommit;
        }

        protected virtual void DoAfterFlush()
        {
        }

        protected virtual void DoBeforeFlush()
        {
        }

        public void PrepareCommit()
        {
            EnsureOpen();
            PrepareCommitInternal();
        }

        private void PrepareCommitInternal()
        {
            lock (commitLock)
            {
                EnsureOpen(false);
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "prepareCommit: flush");
                    infoStream.Message("IW", "  index before flush " + SegString());
                }

                if (hitOOM)
                {
                    throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot commit");
                }

                if (pendingCommit != null)
                {
                    throw new InvalidOperationException("prepareCommit was already called with no corresponding call to commit");
                }

                DoBeforeFlush();
                //assert testPoint("startDoFlush");
                SegmentInfos toCommit = null;
                bool anySegmentsFlushed = false;

                // This is copied from doFlush, except it's modified to
                // clone & incRef the flushed SegmentInfos inside the
                // sync block:

                try
                {

                    lock (fullFlushLock)
                    {
                        bool flushSuccess = false;
                        bool success = false;
                        try
                        {
                            anySegmentsFlushed = docWriter.FlushAllThreads();
                            if (!anySegmentsFlushed)
                            {
                                // prevent double increment since docWriter#doFlush increments the flushcount
                                // if we flushed anything.
                                Interlocked.Increment(ref flushCount);
                            }
                            flushSuccess = true;

                            lock (this)
                            {
                                MaybeApplyDeletes(true);

                                readerPool.Commit(segmentInfos);

                                // Must clone the segmentInfos while we still
                                // hold fullFlushLock and while sync'd so that
                                // no partial changes (eg a delete w/o
                                // corresponding add from an updateDocument) can
                                // sneak into the commit point:
                                toCommit = (SegmentInfos)segmentInfos.Clone();

                                pendingCommitChangeCount = changeCount;

                                // This protects the segmentInfos we are now going
                                // to commit.  This is important in case, eg, while
                                // we are trying to sync all referenced files, a
                                // merge completes which would otherwise have
                                // removed the files we are now syncing.    
                                filesToCommit = toCommit.Files(directory, false);
                                deleter.IncRef(filesToCommit);
                            }
                            success = true;
                        }
                        finally
                        {
                            if (!success)
                            {
                                if (infoStream.IsEnabled("IW"))
                                {
                                    infoStream.Message("IW", "hit exception during prepareCommit");
                                }
                            }
                            // Done: finish the full flush!
                            docWriter.FinishFullFlush(flushSuccess);
                            DoAfterFlush();
                        }
                    }
                }
                catch (OutOfMemoryException oom)
                {
                    HandleOOM(oom, "prepareCommit");
                }

                bool success2 = false;
                try
                {
                    if (anySegmentsFlushed)
                    {
                        MaybeMerge(MergeTrigger.FULL_FLUSH, UNBOUNDED_MAX_MERGE_SEGMENTS);
                    }
                    success2 = true;
                }
                finally
                {
                    if (!success2)
                    {
                        lock (this)
                        {
                            deleter.DecRef(filesToCommit);
                            filesToCommit = null;
                        }
                    }
                }

                StartCommit(toCommit);
            }
        }

        public IDictionary<string, string> CommitData
        {
            get
            {
                lock (this)
                {
                    return segmentInfos.UserData;
                }
            }
            set
            {
                lock (this)
                {
                    segmentInfos.UserData = new HashMap<String, String>(value);
                    ++changeCount;
                }
            }
        }

        private readonly Object commitLock = new Object();

        public void Commit()
        {
            EnsureOpen();
            CommitInternal();
        }

        private void CommitInternal()
        {

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "commit: start");
            }

            lock (commitLock)
            {
                EnsureOpen(false);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "commit: enter lock");
                }

                if (pendingCommit == null)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "commit: now prepare");
                    }
                    PrepareCommitInternal();
                }
                else
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "commit: already prepared");
                    }
                }

                FinishCommit();
            }
        }

        private void FinishCommit()
        {
            lock (this)
            {

                if (pendingCommit != null)
                {
                    try
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "commit: pendingCommit != null");
                        }
                        pendingCommit.FinishCommit(directory);
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "commit: wrote segments file \"" + pendingCommit.SegmentsFileName + "\"");
                        }
                        lastCommitChangeCount = pendingCommitChangeCount;
                        segmentInfos.UpdateGeneration(pendingCommit);
                        rollbackSegments = pendingCommit.CreateBackupSegmentInfos();
                        deleter.Checkpoint(pendingCommit, true);
                    }
                    finally
                    {
                        // Matches the incRef done in prepareCommit:
                        deleter.DecRef(filesToCommit);
                        filesToCommit = null;
                        pendingCommit = null;
                        Monitor.PulseAll(this);
                    }

                }
                else
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "commit: pendingCommit == null; skip");
                    }
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "commit: done");
                }
            }
        }

        private readonly Object fullFlushLock = new Object();

        internal bool HoldsFullFlushLock
        {
            get
            {
                // .NET Port POTENTIALLY DANGEROUS: .NET 4 doesn't support knowing if the current thread has 
                // Monitor.Enter()'ed a lock. .NET 4.5 adds the Monitor.IsEntered method which would give us that
                // capability. Since this is marked as for testing only, this may suffice, although it may break
                // unit tests.
                return false;
                //Original java code: return Thread.holdsLock(fullFlushLock);
            }
        }

        protected void Flush(bool triggerMerge, bool applyAllDeletes)
        {

            // NOTE: this method cannot be sync'd because
            // maybeMerge() in turn calls mergeScheduler.merge which
            // in turn can take a long time to run and we don't want
            // to hold the lock for that.  In the case of
            // ConcurrentMergeScheduler this can lead to deadlock
            // when it stalls due to too many running merges.

            // We can be called during close, when closing==true, so we must pass false to ensureOpen:
            EnsureOpen(false);
            if (DoFlush(applyAllDeletes) && triggerMerge)
            {
                MaybeMerge(MergeTrigger.FULL_FLUSH, UNBOUNDED_MAX_MERGE_SEGMENTS);
            }
        }

        private bool DoFlush(bool applyAllDeletes)
        {
            if (hitOOM)
            {
                throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot flush");
            }

            DoBeforeFlush();
            //assert testPoint("startDoFlush");
            bool success = false;
            try
            {

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "  start flush: applyAllDeletes=" + applyAllDeletes);
                    infoStream.Message("IW", "  index before flush " + SegString());
                }
                bool anySegmentFlushed;

                lock (fullFlushLock)
                {
                    bool flushSuccess = false;
                    try
                    {
                        anySegmentFlushed = docWriter.FlushAllThreads();
                        flushSuccess = true;
                    }
                    finally
                    {
                        docWriter.FinishFullFlush(flushSuccess);
                    }
                }
                lock (this)
                {
                    MaybeApplyDeletes(applyAllDeletes);
                    DoAfterFlush();
                    if (!anySegmentFlushed)
                    {
                        // flushCount is incremented in flushAllThreads
                        Interlocked.Increment(ref flushCount);
                    }
                    success = true;
                    return anySegmentFlushed;
                }
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "doFlush");
                // never hit
                return false;
            }
            finally
            {
                if (!success)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "hit exception during flush");
                    }
                }
            }
        }

        internal void MaybeApplyDeletes(bool applyAllDeletes)
        {
            lock (this)
            {
                if (applyAllDeletes)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "apply all deletes during flush");
                    }
                    ApplyAllDeletes();
                }
                else if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "don't apply deletes now delTermCount=" + bufferedDeletesStream.NumTerms + " bytesUsed=" + bufferedDeletesStream.BytesUsed);
                }
            }
        }

        internal void ApplyAllDeletes()
        {
            lock (this)
            {
                Interlocked.Increment(ref flushDeletesCount);
                BufferedDeletesStream.ApplyDeletesResult result;
                result = bufferedDeletesStream.ApplyDeletes(readerPool, segmentInfos.ToList());
                if (result.anyDeletes)
                {
                    Checkpoint();
                }
                if (!keepFullyDeletedSegments && result.allDeleted != null)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "drop 100% deleted segments: " + SegString(result.allDeleted));
                    }
                    foreach (SegmentInfoPerCommit info in result.allDeleted)
                    {
                        // If a merge has already registered for this
                        // segment, we leave it in the readerPool; the
                        // merge will skip merging it and will then drop
                        // it once it's done:
                        if (!mergingSegments.Contains(info))
                        {
                            segmentInfos.Remove(info);
                            readerPool.Drop(info);
                        }
                    }
                    Checkpoint();
                }
                bufferedDeletesStream.Prune(segmentInfos);
            }
        }

        public long RamSizeInBytes
        {
            get
            {
                EnsureOpen();
                return docWriter.flushControl.NetBytes + bufferedDeletesStream.BytesUsed;
            }
        }

        // for testing only
        internal DocumentsWriter DocsWriter
        {
            get
            {
                bool test = false;
                //assert test = true;
                return test ? docWriter : null;
            }
        }

        public int NumRamDocs
        {
            get
            {
                lock (this)
                {
                    EnsureOpen();
                    return docWriter.NumDocs;
                }
            }
        }

        private void EnsureValidMerge(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                foreach (SegmentInfoPerCommit info in merge.segments)
                {
                    if (!segmentInfos.Contains(info))
                    {
                        throw new MergePolicy.MergeException("MergePolicy selected a segment (" + info.info.name + ") that is not in the current index " + SegString(), directory);
                    }
                }
            }
        }

        private ReadersAndLiveDocs CommitMergedDeletes(MergePolicy.OneMerge merge, MergeState mergeState)
        {
            lock (this)
            {
                //assert testPoint("startCommitMergeDeletes");

                IList<SegmentInfoPerCommit> sourceSegments = merge.segments;

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "commitMergeDeletes " + SegString(merge.segments));
                }

                // Carefully merge deletes that occurred after we
                // started merging:
                int docUpto = 0;
                long minGen = long.MaxValue;

                // Lazy init (only when we find a delete to carry over):
                ReadersAndLiveDocs mergedDeletes = null;
                MergePolicy.DocMap docMap = null;

                for (int i = 0; i < sourceSegments.Count; i++)
                {
                    SegmentInfoPerCommit info = sourceSegments[i];
                    minGen = Math.Min(info.BufferedDeletesGen, minGen);
                    int docCount = info.info.DocCount;
                    IBits prevLiveDocs = merge.readers[i].LiveDocs;
                    IBits currentLiveDocs;
                    ReadersAndLiveDocs rld = readerPool.Get(info, false);
                    // We hold a ref so it should still be in the pool:
                    //assert rld != null: "seg=" + info.info.name;
                    currentLiveDocs = rld.LiveDocs;

                    if (prevLiveDocs != null)
                    {

                        // If we had deletions on starting the merge we must
                        // still have deletions now:
                        //assert currentLiveDocs != null;
                        //assert prevLiveDocs.length() == docCount;
                        //assert currentLiveDocs.length() == docCount;

                        // There were deletes on this segment when the merge
                        // started.  The merge has collapsed away those
                        // deletes, but, if new deletes were flushed since
                        // the merge started, we must now carefully keep any
                        // newly flushed deletes but mapping them to the new
                        // docIDs.

                        // Since we copy-on-write, if any new deletes were
                        // applied after merging has started, we can just
                        // check if the before/after liveDocs have changed.
                        // If so, we must carefully merge the liveDocs one
                        // doc at a time:
                        if (currentLiveDocs != prevLiveDocs)
                        {

                            // This means this segment received new deletes
                            // since we started the merge, so we
                            // must merge them:
                            for (int j = 0; j < docCount; j++)
                            {
                                if (!prevLiveDocs[j])
                                {
                                    //assert !currentLiveDocs.get(j);
                                }
                                else
                                {
                                    if (!currentLiveDocs[j])
                                    {
                                        if (mergedDeletes == null)
                                        {
                                            mergedDeletes = readerPool.Get(merge.info, true);
                                            mergedDeletes.InitWritableLiveDocs();
                                            docMap = merge.GetDocMap(mergeState);
                                            //assert docMap.isConsistent(merge.info.info.getDocCount());
                                        }
                                        mergedDeletes.Delete(docMap.Map(docUpto));
                                    }
                                    docUpto++;
                                }
                            }
                        }
                        else
                        {
                            docUpto += info.info.DocCount - info.DelCount - rld.PendingDeleteCount;
                        }
                    }
                    else if (currentLiveDocs != null)
                    {
                        //assert currentLiveDocs.length() == docCount;
                        // This segment had no deletes before but now it
                        // does:
                        for (int j = 0; j < docCount; j++)
                        {
                            if (!currentLiveDocs[j])
                            {
                                if (mergedDeletes == null)
                                {
                                    mergedDeletes = readerPool.Get(merge.info, true);
                                    mergedDeletes.InitWritableLiveDocs();
                                    docMap = merge.GetDocMap(mergeState);
                                    //assert docMap.isConsistent(merge.info.info.getDocCount());
                                }
                                mergedDeletes.Delete(docMap.Map(docUpto));
                            }
                            docUpto++;
                        }
                    }
                    else
                    {
                        // No deletes before or after
                        docUpto += info.info.DocCount;
                    }
                }

                //assert docUpto == merge.info.info.getDocCount();

                if (infoStream.IsEnabled("IW"))
                {
                    if (mergedDeletes == null)
                    {
                        infoStream.Message("IW", "no new deletes since merge started");
                    }
                    else
                    {
                        infoStream.Message("IW", mergedDeletes.PendingDeleteCount + " new deletes since merge started");
                    }
                }

                merge.info.BufferedDeletesGen = minGen;

                return mergedDeletes;
            }
        }

        private bool CommitMerge(MergePolicy.OneMerge merge, MergeState mergeState)
        {
            lock (this)
            {
                //assert testPoint("startCommitMerge");

                if (hitOOM)
                {
                    throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot complete merge");
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "commitMerge: " + SegString(merge.segments) + " index=" + SegString());
                }

                //assert merge.registerDone;

                // If merge was explicitly aborted, or, if rollback() or
                // rollbackTransaction() had been called since our merge
                // started (which results in an unqualified
                // deleter.refresh() call that will remove any index
                // file that current segments does not reference), we
                // abort this merge
                if (merge.IsAborted)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "commitMerge: skip: it was aborted");
                    }
                    deleter.DeleteNewFiles(merge.info.Files);
                    return false;
                }

                ReadersAndLiveDocs mergedDeletes = merge.info.info.DocCount == 0 ? null : CommitMergedDeletes(merge, mergeState);

                //assert mergedDeletes == null || mergedDeletes.getPendingDeleteCount() != 0;

                // If the doc store we are using has been closed and
                // is in now compound format (but wasn't when we
                // started), then we will switch to the compound
                // format as well:

                //assert !segmentInfos.contains(merge.info);

                bool allDeleted = merge.segments.Count == 0 ||
                  merge.info.info.DocCount == 0 ||
                  (mergedDeletes != null &&
                   mergedDeletes.PendingDeleteCount == merge.info.info.DocCount);

                if (infoStream.IsEnabled("IW"))
                {
                    if (allDeleted)
                    {
                        infoStream.Message("IW", "merged segment " + merge.info + " is 100% deleted" + (keepFullyDeletedSegments ? "" : "; skipping insert"));
                    }
                }

                bool dropSegment = allDeleted && !keepFullyDeletedSegments;

                // If we merged no segments then we better be dropping
                // the new segment:
                //assert merge.segments.size() > 0 || dropSegment;

                //assert merge.info.info.getDocCount() != 0 || keepFullyDeletedSegments || dropSegment;

                segmentInfos.ApplyMergeChanges(merge, dropSegment);

                if (mergedDeletes != null)
                {
                    if (dropSegment)
                    {
                        mergedDeletes.DropChanges();
                    }
                    readerPool.Release(mergedDeletes);
                }

                if (dropSegment)
                {
                    //assert !segmentInfos.contains(merge.info);
                    readerPool.Drop(merge.info);
                    deleter.DeleteNewFiles(merge.info.Files);
                }

                bool success = false;
                try
                {
                    // Must close before checkpoint, otherwise IFD won't be
                    // able to delete the held-open files from the merge
                    // readers:
                    CloseMergeReaders(merge, false);
                    success = true;
                }
                finally
                {
                    // Must note the change to segmentInfos so any commits
                    // in-flight don't lose it (IFD will incRef/protect the
                    // new files we created):
                    if (success)
                    {
                        Checkpoint();
                    }
                    else
                    {
                        try
                        {
                            Checkpoint();
                        }
                        catch
                        {
                            // Ignore so we keep throwing original exception.
                        }
                    }
                }

                deleter.DeletePendingFiles();

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "after commitMerge: " + SegString());
                }

                if (merge.maxNumSegments != -1 && !dropSegment)
                {
                    // cascade the forceMerge:
                    if (!segmentsToMerge.ContainsKey(merge.info))
                    {
                        segmentsToMerge[merge.info] = false;
                    }
                }

                return true;
            }
        }

        private void HandleMergeException(Exception t, MergePolicy.OneMerge merge)
        {

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "handleMergeException: merge=" + SegString(merge.segments) + " exc=" + t);
            }

            // Set the exception on the merge, so if
            // forceMerge is waiting on us it sees the root
            // cause exception:
            merge.Exception = t;
            AddMergeException(merge);

            if (t is MergePolicy.MergeAbortedException)
            {
                // We can ignore this exception (it happens when
                // close(false) or rollback is called), unless the
                // merge involves segments from external directories,
                // in which case we must throw it so, for example, the
                // rollbackTransaction code in addIndexes* is
                // executed.
                if (merge.isExternal)
                    throw (MergePolicy.MergeAbortedException)t;
            }
            else if (t is IOException)
                throw (IOException)t;
            else if (t is SystemException)
                throw (SystemException)t;
            //else if (t is Error)
            //  throw (Error) t;
            else
                // Should not get here
                throw t;
        }

        public virtual void Merge(MergePolicy.OneMerge merge)
        {

            bool success = false;

            long t0 = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            try
            {
                try
                {
                    try
                    {
                        MergeInit(merge);
                        //if (merge.info != null) {
                        //System.out.println("MERGE: " + merge.info.info.name);
                        //}

                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "now merge\n  merge=" + SegString(merge.segments) + "\n  index=" + SegString());
                        }

                        MergeMiddle(merge);
                        MergeSuccess(merge);
                        success = true;
                    }
                    catch (Exception t)
                    {
                        HandleMergeException(t, merge);
                    }
                }
                finally
                {
                    lock (this)
                    {
                        MergeFinish(merge);

                        if (!success)
                        {
                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "hit exception during merge");
                            }
                            if (merge.info != null && !segmentInfos.Contains(merge.info))
                            {
                                deleter.Refresh(merge.info.info.name);
                            }
                        }

                        // This merge (and, generally, any change to the
                        // segments) may now enable new merges, so we call
                        // merge policy & update pending merges.
                        if (success && !merge.IsAborted && (merge.maxNumSegments != -1 || (!closed && !closing)))
                        {
                            UpdatePendingMerges(MergeTrigger.MERGE_FINISHED, merge.maxNumSegments);
                        }
                    }
                }
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "merge");
            }
            if (merge.info != null && !merge.IsAborted)
            {
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "merge time " + ((DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) - t0) + " msec for " + merge.info.info.DocCount + " docs");
                }
            }
        }

        internal virtual void MergeSuccess(MergePolicy.OneMerge merge)
        {
        }

        internal bool RegisterMerge(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                if (merge.registerDone)
                {
                    return true;
                }
                //assert merge.segments.size() > 0;

                if (stopMerges)
                {
                    merge.Abort();
                    throw new MergePolicy.MergeAbortedException("merge is aborted: " + SegString(merge.segments));
                }

                bool isExternal = false;
                foreach (SegmentInfoPerCommit info in merge.segments)
                {
                    if (mergingSegments.Contains(info))
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "reject merge " + SegString(merge.segments) + ": segment " + SegString(info) + " is already marked for merge");
                        }
                        return false;
                    }
                    if (!segmentInfos.Contains(info))
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "reject merge " + SegString(merge.segments) + ": segment " + SegString(info) + " does not exist in live infos");
                        }
                        return false;
                    }
                    if (info.info.dir != directory)
                    {
                        isExternal = true;
                    }
                    if (segmentsToMerge.ContainsKey(info))
                    {
                        merge.maxNumSegments = mergeMaxNumSegments;
                    }
                }

                EnsureValidMerge(merge);

                pendingMerges.AddLast(merge);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "add merge to pendingMerges: " + SegString(merge.segments) + " [total " + pendingMerges.Count + " pending]");
                }

                merge.mergeGen = mergeGen;
                merge.isExternal = isExternal;

                // OK it does not conflict; now record that this merge
                // is running (while synchronized) to avoid race
                // condition where two conflicting merges from different
                // threads, start
                if (infoStream.IsEnabled("IW"))
                {
                    StringBuilder builder = new StringBuilder("registerMerge merging= [");
                    foreach (SegmentInfoPerCommit info in mergingSegments)
                    {
                        builder.Append(info.info.name).Append(", ");
                    }
                    builder.Append("]");
                    // don't call mergingSegments.toString() could lead to ConcurrentModException
                    // since merge updates the segments FieldInfos
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", builder.ToString());
                    }
                }
                foreach (SegmentInfoPerCommit info in merge.segments)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "registerMerge info=" + SegString(info));
                    }
                    mergingSegments.Add(info);
                }

                //assert merge.estimatedMergeBytes == 0;
                //assert merge.totalMergeBytes == 0;
                foreach (SegmentInfoPerCommit info in merge.segments)
                {
                    if (info.info.DocCount > 0)
                    {
                        int delCount = NumDeletedDocs(info);
                        //assert delCount <= info.info.getDocCount();
                        double delRatio = ((double)delCount) / info.info.DocCount;
                        merge.estimatedMergeBytes += (long)(info.SizeInBytes * (1.0 - delRatio));
                        merge.totalMergeBytes += info.SizeInBytes;
                    }
                }

                // Merge is now registered
                merge.registerDone = true;

                return true;
            }
        }

        internal void MergeInit(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                bool success = false;
                try
                {
                    _MergeInit(merge);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "hit exception in mergeInit");
                        }
                        MergeFinish(merge);
                    }
                }
            }
        }

        private void _MergeInit(MergePolicy.OneMerge merge)
        {
            lock (this)
            {

                //assert testPoint("startMergeInit");

                //assert merge.registerDone;
                //assert merge.maxNumSegments == -1 || merge.maxNumSegments > 0;

                if (hitOOM)
                {
                    throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot merge");
                }

                if (merge.info != null)
                {
                    // mergeInit already done
                    return;
                }

                if (merge.IsAborted)
                {
                    return;
                }

                // TODO: in the non-pool'd case this is somewhat
                // wasteful, because we open these readers, close them,
                // and then open them again for merging.  Maybe  we
                // could pre-pool them somehow in that case...

                // Lock order: IW -> BD
                BufferedDeletesStream.ApplyDeletesResult result = bufferedDeletesStream.ApplyDeletes(readerPool, merge.segments);

                if (result.anyDeletes)
                {
                    Checkpoint();
                }

                if (!keepFullyDeletedSegments && result.allDeleted != null)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "drop 100% deleted segments: " + result.allDeleted);
                    }
                    foreach (SegmentInfoPerCommit info in result.allDeleted)
                    {
                        segmentInfos.Remove(info);
                        if (merge.segments.Contains(info))
                        {
                            mergingSegments.Remove(info);
                            merge.segments.Remove(info);
                        }
                        readerPool.Drop(info);
                    }
                    Checkpoint();
                }

                // Bind a new segment name here so even with
                // ConcurrentMergePolicy we keep deterministic segment
                // names.
                String mergeSegmentName = NewSegmentName();
                SegmentInfo si = new SegmentInfo(directory, Constants.LUCENE_MAIN_VERSION, mergeSegmentName, -1, false, codec, null, null);
                IDictionary<String, String> details = new HashMap<String, String>();
                details["mergeMaxNumSegments"] = "" + merge.maxNumSegments;
                details["mergeFactor"] = merge.segments.Count.ToString();
                SetDiagnostics(si, SOURCE_MERGE, details);
                merge.Info = new SegmentInfoPerCommit(si, 0, -1L);

                // Lock order: IW -> BD
                bufferedDeletesStream.Prune(segmentInfos);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "merge seg=" + merge.info.info.name + " " + SegString(merge.segments));
                }
            }
        }

        internal static void SetDiagnostics(SegmentInfo info, String source)
        {
            SetDiagnostics(info, source, null);
        }

        private static void SetDiagnostics(SegmentInfo info, String source, IDictionary<String, String> details)
        {
            IDictionary<String, String> diagnostics = new HashMap<String, String>();
            diagnostics["source"] = source;
            diagnostics["lucene.version"] = Constants.LUCENE_VERSION;
            diagnostics["os"] = Constants.OS_NAME;
            diagnostics["os.arch"] = Constants.OS_ARCH;
            diagnostics["os.version"] = Constants.OS_VERSION;
            diagnostics["java.version"] = Constants.JAVA_VERSION;
            diagnostics["java.vendor"] = Constants.JAVA_VENDOR;
            diagnostics["timestamp"] = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString();
            if (details != null)
            {
                foreach (var kvp in details)
                {
                    diagnostics[kvp.Key] = kvp.Value;
                }
            }
            info.Diagnostics = diagnostics;
        }

        internal void MergeFinish(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                // forceMerge, addIndexes or finishMerges may be waiting
                // on merges to finish.
                Monitor.PulseAll(this);

                // It's possible we are called twice, eg if there was an
                // exception inside mergeInit
                if (merge.registerDone)
                {
                    IList<SegmentInfoPerCommit> sourceSegments = merge.segments;
                    foreach (SegmentInfoPerCommit info in sourceSegments)
                    {
                        mergingSegments.Remove(info);
                    }
                    merge.registerDone = false;
                }

                runningMerges.Remove(merge);
            }
        }

        private void CloseMergeReaders(MergePolicy.OneMerge merge, bool suppressExceptions)
        {
            lock (this)
            {
                int numSegments = merge.readers.Count;
                Exception th = null;

                bool drop = !suppressExceptions;

                for (int i = 0; i < numSegments; i++)
                {
                    SegmentReader sr = merge.readers[i];
                    if (sr != null)
                    {
                        try
                        {
                            ReadersAndLiveDocs rld = readerPool.Get(sr.SegmentInfo, false);
                            // We still hold a ref so it should not have been removed:
                            //assert rld != null;
                            if (drop)
                            {
                                rld.DropChanges();
                            }
                            rld.Release(sr);
                            readerPool.Release(rld);
                            if (drop)
                            {
                                readerPool.Drop(rld.Info);
                            }
                        }
                        catch (Exception t)
                        {
                            if (th == null)
                            {
                                th = t;
                            }
                        }
                        merge.readers[i] = null;
                    }
                }

                // If any error occured, throw it.
                if (!suppressExceptions && th != null)
                {
                    if (th is IOException) throw (IOException)th;
                    if (th is SystemException) throw (SystemException)th;
                    //if (th is Error) throw (Error) th;
                    throw th;
                }
            }
        }

        private int MergeMiddle(MergePolicy.OneMerge merge)
        {

            merge.CheckAborted(directory);

            String mergedName = merge.info.info.name;

            IList<SegmentInfoPerCommit> sourceSegments = merge.segments;

            IOContext context = new IOContext(merge.MergeInfo);

            MergeState.CheckAbort checkAbort = new MergeState.CheckAbort(merge, directory);
            TrackingDirectoryWrapper dirWrapper = new TrackingDirectoryWrapper(directory);

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "merging " + SegString(merge.segments));
            }

            merge.readers = new List<SegmentReader>();

            // This is try/finally to make sure merger's readers are
            // closed:
            bool success = false;
            try
            {
                int segUpto = 0;
                while (segUpto < sourceSegments.Count)
                {

                    SegmentInfoPerCommit info = sourceSegments[segUpto];

                    // Hold onto the "live" reader; we will use this to
                    // commit merged deletes
                    ReadersAndLiveDocs rld = readerPool.Get(info, true);
                    SegmentReader reader = rld.GetMergeReader(context);
                    //assert reader != null;

                    // Carefully pull the most recent live docs:
                    IBits liveDocs;
                    int delCount;

                    lock (this)
                    {
                        // Must sync to ensure BufferedDeletesStream
                        // cannot change liveDocs/pendingDeleteCount while
                        // we pull a copy:
                        liveDocs = rld.ReadOnlyLiveDocs;
                        delCount = rld.PendingDeleteCount + info.DelCount;

                        //assert rld.verifyDocCounts();

                        if (infoStream.IsEnabled("IW"))
                        {
                            if (rld.PendingDeleteCount != 0)
                            {
                                infoStream.Message("IW", "seg=" + SegString(info) + " delCount=" + info.DelCount + " pendingDelCount=" + rld.PendingDeleteCount);
                            }
                            else if (info.DelCount != 0)
                            {
                                infoStream.Message("IW", "seg=" + SegString(info) + " delCount=" + info.DelCount);
                            }
                            else
                            {
                                infoStream.Message("IW", "seg=" + SegString(info) + " no deletes");
                            }
                        }
                    }

                    // Deletes might have happened after we pulled the merge reader and
                    // before we got a read-only copy of the segment's actual live docs
                    // (taking pending deletes into account). In that case we need to
                    // make a new reader with updated live docs and del count.
                    if (reader.NumDeletedDocs != delCount)
                    {
                        // fix the reader's live docs and del count
                        //assert delCount > reader.numDeletedDocs(); // beware of zombies

                        SegmentReader newReader = new SegmentReader(info, reader.core, liveDocs, info.info.DocCount - delCount);
                        bool released = false;
                        try
                        {
                            rld.Release(reader);
                            released = true;
                        }
                        finally
                        {
                            if (!released)
                            {
                                newReader.DecRef();
                            }
                        }

                        reader = newReader;
                    }

                    merge.readers.Add(reader);
                    //assert delCount <= info.info.getDocCount(): "delCount=" + delCount + " info.docCount=" + info.info.getDocCount() + " rld.pendingDeleteCount=" + rld.getPendingDeleteCount() + " info.getDelCount()=" + info.getDelCount();
                    segUpto++;
                }

                // we pass merge.getMergeReaders() instead of merge.readers to allow the
                // OneMerge to return a view over the actual segments to merge
                SegmentMerger merger = new SegmentMerger(merge.MergeReaders,
                    merge.info.info, infoStream, dirWrapper, config.TermIndexInterval,
                    checkAbort, globalFieldNumberMap, context);

                merge.CheckAborted(directory);

                // This is where all the work happens:
                MergeState mergeState;
                bool success3 = false;
                try
                {
                    mergeState = merger.Merge();
                    success3 = true;
                }
                finally
                {
                    if (!success3)
                    {
                        lock (this)
                        {
                            deleter.Refresh(merge.info.info.name);
                        }
                    }
                }
                //assert mergeState.segmentInfo == merge.info.info;
                merge.info.info.Files = new HashSet<String>(dirWrapper.CreatedFiles);

                // Record which codec was used to write the segment

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "merge codec=" + codec + " docCount=" + merge.info.info.DocCount + "; merged segment has " +
                                       (mergeState.fieldInfos.HasVectors ? "vectors" : "no vectors") + "; " +
                                       (mergeState.fieldInfos.HasNorms ? "norms" : "no norms") + "; " +
                                       (mergeState.fieldInfos.HasDocValues ? "docValues" : "no docValues") + "; " +
                                       (mergeState.fieldInfos.HasProx ? "prox" : "no prox") + "; " +
                                       (mergeState.fieldInfos.HasProx ? "freqs" : "no freqs"));
                }

                // Very important to do this before opening the reader
                // because codec must know if prox was written for
                // this segment:
                //System.out.println("merger set hasProx=" + merger.hasProx() + " seg=" + merge.info.name);
                bool useCompoundFile;
                lock (this)
                { // Guard segmentInfos
                    useCompoundFile = mergePolicy.UseCompoundFile(segmentInfos, merge.info);
                }

                if (useCompoundFile)
                {
                    success = false;

                    ICollection<String> filesToRemove = merge.info.Files;

                    try
                    {
                        filesToRemove = CreateCompoundFile(infoStream, directory, checkAbort, merge.info.info, context);
                        success = true;
                    }
                    catch (IOException ioe)
                    {
                        lock (this)
                        {
                            if (merge.IsAborted)
                            {
                                // This can happen if rollback or close(false)
                                // is called -- fall through to logic below to
                                // remove the partially created CFS:
                            }
                            else
                            {
                                HandleMergeException(ioe, merge);
                            }
                        }
                    }
                    catch (Exception t)
                    {
                        HandleMergeException(t, merge);
                    }
                    finally
                    {
                        if (!success)
                        {
                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "hit exception creating compound file during merge");
                            }

                            lock (this)
                            {
                                deleter.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(mergedName, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_EXTENSION));
                                deleter.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(mergedName, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION));
                                deleter.DeleteNewFiles(merge.info.Files);
                            }
                        }
                    }

                    // So that, if we hit exc in deleteNewFiles (next)
                    // or in commitMerge (later), we close the
                    // per-segment readers in the finally clause below:
                    success = false;

                    lock (this)
                    {

                        // delete new non cfs files directly: they were never
                        // registered with IFD
                        deleter.DeleteNewFiles(filesToRemove);

                        if (merge.IsAborted)
                        {
                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "abort merge after building CFS");
                            }
                            deleter.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(mergedName, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_EXTENSION));
                            deleter.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(mergedName, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION));
                            return 0;
                        }
                    }

                    merge.info.info.UseCompoundFile = true;
                }
                else
                {
                    // So that, if we hit exc in commitMerge (later),
                    // we close the per-segment readers in the finally
                    // clause below:
                    success = false;
                }

                // Have codec write SegmentInfo.  Must do this after
                // creating CFS so that 1) .si isn't slurped into CFS,
                // and 2) .si reflects useCompoundFile=true change
                // above:
                bool success2 = false;
                try
                {
                    codec.SegmentInfoFormat.SegmentInfoWriter.Write(directory, merge.info.info, mergeState.fieldInfos, context);
                    success2 = true;
                }
                finally
                {
                    if (!success2)
                    {
                        lock (this)
                        {
                            deleter.DeleteNewFiles(merge.info.Files);
                        }
                    }
                }

                // TODO: ideally we would freeze merge.info here!!
                // because any changes after writing the .si will be
                // lost... 

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", String.Format(CultureInfo.InvariantCulture, "merged segment size={0:0.00} MB vs estimate={1:0.00} MB", merge.info.SizeInBytes / 1024.0 / 1024.0, merge.estimatedMergeBytes / 1024 / 1024.0));
                }

                IndexReaderWarmer mergedSegmentWarmer = config.MergedSegmentWarmer;
                if (poolReaders && mergedSegmentWarmer != null && merge.info.info.DocCount != 0)
                {
                    ReadersAndLiveDocs rld = readerPool.Get(merge.info, true);
                    SegmentReader sr = rld.GetReader(IOContext.READ);
                    try
                    {
                        mergedSegmentWarmer.Warm(sr);
                    }
                    finally
                    {
                        lock (this)
                        {
                            rld.Release(sr);
                            readerPool.Release(rld);
                        }
                    }
                }

                // Force READ context because we merge deletes onto
                // this reader:
                if (!CommitMerge(merge, mergeState))
                {
                    // commitMerge will return false if this merge was aborted
                    return 0;
                }

                success = true;

            }
            finally
            {
                // Readers are already closed in commitMerge if we didn't hit
                // an exc:
                if (!success)
                {
                    CloseMergeReaders(merge, true);
                }
            }

            return merge.info.info.DocCount;
        }

        internal void AddMergeException(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                //assert merge.getException() != null;
                if (!mergeExceptions.Contains(merge) && mergeGen == merge.mergeGen)
                {
                    mergeExceptions.Add(merge);
                }
            }
        }

        // For test purposes.
        internal int BufferedDeleteTermsSize
        {
            get
            {
                return docWriter.BufferedDeleteTermsSize;
            }
        }

        // For test purposes.
        internal int NumBufferedDeleteTerms
        {
            get
            {
                return docWriter.NumBufferedDeleteTerms;
            }
        }

        // utility routines for tests
        internal SegmentInfoPerCommit NewestSegment
        {
            get
            {
                lock (this)
                {
                    return segmentInfos.Count > 0 ? segmentInfos.Info(segmentInfos.Count - 1) : null;
                }
            }
        }

        public string SegString()
        {
            return SegString(segmentInfos);
        }

        public string SegString(IEnumerable<SegmentInfoPerCommit> infos)
        {
            lock (this)
            {
                StringBuilder buffer = new StringBuilder();
                foreach (SegmentInfoPerCommit info in infos)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Append(' ');
                    }
                    buffer.Append(SegString(info));
                }
                return buffer.ToString();
            }
        }

        public string SegString(SegmentInfoPerCommit info)
        {
            lock (this)
            {
                return info.ToString(info.info.dir, NumDeletedDocs(info) - info.DelCount);
            }
        }

        private void DoWait()
        {
            lock (this)
            {
                // NOTE: the callers of this method should in theory
                // be able to do simply wait(), but, as a defense
                // against thread timing hazards where notifyAll()
                // fails to be called, we wait for at most 1 second
                // and then return so caller can check if wait
                // conditions are satisfied:
                try
                {
                    Monitor.Wait(this, 1000);
                }
                catch (ThreadInterruptedException)
                {
                    throw;
                }
            }
        }

        private bool keepFullyDeletedSegments;

        internal void KeepFullyDeletedSegments()
        {
            keepFullyDeletedSegments = true;
        }

        internal bool GetKeepFullyDeletedSegments()
        {
            return keepFullyDeletedSegments;
        }

        private bool FilesExist(SegmentInfos toSync)
        {
            ICollection<String> files = toSync.Files(directory, false);
            foreach (String fileName in files)
            {
                //assert directory.fileExists(fileName): "file " + fileName + " does not exist";
                // If this trips it means we are missing a call to
                // .checkpoint somewhere, because by the time we
                // are called, deleter should know about every
                // file referenced by the current head
                // segmentInfos:
                //assert deleter.exists(fileName): "IndexFileDeleter doesn't know about file " + fileName;
            }
            return true;
        }

        internal SegmentInfos ToLiveInfos(SegmentInfos sis)
        {
            lock (this)
            {
                SegmentInfos newSIS = new SegmentInfos();
                IDictionary<SegmentInfoPerCommit, SegmentInfoPerCommit> liveSIS = new HashMap<SegmentInfoPerCommit, SegmentInfoPerCommit>();
                foreach (SegmentInfoPerCommit info in segmentInfos)
                {
                    liveSIS[info] = info;
                }
                foreach (SegmentInfoPerCommit info in sis)
                {
                    SegmentInfoPerCommit infoWritable = info;
                    SegmentInfoPerCommit liveInfo = liveSIS[info];
                    if (liveInfo != null)
                    {
                        infoWritable = liveInfo;
                    }
                    newSIS.Add(infoWritable);
                }

                return newSIS;
            }
        }

        private void StartCommit(SegmentInfos toSync)
        {

            //assert testPoint("startStartCommit");
            //assert pendingCommit == null;

            if (hitOOM)
            {
                throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot commit");
            }

            try
            {

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "startCommit(): start");
                }

                lock (this)
                {

                    //assert lastCommitChangeCount <= changeCount: "lastCommitChangeCount=" + lastCommitChangeCount + " changeCount=" + changeCount;

                    if (pendingCommitChangeCount == lastCommitChangeCount)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "  skip startCommit(): no changes pending");
                        }
                        deleter.DecRef(filesToCommit);
                        filesToCommit = null;
                        return;
                    }

                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "startCommit index=" + SegString(ToLiveInfos(toSync)) + " changeCount=" + changeCount);
                    }

                    //assert filesExist(toSync);
                }

                //assert testPoint("midStartCommit");

                bool pendingCommitSet = false;

                try
                {

                    //assert testPoint("midStartCommit2");

                    lock (this)
                    {

                        //assert pendingCommit == null;

                        //assert segmentInfos.getGeneration() == toSync.getGeneration();

                        // Exception here means nothing is prepared
                        // (this method unwinds everything it did on
                        // an exception)
                        toSync.PrepareCommit(directory);
                        //System.out.println("DONE prepareCommit");

                        pendingCommitSet = true;
                        pendingCommit = toSync;
                    }

                    // This call can take a long time -- 10s of seconds
                    // or more.  We do it without syncing on this:
                    bool success = false;
                    ICollection<String> filesToSync;
                    try
                    {
                        filesToSync = toSync.Files(directory, false);
                        directory.Sync(filesToSync);
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            pendingCommitSet = false;
                            pendingCommit = null;
                            toSync.RollbackCommit(directory);
                        }
                    }

                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "done all syncs: " + filesToSync);
                    }

                    //assert testPoint("midStartCommitSuccess");

                }
                finally
                {
                    lock (this)
                    {
                        // Have our master segmentInfos record the
                        // generations we just prepared.  We do this
                        // on error or success so we don't
                        // double-write a segments_N file.
                        segmentInfos.UpdateGeneration(toSync);

                        if (!pendingCommitSet)
                        {
                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "hit exception committing segments file");
                            }

                            // Hit exception
                            deleter.DecRef(filesToCommit);
                            filesToCommit = null;
                        }
                    }
                }
            }
            catch (OutOfMemoryException oom)
            {
                HandleOOM(oom, "startCommit");
            }
            //assert testPoint("finishStartCommit");
        }

        public static bool IsLocked(Directory directory)
        {
            return directory.MakeLock(WRITE_LOCK_NAME).IsLocked();
        }

        public static void Unlock(Directory directory)
        {
            directory.MakeLock(IndexWriter.WRITE_LOCK_NAME).Release();
        }

        public abstract class IndexReaderWarmer
        {
            protected IndexReaderWarmer()
            {
            }

            public abstract void Warm(AtomicReader reader);
        }

        private void HandleOOM(OutOfMemoryException oom, String location)
        {
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "hit OutOfMemoryError inside " + location);
            }
            hitOOM = true;
            throw oom;
        }

        internal virtual bool TestPoint(String name)
        {
            return true;
        }

        internal bool NrtIsCurrent(SegmentInfos infos)
        {
            lock (this)
            {
                //System.out.println("IW.nrtIsCurrent " + (infos.version == segmentInfos.version && !docWriter.anyChanges() && !bufferedDeletesStream.any()));
                EnsureOpen();
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "nrtIsCurrent: infoVersion matches: " + (infos.Version == segmentInfos.Version) + "; DW changes: " + docWriter.AnyChanges + "; BD changes: " + bufferedDeletesStream.Any());
                }
                return infos.Version == segmentInfos.Version && !docWriter.AnyChanges && !bufferedDeletesStream.Any();
            }
        }

        internal bool IsClosed
        {
            get
            {
                lock (this)
                {
                    return closed;
                }
            }
        }

        public void DeleteUnusedFiles()
        {
            lock (this)
            {
                EnsureOpen(false);
                deleter.DeletePendingFiles();
                deleter.RevisitPolicy();
            }
        }

        internal void DeletePendingFiles()
        {
            lock (this)
            {
                deleter.DeletePendingFiles();
            }
        }

        internal static ICollection<String> CreateCompoundFile(InfoStream infoStream, Directory directory, MergeState.CheckAbort checkAbort, SegmentInfo info, IOContext context)
        {
            String fileName = Lucene.Net.Index.IndexFileNames.SegmentFileName(info.name, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_EXTENSION);
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "create compound file " + fileName);
            }
            //assert Lucene3xSegmentInfoFormat.getDocStoreOffset(info) == -1;
            // Now merge all added files
            ICollection<String> files = info.Files;
            CompoundFileDirectory cfsDir = new CompoundFileDirectory(directory, fileName, context, true);
            IOException prior = null;
            try
            {
                foreach (String file in files)
                {
                    directory.Copy(cfsDir, file, file, context);
                    checkAbort.Work(directory.FileLength(file));
                }
            }
            catch (IOException ex)
            {
                prior = ex;
            }
            finally
            {
                bool success = false;
                try
                {
                    IOUtils.CloseWhileHandlingException(prior, cfsDir);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        try
                        {
                            directory.DeleteFile(fileName);
                        }
                        catch
                        {
                        }
                        try
                        {
                            directory.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(info.name, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION));
                        }
                        catch
                        {
                        }
                    }
                }
            }

            // Replace all previous files with the CFS/CFE files:
            ISet<String> siFiles = new HashSet<String>();
            siFiles.Add(fileName);
            siFiles.Add(Lucene.Net.Index.IndexFileNames.SegmentFileName(info.name, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION));
            info.Files = siFiles;

            return files;
        }

        internal void DeleteNewFiles(ICollection<String> files)
        {
            lock (this)
            {
                deleter.DeleteNewFiles(files);
            }
        }

        internal void FlushFailed(SegmentInfo info)
        {
            lock (this)
            {
                deleter.Refresh(info.name);
            }
        }
    }
}