using J2N;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
    using Constants = Lucene.Net.Util.Constants;
    using Directory = Lucene.Net.Store.Directory;
    using FieldNumbers = Lucene.Net.Index.FieldInfos.FieldNumbers;
    using IBits = Lucene.Net.Util.IBits;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using Lock = Lucene.Net.Store.Lock;
    using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
    using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;
    using Lucene3xSegmentInfoFormat = Lucene.Net.Codecs.Lucene3x.Lucene3xSegmentInfoFormat;
    using MergeInfo = Lucene.Net.Store.MergeInfo;
    using Query = Lucene.Net.Search.Query;
    using TrackingDirectoryWrapper = Lucene.Net.Store.TrackingDirectoryWrapper;

    /// <summary>
    /// An <see cref="IndexWriter"/> creates and maintains an index.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="OpenMode"/> option on
    /// <see cref="IndexWriterConfig.OpenMode"/> determines
    /// whether a new index is created, or whether an existing index is
    /// opened. Note that you can open an index with <see cref="OpenMode.CREATE"/>
    /// even while readers are using the index. The old readers will
    /// continue to search the "point in time" snapshot they had opened,
    /// and won't see the newly created index until they re-open. If
    /// <see cref="OpenMode.CREATE_OR_APPEND"/> is used <see cref="IndexWriter"/> will create a
    /// new index if there is not already an index at the provided path
    /// and otherwise open the existing index.</para>
    ///
    /// <para>In either case, documents are added with <see cref="AddDocument(IEnumerable{IIndexableField})"/>
    /// and removed with <see cref="DeleteDocuments(Term)"/> or
    /// <see cref="DeleteDocuments(Query)"/>. A document can be updated with
    /// <see cref="UpdateDocument(Term, IEnumerable{IIndexableField})"/> (which just deletes
    /// and then adds the entire document). When finished adding, deleting
    /// and updating documents, <see cref="Dispose()"/> should be called.</para>
    ///
    /// <a name="flush"></a>
    /// <para>These changes are buffered in memory and periodically
    /// flushed to the <see cref="Store.Directory"/> (during the above method
    /// calls). A flush is triggered when there are enough added documents
    /// since the last flush. Flushing is triggered either by RAM usage of the
    /// documents (see <see cref="LiveIndexWriterConfig.RAMBufferSizeMB"/>) or the
    /// number of added documents (see <see cref="LiveIndexWriterConfig.MaxBufferedDocs"/>).
    /// The default is to flush when RAM usage hits
    /// <see cref="IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB"/> MB. For
    /// best indexing speed you should flush by RAM usage with a
    /// large RAM buffer. Additionally, if <see cref="IndexWriter"/> reaches the configured number of
    /// buffered deletes (see <see cref="LiveIndexWriterConfig.MaxBufferedDeleteTerms"/>)
    /// the deleted terms and queries are flushed and applied to existing segments.
    /// In contrast to the other flush options <see cref="LiveIndexWriterConfig.RAMBufferSizeMB"/> and
    /// <see cref="LiveIndexWriterConfig.MaxBufferedDocs"/>, deleted terms
    /// won't trigger a segment flush. Note that flushing just moves the
    /// internal buffered state in <see cref="IndexWriter"/> into the index, but
    /// these changes are not visible to <see cref="IndexReader"/> until either
    /// <see cref="Commit()"/> or <see cref="Dispose()"/> is called.  A flush may
    /// also trigger one or more segment merges which by default
    /// run with a background thread so as not to block the
    /// addDocument calls (see <a href="#mergePolicy">below</a>
    /// for changing the <see cref="mergeScheduler"/>).</para>
    ///
    /// <para>Opening an <see cref="IndexWriter"/> creates a lock file for the directory in use. Trying to open
    /// another <see cref="IndexWriter"/> on the same directory will lead to a
    /// <see cref="LockObtainFailedException"/>. The <see cref="LockObtainFailedException"/>
    /// is also thrown if an <see cref="IndexReader"/> on the same directory is used to delete documents
    /// from the index.</para>
    ///
    /// <a name="deletionPolicy"></a>
    /// <para>Expert: <see cref="IndexWriter"/> allows an optional
    /// <see cref="IndexDeletionPolicy"/> implementation to be
    /// specified.  You can use this to control when prior commits
    /// are deleted from the index.  The default policy is
    /// <see cref="KeepOnlyLastCommitDeletionPolicy"/> which removes all prior
    /// commits as soon as a new commit is done (this matches
    /// behavior before 2.2).  Creating your own policy can allow
    /// you to explicitly keep previous "point in time" commits
    /// alive in the index for some time, to allow readers to
    /// refresh to the new commit without having the old commit
    /// deleted out from under them.  This is necessary on
    /// filesystems like NFS that do not support "delete on last
    /// close" semantics, which Lucene's "point in time" search
    /// normally relies on. </para>
    ///
    /// <a name="mergePolicy"></a> <para>Expert:
    /// <see cref="IndexWriter"/> allows you to separately change
    /// the <see cref="mergePolicy"/> and the <see cref="mergeScheduler"/>.
    /// The <see cref="mergePolicy"/> is invoked whenever there are
    /// changes to the segments in the index.  Its role is to
    /// select which merges to do, if any, and return a 
    /// <see cref="MergePolicy.MergeSpecification"/> describing the merges.
    /// The default is <see cref="LogByteSizeMergePolicy"/>.  Then, the 
    /// <see cref="MergeScheduler"/> is invoked with the requested merges and
    /// it decides when and how to run the merges.  The default is
    /// <see cref="ConcurrentMergeScheduler"/>. </para>
    ///
    /// <a name="OOME"></a><para><b>NOTE</b>: if you hit an
    /// <see cref="OutOfMemoryException"/> then <see cref="IndexWriter"/> will quietly record this
    /// fact and block all future segment commits.  This is a
    /// defensive measure in case any internal state (buffered
    /// documents and deletions) were corrupted.  Any subsequent
    /// calls to <see cref="Commit()"/> will throw an
    /// <see cref="InvalidOperationException"/>.  The only course of action is to
    /// call <see cref="Dispose()"/>, which internally will call
    /// <see cref="Rollback()"/>, to undo any changes to the index since the
    /// last commit.  You can also just call <see cref="Rollback()"/>
    /// directly.</para>
    ///
    /// <a name="thread-safety"></a><para><b>NOTE</b>: 
    /// <see cref="IndexWriter"/> instances are completely thread
    /// safe, meaning multiple threads can call any of its
    /// methods, concurrently.  If your application requires
    /// external synchronization, you should <b>not</b>
    /// synchronize on the <see cref="IndexWriter"/> instance as
    /// this may cause deadlock; use your own (non-Lucene) objects
    /// instead. </para>
    ///
    /// <para><b>NOTE</b>:
    /// Do not use <see cref="Thread.Interrupt()"/> on a thread that's within
    /// <see cref="IndexWriter"/>, as .NET will throw <see cref="ThreadInterruptedException"/> on any
    /// wait, sleep, or join including any lock statement with contention on it.
    /// As a result, it is not practical to try to support <see cref="Thread.Interrupt()"/> due to the
    /// chance <see cref="ThreadInterruptedException"/> could potentially be thrown in the middle of a
    /// <see cref="Commit()"/> or somewhere in the application that will cause a deadlock.</para>
    /// <para>
    /// We recommend using another shutdown mechanism to safely cancel a parallel operation.
    /// See: <a href="https://github.com/apache/lucenenet/issues/526">https://github.com/apache/lucenenet/issues/526</a>.
    /// </para>
    /// </remarks>

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

    public class IndexWriter : IDisposable, ITwoPhaseCommit
    {
        private const int UNBOUNDED_MAX_MERGE_SEGMENTS = -1;

        /// <summary>
        /// Name of the write lock in the index.
        /// </summary>
        public static readonly string WRITE_LOCK_NAME = "write.lock";

        /// <summary>
        /// Key for the source of a segment in the <see cref="SegmentInfo.Diagnostics"/>. </summary>
        public static readonly string SOURCE = "source";

        /// <summary>
        /// Source of a segment which results from a merge of other segments. </summary>
        public static readonly string SOURCE_MERGE = "merge";

        /// <summary>
        /// Source of a segment which results from a flush. </summary>
        public static readonly string SOURCE_FLUSH = "flush";

        /// <summary>
        /// Source of a segment which results from a call to <see cref="AddIndexes(IndexReader[])"/>. </summary>
        public static readonly string SOURCE_ADDINDEXES_READERS = "AddIndexes(params IndexReader[] readers)";

        /// <summary>
        /// Absolute hard maximum length for a term, in bytes once
        /// encoded as UTF8.  If a term arrives from the analyzer
        /// longer than this length, an
        /// <see cref="ArgumentException"/> is thrown
        /// and a message is printed to <see cref="infoStream"/>, if set (see
        /// <see cref="IndexWriterConfig.SetInfoStream(InfoStream)"/>).
        /// </summary>
        public static readonly int MAX_TERM_LENGTH = DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8;

        private volatile bool hitOOM;

        private readonly Directory directory; // where this index resides
        private readonly Analyzer analyzer; // how to analyze text

        private long changeCount; // increments every time a change is completed
        private long lastCommitChangeCount; // last changeCount that was committed

        private IList<SegmentCommitInfo> rollbackSegments; // list of segmentInfo we will fallback to if the commit fails

        internal volatile SegmentInfos pendingCommit; // set when a commit is pending (after prepareCommit() & before commit())
        internal long pendingCommitChangeCount;

        private ICollection<string> filesToCommit;

        internal readonly SegmentInfos segmentInfos; // the segments
        internal readonly FieldNumbers globalFieldNumberMap;

        private readonly DocumentsWriter docWriter;
        private readonly ConcurrentQueue<IEvent> eventQueue;
        internal readonly IndexFileDeleter deleter;

        // used by forceMerge to note those needing merging
        private readonly IDictionary<SegmentCommitInfo, bool> segmentsToMerge = new Dictionary<SegmentCommitInfo, bool>();

        private int mergeMaxNumSegments;

        private Lock writeLock;

        private volatile bool closed;
        private volatile bool closing;

        // Holds all SegmentInfo instances currently involved in
        // merges
        private readonly JCG.HashSet<SegmentCommitInfo> mergingSegments = new JCG.HashSet<SegmentCommitInfo>();

        private readonly MergePolicy mergePolicy;
        private readonly IMergeScheduler mergeScheduler;
        private readonly Queue<MergePolicy.OneMerge> pendingMerges = new Queue<MergePolicy.OneMerge>();
        private readonly JCG.HashSet<MergePolicy.OneMerge> runningMerges = new JCG.HashSet<MergePolicy.OneMerge>();
        private IList<MergePolicy.OneMerge> mergeExceptions = new JCG.List<MergePolicy.OneMerge>();
        private long mergeGen;
        private bool stopMerges;

        internal readonly AtomicInt32 flushCount = new AtomicInt32();
        internal readonly AtomicInt32 flushDeletesCount = new AtomicInt32();

        internal ReaderPool readerPool;
        internal readonly BufferedUpdatesStream bufferedUpdatesStream;

        // this is a "write once" variable (like the organic dye
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

        internal virtual DirectoryReader GetReader()
        {
            return GetReader(true);
        }

        /// <summary>
        /// Expert: returns a readonly reader, covering all
        /// committed as well as un-committed changes to the index.
        /// this provides "near real-time" searching, in that
        /// changes made during an <see cref="IndexWriter"/> session can be
        /// quickly made available for searching without closing
        /// the writer nor calling <see cref="Commit()"/>.
        ///
        /// <para>Note that this is functionally equivalent to calling
        /// Flush() and then opening a new reader.  But the turnaround time of this
        /// method should be faster since it avoids the potentially
        /// costly <see cref="Commit()"/>.</para>
        ///
        /// <para>You must close the <see cref="IndexReader"/> returned by
        /// this method once you are done using it.</para>
        ///
        /// <para>It's <i>near</i> real-time because there is no hard
        /// guarantee on how quickly you can get a new reader after
        /// making changes with <see cref="IndexWriter"/>.  You'll have to
        /// experiment in your situation to determine if it's
        /// fast enough.  As this is a new and experimental
        /// feature, please report back on your findings so we can
        /// learn, improve and iterate.</para>
        ///
        /// <para>The resulting reader supports
        /// <see cref="DirectoryReader.DoOpenIfChanged()"/>, but that call will simply forward
        /// back to this method (though this may change in the
        /// future).</para>
        ///
        /// <para>The very first time this method is called, this
        /// writer instance will make every effort to pool the
        /// readers that it opens for doing merges, applying
        /// deletes, etc.  This means additional resources (RAM,
        /// file descriptors, CPU time) will be consumed.</para>
        ///
        /// <para>For lower latency on reopening a reader, you should
        /// set <see cref="LiveIndexWriterConfig.MergedSegmentWarmer"/> to
        /// pre-warm a newly merged segment before it's committed
        /// to the index.  This is important for minimizing
        /// index-to-search delay after a large merge.  </para>
        ///
        /// <para>If an AddIndexes* call is running in another thread,
        /// then this reader will only search those segments from
        /// the foreign index that have been successfully copied
        /// over, so far.</para>
        ///
        /// <para><b>NOTE</b>: Once the writer is disposed, any
        /// outstanding readers may continue to be used.  However,
        /// if you attempt to reopen any of those readers, you'll
        /// hit an <see cref="ObjectDisposedException"/>.</para>
        ///
        /// @lucene.experimental
        /// </summary>
        /// <returns> <see cref="IndexReader"/> that covers entire index plus all
        /// changes made so far by this <see cref="IndexWriter"/> instance
        /// </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        public virtual DirectoryReader GetReader(bool applyAllDeletes)
        {
            EnsureOpen();

            long tStart = Time.NanoTime() / Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

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
                UninterruptableMonitor.Enter(fullFlushLock);
                try
                {
                    bool success = false;
                    try
                    {
                        anySegmentFlushed = docWriter.FlushAllThreads(this);
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
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            MaybeApplyDeletes(applyAllDeletes);
                            r = StandardDirectoryReader.Open(this, segmentInfos, applyAllDeletes);
                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "return reader version=" + r.Version + " reader=" + r);
                            }
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
                        }
                    }
                    catch (Exception oom) when (oom.IsOutOfMemoryError())
                    {
                        HandleOOM(oom, "GetReader");
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
                        ProcessEvents(false, true);
                        DoAfterFlush();
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(fullFlushLock);
                }
                if (anySegmentFlushed)
                {
                    MaybeMerge(MergeTrigger.FULL_FLUSH, UNBOUNDED_MAX_MERGE_SEGMENTS);
                }
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "getReader took " + ((Time.NanoTime() / Time.MillisecondsPerNanosecond) - tStart) + " msec"); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                }
                success2 = true;
            }
            finally
            {
                if (!success2)
                {
                    IOUtils.DisposeWhileHandlingException(r);
                }
            }
            return r;
        }

        /// <summary>
        /// Holds shared <see cref="SegmentReader"/> instances. <see cref="IndexWriter"/> uses
        /// <see cref="SegmentReader"/>s for 1) applying deletes, 2) doing
        /// merges, 3) handing out a real-time reader.  This pool
        /// reuses instances of the <see cref="SegmentReader"/>s in all these
        /// places if it is in "near real-time mode" (<see cref="GetReader()"/>
        /// has been called on this instance).
        /// </summary>
        internal class ReaderPool : IDisposable
        {
            private readonly IndexWriter outerInstance;

            public ReaderPool(IndexWriter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

#if FEATURE_DICTIONARY_REMOVE_CONTINUEENUMERATION
            private readonly IDictionary<SegmentCommitInfo, ReadersAndUpdates> readerMap = new Dictionary<SegmentCommitInfo, ReadersAndUpdates>();
#else
            // LUCENENET: We use ConcurrentDictionary<TKey, TValue> because Dictionary<TKey, TValue> doesn't support
            // deletion while iterating, but ConcurrentDictionary does.
            private readonly IDictionary<SegmentCommitInfo, ReadersAndUpdates> readerMap = new ConcurrentDictionary<SegmentCommitInfo, ReadersAndUpdates>();
#endif

            // used only by asserts
            public virtual bool InfoIsLive(SegmentCommitInfo info)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    int idx = outerInstance.segmentInfos.IndexOf(info);
                    Debugging.Assert(idx != -1, "info={0} isn't live", info);
                    Debugging.Assert(outerInstance.segmentInfos[idx] == info, "info={0} doesn't match live info in segmentInfos", info);
                    return true;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            public virtual void Drop(SegmentCommitInfo info)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (readerMap.TryGetValue(info, out ReadersAndUpdates rld) && rld != null)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(info == rld.Info);
                        //        System.out.println("[" + Thread.currentThread().getName() + "] ReaderPool.drop: " + info);
                        readerMap.Remove(info);
                        rld.DropReaders();
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            public virtual bool AnyPendingDeletes()
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    foreach (ReadersAndUpdates rld in readerMap.Values)
                    {
                        if (rld.PendingDeleteCount != 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            public virtual void Release(ReadersAndUpdates rld)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    Release(rld, true);
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            public virtual void Release(ReadersAndUpdates rld, bool assertInfoLive)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    // Matches incRef in get:
                    rld.DecRef();

                    // Pool still holds a ref:
                    if (Debugging.AssertsEnabled) Debugging.Assert(rld.RefCount() >= 1);

                    if (!outerInstance.poolReaders && rld.RefCount() == 1)
                    {
                        // this is the last ref to this RLD, and we're not
                        // pooling, so remove it:
                        //        System.out.println("[" + Thread.currentThread().getName() + "] ReaderPool.release: " + rld.info);
                        if (rld.WriteLiveDocs(outerInstance.directory))
                        {
                            // Make sure we only write del docs for a live segment:
                            if (Debugging.AssertsEnabled) Debugging.Assert(assertInfoLive == false || InfoIsLive(rld.Info));
                            // Must checkpoint because we just
                            // created new _X_N.del and field updates files;
                            // don't call IW.checkpoint because that also
                            // increments SIS.version, which we do not want to
                            // do here: it was done previously (after we
                            // invoked BDS.applyDeletes), whereas here all we
                            // did was move the state to disk:
                            outerInstance.CheckpointNoSIS();
                        }
                        //System.out.println("IW: done writeLiveDocs for info=" + rld.info);

                        //        System.out.println("[" + Thread.currentThread().getName() + "] ReaderPool.release: drop readers " + rld.info);
                        rld.DropReaders();
                        readerMap.Remove(rld.Info);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    DropAll(false);
                }
            }

            /// <summary>
            /// Remove all our references to readers, and commits
            /// any pending changes.
            /// </summary>
            internal virtual void DropAll(bool doSave)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    Exception priorE = null;
                    foreach (var pair in readerMap)
                    {
                        ReadersAndUpdates rld = pair.Value;

                        try
                        {
                            if (doSave && rld.WriteLiveDocs(outerInstance.directory)) // Throws IOException
                            {
                                // Make sure we only write del docs and field updates for a live segment:
                                if (Debugging.AssertsEnabled) Debugging.Assert(InfoIsLive(rld.Info));
                                // Must checkpoint because we just
                                // created new _X_N.del and field updates files;
                                // don't call IW.checkpoint because that also
                                // increments SIS.version, which we do not want to
                                // do here: it was done previously (after we
                                // invoked BDS.applyDeletes), whereas here all we
                                // did was move the state to disk:
                                outerInstance.CheckpointNoSIS(); // Throws IOException
                            }
                        }
                        catch (Exception t) when (t.IsThrowable())
                        {
                            if (doSave)
                            {
                                //IOUtils.ReThrow(t);
                                throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                            }
                            else if (priorE is null)
                            {
                                priorE = t;
                            }
                        }

                        // Important to remove as-we-go, not with .clear()
                        // in the end, in case we hit an exception;
                        // otherwise we could over-decref if close() is
                        // called again:
                        readerMap.Remove(pair.Key);

                        // NOTE: it is allowed that these decRefs do not
                        // actually close the SRs; this happens when a
                        // near real-time reader is kept open after the
                        // IndexWriter instance is closed:
                        try
                        {
                            rld.DropReaders(); // Throws IOException
                        }
                        catch (Exception t) when (t.IsThrowable())
                        {
                            if (doSave)
                            {
                                //IOUtils.ReThrow(t);
                                throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                            }
                            else if (priorE is null)
                            {
                                priorE = t;
                            }
                        }
                    }
                    if (Debugging.AssertsEnabled) Debugging.Assert(readerMap.Count == 0);
                    IOUtils.ReThrow(priorE);
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            /// <summary>
            /// Commit live docs changes for the segment readers for
            /// the provided infos.
            /// </summary>
            /// <exception cref="IOException"> If there is a low-level I/O error </exception>
            public virtual void Commit(SegmentInfos infos)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    foreach (SegmentCommitInfo info in infos.Segments)
                    {
                        if (readerMap.TryGetValue(info, out ReadersAndUpdates rld))
                        {
                            if (Debugging.AssertsEnabled) Debugging.Assert(rld.Info == info);
                            if (rld.WriteLiveDocs(outerInstance.directory))
                            {
                                // Make sure we only write del docs for a live segment:
                                if (Debugging.AssertsEnabled) Debugging.Assert(InfoIsLive(info));
                                // Must checkpoint because we just
                                // created new _X_N.del and field updates files;
                                // don't call IW.checkpoint because that also
                                // increments SIS.version, which we do not want to
                                // do here: it was done previously (after we
                                // invoked BDS.applyDeletes), whereas here all we
                                // did was move the state to disk:
                                outerInstance.CheckpointNoSIS();
                            }
                        }
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            /// <summary>
            /// Obtain a <see cref="ReadersAndUpdates"/> instance from the
            /// readerPool.  If <paramref name="create"/> is <c>true</c>, you must later call
            /// <see cref="Release(ReadersAndUpdates)"/>.
            /// </summary>
            public virtual ReadersAndUpdates Get(SegmentCommitInfo info, bool create)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(info.Info.Dir == outerInstance.directory, "info.dir={0} vs {1}", info.Info.Dir, outerInstance.directory);

                    if (!readerMap.TryGetValue(info, out ReadersAndUpdates rld) || rld is null)
                    {
                        if (!create)
                        {
                            return null;
                        }
                        rld = new ReadersAndUpdates(outerInstance, info);
                        // Steal initial reference:
                        readerMap[info] = rld;
                    }
                    else
                    {
                        if (Debugging.AssertsEnabled && !(rld.Info == info))
                            throw AssertionError.Create(string.Format("rld.info={0} info={1} isLive?={2} vs {3}", rld.Info, info, InfoIsLive(rld.Info), InfoIsLive(info)));
                    }

                    if (create)
                    {
                        // Return ref to caller:
                        rld.IncRef();
                    }

                    if (Debugging.AssertsEnabled) Debugging.Assert(NoDups());

                    return rld;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            /// <summary>
            /// Make sure that every segment appears only once in the
            /// pool:
            /// </summary>
            private bool NoDups()
            {
                JCG.HashSet<string> seen = new JCG.HashSet<string>();
                foreach (SegmentCommitInfo info in readerMap.Keys)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(!seen.Contains(info.Info.Name));
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
        /// Used internally to throw an <see cref="ObjectDisposedException"/> if this
        /// <see cref="IndexWriter"/> has been disposed or is in the process of diposing.
        /// </summary>
        /// <param name="failIfDisposing">
        ///          if <c>true</c>, also fail when <see cref="IndexWriter"/> is in the process of
        ///          disposing (<c>closing=true</c>) but not yet done disposing (
        ///          <c>closed=false</c>) </param>
        /// <exception cref="ObjectDisposedException">
        ///           if this IndexWriter is closed or in the process of closing </exception>
        protected internal void EnsureOpen(bool failIfDisposing)
        {
            if (closed || (failIfDisposing && closing))
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this IndexWriter is disposed.");
            }
        }

        /// <summary>
        /// Used internally to throw an
        /// <see cref="ObjectDisposedException"/> if this <see cref="IndexWriter"/> has been
        /// disposed (<c>closed=true</c>) or is in the process of
        /// disposing (<c>closing=true</c>).
        /// <para/>
        /// Calls <see cref="EnsureOpen(bool)"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException"> if this <see cref="IndexWriter"/> is disposed </exception>
        protected internal void EnsureOpen()
        {
            EnsureOpen(true);
        }

        internal readonly Codec codec; // for writing new segments

        /// <summary>
        /// Constructs a new <see cref="IndexWriter"/> per the settings given in <paramref name="conf"/>.
        /// If you want to make "live" changes to this writer instance, use
        /// <see cref="Config"/>.
        ///
        /// <para/>
        /// <b>NOTE:</b> after ths writer is created, the given configuration instance
        /// cannot be passed to another writer. If you intend to do so, you should
        /// <see cref="IndexWriterConfig.Clone()"/> it beforehand.
        /// </summary>
        /// <param name="d">
        ///          the index directory. The index is either created or appended
        ///          according <see cref="IndexWriterConfig.OpenMode"/>. </param>
        /// <param name="conf">
        ///          the configuration settings according to which <see cref="IndexWriter"/> should
        ///          be initialized. </param>
        /// <exception cref="IOException">
        ///           if the directory cannot be read/written to, or if it does not
        ///           exist and <see cref="IndexWriterConfig.OpenMode"/> is
        ///           <see cref="OpenMode.APPEND"/> or if there is any other low-level
        ///           IO error </exception>
        public IndexWriter(Directory d, IndexWriterConfig conf)
        {
            readerPool = new ReaderPool(this);
            conf.SetIndexWriter(this); // prevent reuse by other instances
            config = new LiveIndexWriterConfig(conf);
            directory = d;
            analyzer = config.Analyzer;
            infoStream = config.InfoStream;
            mergePolicy = config.MergePolicy;
            mergePolicy.SetIndexWriter(this);
            mergeScheduler = config.MergeScheduler;
            codec = config.Codec;

            bufferedUpdatesStream = new BufferedUpdatesStream(infoStream);
            poolReaders = config.UseReaderPooling;

            writeLock = directory.MakeLock(WRITE_LOCK_NAME);

            if (!writeLock.Obtain(config.WriteLockTimeout)) // obtain write lock
            {
                throw new LockObtainFailedException("Index locked for write: " + writeLock);
            }

            bool success = false;
            try
            {
                OpenMode? mode = config.OpenMode;
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
                    // Try to read first.  this is to allow create
                    // against an index that's currently open for
                    // searching.  In this case we write the next
                    // segments_N file with no segments:
                    try
                    {
                        segmentInfos.Read(directory);
                        segmentInfos.Clear();
                    }
                    catch (Exception e) when (e.IsIOException())
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

                rollbackSegments = segmentInfos.CreateBackupSegmentInfos();

                // start with previous field numbers, but new FieldInfos
                globalFieldNumberMap = FieldNumberMap;
                config.FlushPolicy.Init(config);
                docWriter = new DocumentsWriter(this, config, directory);
                eventQueue = docWriter.EventQueue;

                // Default deleter (for backwards compatibility) is
                // KeepOnlyLastCommitDeleter:
                UninterruptableMonitor.Enter(this);
                try
                {
                    deleter = new IndexFileDeleter(directory, config.IndexDeletionPolicy, segmentInfos, infoStream, this, initialIndexExists);
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
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
                    IOUtils.DisposeWhileHandlingException(writeLock);
                    writeLock = null;
                }
            }
        }

        /// <summary>
        /// Loads or returns the already loaded the global field number map for <see cref="segmentInfos"/>.
        /// If <see cref="segmentInfos"/> has no global field number map the returned instance is empty
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
        /// Returns a <see cref="LiveIndexWriterConfig"/>, which can be used to query the <see cref="IndexWriter"/>
        /// current settings, as well as modify "live" ones.
        /// </summary>
        public virtual LiveIndexWriterConfig Config
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
                infoStream.Message("IW", "\ndir=" + directory + "\n" + "index=" + SegString() + "\n" + "version=" + Constants.LUCENE_VERSION + "\n" + config.ToString());
            }
        }

        /// <summary>
        /// Commits all changes to an index, waits for pending merges
        /// to complete, and closes all associated files.
        /// <para/>
        /// This is a "slow graceful shutdown" which may take a long time
        /// especially if a big merge is pending: If you only want to close
        /// resources use <see cref="Rollback()"/>. If you only want to commit
        /// pending changes and close resources see <see cref="Dispose(bool)"/>.
        /// <para/>
        /// Note that this may be a costly
        /// operation, so, try to re-use a single writer instead of
        /// closing and opening a new one.  See <see cref="Commit()"/> for
        /// caveats about write caching done by some IO devices.
        ///
        /// <para> If an <see cref="Exception"/> is hit during close, eg due to disk
        /// full or some other reason, then both the on-disk index
        /// and the internal state of the <see cref="IndexWriter"/> instance will
        /// be consistent.  However, the close will not be complete
        /// even though part of it (flushing buffered documents)
        /// may have succeeded, so the write lock will still be
        /// held.</para>
        ///
        /// <para> If you can correct the underlying cause (eg free up
        /// some disk space) then you can call <see cref="Dispose()"/> again.
        /// Failing that, if you want to force the write lock to be
        /// released (dangerous, because you may then lose buffered
        /// docs in the <see cref="IndexWriter"/> instance) then you can do
        /// something like this:</para>
        ///
        /// <code>
        /// try 
        /// {
        ///     writer.Dispose();
        /// } 
        /// finally 
        /// {
        ///     if (IndexWriter.IsLocked(directory)) 
        ///     {
        ///         IndexWriter.Unlock(directory);
        ///     }
        /// }
        /// </code>
        /// 
        /// after which, you must be certain not to use the writer
        /// instance anymore.
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer, again.  See 
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dispose()
        {
            Dispose(disposing: true, waitForMerges: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the index with or without waiting for currently
        /// running merges to finish.  This is only meaningful when
        /// using a <see cref="MergeScheduler"/> that runs merges in background
        /// threads.
        ///
        /// <para><b>NOTE</b>: If this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer, again.  See 
        /// <see cref="IndexWriter"/> for details.</para>
        ///
        /// <para><b>NOTE</b>: It is dangerous to always call
        /// <c>Dispose(false)</c>, especially when <see cref="IndexWriter"/> is not open
        /// for very long, because this can result in "merge
        /// starvation" whereby long merges will never have a
        /// chance to finish.  This will cause too many segments in
        /// your index over time.</para>
        ///
        /// <para><b>NOTE</b>: This overload should not be called when implementing a finalizer.
        /// Instead, call <see cref="Dispose(bool, bool)"/> with <c>disposing</c> set to
        /// <c>false</c> and <c>waitForMerges</c> set to <c>true</c>.</para>
        /// </summary>
        /// <param name="waitForMerges"> If <c>true</c>, this call will block
        /// until all merges complete; else, it will ask all
        /// running merges to abort, wait until those merges have
        /// finished (which should be at most a few seconds), and
        /// then return. </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "This is Lucene's alternate path to Dispose() and we must suppress the finalizer here.")]
        [SuppressMessage("Usage", "S2953:Methods named \"Dispose\" should implement \"IDisposable.Dispose\"", Justification = "This is Lucene's alternate path to Dispose() and we must suppress the finalizer here.")]
        [SuppressMessage("Usage", "S3971:\"GC.SuppressFinalize\" should not be called", Justification = "This is Lucene's alternate path to Dispose() and we must suppress the finalizer here.")]
        public void Dispose(bool waitForMerges)
        {
            Dispose(disposing: true, waitForMerges);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the index with or without waiting for currently
        /// running merges to finish.  This is only meaningful when
        /// using a <see cref="MergeScheduler"/> that runs merges in background
        /// threads.
        ///
        /// <para>This call will block
        /// until all merges complete; else, it will ask all
        /// running merges to abort, wait until those merges have
        /// finished (which should be at most a few seconds), and
        /// then return.
        /// </para>
        ///
        /// <para><b>NOTE</b>: Always be sure to call <c>base.Dispose(disposing, waitForMerges)</c>
        /// when overriding this method.</para>
        ///
        /// <para><b>NOTE</b>: When implementing a finalizer in a subclass, this overload should be called
        /// with <paramref name="disposing"/> set to <c>false</c> and <paramref name="waitForMerges"/>
        /// set to <c>true</c>.</para>
        ///
        /// <para><b>NOTE</b>: If this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer, again.  See 
        /// <see cref="IndexWriter"/> for details.</para>
        ///
        /// <para><b>NOTE</b>: It is dangerous to always call
        /// with <paramref name="waitForMerges"/> set to <c>false</c>,
        /// especially when <see cref="IndexWriter"/> is not open
        /// for very long, because this can result in "merge
        /// starvation" whereby long merges will never have a
        /// chance to finish.  This will cause too many segments in
        /// your index over time.</para>
        /// </summary>
        /// <param name="waitForMerges"> If <c>true</c>, this call will block
        /// until all merges complete; else, it will ask all
        /// running merges to abort, wait until those merges have
        /// finished (which should be at most a few seconds), and
        /// then return. </param>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources. </param>
        // LUCENENET specific - Added this overload to allow subclasses to dispose resoruces
        // in one place without also having to override Dispose(bool).
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected virtual void Dispose(bool disposing, bool waitForMerges)
        {
            if (disposing)
            {
                Close(waitForMerges);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Close(bool waitForMerges)
        {
            // Ensure that only one thread actually gets to do the
            // closing, and make sure no commit is also in progress:
            UninterruptableMonitor.Enter(commitLock);
            try
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
                        if (Debugging.AssertsEnabled) Debugging.Assert(AssertEventQueueAfterClose());
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(commitLock);
            }
        }

        private bool AssertEventQueueAfterClose()
        {
            if (eventQueue.IsEmpty)
            {
                return true;
            }
            foreach (IEvent e in eventQueue)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(e is DocumentsWriter.MergePendingEvent, "{0}", e);
            }
            return true;
        }

        /// <summary>
        /// Returns <c>true</c> if this thread should attempt to close, or
        /// false if IndexWriter is now closed; else, waits until
        /// another thread finishes closing
        /// </summary>
        private bool ShouldClose()
        {
            UninterruptableMonitor.Enter(this);
            try
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
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void CloseInternal(bool waitForMerges, bool doFlush)
        {
            bool interrupted = false;
            try
            {
                if (pendingCommit != null)
                {
                    throw IllegalStateException.Create("cannot close: prepareCommit was already called with no corresponding call to commit");
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
                        docWriter.Abort(this); // already closed -- never sync on IW
                    }
                }
                finally
                {
                    try
                    {
                        // LUCENENET specific - Java calls Thread.interrupted(), which resets and returns the
                        // initial "interrupted status". .NET has no such method. However, following the logic
                        // carefully below, we call Thread.CurrentThread.Interrupted() if interrupted is true.
                        // If the current thread is already in "interrupted status", there is no reason to call
                        // Thread.CurrentThread.Interrupted() since it is already in that state.

                        // clean up merge scheduler in all cases, although flushing may have failed:
                        //interrupted = ThreadJob.Interrupted();

                        if (waitForMerges)
                        {
                            try
                            {
                                // Give merge scheduler last chance to run, in case
                                // any pending merges are waiting:
                                mergeScheduler.Merge(this, MergeTrigger.CLOSING, false);
                            }
                            catch (Util.ThreadInterruptedException)
                            {
                                // ignore any interruption, does not matter
                                interrupted = true;
                                if (infoStream.IsEnabled("IW"))
                                {
                                    infoStream.Message("IW", "interrupted while waiting for final merges");
                                }
                            }
                        }

                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            for (; ; )
                            {
                                try
                                {
                                    FinishMerges(waitForMerges && !interrupted);
                                    break;
                                }
                                catch (Util.ThreadInterruptedException)
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
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
                        }
                    }
                    finally
                    {
                        // shutdown policy, scheduler and all threads (this call is not interruptible):
                        IOUtils.DisposeWhileHandlingException(mergePolicy, mergeScheduler);
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
                UninterruptableMonitor.Enter(this);
                try
                {
                    // commitInternal calls ReaderPool.commit, which
                    // writes any pending liveDocs from ReaderPool, so
                    // it's safe to drop all readers now:
                    readerPool.DropAll(true);
                    deleter.Dispose();
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "at close: " + SegString());
                }

                if (writeLock != null)
                {
                    writeLock.Dispose(); // release write lock
                    writeLock = null;
                }
                UninterruptableMonitor.Enter(this);
                try
                {
                    closed = true;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
                if (Debugging.AssertsEnabled)
                {
                    // LUCENENET specific - store the number of states so we don't have to call this method twice
                    int numDeactivatedThreadStates = docWriter.perThreadPool.NumDeactivatedThreadStates();
                    Debugging.Assert(numDeactivatedThreadStates == docWriter.perThreadPool.MaxThreadStates, "{0} {1}", numDeactivatedThreadStates, docWriter.perThreadPool.MaxThreadStates);
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "CloseInternal");
            }
            finally
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    closing = false;
                    UninterruptableMonitor.PulseAll(this);
                    if (!closed)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "hit exception while closing");
                        }
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
                // finally, restore interrupt status:
                if (interrupted)
                {
                    Thread.CurrentThread.Interrupt();
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="Store.Directory"/> used by this index. </summary>
        public virtual Directory Directory => directory;

        /// <summary>
        /// Gets the analyzer used by this index. </summary>
        public virtual Analyzer Analyzer
        {
            get
            {
                EnsureOpen();
                return analyzer;
            }
        }

        /// <summary>
        /// Gets total number of docs in this index, including
        /// docs not yet flushed (still in the RAM buffer),
        /// not counting deletions.
        /// </summary>
        /// <seealso cref="NumDocs"/>
        public virtual int MaxDoc
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    EnsureOpen();
                    return docWriter.NumDocs + segmentInfos.TotalDocCount;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Gets total number of docs in this index, including
        /// docs not yet flushed (still in the RAM buffer), and
        /// including deletions.  <b>NOTE:</b> buffered deletions
        /// are not counted.  If you really need these to be
        /// counted you should call <see cref="Commit()"/> first.
        /// </summary>
        /// <seealso cref="MaxDoc"/>
        public virtual int NumDocs // LUCENENET NOTE: This is not a great candidate for a property, but changing because IndexReader has a property with the same name
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    EnsureOpen();
                    int count = docWriter.NumDocs;
                    foreach (SegmentCommitInfo info in segmentInfos)
                    {
                        count += info.Info.DocCount - NumDeletedDocs(info);
                    }
                    return count;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> if this index has deletions (including
        /// buffered deletions).  Note that this will return <c>true</c>
        /// if there are buffered Term/Query deletions, even if it
        /// turns out those buffered deletions don't match any
        /// documents. Also, if a merge kicked off as a result of flushing a
        /// </summary>
        public virtual bool HasDeletions()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                EnsureOpen();
                if (bufferedUpdatesStream.Any())
                {
                    return true;
                }
                if (docWriter.AnyDeletions())
                {
                    return true;
                }
                if (readerPool.AnyPendingDeletes())
                {
                    return true;
                }
                foreach (SegmentCommitInfo info in segmentInfos.Segments)
                {
                    if (info.HasDeletions)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Adds a document to this index.
        ///
        /// <para> Note that if an <see cref="Exception"/> is hit (for example disk full)
        /// then the index will be consistent, but this document
        /// may not have been added.  Furthermore, it's possible
        /// the index will have one segment in non-compound format
        /// even when using compound files (when a merge has
        /// partially succeeded).</para>
        ///
        /// <para>This method periodically flushes pending documents
        /// to the <see cref="Directory"/> (see <see cref="IndexWriter"/>), and
        /// also periodically triggers segment merges in the index
        /// according to the <see cref="MergePolicy"/> in use.</para>
        ///
        /// <para>Merges temporarily consume space in the
        /// directory. The amount of space required is up to 1X the
        /// size of all segments being merged, when no
        /// readers/searchers are open against the index, and up to
        /// 2X the size of all segments being merged when
        /// readers/searchers are open against the index (see
        /// <see cref="ForceMerge(int)"/> for details). The sequence of
        /// primitive merge operations performed is governed by the
        /// merge policy.</para>
        ///
        /// <para>Note that each term in the document can be no longer
        /// than <see cref="MAX_TERM_LENGTH"/> in bytes, otherwise an
        /// <see cref="ArgumentException"/> will be thrown.</para>
        ///
        /// <para>Note that it's possible to create an invalid Unicode
        /// string in java if a UTF16 surrogate pair is malformed.
        /// In this case, the invalid characters are silently
        /// replaced with the Unicode replacement character
        /// U+FFFD.</para>
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See 
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void AddDocument(IEnumerable<IIndexableField> doc)
        {
            AddDocument(doc, analyzer);
        }

        /// <summary>
        /// Adds a document to this index, using the provided <paramref name="analyzer"/> instead of the
        /// value of <see cref="Analyzer"/>.
        ///
        /// <para>See <see cref="AddDocument(IEnumerable{IIndexableField})"/> for details on
        /// index and <see cref="IndexWriter"/> state after an <see cref="Exception"/>, and
        /// flushing/merging temporary free space requirements.</para>
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void AddDocument(IEnumerable<IIndexableField> doc, Analyzer analyzer)
        {
            UpdateDocument(null, doc, analyzer);
        }

        /// <summary>
        /// Atomically adds a block of documents with sequentially
        /// assigned document IDs, such that an external reader
        /// will see all or none of the documents.
        ///
        /// <para><b>WARNING</b>: the index does not currently record
        /// which documents were added as a block.  Today this is
        /// fine, because merging will preserve a block. The order of
        /// documents within a segment will be preserved, even when child
        /// documents within a block are deleted. Most search features
        /// (like result grouping and block joining) require you to
        /// mark documents; when these documents are deleted these
        /// search features will not work as expected. Obviously adding
        /// documents to an existing block will require you the reindex
        /// the entire block.</para>
        ///
        /// <para>However it's possible that in the future Lucene may
        /// merge more aggressively re-order documents (for example,
        /// perhaps to obtain better index compression), in which case
        /// you may need to fully re-index your documents at that time.</para>
        ///
        /// <para>See <see cref="AddDocument(IEnumerable{IIndexableField})"/> for details on
        /// index and <see cref="IndexWriter"/> state after an <see cref="Exception"/>, and
        /// flushing/merging temporary free space requirements.</para>
        ///
        /// <para><b>NOTE</b>: tools that do offline splitting of an index
        /// (for example, IndexSplitter in Lucene.Net.Misc) or
        /// re-sorting of documents (for example, IndexSorter in
        /// contrib) are not aware of these atomically added documents
        /// and will likely break them up.  Use such tools at your
        /// own risk!</para>
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        /// 
        /// @lucene.experimental 
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            AddDocuments(docs, analyzer);
        }

        /// <summary>
        /// Atomically adds a block of documents, analyzed using the
        /// provided <paramref name="analyzer"/>, with sequentially assigned document
        /// IDs, such that an external reader will see all or none
        /// of the documents.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer analyzer)
        {
            UpdateDocuments(null, docs, analyzer);
        }

        /// <summary>
        /// Atomically deletes documents matching the provided
        /// <paramref name="delTerm"/> and adds a block of documents with sequentially
        /// assigned document IDs, such that an external reader
        /// will see all or none of the documents.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <seealso cref="AddDocuments(IEnumerable{IEnumerable{IIndexableField}})"/>
        public virtual void UpdateDocuments(Term delTerm, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            UpdateDocuments(delTerm, docs, analyzer);
        }

        /// <summary>
        /// Atomically deletes documents matching the provided
        /// <paramref name="delTerm"/> and adds a block of documents, analyzed using
        /// the provided <paramref name="analyzer"/>, with sequentially
        /// assigned document IDs, such that an external reader
        /// will see all or none of the documents.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <seealso cref="AddDocuments(IEnumerable{IEnumerable{IIndexableField}})"/>
        public virtual void UpdateDocuments(Term delTerm, IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer analyzer)
        {
            EnsureOpen();
            try
            {
                bool success = false;
                try
                {
                    if (docWriter.UpdateDocuments(docs, analyzer, delTerm))
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
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "UpdateDocuments");
            }
        }

        /// <summary>
        /// Deletes the document(s) containing <paramref name="term"/>.
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See 
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        /// <param name="term"> the term to identify the documents to be deleted </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void DeleteDocuments(Term term)
        {
            EnsureOpen();
            try
            {
                if (docWriter.DeleteTerms(term))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "DeleteDocuments(Term)");
            }
        }

        /// <summary>
        /// Expert: attempts to delete by document ID, as long as
        /// the provided <paramref name="readerIn"/> is a near-real-time reader (from 
        /// <see cref="DirectoryReader.Open(IndexWriter, bool)"/>.  If the
        /// provided <paramref name="readerIn"/> is an NRT reader obtained from this
        /// writer, and its segment has not been merged away, then
        /// the delete succeeds and this method returns <c>true</c>; else, it
        /// returns <c>false</c> the caller must then separately delete by
        /// Term or Query.
        ///
        /// <b>NOTE</b>: this method can only delete documents
        /// visible to the currently open NRT reader.  If you need
        /// to delete documents indexed after opening the NRT
        /// reader you must use the other DeleteDocument() methods
        /// (e.g., <see cref="DeleteDocuments(Term)"/>).
        /// </summary>
        public virtual bool TryDeleteDocument(IndexReader readerIn, int docID)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (readerIn is not AtomicReader reader)
                {
                    // Composite reader: lookup sub-reader and re-base docID:
                    IList<AtomicReaderContext> leaves = readerIn.Leaves;
                    int subIndex = ReaderUtil.SubIndex(docID, leaves);
                    reader = leaves[subIndex].AtomicReader;
                    docID -= leaves[subIndex].DocBase;
                    if (Debugging.AssertsEnabled)
                    {
                        Debugging.Assert(docID >= 0);
                        Debugging.Assert(docID < reader.MaxDoc);
                    }
                }
                // else: Reader is already atomic: use the incoming docID

                if (reader is not SegmentReader segmentReader)
                {
                    throw new ArgumentException("the reader must be a SegmentReader or composite reader containing only SegmentReaders");
                }

                SegmentCommitInfo info = segmentReader.SegmentInfo;

                // TODO: this is a slow linear search, but, number of
                // segments should be contained unless something is
                // seriously wrong w/ the index, so it should be a minor
                // cost:

                if (segmentInfos.IndexOf(info) != -1)
                {
                    ReadersAndUpdates rld = readerPool.Get(info, false);
                    if (rld != null)
                    {
                        UninterruptableMonitor.Enter(bufferedUpdatesStream);
                        try
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
                        finally
                        {
                            UninterruptableMonitor.Exit(bufferedUpdatesStream);
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
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Deletes the document(s) containing any of the
        /// terms. All given deletes are applied and flushed atomically
        /// at the same time.
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
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
                if (docWriter.DeleteTerms(terms))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "DeleteDocuments(Term..)");
            }
        }

        /// <summary>
        /// Deletes the document(s) matching the provided query.
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        /// <param name="query"> the query to identify the documents to be deleted </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void DeleteDocuments(Query query)
        {
            EnsureOpen();
            try
            {
                if (docWriter.DeleteQueries(query))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "DeleteDocuments(Query)");
            }
        }

        /// <summary>
        /// Deletes the document(s) matching any of the provided queries.
        /// All given deletes are applied and flushed atomically at the same time.
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
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
                if (docWriter.DeleteQueries(queries))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "DeleteDocuments(Query..)");
            }
        }

        /// <summary>
        /// Updates a document by first deleting the document(s)
        /// containing <paramref name="term"/> and then adding the new
        /// document.  The delete and then add are atomic as seen
        /// by a reader on the same index (flush may happen only after
        /// the add).
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        /// <param name="term"> the term to identify the document(s) to be
        /// deleted </param>
        /// <param name="doc"> the document to be added </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void UpdateDocument(Term term, IEnumerable<IIndexableField> doc)
        {
            EnsureOpen();
            UpdateDocument(term, doc, analyzer);
        }

        /// <summary>
        /// Updates a document by first deleting the document(s)
        /// containing <paramref name="term"/> and then adding the new
        /// document.  The delete and then add are atomic as seen
        /// by a reader on the same index (flush may happen only after
        /// the add).
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        /// <param name="term"> the term to identify the document(s) to be
        /// deleted </param>
        /// <param name="doc"> the document to be added </param>
        /// <param name="analyzer"> the analyzer to use when analyzing the document </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void UpdateDocument(Term term, IEnumerable<IIndexableField> doc, Analyzer analyzer)
        {
            EnsureOpen();
            try
            {
                bool success = false;
                try
                {
                    if (docWriter.UpdateDocument(doc, analyzer, term))
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
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "UpdateDocument");
            }
        }

        /// <summary>
        /// Updates a document's <see cref="NumericDocValues"/> for <paramref name="field"/> to the
        /// given <paramref name="value"/>. This method can be used to 'unset' a document's
        /// value by passing <c>null</c> as the new <paramref name="value"/>. Also, you can only update
        /// fields that already exist in the index, not add new fields through this
        /// method.
        ///
        /// <para>
        /// <b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/> you should immediately
        /// dispose the writer. See <see cref="IndexWriter"/> for details.
        /// </para>
        /// </summary>
        /// <param name="term">
        ///          the term to identify the document(s) to be updated </param>
        /// <param name="field">
        ///          field name of the <see cref="NumericDocValues"/> field </param>
        /// <param name="value">
        ///          new value for the field </param>
        /// <exception cref="CorruptIndexException">
        ///           if the index is corrupt </exception>
        /// <exception cref="IOException">
        ///           if there is a low-level IO error </exception>
        public virtual void UpdateNumericDocValue(Term term, string field, long? value)
        {
            EnsureOpen();
            if (!globalFieldNumberMap.Contains(field, DocValuesType.NUMERIC))
            {
                throw new ArgumentException("can only update existing numeric-docvalues fields!");
            }
            try
            {
                if (docWriter.UpdateNumericDocValue(term, field, value))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "UpdateNumericDocValue");
            }
        }

        /// <summary>
        /// Updates a document's <see cref="BinaryDocValues"/> for <paramref name="field"/> to the
        /// given <paramref name="value"/>. this method can be used to 'unset' a document's
        /// value by passing <c>null</c> as the new <paramref name="value"/>. Also, you can only update
        /// fields that already exist in the index, not add new fields through this
        /// method.
        ///
        /// <para/>
        /// <b>NOTE:</b> this method currently replaces the existing value of all
        /// affected documents with the new value.
        ///
        /// <para>
        /// <b>NOTE:</b> if this method hits an <see cref="OutOfMemoryException"/> you should immediately
        /// dispose the writer. See <see cref="IndexWriter"/> for details.
        /// </para>
        /// </summary>
        /// <param name="term">
        ///          the term to identify the document(s) to be updated </param>
        /// <param name="field">
        ///          field name of the <see cref="BinaryDocValues"/> field </param>
        /// <param name="value">
        ///          new value for the field </param>
        /// <exception cref="CorruptIndexException">
        ///           if the index is corrupt </exception>
        /// <exception cref="IOException">
        ///           if there is a low-level IO error </exception>
        public virtual void UpdateBinaryDocValue(Term term, string field, BytesRef value)
        {
            EnsureOpen();
            if (!globalFieldNumberMap.Contains(field, DocValuesType.BINARY))
            {
                throw new ArgumentException("can only update existing binary-docvalues fields!");
            }
            try
            {
                if (docWriter.UpdateBinaryDocValue(term, field, value))
                {
                    ProcessEvents(true, false);
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "UpdateBinaryDocValue");
            }
        }

        // for test purpose
        internal int SegmentCount
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return segmentInfos.Count;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        // for test purpose
        internal int NumBufferedDocuments
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return docWriter.NumDocs;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        // for test purpose
        internal ICollection<string> IndexFileNames
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return segmentInfos.GetFiles(directory, true);
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        // for test purpose
        internal int GetDocCount(int i)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (i >= 0 && i < segmentInfos.Count)
                {
                    return segmentInfos[i].Info.DocCount;
                }
                else
                {
                    return -1;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // for test purpose
        internal int FlushCount => flushCount;

        // for test purpose
        internal int FlushDeletesCount => flushDeletesCount;

        internal string NewSegmentName()
        {
            // Cannot synchronize on IndexWriter because that causes
            // deadlock
            UninterruptableMonitor.Enter(segmentInfos);
            try
            {
                // Important to increment changeCount so that the
                // segmentInfos is written on close.  Otherwise we
                // could close, re-open and re-return the same segment
                // name that was previously returned which can cause
                // problems at least with ConcurrentMergeScheduler.
                changeCount++;
                segmentInfos.Changed();
                return "_" + SegmentInfos.SegmentNumberToString(segmentInfos.Counter++, allowLegacyNames: false); // LUCENENET specific - we had this right thru all of the betas, so don't change if the legacy feature is enabled
            }
            finally
            {
                UninterruptableMonitor.Exit(segmentInfos);
            }
        }

        /// <summary>
        /// If non-null, information about merges will be printed to this.
        /// </summary>
        internal readonly InfoStream infoStream;

        /// <summary>
        /// Forces merge policy to merge segments until there are &lt;=
        /// <paramref name="maxNumSegments"/>.  The actual merges to be
        /// executed are determined by the <see cref="MergePolicy"/>.
        ///
        /// <para>This is a horribly costly operation, especially when
        /// you pass a small <paramref name="maxNumSegments"/>; usually you
        /// should only call this if the index is static (will no
        /// longer be changed).</para>
        ///
        /// <para>Note that this requires up to 2X the index size free
        /// space in your Directory (3X if you're using compound
        /// file format).  For example, if your index size is 10 MB
        /// then you need up to 20 MB free for this to complete (30
        /// MB if you're using compound file format).  Also,
        /// it's best to call <see cref="Commit()"/> afterwards,
        /// to allow <see cref="IndexWriter"/> to free up disk space.</para>
        ///
        /// <para>If some but not all readers re-open while merging
        /// is underway, this will cause &gt; 2X temporary
        /// space to be consumed as those new readers will then
        /// hold open the temporary segments at that time.  It is
        /// best not to re-open readers while merging is running.</para>
        ///
        /// <para>The actual temporary usage could be much less than
        /// these figures (it depends on many factors).</para>
        ///
        /// <para>In general, once this completes, the total size of the
        /// index will be less than the size of the starting index.
        /// It could be quite a bit smaller (if there were many
        /// pending deletes) or just slightly smaller.</para>
        ///
        /// <para>If an <see cref="Exception"/> is hit, for example
        /// due to disk full, the index will not be corrupted and no
        /// documents will be lost.  However, it may have
        /// been partially merged (some segments were merged but
        /// not all), and it's possible that one of the segments in
        /// the index will be in non-compound format even when
        /// using compound file format.  This will occur when the
        /// <see cref="Exception"/> is hit during conversion of the segment into
        /// compound format.</para>
        ///
        /// <para>This call will merge those segments present in
        /// the index when the call started.  If other threads are
        /// still adding documents and flushing segments, those
        /// newly created segments will not be merged unless you
        /// call <see cref="ForceMerge(int)"/> again.</para>
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        ///
        /// <para><b>NOTE</b>: if you call <see cref="Dispose(bool)"/>
        /// with <c>false</c>, which aborts all running merges,
        /// then any thread still running this method might hit a
        /// <see cref="MergePolicy.MergeAbortedException"/>.</para>
        /// </summary>
        /// <param name="maxNumSegments"> maximum number of segments left
        /// in the index after merging finishes
        /// </param>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        /// <seealso cref="MergePolicy.FindMerges(MergeTrigger, SegmentInfos)"/>
        public virtual void ForceMerge(int maxNumSegments)
        {
            ForceMerge(maxNumSegments, true);
        }

        /// <summary>
        /// Just like <see cref="ForceMerge(int)"/>, except you can
        /// specify whether the call should block until
        /// all merging completes.  This is only meaningful with a
        /// <see cref="mergeScheduler"/> that is able to run merges in
        /// background threads.
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        public virtual void ForceMerge(int maxNumSegments, bool doWait)
        {
            EnsureOpen();

            if (maxNumSegments < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNumSegments), "maxNumSegments must be >= 1; got " + maxNumSegments); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "forceMerge: index now " + SegString());
                infoStream.Message("IW", "now flush at forceMerge");
            }

            Flush(true, true);

            UninterruptableMonitor.Enter(this);
            try
            {
                ResetMergeExceptions();
                segmentsToMerge.Clear();
                foreach (SegmentCommitInfo info in segmentInfos.Segments)
                {
                    if (info != null) segmentsToMerge[info] = true;
                }
                mergeMaxNumSegments = maxNumSegments;

                // Now mark all pending & running merges for forced
                // merge:
                foreach (MergePolicy.OneMerge merge in pendingMerges)
                {
                    merge.MaxNumSegments = maxNumSegments;
                    if (merge.Info != null) segmentsToMerge[merge.Info] = true;
                }

                foreach (MergePolicy.OneMerge merge in runningMerges)
                {
                    merge.MaxNumSegments = maxNumSegments;
                    if (merge.Info != null) segmentsToMerge[merge.Info] = true;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }

            MaybeMerge(MergeTrigger.EXPLICIT, maxNumSegments);

            if (doWait)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    while (true)
                    {
                        if (hitOOM)
                        {
                            throw IllegalStateException.Create("this writer hit an OutOfMemoryError; cannot complete forceMerge");
                        }

                        if (mergeExceptions.Count > 0)
                        {
                            // Forward any exceptions in background merge
                            // threads to the current thread:
                            int size = mergeExceptions.Count;
                            for (int i = 0; i < size; i++)
                            {
                                MergePolicy.OneMerge merge = mergeExceptions[i];
                                if (merge.MaxNumSegments != -1)
                                {
                                    string message = "background merge hit exception: " + merge.SegString(directory);
                                    Exception t = merge.Exception;
                                    if (t != null)
                                    {
                                        //err.initCause(t);
                                        throw new IOException(message + t.ToString(), t);
                                    }
                                    //throw err;
                                    throw new IOException(message);
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
                finally
                {
                    UninterruptableMonitor.Exit(this);
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
        /// Returns <c>true</c> if any merges in <see cref="pendingMerges"/> or
        /// <see cref="runningMerges"/> are <see cref="mergeMaxNumSegments"/> merges.
        /// </summary>
        private bool MaxNumSegmentsMergesPending()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                foreach (MergePolicy.OneMerge merge in pendingMerges)
                {
                    if (merge.MaxNumSegments != -1)
                    {
                        return true;
                    }
                }

                foreach (MergePolicy.OneMerge merge in runningMerges)
                {
                    if (merge.MaxNumSegments != -1)
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Just like <see cref="ForceMergeDeletes()"/>, except you can
        /// specify whether the call should block until the
        /// operation completes.  This is only meaningful with a
        /// <see cref="MergeScheduler"/> that is able to run merges in
        /// background threads.
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        ///
        /// <para><b>NOTE</b>: if you call <see cref="Dispose(bool)"/>
        /// with <c>false</c>, which aborts all running merges,
        /// then any thread still running this method might hit a
        /// <see cref="MergePolicy.MergeAbortedException"/>.</para>
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

            UninterruptableMonitor.Enter(this);
            try
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
            finally
            {
                UninterruptableMonitor.Exit(this);
            }

            mergeScheduler.Merge(this, MergeTrigger.EXPLICIT, newMergesFound);

            if (spec != null && doWait)
            {
                int numMerges = spec.Merges.Count;
                UninterruptableMonitor.Enter(this);
                try
                {
                    bool running = true;
                    while (running)
                    {
                        if (hitOOM)
                        {
                            throw IllegalStateException.Create("this writer hit an OutOfMemoryError; cannot complete forceMergeDeletes");
                        }

                        // Check each merge that MergePolicy asked us to
                        // do, to see if any of them are still running and
                        // if any of them have hit an exception.
                        running = false;
                        for (int i = 0; i < numMerges; i++)
                        {
                            MergePolicy.OneMerge merge = spec.Merges[i];
                            if (pendingMerges.Contains(merge) || runningMerges.Contains(merge))
                            {
                                running = true;
                            }
                            Exception t = merge.Exception;
                            if (t != null)
                            {
                                throw new IOException("background merge hit exception: " + merge.SegString(directory), t);
                            }
                        }

                        // If any of our merges are still running, wait:
                        if (running)
                        {
                            DoWait();
                        }
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            // NOTE: in the ConcurrentMergeScheduler case, when
            // doWait is false, we can return immediately while
            // background threads accomplish the merging
        }

        /// <summary>
        /// Forces merging of all segments that have deleted
        /// documents.  The actual merges to be executed are
        /// determined by the <see cref="MergePolicy"/>.  For example,
        /// the default <see cref="TieredMergePolicy"/> will only
        /// pick a segment if the percentage of
        /// deleted docs is over 10%.
        ///
        /// <para>This is often a horribly costly operation; rarely
        /// is it warranted.</para>
        ///
        /// <para>To see how
        /// many deletions you have pending in your index, call
        /// <see cref="IndexReader.NumDeletedDocs"/>.</para>
        ///
        /// <para><b>NOTE</b>: this method first flushes a new
        /// segment (if there are indexed documents), and applies
        /// all buffered deletes.</para>
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        public virtual void ForceMergeDeletes()
        {
            ForceMergeDeletes(true);
        }

        /// <summary>
        /// Expert: asks the <see cref="mergePolicy"/> whether any merges are
        /// necessary now and if so, runs the requested merges and
        /// then iterate (test again if merges are needed) until no
        /// more merges are returned by the <see cref="mergePolicy"/>.
        /// <para/>
        /// Explicit calls to <see cref="MaybeMerge()"/> are usually not
        /// necessary. The most common case is when merge policy
        /// parameters have changed.
        /// <para/>
        /// this method will call the <see cref="mergePolicy"/> with
        /// <see cref="MergeTrigger.EXPLICIT"/>.
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
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
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(maxNumSegments == -1 || maxNumSegments > 0);
                //if (Debugging.AssertsEnabled) Debugging.Assert(trigger != null); // LUCENENET NOTE: Enum cannot be null in .NET
                if (stopMerges)
                {
                    return false;
                }

                // Do not start new merges if we've hit OOME
                if (hitOOM)
                {
                    return false;
                }
                bool newMergesFound; // LUCENENET specific - removed unnecessary assignment
                MergePolicy.MergeSpecification spec;
                if (maxNumSegments != UNBOUNDED_MAX_MERGE_SEGMENTS)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(trigger == MergeTrigger.EXPLICIT || trigger == MergeTrigger.MERGE_FINISHED, "Expected EXPLICT or MERGE_FINISHED as trigger even with maxNumSegments set but was: {0}", trigger);
                    spec = mergePolicy.FindForcedMerges(segmentInfos, maxNumSegments, segmentsToMerge);
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
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Expert: to be used by a <see cref="MergePolicy"/> to avoid
        /// selecting merges for segments already being merged.
        /// The returned collection is not cloned, and thus is
        /// only safe to access if you hold <see cref="IndexWriter"/>'s lock
        /// (which you do when <see cref="IndexWriter"/> invokes the
        /// <see cref="MergePolicy"/>).
        /// <para/>
        /// Do not alter the returned collection!
        /// </summary>
        public virtual ICollection<SegmentCommitInfo> MergingSegments
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return mergingSegments;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Expert: the <see cref="mergeScheduler"/> calls this method to retrieve the next
        /// merge requested by the <see cref="MergePolicy"/>
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public virtual MergePolicy.OneMerge NextMerge()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (pendingMerges.Count == 0)
                {
                    return null;
                }
                else
                {
                    // Advance the merge from pending to running
                    MergePolicy.OneMerge merge = pendingMerges.Dequeue();
                    runningMerges.Add(merge);
                    return merge;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Expert: returns true if there are merges waiting to be scheduled.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public virtual bool HasPendingMerges()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                return pendingMerges.Count != 0;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Close the <see cref="IndexWriter"/> without committing
        /// any changes that have occurred since the last commit
        /// (or since it was opened, if commit hasn't been called).
        /// this removes any temporary files that had been created,
        /// after which the state of the index will be the same as
        /// it was when <see cref="Commit()"/> was last called or when this
        /// writer was first opened.  This also clears a previous
        /// call to <see cref="PrepareCommit()"/>.
        /// </summary>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public virtual void Rollback()
        {
            // don't call ensureOpen here: this acts like "close()" in closeable.

            // Ensure that only one thread actually gets to do the
            // closing, and make sure no commit is also in progress:
            UninterruptableMonitor.Enter(commitLock);
            try
            {
                if (ShouldClose())
                {
                    RollbackInternal();
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(commitLock);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RollbackInternal()
        {
            bool success = false;

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "rollback");
            }

            try
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    FinishMerges(false);
                    stopMerges = true;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
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

                bufferedUpdatesStream.Clear();
                docWriter.Dispose(); // mark it as closed first to prevent subsequent indexing actions/flushes
                docWriter.Abort(this); // don't sync on IW here
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (pendingCommit != null)
                    {
                        pendingCommit.RollbackCommit(directory);
                        deleter.DecRef(pendingCommit);
                        pendingCommit = null;
                        UninterruptableMonitor.PulseAll(this);
                    }

                    // Don't bother saving any changes in our segmentInfos
                    readerPool.DropAll(false);

                    // Keep the same segmentInfos instance but replace all
                    // of its SegmentInfo instances.  this is so the next
                    // attempt to commit using this instance of IndexWriter
                    // will always write to a new generation ("write
                    // once").
                    segmentInfos.RollbackSegmentInfos(rollbackSegments);
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "rollback: infos=" + SegString(segmentInfos.Segments));
                    }

                    if (Debugging.AssertsEnabled) Debugging.Assert(TestPoint("rollback before checkpoint"));

                    // Ask deleter to locate unreferenced files & remove
                    // them:
                    deleter.Checkpoint(segmentInfos, false);
                    deleter.Refresh();

                    lastCommitChangeCount = changeCount;

                    deleter.Refresh();
                    deleter.Dispose();

                    IOUtils.Dispose(writeLock); // release write lock
                    writeLock = null;

                    if (Debugging.AssertsEnabled)
                    {
                        // LUCENENET specific - store the number of states so we don't have to call this method twice
                        int numDeactivatedThreadStates = docWriter.perThreadPool.NumDeactivatedThreadStates();
                        Debugging.Assert(numDeactivatedThreadStates == docWriter.perThreadPool.MaxThreadStates, "{0} {1}", numDeactivatedThreadStates, docWriter.perThreadPool.MaxThreadStates);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }

                success = true;
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "RollbackInternal");
            }
            finally
            {
                if (!success)
                {
                    // Must not hold IW's lock while closing
                    // mergePolicy/Scheduler: this can lead to deadlock,
                    // e.g. TestIW.testThreadInterruptDeadlock
                    IOUtils.DisposeWhileHandlingException(mergePolicy, mergeScheduler);
                }
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (!success)
                    {
                        // we tried to be nice about it: do the minimum

                        // don't leak a segments_N file if there is a pending commit
                        if (pendingCommit != null)
                        {
                            try
                            {
                                pendingCommit.RollbackCommit(directory);
                                deleter.DecRef(pendingCommit);
                            }
                            catch (Exception t) when (t.IsThrowable())
                            {
                            }
                        }

                        // close all the closeables we can (but important is readerPool and writeLock to prevent leaks)
                        IOUtils.DisposeWhileHandlingException(readerPool, deleter, writeLock);
                        writeLock = null;
                    }
                    closed = true;
                    closing = false;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Delete all documents in the index.
        ///
        /// <para>This method will drop all buffered documents and will
        ///    remove all segments from the index. This change will not be
        ///    visible until a <see cref="Commit()"/> has been called. This method
        ///    can be rolled back using <see cref="Rollback()"/>.</para>
        ///
        /// <para>NOTE: this method is much faster than using <c>DeleteDocuments(new MatchAllDocsQuery())</c>.
        ///    Yet, this method also has different semantics compared to <see cref="DeleteDocuments(Query)"/>
        ///    / <see cref="DeleteDocuments(Query[])"/> since internal data-structures are cleared as well
        ///    as all segment information is forcefully dropped anti-viral semantics like omitting norms
        ///    are reset or doc value types are cleared. Essentially a call to <see cref="DeleteAll()"/> is equivalent
        ///    to creating a new <see cref="IndexWriter"/> with <see cref="OpenMode.CREATE"/> which a delete query only marks
        ///    documents as deleted.</para>
        ///
        /// <para>NOTE: this method will forcefully abort all merges
        ///    in progress.  If other threads are running 
        ///    <see cref="ForceMerge(int)"/>, <see cref="AddIndexes(IndexReader[])"/> or
        ///    <see cref="ForceMergeDeletes()"/> methods, they may receive
        ///    <see cref="MergePolicy.MergeAbortedException"/>s.</para>
        /// </summary>
        public virtual void DeleteAll()
        {
            EnsureOpen();
            // Remove any buffered docs
            bool success = false;
            /* hold the full flush lock to prevent concurrency commits / NRT reopens to
             * get in our way and do unnecessary work. -- if we don't lock this here we might
             * get in trouble if */
            UninterruptableMonitor.Enter(fullFlushLock);
            try
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
                    docWriter.LockAndAbortAll(this);
                    ProcessEvents(false, true);
                    UninterruptableMonitor.Enter(this);
                    try
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
                        catch (Exception oom) when (oom.IsOutOfMemoryError())
                        {
                            HandleOOM(oom, "DeleteAll");
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
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
                finally
                {
                    docWriter.UnlockAllAfterAbortAll(this);
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(fullFlushLock);
            }
        }

        private void FinishMerges(bool waitForMerges)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!waitForMerges)
                {
                    stopMerges = true;

                    // Abort all pending & running merges:
                    foreach (MergePolicy.OneMerge merge in pendingMerges)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "now abort pending merge " + SegString(merge.Segments));
                        }
                        merge.Abort();
                        MergeFinish(merge);
                    }
                    pendingMerges.Clear();

                    foreach (MergePolicy.OneMerge merge in runningMerges)
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
                    while (runningMerges.Count > 0)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "now wait for " + runningMerges.Count + " running merge/s to abort");
                        }
                        DoWait();
                    }

                    stopMerges = false;
                    UninterruptableMonitor.PulseAll(this);

                    if (Debugging.AssertsEnabled) Debugging.Assert(0 == mergingSegments.Count);

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
                    // ObjectDisposedException.
                    WaitForMerges();
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Wait for any currently outstanding merges to finish.
        ///
        /// <para>It is guaranteed that any merges started prior to calling this method
        ///    will have completed once this method completes.</para>
        /// </summary>
        public virtual void WaitForMerges()
        {
            UninterruptableMonitor.Enter(this);
            try
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
                if (Debugging.AssertsEnabled) Debugging.Assert(0 == mergingSegments.Count);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "waitForMerges done");
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Called whenever the <see cref="SegmentInfos"/> has been updated and
        /// the index files referenced exist (correctly) in the
        /// index directory.
        /// </summary>
        internal virtual void Checkpoint()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                Changed();
                deleter.Checkpoint(segmentInfos, false);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Checkpoints with <see cref="IndexFileDeleter"/>, so it's aware of
        /// new files, and increments <see cref="changeCount"/>, so on
        /// close/commit we will write a new segments file, but
        /// does NOT bump segmentInfos.version.
        /// </summary>
        internal virtual void CheckpointNoSIS()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                changeCount++;
                deleter.Checkpoint(segmentInfos, false);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Called internally if any index state has changed. </summary>
        internal void Changed()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                changeCount++;
                segmentInfos.Changed();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal virtual void PublishFrozenUpdates(FrozenBufferedUpdates packet)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(packet != null && packet.Any());
                UninterruptableMonitor.Enter(bufferedUpdatesStream);
                try
                {
                    bufferedUpdatesStream.Push(packet);
                }
                finally
                {
                    UninterruptableMonitor.Exit(bufferedUpdatesStream);
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Atomically adds the segment private delete packet and publishes the flushed
        /// segments <see cref="SegmentInfo"/> to the index writer.
        /// </summary>
        internal virtual void PublishFlushedSegment(SegmentCommitInfo newSegment, FrozenBufferedUpdates packet, FrozenBufferedUpdates globalPacket)
        {
            try
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    // Lock order IW -> BDS
                    UninterruptableMonitor.Enter(bufferedUpdatesStream);
                    try
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "publishFlushedSegment");
                        }

                        if (globalPacket != null && globalPacket.Any())
                        {
                            bufferedUpdatesStream.Push(globalPacket);
                        }
                        // Publishing the segment must be synched on IW -> BDS to make the sure
                        // that no merge prunes away the seg. private delete packet
                        long nextGen;
                        if (packet != null && packet.Any())
                        {
                            nextGen = bufferedUpdatesStream.Push(packet);
                        }
                        else
                        {
                            // Since we don't have a delete packet to apply we can get a new
                            // generation right away
                            nextGen = bufferedUpdatesStream.GetNextGen();
                        }
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "publish sets newSegment delGen=" + nextGen + " seg=" + SegString(newSegment));
                        }
                        newSegment.SetBufferedDeletesGen(nextGen);
                        segmentInfos.Add(newSegment);
                        Checkpoint();
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(bufferedUpdatesStream);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
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
            UninterruptableMonitor.Enter(this);
            try
            {
                mergeExceptions = new JCG.List<MergePolicy.OneMerge>();
                mergeGen++;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void NoDupDirs(params Directory[] dirs)
        {
            JCG.HashSet<Directory> dups = new JCG.HashSet<Directory>();
            for (int i = 0; i < dirs.Length; i++)
            {
                if (dups.Contains(dirs[i]))
                {
                    throw new ArgumentException("Directory " + dirs[i] + " appears more than once");
                }
                if (dirs[i] == directory)
                {
                    throw new ArgumentException("Cannot add directory to itself");
                }
                dups.Add(dirs[i]);
            }
        }

        /// <summary>
        /// Acquires write locks on all the directories; be sure
        /// to match with a call to <see cref="IOUtils.Dispose(IEnumerable{IDisposable})"/> in a
        /// finally clause.
        /// </summary>
        private IEnumerable<Lock> AcquireWriteLocks(params Directory[] dirs)
        {
            IList<Lock> locks = new JCG.List<Lock>();
            for (int i = 0; i < dirs.Length; i++)
            {
                bool success = false;
                try
                {
                    Lock @lock = dirs[i].MakeLock(WRITE_LOCK_NAME);
                    locks.Add(@lock);
                    @lock.Obtain(config.WriteLockTimeout);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Release all previously acquired locks:
                        IOUtils.DisposeWhileHandlingException(locks);
                    }
                }
            }
            return locks;
        }

        /// <summary>
        /// Adds all segments from an array of indexes into this index.
        ///
        /// <para/>This may be used to parallelize batch indexing. A large document
        /// collection can be broken into sub-collections. Each sub-collection can be
        /// indexed in parallel, on a different thread, process or machine. The
        /// complete index can then be created by merging sub-collection indexes
        /// with this method.
        ///
        /// <para/>
        /// <b>NOTE:</b> this method acquires the write lock in
        /// each directory, to ensure that no <see cref="IndexWriter"/>
        /// is currently open or tries to open while this is
        /// running.
        ///
        /// <para/>This method is transactional in how <see cref="Exception"/>s are
        /// handled: it does not commit a new segments_N file until
        /// all indexes are added.  this means if an <see cref="Exception"/>
        /// occurs (for example disk full), then either no indexes
        /// will have been added or they all will have been.
        ///
        /// <para/>Note that this requires temporary free space in the
        /// <see cref="Store.Directory"/> up to 2X the sum of all input indexes
        /// (including the starting index). If readers/searchers
        /// are open against the starting index, then temporary
        /// free space required will be higher by the size of the
        /// starting index (see <see cref="ForceMerge(int)"/> for details).
        ///
        /// <para/>
        /// <b>NOTE:</b> this method only copies the segments of the incoming indexes
        /// and does not merge them. Therefore deleted documents are not removed and
        /// the new segments are not merged with the existing ones.
        ///
        /// <para/>This requires this index not be among those to be added.
        ///
        /// <para/>
        /// <b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer. See
        /// <see cref="IndexWriter"/> for details.
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

                IList<SegmentCommitInfo> infos = new JCG.List<SegmentCommitInfo>();
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
                        JCG.HashSet<string> dsFilesCopied = new JCG.HashSet<string>();
                        IDictionary<string, string> dsNames = new Dictionary<string, string>();
                        JCG.HashSet<string> copiedFiles = new JCG.HashSet<string>();
                        foreach (SegmentCommitInfo info in sis.Segments)
                        {
                            if (Debugging.AssertsEnabled) Debugging.Assert(!infos.Contains(info),"dup info dir={0} name={1}", info.Info.Dir, info.Info.Name);

                            string newSegName = NewSegmentName();

                            if (infoStream.IsEnabled("IW"))
                            {
                                infoStream.Message("IW", "addIndexes: process segment origName=" + info.Info.Name + " newName=" + newSegName + " info=" + info);
                            }

                            IOContext context = new IOContext(new MergeInfo(info.Info.DocCount, info.GetSizeInBytes(), true, -1));

                            foreach (FieldInfo fi in SegmentReader.ReadFieldInfos(info))
                            {
                                globalFieldNumberMap.AddOrGet(fi.Name, fi.Number, fi.DocValuesType);
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
                            foreach (string file in sipc.GetFiles())
                            {
                                try
                                {
                                    directory.DeleteFile(file);
                                }
                                catch (Exception t) when (t.IsThrowable())
                                {
                                }
                            }
                        }
                    }
                }

                UninterruptableMonitor.Enter(this);
                try
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
                                foreach (string file in sipc.GetFiles())
                                {
                                    try
                                    {
                                        directory.DeleteFile(file);
                                    }
                                    catch (Exception t) when (t.IsThrowable())
                                    {
                                    }
                                }
                            }
                        }
                    }
                    segmentInfos.AddAll(infos);
                    Checkpoint();
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }

                successTop = true;
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "AddIndexes(Directory...)");
            }
            finally
            {
                if (successTop)
                {
                    IOUtils.Dispose(locks);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(locks);
                }
            }
        }

        /// <summary>
        /// Merges the provided indexes into this index.
        ///
        /// <para/>
        /// The provided <see cref="IndexReader"/>s are not closed.
        ///
        /// <para/>
        /// See <see cref="AddIndexes(IndexReader[])"/> for details on transactional semantics, temporary
        /// free space required in the <see cref="Store.Directory"/>, and non-CFS segments on an <see cref="Exception"/>.
        ///
        /// <para/>
        /// <b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/> you should immediately
        /// dispose the writer. See <see cref="IndexWriter"/> for details.
        ///
        /// <para/>
        /// <b>NOTE:</b> empty segments are dropped by this method and not added to this
        /// index.
        ///
        /// <para/>
        /// <b>NOTE:</b> this method merges all given <see cref="IndexReader"/>s in one
        /// merge. If you intend to merge a large number of readers, it may be better
        /// to call this method multiple times, each time with a small set of readers.
        /// In principle, if you use a merge policy with a <c>mergeFactor</c> or
        /// <c>maxMergeAtOnce</c> parameter, you should pass that many readers in one
        /// call. Also, if the given readers are <see cref="DirectoryReader"/>s, they can be
        /// opened with <c>termIndexInterval=-1</c> to save RAM, since during merge
        /// the in-memory structure is not used. See
        /// <see cref="DirectoryReader.Open(Directory, int)"/>.
        ///
        /// <para/>
        /// <b>NOTE</b>: if you call <see cref="Dispose(bool)"/> with <c>false</c>, which
        /// aborts all running merges, then any thread still running this method might
        /// hit a <see cref="MergePolicy.MergeAbortedException"/>.
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
                IList<AtomicReader> mergeReaders = new JCG.List<AtomicReader>();
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

                SegmentInfo info = new SegmentInfo(directory, Constants.LUCENE_MAIN_VERSION, mergedName, -1, false, codec, null);

                SegmentMerger merger = new SegmentMerger(mergeReaders, info, infoStream, trackingDir, config.TermIndexInterval, CheckAbort.NONE, globalFieldNumberMap, context, config.CheckIntegrityAtMerge);

                if (!merger.ShouldMerge)
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
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            deleter.Refresh(info.Name);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
                        }
                    }
                }

                SegmentCommitInfo infoPerCommit = new SegmentCommitInfo(info, 0, -1L, -1L);

                info.SetFiles(new JCG.HashSet<string>(trackingDir.CreatedFiles));
                trackingDir.CreatedFiles.Clear();

                SetDiagnostics(info, SOURCE_ADDINDEXES_READERS);

                bool useCompoundFile;
                UninterruptableMonitor.Enter(this); // Guard segmentInfos
                try
                {
                    if (stopMerges)
                    {
                        deleter.DeleteNewFiles(infoPerCommit.GetFiles());
                        return;
                    }
                    EnsureOpen();
                    useCompoundFile = mergePolicy.UseCompoundFile(segmentInfos, infoPerCommit);
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }

                // Now create the compound file if needed
                if (useCompoundFile)
                {
                    ICollection<string> filesToDelete = infoPerCommit.GetFiles();
                    try
                    {
                        CreateCompoundFile(infoStream, directory, CheckAbort.NONE, info, context);
                    }
                    finally
                    {
                        // delete new non cfs files directly: they were never
                        // registered with IFD
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            deleter.DeleteNewFiles(filesToDelete);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
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
                    codec.SegmentInfoFormat.SegmentInfoWriter.Write(trackingDir, info, mergeState.FieldInfos, context);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            deleter.Refresh(info.Name);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
                        }
                    }
                }

                info.AddFiles(trackingDir.CreatedFiles);

                // Register the new segment
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (stopMerges)
                    {
                        deleter.DeleteNewFiles(info.GetFiles());
                        return;
                    }
                    EnsureOpen();
                    segmentInfos.Add(infoPerCommit);
                    Checkpoint();
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "AddIndexes(IndexReader...)");
            }
        }

        /// <summary>
        /// Copies the segment files as-is into the <see cref="IndexWriter"/>'s directory. </summary>
        private SegmentCommitInfo CopySegmentAsIs(SegmentCommitInfo info, string segName, IDictionary<string, string> dsNames, ISet<string> dsFilesCopied, IOContext context, ISet<string> copiedFiles)
        {
            // Determine if the doc store of this segment needs to be copied. It's
            // only relevant for segments that share doc store with others,
            // because the DS might have been copied already, in which case we
            // just want to update the DS name of this SegmentInfo.
            string dsName = Lucene3xSegmentInfoFormat.GetDocStoreSegment(info.Info);
            if (Debugging.AssertsEnabled) Debugging.Assert(dsName != null);
            // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
            if (!dsNames.TryGetValue(dsName, out string newDsName))
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
#pragma warning disable CS0618 // Type or member is obsolete
            if (info.Info.Attributes is null)
            {
                attributes = new Dictionary<string, string>();
            }
            else
            {
                attributes = new Dictionary<string, string>(info.Info.Attributes);
            }
#pragma warning restore CS0618 // Type or member is obsolete
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

            JCG.HashSet<string> segFiles = new JCG.HashSet<string>();

            // Build up new segment's file names.  Must do this
            // before writing SegmentInfo:
            foreach (string file in info.GetFiles())
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
            newInfo.SetFiles(segFiles);

            // We must rewrite the SI file because it references
            // segment name (its own name, if its 3.x, and doc
            // store segment name):
            TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(directory);
            Codec currentCodec = newInfo.Codec;
            try
            {
                currentCodec.SegmentInfoFormat.SegmentInfoWriter.Write(trackingDir, newInfo, fis, context);
            }
            catch (Exception uoe) when (uoe.IsUnsupportedOperationException())
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (currentCodec is Lucene3xCodec)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    // OK: 3x codec cannot write a new SI file;
                    // SegmentInfos will write this on commit
                }
                else
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
            }

            ICollection<string> siFiles = trackingDir.CreatedFiles;

            bool success = false;
            try
            {
                // Copy the segment's files
                foreach (string file in info.GetFiles())
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

                    if (Debugging.AssertsEnabled)
                    {
                        Debugging.Assert(!SlowFileExists(directory, newFileName), "file \"{0}\" already exists; siFiles={1}", newFileName, siFiles);
                        Debugging.Assert(!copiedFiles.Contains(file), "file \"{0}\" is being copied more than once", file);
                    }
                    copiedFiles.Add(file);
                    info.Info.Dir.Copy(directory, file, newFileName, context);
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    foreach (string file in newInfo.GetFiles())
                    {
                        try
                        {
                            directory.DeleteFile(file);
                        }
                        catch (Exception t) when (t.IsThrowable())
                        {
                        }
                    }
                }
            }

            return newInfoPerCommit;
        }

        /// <summary>
        /// A hook for extending classes to execute operations after pending added and
        /// deleted documents have been flushed to the <see cref="Store.Directory"/> but before the change
        /// is committed (new segments_N file written).
        /// </summary>
        protected virtual void DoAfterFlush()
        {
        }

        /// <summary>
        /// A hook for extending classes to execute operations before pending added and
        /// deleted documents are flushed to the <see cref="Store.Directory"/>.
        /// </summary>
        protected virtual void DoBeforeFlush()
        {
        }

        /// <summary>
        /// <para>Expert: prepare for commit.  This does the
        /// first phase of 2-phase commit. this method does all
        /// steps necessary to commit changes since this writer
        /// was opened: flushes pending added and deleted docs,
        /// syncs the index files, writes most of next segments_N
        /// file.  After calling this you must call either 
        /// <see cref="Commit()"/> to finish the commit, or 
        /// <see cref="Rollback()"/> to revert the commit and undo all changes
        /// done since the writer was opened.</para>
        ///
        /// <para>You can also just call <see cref="Commit()"/> directly
        /// without <see cref="PrepareCommit()"/> first in which case that method
        /// will internally call <see cref="PrepareCommit()"/>.</para>
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        public void PrepareCommit()
        {
            EnsureOpen();
            PrepareCommitInternal();
        }

        private void PrepareCommitInternal()
        {
            UninterruptableMonitor.Enter(commitLock);
            try
            {
                EnsureOpen(false);
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "prepareCommit: flush");
                    infoStream.Message("IW", "  index before flush " + SegString());
                }

                if (hitOOM)
                {
                    throw IllegalStateException.Create("this writer hit an OutOfMemoryError; cannot commit");
                }

                if (pendingCommit != null)
                {
                    throw IllegalStateException.Create("prepareCommit was already called with no corresponding call to commit");
                }

                DoBeforeFlush();
                // LUCENENET: .NET doesn't support asserts in release mode
                if (Lucene.Net.Diagnostics.Debugging.AssertsEnabled) TestPoint("startDoFlush");
                SegmentInfos toCommit = null;
                bool anySegmentsFlushed = false;

                // this is copied from doFlush, except it's modified to
                // clone & incRef the flushed SegmentInfos inside the
                // sync block:

                try
                {
                    UninterruptableMonitor.Enter(fullFlushLock);
                    try
                    {
                        bool flushSuccess = false;
                        bool success = false;
                        try
                        {
                            anySegmentsFlushed = docWriter.FlushAllThreads(this);
                            if (!anySegmentsFlushed)
                            {
                                // prevent double increment since docWriter#doFlush increments the flushcount
                                // if we flushed anything.
                                flushCount.IncrementAndGet();
                            }
                            ProcessEvents(false, true);
                            flushSuccess = true;

                            UninterruptableMonitor.Enter(this);
                            try
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

                                // this protects the segmentInfos we are now going
                                // to commit.  this is important in case, eg, while
                                // we are trying to sync all referenced files, a
                                // merge completes which would otherwise have
                                // removed the files we are now syncing.
                                filesToCommit = toCommit.GetFiles(directory, false);
                                deleter.IncRef(filesToCommit);
                            }
                            finally
                            {
                                UninterruptableMonitor.Exit(this);
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
                    finally
                    {
                        UninterruptableMonitor.Exit(fullFlushLock);
                    }
                }
                catch (Exception oom) when (oom.IsOutOfMemoryError())
                {
                    HandleOOM(oom, "PrepareCommit");
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
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            if (filesToCommit != null)
                            {
                                deleter.DecRef(filesToCommit);
                                filesToCommit = null;
                            }
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
                        }
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(commitLock);
            }
        }

        /// <summary>
        /// Sets the commit user data map. That method is considered a transaction by
        /// <see cref="IndexWriter"/> and will be committed (<see cref="Commit()"/> even if no other
        /// changes were made to the writer instance. Note that you must call this method
        /// before <see cref="PrepareCommit()"/>, or otherwise it won't be included in the
        /// follow-on <see cref="Commit()"/>.
        /// <para/>
        /// <b>NOTE:</b> the dictionary is cloned internally, therefore altering the dictionary's
        /// contents after calling this method has no effect.
        /// </summary>
        public void SetCommitData(IDictionary<string, string> commitUserData)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                segmentInfos.UserData = new Dictionary<string, string>(commitUserData);
                ++changeCount;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns the commit user data map that was last committed, or the one that
        /// was set on <see cref="SetCommitData(IDictionary{string, string})"/>.
        /// </summary>
        public IDictionary<string, string> CommitData
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return segmentInfos.UserData;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Used only by commit and prepareCommit, below; lock
        /// order is commitLock -> IW
        /// </summary>
        private readonly object commitLock = new object();

        /// <summary>
        /// <para>Commits all pending changes (added &amp; deleted
        /// documents, segment merges, added
        /// indexes, etc.) to the index, and syncs all referenced
        /// index files, such that a reader will see the changes
        /// and the index updates will survive an OS or machine
        /// crash or power loss.  Note that this does not wait for
        /// any running background merges to finish.  This may be a
        /// costly operation, so you should test the cost in your
        /// application and do it only when really necessary.</para>
        ///
        /// <para> Note that this operation calls <see cref="Store.Directory.Sync(ICollection{string})"/> on
        /// the index files.  That call should not return until the
        /// file contents &amp; metadata are on stable storage.  For
        /// <see cref="Store.FSDirectory"/>, this calls the OS's fsync.  But, beware:
        /// some hardware devices may in fact cache writes even
        /// during fsync, and return before the bits are actually
        /// on stable storage, to give the appearance of faster
        /// performance.  If you have such a device, and it does
        /// not have a battery backup (for example) then on power
        /// loss it may still lose data.  Lucene cannot guarantee
        /// consistency on such devices.  </para>
        ///
        /// <para><b>NOTE</b>: if this method hits an <see cref="OutOfMemoryException"/>
        /// you should immediately dispose the writer.  See
        /// <see cref="IndexWriter"/> for details.</para>
        /// </summary>
        public void Commit()
        {
            EnsureOpen();
            CommitInternal();
        }

        /// <summary>
        /// Returns <c>true</c> if there may be changes that have not been
        /// committed.  There are cases where this may return <c>true</c>
        /// when there are no actual "real" changes to the index,
        /// for example if you've deleted by <see cref="Term"/> or <see cref="Query"/> but
        /// that <see cref="Term"/> or <see cref="Query"/> does not match any documents.
        /// Also, if a merge kicked off as a result of flushing a
        /// new segment during <see cref="Commit()"/>, or a concurrent
        /// merged finished, this method may return <c>true</c> right
        /// after you had just called <see cref="Commit()"/>.
        /// </summary>
        public bool HasUncommittedChanges()
        {
            return changeCount != lastCommitChangeCount || docWriter.AnyChanges() || bufferedUpdatesStream.Any();
        }

        private void CommitInternal()
        {
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "commit: start");
            }

            UninterruptableMonitor.Enter(commitLock);
            try
            {
                EnsureOpen(false);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "commit: enter lock");
                }

                if (pendingCommit is null)
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
            finally
            {
                UninterruptableMonitor.Exit(commitLock);
            }
        }

        private void FinishCommit()
        {
            UninterruptableMonitor.Enter(this);
            try
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
                            infoStream.Message("IW", "commit: wrote segments file \"" + pendingCommit.GetSegmentsFileName() + "\"");
                        }
                        segmentInfos.UpdateGeneration(pendingCommit);
                        lastCommitChangeCount = pendingCommitChangeCount;
                        rollbackSegments = pendingCommit.CreateBackupSegmentInfos();
                        // NOTE: don't use this.checkpoint() here, because
                        // we do not want to increment changeCount:
                        deleter.Checkpoint(pendingCommit, true);
                    }
                    finally
                    {
                        // Matches the incRef done in prepareCommit:
                        deleter.DecRef(filesToCommit);
                        filesToCommit = null;
                        pendingCommit = null;
                        UninterruptableMonitor.PulseAll(this);
                    }
                }
                else
                {
                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "commit: pendingCommit is null; skip");
                    }
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "commit: done");
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Ensures only one <see cref="Flush(bool, bool)"/> is actually flushing segments
        /// at a time:
        /// </summary>
        private readonly object fullFlushLock = new object();

        // for assert
        internal virtual bool HoldsFullFlushLock => UninterruptableMonitor.IsEntered(fullFlushLock);

        /// <summary>
        /// Flush all in-memory buffered updates (adds and deletes)
        /// to the <see cref="Store.Directory"/>. </summary>
        /// <param name="triggerMerge"> if <c>true</c>, we may merge segments (if
        /// deletes or docs were flushed) if necessary </param>
        /// <param name="applyAllDeletes"> whether pending deletes should also </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
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
            if (hitOOM)
            {
                throw IllegalStateException.Create("this writer hit an OutOfMemoryError; cannot flush");
            }

            DoBeforeFlush();
            // LUCENENET: .NET doesn't support asserts in release mode
            if (Lucene.Net.Diagnostics.Debugging.AssertsEnabled) TestPoint("startDoFlush");
            bool success = false;
            try
            {
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "  start flush: applyAllDeletes=" + applyAllDeletes);
                    infoStream.Message("IW", "  index before flush " + SegString());
                }
                bool anySegmentFlushed;

                UninterruptableMonitor.Enter(fullFlushLock);
                try
                {
                    bool flushSuccess = false;
                    try
                    {
                        anySegmentFlushed = docWriter.FlushAllThreads(this);
                        flushSuccess = true;
                    }
                    finally
                    {
                        docWriter.FinishFullFlush(flushSuccess);
                        ProcessEvents(false, true);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(fullFlushLock);
                }
                UninterruptableMonitor.Enter(this);
                try
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
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "DoFlush");
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
            UninterruptableMonitor.Enter(this);
            try
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
                    infoStream.Message("IW", "don't apply deletes now delTermCount=" + bufferedUpdatesStream.NumTerms + " bytesUsed=" + bufferedUpdatesStream.BytesUsed);
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal void ApplyAllDeletesAndUpdates()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                flushDeletesCount.IncrementAndGet();
                BufferedUpdatesStream.ApplyDeletesResult result;
                result = bufferedUpdatesStream.ApplyDeletesAndUpdates(readerPool, segmentInfos.AsList());
                if (result.AnyDeletes)
                {
                    Checkpoint();
                }
                if (!keepFullyDeletedSegments && result.AllDeleted != null)
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
                bufferedUpdatesStream.Prune(segmentInfos);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Expert:  Return the total size of all index files currently cached in memory.
        /// Useful for size management with flushRamDocs()
        /// </summary>
        public long RamSizeInBytes()
        {
            EnsureOpen();
            return docWriter.flushControl.NetBytes + bufferedUpdatesStream.BytesUsed;
        }

        // for testing only
        internal virtual DocumentsWriter DocsWriter
            => Debugging.AssertsEnabled ? docWriter : null; // LUCENENET specific - just read the status, simpler than using Assert() to set a local variable

        /// <summary>
        /// Expert:  Return the number of documents currently
        /// buffered in RAM.
        /// </summary>
        public int NumRamDocs()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                EnsureOpen();
                return docWriter.NumDocs;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void EnsureValidMerge(MergePolicy.OneMerge merge)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                foreach (SegmentCommitInfo info in merge.Segments)
                {
                    if (!segmentInfos.Contains(info))
                    {
                        throw new MergePolicy.MergeException("MergePolicy selected a segment (" + info.Info.Name + ") that is not in the current index " + SegString(), directory);
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private static void SkipDeletedDoc(DocValuesFieldUpdatesIterator[] updatesIters, int deletedDoc) // LUCENENET: CA1822: Mark members as static
        {
            foreach (DocValuesFieldUpdatesIterator iter in updatesIters)
            {
                if (iter.Doc == deletedDoc)
                {
                    iter.NextDoc();
                }
                // when entering the method, all iterators must already be beyond the
                // deleted document, or right on it, in which case we advance them over
                // and they must be beyond it now.
                if (Debugging.AssertsEnabled) Debugging.Assert(iter.Doc > deletedDoc,"updateDoc={0} deletedDoc={1}", iter.Doc, deletedDoc);
            }
        }

        private class MergedDeletesAndUpdates
        {
            internal ReadersAndUpdates mergedDeletesAndUpdates = null;
            internal MergePolicy.DocMap docMap = null;
            internal bool initializedWritableLiveDocs = false;

            internal MergedDeletesAndUpdates()
            {
            }

            internal void Init(ReaderPool readerPool, MergePolicy.OneMerge merge, MergeState mergeState, bool initWritableLiveDocs)
            {
                if (mergedDeletesAndUpdates is null)
                {
                    mergedDeletesAndUpdates = readerPool.Get(merge.info, true);
                    docMap = merge.GetDocMap(mergeState);
                    if (Debugging.AssertsEnabled) Debugging.Assert(docMap.IsConsistent(merge.info.Info.DocCount));
                }
                if (initWritableLiveDocs && !initializedWritableLiveDocs)
                {
                    mergedDeletesAndUpdates.InitWritableLiveDocs();
                    this.initializedWritableLiveDocs = true;
                }
            }
        }

        private void MaybeApplyMergedDVUpdates(MergePolicy.OneMerge merge, MergeState mergeState, int docUpto, MergedDeletesAndUpdates holder, string[] mergingFields, DocValuesFieldUpdates[] dvFieldUpdates, DocValuesFieldUpdatesIterator[] updatesIters, int curDoc)
        {
            int newDoc = -1;
            for (int idx = 0; idx < mergingFields.Length; idx++)
            {
                DocValuesFieldUpdatesIterator updatesIter = updatesIters[idx];
                if (updatesIter.Doc == curDoc) // document has an update
                {
                    if (holder.mergedDeletesAndUpdates is null)
                    {
                        holder.Init(readerPool, merge, mergeState, false);
                    }
                    if (newDoc == -1) // map once per all field updates, but only if there are any updates
                    {
                        newDoc = holder.docMap.Map(docUpto);
                    }
                    DocValuesFieldUpdates dvUpdates = dvFieldUpdates[idx];
                    // LUCENENET specific - dvUpdates handles getting the value so we don't need to deal with boxing/unboxing here.
                    dvUpdates.AddFromIterator(newDoc, updatesIter);
                    updatesIter.NextDoc(); // advance to next document
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(updatesIter.Doc > curDoc, "field={0} updateDoc={1} curDoc={2}", mergingFields[idx], updatesIter.Doc, curDoc);
                }
            }
        }

        /// <summary>
        /// Carefully merges deletes and updates for the segments we just merged. This
        /// is tricky because, although merging will clear all deletes (compacts the
        /// documents) and compact all the updates, new deletes and updates may have
        /// been flushed to the segments since the merge was started. This method
        /// "carries over" such new deletes and updates onto the newly merged segment,
        /// and saves the resulting deletes and updates files (incrementing the delete
        /// and DV generations for merge.info). If no deletes were flushed, no new
        /// deletes file is saved.
        /// </summary>
        private ReadersAndUpdates CommitMergedDeletesAndUpdates(MergePolicy.OneMerge merge, MergeState mergeState)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(TestPoint("startCommitMergeDeletes"));

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
                    IBits prevLiveDocs = merge.readers[i].LiveDocs;
                    ReadersAndUpdates rld = readerPool.Get(info, false);
                    // We hold a ref so it should still be in the pool:
                    if (Debugging.AssertsEnabled) Debugging.Assert(rld != null, "seg={0}", info.Info.Name);
                    IBits currentLiveDocs = rld.LiveDocs;
                    IDictionary<string, DocValuesFieldUpdates> mergingFieldUpdates = rld.MergingFieldUpdates;
                    string[] mergingFields;
                    DocValuesFieldUpdates[] dvFieldUpdates;
                    DocValuesFieldUpdatesIterator[] updatesIters;
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
                        updatesIters = new DocValuesFieldUpdatesIterator[mergingFieldUpdates.Count];
                        int idx = 0;
                        foreach (KeyValuePair<string, DocValuesFieldUpdates> e in mergingFieldUpdates)
                        {
                            string field = e.Key;
                            DocValuesFieldUpdates updates = e.Value;
                            mergingFields[idx] = field;
                            dvFieldUpdates[idx] = mergedDVUpdates.GetUpdates(field, updates.type);
                            if (dvFieldUpdates[idx] is null)
                            {
                                dvFieldUpdates[idx] = mergedDVUpdates.NewUpdates(field, updates.type, mergeState.SegmentInfo.DocCount);
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
                        if (Debugging.AssertsEnabled)
                        {
                            Debugging.Assert(currentLiveDocs != null);
                            Debugging.Assert(prevLiveDocs.Length == docCount);
                            Debugging.Assert(currentLiveDocs.Length == docCount);
                        }

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
                                    if (Debugging.AssertsEnabled) Debugging.Assert(!currentLiveDocs.Get(j));
                                }
                                else
                                {
                                    if (!currentLiveDocs.Get(j))
                                    {
                                        if (holder.mergedDeletesAndUpdates is null || !holder.initializedWritableLiveDocs)
                                        {
                                            holder.Init(readerPool, merge, mergeState, true);
                                        }
                                        holder.mergedDeletesAndUpdates.Delete(holder.docMap.Map(docUpto));
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
                        if (Debugging.AssertsEnabled) Debugging.Assert(currentLiveDocs.Length == docCount);
                        // this segment had no deletes before but now it
                        // does:
                        for (int j = 0; j < docCount; j++)
                        {
                            if (!currentLiveDocs.Get(j))
                            {
                                if (holder.mergedDeletesAndUpdates is null || !holder.initializedWritableLiveDocs)
                                {
                                    holder.Init(readerPool, merge, mergeState, true);
                                }
                                holder.mergedDeletesAndUpdates.Delete(holder.docMap.Map(docUpto));
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

                if (Debugging.AssertsEnabled) Debugging.Assert(docUpto == merge.info.Info.DocCount);

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
                        holder.mergedDeletesAndUpdates.WriteFieldUpdates(directory, mergedDVUpdates);
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            holder.mergedDeletesAndUpdates.DropChanges();
                            readerPool.Drop(merge.info);
                        }
                    }
                }

                if (infoStream.IsEnabled("IW"))
                {
                    if (holder.mergedDeletesAndUpdates is null)
                    {
                        infoStream.Message("IW", "no new deletes or field updates since merge started");
                    }
                    else
                    {
                        string msg = holder.mergedDeletesAndUpdates.PendingDeleteCount + " new deletes";
                        if (mergedDVUpdates.Any())
                        {
                            msg += " and " + mergedDVUpdates.Count + " new field updates";
                        }
                        msg += " since merge started";
                        infoStream.Message("IW", msg);
                    }
                }

                merge.info.SetBufferedDeletesGen(minGen);

                return holder.mergedDeletesAndUpdates;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private bool CommitMerge(MergePolicy.OneMerge merge, MergeState mergeState)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(TestPoint("startCommitMerge"));

                if (hitOOM)
                {
                    throw IllegalStateException.Create("this writer hit an OutOfMemoryError; cannot complete merge");
                }

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "commitMerge: " + SegString(merge.Segments) + " index=" + SegString());
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(merge.registerDone);

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
                    deleter.DeleteNewFiles(merge.info.GetFiles());
                    return false;
                }

                ReadersAndUpdates mergedUpdates = merge.info.Info.DocCount == 0 ? null : CommitMergedDeletesAndUpdates(merge, mergeState);
                //    System.out.println("[" + Thread.currentThread().getName() + "] IW.commitMerge: mergedDeletes=" + mergedDeletes);

                // If the doc store we are using has been closed and
                // is in now compound format (but wasn't when we
                // started), then we will switch to the compound
                // format as well:

                if (Debugging.AssertsEnabled) Debugging.Assert(!segmentInfos.Contains(merge.info));

                bool allDeleted = merge.Segments.Count == 0 || merge.info.Info.DocCount == 0 || (mergedUpdates != null && mergedUpdates.PendingDeleteCount == merge.info.Info.DocCount);

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
                if (Debugging.AssertsEnabled) Debugging.Assert(merge.Segments.Count > 0 || dropSegment);

                if (Debugging.AssertsEnabled) Debugging.Assert(merge.info.Info.DocCount != 0 || keepFullyDeletedSegments || dropSegment);

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
                    if (Debugging.AssertsEnabled) Debugging.Assert(!segmentInfos.Contains(merge.info));
                    readerPool.Drop(merge.info);
                    deleter.DeleteNewFiles(merge.info.GetFiles());
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
                        catch (Exception t) when (t.IsThrowable())
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

                if (merge.MaxNumSegments != -1 && !dropSegment)
                {
                    // cascade the forceMerge:
                    if (!segmentsToMerge.ContainsKey(merge.info))
                    {
                        segmentsToMerge[merge.info] = false;
                    }
                }

                return true;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
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
                if (merge.isExternal)
                {
                    ExceptionDispatchInfo.Capture(t).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
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
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void Merge(MergePolicy.OneMerge merge)
        {
            bool success = false;

            long t0 = Time.NanoTime() / Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results

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
                    catch (Exception t) when (t.IsThrowable())
                    {
                        HandleMergeException(t, merge);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Enter(this);
                    try
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
                                deleter.Refresh(merge.info.Info.Name);
                            }
                        }

                        // this merge (and, generally, any change to the
                        // segments) may now enable new merges, so we call
                        // merge policy & update pending merges.
                        if (success && !merge.IsAborted && (merge.MaxNumSegments != -1 || (!closed && !closing)))
                        {
                            UpdatePendingMerges(MergeTrigger.MERGE_FINISHED, merge.MaxNumSegments);
                        }
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "Merge");
            }
            if (merge.info != null && !merge.IsAborted)
            {
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "merge time " + ((Time.NanoTime() / Time.MillisecondsPerNanosecond) - t0) + " msec for " + merge.info.Info.DocCount + " docs"); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
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
        /// already participating in a merge.  If not, this merge
        /// is "registered", meaning we record that its segments
        /// are now participating in a merge, and <c>true</c> is
        /// returned.  Else (the merge conflicts) <c>false</c> is
        /// returned.
        /// </summary>
        internal bool RegisterMerge(MergePolicy.OneMerge merge)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (merge.registerDone)
                {
                    return true;
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(merge.Segments.Count > 0);

                if (stopMerges)
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
                    if (segmentsToMerge.ContainsKey(info))
                    {
                        merge.MaxNumSegments = mergeMaxNumSegments;
                    }
                }

                EnsureValidMerge(merge);

                pendingMerges.Enqueue(merge);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "add merge to pendingMerges: " + SegString(merge.Segments) + " [total " + pendingMerges.Count + " pending]");
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
                    foreach (SegmentCommitInfo info in mergingSegments)
                    {
                        builder.Append(info.Info.Name).Append(", ");
                    }
                    builder.Append(']');
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

                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(merge.EstimatedMergeBytes == 0);
                    Debugging.Assert(merge.totalMergeBytes == 0);
                }
                foreach (SegmentCommitInfo info in merge.Segments)
                {
                    if (info.Info.DocCount > 0)
                    {
                        int delCount = NumDeletedDocs(info);
                        if (Debugging.AssertsEnabled) Debugging.Assert(delCount <= info.Info.DocCount);
                        double delRatio = ((double)delCount) / info.Info.DocCount;
                        merge.EstimatedMergeBytes += (long)(info.GetSizeInBytes() * (1.0 - delRatio));
                        merge.totalMergeBytes += info.GetSizeInBytes();
                    }
                }

                // Merge is now registered
                merge.registerDone = true;

                return true;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Does initial setup for a merge, which is fast but holds
        /// the synchronized lock on <see cref="IndexWriter"/> instance.
        /// </summary>
        internal void MergeInit(MergePolicy.OneMerge merge)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                bool success = false;
                try
                {
                    MergeInitImpl(merge);
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
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void MergeInitImpl(MergePolicy.OneMerge merge) // LUCENENET specific: renamed from _mergeInit
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(TestPoint("startMergeInit"));

                    Debugging.Assert(merge.registerDone);
                    Debugging.Assert(merge.MaxNumSegments == -1 || merge.MaxNumSegments > 0);
                }

                if (hitOOM)
                {
                    throw IllegalStateException.Create("this writer hit an OutOfMemoryError; cannot merge");
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
                BufferedUpdatesStream.ApplyDeletesResult result = bufferedUpdatesStream.ApplyDeletesAndUpdates(readerPool, merge.Segments);

                if (result.AnyDeletes)
                {
                    Checkpoint();
                }

                if (!keepFullyDeletedSegments && result.AllDeleted != null)
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
                SegmentInfo si = new SegmentInfo(directory, Constants.LUCENE_MAIN_VERSION, mergeSegmentName, -1, false, codec, null);
                IDictionary<string, string> details = new Dictionary<string, string>
                {
                    ["mergeMaxNumSegments"] = "" + merge.MaxNumSegments,
                    ["mergeFactor"] = Convert.ToString(merge.Segments.Count)
                };
                SetDiagnostics(si, SOURCE_MERGE, details);
                merge.Info = new SegmentCommitInfo(si, 0, -1L, -1L);

                //    System.out.println("[" + Thread.currentThread().getName() + "] IW._mergeInit: " + segString(merge.segments) + " into " + si);

                // Lock order: IW -> BD
                bufferedUpdatesStream.Prune(segmentInfos);

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "merge seg=" + merge.info.Info.Name + " " + SegString(merge.Segments));
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal static void SetDiagnostics(SegmentInfo info, string source)
        {
            SetDiagnostics(info, source, null);
        }

        private static void SetDiagnostics(SegmentInfo info, string source, IDictionary<string, string> details)
        {
            IDictionary<string, string> diagnostics = new Dictionary<string, string>
            {
                ["source"] = source,
                ["lucene.version"] = Constants.LUCENE_VERSION,
                ["os"] = Constants.OS_NAME,
                ["os.arch"] = Constants.OS_ARCH,
                ["os.version"] = Constants.OS_VERSION,
                ["java.version"] = Constants.RUNTIME_VERSION,
                ["java.vendor"] = Constants.RUNTIME_VENDOR,
                ["timestamp"] = Convert.ToString((DateTime.Now))
            };
            if (details != null)
            {
                diagnostics.PutAll(details);
            }
            info.Diagnostics = diagnostics;
        }

        /// <summary>
        /// Does fininishing for a merge, which is fast but holds
        /// the synchronized lock on <see cref="IndexWriter"/> instance.
        /// </summary>
        public void MergeFinish(MergePolicy.OneMerge merge)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // forceMerge, addIndexes or finishMerges may be waiting
                // on merges to finish.
                UninterruptableMonitor.PulseAll(this);

                // It's possible we are called twice, eg if there was an
                // exception inside mergeInit
                if (merge.registerDone)
                {
                    IList<SegmentCommitInfo> sourceSegments = merge.Segments;
                    foreach (SegmentCommitInfo info in sourceSegments)
                    {
                        mergingSegments.Remove(info);
                    }
                    merge.registerDone = false;
                }

                runningMerges.Remove(merge);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void CloseMergeReaders(MergePolicy.OneMerge merge, bool suppressExceptions)
        {
            UninterruptableMonitor.Enter(this);
            try
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
                            ReadersAndUpdates rld = readerPool.Get(sr.SegmentInfo, false);
                            // We still hold a ref so it should not have been removed:
                            if (Debugging.AssertsEnabled) Debugging.Assert(rld != null);
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
                        catch (Exception t) when (t.IsThrowable())
                        {
                            if (th is null)
                            {
                                th = t;
                            }
                        }
                        merge.readers[i] = null;
                    }
                }

                // If any error occured, throw it.
                if (!suppressExceptions)
                {
                    IOUtils.ReThrow(th);
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Does the actual (time-consuming) work of the merge,
        /// but without holding synchronized lock on <see cref="IndexWriter"/>
        /// instance
        /// </summary>
        private int MergeMiddle(MergePolicy.OneMerge merge)
        {
            merge.CheckAborted(directory);

            string mergedName = merge.info.Info.Name;

            IList<SegmentCommitInfo> sourceSegments = merge.Segments;

            IOContext context = new IOContext(merge.MergeInfo);

            CheckAbort checkAbort = new CheckAbort(merge, directory);
            TrackingDirectoryWrapper dirWrapper = new TrackingDirectoryWrapper(directory);

            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "merging " + SegString(merge.Segments));
            }

            merge.readers = new JCG.List<SegmentReader>();

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
                    IBits liveDocs;
                    int delCount;

                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        // Must sync to ensure BufferedDeletesStream cannot change liveDocs,
                        // pendingDeleteCount and field updates while we pull a copy:
                        reader = rld.GetReaderForMerge(context);
                        liveDocs = rld.GetReadOnlyLiveDocs();
                        delCount = rld.PendingDeleteCount + info.DelCount;

                        if (Debugging.AssertsEnabled)
                        {
                            Debugging.Assert(reader != null);
                            Debugging.Assert(rld.VerifyDocCounts());
                        }

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
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }

                    // Deletes might have happened after we pulled the merge reader and
                    // before we got a read-only copy of the segment's actual live docs
                    // (taking pending deletes into account). In that case we need to
                    // make a new reader with updated live docs and del count.
                    if (reader.NumDeletedDocs != delCount)
                    {
                        // fix the reader's live docs and del count
                        if (Debugging.AssertsEnabled) Debugging.Assert(delCount > reader.NumDeletedDocs); // beware of zombies

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

                    merge.readers.Add(reader);
                    if (Debugging.AssertsEnabled) Debugging.Assert(delCount <= info.Info.DocCount, "delCount={0} info.DocCount={1} rld.PendingDeleteCount={2} info.DelCount=", delCount, info.Info.DocCount, rld.PendingDeleteCount, info.DelCount);
                    segUpto++;
                }

                //      System.out.println("[" + Thread.currentThread().getName() + "] IW.mergeMiddle: merging " + merge.getMergeReaders());

                // we pass merge.getMergeReaders() instead of merge.readers to allow the
                // OneMerge to return a view over the actual segments to merge
                SegmentMerger merger = new SegmentMerger(merge.GetMergeReaders(), merge.info.Info, infoStream, dirWrapper, config.TermIndexInterval, checkAbort, globalFieldNumberMap, context, config.CheckIntegrityAtMerge);

                merge.CheckAborted(directory);

                // this is where all the work happens:
                MergeState mergeState;
                bool success3 = false;
                try
                {
                    if (!merger.ShouldMerge)
                    {
                        // would result in a 0 document segment: nothing to merge!
                        mergeState = new MergeState(new JCG.List<AtomicReader>(), merge.info.Info, infoStream, checkAbort);
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
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            deleter.Refresh(merge.info.Info.Name);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
                        }
                    }
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(mergeState.SegmentInfo == merge.info.Info);
                merge.info.Info.SetFiles(new JCG.HashSet<string>(dirWrapper.CreatedFiles));

                // Record which codec was used to write the segment

                if (infoStream.IsEnabled("IW"))
                {
                    if (merge.info.Info.DocCount == 0)
                    {
                        infoStream.Message("IW", "merge away fully deleted segments");
                    }
                    else
                    {
                        infoStream.Message("IW", "merge codec=" + codec + " docCount=" + merge.info.Info.DocCount + "; merged segment has " + (mergeState.FieldInfos.HasVectors ? "vectors" : "no vectors") + "; " + (mergeState.FieldInfos.HasNorms ? "norms" : "no norms") + "; " + (mergeState.FieldInfos.HasDocValues ? "docValues" : "no docValues") + "; " + (mergeState.FieldInfos.HasProx ? "prox" : "no prox") + "; " + (mergeState.FieldInfos.HasProx ? "freqs" : "no freqs"));
                    }
                }

                // Very important to do this before opening the reader
                // because codec must know if prox was written for
                // this segment:
                //System.out.println("merger set hasProx=" + merger.hasProx() + " seg=" + merge.info.name);
                bool useCompoundFile;
                UninterruptableMonitor.Enter(this); // Guard segmentInfos
                try
                {
                    useCompoundFile = mergePolicy.UseCompoundFile(segmentInfos, merge.info);
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }

                if (useCompoundFile)
                {
                    success = false;

                    ICollection<string> filesToRemove = merge.info.GetFiles();

                    try
                    {
                        filesToRemove = CreateCompoundFile(infoStream, directory, checkAbort, merge.info.Info, context);
                        success = true;
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            if (merge.IsAborted)
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
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
                        }
                    }
                    catch (Exception t) when (t.IsThrowable())
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

                            UninterruptableMonitor.Enter(this);
                            try
                            {
                                deleter.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(mergedName, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_EXTENSION));
                                deleter.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(mergedName, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION));
                                deleter.DeleteNewFiles(merge.info.GetFiles());
                            }
                            finally
                            {
                                UninterruptableMonitor.Exit(this);
                            }
                        }
                    }

                    // So that, if we hit exc in deleteNewFiles (next)
                    // or in commitMerge (later), we close the
                    // per-segment readers in the finally clause below:
                    success = false;

                    UninterruptableMonitor.Enter(this);
                    try
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
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
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
                    codec.SegmentInfoFormat.SegmentInfoWriter.Write(directory, merge.info.Info, mergeState.FieldInfos, context);
                    success2 = true;
                }
                finally
                {
                    if (!success2)
                    {
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            deleter.DeleteNewFiles(merge.info.GetFiles());
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
                        }
                    }
                }

                // TODO: ideally we would freeze merge.info here!!
                // because any changes after writing the .si will be
                // lost...

                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", string.Format(CultureInfo.InvariantCulture, "merged segment size={0:n3} MB vs estimate={1:n3} MB", merge.info.GetSizeInBytes() / 1024.0 / 1024.0, merge.EstimatedMergeBytes / 1024 / 1024.0));
                }

                IndexReaderWarmer mergedSegmentWarmer = config.MergedSegmentWarmer;
                if (poolReaders && mergedSegmentWarmer != null && merge.info.Info.DocCount != 0)
                {
                    ReadersAndUpdates rld = readerPool.Get(merge.info, true);
                    SegmentReader sr = rld.GetReader(IOContext.READ);
                    try
                    {
                        mergedSegmentWarmer.Warm(sr);
                    }
                    finally
                    {
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            rld.Release(sr);
                            readerPool.Release(rld);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
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
            UninterruptableMonitor.Enter(this);
            try
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(merge.Exception != null);
                if (!mergeExceptions.Contains(merge) && mergeGen == merge.mergeGen)
                {
                    mergeExceptions.Add(merge);
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // For test purposes.
        internal int BufferedDeleteTermsSize => docWriter.BufferedDeleteTermsSize;

        // For test purposes.
        internal int NumBufferedDeleteTerms => docWriter.NumBufferedDeleteTerms;

        // utility routines for tests
        internal virtual SegmentCommitInfo NewestSegment()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                return segmentInfos.Count > 0 ? segmentInfos[segmentInfos.Count - 1] : null;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns a string description of all segments, for
        /// debugging.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public virtual string SegString()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                return SegString(segmentInfos.Segments);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns a string description of the specified
        /// segments, for debugging.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public virtual string SegString(IEnumerable<SegmentCommitInfo> infos)
        {
            UninterruptableMonitor.Enter(this);
            try
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
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns a string description of the specified
        /// segment, for debugging.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public virtual string SegString(SegmentCommitInfo info)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                return info.ToString(info.Info.Dir, NumDeletedDocs(info) - info.DelCount);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void DoWait()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // NOTE: the callers of this method should in theory
                // be able to do simply wait(), but, as a defense
                // against thread timing hazards where notifyAll()
                // fails to be called, we wait for at most 1 second
                // and then return so caller can check if wait
                // conditions are satisfied:
                try
                {
                    UninterruptableMonitor.Wait(this, TimeSpan.FromMilliseconds(1000));
                }
                catch (Exception ie) when (ie.IsInterruptedException())
                {
                    throw new Util.ThreadInterruptedException(ie);
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private bool keepFullyDeletedSegments;

        /// <summary>
        /// Only for testing.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public virtual bool KeepFullyDeletedSegments
        {
            get => keepFullyDeletedSegments;
            set => keepFullyDeletedSegments = value;
        }

        // called only from assert
        private bool FilesExist(SegmentInfos toSync)
        {
            ICollection<string> files = toSync.GetFiles(directory, false);
            foreach (string fileName in files)
            {
                if (Debugging.AssertsEnabled)
                {
                    // LUCENENET specific - use Directory.ListAllFormatter to defer directory listing/string building until after the condition fails
                    Debugging.Assert(SlowFileExists(directory, fileName), "file {0} does not exist; files={1}", fileName, new Directory.ListAllFormatter(directory));
                    // If this trips it means we are missing a call to
                    // .checkpoint somewhere, because by the time we
                    // are called, deleter should know about every
                    // file referenced by the current head
                    // segmentInfos:
                    Debugging.Assert(deleter.Exists(fileName), "IndexFileDeleter doesn't know about file {0}", fileName);
                }
            }
            return true;
        }

        // For infoStream output
        internal virtual SegmentInfos ToLiveInfos(SegmentInfos sis)
        {
            UninterruptableMonitor.Enter(this);
            try
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
                    if (liveSIS.TryGetValue(info, out SegmentCommitInfo liveInfo))
                    {
                        infoMod = liveInfo;
                    }
                    newSIS.Add(infoMod);
                }

                return newSIS;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Walk through all files referenced by the current
        /// <see cref="segmentInfos"/> and ask the <see cref="Store.Directory"/> to sync each file,
        /// if it wasn't already.  If that succeeds, then we
        /// prepare a new segments_N file but do not fully commit
        /// it.
        /// </summary>
        private void StartCommit(SegmentInfos toSync)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(TestPoint("startStartCommit"));
                Debugging.Assert(pendingCommit is null);
            }

            if (hitOOM)
            {
                throw IllegalStateException.Create("this writer hit an OutOfMemoryError; cannot commit");
            }

            try
            {
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "StartCommit(): start");
                }

                UninterruptableMonitor.Enter(this);
                try
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(lastCommitChangeCount <= changeCount,"lastCommitChangeCount={0} changeCount={1}", lastCommitChangeCount, changeCount);

                    if (pendingCommitChangeCount == lastCommitChangeCount)
                    {
                        if (infoStream.IsEnabled("IW"))
                        {
                            infoStream.Message("IW", "  skip StartCommit(): no changes pending");
                        }
                        deleter.DecRef(filesToCommit);
                        filesToCommit = null;
                        return;
                    }

                    if (infoStream.IsEnabled("IW"))
                    {
                        infoStream.Message("IW", "startCommit index=" + SegString(ToLiveInfos(toSync).Segments) + " changeCount=" + changeCount);
                    }

                    if (Debugging.AssertsEnabled) Debugging.Assert(FilesExist(toSync));
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(TestPoint("midStartCommit"));

                bool pendingCommitSet = false;

                try
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(TestPoint("midStartCommit2"));

                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(pendingCommit is null);

                        if (Debugging.AssertsEnabled) Debugging.Assert(segmentInfos.Generation == toSync.Generation);

                        // Exception here means nothing is prepared
                        // (this method unwinds everything it did on
                        // an exception)
                        toSync.PrepareCommit(directory);
                        //System.out.println("DONE prepareCommit");

                        pendingCommitSet = true;
                        pendingCommit = toSync;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }

                    // this call can take a long time -- 10s of seconds
                    // or more.  We do it without syncing on this:
                    bool success = false;
                    ICollection<string> filesToSync;
                    try
                    {
                        filesToSync = toSync.GetFiles(directory, false);
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
                        infoStream.Message("IW", "done all syncs: " + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", filesToSync));
                    }

                    if (Debugging.AssertsEnabled) Debugging.Assert(TestPoint("midStartCommitSuccess"));
                }
                finally
                {
                    UninterruptableMonitor.Enter(this);
                    try
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
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }
            catch (Exception oom) when (oom.IsOutOfMemoryError())
            {
                HandleOOM(oom, "StartCommit");
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(TestPoint("finishStartCommit"));
        }

        /// <summary>
        /// Returns <c>true</c> iff the index in the named directory is
        /// currently locked. </summary>
        /// <param name="directory"> the directory to check for a lock </param>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        public static bool IsLocked(Directory directory)
        {
            return directory.MakeLock(WRITE_LOCK_NAME).IsLocked();
        }

        /// <summary>
        /// Forcibly unlocks the index in the named directory.
        /// <para/>
        /// Caution: this should only be used by failure recovery code,
        /// when it is known that no other process nor thread is in fact
        /// currently accessing this index.
        /// </summary>
        public static void Unlock(Directory directory)
        {
            using var _ = directory.MakeLock(IndexWriter.WRITE_LOCK_NAME);
        }

        /// <summary>
        /// If <see cref="DirectoryReader.Open(IndexWriter, bool)"/> has
        /// been called (ie, this writer is in near real-time
        /// mode), then after a merge completes, this class can be
        /// invoked to warm the reader on the newly merged
        /// segment, before the merge commits.  This is not
        /// required for near real-time search, but will reduce
        /// search latency on opening a new near real-time reader
        /// after a merge completes.
        /// <para/>
        /// @lucene.experimental
        /// 
        /// <para/><b>NOTE</b>: <see cref="Warm(AtomicReader)"/> is called before any deletes have
        /// been carried over to the merged segment.
        /// </summary>
        public abstract class IndexReaderWarmer
        {
            /// <summary>
            /// Sole constructor. (For invocation by subclass
            /// constructors, typically implicit.)
            /// </summary>
            protected IndexReaderWarmer()
            {
            }

            /// <summary>
            /// Invoked on the <see cref="AtomicReader"/> for the newly
            /// merged segment, before that segment is made visible
            /// to near-real-time readers.
            /// </summary>
            public abstract void Warm(AtomicReader reader);
        }

        private void HandleOOM(/*OutOfMemory*/Exception oom, string location) // LUCENENET: Our handler doesn't cast to OutOfMemoryException, so we need to widen the parameter to accept Exception
        {
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "hit OutOfMemoryError inside " + location);
            }
            hitOOM = true;
            ExceptionDispatchInfo.Capture(oom).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
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
            UninterruptableMonitor.Enter(this);
            try
            {
                //System.out.println("IW.nrtIsCurrent " + (infos.version == segmentInfos.version && !docWriter.anyChanges() && !bufferedDeletesStream.any()));
                EnsureOpen();
                if (infoStream.IsEnabled("IW"))
                {
                    infoStream.Message("IW", "nrtIsCurrent: infoVersion matches: " + (infos.Version == segmentInfos.Version) + "; DW changes: " + docWriter.AnyChanges() + "; BD changes: " + bufferedUpdatesStream.Any());
                }
                return infos.Version == segmentInfos.Version && !docWriter.AnyChanges() && !bufferedUpdatesStream.Any();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public virtual bool IsClosed
        {
            get
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    return closed;
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// Expert: remove any index files that are no longer
        /// used.
        ///
        /// <para> <see cref="IndexWriter"/> normally deletes unused files itself,
        /// during indexing.  However, on Windows, which disallows
        /// deletion of open files, if there is a reader open on
        /// the index then those files cannot be deleted.  This is
        /// fine, because <see cref="IndexWriter"/> will periodically retry
        /// the deletion.</para>
        ///
        /// <para> However, <see cref="IndexWriter"/> doesn't try that often: only
        /// on open, close, flushing a new segment, and finishing
        /// a merge.  If you don't do any of these actions with your
        /// <see cref="IndexWriter"/>, you'll see the unused files linger.  If
        /// that's a problem, call this method to delete them
        /// (once you've closed the open readers that were
        /// preventing their deletion).</para>
        ///
        /// <para> In addition, you can call this method to delete
        /// unreferenced index commits. this might be useful if you
        /// are using an <see cref="IndexDeletionPolicy"/> which holds
        /// onto index commits until some criteria are met, but those
        /// commits are no longer needed. Otherwise, those commits will
        /// be deleted the next time <see cref="Commit()"/> is called.</para>
        /// </summary>
        public virtual void DeleteUnusedFiles()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                EnsureOpen(false);
                deleter.DeletePendingFiles();
                deleter.RevisitPolicy();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        // LUCENENET specific - DeletePendingFiles() excluded because it is not referenced - IDE0051


        /// <summary>
        /// NOTE: this method creates a compound file for all files returned by
        /// info.files(). While, generally, this may include separate norms and
        /// deletion files, this <see cref="SegmentInfo"/> must not reference such files when this
        /// method is called, because they are not allowed within a compound file.
        /// </summary>
        internal static ICollection<string> CreateCompoundFile(InfoStream infoStream, Directory directory, CheckAbort checkAbort, SegmentInfo info, IOContext context)
        {
            string fileName = Index.IndexFileNames.SegmentFileName(info.Name, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_EXTENSION);
            if (infoStream.IsEnabled("IW"))
            {
                infoStream.Message("IW", "create compound file " + fileName);
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(Lucene3xSegmentInfoFormat.GetDocStoreOffset(info) == -1);
            // Now merge all added files
            ICollection<string> files = info.GetFiles();
            CompoundFileDirectory cfsDir = new CompoundFileDirectory(directory, fileName, context, true);
            // LUCENENET: Ported changes to this method from 4.8.1
            bool success = false;
            try
            {
                foreach (string file in files)
                {
                    directory.Copy(cfsDir, file, file, context);
                    checkAbort.Work(directory.FileLength(file));
                }
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(cfsDir);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(cfsDir);
                    try
                    {
                        directory.DeleteFile(fileName);
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                    }
                    try
                    {
                        directory.DeleteFile(Lucene.Net.Index.IndexFileNames.SegmentFileName(info.Name, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION));
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                    }
                }
            }

            // Replace all previous files with the CFS/CFE files:
            JCG.HashSet<string> siFiles = new JCG.HashSet<string>
            {
                fileName,
                Lucene.Net.Index.IndexFileNames.SegmentFileName(info.Name, "", Lucene.Net.Index.IndexFileNames.COMPOUND_FILE_ENTRIES_EXTENSION)
            };
            info.SetFiles(siFiles);

            return files;
        }

        /// <summary>
        /// Tries to delete the given files if unreferenced </summary>
        /// <param name="files"> the files to delete </param>
        /// <exception cref="IOException"> if an <see cref="IOException"/> occurs </exception>
        /// <seealso cref="IndexFileDeleter.DeleteNewFiles(ICollection{string})"/>
        internal void DeleteNewFiles(ICollection<string> files)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                deleter.DeleteNewFiles(files);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Cleans up residuals from a segment that could not be entirely flushed due to an error </summary>
        /// <seealso cref="IndexFileDeleter.Refresh(string)"/>
        internal void FlushFailed(SegmentInfo info)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                deleter.Refresh(info.Name);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal int Purge(bool forced)
        {
            return docWriter.PurgeBuffer(this, forced);
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
            UninterruptableMonitor.Enter(this);
            try
            {
                EnsureOpen();
                deleter.IncRef(segmentInfos, false);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal virtual void DecRefDeleter(SegmentInfos segmentInfos)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                EnsureOpen();
                deleter.DecRef(segmentInfos);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private bool ProcessEvents(bool triggerMerge, bool forcePurge)
        {
            return ProcessEvents(eventQueue, triggerMerge, forcePurge);
        }

        private bool ProcessEvents(ConcurrentQueue<IEvent> queue, bool triggerMerge, bool forcePurge)
        {
            bool processed = false;
            while (queue.TryDequeue(out IEvent @event))
            {
                processed = true;
                @event.Process(this, triggerMerge, forcePurge);
            }
            return processed;
        }

        /// <summary>
        /// Interface for internal atomic events. See <see cref="DocumentsWriter"/> for details. Events are executed concurrently and no order is guaranteed.
        /// Each event should only rely on the serializeability within it's process method. All actions that must happen before or after a certain action must be
        /// encoded inside the <see cref="Process(IndexWriter, bool, bool)"/> method.
        /// </summary>
        internal interface IEvent
        {
            /// <summary>
            /// Processes the event. this method is called by the <see cref="IndexWriter"/>
            /// passed as the first argument.
            /// </summary>
            /// <param name="writer">
            ///          the <see cref="IndexWriter"/> that executes the event. </param>
            /// <param name="triggerMerge">
            ///          <c>false</c> iff this event should not trigger any segment merges </param>
            /// <param name="clearBuffers">
            ///          <c>true</c> iff this event should clear all buffers associated with the event. </param>
            /// <exception cref="IOException">
            ///           if an <see cref="IOException"/> occurs </exception>
            void Process(IndexWriter writer, bool triggerMerge, bool clearBuffers);
        }

        /// <summary>
        /// Used only by asserts: returns <c>true</c> if the file exists
        /// (can be opened), <c>false</c> if it cannot be opened, and
        /// (unlike <see cref="File.Exists(string)"/>) throws <see cref="IOException"/> if
        /// there's some unexpected error.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool SlowFileExists(Directory dir, string fileName)
        {
            try
            {
                using (var input = dir.OpenInput(fileName, IOContext.DEFAULT)) { }
                return true;
            }
            catch (Exception e) when (e.IsNoSuchFileExceptionOrFileNotFoundException())
            {
                return false;
            }
        }
    }
}