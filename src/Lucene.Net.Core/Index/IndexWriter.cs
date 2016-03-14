using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.IO;
    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;

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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CheckAbort = Lucene.Net.Index.MergeState.CheckAbort;
    using Codec = Lucene.Net.Codecs.Codec;
    using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
    using Constants = Lucene.Net.Util.Constants;
    using Directory = Lucene.Net.Store.Directory;
    using DocValuesType_e = Lucene.Net.Index.FieldInfo.DocValuesType_e;
    using FieldNumbers = Lucene.Net.Index.FieldInfos.FieldNumbers;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using Lock = Lucene.Net.Store.Lock;
    using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
    using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;
    using Lucene3xSegmentInfoFormat = Lucene.Net.Codecs.Lucene3x.Lucene3xSegmentInfoFormat;
    using MergeInfo = Lucene.Net.Store.MergeInfo;
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using Query = Lucene.Net.Search.Query;
    using TrackingDirectoryWrapper = Lucene.Net.Store.TrackingDirectoryWrapper;

    /// <summary>
    ///  An <code>IndexWriter</code> creates and maintains an index.
    ///
    ///  <p>The <seealso cref="OpenMode"/> option on
    ///  <seealso cref="IndexWriterConfig#setOpenMode(OpenMode)"/> determines
    ///  whether a new index is created, or whether an existing index is
    ///  opened. Note that you can open an index with <seealso cref="OpenMode#CREATE"/>
    ///  even while readers are using the index. The old readers will
    ///  continue to search the "point in time" snapshot they had opened,
    ///  and won't see the newly created index until they re-open. If
    ///  <seealso cref="OpenMode#CREATE_OR_APPEND"/> is used IndexWriter will create a
    ///  new index if there is not already an index at the provided path
    ///  and otherwise open the existing index.</p>
    ///
    ///  <p>In either case, documents are added with {@link #addDocument(Iterable)
    ///  addDocument} and removed with <seealso cref="#deleteDocuments(Term)"/> or {@link
    ///  #deleteDocuments(Query)}. A document can be updated with {@link
    ///  #updateDocument(Term, Iterable) updateDocument} (which just deletes
    ///  and then adds the entire document). When finished adding, deleting
    ///  and updating documents, <seealso cref="#close() close"/> should be called.</p>
    ///
    ///  <a name="flush"></a>
    ///  <p>These changes are buffered in memory and periodically
    ///  flushed to the <seealso cref="Directory"/> (during the above method
    ///  calls). A flush is triggered when there are enough added documents
    ///  since the last flush. Flushing is triggered either by RAM usage of the
    ///  documents (see <seealso cref="IndexWriterConfig#setRAMBufferSizeMB"/>) or the
    ///  number of added documents (see <seealso cref="IndexWriterConfig#setMaxBufferedDocs(int)"/>).
    ///  The default is to flush when RAM usage hits
    ///  <seealso cref="IndexWriterConfig#DEFAULT_RAM_BUFFER_SIZE_MB"/> MB. For
    ///  best indexing speed you should flush by RAM usage with a
    ///  large RAM buffer. Additionally, if IndexWriter reaches the configured number of
    ///  buffered deletes (see <seealso cref="IndexWriterConfig#setMaxBufferedDeleteTerms"/>)
    ///  the deleted terms and queries are flushed and applied to existing segments.
    ///  In contrast to the other flush options <seealso cref="IndexWriterConfig#setRAMBufferSizeMB"/> and
    ///  <seealso cref="IndexWriterConfig#setMaxBufferedDocs(int)"/>, deleted terms
    ///  won't trigger a segment flush. Note that flushing just moves the
    ///  internal buffered state in IndexWriter into the index, but
    ///  these changes are not visible to IndexReader until either
    ///  <seealso cref="#commit()"/> or <seealso cref="#close"/> is called.  A flush may
    ///  also trigger one or more segment merges which by default
    ///  run with a background thread so as not to block the
    ///  addDocument calls (see <a href="#mergePolicy">below</a>
    ///  for changing the <seealso cref="mergeScheduler"/>).</p>
    ///
    ///  <p>Opening an <code>IndexWriter</code> creates a lock file for the directory in use. Trying to open
    ///  another <code>IndexWriter</code> on the same directory will lead to a
    ///  <seealso cref="LockObtainFailedException"/>. The <seealso cref="LockObtainFailedException"/>
    ///  is also thrown if an IndexReader on the same directory is used to delete documents
    ///  from the index.</p>
    ///
    ///  <a name="deletionPolicy"></a>
    ///  <p>Expert: <code>IndexWriter</code> allows an optional
    ///  <seealso cref="IndexDeletionPolicy"/> implementation to be
    ///  specified.  You can use this to control when prior commits
    ///  are deleted from the index.  The default policy is {@link
    ///  KeepOnlyLastCommitDeletionPolicy} which removes all prior
    ///  commits as soon as a new commit is done (this matches
    ///  behavior before 2.2).  Creating your own policy can allow
    ///  you to explicitly keep previous "point in time" commits
    ///  alive in the index for some time, to allow readers to
    ///  refresh to the new commit without having the old commit
    ///  deleted out from under them.  this is necessary on
    ///  filesystems like NFS that do not support "delete on last
    ///  close" semantics, which Lucene's "point in time" search
    ///  normally relies on. </p>
    ///
    ///  <a name="mergePolicy"></a> <p>Expert:
    ///  <code>IndexWriter</code> allows you to separately change
    ///  the <seealso cref="mergePolicy"/> and the <seealso cref="mergeScheduler"/>.
    ///  The <seealso cref="mergePolicy"/> is invoked whenever there are
    ///  changes to the segments in the index.  Its role is to
    ///  select which merges to do, if any, and return a {@link
    ///  MergePolicy.MergeSpecification} describing the merges.
    ///  The default is <seealso cref="LogByteSizeMergePolicy"/>.  Then, the {@link
    ///  MergeScheduler} is invoked with the requested merges and
    ///  it decides when and how to run the merges.  The default is
    ///  <seealso cref="ConcurrentMergeScheduler"/>. </p>
    ///
    ///  <a name="OOME"></a><p><b>NOTE</b>: if you hit an
    ///  OutOfMemoryError then IndexWriter will quietly record this
    ///  fact and block all future segment commits.  this is a
    ///  defensive measure in case any internal state (buffered
    ///  documents and deletions) were corrupted.  Any subsequent
    ///  calls to <seealso cref="#commit()"/> will throw an
    ///  InvalidOperationException.  The only course of action is to
    ///  call <seealso cref="#close()"/>, which internally will call {@link
    ///  #rollback()}, to undo any changes to the index since the
    ///  last commit.  You can also just call <seealso cref="#rollback()"/>
    ///  directly.</p>
    ///
    ///  <a name="thread-safety"></a><p><b>NOTE</b>: {@link
    ///  IndexWriter} instances are completely thread
    ///  safe, meaning multiple threads can call any of its
    ///  methods, concurrently.  If your application requires
    ///  external synchronization, you should <b>not</b>
    ///  synchronize on the <code>IndexWriter</code> instance as
    ///  this may cause deadlock; use your own (non-Lucene) objects
    ///  instead. </p>
    ///
    ///  <p><b>NOTE</b>: If you call
    ///  <code>Thread.interrupt()</code> on a thread that's within
    ///  IndexWriter, IndexWriter will try to catch this (eg, if
    ///  it's in a wait() or Thread.sleep()), and will then throw
    ///  the unchecked exception <seealso cref="ThreadInterruptedException"/>
    ///  and <b>clear</b> the interrupt status on the thread.</p>
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
     * becomes the new "front" of the index. this allows the IndexFileDeleter
     * to delete files that are referenced only by stale checkpoints.
     * (files that were created since the last commit, but are no longer
     * referenced by the "front" of the index). For this, IndexFileDeleter
     * keeps track of the last non commit checkpoint.
     */

    public class IndexWriter : IDisposable, TwoPhaseCommit
    {
        private bool InstanceFieldsInitialized = false;

        private void InitializeInstanceFields()
        {
            readerPool = new ReaderPool(this);
        }

        private const int UNBOUNDED_MAX_MERGE_SEGMENTS = -1;

        /// <summary>
        /// Name of the write lock in the index.
        /// </summary>
        public const string WRITE_LOCK_NAME = "write.lock";

        /// <summary>
        /// Key for the source of a segment in the <seealso cref="SegmentInfo#getDiagnostics() diagnostics"/>. </summary>
        public const string SOURCE = "source";

        /// <summary>
        /// Source of a segment which results from a merge of other segments. </summary>
        public const string SOURCE_MERGE = "merge";

        /// <summary>
        /// Source of a segment which results from a flush. </summary>
        public const string SOURCE_FLUSH = "flush";

        /// <summary>
        /// Source of a segment which results from a call to <seealso cref="#addIndexes(IndexReader...)"/>. </summary>
        public const string SOURCE_ADDINDEXES_READERS = "addIndexes(IndexReader...)";

        /// <summary>
        /// Absolute hard maximum length for a term, in bytes once
        /// encoded as UTF8.  If a term arrives from the analyzer
        /// longer than this length, an
        /// <code>IllegalArgumentException</code>  is thrown
        /// and a message is printed to infoStream, if set (see {@link
        /// IndexWriterConfig#setInfoStream(InfoStream)}).
        /// </summary>
        public static readonly int MAX_TERM_LENGTH = DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8;

        volatile private bool HitOOM;

        private readonly Directory directory; // where this index resides
        private readonly Analyzer analyzer; // how to analyze text

        private long ChangeCount; // increments every time a change is completed
        private long LastCommitChangeCount; // last changeCount that was committed

        private IList<SegmentCommitInfo> RollbackSegments; // list of segmentInfo we will fallback to if the commit fails

        internal volatile SegmentInfos PendingCommit; // set when a commit is pending (after prepareCommit() & before commit())
        internal long PendingCommitChangeCount;

        private ICollection<string> FilesToCommit;

        internal readonly SegmentInfos segmentInfos; // the segments
        internal readonly FieldNumbers GlobalFieldNumberMap;

        private readonly DocumentsWriter DocWriter;
        private readonly ConcurrentQueue<Event> eventQueue;
        internal readonly IndexFileDeleter Deleter;

        // used by forceMerge to note those needing merging
        private readonly IDictionary<SegmentCommitInfo, bool?> SegmentsToMerge = new Dictionary<SegmentCommitInfo, bool?>();

        private int MergeMaxNumSegments;

        private Lock WriteLock;

        private volatile bool closed;
        private volatile bool Closing;

        // Holds all SegmentInfo instances currently involved in
        // merges
        private readonly HashSet<SegmentCommitInfo> mergingSegments = new HashSet<SegmentCommitInfo>();

        private readonly MergePolicy mergePolicy;
        private readonly MergeScheduler mergeScheduler;
        private readonly LinkedList<MergePolicy.OneMerge> PendingMerges = new LinkedList<MergePolicy.OneMerge>();
        private readonly HashSet<MergePolicy.OneMerge> RunningMerges = new HashSet<MergePolicy.OneMerge>();
        private IList<MergePolicy.OneMerge> MergeExceptions = new List<MergePolicy.OneMerge>();
        private long MergeGen;
        private bool StopMerges;

        internal readonly AtomicInteger flushCount = new AtomicInteger();
        internal readonly AtomicInteger flushDeletesCount = new AtomicInteger();

        internal ReaderPool readerPool;
        internal readonly BufferedUpdatesStream BufferedUpdatesStream;

        // this is a "write once" variable (like the organic dye
        // on a DVD-R that may or may not be heated by a laser and
        // then cooled to permanently record the event): it's
        // false, until getReader() is called for the first time,
        // at which point it's switched to true and never changes
        // back to false.  Once this is true, we hold open and
        // reuse SegmentReader instances internally for applying
        // deletes, doing merges, and reopening near real-time
        // readers.
        private volatile bool PoolReaders;

        // The instance that was passed to the constructor. It is saved only in order
        // to allow users to query an IndexWriter settings.
        private readonly LiveIndexWriterConfig Config_Renamed;

        public virtual DirectoryReader Reader
        {
            get
            {
                return GetReader(true);
            }
        }

        //For unit tests
        public bool BufferedUpdatesStreamAny
        {
            get { return BufferedUpdatesStream.Any(); }
        }

        public int GetSegmentInfosSize_Nunit()
        {
            return segmentInfos.Size();
        }

        /// <summary>
        /// Expert: returns a readonly reader, covering all
        /// committed as well as un-committed changes to the index.
        /// this provides "near real-time" searching, in that
        /// changes made during an IndexWriter session can be
        /// quickly made available for searching without closing
        /// the writer nor calling <seealso cref="#commit"/>.
        ///
        /// <p>Note that this is functionally equivalent to calling
        /// {#flush} and then opening a new reader.  But the turnaround time of this
        /// method should be faster since it avoids the potentially
        /// costly <seealso cref="#commit"/>.</p>
        ///
        /// <p>You must close the <seealso cref="IndexReader"/> returned by
        /// this method once you are done using it.</p>
        ///
        /// <p>It's <i>near</i> real-time because there is no hard
        /// guarantee on how quickly you can get a new reader after
        /// making changes with IndexWriter.  You'll have to
        /// experiment in your situation to determine if it's
        /// fast enough.  As this is a new and experimental
        /// feature, please report back on your findings so we can
        /// learn, improve and iterate.</p>
        ///
        /// <p>The resulting reader supports {@link
        /// DirectoryReader#openIfChanged}, but that call will simply forward
        /// back to this method (though this may change in the
        /// future).</p>
        ///
        /// <p>The very first time this method is called, this
        /// writer instance will make every effort to pool the
        /// readers that it opens for doing merges, applying
        /// deletes, etc.  this means additional resources (RAM,
        /// file descriptors, CPU time) will be consumed.</p>
        ///
        /// <p>For lower latency on reopening a reader, you should
        /// call <seealso cref="IndexWriterConfig#setMergedSegmentWarmer"/> to
        /// pre-warm a newly merged segment before it's committed
        /// to the index.  this is important for minimizing
        /// index-to-search delay after a large merge.  </p>
        ///
        /// <p>If an addIndexes* call is running in another thread,
        /// then this reader will only search those segments from
        /// the foreign index that have been successfully copied
        /// over, so far</p>.
        ///
        /// <p><b>NOTE</b>: Once the writer is closed, any
        /// outstanding readers may continue to be used.  However,
        /// if you attempt to reopen any of those readers, you'll
        /// hit an <seealso cref="AlreadyClosedException"/>.</p>
        ///
        /// @lucene.experimental
        /// </summary>
        /// <returns> IndexReader that covers entire index plus all
        /// changes made so far by this IndexWriter instance
        /// </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        public virtual DirectoryReader GetReader(bool applyAllDeletes)
        {
            EnsureOpen();

            long tStart = Environment.TickCount;

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "flush at getReader");
            }
            // Do this up front before flushing so that the readers
            // obtained during this flush are pooled, the first time
            // this method is called:
            PoolReaders = true;
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
                lock (FullFlushLock)
                {
                    bool success = false;
                    try
                    {
                        anySegmentFlushed = DocWriter.FlushAllThreads(this);
                        if (!anySegmentFlushed)
                        {
                            // prevent double increment since docWriter#doFlush increments the flushcount
                            // if we flushed anything.
                            flushCount.IncrementAndGet();
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
                    catch (System.OutOfMemoryException oom)
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
                        DocWriter.FinishFullFlush(success);
                        ProcessEvents(false, true);
                        DoAfterFlush();
                    }
                }
                if (anySegmentFlushed)
                {
                    MaybeMerge(MergeTrigger.FULL_FLUSH, UNBOUNDED_MAX_MERGE_SEGMENTS);
                }
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "getReader took " + (Environment.TickCount - tStart) + " msec");
                }
                success2 = true;
            }
            finally
            {
                if (!success2)
                {
                    IOUtils.CloseWhileHandlingException(r);
                }
            }
            return r;
        }

        /// <summary>
        /// Holds shared SegmentReader instances. IndexWriter uses
        ///  SegmentReaders for 1) applying deletes, 2) doing
        ///  merges, 3) handing out a real-time reader.  this pool
        ///  reuses instances of the SegmentReaders in all these
        ///  places if it is in "near real-time mode" (getReader()
        ///  has been called on this instance).
        /// </summary>
        public class ReaderPool : IDisposable
        {
            private readonly IndexWriter OuterInstance;

            public ReaderPool(IndexWriter outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            internal readonly IDictionary<SegmentCommitInfo, ReadersAndUpdates> ReaderMap = new Dictionary<SegmentCommitInfo, ReadersAndUpdates>();

            // used only by asserts
            public virtual bool InfoIsLive(SegmentCommitInfo info)
            {
                lock (this)
                {
                    int idx = OuterInstance.segmentInfos.IndexOf(info);
                    Debug.Assert(idx != -1, "info=" + info + " isn't live");
                    Debug.Assert(OuterInstance.segmentInfos.Info(idx) == info, "info=" + info + " doesn't match live info in segmentInfos");
                    return true;
                }
            }

            public virtual void Drop(SegmentCommitInfo info)
            {
                lock (this)
                {
                    ReadersAndUpdates rld;
                    ReaderMap.TryGetValue(info, out rld);
                    if (rld != null)
                    {
                        Debug.Assert(info == rld.Info);
                        //        System.out.println("[" + Thread.currentThread().getName() + "] ReaderPool.drop: " + info);
                        ReaderMap.Remove(info);
                        rld.DropReaders();
                    }
                }
            }

            public virtual bool AnyPendingDeletes()
            {
                lock (this)
                {
                    foreach (ReadersAndUpdates rld in ReaderMap.Values)
                    {
                        if (rld.PendingDeleteCount != 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            public virtual void Release(ReadersAndUpdates rld)
            {
                lock (this)
                {
                    Release(rld, true);
                }
            }

            public virtual void Release(ReadersAndUpdates rld, bool assertInfoLive)
            {
                lock (this)
                {
                    // Matches incRef in get:
                    rld.DecRef();

                    // Pool still holds a ref:
                    Debug.Assert(rld.RefCount() >= 1);

                    if (!OuterInstance.PoolReaders && rld.RefCount() == 1)
                    {
                        // this is the last ref to this RLD, and we're not
                        // pooling, so remove it:
                        //        System.out.println("[" + Thread.currentThread().getName() + "] ReaderPool.release: " + rld.info);
                        if (rld.WriteLiveDocs(OuterInstance.directory))
                        {
                            // Make sure we only write del docs for a live segment:
                            Debug.Assert(assertInfoLive == false || InfoIsLive(rld.Info));
                            // Must checkpoint because we just
                            // created new _X_N.del and field updates files;
                            // don't call IW.checkpoint because that also
                            // increments SIS.version, which we do not want to
                            // do here: it was done previously (after we
                            // invoked BDS.applyDeletes), whereas here all we
                            // did was move the state to disk:
                            OuterInstance.CheckpointNoSIS();
                        }
                        //System.out.println("IW: done writeLiveDocs for info=" + rld.info);

                        //        System.out.println("[" + Thread.currentThread().getName() + "] ReaderPool.release: drop readers " + rld.info);
                        rld.DropReaders();
                        ReaderMap.Remove(rld.Info);
                    }
                }
            }

            public void Dispose()
            {
                DropAll(false);
            }

            /// <summary>
            /// Remove all our references to readers, and commits
            ///  any pending changes.
            /// </summary>
            internal virtual void DropAll(bool doSave)
            {
                lock (this)
                {
                    Exception priorE = null;
                    IEnumerator<KeyValuePair<SegmentCommitInfo, ReadersAndUpdates>> it = ReaderMap.GetEnumerator();

                    //Using outer try-catch to avoid deleting as iterating to avoid item corruption. Whether or not
                    //an exception is encountered in the outer while-loop, the ReaderMap will always be Clear()ed out
                    try
                    {
                        while (it.MoveNext())
                        {
                            ReadersAndUpdates rld = it.Current.Value;

                            try
                            {
                                if (doSave && rld.WriteLiveDocs(OuterInstance.directory))
                                {
                                    // Make sure we only write del docs and field updates for a live segment:
                                    Debug.Assert(InfoIsLive(rld.Info));
                                    // Must checkpoint because we just
                                    // created new _X_N.del and field updates files;
                                    // don't call IW.checkpoint because that also
                                    // increments SIS.version, which we do not want to
                                    // do here: it was done previously (after we
                                    // invoked BDS.applyDeletes), whereas here all we
                                    // did was move the state to disk:
                                    OuterInstance.CheckpointNoSIS();
                                }
                            }
                            catch (Exception t)
                            {
                                if (doSave)
                                {
                                    IOUtils.ReThrow(t);
                                }
                                else if (priorE == null)
                                {
                                    priorE = t;
                                }
                            }

                            // Important to remove as-we-go, not with .clear()
                            // in the end, in case we hit an exception;
                            // otherwise we could over-decref if close() is
                            // called again:
                            //ReaderMap.Remove(it.Current);

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
                                if (doSave)
                                {
                                    IOUtils.ReThrow(t);
                                }
                                else if (priorE == null)
                                {
                                    priorE = t;
                                }
                            }
                        }
                    }
                    catch (Exception disruption)
                    {
                    }
                    finally
                    {
                        ReaderMap.Clear();
                    }
                    Debug.Assert(ReaderMap.Count == 0);
                    IOUtils.ReThrow(priorE);
                }
            }

            /// <summary>
            /// Commit live docs changes for the segment readers for
            /// the provided infos.
            /// </summary>
            /// <exception cref="IOException"> If there is a low-level I/O error </exception>
            public virtual void Commit(SegmentInfos infos)
            {
                lock (this)
                {
                    foreach (SegmentCommitInfo info in infos.Segments)
                    {
                        ReadersAndUpdates rld;
                        if (ReaderMap.TryGetValue(info, out rld))
                        {
                            Debug.Assert(rld.Info == info);
                            if (rld.WriteLiveDocs(OuterInstance.directory))
                            {
                                // Make sure we only write del docs for a live segment:
                                Debug.Assert(InfoIsLive(info));
                                // Must checkpoint because we just
                                // created new _X_N.del and field updates files;
                                // don't call IW.checkpoint because that also
                                // increments SIS.version, which we do not want to
                                // do here: it was done previously (after we
                                // invoked BDS.applyDeletes), whereas here all we
                                // did was move the state to disk:
                                OuterInstance.CheckpointNoSIS();
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Obtain a ReadersAndLiveDocs instance from the
            /// readerPool.  If create is true, you must later call
            /// <seealso cref="#release(ReadersAndUpdates)"/>.
            /// </summary>
            public virtual ReadersAndUpdates Get(SegmentCommitInfo info, bool create)
            {
                lock (this)
                {
                    Debug.Assert(info.Info.Dir == OuterInstance.directory, "info.dir=" + info.Info.Dir + " vs " + OuterInstance.directory);

                    ReadersAndUpdates rld;
                    ReaderMap.TryGetValue(info, out rld);
                    if (rld == null)
                    {
                        if (!create)
                        {
                            return null;
                        }
                        rld = new ReadersAndUpdates(OuterInstance, info);
                        // Steal initial reference:
                        ReaderMap[info] = rld;
                    }
                    else
                    {
                        Debug.Assert(rld.Info == info, "Infos are not equal");//, "rld.info=" + rld.Info + " info=" + info + " isLive?=" + InfoIsLive(rld.Info) + " vs " + InfoIsLive(info));
                    }

                    if (create)
                    {
                        // Return ref to caller:
                        rld.IncRef();
                    }

                    Debug.Assert(NoDups());

                    return rld;
                }
            }

            // Make sure that every segment appears only once in the
            // pool:
            internal virtual bool NoDups()
            {
                HashSet<string> seen = new HashSet<string>();
                foreach (SegmentCommitInfo info in ReaderMap.Keys)
                {
                    Debug.Assert(!seen.Contains(info.Info.Name));
                    seen.Add(info.Info.Name);
                }
                return true;
            }
        }

        /// <summary>
        /// Obtain the number of deleted docs for a pooled reader.
        /// If the reader isn't being pooled, the segmentInfo's
        /// delCount is returned.
        /// </summary>
        public virtual int NumDeletedDocs(SegmentCommitInfo info)
        {
            EnsureOpen(false);
            int delCount = info.DelCount;

            ReadersAndUpdates rld = readerPool.Get(info, false);
            if (rld != null)
            {
                delCount += rld.PendingDeleteCount;
            }
            return delCount;
        }

        /// <summary>
        /// Used internally to throw an <seealso cref="AlreadyClosedException"/> if this
        /// IndexWriter has been closed or is in the process of closing.
        /// </summary>
        /// <param name="failIfClosing">
        ///          if true, also fail when {@code IndexWriter} is in the process of
        ///          closing ({@code closing=true}) but not yet done closing (
        ///          {@code closed=false}) </param>
        /// <exception cref="AlreadyClosedException">
        ///           if this IndexWriter is closed or in the process of closing </exception>
        protected internal void EnsureOpen(bool failIfClosing)
        {
            if (closed || (failIfClosing && Closing))
            {
                throw new AlreadyClosedException("this IndexWriter is closed");
            }
        }

        /// <summary>
        /// Used internally to throw an {@link
        /// AlreadyClosedException} if this IndexWriter has been
        /// closed ({@code closed=true}) or is in the process of
        /// closing ({@code closing=true}).
        /// <p>
        /// Calls <seealso cref="#ensureOpen(boolean) ensureOpen(true)"/>. </summary>
        /// <exception cref="AlreadyClosedException"> if this IndexWriter is closed </exception>
        protected internal void EnsureOpen()
        {
            EnsureOpen(true);
        }

        internal readonly Codec Codec; // for writing new segments

        /// <summary>
        /// Constructs a new IndexWriter per the settings given in <code>conf</code>.
        /// If you want to make "live" changes to this writer instance, use
        /// <seealso cref="#getConfig()"/>.
        ///
        /// <p>
        /// <b>NOTE:</b> after ths writer is created, the given configuration instance
        /// cannot be passed to another writer. If you intend to do so, you should
        /// <seealso cref="IndexWriterConfig#clone() clone"/> it beforehand.
        /// </summary>
        /// <param name="d">
        ///          the index directory. The index is either created or appended
        ///          according <code>conf.getOpenMode()</code>. </param>
        /// <param name="conf">
        ///          the configuration settings according to which IndexWriter should
        ///          be initialized. </param>
        /// <exception cref="IOException">
        ///           if the directory cannot be read/written to, or if it does not
        ///           exist and <code>conf.getOpenMode()</code> is
        ///           <code>OpenMode.APPEND</code> or if there is any other low-level
        ///           IO error </exception>
        public IndexWriter(Directory d, IndexWriterConfig conf)
        {
            /*if (!InstanceFieldsInitialized)
            {
                InitializeInstanceFields();
                InstanceFieldsInitialized = true;
            }*/
            readerPool = new ReaderPool(this);
            conf.SetIndexWriter(this); // prevent reuse by other instances
            Config_Renamed = new LiveIndexWriterConfig(conf);
            directory = d;
            analyzer = Config_Renamed.Analyzer;
            infoStream = Config_Renamed.InfoStream;
            mergePolicy = Config_Renamed.MergePolicy;
            mergePolicy.IndexWriter = this;
            mergeScheduler = Config_Renamed.MergeScheduler;
            Codec = Config_Renamed.Codec;

            BufferedUpdatesStream = new BufferedUpdatesStream(infoStream);
            PoolReaders = Config_Renamed.ReaderPooling;

            WriteLock = directory.MakeLock(WRITE_LOCK_NAME);

            if (!WriteLock.Obtain(Config_Renamed.WriteLockTimeout)) // obtain write lock
            {
                throw new LockObtainFailedException("Index locked for write: " + WriteLock);
            }

            bool success = false;
            try
            {
                OpenMode_e? mode = Config_Renamed.OpenMode;
                bool create;
                if (mode == OpenMode_e.CREATE)
                {
                    create = true;
                }
                else if (mode == OpenMode_e.APPEND)
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
                    // Try to read first.  this is to allow create
                    // against an index that's currently open for
                    // searching.  In this case we write the next
                    // segments_N file with no segments:
                    try
                    {
                        segmentInfos.Read(directory);
                        segmentInfos.Clear();
                    }
                    catch (IOException)
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

                    IndexCommit commit = Config_Renamed.IndexCommit;
                    if (commit != null)
                    {
                        // Swap out all segments, but, keep metadata in
                        // SegmentInfos, like version & generation, to
                        // preserve write-once.  this is important if
                        // readers are open against the future commit
                        // points.
                        if (commit.Directory != directory)
                        {
                            throw new ArgumentException(string.Format("IndexCommit's directory doesn't match my directory (mine: {0}, commit's: {1})", directory, commit.Directory));
                        }
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

                RollbackSegments = segmentInfos.CreateBackupSegmentInfos();

                // start with previous field numbers, but new FieldInfos
                GlobalFieldNumberMap = FieldNumberMap;
                Config_Renamed.FlushPolicy.Init(Config_Renamed);
                DocWriter = new DocumentsWriter(this, Config_Renamed, directory);
                eventQueue = DocWriter.EventQueue();

                // Default deleter (for backwards compatibility) is
                // KeepOnlyLastCommitDeleter:
                lock (this)
                {
                    Deleter = new IndexFileDeleter(directory, Config_Renamed.DelPolicy, segmentInfos, infoStream, this, initialIndexExists);
                }

                if (Deleter.StartingCommitDeleted)
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
                    WriteLock.Release();
                    IOUtils.CloseWhileHandlingException(WriteLock);
                    WriteLock = null;
                }
            }
        }

        /// <summary>
        /// Loads or returns the already loaded the global field number map for this <seealso cref="segmentInfos"/>.
        /// If this <seealso cref="segmentInfos"/> has no global field number map the returned instance is empty
        /// </summary>
        private FieldNumbers FieldNumberMap
        {
            get
            {
                FieldNumbers map = new FieldNumbers();

                foreach (SegmentCommitInfo info in segmentInfos.Segments)
                {
                    foreach (FieldInfo fi in SegmentReader.ReadFieldInfos(info))
                    {
                        map.AddOrGet(fi.Name, fi.Number, fi.DocValuesType);
                    }
                }

                return map;
            }
        }

        /// <summary>
        /// Returns a <seealso cref="LiveIndexWriterConfig"/>, which can be used to query the IndexWriter
        /// current settings, as well as modify "live" ones.
        /// </summary>
        public virtual LiveIndexWriterConfig Config
        {
            get
            {
                EnsureOpen(false);
                return Config_Renamed;
            }
        }

        private void MessageState()
        {
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "\ndir=" + directory + "\n" + "index=" + SegString() + "\n" + "version=" + Constants.LUCENE_VERSION + "\n" + Config_Renamed.ToString());
            }
        }

        /// <summary>
        /// Commits all changes to an index, waits for pending merges
        /// to complete, and closes all associated files.
        /// <p>
        /// this is a "slow graceful shutdown" which may take a long time
        /// especially if a big merge is pending: If you only want to close
        /// resources use <seealso cref="#rollback()"/>. If you only want to commit
        /// pending changes and close resources see <seealso cref="#close(boolean)"/>.
        /// <p>
        /// Note that this may be a costly
        /// operation, so, try to re-use a single writer instead of
        /// closing and opening a new one.  See <seealso cref="#commit()"/> for
        /// caveats about write caching done by some IO devices.
        ///
        /// <p> If an Exception is hit during close, eg due to disk
        /// full or some other reason, then both the on-disk index
        /// and the internal state of the IndexWriter instance will
        /// be consistent.  However, the close will not be complete
        /// even though part of it (flushing buffered documents)
        /// may have succeeded, so the write lock will still be
        /// held.</p>
        ///
        /// <p> If you can correct the underlying cause (eg free up
        /// some disk space) then you can call close() again.
        /// Failing that, if you want to force the write lock to be
        /// released (dangerous, because you may then lose buffered
        /// docs in the IndexWriter instance) then you can do
        /// something like this:</p>
        ///
        /// <pre class="prettyprint">
        /// try {
        ///   writer.Dispose();
        /// } finally {
        ///   if (IndexWriter.isLocked(directory)) {
        ///     IndexWriter.unlock(directory);
        ///   }
        /// }
        /// </pre>
        ///
        /// after which, you must be certain not to use the writer
        /// instance anymore.</p>
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer, again.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Closes the index with or without waiting for currently
        /// running merges to finish.  this is only meaningful when
        /// using a MergeScheduler that runs merges in background
        /// threads.
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer, again.  See <a
        /// href="#OOME">above</a> for details.</p>
        ///
        /// <p><b>NOTE</b>: it is dangerous to always call
        /// close(false), especially when IndexWriter is not open
        /// for very long, because this can result in "merge
        /// starvation" whereby long merges will never have a
        /// chance to finish.  this will cause too many segments in
        /// your index over time.</p>
        /// </summary>
        /// <param name="waitForMerges"> if true, this call will block
        /// until all merges complete; else, it will ask all
        /// running merges to abort, wait until those merges have
        /// finished (which should be at most a few seconds), and
        /// then return. </param>
        public virtual void Dispose(bool waitForMerges)
        {
            // Ensure that only one thread actually gets to do the
            // closing, and make sure no commit is also in progress:
            lock (CommitLock)
            {
                if (ShouldClose())
                {
                    // If any methods have hit OutOfMemoryError, then abort
                    // on close, in case the internal state of IndexWriter
                    // or DocumentsWriter is corrupt
                    if (HitOOM)
                    {
                        RollbackInternal();
                    }
                    else
                    {
                        CloseInternal(waitForMerges, true);
                        Debug.Assert(AssertEventQueueAfterClose());
                    }
                }
            }
        }

        private bool AssertEventQueueAfterClose()
        {
            if (eventQueue.Count == 0)
            {
                return true;
            }
            foreach (Event e in eventQueue)
            {
                Debug.Assert(e is DocumentsWriter.MergePendingEvent, e.ToString());
            }
            return true;
        }

        // Returns true if this thread should attempt to close, or
        // false if IndexWriter is now closed; else, waits until
        // another thread finishes closing
        private bool ShouldClose()
        {
            lock (this)
            {
                while (true)
                {
                    if (!closed)
                    {
                        if (!Closing)
                        {
                            Closing = true;
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
                if (PendingCommit != null)
                {
                    throw new InvalidOperationException("cannot close: prepareCommit was already called with no corresponding call to commit");
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "now flush at close waitForMerges=" + waitForMerges);
                }

                DocWriter.Dispose();

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
                        DocWriter.Abort(this); // already closed -- never sync on IW
                    }
                }
                finally
                {
                    try
                    {
                        // clean up merge scheduler in all cases, although flushing may have failed:
                        //interrupted = Thread.Interrupted();
                        //LUCENE TO-DO
                        interrupted = false;

                        //TODO: conniey
                        //if (waitForMerges)
                        //{
                        //    try
                        //    {
                        //        // Give merge scheduler last chance to run, in case
                        //        // any pending merges are waiting:
                        //        mergeScheduler.Merge(this, MergeTrigger.CLOSING, false);
                        //    }
                        //    catch (ThreadInterruptedException)
                        //    {
                        //        // ignore any interruption, does not matter
                        //        interrupted = true;
                        //        if (infoStream.IsEnabled("IW"))
                        //        {
                        //            infoStream.Message("IW", "interrupted while waiting for final merges");
                        //        }
                        //    }
                        //}

                        lock (this)
                        {
                            for (; ; )
                            {
                                //TODO: conniey
                                //try
                                //{
                                //    FinishMerges(waitForMerges && !interrupted);
                                //    break;
                                //}
                                //catch (ThreadInterruptedException)
                                //{
                                //    // by setting the interrupted status, the
                                //    // next call to finishMerges will pass false,
                                //    // so it will not wait
                                //    interrupted = true;
                                //    if (infoStream.IsEnabled("IW"))
                                //    {
                                //        infoStream.Message("IW", "interrupted while waiting for merges to finish");
                                //    }
                                //}
                            }
                            StopMerges = true;
                        }
                    }
                    finally
                    {
                        // shutdown policy, scheduler and all threads (this call is not interruptible):
                        IOUtils.CloseWhileHandlingException(mergePolicy, mergeScheduler);
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
                ProcessEvents(false, true);
                lock (this)
                {
                    // commitInternal calls ReaderPool.commit, which
                    // writes any pending liveDocs from ReaderPool, so
                    // it's safe to drop all readers now:
                    readerPool.DropAll(true);
                    Deleter.Dispose();
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "at close: " + SegString());
                }

                if (WriteLock != null)
                {
                    WriteLock.Release(); // release write lock
                    WriteLock.Dispose();
                    WriteLock = null;
                }
                lock (this)
                {
                    closed = true;
                }
                Debug.Assert(DocWriter.PerThreadPool.NumDeactivatedThreadStates() == DocWriter.PerThreadPool.MaxThreadStates, "" + DocWriter.PerThreadPool.NumDeactivatedThreadStates() + " " + DocWriter.PerThreadPool.MaxThreadStates);
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "closeInternal");
            }
            finally
            {
                lock (this)
                {
                    Closing = false;
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
                if (interrupted)
                {
                    //TODO: conniey
                    //Thread.CurrentThread.Interrupt();
                }
            }
        }

        /// <summary>
        /// Returns the Directory used by this index. </summary>
        public virtual Directory Directory
        {
            get
            {
                return directory;
            }
        }

        /// <summary>
        /// Returns the analyzer used by this index. </summary>
        public virtual Analyzer Analyzer
        {
            get
            {
                EnsureOpen();
                return analyzer;
            }
        }

        /// <summary>
        /// Returns total number of docs in this index, including
        ///  docs not yet flushed (still in the RAM buffer),
        ///  not counting deletions. </summary>
        ///  <seealso> cref= #numDocs  </seealso>
        public virtual int MaxDoc
        {
            get
            {
                lock (this)
                {
                    EnsureOpen();
                    return DocWriter.NumDocs + segmentInfos.TotalDocCount();
                }
            }
        }

        /// <summary>
        /// Returns total number of docs in this index, including
        ///  docs not yet flushed (still in the RAM buffer), and
        ///  including deletions.  <b>NOTE:</b> buffered deletions
        ///  are not counted.  If you really need these to be
        ///  counted you should call <seealso cref="#commit()"/> first. </summary>
        ///  <seealso> cref= #numDocs  </seealso>
        public virtual int NumDocs()
        {
            lock (this)
            {
                EnsureOpen();
                return DocWriter.NumDocs + segmentInfos.Segments.Sum(info => info.Info.DocCount - NumDeletedDocs(info));
            }
        }

        /// <summary>
        /// Returns true if this index has deletions (including
        /// buffered deletions).  Note that this will return true
        /// if there are buffered Term/Query deletions, even if it
        /// turns out those buffered deletions don't match any
        /// documents.
        /// </summary>
        public virtual bool HasDeletions()
        {
            lock (this)
            {
                EnsureOpen();
                if (BufferedUpdatesStream.Any())
                {
                    return true;
                }
                if (DocWriter.AnyDeletions())
                {
                    return true;
                }
                if (readerPool.AnyPendingDeletes())
                {
                    return true;
                }
                foreach (SegmentCommitInfo info in segmentInfos.Segments)
                {
                    if (info.HasDeletions())
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Adds a document to this index.
        ///
        /// <p> Note that if an Exception is hit (for example disk full)
        /// then the index will be consistent, but this document
        /// may not have been added.  Furthermore, it's possible
        /// the index will have one segment in non-compound format
        /// even when using compound files (when a merge has
        /// partially succeeded).</p>
        ///
        /// <p> this method periodically flushes pending documents
        /// to the Directory (see <a href="#flush">above</a>), and
        /// also periodically triggers segment merges in the index
        /// according to the <seealso cref="MergePolicy"/> in use.</p>
        ///
        /// <p>Merges temporarily consume space in the
        /// directory. The amount of space required is up to 1X the
        /// size of all segments being merged, when no
        /// readers/searchers are open against the index, and up to
        /// 2X the size of all segments being merged when
        /// readers/searchers are open against the index (see
        /// <seealso cref="#forceMerge(int)"/> for details). The sequence of
        /// primitive merge operations performed is governed by the
        /// merge policy.
        ///
        /// <p>Note that each term in the document can be no longer
        /// than <seealso cref="#MAX_TERM_LENGTH"/> in bytes, otherwise an
        /// IllegalArgumentException will be thrown.</p>
        ///
        /// <p>Note that it's possible to create an invalid Unicode
        /// string in java if a UTF16 surrogate pair is malformed.
        /// In this case, the invalid characters are silently
        /// replaced with the Unicode replacement character
        /// U+FFFD.</p>
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void AddDocument(IEnumerable<IndexableField> doc)
        {
            AddDocument(doc, analyzer);
        }

        /// <summary>
        /// Adds a document to this index, using the provided analyzer instead of the
        /// value of <seealso cref="#getAnalyzer()"/>.
        ///
        /// <p>See <seealso cref="#addDocument(Iterable)"/> for details on
        /// index and IndexWriter state after an Exception, and
        /// flushing/merging temporary free space requirements.</p>
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void AddDocument(IEnumerable<IndexableField> doc, Analyzer analyzer)
        {
            UpdateDocument(null, doc, analyzer);
        }

        /// <summary>
        /// Atomically adds a block of documents with sequentially
        /// assigned document IDs, such that an external reader
        /// will see all or none of the documents.
        ///
        /// <p><b>WARNING</b>: the index does not currently record
        /// which documents were added as a block.  Today this is
        /// fine, because merging will preserve a block. The order of
        /// documents within a segment will be preserved, even when child
        /// documents within a block are deleted. Most search features
        /// (like result grouping and block joining) require you to
        /// mark documents; when these documents are deleted these
        /// search features will not work as expected. Obviously adding
        /// documents to an existing block will require you the reindex
        /// the entire block.
        ///
        /// <p>However it's possible that in the future Lucene may
        /// merge more aggressively re-order documents (for example,
        /// perhaps to obtain better index compression), in which case
        /// you may need to fully re-index your documents at that time.
        ///
        /// <p>See <seealso cref="#addDocument(Iterable)"/> for details on
        /// index and IndexWriter state after an Exception, and
        /// flushing/merging temporary free space requirements.</p>
        ///
        /// <p><b>NOTE</b>: tools that do offline splitting of an index
        /// (for example, IndexSplitter in contrib) or
        /// re-sorting of documents (for example, IndexSorter in
        /// contrib) are not aware of these atomically added documents
        /// and will likely break them up.  Use such tools at your
        /// own risk!
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error
        ///
        /// @lucene.experimental </exception>
        public virtual void AddDocuments(IEnumerable<IEnumerable<IndexableField>> docs)
        {
            AddDocuments(docs, analyzer);
        }

        /// <summary>
        /// Atomically adds a block of documents, analyzed using the
        /// provided analyzer, with sequentially assigned document
        /// IDs, such that an external reader will see all or none
        /// of the documents.
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error
        ///
        /// @lucene.experimental </exception>
        public virtual void AddDocuments(IEnumerable<IEnumerable<IndexableField>> docs, Analyzer analyzer)
        {
            UpdateDocuments(null, docs, analyzer);
        }

        /// <summary>
        /// Atomically deletes documents matching the provided
        /// delTerm and adds a block of documents with sequentially
        /// assigned document IDs, such that an external reader
        /// will see all or none of the documents.
        ///
        /// See <seealso cref="#addDocuments(Iterable)"/>.
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error
        ///
        /// @lucene.experimental </exception>
        public virtual void UpdateDocuments(Term delTerm, IEnumerable<IEnumerable<IndexableField>> docs)
        {
            UpdateDocuments(delTerm, docs, analyzer);
        }

        /// <summary>
        /// Atomically deletes documents matching the provided
        /// delTerm and adds a block of documents, analyzed  using
        /// the provided analyzer, with sequentially
        /// assigned document IDs, such that an external reader
        /// will see all or none of the documents.
        ///
        /// See <seealso cref="#addDocuments(Iterable)"/>.
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error
        ///
        /// @lucene.experimental </exception>
        public virtual void UpdateDocuments(Term delTerm, IEnumerable<IEnumerable<IndexableField>> docs, Analyzer analyzer)
        {
            EnsureOpen();
            try
            {
                bool success = false;
                try
                {
                    if (DocWriter.UpdateDocuments(docs, analyzer, delTerm))
                    {
                        ProcessEvents(true, false);
                    }
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
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "updateDocuments");
            }
        }

        /// <summary>
        /// Deletes the document(s) containing <code>term</code>.
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        /// <param name="term"> the term to identify the documents to be deleted </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void DeleteDocuments(Term term)
        {
            EnsureOpen();
            try
            {
                if (DocWriter.DeleteTerms(term))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "deleteDocuments(Term)");
            }
        }

        /// <summary>
        /// Expert: attempts to delete by document ID, as long as
        ///  the provided reader is a near-real-time reader (from {@link
        ///  DirectoryReader#open(IndexWriter,boolean)}).  If the
        ///  provided reader is an NRT reader obtained from this
        ///  writer, and its segment has not been merged away, then
        ///  the delete succeeds and this method returns true; else, it
        ///  returns false the caller must then separately delete by
        ///  Term or Query.
        ///
        ///  <b>NOTE</b>: this method can only delete documents
        ///  visible to the currently open NRT reader.  If you need
        ///  to delete documents indexed after opening the NRT
        ///  reader you must use the other deleteDocument methods
        ///  (e.g., <seealso cref="#deleteDocuments(Term)"/>).
        /// </summary>
        public virtual bool TryDeleteDocument(IndexReader readerIn, int docID)
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
                    reader = leaves[subIndex].AtomicReader;
                    docID -= leaves[subIndex].DocBase;
                    Debug.Assert(docID >= 0);
                    Debug.Assert(docID < reader.MaxDoc);
                }

                if (!(reader is SegmentReader))
                {
                    throw new System.ArgumentException("the reader must be a SegmentReader or composite reader containing only SegmentReaders");
                }

                SegmentCommitInfo info = ((SegmentReader)reader).SegmentInfo;

                // TODO: this is a slow linear search, but, number of
                // segments should be contained unless something is
                // seriously wrong w/ the index, so it should be a minor
                // cost:

                if (segmentInfos.IndexOf(info) != -1)
                {
                    ReadersAndUpdates rld = readerPool.Get(info, false);
                    if (rld != null)
                    {
                        lock (BufferedUpdatesStream)
                        {
                            rld.InitWritableLiveDocs();
                            if (rld.Delete(docID))
                            {
                                int fullDelCount = rld.Info.DelCount + rld.PendingDeleteCount;
                                if (fullDelCount == rld.Info.Info.DocCount)
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

        /// <summary>
        /// Deletes the document(s) containing any of the
        /// terms. All given deletes are applied and flushed atomically
        /// at the same time.
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        /// <param name="terms"> array of terms to identify the documents
        /// to be deleted </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void DeleteDocuments(params Term[] terms)
        {
            EnsureOpen();
            try
            {
                if (DocWriter.DeleteTerms(terms))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "deleteDocuments(Term..)");
            }
        }

        /// <summary>
        /// Deletes the document(s) matching the provided query.
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        /// <param name="query"> the query to identify the documents to be deleted </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void DeleteDocuments(Query query)
        {
            EnsureOpen();
            try
            {
                if (DocWriter.DeleteQueries(query))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "deleteDocuments(Query)");
            }
        }

        /// <summary>
        /// Deletes the document(s) matching any of the provided queries.
        /// All given deletes are applied and flushed atomically at the same time.
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        /// <param name="queries"> array of queries to identify the documents
        /// to be deleted </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void DeleteDocuments(params Query[] queries)
        {
            EnsureOpen();
            try
            {
                if (DocWriter.DeleteQueries(queries))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "deleteDocuments(Query..)");
            }
        }

        /// <summary>
        /// Updates a document by first deleting the document(s)
        /// containing <code>term</code> and then adding the new
        /// document.  The delete and then add are atomic as seen
        /// by a reader on the same index (flush may happen only after
        /// the add).
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        /// <param name="term"> the term to identify the document(s) to be
        /// deleted </param>
        /// <param name="doc"> the document to be added </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void UpdateDocument(Term term, IEnumerable<IndexableField> doc)
        {
            EnsureOpen();
            UpdateDocument(term, doc, analyzer);
        }

        /// <summary>
        /// Updates a document by first deleting the document(s)
        /// containing <code>term</code> and then adding the new
        /// document.  The delete and then add are atomic as seen
        /// by a reader on the same index (flush may happen only after
        /// the add).
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        /// <param name="term"> the term to identify the document(s) to be
        /// deleted </param>
        /// <param name="doc"> the document to be added </param>
        /// <param name="analyzer"> the analyzer to use when analyzing the document </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void UpdateDocument(Term term, IEnumerable<IndexableField> doc, Analyzer analyzer)
        {
            EnsureOpen();
            try
            {
                bool success = false;
                try
                {
                    if (DocWriter.UpdateDocument(doc, analyzer, term))
                    {
                        ProcessEvents(true, false);
                    }
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
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "updateDocument");
            }
        }

        /// <summary>
        /// Updates a document's <seealso cref="NumericDocValues"/> for <code>field</code> to the
        /// given <code>value</code>. this method can be used to 'unset' a document's
        /// value by passing {@code null} as the new value. Also, you can only update
        /// fields that already exist in the index, not add new fields through this
        /// method.
        ///
        /// <p>
        /// <b>NOTE</b>: if this method hits an OutOfMemoryError you should immediately
        /// close the writer. See <a href="#OOME">above</a> for details.
        /// </p>
        /// </summary>
        /// <param name="term">
        ///          the term to identify the document(s) to be updated </param>
        /// <param name="field">
        ///          field name of the <seealso cref="NumericDocValues"/> field </param>
        /// <param name="value">
        ///          new value for the field </param>
        /// <exception cref="CorruptIndexException">
        ///           if the index is corrupt </exception>
        /// <exception cref="IOException">
        ///           if there is a low-level IO error </exception>
        public virtual void UpdateNumericDocValue(Term term, string field, long? value)
        {
            EnsureOpen();
            if (!GlobalFieldNumberMap.Contains(field, DocValuesType_e.NUMERIC))
            {
                throw new System.ArgumentException("can only update existing numeric-docvalues fields!");
            }
            try
            {
                if (DocWriter.UpdateNumericDocValue(term, field, value))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "updateNumericDocValue");
            }
        }

        /// <summary>
        /// Updates a document's <seealso cref="BinaryDocValues"/> for <code>field</code> to the
        /// given <code>value</code>. this method can be used to 'unset' a document's
        /// value by passing {@code null} as the new value. Also, you can only update
        /// fields that already exist in the index, not add new fields through this
        /// method.
        ///
        /// <p>
        /// <b>NOTE:</b> this method currently replaces the existing value of all
        /// affected documents with the new value.
        ///
        /// <p>
        /// <b>NOTE:</b> if this method hits an OutOfMemoryError you should immediately
        /// close the writer. See <a href="#OOME">above</a> for details.
        /// </p>
        /// </summary>
        /// <param name="term">
        ///          the term to identify the document(s) to be updated </param>
        /// <param name="field">
        ///          field name of the <seealso cref="BinaryDocValues"/> field </param>
        /// <param name="value">
        ///          new value for the field </param>
        /// <exception cref="CorruptIndexException">
        ///           if the index is corrupt </exception>
        /// <exception cref="IOException">
        ///           if there is a low-level IO error </exception>
        public virtual void UpdateBinaryDocValue(Term term, string field, BytesRef value)
        {
            EnsureOpen();
            if (!GlobalFieldNumberMap.Contains(field, DocValuesType_e.BINARY))
            {
                throw new System.ArgumentException("can only update existing binary-docvalues fields!");
            }
            try
            {
                if (DocWriter.UpdateBinaryDocValue(term, field, value))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "updateBinaryDocValue");
            }
        }

        // for test purpose
        public int SegmentCount
        {
            get
            {
                lock (this)
                {
                    return segmentInfos.Size();
                }
            }
        }

        // for test purpose
        public int NumBufferedDocuments
        {
            get
            {
                lock (this)
                {
                    return DocWriter.NumDocs;
                }
            }
        }

        // for test purpose
        internal ICollection<string> IndexFileNames
        {
            get
            {
                lock (this)
                {
                    return segmentInfos.Files(directory, true);
                }
            }
        }

        // for test purpose
        public int GetDocCount(int i)
        {
            lock (this)
            {
                if (i >= 0 && i < segmentInfos.Size())
                {
                    return segmentInfos.Info(i).Info.DocCount;
                }
                else
                {
                    return -1;
                }
            }
        }

        // for test purpose
        public int FlushCount
        {
            get
            {
                return flushCount.Get();
            }
        }

        // for test purpose
        public int FlushDeletesCount
        {
            get
            {
                return flushDeletesCount.Get();
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
                ChangeCount++;
                segmentInfos.Changed();
                return "_" + Number.ToString(segmentInfos.Counter++, Character.MAX_RADIX);
            }
        }

        /// <summary>
        /// If non-null, information about merges will be printed to this.
        /// </summary>
        internal readonly InfoStream infoStream;

        /// <summary>
        /// Forces merge policy to merge segments until there are <=
        /// maxNumSegments.  The actual merges to be
        /// executed are determined by the <seealso cref="MergePolicy"/>.
        ///
        /// <p>this is a horribly costly operation, especially when
        /// you pass a small {@code maxNumSegments}; usually you
        /// should only call this if the index is static (will no
        /// longer be changed).</p>
        ///
        /// <p>Note that this requires up to 2X the index size free
        /// space in your Directory (3X if you're using compound
        /// file format).  For example, if your index size is 10 MB
        /// then you need up to 20 MB free for this to complete (30
        /// MB if you're using compound file format).  Also,
        /// it's best to call <seealso cref="#commit()"/> afterwards,
        /// to allow IndexWriter to free up disk space.</p>
        ///
        /// <p>If some but not all readers re-open while merging
        /// is underway, this will cause > 2X temporary
        /// space to be consumed as those new readers will then
        /// hold open the temporary segments at that time.  It is
        /// best not to re-open readers while merging is running.</p>
        ///
        /// <p>The actual temporary usage could be much less than
        /// these figures (it depends on many factors).</p>
        ///
        /// <p>In general, once this completes, the total size of the
        /// index will be less than the size of the starting index.
        /// It could be quite a bit smaller (if there were many
        /// pending deletes) or just slightly smaller.</p>
        ///
        /// <p>If an Exception is hit, for example
        /// due to disk full, the index will not be corrupted and no
        /// documents will be lost.  However, it may have
        /// been partially merged (some segments were merged but
        /// not all), and it's possible that one of the segments in
        /// the index will be in non-compound format even when
        /// using compound file format.  this will occur when the
        /// Exception is hit during conversion of the segment into
        /// compound format.</p>
        ///
        /// <p>this call will merge those segments present in
        /// the index when the call started.  If other threads are
        /// still adding documents and flushing segments, those
        /// newly created segments will not be merged unless you
        /// call forceMerge again.</p>
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        ///
        /// <p><b>NOTE</b>: if you call <seealso cref="#close(boolean)"/>
        /// with <tt>false</tt>, which aborts all running merges,
        /// then any thread still running this method might hit a
        /// <seealso cref="MergePolicy.MergeAbortedException"/>.
        /// </summary>
        /// <param name="maxNumSegments"> maximum number of segments left
        /// in the index after merging finishes
        /// </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <seealso cref= MergePolicy#findMerges
        ///  </seealso>
        public virtual void ForceMerge(int maxNumSegments)
        {
            ForceMerge(maxNumSegments, true);
        }

        /// <summary>
        /// Just like <seealso cref="#forceMerge(int)"/>, except you can
        ///  specify whether the call should block until
        ///  all merging completes.  this is only meaningful with a
        ///  <seealso cref="mergeScheduler"/> that is able to run merges in
        ///  background threads.
        ///
        ///  <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        ///  you should immediately close the writer.  See <a
        ///  href="#OOME">above</a> for details.</p>
        /// </summary>
        public virtual void ForceMerge(int maxNumSegments, bool doWait)
        {
            EnsureOpen();

            if (maxNumSegments < 1)
            {
                throw new ArgumentException("maxNumSegments must be >= 1; got " + maxNumSegments);
            }

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "forceMerge: index now " + SegString());
                infoStream.Message("IW", "now flush at forceMerge");
            }

            Flush(true, true);

            lock (this)
            {
                ResetMergeExceptions();
                SegmentsToMerge.Clear();
                foreach (SegmentCommitInfo info in segmentInfos.Segments)
                {
                    if (info != null) SegmentsToMerge[info] = true;
                }
                MergeMaxNumSegments = maxNumSegments;

                // Now mark all pending & running merges for forced
                // merge:
                foreach (MergePolicy.OneMerge merge in PendingMerges)
                {
                    merge.MaxNumSegments = maxNumSegments;
                    if (merge.Info != null) SegmentsToMerge[merge.Info] = true;
                }

                foreach (MergePolicy.OneMerge merge in RunningMerges)
                {
                    merge.MaxNumSegments = maxNumSegments;
                    if (merge.Info != null) SegmentsToMerge[merge.Info] = true;
                }
            }

            MaybeMerge(MergeTrigger.EXPLICIT, maxNumSegments);

            if (doWait)
            {
                lock (this)
                {
                    while (true)
                    {
                        if (HitOOM)
                        {
                            throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot complete forceMerge");
                        }

                        if (MergeExceptions.Count > 0)
                        {
                            // Forward any exceptions in background merge
                            // threads to the current thread:
                            int size = MergeExceptions.Count;
                            for (int i = 0; i < size; i++)
                            {
                                MergePolicy.OneMerge merge = MergeExceptions[i];
                                if (merge.MaxNumSegments != -1)
                                {
                                    throw new System.IO.IOException("background merge hit exception: " + merge.SegString(directory), merge.Exception ?? new Exception());
                                    /*Exception t = merge.Exception;
                                    if (t != null)
                                    {
                                      err.initCause(t);
                                    }
                                    throw err;*/
                                }
                            }
                        }

                        if (MaxNumSegmentsMergesPending())
                        {
                            DoWait();
                        }
                        else
                        {
                            break;
                        }
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

        /// <summary>
        /// Returns true if any merges in pendingMerges or
        ///  runningMerges are maxNumSegments merges.
        /// </summary>
        private bool MaxNumSegmentsMergesPending()
        {
            lock (this)
            {
                foreach (MergePolicy.OneMerge merge in PendingMerges)
                {
                    if (merge.MaxNumSegments != -1)
                    {
                        return true;
                    }
                }

                foreach (MergePolicy.OneMerge merge in RunningMerges)
                {
                    if (merge.MaxNumSegments != -1)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Just like <seealso cref="#forceMergeDeletes()"/>, except you can
        ///  specify whether the call should block until the
        ///  operation completes.  this is only meaningful with a
        ///  <seealso cref="MergeScheduler"/> that is able to run merges in
        ///  background threads.
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        ///
        /// <p><b>NOTE</b>: if you call <seealso cref="#close(boolean)"/>
        /// with <tt>false</tt>, which aborts all running merges,
        /// then any thread still running this method might hit a
        /// <seealso cref="MergePolicy.MergeAbortedException"/>.
        /// </summary>
        public virtual void ForceMergeDeletes(bool doWait)
        {
            EnsureOpen();

            Flush(true, true);

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "forceMergeDeletes: index now " + SegString());
            }

            MergePolicy.MergeSpecification spec;
            bool newMergesFound = false;
            lock (this)
            {
                spec = mergePolicy.FindForcedDeletesMerges(segmentInfos);
                newMergesFound = spec != null;
                if (newMergesFound)
                {
                    int numMerges = spec.Merges.Count;
                    for (int i = 0; i < numMerges; i++)
                    {
                        RegisterMerge(spec.Merges[i]);
                    }
                }
            }

            mergeScheduler.Merge(this, MergeTrigger.EXPLICIT, newMergesFound);

            if (spec != null && doWait)
            {
                int numMerges = spec.Merges.Count;
                lock (this)
                {
                    bool running = true;
                    while (running)
                    {
                        if (HitOOM)
                        {
                            throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot complete forceMergeDeletes");
                        }

                        // Check each merge that MergePolicy asked us to
                        // do, to see if any of them are still running and
                        // if any of them have hit an exception.
                        running = false;
                        for (int i = 0; i < numMerges; i++)
                        {
                            MergePolicy.OneMerge merge = spec.Merges[i];
                            if (PendingMerges.Contains(merge) || RunningMerges.Contains(merge))
                            {
                                running = true;
                            }
                            Exception t = merge.Exception;
                            if (t != null)
                            {
                                throw new System.IO.IOException("background merge hit exception: " + merge.SegString(directory), t);
                            }
                        }

                        // If any of our merges are still running, wait:
                        if (running)
                        {
                            DoWait();
                        }
                    }
                }
            }

            // NOTE: in the ConcurrentMergeScheduler case, when
            // doWait is false, we can return immediately while
            // background threads accomplish the merging
        }

        /// <summary>
        ///  Forces merging of all segments that have deleted
        ///  documents.  The actual merges to be executed are
        ///  determined by the <seealso cref="MergePolicy"/>.  For example,
        ///  the default <seealso cref="TieredMergePolicy"/> will only
        ///  pick a segment if the percentage of
        ///  deleted docs is over 10%.
        ///
        ///  <p>this is often a horribly costly operation; rarely
        ///  is it warranted.</p>
        ///
        ///  <p>To see how
        ///  many deletions you have pending in your index, call
        ///  <seealso cref="IndexReader#numDeletedDocs"/>.</p>
        ///
        ///  <p><b>NOTE</b>: this method first flushes a new
        ///  segment (if there are indexed documents), and applies
        ///  all buffered deletes.
        ///
        ///  <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        ///  you should immediately close the writer.  See <a
        ///  href="#OOME">above</a> for details.</p>
        /// </summary>
        public virtual void ForceMergeDeletes()
        {
            ForceMergeDeletes(true);
        }

        /// <summary>
        /// Expert: asks the mergePolicy whether any merges are
        /// necessary now and if so, runs the requested merges and
        /// then iterate (test again if merges are needed) until no
        /// more merges are returned by the mergePolicy.
        ///
        /// Explicit calls to maybeMerge() are usually not
        /// necessary. The most common case is when merge policy
        /// parameters have changed.
        ///
        /// this method will call the <seealso cref="mergePolicy"/> with
        /// <seealso cref="MergeTrigger#EXPLICIT"/>.
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        public void MaybeMerge()
        {
            MaybeMerge(MergeTrigger.EXPLICIT, UNBOUNDED_MAX_MERGE_SEGMENTS);
        }

        private void MaybeMerge(MergeTrigger trigger, int maxNumSegments)
        {
            EnsureOpen(false);
            bool newMergesFound = UpdatePendingMerges(trigger, maxNumSegments);
            mergeScheduler.Merge(this, trigger, newMergesFound);
        }

        private bool UpdatePendingMerges(MergeTrigger trigger, int maxNumSegments)
        {
            lock (this)
            {
                Debug.Assert(maxNumSegments == -1 || maxNumSegments > 0);
                Debug.Assert(trigger != null);
                if (StopMerges)
                {
                    return false;
                }

                // Do not start new merges if we've hit OOME
                if (HitOOM)
                {
                    return false;
                }
                bool newMergesFound = false;
                MergePolicy.MergeSpecification spec;
                if (maxNumSegments != UNBOUNDED_MAX_MERGE_SEGMENTS)
                {
                    Debug.Assert(trigger == MergeTrigger.EXPLICIT || trigger == MergeTrigger.MERGE_FINISHED, "Expected EXPLICT or MERGE_FINISHED as trigger even with maxNumSegments set but was: " + trigger.ToString());
                    spec = mergePolicy.FindForcedMerges(segmentInfos, maxNumSegments, SegmentsToMerge);
                    newMergesFound = spec != null;
                    if (newMergesFound)
                    {
                        int numMerges = spec.Merges.Count;
                        for (int i = 0; i < numMerges; i++)
                        {
                            MergePolicy.OneMerge merge = spec.Merges[i];
                            merge.MaxNumSegments = maxNumSegments;
                        }
                    }
                }
                else
                {
                    spec = mergePolicy.FindMerges(trigger, segmentInfos);
                }
                newMergesFound = spec != null;
                if (newMergesFound)
                {
                    int numMerges = spec.Merges.Count;
                    for (int i = 0; i < numMerges; i++)
                    {
                        RegisterMerge(spec.Merges[i]);
                    }
                }
                return newMergesFound;
            }
        }

        /// <summary>
        /// Expert: to be used by a <seealso cref="MergePolicy"/> to avoid
        ///  selecting merges for segments already being merged.
        ///  The returned collection is not cloned, and thus is
        ///  only safe to access if you hold IndexWriter's lock
        ///  (which you do when IndexWriter invokes the
        ///  MergePolicy).
        ///
        ///  <p>Do not alter the returned collection!
        /// </summary>
        public virtual ICollection<SegmentCommitInfo> MergingSegments
        {
            get
            {
                lock (this)
                {
                    return mergingSegments;
                }
            }
        }

        /// <summary>
        /// Expert: the <seealso cref="mergeScheduler"/> calls this method to retrieve the next
        /// merge requested by the MergePolicy
        ///
        /// @lucene.experimental
        /// </summary>
        public virtual MergePolicy.OneMerge NextMerge
        {
            get
            {
                lock (this)
                {
                    if (PendingMerges.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        // Advance the merge from pending to running
                        MergePolicy.OneMerge merge = PendingMerges.First.Value;
                        PendingMerges.RemoveFirst();
                        RunningMerges.Add(merge);
                        return merge;
                    }
                }
            }
        }

        /// <summary>
        /// Expert: returns true if there are merges waiting to be scheduled.
        ///
        /// @lucene.experimental
        /// </summary>
        public virtual bool HasPendingMerges()
        {
            lock (this)
            {
                return PendingMerges.Count != 0;
            }
        }

        /// <summary>
        /// Close the <code>IndexWriter</code> without committing
        /// any changes that have occurred since the last commit
        /// (or since it was opened, if commit hasn't been called).
        /// this removes any temporary files that had been created,
        /// after which the state of the index will be the same as
        /// it was when commit() was last called or when this
        /// writer was first opened.  this also clears a previous
        /// call to <seealso cref="#prepareCommit"/>. </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public void Rollback()
        {
            // don't call ensureOpen here: this acts like "close()" in closeable.

            // Ensure that only one thread actually gets to do the
            // closing, and make sure no commit is also in progress:
            lock (CommitLock)
            {
                if (ShouldClose())
                {
                    RollbackInternal();
                }
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
                    StopMerges = true;
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

                BufferedUpdatesStream.Clear();
                DocWriter.Dispose(); // mark it as closed first to prevent subsequent indexing actions/flushes
                DocWriter.Abort(this); // don't sync on IW here
                lock (this)
                {
                    if (PendingCommit != null)
                    {
                        PendingCommit.RollbackCommit(directory);
                        Deleter.DecRef(PendingCommit);
                        PendingCommit = null;
                        Monitor.PulseAll(this);
                    }

                    // Don't bother saving any changes in our segmentInfos
                    readerPool.DropAll(false);

                    // Keep the same segmentInfos instance but replace all
                    // of its SegmentInfo instances.  this is so the next
                    // attempt to commit using this instance of IndexWriter
                    // will always write to a new generation ("write
                    // once").
                    segmentInfos.RollbackSegmentInfos(RollbackSegments);
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "rollback: infos=" + SegString(segmentInfos.Segments));
                    }

                    var tpResult = TestPoint("rollback before checkpoint");
                    Debug.Assert(tpResult);

                    // Ask deleter to locate unreferenced files & remove
                    // them:
                    Deleter.Checkpoint(segmentInfos, false);
                    Deleter.Refresh();

                    LastCommitChangeCount = ChangeCount;

                    Deleter.Refresh();
                    Deleter.Dispose();

                    WriteLock.Release();
                    IOUtils.Close(WriteLock); // release write lock
                    WriteLock = null;

                    Debug.Assert(DocWriter.PerThreadPool.NumDeactivatedThreadStates() == DocWriter.PerThreadPool.MaxThreadStates, "" + DocWriter.PerThreadPool.NumDeactivatedThreadStates() + " " + DocWriter.PerThreadPool.MaxThreadStates);
                }

                success = true;
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "rollbackInternal");
            }
            finally
            {
                if (!success)
                {
                    // Must not hold IW's lock while closing
                    // mergePolicy/Scheduler: this can lead to deadlock,
                    // e.g. TestIW.testThreadInterruptDeadlock
                    IOUtils.CloseWhileHandlingException(mergePolicy, mergeScheduler);
                }
                lock (this)
                {
                    if (!success)
                    {
                        // we tried to be nice about it: do the minimum

                        // don't leak a segments_N file if there is a pending commit
                        if (PendingCommit != null)
                        {
                            try
                            {
                                PendingCommit.RollbackCommit(directory);
                                Deleter.DecRef(PendingCommit);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        // close all the closeables we can (but important is readerPool and writeLock to prevent leaks)
                        if (WriteLock != null)
                        {
                            WriteLock.Release();
                        }
                        IOUtils.CloseWhileHandlingException(readerPool, Deleter, WriteLock);
                        WriteLock = null;
                    }
                    closed = true;
                    Closing = false;
                }
            }
        }

        /// <summary>
        /// Delete all documents in the index.
        ///
        /// <p>this method will drop all buffered documents and will
        ///    remove all segments from the index. this change will not be
        ///    visible until a <seealso cref="#commit()"/> has been called. this method
        ///    can be rolled back using <seealso cref="#rollback()"/>.</p>
        ///
        /// <p>NOTE: this method is much faster than using deleteDocuments( new MatchAllDocsQuery() ).
        ///    Yet, this method also has different semantics compared to <seealso cref="#deleteDocuments(Query)"/>
        ///    / <seealso cref="#deleteDocuments(Query...)"/> since internal data-structures are cleared as well
        ///    as all segment information is forcefully dropped anti-viral semantics like omitting norms
        ///    are reset or doc value types are cleared. Essentially a call to <seealso cref="#deleteAll()"/> is equivalent
        ///    to creating a new <seealso cref="IndexWriter"/> with <seealso cref="OpenMode#CREATE"/> which a delete query only marks
        ///    documents as deleted.</p>
        ///
        /// <p>NOTE: this method will forcefully abort all merges
        ///    in progress.  If other threads are running {@link
        ///    #forceMerge}, <seealso cref="#addIndexes(IndexReader[])"/> or
        ///    <seealso cref="#forceMergeDeletes"/> methods, they may receive
        ///    <seealso cref="MergePolicy.MergeAbortedException"/>s.
        /// </summary>
        public virtual void DeleteAll()
        {
            EnsureOpen();
            // Remove any buffered docs
            bool success = false;
            /* hold the full flush lock to prevent concurrency commits / NRT reopens to
             * get in our way and do unnecessary work. -- if we don't lock this here we might
             * get in trouble if */
            lock (FullFlushLock)
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
                    DocWriter.LockAndAbortAll(this);
                    ProcessEvents(false, true);
                    lock (this)
                    {
                        try
                        {
                            // Abort any running merges
                            FinishMerges(false);
                            // Remove all segments
                            segmentInfos.Clear();
                            // Ask deleter to locate unreferenced files & remove them:
                            Deleter.Checkpoint(segmentInfos, false);
                            /* don't refresh the deleter here since there might
                             * be concurrent indexing requests coming in opening
                             * files on the directory after we called DW#abort()
                             * if we do so these indexing requests might hit FNF exceptions.
                             * We will remove the files incrementally as we go...
                             */
                            // Don't bother saving any changes in our segmentInfos
                            readerPool.DropAll(false);
                            // Mark that the index has changed
                            ++ChangeCount;
                            segmentInfos.Changed();
                            GlobalFieldNumberMap.Clear();
                            success = true;
                        }
                        catch (System.OutOfMemoryException oom)
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
                    DocWriter.UnlockAllAfterAbortAll(this);
                }
            }
        }

        private void FinishMerges(bool waitForMerges)
        {
            lock (this)
            {
                if (!waitForMerges)
                {
                    StopMerges = true;

                    // Abort all pending & running merges:
                    foreach (MergePolicy.OneMerge merge in PendingMerges)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "now abort pending merge " + SegString(merge.Segments));
                        }
                        merge.Abort();
                        MergeFinish(merge);
                    }
                    PendingMerges.Clear();

                    foreach (MergePolicy.OneMerge merge in RunningMerges)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "now abort running merge " + SegString(merge.Segments));
                        }
                        merge.Abort();
                    }

                    // These merges periodically check whether they have
                    // been aborted, and stop if so.  We wait here to make
                    // sure they all stop.  It should not take very long
                    // because the merge threads periodically check if
                    // they are aborted.
                    while (RunningMerges.Count > 0)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "now wait for " + RunningMerges.Count + " running merge/s to abort");
                        }
                        DoWait();
                    }

                    StopMerges = false;
                    Monitor.PulseAll(this);

                    Debug.Assert(0 == mergingSegments.Count);

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

        /// <summary>
        /// Wait for any currently outstanding merges to finish.
        ///
        /// <p>It is guaranteed that any merges started prior to calling this method
        ///    will have completed once this method completes.</p>
        /// </summary>
        public virtual void WaitForMerges()
        {
            lock (this)
            {
                EnsureOpen(false);
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "waitForMerges");
                }
                while (PendingMerges.Count > 0 || RunningMerges.Count > 0)
                {
                    DoWait();
                }

                // sanity check
                Debug.Assert(0 == mergingSegments.Count);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "waitForMerges done");
                }
            }
        }

        /// <summary>
        /// Called whenever the SegmentInfos has been updated and
        /// the index files referenced exist (correctly) in the
        /// index directory.
        /// </summary>
        internal virtual void Checkpoint()
        {
            lock (this)
            {
                Changed();
                Deleter.Checkpoint(segmentInfos, false);
            }
        }

        /// <summary>
        /// Checkpoints with IndexFileDeleter, so it's aware of
        ///  new files, and increments changeCount, so on
        ///  close/commit we will write a new segments file, but
        ///  does NOT bump segmentInfos.version.
        /// </summary>
        internal virtual void CheckpointNoSIS()
        {
            lock (this)
            {
                ChangeCount++;
                Deleter.Checkpoint(segmentInfos, false);
            }
        }

        /// <summary>
        /// Called internally if any index state has changed. </summary>
        internal void Changed()
        {
            lock (this)
            {
                ChangeCount++;
                segmentInfos.Changed();
            }
        }

        internal virtual void PublishFrozenUpdates(FrozenBufferedUpdates packet)
        {
            lock (this)
            {
                Debug.Assert(packet != null && packet.Any());
                lock (BufferedUpdatesStream)
                {
                    BufferedUpdatesStream.Push(packet);
                }
            }
        }

        /// <summary>
        /// Atomically adds the segment private delete packet and publishes the flushed
        /// segments SegmentInfo to the index writer.
        /// </summary>
        internal virtual void PublishFlushedSegment(SegmentCommitInfo newSegment, FrozenBufferedUpdates packet, FrozenBufferedUpdates globalPacket)
        {
            try
            {
                lock (this)
                {
                    // Lock order IW -> BDS
                    lock (BufferedUpdatesStream)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "publishFlushedSegment");
                        }

                        if (globalPacket != null && globalPacket.Any())
                        {
                            BufferedUpdatesStream.Push(globalPacket);
                        }
                        // Publishing the segment must be synched on IW -> BDS to make the sure
                        // that no merge prunes away the seg. private delete packet
                        long nextGen;
                        if (packet != null && packet.Any())
                        {
                            nextGen = BufferedUpdatesStream.Push(packet);
                        }
                        else
                        {
                            // Since we don't have a delete packet to apply we can get a new
                            // generation right away
                            nextGen = BufferedUpdatesStream.NextGen;
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
            finally
            {
                flushCount.IncrementAndGet();
                DoAfterFlush();
            }
        }

        private void ResetMergeExceptions()
        {
            lock (this)
            {
                MergeExceptions = new List<MergePolicy.OneMerge>();
                MergeGen++;
            }
        }

        private void NoDupDirs(params Directory[] dirs)
        {
            HashSet<Directory> dups = new HashSet<Directory>();
            for (int i = 0; i < dirs.Length; i++)
            {
                if (dups.Contains(dirs[i]))
                {
                    throw new System.ArgumentException("Directory " + dirs[i] + " appears more than once");
                }
                if (dirs[i] == directory)
                {
                    throw new System.ArgumentException("Cannot add directory to itself");
                }
                dups.Add(dirs[i]);
            }
        }

        /// <summary>
        /// Acquires write locks on all the directories; be sure
        ///  to match with a call to <seealso cref="IOUtils#close"/> in a
        ///  finally clause.
        /// </summary>
        private IEnumerable<Lock> AcquireWriteLocks(params Directory[] dirs)
        {
            IList<Lock> locks = new List<Lock>();
            for (int i = 0; i < dirs.Length; i++)
            {
                bool success = false;
                try
                {
                    Lock @lock = dirs[i].MakeLock(WRITE_LOCK_NAME);
                    locks.Add(@lock);
                    @lock.Obtain(Config_Renamed.WriteLockTimeout);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Release all previously acquired locks:
                        IOUtils.CloseWhileHandlingException(locks);
                    }
                }
            }
            return locks;
        }

        /// <summary>
        /// Adds all segments from an array of indexes into this index.
        ///
        /// <p>this may be used to parallelize batch indexing. A large document
        /// collection can be broken into sub-collections. Each sub-collection can be
        /// indexed in parallel, on a different thread, process or machine. The
        /// complete index can then be created by merging sub-collection indexes
        /// with this method.
        ///
        /// <p>
        /// <b>NOTE:</b> this method acquires the write lock in
        /// each directory, to ensure that no {@code IndexWriter}
        /// is currently open or tries to open while this is
        /// running.
        ///
        /// <p>this method is transactional in how Exceptions are
        /// handled: it does not commit a new segments_N file until
        /// all indexes are added.  this means if an Exception
        /// occurs (for example disk full), then either no indexes
        /// will have been added or they all will have been.
        ///
        /// <p>Note that this requires temporary free space in the
        /// <seealso cref="Directory"/> up to 2X the sum of all input indexes
        /// (including the starting index). If readers/searchers
        /// are open against the starting index, then temporary
        /// free space required will be higher by the size of the
        /// starting index (see <seealso cref="#forceMerge(int)"/> for details).
        ///
        /// <p>
        /// <b>NOTE:</b> this method only copies the segments of the incoming indexes
        /// and does not merge them. Therefore deleted documents are not removed and
        /// the new segments are not merged with the existing ones.
        ///
        /// <p>this requires this index not be among those to be added.
        ///
        /// <p>
        /// <b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer. See <a
        /// href="#OOME">above</a> for details.
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <exception cref="LockObtainFailedException"> if we were unable to
        ///   acquire the write lock in at least one directory </exception>
        public virtual void AddIndexes(params Directory[] dirs)
        {
            EnsureOpen();

            NoDupDirs(dirs);

            IEnumerable<Lock> locks = AcquireWriteLocks(dirs);

            bool successTop = false;

            try
            {
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "flush at addIndexes(Directory...)");
                }

                Flush(false, true);

                IList<SegmentCommitInfo> infos = new List<SegmentCommitInfo>();
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
                        HashSet<string> dsFilesCopied = new HashSet<string>();
                        IDictionary<string, string> dsNames = new Dictionary<string, string>();
                        HashSet<string> copiedFiles = new HashSet<string>();
                        foreach (SegmentCommitInfo info in sis.Segments)
                        {
                            Debug.Assert(!infos.Contains(info), "dup info dir=" + info.Info.Dir + " name=" + info.Info.Name);

                            string newSegName = NewSegmentName();

                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "addIndexes: process segment origName=" + info.Info.Name + " newName=" + newSegName + " info=" + info);
                            }

                            IOContext context = new IOContext(new MergeInfo(info.Info.DocCount, info.SizeInBytes(), true, -1));

                            foreach (FieldInfo fi in SegmentReader.ReadFieldInfos(info))
                            {
                                GlobalFieldNumberMap.AddOrGet(fi.Name, fi.Number, fi.DocValuesType);
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
                        foreach (SegmentCommitInfo sipc in infos)
                        {
                            foreach (string file in sipc.Files())
                            {
                                try
                                {
                                    directory.DeleteFile(file);
                                }
                                catch (Exception)
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
                            foreach (SegmentCommitInfo sipc in infos)
                            {
                                foreach (string file in sipc.Files())
                                {
                                    try
                                    {
                                        directory.DeleteFile(file);
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            }
                        }
                    }
                    segmentInfos.AddAll(infos);
                    Checkpoint();
                }

                successTop = true;
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "addIndexes(Directory...)");
            }
            finally
            {
                if (locks != null)
                {
                    foreach (var lk in locks)
                    {
                        lk.Release();
                    }
                }

                if (successTop)
                {
                    IOUtils.Close(locks);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException(locks);
                }
            }
        }

        /// <summary>
        /// Merges the provided indexes into this index.
        ///
        /// <p>
        /// The provided IndexReaders are not closed.
        ///
        /// <p>
        /// See <seealso cref="#addIndexes"/> for details on transactional semantics, temporary
        /// free space required in the Directory, and non-CFS segments on an Exception.
        ///
        /// <p>
        /// <b>NOTE</b>: if this method hits an OutOfMemoryError you should immediately
        /// close the writer. See <a href="#OOME">above</a> for details.
        ///
        /// <p>
        /// <b>NOTE:</b> empty segments are dropped by this method and not added to this
        /// index.
        ///
        /// <p>
        /// <b>NOTE:</b> this method merges all given <seealso cref="IndexReader"/>s in one
        /// merge. If you intend to merge a large number of readers, it may be better
        /// to call this method multiple times, each time with a small set of readers.
        /// In principle, if you use a merge policy with a {@code mergeFactor} or
        /// {@code maxMergeAtOnce} parameter, you should pass that many readers in one
        /// call. Also, if the given readers are <seealso cref="DirectoryReader"/>s, they can be
        /// opened with {@code termIndexInterval=-1} to save RAM, since during merge
        /// the in-memory structure is not used. See
        /// <seealso cref="DirectoryReader#open(Directory, int)"/>.
        ///
        /// <p>
        /// <b>NOTE</b>: if you call <seealso cref="#close(boolean)"/> with <tt>false</tt>, which
        /// aborts all running merges, then any thread still running this method might
        /// hit a <seealso cref="MergePolicy.MergeAbortedException"/>.
        /// </summary>
        /// <exception cref="CorruptIndexException">
        ///           if the index is corrupt </exception>
        /// <exception cref="IOException">
        ///           if there is a low-level IO error </exception>
        public virtual void AddIndexes(params IndexReader[] readers)
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

                string mergedName = NewSegmentName();
                IList<AtomicReader> mergeReaders = new List<AtomicReader>();
                foreach (IndexReader indexReader in readers)
                {
                    numDocs += indexReader.NumDocs;
                    foreach (AtomicReaderContext ctx in indexReader.Leaves)
                    {
                        mergeReaders.Add(ctx.AtomicReader);
                    }
                }

                IOContext context = new IOContext(new MergeInfo(numDocs, -1, true, -1));

                // TODO: somehow we should fix this merge so it's
                // abortable so that IW.close(false) is able to stop it
                TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(directory);

                SegmentInfo info = new SegmentInfo(directory, Constants.LUCENE_MAIN_VERSION, mergedName, -1, false, Codec, null);

                SegmentMerger merger = new SegmentMerger(mergeReaders, info, infoStream, trackingDir, Config_Renamed.TermIndexInterval, MergeState.CheckAbort.NONE, GlobalFieldNumberMap, context, Config_Renamed.CheckIntegrityAtMerge);

                if (!merger.ShouldMerge())
                {
                    return;
                }

                MergeState mergeState;
                bool success = false;
                try
                {
                    mergeState = merger.Merge(); // merge 'em
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        lock (this)
                        {
                            Deleter.Refresh(info.Name);
                        }
                    }
                }

                SegmentCommitInfo infoPerCommit = new SegmentCommitInfo(info, 0, -1L, -1L);

                info.Files = new HashSet<string>(trackingDir.CreatedFiles);
                trackingDir.CreatedFiles.Clear();

                SetDiagnostics(info, SOURCE_ADDINDEXES_READERS);

                bool useCompoundFile;
                lock (this) // Guard segmentInfos
                {
                    if (StopMerges)
                    {
                        Deleter.DeleteNewFiles(infoPerCommit.Files());
                        return;
                    }
                    EnsureOpen();
                    useCompoundFile = mergePolicy.UseCompoundFile(segmentInfos, infoPerCommit);
                }

                // Now create the compound file if needed
                if (useCompoundFile)
                {
                    ICollection<string> filesToDelete = infoPerCommit.Files();
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
                            Deleter.DeleteNewFiles(filesToDelete);
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
                    Codec.SegmentInfoFormat().SegmentInfoWriter.Write(trackingDir, info, mergeState.FieldInfos, context);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        lock (this)
                        {
                            Deleter.Refresh(info.Name);
                        }
                    }
                }

                info.AddFiles(trackingDir.CreatedFiles);

                // Register the new segment
                lock (this)
                {
                    if (StopMerges)
                    {
                        Deleter.DeleteNewFiles(info.Files);
                        return;
                    }
                    EnsureOpen();
                    segmentInfos.Add(infoPerCommit);
                    Checkpoint();
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "addIndexes(IndexReader...)");
            }
        }

        /// <summary>
        /// Copies the segment files as-is into the IndexWriter's directory. </summary>
        private SegmentCommitInfo CopySegmentAsIs(SegmentCommitInfo info, string segName, IDictionary<string, string> dsNames, ISet<string> dsFilesCopied, IOContext context, ISet<string> copiedFiles)
        {
            // Determine if the doc store of this segment needs to be copied. It's
            // only relevant for segments that share doc store with others,
            // because the DS might have been copied already, in which case we
            // just want to update the DS name of this SegmentInfo.
            string dsName = Lucene3xSegmentInfoFormat.GetDocStoreSegment(info.Info);
            Debug.Assert(dsName != null);
            string newDsName;
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
            FieldInfos fis = SegmentReader.ReadFieldInfos(info);

            ISet<string> docStoreFiles3xOnly = Lucene3xCodec.GetDocStoreFiles(info.Info);

            IDictionary<string, string> attributes;
            // copy the attributes map, we might modify it below.
            // also we need to ensure its read-write, since we will invoke the SIwriter (which might want to set something).
            if (info.Info.Attributes() == null)
            {
                attributes = new Dictionary<string, string>();
            }
            else
            {
                attributes = new Dictionary<string, string>(info.Info.Attributes());
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
            SegmentInfo newInfo = new SegmentInfo(directory, info.Info.Version, segName, info.Info.DocCount, info.Info.UseCompoundFile, info.Info.Codec, info.Info.Diagnostics, attributes);
            SegmentCommitInfo newInfoPerCommit = new SegmentCommitInfo(newInfo, info.DelCount, info.DelGen, info.FieldInfosGen);

            HashSet<string> segFiles = new HashSet<string>();

            // Build up new segment's file names.  Must do this
            // before writing SegmentInfo:
            foreach (string file in info.Files())
            {
                string newFileName;
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
            Codec currentCodec = newInfo.Codec;
            try
            {
                currentCodec.SegmentInfoFormat().SegmentInfoWriter.Write(trackingDir, newInfo, fis, context);
            }
            catch (System.NotSupportedException uoe)
            {
                if (currentCodec is Lucene3xCodec)
                {
                    // OK: 3x codec cannot write a new SI file;
                    // SegmentInfos will write this on commit
                }
                else
                {
                    throw uoe;
                }
            }

            ICollection<string> siFiles = trackingDir.CreatedFiles;

            bool success = false;
            try
            {
                // Copy the segment's files
                foreach (string file in info.Files())
                {
                    string newFileName;
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

                    Debug.Assert(!SlowFileExists(directory, newFileName), "file \"" + newFileName + "\" already exists; siFiles=" + siFiles);
                    Debug.Assert(!copiedFiles.Contains(file), "file \"" + file + "\" is being copied more than once");
                    copiedFiles.Add(file);
                    info.Info.Dir.Copy(directory, file, newFileName, context);
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    foreach (string file in newInfo.Files)
                    {
                        try
                        {
                            directory.DeleteFile(file);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            return newInfoPerCommit;
        }

        /// <summary>
        /// A hook for extending classes to execute operations after pending added and
        /// deleted documents have been flushed to the Directory but before the change
        /// is committed (new segments_N file written).
        /// </summary>
        protected internal virtual void DoAfterFlush()
        {
        }

        /// <summary>
        /// A hook for extending classes to execute operations before pending added and
        /// deleted documents are flushed to the Directory.
        /// </summary>
        protected internal virtual void DoBeforeFlush()
        {
        }

        /// <summary>
        /// <p>Expert: prepare for commit.  this does the
        ///  first phase of 2-phase commit. this method does all
        ///  steps necessary to commit changes since this writer
        ///  was opened: flushes pending added and deleted docs,
        ///  syncs the index files, writes most of next segments_N
        ///  file.  After calling this you must call either {@link
        ///  #commit()} to finish the commit, or {@link
        ///  #rollback()} to revert the commit and undo all changes
        ///  done since the writer was opened.</p>
        ///
        /// <p>You can also just call <seealso cref="#commit()"/> directly
        ///  without prepareCommit first in which case that method
        ///  will internally call prepareCommit.
        ///
        ///  <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        ///  you should immediately close the writer.  See <a
        ///  href="#OOME">above</a> for details.</p>
        /// </summary>
        public void PrepareCommit()
        {
            EnsureOpen();
            PrepareCommitInternal();
        }

        private void PrepareCommitInternal()
        {
            lock (CommitLock)
            {
                EnsureOpen(false);
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "prepareCommit: flush");
                    infoStream.Message("IW", "  index before flush " + SegString());
                }

                if (HitOOM)
                {
                    throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot commit");
                }

                if (PendingCommit != null)
                {
                    throw new InvalidOperationException("prepareCommit was already called with no corresponding call to commit");
                }

                DoBeforeFlush();
                var tpResult = TestPoint("startDoFlush");
                Debug.Assert(tpResult);
                SegmentInfos toCommit = null;
                bool anySegmentsFlushed = false;

                // this is copied from doFlush, except it's modified to
                // clone & incRef the flushed SegmentInfos inside the
                // sync block:

                try
                {
                    lock (FullFlushLock)
                    {
                        bool flushSuccess = false;
                        bool success = false;
                        try
                        {
                            anySegmentsFlushed = DocWriter.FlushAllThreads(this);
                            if (!anySegmentsFlushed)
                            {
                                // prevent double increment since docWriter#doFlush increments the flushcount
                                // if we flushed anything.
                                flushCount.IncrementAndGet();
                            }
                            ProcessEvents(false, true);
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

                                PendingCommitChangeCount = ChangeCount;

                                // this protects the segmentInfos we are now going
                                // to commit.  this is important in case, eg, while
                                // we are trying to sync all referenced files, a
                                // merge completes which would otherwise have
                                // removed the files we are now syncing.
                                FilesToCommit = toCommit.Files(directory, false);
                                Deleter.IncRef(FilesToCommit);
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
                            DocWriter.FinishFullFlush(flushSuccess);
                            DoAfterFlush();
                        }
                    }
                }
                catch (System.OutOfMemoryException oom)
                {
                    HandleOOM(oom, "prepareCommit");
                }

                bool success_ = false;
                try
                {
                    if (anySegmentsFlushed)
                    {
                        MaybeMerge(MergeTrigger.FULL_FLUSH, UNBOUNDED_MAX_MERGE_SEGMENTS);
                    }
                    StartCommit(toCommit);
                    success_ = true;
                }
                finally
                {
                    if (!success_)
                    {
                        lock (this)
                        {
                            if (FilesToCommit != null)
                            {
                                Deleter.DecRef(FilesToCommit);
                                FilesToCommit = null;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the commit user data map. That method is considered a transaction by
        /// <seealso cref="IndexWriter"/> and will be <seealso cref="#commit() committed"/> even if no other
        /// changes were made to the writer instance. Note that you must call this method
        /// before <seealso cref="#prepareCommit()"/>, or otherwise it won't be included in the
        /// follow-on <seealso cref="#commit()"/>.
        /// <p>
        /// <b>NOTE:</b> the map is cloned internally, therefore altering the map's
        /// contents after calling this method has no effect.
        /// </summary>
        public IDictionary<string, string> CommitData
        {
            set
            {
                lock (this)
                {
                    segmentInfos.UserData = new Dictionary<string, string>(value);
                    ++ChangeCount;
                }
            }
            get
            {
                lock (this)
                {
                    return segmentInfos.UserData;
                }
            }
        }

        // Used only by commit and prepareCommit, below; lock
        // order is commitLock -> IW
        private readonly object CommitLock = new object();

        /// <summary>
        /// <p>Commits all pending changes (added & deleted
        /// documents, segment merges, added
        /// indexes, etc.) to the index, and syncs all referenced
        /// index files, such that a reader will see the changes
        /// and the index updates will survive an OS or machine
        /// crash or power loss.  Note that this does not wait for
        /// any running background merges to finish.  this may be a
        /// costly operation, so you should test the cost in your
        /// application and do it only when really necessary.</p>
        ///
        /// <p> Note that this operation calls Directory.sync on
        /// the index files.  That call should not return until the
        /// file contents & metadata are on stable storage.  For
        /// FSDirectory, this calls the OS's fsync.  But, beware:
        /// some hardware devices may in fact cache writes even
        /// during fsync, and return before the bits are actually
        /// on stable storage, to give the appearance of faster
        /// performance.  If you have such a device, and it does
        /// not have a battery backup (for example) then on power
        /// loss it may still lose data.  Lucene cannot guarantee
        /// consistency on such devices.  </p>
        ///
        /// <p><b>NOTE</b>: if this method hits an OutOfMemoryError
        /// you should immediately close the writer.  See <a
        /// href="#OOME">above</a> for details.</p>
        /// </summary>
        public void Commit()
        {
            EnsureOpen();
            CommitInternal();
        }

        /// <summary>
        /// Returns true if there may be changes that have not been
        ///  committed.  There are cases where this may return true
        ///  when there are no actual "real" changes to the index,
        ///  for example if you've deleted by Term or Query but
        ///  that Term or Query does not match any documents.
        ///  Also, if a merge kicked off as a result of flushing a
        ///  new segment during <seealso cref="#commit"/>, or a concurrent
        ///  merged finished, this method may return true right
        ///  after you had just called <seealso cref="#commit"/>.
        /// </summary>
        public bool HasUncommittedChanges()
        {
            return ChangeCount != LastCommitChangeCount || DocWriter.AnyChanges() || BufferedUpdatesStream.Any();
        }

        private void CommitInternal()
        {
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "commit: start");
            }

            lock (CommitLock)
            {
                EnsureOpen(false);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "commit: enter lock");
                }

                if (PendingCommit == null)
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
                if (PendingCommit != null)
                {
                    try
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "commit: pendingCommit != null");
                        }
                        PendingCommit.FinishCommit(directory);
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "commit: wrote segments file \"" + PendingCommit.SegmentsFileName + "\"");
                        }
                        segmentInfos.UpdateGeneration(PendingCommit);
                        LastCommitChangeCount = PendingCommitChangeCount;
                        RollbackSegments = PendingCommit.CreateBackupSegmentInfos();
                        // NOTE: don't use this.checkpoint() here, because
                        // we do not want to increment changeCount:
                        Deleter.Checkpoint(PendingCommit, true);
                    }
                    finally
                    {
                        // Matches the incRef done in prepareCommit:
                        Deleter.DecRef(FilesToCommit);
                        FilesToCommit = null;
                        PendingCommit = null;
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

        // Ensures only one flush() is actually flushing segments
        // at a time:
        private readonly object FullFlushLock = new object();

        //LUCENE TO-DO Not possible in .NET
        /*// for assert
        internal virtual bool HoldsFullFlushLock()
        {
          return Thread.holdsLock(FullFlushLock);
        }*/

        /// <summary>
        /// Flush all in-memory buffered updates (adds and deletes)
        /// to the Directory. </summary>
        /// <param name="triggerMerge"> if true, we may merge segments (if
        ///  deletes or docs were flushed) if necessary </param>
        /// <param name="applyAllDeletes"> whether pending deletes should also </param>
        public void Flush(bool triggerMerge, bool applyAllDeletes)
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
            if (HitOOM)
            {
                throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot flush");
            }

            DoBeforeFlush();
            var tpResult = TestPoint("startDoFlush");
            Debug.Assert(tpResult);
            bool success = false;
            try
            {
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "  start flush: applyAllDeletes=" + applyAllDeletes);
                    infoStream.Message("IW", "  index before flush " + SegString());
                }
                bool anySegmentFlushed;

                lock (FullFlushLock)
                {
                    bool flushSuccess = false;
                    try
                    {
                        anySegmentFlushed = DocWriter.FlushAllThreads(this);
                        flushSuccess = true;
                    }
                    finally
                    {
                        DocWriter.FinishFullFlush(flushSuccess);
                        ProcessEvents(false, true);
                    }
                }
                lock (this)
                {
                    MaybeApplyDeletes(applyAllDeletes);
                    DoAfterFlush();
                    if (!anySegmentFlushed)
                    {
                        // flushCount is incremented in flushAllThreads
                        flushCount.IncrementAndGet();
                    }
                    success = true;
                    return anySegmentFlushed;
                }
            }
            catch (System.OutOfMemoryException oom)
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
                    ApplyAllDeletesAndUpdates();
                }
                else if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "don't apply deletes now delTermCount=" + BufferedUpdatesStream.NumTerms() + " bytesUsed=" + BufferedUpdatesStream.BytesUsed());
                }
            }
        }

        internal void ApplyAllDeletesAndUpdates()
        {
            lock (this)
            {
                flushDeletesCount.IncrementAndGet();
                BufferedUpdatesStream.ApplyDeletesResult result;
                result = BufferedUpdatesStream.ApplyDeletesAndUpdates(readerPool, segmentInfos.AsList());
                if (result.AnyDeletes)
                {
                    Checkpoint();
                }
                if (!KeepFullyDeletedSegments_Renamed && result.AllDeleted != null)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "drop 100% deleted segments: " + SegString(result.AllDeleted));
                    }
                    foreach (SegmentCommitInfo info in result.AllDeleted)
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
                BufferedUpdatesStream.Prune(segmentInfos);
            }
        }

        /// <summary>
        /// Expert:  Return the total size of all index files currently cached in memory.
        /// Useful for size management with flushRamDocs()
        /// </summary>
        public long RamSizeInBytes()
        {
            EnsureOpen();
            return DocWriter.FlushControl.NetBytes() + BufferedUpdatesStream.BytesUsed();
        }

        // for testing only
        public virtual DocumentsWriter DocsWriter
        {
            get
            {
                bool test = false;
                Debug.Assert(test = true);
                return test ? DocWriter : null;
            }
        }

        /// <summary>
        /// Expert:  Return the number of documents currently
        ///  buffered in RAM.
        /// </summary>
        public int NumRamDocs()
        {
            lock (this)
            {
                EnsureOpen();
                return DocWriter.NumDocs;
            }
        }

        private void EnsureValidMerge(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                foreach (SegmentCommitInfo info in merge.Segments)
                {
                    if (!segmentInfos.Contains(info))
                    {
                        throw new MergePolicy.MergeException("MergePolicy selected a segment (" + info.Info.Name + ") that is not in the current index " + SegString(), directory);
                    }
                }
            }
        }

        private void SkipDeletedDoc(DocValuesFieldUpdates.Iterator[] updatesIters, int deletedDoc)
        {
            foreach (DocValuesFieldUpdates.Iterator iter in updatesIters)
            {
                if (iter.Doc() == deletedDoc)
                {
                    iter.NextDoc();
                }
                // when entering the method, all iterators must already be beyond the
                // deleted document, or right on it, in which case we advance them over
                // and they must be beyond it now.
                Debug.Assert(iter.Doc() > deletedDoc, "updateDoc=" + iter.Doc() + " deletedDoc=" + deletedDoc);
            }
        }

        private class MergedDeletesAndUpdates
        {
            internal ReadersAndUpdates MergedDeletesAndUpdates_Renamed = null;
            internal MergePolicy.DocMap DocMap = null;
            internal bool InitializedWritableLiveDocs = false;

            internal MergedDeletesAndUpdates()
            {
            }

            internal void Init(ReaderPool readerPool, MergePolicy.OneMerge merge, MergeState mergeState, bool initWritableLiveDocs)
            {
                if (MergedDeletesAndUpdates_Renamed == null)
                {
                    MergedDeletesAndUpdates_Renamed = readerPool.Get(merge.info, true);
                    DocMap = merge.GetDocMap(mergeState);
                    Debug.Assert(DocMap.IsConsistent(merge.info.Info.DocCount));
                }
                if (initWritableLiveDocs && !InitializedWritableLiveDocs)
                {
                    MergedDeletesAndUpdates_Renamed.InitWritableLiveDocs();
                    this.InitializedWritableLiveDocs = true;
                }
            }
        }

        private void MaybeApplyMergedDVUpdates(MergePolicy.OneMerge merge, MergeState mergeState, int docUpto, MergedDeletesAndUpdates holder, string[] mergingFields, DocValuesFieldUpdates[] dvFieldUpdates, DocValuesFieldUpdates.Iterator[] updatesIters, int curDoc)
        {
            int newDoc = -1;
            for (int idx = 0; idx < mergingFields.Length; idx++)
            {
                DocValuesFieldUpdates.Iterator updatesIter = updatesIters[idx];
                if (updatesIter.Doc() == curDoc) // document has an update
                {
                    if (holder.MergedDeletesAndUpdates_Renamed == null)
                    {
                        holder.Init(readerPool, merge, mergeState, false);
                    }
                    if (newDoc == -1) // map once per all field updates, but only if there are any updates
                    {
                        newDoc = holder.DocMap.Map(docUpto);
                    }
                    DocValuesFieldUpdates dvUpdates = dvFieldUpdates[idx];
                    dvUpdates.Add(newDoc, updatesIter.Value());
                    updatesIter.NextDoc(); // advance to next document
                }
                else
                {
                    Debug.Assert(updatesIter.Doc() > curDoc, "field=" + mergingFields[idx] + " updateDoc=" + updatesIter.Doc() + " curDoc=" + curDoc);
                }
            }
        }

        /// <summary>
        /// Carefully merges deletes and updates for the segments we just merged. this
        /// is tricky because, although merging will clear all deletes (compacts the
        /// documents) and compact all the updates, new deletes and updates may have
        /// been flushed to the segments since the merge was started. this method
        /// "carries over" such new deletes and updates onto the newly merged segment,
        /// and saves the resulting deletes and updates files (incrementing the delete
        /// and DV generations for merge.info). If no deletes were flushed, no new
        /// deletes file is saved.
        /// </summary>
        private ReadersAndUpdates CommitMergedDeletesAndUpdates(MergePolicy.OneMerge merge, MergeState mergeState)
        {
            lock (this)
            {
                var tpResult = TestPoint("startCommitMergeDeletes");
                Debug.Assert(tpResult);

                IList<SegmentCommitInfo> sourceSegments = merge.Segments;

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "commitMergeDeletes " + SegString(merge.Segments));
                }

                // Carefully merge deletes that occurred after we
                // started merging:
                int docUpto = 0;
                long minGen = long.MaxValue;

                // Lazy init (only when we find a delete to carry over):
                MergedDeletesAndUpdates holder = new MergedDeletesAndUpdates();
                DocValuesFieldUpdates.Container mergedDVUpdates = new DocValuesFieldUpdates.Container();

                for (int i = 0; i < sourceSegments.Count; i++)
                {
                    SegmentCommitInfo info = sourceSegments[i];
                    minGen = Math.Min(info.BufferedDeletesGen, minGen);
                    int docCount = info.Info.DocCount;
                    Bits prevLiveDocs = merge.Readers[i].LiveDocs;
                    ReadersAndUpdates rld = readerPool.Get(info, false);
                    // We hold a ref so it should still be in the pool:
                    Debug.Assert(rld != null, "seg=" + info.Info.Name);
                    Bits currentLiveDocs = rld.LiveDocs;
                    IDictionary<string, DocValuesFieldUpdates> mergingFieldUpdates = rld.MergingFieldUpdates;
                    string[] mergingFields;
                    DocValuesFieldUpdates[] dvFieldUpdates;
                    DocValuesFieldUpdates.Iterator[] updatesIters;
                    if (mergingFieldUpdates.Count == 0)
                    {
                        mergingFields = null;
                        updatesIters = null;
                        dvFieldUpdates = null;
                    }
                    else
                    {
                        mergingFields = new string[mergingFieldUpdates.Count];
                        dvFieldUpdates = new DocValuesFieldUpdates[mergingFieldUpdates.Count];
                        updatesIters = new DocValuesFieldUpdates.Iterator[mergingFieldUpdates.Count];
                        int idx = 0;
                        foreach (KeyValuePair<string, DocValuesFieldUpdates> e in mergingFieldUpdates)
                        {
                            string field = e.Key;
                            DocValuesFieldUpdates updates = e.Value;
                            mergingFields[idx] = field;
                            dvFieldUpdates[idx] = mergedDVUpdates.GetUpdates(field, updates.Type);
                            if (dvFieldUpdates[idx] == null)
                            {
                                dvFieldUpdates[idx] = mergedDVUpdates.NewUpdates(field, updates.Type, mergeState.SegmentInfo.DocCount);
                            }
                            updatesIters[idx] = updates.GetIterator();
                            updatesIters[idx].NextDoc(); // advance to first update doc
                            ++idx;
                        }
                    }
                    //      System.out.println("[" + Thread.currentThread().getName() + "] IW.commitMergedDeletes: info=" + info + ", mergingUpdates=" + mergingUpdates);

                    if (prevLiveDocs != null)
                    {
                        // If we had deletions on starting the merge we must
                        // still have deletions now:
                        Debug.Assert(currentLiveDocs != null);
                        Debug.Assert(prevLiveDocs.Length() == docCount);
                        Debug.Assert(currentLiveDocs.Length() == docCount);

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
                            // this means this segment received new deletes
                            // since we started the merge, so we
                            // must merge them:
                            for (int j = 0; j < docCount; j++)
                            {
                                if (!prevLiveDocs.Get(j))
                                {
                                    Debug.Assert(!currentLiveDocs.Get(j));
                                }
                                else
                                {
                                    if (!currentLiveDocs.Get(j))
                                    {
                                        if (holder.MergedDeletesAndUpdates_Renamed == null || !holder.InitializedWritableLiveDocs)
                                        {
                                            holder.Init(readerPool, merge, mergeState, true);
                                        }
                                        holder.MergedDeletesAndUpdates_Renamed.Delete(holder.DocMap.Map(docUpto));
                                        if (mergingFields != null) // advance all iters beyond the deleted document
                                        {
                                            SkipDeletedDoc(updatesIters, j);
                                        }
                                    }
                                    else if (mergingFields != null)
                                    {
                                        MaybeApplyMergedDVUpdates(merge, mergeState, docUpto, holder, mergingFields, dvFieldUpdates, updatesIters, j);
                                    }
                                    docUpto++;
                                }
                            }
                        }
                        else if (mergingFields != null)
                        {
                            // need to check each non-deleted document if it has any updates
                            for (int j = 0; j < docCount; j++)
                            {
                                if (prevLiveDocs.Get(j))
                                {
                                    // document isn't deleted, check if any of the fields have an update to it
                                    MaybeApplyMergedDVUpdates(merge, mergeState, docUpto, holder, mergingFields, dvFieldUpdates, updatesIters, j);
                                    // advance docUpto for every non-deleted document
                                    docUpto++;
                                }
                                else
                                {
                                    // advance all iters beyond the deleted document
                                    SkipDeletedDoc(updatesIters, j);
                                }
                            }
                        }
                        else
                        {
                            docUpto += info.Info.DocCount - info.DelCount - rld.PendingDeleteCount;
                        }
                    }
                    else if (currentLiveDocs != null)
                    {
                        Debug.Assert(currentLiveDocs.Length() == docCount);
                        // this segment had no deletes before but now it
                        // does:
                        for (int j = 0; j < docCount; j++)
                        {
                            if (!currentLiveDocs.Get(j))
                            {
                                if (holder.MergedDeletesAndUpdates_Renamed == null || !holder.InitializedWritableLiveDocs)
                                {
                                    holder.Init(readerPool, merge, mergeState, true);
                                }
                                holder.MergedDeletesAndUpdates_Renamed.Delete(holder.DocMap.Map(docUpto));
                                if (mergingFields != null) // advance all iters beyond the deleted document
                                {
                                    SkipDeletedDoc(updatesIters, j);
                                }
                            }
                            else if (mergingFields != null)
                            {
                                MaybeApplyMergedDVUpdates(merge, mergeState, docUpto, holder, mergingFields, dvFieldUpdates, updatesIters, j);
                            }
                            docUpto++;
                        }
                    }
                    else if (mergingFields != null)
                    {
                        // no deletions before or after, but there were updates
                        for (int j = 0; j < docCount; j++)
                        {
                            MaybeApplyMergedDVUpdates(merge, mergeState, docUpto, holder, mergingFields, dvFieldUpdates, updatesIters, j);
                            // advance docUpto for every non-deleted document
                            docUpto++;
                        }
                    }
                    else
                    {
                        // No deletes or updates before or after
                        docUpto += info.Info.DocCount;
                    }
                }

                Debug.Assert(docUpto == merge.info.Info.DocCount);

                if (mergedDVUpdates.Any())
                {
                    //      System.out.println("[" + Thread.currentThread().getName() + "] IW.commitMergedDeletes: mergedDeletes.info=" + mergedDeletes.info + ", mergedFieldUpdates=" + mergedFieldUpdates);
                    bool success = false;
                    try
                    {
                        // if any error occurs while writing the field updates we should release
                        // the info, otherwise it stays in the pool but is considered not "live"
                        // which later causes false exceptions in pool.dropAll().
                        // NOTE: currently this is the only place which throws a true
                        // IOException. If this ever changes, we need to extend that try/finally
                        // block to the rest of the method too.
                        holder.MergedDeletesAndUpdates_Renamed.WriteFieldUpdates(directory, mergedDVUpdates);
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            holder.MergedDeletesAndUpdates_Renamed.DropChanges();
                            readerPool.Drop(merge.info);
                        }
                    }
                }

                if (infoStream.IsEnabled("IW"))
                {
                    if (holder.MergedDeletesAndUpdates_Renamed == null)
                    {
                        infoStream.Message("IW", "no new deletes or field updates since merge started");
                    }
                    else
                    {
                        string msg = holder.MergedDeletesAndUpdates_Renamed.PendingDeleteCount + " new deletes";
                        if (mergedDVUpdates.Any())
                        {
                            msg += " and " + mergedDVUpdates.Size() + " new field updates";
                        }
                        msg += " since merge started";
                        infoStream.Message("IW", msg);
                    }
                }

                merge.info.BufferedDeletesGen = minGen;

                return holder.MergedDeletesAndUpdates_Renamed;
            }
        }

        private bool CommitMerge(MergePolicy.OneMerge merge, MergeState mergeState)
        {
            lock (this)
            {
                var tpResult = TestPoint("startCommitMerge");
                Debug.Assert(tpResult);

                if (HitOOM)
                {
                    throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot complete merge");
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "commitMerge: " + SegString(merge.Segments) + " index=" + SegString());
                }

                Debug.Assert(merge.RegisterDone);

                // If merge was explicitly aborted, or, if rollback() or
                // rollbackTransaction() had been called since our merge
                // started (which results in an unqualified
                // deleter.refresh() call that will remove any index
                // file that current segments does not reference), we
                // abort this merge
                if (merge.Aborted)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "commitMerge: skip: it was aborted");
                    }
                    // In case we opened and pooled a reader for this
                    // segment, drop it now.  this ensures that we close
                    // the reader before trying to delete any of its
                    // files.  this is not a very big deal, since this
                    // reader will never be used by any NRT reader, and
                    // another thread is currently running close(false)
                    // so it will be dropped shortly anyway, but not
                    // doing this  makes  MockDirWrapper angry in
                    // TestNRTThreads (LUCENE-5434):
                    readerPool.Drop(merge.info);
                    Deleter.DeleteNewFiles(merge.info.Files());
                    return false;
                }

                ReadersAndUpdates mergedUpdates = merge.info.Info.DocCount == 0 ? null : CommitMergedDeletesAndUpdates(merge, mergeState);
                //    System.out.println("[" + Thread.currentThread().getName() + "] IW.commitMerge: mergedDeletes=" + mergedDeletes);

                // If the doc store we are using has been closed and
                // is in now compound format (but wasn't when we
                // started), then we will switch to the compound
                // format as well:

                Debug.Assert(!segmentInfos.Contains(merge.info));

                bool allDeleted = merge.Segments.Count == 0 || merge.info.Info.DocCount == 0 || (mergedUpdates != null && mergedUpdates.PendingDeleteCount == merge.info.Info.DocCount);

                if (infoStream.IsEnabled("IW"))
                {
                    if (allDeleted)
                    {
                        infoStream.Message("IW", "merged segment " + merge.info + " is 100% deleted" + (KeepFullyDeletedSegments_Renamed ? "" : "; skipping insert"));
                    }
                }

                bool dropSegment = allDeleted && !KeepFullyDeletedSegments_Renamed;

                // If we merged no segments then we better be dropping
                // the new segment:
                Debug.Assert(merge.Segments.Count > 0 || dropSegment);

                Debug.Assert(merge.info.Info.DocCount != 0 || KeepFullyDeletedSegments_Renamed || dropSegment);

                if (mergedUpdates != null)
                {
                    bool success = false;
                    try
                    {
                        if (dropSegment)
                        {
                            mergedUpdates.DropChanges();
                        }
                        // Pass false for assertInfoLive because the merged
                        // segment is not yet live (only below do we commit it
                        // to the segmentInfos):
                        readerPool.Release(mergedUpdates, false);
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            mergedUpdates.DropChanges();
                            readerPool.Drop(merge.info);
                        }
                    }
                }

                // Must do this after readerPool.release, in case an
                // exception is hit e.g. writing the live docs for the
                // merge segment, in which case we need to abort the
                // merge:
                segmentInfos.ApplyMergeChanges(merge, dropSegment);

                if (dropSegment)
                {
                    Debug.Assert(!segmentInfos.Contains(merge.info));
                    readerPool.Drop(merge.info);
                    Deleter.DeleteNewFiles(merge.info.Files());
                }

                bool success_ = false;
                try
                {
                    // Must close before checkpoint, otherwise IFD won't be
                    // able to delete the held-open files from the merge
                    // readers:
                    CloseMergeReaders(merge, false);
                    success_ = true;
                }
                finally
                {
                    // Must note the change to segmentInfos so any commits
                    // in-flight don't lose it (IFD will incRef/protect the
                    // new files we created):
                    if (success_)
                    {
                        Checkpoint();
                    }
                    else
                    {
                        try
                        {
                            Checkpoint();
                        }
                        catch (Exception)
                        {
                            // Ignore so we keep throwing original exception.
                        }
                    }
                }

                Deleter.DeletePendingFiles();

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "after commitMerge: " + SegString());
                }

                if (merge.MaxNumSegments != -1 && !dropSegment)
                {
                    // cascade the forceMerge:
                    if (!SegmentsToMerge.ContainsKey(merge.info))
                    {
                        SegmentsToMerge[merge.info] = false;
                    }
                }

                return true;
            }
        }

        private void HandleMergeException(Exception t, MergePolicy.OneMerge merge)
        {
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "handleMergeException: merge=" + SegString(merge.Segments) + " exc=" + t);
            }

            // Set the exception on the merge, so if
            // forceMerge is waiting on us it sees the root
            // cause exception:
            merge.Exception = t;
            AddMergeException(merge);

            if ((t as MergePolicy.MergeAbortedException) != null)
            {
                // We can ignore this exception (it happens when
                // close(false) or rollback is called), unless the
                // merge involves segments from external directories,
                // in which case we must throw it so, for example, the
                // rollbackTransaction code in addIndexes* is
                // executed.
                if (merge.IsExternal)
                {
                    throw t;
                }
            }
            else
            {
                IOUtils.ReThrow(t);
            }
        }

        /// <summary>
        /// Merges the indicated segments, replacing them in the stack with a
        /// single segment.
        ///
        /// @lucene.experimental
        /// </summary>
        public virtual void Merge(MergePolicy.OneMerge merge)
        {
            bool success = false;

            long t0 = Environment.TickCount;

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
                            infoStream.Message("IW", "now merge\n  merge=" + SegString(merge.Segments) + "\n  index=" + SegString());
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
                                Deleter.Refresh(merge.info.Info.Name);
                            }
                        }

                        // this merge (and, generally, any change to the
                        // segments) may now enable new merges, so we call
                        // merge policy & update pending merges.
                        if (success && !merge.Aborted && (merge.MaxNumSegments != -1 || (!closed && !Closing)))
                        {
                            UpdatePendingMerges(MergeTrigger.MERGE_FINISHED, merge.MaxNumSegments);
                        }
                    }
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "merge");
            }
            if (merge.info != null && !merge.Aborted)
            {
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "merge time " + (Environment.TickCount - t0) + " msec for " + merge.info.Info.DocCount + " docs");
                }
            }
        }

        /// <summary>
        /// Hook that's called when the specified merge is complete. </summary>
        internal virtual void MergeSuccess(MergePolicy.OneMerge merge)
        {
        }

        /// <summary>
        /// Checks whether this merge involves any segments
        ///  already participating in a merge.  If not, this merge
        ///  is "registered", meaning we record that its segments
        ///  are now participating in a merge, and true is
        ///  returned.  Else (the merge conflicts) false is
        ///  returned.
        /// </summary>
        internal bool RegisterMerge(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                if (merge.RegisterDone)
                {
                    return true;
                }
                Debug.Assert(merge.Segments.Count > 0);

                if (StopMerges)
                {
                    merge.Abort();
                    throw new MergePolicy.MergeAbortedException("merge is aborted: " + SegString(merge.Segments));
                }

                bool isExternal = false;
                foreach (SegmentCommitInfo info in merge.Segments)
                {
                    if (mergingSegments.Contains(info))
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "reject merge " + SegString(merge.Segments) + ": segment " + SegString(info) + " is already marked for merge");
                        }
                        return false;
                    }
                    if (!segmentInfos.Contains(info))
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "reject merge " + SegString(merge.Segments) + ": segment " + SegString(info) + " does not exist in live infos");
                        }
                        return false;
                    }
                    if (info.Info.Dir != directory)
                    {
                        isExternal = true;
                    }
                    if (SegmentsToMerge.ContainsKey(info))
                    {
                        merge.MaxNumSegments = MergeMaxNumSegments;
                    }
                }

                EnsureValidMerge(merge);

                PendingMerges.AddLast(merge);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "add merge to pendingMerges: " + SegString(merge.Segments) + " [total " + PendingMerges.Count + " pending]");
                }

                merge.MergeGen = MergeGen;
                merge.IsExternal = isExternal;

                // OK it does not conflict; now record that this merge
                // is running (while synchronized) to avoid race
                // condition where two conflicting merges from different
                // threads, start
                if (infoStream.IsEnabled("IW"))
                {
                    StringBuilder builder = new StringBuilder("registerMerge merging= [");
                    foreach (SegmentCommitInfo info in mergingSegments)
                    {
                        builder.Append(info.Info.Name).Append(", ");
                    }
                    builder.Append("]");
                    // don't call mergingSegments.toString() could lead to ConcurrentModException
                    // since merge updates the segments FieldInfos
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", builder.ToString());
                    }
                }
                foreach (SegmentCommitInfo info in merge.Segments)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "registerMerge info=" + SegString(info));
                    }
                    mergingSegments.Add(info);
                }

                Debug.Assert(merge.EstimatedMergeBytes == 0);
                Debug.Assert(merge.TotalMergeBytes == 0);
                foreach (SegmentCommitInfo info in merge.Segments)
                {
                    if (info.Info.DocCount > 0)
                    {
                        int delCount = NumDeletedDocs(info);
                        Debug.Assert(delCount <= info.Info.DocCount);
                        double delRatio = ((double)delCount) / info.Info.DocCount;
                        merge.EstimatedMergeBytes += (long)(info.SizeInBytes() * (1.0 - delRatio));
                        merge.TotalMergeBytes += info.SizeInBytes();
                    }
                }

                // Merge is now registered
                merge.RegisterDone = true;

                return true;
            }
        }

        /// <summary>
        /// Does initial setup for a merge, which is fast but holds
        ///  the synchronized lock on IndexWriter instance.
        /// </summary>
        internal void MergeInit(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                bool success = false;
                try
                {
                    _mergeInit(merge);
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

        private void _mergeInit(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                var testPointResult = TestPoint("startMergeInit");
                Debug.Assert(testPointResult);

                Debug.Assert(merge.RegisterDone);
                Debug.Assert(merge.MaxNumSegments == -1 || merge.MaxNumSegments > 0);

                if (HitOOM)
                {
                    throw new InvalidOperationException("this writer hit an OutOfMemoryError; cannot merge");
                }

                if (merge.info != null)
                {
                    // mergeInit already done
                    return;
                }

                if (merge.Aborted)
                {
                    return;
                }

                // TODO: in the non-pool'd case this is somewhat
                // wasteful, because we open these readers, close them,
                // and then open them again for merging.  Maybe  we
                // could pre-pool them somehow in that case...

                // Lock order: IW -> BD
                BufferedUpdatesStream.ApplyDeletesResult result = BufferedUpdatesStream.ApplyDeletesAndUpdates(readerPool, merge.Segments);

                if (result.AnyDeletes)
                {
                    Checkpoint();
                }

                if (!KeepFullyDeletedSegments_Renamed && result.AllDeleted != null)
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "drop 100% deleted segments: " + result.AllDeleted);
                    }
                    foreach (SegmentCommitInfo info in result.AllDeleted)
                    {
                        segmentInfos.Remove(info);
                        if (merge.Segments.Contains(info))
                        {
                            mergingSegments.Remove(info);
                            merge.Segments.Remove(info);
                        }
                        readerPool.Drop(info);
                    }
                    Checkpoint();
                }

                // Bind a new segment name here so even with
                // ConcurrentMergePolicy we keep deterministic segment
                // names.
                string mergeSegmentName = NewSegmentName();
                SegmentInfo si = new SegmentInfo(directory, Constants.LUCENE_MAIN_VERSION, mergeSegmentName, -1, false, Codec, null);
                IDictionary<string, string> details = new Dictionary<string, string>();
                details["mergeMaxNumSegments"] = "" + merge.MaxNumSegments;
                details["mergeFactor"] = Convert.ToString(merge.Segments.Count);
                SetDiagnostics(si, SOURCE_MERGE, details);
                merge.Info = new SegmentCommitInfo(si, 0, -1L, -1L);

                //    System.out.println("[" + Thread.currentThread().getName() + "] IW._mergeInit: " + segString(merge.segments) + " into " + si);

                // Lock order: IW -> BD
                BufferedUpdatesStream.Prune(segmentInfos);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "merge seg=" + merge.info.Info.Name + " " + SegString(merge.Segments));
                }
            }
        }

        internal static void SetDiagnostics(SegmentInfo info, string source)
        {
            SetDiagnostics(info, source, null);
        }

        private static void SetDiagnostics(SegmentInfo info, string source, IDictionary<string, string> details)
        {
            IDictionary<string, string> diagnostics = new Dictionary<string, string>();
            diagnostics["source"] = source;
            diagnostics["lucene.version"] = Constants.LUCENE_VERSION;
            diagnostics["os"] = Constants.OS_NAME;
            diagnostics["os.arch"] = Constants.OS_ARCH;
            diagnostics["os.version"] = Constants.OS_VERSION;
            diagnostics["java.version"] = Constants.JAVA_VERSION;
            diagnostics["java.vendor"] = Constants.JAVA_VENDOR;
            diagnostics["timestamp"] = Convert.ToString((DateTime.Now));
            if (details != null)
            {
                diagnostics.PutAll(details);
            }
            info.Diagnostics = diagnostics;
        }

        /// <summary>
        /// Does fininishing for a merge, which is fast but holds
        ///  the synchronized lock on IndexWriter instance.
        /// </summary>
        public void MergeFinish(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                // forceMerge, addIndexes or finishMerges may be waiting
                // on merges to finish.
                Monitor.PulseAll(this);

                // It's possible we are called twice, eg if there was an
                // exception inside mergeInit
                if (merge.RegisterDone)
                {
                    IList<SegmentCommitInfo> sourceSegments = merge.Segments;
                    foreach (SegmentCommitInfo info in sourceSegments)
                    {
                        mergingSegments.Remove(info);
                    }
                    merge.RegisterDone = false;
                }

                RunningMerges.Remove(merge);
            }
        }

        private void CloseMergeReaders(MergePolicy.OneMerge merge, bool suppressExceptions)
        {
            lock (this)
            {
                int numSegments = merge.Readers.Count;
                Exception th = null;

                bool drop = !suppressExceptions;

                for (int i = 0; i < numSegments; i++)
                {
                    SegmentReader sr = merge.Readers[i];
                    if (sr != null)
                    {
                        try
                        {
                            ReadersAndUpdates rld = readerPool.Get(sr.SegmentInfo, false);
                            // We still hold a ref so it should not have been removed:
                            Debug.Assert(rld != null);
                            if (drop)
                            {
                                rld.DropChanges();
                            }
                            else
                            {
                                rld.DropMergingUpdates();
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
                        merge.Readers[i] = null;
                    }
                }

                // If any error occured, throw it.
                if (!suppressExceptions)
                {
                    IOUtils.ReThrow(th);
                }
            }
        }

        /// <summary>
        /// Does the actual (time-consuming) work of the merge,
        ///  but without holding synchronized lock on IndexWriter
        ///  instance
        /// </summary>
        private int MergeMiddle(MergePolicy.OneMerge merge)
        {
            merge.CheckAborted(directory);

            string mergedName = merge.info.Info.Name;

            IList<SegmentCommitInfo> sourceSegments = merge.Segments;

            IOContext context = new IOContext(merge.MergeInfo);

            MergeState.CheckAbort checkAbort = new MergeState.CheckAbort(merge, directory);
            TrackingDirectoryWrapper dirWrapper = new TrackingDirectoryWrapper(directory);

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "merging " + SegString(merge.Segments));
            }

            merge.Readers = new List<SegmentReader>();

            // this is try/finally to make sure merger's readers are
            // closed:
            bool success = false;
            try
            {
                int segUpto = 0;
                while (segUpto < sourceSegments.Count)
                {
                    SegmentCommitInfo info = sourceSegments[segUpto];

                    // Hold onto the "live" reader; we will use this to
                    // commit merged deletes
                    ReadersAndUpdates rld = readerPool.Get(info, true);

                    // Carefully pull the most recent live docs and reader
                    SegmentReader reader;
                    Bits liveDocs;
                    int delCount;

                    lock (this)
                    {
                        // Must sync to ensure BufferedDeletesStream cannot change liveDocs,
                        // pendingDeleteCount and field updates while we pull a copy:
                        reader = rld.GetReaderForMerge(context);
                        liveDocs = rld.ReadOnlyLiveDocs;
                        delCount = rld.PendingDeleteCount + info.DelCount;

                        Debug.Assert(reader != null);
                        Debug.Assert(rld.VerifyDocCounts());

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
                        Debug.Assert(delCount > reader.NumDeletedDocs); // beware of zombies

                        SegmentReader newReader = new SegmentReader(info, reader, liveDocs, info.Info.DocCount - delCount);
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

                    merge.Readers.Add(reader);
                    Debug.Assert(delCount <= info.Info.DocCount, "delCount=" + delCount + " info.docCount=" + info.Info.DocCount + " rld.pendingDeleteCount=" + rld.PendingDeleteCount + " info.getDelCount()=" + info.DelCount);
                    segUpto++;
                }

                //      System.out.println("[" + Thread.currentThread().getName() + "] IW.mergeMiddle: merging " + merge.getMergeReaders());

                // we pass merge.getMergeReaders() instead of merge.readers to allow the
                // OneMerge to return a view over the actual segments to merge
                SegmentMerger merger = new SegmentMerger(merge.MergeReaders, merge.info.Info, infoStream, dirWrapper, Config_Renamed.TermIndexInterval, checkAbort, GlobalFieldNumberMap, context, Config_Renamed.CheckIntegrityAtMerge);

                merge.CheckAborted(directory);

                // this is where all the work happens:
                MergeState mergeState;
                bool success3 = false;
                try
                {
                    if (!merger.ShouldMerge())
                    {
                        // would result in a 0 document segment: nothing to merge!
                        mergeState = new MergeState(new List<AtomicReader>(), merge.info.Info, infoStream, checkAbort);
                    }
                    else
                    {
                        mergeState = merger.Merge();
                    }
                    success3 = true;
                }
                finally
                {
                    if (!success3)
                    {
                        lock (this)
                        {
                            Deleter.Refresh(merge.info.Info.Name);
                        }
                    }
                }
                Debug.Assert(mergeState.SegmentInfo == merge.info.Info);
                merge.info.Info.Files = new HashSet<string>(dirWrapper.CreatedFiles);

                // Record which codec was used to write the segment

                if (infoStream.IsEnabled("IW"))
                {
                    if (merge.info.Info.DocCount == 0)
                    {
                        infoStream.Message("IW", "merge away fully deleted segments");
                    }
                    else
                    {
                        infoStream.Message("IW", "merge codec=" + Codec + " docCount=" + merge.info.Info.DocCount + "; merged segment has " + (mergeState.FieldInfos.HasVectors() ? "vectors" : "no vectors") + "; " + (mergeState.FieldInfos.HasNorms() ? "norms" : "no norms") + "; " + (mergeState.FieldInfos.HasDocValues() ? "docValues" : "no docValues") + "; " + (mergeState.FieldInfos.HasProx() ? "prox" : "no prox") + "; " + (mergeState.FieldInfos.HasProx() ? "freqs" : "no freqs"));
                    }
                }

                // Very important to do this before opening the reader
                // because codec must know if prox was written for
                // this segment:
                //System.out.println("merger set hasProx=" + merger.hasProx() + " seg=" + merge.info.name);
                bool useCompoundFile;
                lock (this) // Guard segmentInfos
                {
                    useCompoundFile = mergePolicy.UseCompoundFile(segmentInfos, merge.info);
                }

                if (useCompoundFile)
                {
                    success = false;

                    ICollection<string> filesToRemove = merge.info.Files();

                    try
                    {
                        filesToRemove = CreateCompoundFile(infoStream, directory, checkAbort, merge.info.Info, context);
                        success = true;
                    }
                    catch (System.IO.IOException ioe)
                    {
                        lock (this)
                        {
                            if (merge.Aborted)
                            {
                                // this can happen if rollback or close(false)
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
                                Deleter.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(mergedName, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_EXTENSION));
                                Deleter.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(mergedName, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION));
                                Deleter.DeleteNewFiles(merge.info.Files());
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
                        Deleter.DeleteNewFiles(filesToRemove);

                        if (merge.Aborted)
                        {
                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "abort merge after building CFS");
                            }
                            Deleter.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(mergedName, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_EXTENSION));
                            Deleter.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(mergedName, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION));
                            return 0;
                        }
                    }

                    merge.info.Info.UseCompoundFile = true;
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
                    Codec.SegmentInfoFormat().SegmentInfoWriter.Write(directory, merge.info.Info, mergeState.FieldInfos, context);
                    success2 = true;
                }
                finally
                {
                    if (!success2)
                    {
                        lock (this)
                        {
                            Deleter.DeleteNewFiles(merge.info.Files());
                        }
                    }
                }

                // TODO: ideally we would freeze merge.info here!!
                // because any changes after writing the .si will be
                // lost...

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", string.Format(CultureInfo.InvariantCulture, "merged segment size=%.3f MB vs estimate=%.3f MB", merge.info.SizeInBytes() / 1024.0 / 1024.0, merge.EstimatedMergeBytes / 1024 / 1024.0));
                }

                IndexReaderWarmer mergedSegmentWarmer = Config_Renamed.MergedSegmentWarmer;
                if (PoolReaders && mergedSegmentWarmer != null && merge.info.Info.DocCount != 0)
                {
                    ReadersAndUpdates rld = readerPool.Get(merge.info, true);
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
                    // commitMerge will return false if this merge was
                    // aborted
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

            return merge.info.Info.DocCount;
        }

        internal virtual void AddMergeException(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                Debug.Assert(merge.Exception != null);
                if (!MergeExceptions.Contains(merge) && MergeGen == merge.MergeGen)
                {
                    MergeExceptions.Add(merge);
                }
            }
        }

        // For test purposes.
        public int BufferedDeleteTermsSize
        {
            get
            {
                return DocWriter.BufferedDeleteTermsSize;
            }
        }

        // For test purposes.
        public int NumBufferedDeleteTerms
        {
            get
            {
                return DocWriter.NumBufferedDeleteTerms;
            }
        }

        // utility routines for tests
        public virtual SegmentCommitInfo NewestSegment()
        {
            lock (this)
            {
                return segmentInfos.Size() > 0 ? segmentInfos.Info(segmentInfos.Size() - 1) : null;
            }
        }

        /// <summary>
        /// Returns a string description of all segments, for
        ///  debugging.
        ///
        /// @lucene.internal
        /// </summary>
        public virtual string SegString()
        {
            lock (this)
            {
                return SegString(segmentInfos.Segments);
            }
        }

        /// <summary>
        /// Returns a string description of the specified
        ///  segments, for debugging.
        ///
        /// @lucene.internal
        /// </summary>
        public virtual string SegString(IEnumerable<SegmentCommitInfo> infos)
        {
            lock (this)
            {
                StringBuilder buffer = new StringBuilder();
                foreach (SegmentCommitInfo info in infos)
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

        /// <summary>
        /// Returns a string description of the specified
        ///  segment, for debugging.
        ///
        /// @lucene.internal
        /// </summary>
        public virtual string SegString(SegmentCommitInfo info)
        {
            lock (this)
            {
                return info.ToString(info.Info.Dir, NumDeletedDocs(info) - info.DelCount);
            }
        }

        private void DoWait()
        {
            //TODO: conniey
            //lock (this)
            //{
            //    // NOTE: the callers of this method should in theory
            //    // be able to do simply wait(), but, as a defense
            //    // against thread timing hazards where notifyAll()
            //    // fails to be called, we wait for at most 1 second
            //    // and then return so caller can check if wait
            //    // conditions are satisfied:
            //    try
            //    {
            //        Monitor.Wait(this, TimeSpan.FromMilliseconds(1000));
            //    }
            //    catch (ThreadInterruptedException ie)
            //    {
            //        throw new ThreadInterruptedException("Thread Interrupted Exception", ie);
            //    }
            //}
        }

        private bool KeepFullyDeletedSegments_Renamed;

        /// <summary>
        /// Only for testing.
        ///
        /// @lucene.internal
        /// </summary>
        public virtual bool KeepFullyDeletedSegments
        {
            set
            {
                KeepFullyDeletedSegments_Renamed = value;
            }
            get
            {
                return KeepFullyDeletedSegments_Renamed;
            }
        }

        // called only from assert
        private bool FilesExist(SegmentInfos toSync)
        {
            ICollection<string> files = toSync.Files(directory, false);
            foreach (String fileName in files)
            {
                Debug.Assert(SlowFileExists(directory, fileName), "file " + fileName + " does not exist; files=" + Arrays.ToString(directory.ListAll()));
                // If this trips it means we are missing a call to
                // .checkpoint somewhere, because by the time we
                // are called, deleter should know about every
                // file referenced by the current head
                // segmentInfos:
                Debug.Assert(Deleter.Exists(fileName), "IndexFileDeleter doesn't know about file " + fileName);
            }
            return true;
        }

        // For infoStream output
        internal virtual SegmentInfos ToLiveInfos(SegmentInfos sis)
        {
            lock (this)
            {
                SegmentInfos newSIS = new SegmentInfos();
                IDictionary<SegmentCommitInfo, SegmentCommitInfo> liveSIS = new Dictionary<SegmentCommitInfo, SegmentCommitInfo>();
                foreach (SegmentCommitInfo info in segmentInfos.Segments)
                {
                    liveSIS[info] = info;
                }
                foreach (SegmentCommitInfo info in sis.Segments)
                {
                    SegmentCommitInfo infoMod = info;
                    SegmentCommitInfo liveInfo;
                    if (liveSIS.TryGetValue(info, out liveInfo))
                    {
                        infoMod = liveInfo;
                    }
                    newSIS.Add(infoMod);
                }

                return newSIS;
            }
        }

        /// <summary>
        /// Walk through all files referenced by the current
        ///  segmentInfos and ask the Directory to sync each file,
        ///  if it wasn't already.  If that succeeds, then we
        ///  prepare a new segments_N file but do not fully commit
        ///  it.
        /// </summary>
        private void StartCommit(SegmentInfos toSync)
        {
            var tpResult = TestPoint("startStartCommit");
            Debug.Assert(tpResult);
            Debug.Assert(PendingCommit == null);

            if (HitOOM)
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
                    Debug.Assert(LastCommitChangeCount <= ChangeCount, "lastCommitChangeCount=" + LastCommitChangeCount + " changeCount=" + ChangeCount);

                    if (PendingCommitChangeCount == LastCommitChangeCount)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "  skip startCommit(): no changes pending");
                        }
                        Deleter.DecRef(FilesToCommit);
                        FilesToCommit = null;
                        return;
                    }

                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "startCommit index=" + SegString(ToLiveInfos(toSync).Segments) + " changeCount=" + ChangeCount);
                    }

                    Debug.Assert(FilesExist(toSync));
                }

                tpResult = TestPoint("midStartCommit");
                Debug.Assert(tpResult);

                bool pendingCommitSet = false;

                try
                {
                    tpResult = TestPoint("midStartCommit2");
                    Debug.Assert(tpResult);

                    lock (this)
                    {
                        Debug.Assert(PendingCommit == null);

                        Debug.Assert(segmentInfos.Generation == toSync.Generation);

                        // Exception here means nothing is prepared
                        // (this method unwinds everything it did on
                        // an exception)
                        toSync.PrepareCommit(directory);
                        //System.out.println("DONE prepareCommit");

                        pendingCommitSet = true;
                        PendingCommit = toSync;
                    }

                    // this call can take a long time -- 10s of seconds
                    // or more.  We do it without syncing on this:
                    bool success = false;
                    ICollection<string> filesToSync;
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
                            PendingCommit = null;
                            toSync.RollbackCommit(directory);
                        }
                    }

                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "done all syncs: " + filesToSync);
                    }

                    tpResult = TestPoint("midStartCommitSuccess");
                    Debug.Assert(tpResult);
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
                            Deleter.DecRef(FilesToCommit);
                            FilesToCommit = null;
                        }
                    }
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                HandleOOM(oom, "startCommit");
            }
            tpResult = TestPoint("finishStartCommit");
            Debug.Assert(tpResult);
        }

        /// <summary>
        /// Returns <code>true</code> iff the index in the named directory is
        /// currently locked. </summary>
        /// <param name="directory"> the directory to check for a lock </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public static bool IsLocked(Directory directory)
        {
            return directory.MakeLock(WRITE_LOCK_NAME).Locked;
        }

        /// <summary>
        /// Forcibly unlocks the index in the named directory.
        /// <P>
        /// Caution: this should only be used by failure recovery code,
        /// when it is known that no other process nor thread is in fact
        /// currently accessing this index.
        /// </summary>
        public static void Unlock(Directory directory)
        {
            directory.MakeLock(IndexWriter.WRITE_LOCK_NAME).Release();
        }

        /// <summary>
        /// If <seealso cref="DirectoryReader#open(IndexWriter,boolean)"/> has
        ///  been called (ie, this writer is in near real-time
        ///  mode), then after a merge completes, this class can be
        ///  invoked to warm the reader on the newly merged
        ///  segment, before the merge commits.  this is not
        ///  required for near real-time search, but will reduce
        ///  search latency on opening a new near real-time reader
        ///  after a merge completes.
        ///
        /// @lucene.experimental
        ///
        /// <p><b>NOTE</b>: warm is called before any deletes have
        /// been carried over to the merged segment.
        /// </summary>
        public abstract class IndexReaderWarmer
        {
            /// <summary>
            /// Sole constructor. (For invocation by subclass
            ///  constructors, typically implicit.)
            /// </summary>
            protected internal IndexReaderWarmer()
            {
            }

            /// <summary>
            /// Invoked on the <seealso cref="AtomicReader"/> for the newly
            ///  merged segment, before that segment is made visible
            ///  to near-real-time readers.
            /// </summary>
            public abstract void Warm(AtomicReader reader);
        }

        private void HandleOOM(System.OutOfMemoryException oom, string location)
        {
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "hit OutOfMemoryError inside " + location);
            }
            HitOOM = true;
            throw oom;
        }

        // Used only by assert for testing.  Current points:
        //   startDoFlush
        //   startCommitMerge
        //   startStartCommit
        //   midStartCommit
        //   midStartCommit2
        //   midStartCommitSuccess
        //   finishStartCommit
        //   startCommitMergeDeletes
        //   startMergeInit
        //   DocumentsWriter.ThreadState.init start
        private bool TestPoint(string message)
        {
            if (infoStream.IsEnabled("TP"))
            {
                infoStream.Message("TP", message);
            }
            return true;
        }

        internal virtual bool NrtIsCurrent(SegmentInfos infos)
        {
            lock (this)
            {
                //System.out.println("IW.nrtIsCurrent " + (infos.version == segmentInfos.version && !docWriter.anyChanges() && !bufferedDeletesStream.any()));
                EnsureOpen();
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "nrtIsCurrent: infoVersion matches: " + (infos.Version == segmentInfos.Version) + "; DW changes: " + DocWriter.AnyChanges() + "; BD changes: " + BufferedUpdatesStream.Any());
                }
                return infos.Version == segmentInfos.Version && !DocWriter.AnyChanges() && !BufferedUpdatesStream.Any();
            }
        }

        public virtual bool Closed
        {
            get
            {
                lock (this)
                {
                    return closed;
                }
            }
        }

        /// <summary>
        /// Expert: remove any index files that are no longer
        ///  used.
        ///
        ///  <p> IndexWriter normally deletes unused files itself,
        ///  during indexing.  However, on Windows, which disallows
        ///  deletion of open files, if there is a reader open on
        ///  the index then those files cannot be deleted.  this is
        ///  fine, because IndexWriter will periodically retry
        ///  the deletion.</p>
        ///
        ///  <p> However, IndexWriter doesn't try that often: only
        ///  on open, close, flushing a new segment, and finishing
        ///  a merge.  If you don't do any of these actions with your
        ///  IndexWriter, you'll see the unused files linger.  If
        ///  that's a problem, call this method to delete them
        ///  (once you've closed the open readers that were
        ///  preventing their deletion).
        ///
        ///  <p> In addition, you can call this method to delete
        ///  unreferenced index commits. this might be useful if you
        ///  are using an <seealso cref="IndexDeletionPolicy"/> which holds
        ///  onto index commits until some criteria are met, but those
        ///  commits are no longer needed. Otherwise, those commits will
        ///  be deleted the next time commit() is called.
        /// </summary>
        public virtual void DeleteUnusedFiles()
        {
            lock (this)
            {
                EnsureOpen(false);
                Deleter.DeletePendingFiles();
                Deleter.RevisitPolicy();
            }
        }

        private void DeletePendingFiles()
        {
            lock (this)
            {
                Deleter.DeletePendingFiles();
            }
        }

        /// <summary>
        /// NOTE: this method creates a compound file for all files returned by
        /// info.files(). While, generally, this may include separate norms and
        /// deletion files, this SegmentInfo must not reference such files when this
        /// method is called, because they are not allowed within a compound file.
        /// </summary>
        public static ICollection<string> CreateCompoundFile(InfoStream infoStream, Directory directory, CheckAbort checkAbort, SegmentInfo info, IOContext context)
        {
            string fileName = Index.IndexFileNames.SegmentFileName(info.Name, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_EXTENSION);
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "create compound file " + fileName);
            }
            Debug.Assert(Lucene3xSegmentInfoFormat.GetDocStoreOffset(info) == -1);
            // Now merge all added files
            ICollection<string> files = info.Files;
            CompoundFileDirectory cfsDir = new CompoundFileDirectory(directory, fileName, context, true);
            IOException prior = null;
            try
            {
                foreach (string file in files)
                {
                    directory.Copy(cfsDir, file, file, context);
                    checkAbort.Work(directory.FileLength(file));
                }
            }
            catch (System.IO.IOException ex)
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
                        catch (Exception)
                        {
                        }
                        try
                        {
                            directory.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(info.Name, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            // Replace all previous files with the CFS/CFE files:
            HashSet<string> siFiles = new HashSet<string>();
            siFiles.Add(fileName);
            siFiles.Add(Lucene.Net.Index.IndexFileNames.SegmentFileName(info.Name, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION));
            info.Files = siFiles;

            return files;
        }

        /// <summary>
        /// Tries to delete the given files if unreferenced </summary>
        /// <param name="files"> the files to delete </param>
        /// <exception cref="IOException"> if an <seealso cref="IOException"/> occurs </exception>
        /// <seealso cref= IndexFileDeleter#deleteNewFiles(Collection) </seealso>
        internal void DeleteNewFiles(ICollection<string> files)
        {
            lock (this)
            {
                Deleter.DeleteNewFiles(files);
            }
        }

        /// <summary>
        /// Cleans up residuals from a segment that could not be entirely flushed due to an error </summary>
        /// <seealso cref= IndexFileDeleter#refresh(String)  </seealso>
        internal void FlushFailed(SegmentInfo info)
        {
            lock (this)
            {
                Deleter.Refresh(info.Name);
            }
        }

        internal int Purge(bool forced)
        {
            return DocWriter.PurgeBuffer(this, forced);
        }

        internal void ApplyDeletesAndPurge(bool forcePurge)
        {
            try
            {
                Purge(forcePurge);
            }
            finally
            {
                ApplyAllDeletesAndUpdates();
                flushCount.IncrementAndGet();
            }
        }

        internal void DoAfterSegmentFlushed(bool triggerMerge, bool forcePurge)
        {
            try
            {
                Purge(forcePurge);
            }
            finally
            {
                if (triggerMerge)
                {
                    MaybeMerge(MergeTrigger.SEGMENT_FLUSH, UNBOUNDED_MAX_MERGE_SEGMENTS);
                }
            }
        }

        internal virtual void IncRefDeleter(SegmentInfos segmentInfos)
        {
            lock (this)
            {
                EnsureOpen();
                Deleter.IncRef(segmentInfos, false);
            }
        }

        internal virtual void DecRefDeleter(SegmentInfos segmentInfos)
        {
            lock (this)
            {
                EnsureOpen();
                Deleter.DecRef(segmentInfos);
            }
        }

        private bool ProcessEvents(bool triggerMerge, bool forcePurge)
        {
            return ProcessEvents(eventQueue, triggerMerge, forcePurge);
        }

        private bool ProcessEvents(ConcurrentQueue<Event> queue, bool triggerMerge, bool forcePurge)
        {
            Event @event;
            bool processed = false;
            //while ((@event = queue.RemoveFirst()) != null)
            while (queue.TryDequeue(out @event))
            {
                processed = true;
                @event.Process(this, triggerMerge, forcePurge);
            }
            return processed;
        }

        /// <summary>
        /// Interface for internal atomic events. See <seealso cref="DocumentsWriter"/> for details. Events are executed concurrently and no order is guaranteed.
        /// Each event should only rely on the serializeability within it's process method. All actions that must happen before or after a certain action must be
        /// encoded inside the <seealso cref="#process(IndexWriter, boolean, boolean)"/> method.
        ///
        /// </summary>
        public interface Event
        {
            /// <summary>
            /// Processes the event. this method is called by the <seealso cref="IndexWriter"/>
            /// passed as the first argument.
            /// </summary>
            /// <param name="writer">
            ///          the <seealso cref="IndexWriter"/> that executes the event. </param>
            /// <param name="triggerMerge">
            ///          <code>false</code> iff this event should not trigger any segment merges </param>
            /// <param name="clearBuffers">
            ///          <code>true</code> iff this event should clear all buffers associated with the event. </param>
            /// <exception cref="IOException">
            ///           if an <seealso cref="IOException"/> occurs </exception>
            void Process(IndexWriter writer, bool triggerMerge, bool clearBuffers);
        }

        /// <summary>
        /// Used only by asserts: returns true if the file exists
        ///  (can be opened), false if it cannot be opened, and
        ///  (unlike Java's File.exists) throws IOException if
        ///  there's some unexpected error.
        /// </summary>
        private static bool SlowFileExists(Directory dir, string fileName)
        {
            /*
            try
            {
                dir.OpenInput(fileName, IOContext.DEFAULT).Dispose();
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }*/
            return dir.FileExists(fileName);
        }
    }
}