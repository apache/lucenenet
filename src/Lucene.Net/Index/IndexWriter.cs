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

using Analyzer = Lucene.Net.Analysis.Analyzer;
using Document = Lucene.Net.Documents.Document;
using Similarity = Lucene.Net.Search.Similarity;
using Query = Lucene.Net.Search.Query;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using Lock = Lucene.Net.Store.Lock;
using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
using BitVector = Lucene.Net.Util.BitVector;
using Constants = Lucene.Net.Util.Constants;

using System.Collections.Generic;

namespace Lucene.Net.Index
{
    /// <summary>
    /// An <code>IndexWriter</code> creates and maintains an index.
    ///  <p>The <code>create</code> argument to the 
    /// <a href="#IndexWriter(org.apache.lucene.store.Directory, org.apache.lucene.analysis.Analyzer, bool)"><b>constructor</b></a>
    /// determines whether a new index is created, or whether an existing index is
    /// opened.  Note that you
    /// can open an index with <code>create=true</code> even while readers are
    /// using the index.  The old readers will continue to search
    /// the "point in time" snapshot they had opened, and won't
    /// see the newly created index until they re-open.  There are
    /// also <a href="#IndexWriter(org.apache.lucene.store.Directory, org.apache.lucene.analysis.Analyzer)"><b>constructors</b></a>
    /// with no <code>create</code> argument which
    /// will create a new index if there is not already an index at the
    /// provided path and otherwise open the existing index.</p>
    ///
    /// <p>In either case, documents are added with <a
    /// href="#addDocument(org.apache.lucene.document.Document)"><b>addDocument</b></a>
    /// and removed with <a
    /// href="#deleteDocuments(org.apache.lucene.index.Term)"><b>deleteDocuments(Term)</b></a>
    /// or <a
    /// href="#deleteDocuments(org.apache.lucene.search.Query)"><b>deleteDocuments(Query)</b></a>.
    /// A document can be updated with <a href="#updateDocument(org.apache.lucene.index.Term, org.apache.lucene.document.Document)"><b>updateDocument</b></a> 
    /// (which just deletes and then adds the entire document).
    /// When finished adding, deleting and updating documents, <a href="#Close()"><b>close</b></a> should be called.</p>
    ///
    /// <a name="flush"></a>
    /// <p>These changes are buffered in memory and periodically
    /// flushed to the {@link Directory} (during the above method
    /// calls).  A flush is triggered when there are enough
    /// buffered deletes (see {@link #setMaxBufferedDeleteTerms})
    /// or enough added documents since the last flush, whichever
    /// is sooner.  For the added documents, flushing is triggered
    /// either by RAM usage of the documents (see {@link
    /// #setRAMBufferSizeMB}) or the number of added documents.
    /// The default is to flush when RAM usage hits 16 MB.  For
    /// best indexing speed you should flush by RAM usage with a
    /// large RAM buffer.  Note that flushing just moves the
    /// internal buffered state in IndexWriter into the index, but
    /// these changes are not visible to IndexReader until either
    /// {@link #Commit()} or {@link #close} is called.  A flush may
    /// also trigger one or more segment merges which by default
    /// run with a background thread so as not to block the
    /// addDocument calls (see <a href="#mergePolicy">below</a>
    /// for changing the {@link MergeScheduler}).</p>
    ///
    /// <a name="autoCommit"></a>
    /// <p>The optional <code>autoCommit</code> argument to the <a
    /// href="#IndexWriter(org.apache.lucene.store.Directory,
    /// bool,
    /// org.apache.lucene.analysis.Analyzer)"><b>constructors</b></a>
    /// controls visibility of the changes to {@link IndexReader}
    /// instances reading the same index.  When this is
    /// <code>false</code>, changes are not visible until {@link
    /// #Close()} or {@link #Commit()} is called.  Note that changes will still be
    /// flushed to the {@link org.apache.lucene.store.Directory}
    /// as new files, but are not committed (no new
    /// <code>segments_N</code> file is written referencing the
    /// new files, nor are the files sync'd to stable storage)
    /// until {@link #Close()} or {@link #Commit()} is called.  If something
    /// goes terribly wrong (for example the JVM crashes), then
    /// the index will reflect none of the changes made since the
    /// last commit, or the starting state if commit was not called.
    /// You can also call {@link #rollback}, which closes the writer
    /// without committing any changes, and removes any index
    /// files that had been flushed but are now unreferenced.
    /// This mode is useful for preventing readers from refreshing
    /// at a bad time (for example after you've done all your
    /// deletes but before you've done your adds).  It can also be
    /// used to implement simple single-writer transactional
    /// semantics ("all or none").  You can do a two-phase commit
    /// by calling {@link #PrepareCommit()}
    /// followed by {@link #Commit()}. This is necessary when
    /// Lucene is working with an external resource (for example,
    /// a database) and both must either commit or rollback the
    /// transaction.</p>
    ///
    /// <p>When <code>autoCommit</code> is <code>true</code> then
    /// the writer will periodically commit on its own.  [<b>Deprecated</b>: Note that in 3.0, IndexWriter will
    /// no longer accept autoCommit=true (it will be hardwired to
    /// false).  You can always call {@link #Commit()} yourself
    /// when needed]. There is
    /// no guarantee when exactly an auto commit will occur (it
    /// used to be after every flush, but it is now after every
    /// completed merge, as of 2.4).  If you want to force a
    /// commit, call {@link #Commit()}, or, close the writer.  Once
    /// a commit has finished, newly opened {@link IndexReader} instances will
    /// see the changes to the index as of that commit.  When
    /// running in this mode, be careful not to refresh your
    /// readers while optimize or segment merges are taking place
    /// as this can tie up substantial disk space.</p>
    ///
    /// <p>Regardless of <code>autoCommit</code>, an {@link
    /// IndexReader} or {@link org.apache.lucene.search.IndexSearcher} will only see the
    /// index as of the "point in time" that it was opened.  Any
    /// changes committed to the index after the reader was opened
    /// are not visible until the reader is re-opened.</p>
    ///
    /// <p>If an index will not have more documents added for a while and optimal search
    /// performance is desired, then either the full <a href="#Optimize()"><b>optimize</b></a>
    /// method or partial {@link #Optimize(int)} method should be
    /// called before the index is closed.</p>
    ///
    /// <p>Opening an <code>IndexWriter</code> creates a lock file for the directory in use. Trying to open
    /// another <code>IndexWriter</code> on the same directory will lead to a
    /// {@link LockObtainFailedException}. The {@link LockObtainFailedException}
    /// is also thrown if an IndexReader on the same directory is used to delete documents
    /// from the index.</p>
    ///
    /// <a name="deletionPolicy"></a>
    /// <p>Expert: <code>IndexWriter</code> allows an optional
    /// {@link IndexDeletionPolicy} implementation to be
    /// specified.  You can use this to control when prior commits
    /// are deleted from the index.  The default policy is {@link
    /// KeepOnlyLastCommitDeletionPolicy} which removes all prior
    /// commits as soon as a new commit is done (this matches
    /// behavior before 2.2).  Creating your own policy can allow
    /// you to explicitly keep previous "point in time" commits
    /// alive in the index for some time, to allow readers to
    /// refresh to the new commit without having the old commit
    /// deleted out from under them.  This is necessary on
    /// filesystems like NFS that do not support "delete on last
    /// close" semantics, which Lucene's "point in time" search
    /// normally relies on. </p>
    ///
    /// <a name="mergePolicy"></a> <p>Expert:
    /// <code>IndexWriter</code> allows you to separately change
    /// the {@link MergePolicy} and the {@link MergeScheduler}.
    /// The {@link MergePolicy} is invoked whenever there are
    /// changes to the segments in the index.  Its role is to
    /// select which merges to do, if any, and return a {@link
    /// MergePolicy.MergeSpecification} describing the merges.  It
    /// also selects merges to do for Optimize().  (The default is
    /// {@link LogByteSizeMergePolicy}.  Then, the {@link
    /// MergeScheduler} is invoked with the requested merges and
    /// it decides when and how to run the merges.  The default is
    /// {@link ConcurrentMergeScheduler}. </p>
    /// </summary>

    /*
     * Clarification: Check Points (and commits)
     * Being able to set autoCommit=false allows IndexWriter to flush and 
     * write new index files to the directory without writing a new segments_N
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
     * With autoCommit=true, every checkPoint is also a CommitPoint.
     * With autoCommit=false, some checkPoints may not be commits.
     * 
     * A new checkpoint always replaces the previous checkpoint and 
     * becomes the new "front" of the index. This allows the IndexFileDeleter 
     * to delete files that are referenced only by stale checkpoints.
     * (files that were created since the last commit, but are no longer
     * referenced by the "front" of the index). For this, IndexFileDeleter 
     * keeps track of the last non commit checkpoint.
     */
    public class IndexWriter
    {
        private void InitBlock()
        {
            similarity = Similarity.GetDefault();
        }

        /// <summary> Default value for the write lock timeout (1,000).</summary>
        /// <seealso cref="setDefaultWriteLockTimeout">
        /// </seealso>
        public static long WRITE_LOCK_TIMEOUT = 1000;

        private long writeLockTimeout = WRITE_LOCK_TIMEOUT;

        /// <summary> Name of the write lock in the index.</summary>
        public const string WRITE_LOCK_NAME = "write.lock";

        /// <deprecated>
        /// </deprecated>
        /// <seealso cref="LogMergePolicy.DEFAULT_MERGE_FACTOR">
        /// </seealso>
        public static readonly int DEFAULT_MERGE_FACTOR = LogMergePolicy.DEFAULT_MERGE_FACTOR;

        /// <summary> Value to denote a flush trigger is disabled</summary>
        public const int DISABLE_AUTO_FLUSH = -1;

        /// <summary> Disabled by default (because IndexWriter flushes by RAM usage
        /// by default). Change using {@link #SetMaxBufferedDocs(int)}.
        /// </summary>
        public static readonly int DEFAULT_MAX_BUFFERED_DOCS = DISABLE_AUTO_FLUSH;

        /// <summary> Default value is 16 MB (which means flush when buffered
        /// docs consume 16 MB RAM).  Change using {@link #setRAMBufferSizeMB}.
        /// </summary>
        public const double DEFAULT_RAM_BUFFER_SIZE_MB = 16.0;

        /// <summary> Disabled by default (because IndexWriter flushes by RAM usage
        /// by default). Change using {@link #SetMaxBufferedDeleteTerms(int)}.
        /// </summary>
        public static readonly int DEFAULT_MAX_BUFFERED_DELETE_TERMS = DISABLE_AUTO_FLUSH;

        /// <deprecated>
        /// </deprecated>
        /// <seealso cref="LogDocMergePolicy.DEFAULT_MAX_MERGE_DOCS">
        /// </seealso>
        public static readonly int DEFAULT_MAX_MERGE_DOCS;

        /// <summary> Default value is 10,000. Change using {@link #SetMaxFieldLength(int)}.</summary>
        public const int DEFAULT_MAX_FIELD_LENGTH = 10000;

        /// <summary> Default value is 128. Change using {@link #SetTermIndexInterval(int)}.</summary>
        public const int DEFAULT_TERM_INDEX_INTERVAL = 128;

        /// <summary> Absolute hard maximum length for a term.  If a term
        /// arrives from the analyzer longer than this length, it
        /// is skipped and a message is printed to infoStream, if
        /// set (see {@link #setInfoStream}).
        /// </summary>
        public static readonly int MAX_TERM_LENGTH;

        /// <summary>
        /// Default for {@link #getMaxSyncPauseSeconds}.  On
        /// Windows this defaults to 10.0 seconds; elsewhere it's
        /// 0.
        /// </summary>
        public static readonly double DEFAULT_MAX_SYNC_PAUSE_SECONDS;

        // The normal read buffer size defaults to 1024, but
        // increasing this during merging seems to yield
        // performance gains.  However we don't want to increase
        // it too much because there are quite a few
        // BufferedIndexInputs created during merging.  See
        // LUCENE-888 for details.
        private const int MERGE_READ_BUFFER_SIZE = 4096;

        // Used for printing messages
        private static object MESSAGE_ID_LOCK = new object();
        private static int MESSAGE_ID = 0;
        private int messageID = -1;
        volatile private bool hitOOM;

        private Directory directory; // where this index resides
        private Analyzer analyzer; // how to analyze text

        private Similarity similarity; // how to normalize

        // {{dougsale-2.4.0}}:
        // per the VS compiler: "a volatile field cannot be of type 'long'"
        // use uint, same positive cardinality as Java long
        private volatile uint changeCount; // increments every time a change is completed
        private long lastCommitChangeCount; // last changeCount that was committed

        private SegmentInfos rollbackSegmentInfos; // segmentInfos we will fallback to if the commit fails
        private Dictionary<SegmentInfo, int> rollbackSegments;

        internal volatile SegmentInfos pendingCommit; // set when a commit is pending (after PrepareCommit() & before Commit())
        // {{dougsale-2.4.0}}:
        // per the VS compiler: "a volatile field cannot be of type 'long'"
        // use uint, same positive cardinality as Java long
        internal volatile uint pendingCommitChangeCount;

        private SegmentInfos localRollbackSegmentInfos; // segmentInfos we will fallback to if the commit fails
        private bool localAutoCommit; // saved autoCommit during local transaction
        private int localFlushedDocCount;  // saved docWriter.GetFlushedDocCount during local transaction
        private bool autoCommit = true; // false if we should commit only on close

        private SegmentInfos segmentInfos = new SegmentInfos(); // the segments
        private DocumentsWriter docWriter;
        private IndexFileDeleter deleter;

        private Dictionary<SegmentInfo, SegmentInfo> segmentsToOptimize = new Dictionary<SegmentInfo, SegmentInfo>(); // used by optimize to note those needing optimization

        private Lock writeLock;

        private int termIndexInterval = DEFAULT_TERM_INDEX_INTERVAL;

        private bool closeDir;
        private bool closed;
        private bool closing;

        // Holds all SegmentInfo instances currently involved in
        // merges
        private Dictionary<SegmentInfo, SegmentInfo> mergingSegments = new Dictionary<SegmentInfo, SegmentInfo>();

        private MergePolicy mergePolicy = new LogByteSizeMergePolicy();
        private MergeScheduler mergeScheduler = new ConcurrentMergeScheduler();
        private List<MergePolicy.OneMerge> pendingMerges = new List<MergePolicy.OneMerge>();
        private Dictionary<MergePolicy.OneMerge, MergePolicy.OneMerge> runningMerges = new Dictionary<MergePolicy.OneMerge, MergePolicy.OneMerge>();
        private System.Collections.IList mergeExceptions = new System.Collections.ArrayList();
        private long mergeGen;
        private bool stopMerges;

        private int flushCount;
        private int flushDeletesCount;
        private double maxSyncPauseSeconds = DEFAULT_MAX_SYNC_PAUSE_SECONDS;

        // Used to only allow one AddIndexes to proceed at once
        // TODO: use ReadWriteLock once we are on 5.0
        private int readCount;                          // count of how many threads are holding read lock
        //private SupportClass.ThreadClass writeThread;                     // non-null if any thread holds write lock
        private System.Threading.Thread writeThread;                     // non-null if any thread holds write lock

        internal void AcquireWrite()
        {
            lock (this)
            {
                while (writeThread != null || readCount > 0)
                    DoWait();

                // We could have been closed while we were waiting:
                EnsureOpen();

                //writeThread = SupportClass.ThreadClass.Current();
                writeThread = System.Threading.Thread.CurrentThread;
            }
        }

        internal void ReleaseWrite()
        {
            lock (this)
            {
                //System.Diagnostics.Debug.Assert(SupportClass.ThreadClass.Current() == writeThread);
                System.Diagnostics.Debug.Assert(System.Threading.Thread.CurrentThread == writeThread);
                writeThread = null;
                System.Threading.Monitor.PulseAll(this);
            }
        }

        internal void AcquireRead()
        {
            lock (this)
            {
                //SupportClass.ThreadClass current = SupportClass.ThreadClass.Current();
                System.Threading.Thread current = System.Threading.Thread.CurrentThread;
                while (writeThread != null && writeThread != current)
                    DoWait();

                readCount++;
            }
        }

        internal void ReleaseRead()
        {
            lock (this)
            {
                readCount--;
                System.Diagnostics.Debug.Assert(readCount >= 0);
                if (0 == readCount)
                    System.Threading.Monitor.PulseAll(this);
            }
        }

        /// <summary> Used internally to throw an {@link
        /// AlreadyClosedException} if this IndexWriter has been
        /// closed.
        /// </summary>
        /// <throws>  AlreadyClosedException if this IndexWriter is </throws>
        protected internal void EnsureOpen(bool includePendingClose)
        {
            lock (this)
            {
                if (closed || (includePendingClose && closing))
                {
                    throw new AlreadyClosedException("this IndexWriter is closed");
                }
            }
        }

        protected void EnsureOpen()
        {
            lock (this)
            {
                EnsureOpen(true);
            }
        }

        /// <summary> Prints a message to the infoStream (if non-null),
        /// prefixed with the identifying information for this
        /// writer and the thread that's calling it.
        /// </summary>
        public virtual void Message(string message)
        {
            if (infoStream != null)
                infoStream.WriteLine("IW " + messageID + " [" + SupportClass.ThreadClass.Current().Name + "]: " + message);
        }

        private void SetMessageID(System.IO.TextWriter infoStream)
        {
            lock (this)
            {
                if (infoStream != null && messageID == -1)
                {
                    lock (MESSAGE_ID_LOCK)
                    {
                        messageID = MESSAGE_ID++;
                    }
                }
                this.infoStream = infoStream;
            }
        }

        /// <summary> Casts current mergePolicy to LogMergePolicy, and throws
        /// an exception if the mergePolicy is not a LogMergePolicy.
        /// </summary>
        private LogMergePolicy GetLogMergePolicy()
        {
            if (mergePolicy is LogMergePolicy)
                return (LogMergePolicy)mergePolicy;
            else
                throw new System.ArgumentException("this method can only be called when the merge policy is the default LogMergePolicy");
        }

        /// <summary><p>Get the current setting of whether newly flushed
        /// segments will use the compound file format.  Note that
        /// this just returns the value previously set with
        /// setUseCompoundFile(bool), or the default value
        /// (true).  You cannot use this to query the status of
        /// previously flushed segments.</p>
        /// 
        /// <p>Note that this method is a convenience method: it
        /// just calls mergePolicy.GetUseCompoundFile as long as
        /// mergePolicy is an instance of {@link LogMergePolicy}.
        /// Otherwise an System.ArgumentException is thrown.</p>
        /// 
        /// </summary>
        /// <seealso cref="SetUseCompoundFile(bool)">
        /// </seealso>
        public virtual bool GetUseCompoundFile()
        {
            return GetLogMergePolicy().GetUseCompoundFile();
        }

        /// <summary><p>Setting to turn on usage of a compound file. When on,
        /// multiple files for each segment are merged into a
        /// single file when a new segment is flushed.</p>
        /// 
        /// <p>Note that this method is a convenience method: it
        /// just calls mergePolicy.SetUseCompoundFile as long as
        /// mergePolicy is an instance of {@link LogMergePolicy}.
        /// Otherwise an System.ArgumentException is thrown.</p>
        /// </summary>
        public virtual void SetUseCompoundFile(bool value_Renamed)
        {
            GetLogMergePolicy().SetUseCompoundFile(value_Renamed);
            GetLogMergePolicy().SetUseCompoundDocStore(value_Renamed);
        }

        /// <summary>Expert: Set the Similarity implementation used by this IndexWriter.
        /// 
        /// </summary>
        /// <seealso cref="Similarity.SetDefault(Similarity)">
        /// </seealso>
        public virtual void SetSimilarity(Similarity similarity)
        {
            EnsureOpen();
            this.similarity = similarity;
            docWriter.SetSimilarity(similarity);
        }

        /// <summary>Expert: Return the Similarity implementation used by this IndexWriter.
        /// 
        /// <p>This defaults to the current value of {@link Similarity#GetDefault()}.
        /// </summary>
        public virtual Similarity GetSimilarity()
        {
            EnsureOpen();
            return this.similarity;
        }

        /// <summary>Expert: Set the interval between indexed terms.  Large values cause less
        /// memory to be used by IndexReader, but slow random-access to terms.  Small
        /// values cause more memory to be used by an IndexReader, and speed
        /// random-access to terms.
        /// 
        /// This parameter determines the amount of computation required per query
        /// term, regardless of the number of documents that contain that term.  In
        /// particular, it is the maximum number of other terms that must be
        /// scanned before a term is located and its frequency and position information
        /// may be processed.  In a large index with user-entered query terms, query
        /// processing time is likely to be dominated not by term lookup but rather
        /// by the processing of frequency and positional data.  In a small index
        /// or when many uncommon query terms are generated (e.g., by wildcard
        /// queries) term lookup may become a dominant cost.
        /// 
        /// In particular, <code>numUniqueTerms/interval</code> terms are read into
        /// memory by an IndexReader, and, on average, <code>interval/2</code> terms
        /// must be scanned for each random term access.
        /// 
        /// </summary>
        /// <seealso cref="DEFAULT_TERM_INDEX_INTERVAL">
        /// </seealso>
        public virtual void SetTermIndexInterval(int interval)
        {
            EnsureOpen();
            this.termIndexInterval = interval;
        }

        /// <summary>Expert: Return the interval between indexed terms.
        /// 
        /// </summary>
        /// <seealso cref="SetTermIndexInterval(int)">
        /// </seealso>
        public virtual int GetTermIndexInterval()
        {
            // we pass false because this method is called by SegmentMerger while we are in the process of closing
            EnsureOpen(false);
            return termIndexInterval;
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.  If <code>create</code>
        /// is true, then a new, empty index will be created in
        /// <code>path</code>, replacing the index already there, if any.
        /// <para>
        /// NOTE: autoCommit (see above) is set to false with this constructor.
        /// </para>
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to, or if it does not exist and <code>create</code> is <code>false</code> or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="path">the path to the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="create"><code>true</code> to create the index or overwrite the existing one; <code>false</code> to append to the existing index</param>
        /// <param name="mfl">Maximum field Length: LIMITED, UNLIMITED, or user-specified</param>
        public IndexWriter(string path, Analyzer a, bool create, MaxFieldLength mfl)
        {
            InitBlock();
            Init(FSDirectory.GetDirectory(path), a, create, true, null, false, mfl.GetLimit());
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.  If <code>create</code>
        /// is true, then a new, empty index will be created in
        /// <code>path</code>, replacing the index already there, if any.
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to, or if it does not exist and <code>create</code> is <code>false</code> or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="path">the path to the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="create"><code>true</code> to create the index or overwrite the existing one; <code>false</code> to append to the existing index</param>
        [System.Obsolete("This constructor will be removed in the 3.0 release.  Use IndexWriter(string, Analyzer, bool, MaxFieldLength) instead, and call Commit() when needed")]
        public IndexWriter(string path, Analyzer a, bool create)
        {
            InitBlock();
            Init(FSDirectory.GetDirectory(path), a, create, true, null, true, DEFAULT_MAX_FIELD_LENGTH);
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.  If <code>create</code>
        /// is true, then a new, empty index will be created in
        /// <code>path</code>, replacing the index already there, if any.
        /// <para>
        /// NOTE: autoCommit (see above) is set to false with this constructor.
        /// </para>
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to, or if it does not exist and <code>create</code> is <code>false</code> or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="path">the path to the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="create"><code>true</code> to create the index or overwrite the existing one; <code>false</code> to append to the existing index</param>
        /// <param name="mfl">Maximum field Length: LIMITED, UNLIMITED, or user-specified</param>
        public IndexWriter(System.IO.FileInfo path, Analyzer a, bool create, MaxFieldLength mfl)
        {
            InitBlock();
            Init(FSDirectory.GetDirectory(path), a, create, true, null, false, mfl.GetLimit());
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.  If <code>create</code>
        /// is true, then a new, empty index will be created in
        /// <code>path</code>, replacing the index already there, if any.
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to, or if it does not exist and <code>create</code> is <code>false</code> or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="path">the path to the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="create"><code>true</code> to create the index or overwrite the existing one; <code>false</code> to append to the existing index</param>
        [System.Obsolete("This constructor will be removed in the 3.0 release.  Use IndexWriter(FileInfo, Analyzer, bool, MaxFieldLength) instead, and call Commit() when needed")]
        public IndexWriter(System.IO.FileInfo path, Analyzer a, bool create)
        {
            InitBlock();
            Init(FSDirectory.GetDirectory(path), a, create, true, null, true, DEFAULT_MAX_FIELD_LENGTH);
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.  If <code>create</code>
        /// is true, then a new, empty index will be created in
        /// <code>d</code>, replacing the index already there, if any.
        /// <para>
        /// NOTE: autoCommit (see above) is set to false with this constructor.
        /// </para>
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to, or if it does not exist and <code>create</code> is <code>false</code> or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="d">the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="create"><code>true</code> to create the index or overwrite the existing one; <code>false</code> to append to the existing index</param>
        /// <param name="mfl">Maximum field Length: LIMITED, UNLIMITED, or user-specified</param>
        public IndexWriter(Directory d, Analyzer a, bool create, MaxFieldLength mfl)
        {
            InitBlock();
            Init(d, a, create, false, null, false, mfl.GetLimit());
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.  If <code>create</code>
        /// is true, then a new, empty index will be created in
        /// <code>d</code>, replacing the index already there, if any.
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to, or if it does not exist and <code>create</code> is <code>false</code> or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="d">the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="create"><code>true</code> to create the index or overwrite the existing one; <code>false</code> to append to the existing index</param>
        [System.Obsolete("This constructor will be removed in the 3.0 release.  Use IndexWriter(Directory, Analyzer, bool, MaxFieldLength) instead, and call Commit() when needed")]
        public IndexWriter(Directory d, Analyzer a, bool create)
        {
            InitBlock();
            Init(d, a, create, false, null, true, DEFAULT_MAX_FIELD_LENGTH);
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.
        /// <para>
        /// NOTE: autoCommit (see above) is set to false with this constructor.
        /// </para>
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="path">the path to the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="mfl">Maximum field Length: LIMITED, UNLIMITED, or user-specified</param>
        public IndexWriter(string path, Analyzer a, MaxFieldLength mfl)
        {
            InitBlock();
            Init(FSDirectory.GetDirectory(path), a, true, null, false, mfl.GetLimit());
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="path">the path to the index directory</param>
        /// <param name="a">the analyzer to use</param>
        [System.Obsolete("This constructor will be removed in the 3.0 release.  Use IndexWriter(string, Analyzer, MaxFieldLength) instead, and call Commit() when needed")]
        public IndexWriter(string path, Analyzer a)
        {
            InitBlock();
            Init(FSDirectory.GetDirectory(path), a, true, null, true, DEFAULT_MAX_FIELD_LENGTH);
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.
        /// <para>
        /// NOTE: autoCommit (see above) is set to false with this constructor.
        /// </para>
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="path">the path to the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="mfl">Maximum field Length: LIMITED, UNLIMITED, or user-specified</param>
        public IndexWriter(System.IO.FileInfo path, Analyzer a, MaxFieldLength mfl)
        {
            InitBlock();
            Init(FSDirectory.GetDirectory(path), a, true, null, false, mfl.GetLimit());
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="path">the path to the index directory</param>
        /// <param name="a">the analyzer to use</param>
        [System.Obsolete("This constructor will be removed in the 3.0 release.  Use IndexWriter(FileInfo, Analyzer, MaxFieldLength) instead, and call Commit() when needed")]
        public IndexWriter(System.IO.FileInfo path, Analyzer a)
        {
            InitBlock();
            Init(FSDirectory.GetDirectory(path), a, true, null, true, DEFAULT_MAX_FIELD_LENGTH);
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.
        /// <para>
        /// NOTE: autoCommit (see above) is set to false with this constructor.
        /// </para>
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="d">the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="mfl">Maximum field Length: LIMITED, UNLIMITED, or user-specified</param>
        public IndexWriter(Directory d, Analyzer a, MaxFieldLength mfl)
        {
            InitBlock();
            Init(d, a, false, null, false, mfl.GetLimit());
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="d">the index directory</param>
        /// <param name="a">the analyzer to use</param>
        [System.Obsolete("This constructor will be removed in the 3.0 release.  Use IndexWriter(Directory, Analyzer, MaxFieldLength) instead, and call Commit() when needed")]
        public IndexWriter(Directory d, Analyzer a)
        {
            InitBlock();
            Init(d, a, false, null, true, DEFAULT_MAX_FIELD_LENGTH);
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="d">the index directory</param>
        /// <param name="autoCommit">see autoCommit above</param>
        /// <param name="a">the analyzer to use</param>
        [System.Obsolete("This constructor will be removed in the 3.0 release.  Use IndexWriter(Directory, Analyzer, MaxFieldLength) instead, and call Commit() when needed")]
        public IndexWriter(Directory d, bool autoCommit, Analyzer a)
        {
            InitBlock();
            Init(d, a, false, null, autoCommit, DEFAULT_MAX_FIELD_LENGTH);
        }

        /// <summary>
        /// Constructs an IndexWriter for the index in <code>path</code>.
        /// Text will be analyzed with <code>a</code>.  If <code>create</code>
        /// is true, then a new, empty index will be created in
        /// <code>d</code>, replacing the index already there, if any.
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to, or if it does not exist and <code>create</code> is <code>false</code> or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="path">the path to the index directory</param>
        /// <param name="autoCommit">see autoCommit above</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="create"><code>true</code> to create the index or overwrite the existing one; <code>false</code> to append to the existing index</param>
        [System.Obsolete("This constructor will be removed in the 3.0 release.  Use IndexWriter(Directory, Analyzer, bool, MaxFieldLength) instead, and call Commit() when needed")]
        public IndexWriter(Directory d, bool autoCommit, Analyzer a, bool create)
        {
            InitBlock();
            Init(d, a, create, false, null, autoCommit, DEFAULT_MAX_FIELD_LENGTH);
        }

        /// <summary>
        /// Expert: constructs an IndexWriter with a custom 
        /// IndexDeletionPolicy, for the index in <code>d</code>,
        /// first creating it if it does not already exist.  Text
        /// will be analyzed with <code>a</code>.
        /// </summary>
        /// <para>
        /// NOTE: autoCommit (see above) is set to false with this constructor.
        /// </para>
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="d">the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="deletionPolicy">see deletionPolicy above</param>
        /// <param name="mfl">Maximum field Length: LIMITED, UNLIMITED, or user-specified</param>
        public IndexWriter(Directory d, Analyzer a, IndexDeletionPolicy deletionPolicy, MaxFieldLength mfl)
        {
            InitBlock();
            Init(d, a, false, deletionPolicy, false, mfl.GetLimit());
        }

        /// <summary>
        /// Expert: constructs an IndexWriter with a custom 
        /// IndexDeletionPolicy, for the index in <code>d</code>,
        /// first creating it if it does not already exist.  Text
        /// will be analyzed with <code>a</code>.
        /// </summary>
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="d">the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="deletionPolicy">see deletionPolicy above</param>
        /// <param name="mfl">Maximum field Length: LIMITED, UNLIMITED, or user-specified</param>
        [System.Obsolete("This constructor will be removed in the 3.0 release.  Use IndexWriter(Directory, Analyzer, IndexDeletionPolicy, MaxFieldLength) instead, and call Commit() when needed")]
        public IndexWriter(Directory d, bool autoCommit, Analyzer a, IndexDeletionPolicy deletionPolicy)
        {
            InitBlock();
            Init(d, a, false, deletionPolicy, autoCommit, DEFAULT_MAX_FIELD_LENGTH);
        }

        /// <summary>
        /// Expert: constructs an IndexWriter with a custom 
        /// IndexDeletionPolicy, for the index in <code>d</code>,
        /// first creating it if it does not already exist.  Text
        /// will be analyzed with <code>a</code>.  If <code>create</code>
        /// is true, then a new, empty index will be created in
        /// <code>d</code>, replacing the index already there, if any.
        /// </summary>
        /// <para>
        /// NOTE: autoCommit (see above) is set to false with this constructor.
        /// </para>
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to, or if it does not exist and <code>create</code> is <code>false</code> or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="d">the index directory</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="create"><code>true</code> to create the index or overwrite the existing one; <code>false</code> to append to the existing index</param>
        /// <param name="deletionPolicy">see deletionPolicy above</param>
        /// <param name="mfl">Maximum field Length: LIMITED, UNLIMITED, or user-specified</param>
        public IndexWriter(Directory d, Analyzer a, bool create, IndexDeletionPolicy deletionPolicy, MaxFieldLength mfl)
        {
            InitBlock();
            Init(d, a, create, false, deletionPolicy, false, mfl.GetLimit());
        }

        /// <summary>
        /// Expert: constructs an IndexWriter with a custom 
        /// IndexDeletionPolicy, for the index in <code>d</code>,
        /// first creating it if it does not already exist.  Text
        /// will be analyzed with <code>a</code>.  If <code>create</code>
        /// is true, then a new, empty index will be created in
        /// <code>d</code>, replacing the index already there, if any.
        /// </summary>
        /// <para>Throws CorruptIndexException if the index is corrupt</para>
        /// <para>Throws LockObtainFailedException if another writer has this index open (<code>write.lock</code> could not be obtained)</para>
        /// <para>Throws System.IO.IOException if the directory cannot be read/written to, or if it does not exist and <code>create</code> is <code>false</code> or if there is any other low-level IO error</para>
        /// </summary>
        /// <param name="d">the index directory</param>
        /// <param name="autoCommit">see autoCommit above</param>
        /// <param name="a">the analyzer to use</param>
        /// <param name="create"><code>true</code> to create the index or overwrite the existing one; <code>false</code> to append to the existing index</param>
        /// <param name="deletionPolicy">see deletionPolicy above</param>
        /// <param name="mfl">Maximum field Length: LIMITED, UNLIMITED, or user-specified</param>
        [System.Obsolete("This constructor will be removed in the 3.0 release.  Use IndexWriter(Directory, Analyzer, bool, IndexDeletionPolicy, MaxFieldLength) instead, and call Commit() when needed")]
        public IndexWriter(Directory d, bool autoCommit, Analyzer a, bool create, IndexDeletionPolicy deletionPolicy)
        {
            InitBlock();
            Init(d, a, create, false, deletionPolicy, autoCommit, DEFAULT_MAX_FIELD_LENGTH);
        }

        private void Init(Directory d, Analyzer a, bool closeDir, IndexDeletionPolicy deletionPolicy, bool autoCommit, int maxFieldLength)
        {
            if (IndexReader.IndexExists(d))
            {
                Init(d, a, false, closeDir, deletionPolicy, autoCommit, maxFieldLength);
            }
            else
            {
                Init(d, a, true, closeDir, deletionPolicy, autoCommit, maxFieldLength);
            }
        }

        private void Init(Directory d, Analyzer a, bool create, bool closeDir, IndexDeletionPolicy deletionPolicy, bool autoCommit, int maxFieldLength)
        {
            this.closeDir = closeDir;
            directory = d;
            analyzer = a;
            SetMessageID(defaultInfoStream);
            this.maxFieldLength = maxFieldLength;

            if (create)
            {
                // Clear the write lock in case it's leftover:
                directory.ClearLock(WRITE_LOCK_NAME);
            }

            Lock writeLock = directory.MakeLock(WRITE_LOCK_NAME);
            if (!writeLock.Obtain(writeLockTimeout))
            // obtain write lock
            {
                throw new LockObtainFailedException("Index locked for write: " + writeLock);
            }
            this.writeLock = writeLock; // save it

            try
            {
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
                    catch (System.IO.IOException)
                    {
                        // Likely this means it's a fresh directory
                    }
                    segmentInfos.Commit(directory);
                }
                else
                {
                    segmentInfos.Read(directory);

                    // We assume that this segments_N was previously
                    // properly sync'd:
                    for (int i = 0; i < segmentInfos.Count; i++)
                    {
                        SegmentInfo info = segmentInfos.Info(i);
                        List<string> files = info.Files();
                        for (int j = 0; j < files.Count; j++)
                            synced[files[j]] = files[j];
                    }
                }

                this.autoCommit = autoCommit;
                SetRollbackSegmentInfos(segmentInfos);

                docWriter = new DocumentsWriter(directory, this);
                docWriter.SetInfoStream(infoStream);
                docWriter.SetMaxFieldLength(maxFieldLength);

                // Default deleter (for backwards compatibility) is
                // KeepOnlyLastCommitDeleter:
                deleter = new IndexFileDeleter(directory, deletionPolicy == null ? new KeepOnlyLastCommitDeletionPolicy() : deletionPolicy, segmentInfos, infoStream, docWriter);

                PushMaxBufferedDocs();

                if (infoStream != null)
                {
                    Message("init: create=" + create);
                    MessageState();
                }
            }
            catch (System.IO.IOException e)
            {
                this.writeLock.Release();
                this.writeLock = null;
                throw e;
            }
        }

        private void SetRollbackSegmentInfos(SegmentInfos infos)
        {
            lock (this)
            {
                rollbackSegmentInfos = (SegmentInfos)infos.Clone();
                System.Diagnostics.Debug.Assert(!HasExternalSegments(rollbackSegmentInfos));
                rollbackSegments = new Dictionary<SegmentInfo, int>();
                int size = rollbackSegmentInfos.Count;
                for (int i = 0; i < size; i++)
                    rollbackSegments[rollbackSegmentInfos.Info(i)] = i;
            }
        }

        /// <summary> Expert: set the merge policy used by this writer.</summary>
        public virtual void SetMergePolicy(MergePolicy mp)
        {
            EnsureOpen();
            if (mp == null)
                throw new System.NullReferenceException("MergePolicy must be non-null");

            if (mergePolicy != mp)
                mergePolicy.Close();
            mergePolicy = mp;
            PushMaxBufferedDocs();
            if (infoStream != null)
            {
                Message("setMergePolicy " + mp);
            }
        }

        /// <summary> Expert: returns the current MergePolicy in use by this writer.</summary>
        /// <seealso cref="setMergePolicy">
        /// </seealso>
        public virtual MergePolicy GetMergePolicy()
        {
            EnsureOpen();
            return mergePolicy;
        }

        /// <summary> Expert: set the merge scheduler used by this writer.</summary>
        public virtual void SetMergeScheduler(MergeScheduler mergeScheduler)
        {
            lock (this)
            {
                EnsureOpen();
                if (mergeScheduler == null)
                    throw new System.NullReferenceException("MergeScheduler must be non-null");

                if (this.mergeScheduler != mergeScheduler)
                {
                    FinishMerges(true);
                    this.mergeScheduler.Close();
                }
                this.mergeScheduler = mergeScheduler;
                if (infoStream != null)
                    Message("setMergeScheduler " + mergeScheduler);
            }
        }

        /// <summary> Expert: returns the current MergePolicy in use by this
        /// writer.
        /// </summary>
        /// <seealso cref="setMergePolicy">
        /// </seealso>
        public virtual MergeScheduler GetMergeScheduler()
        {
            EnsureOpen();
            return mergeScheduler;
        }

        /// <summary><p>Determines the largest segment (measured by
        /// document count) that may be merged with other segments.
        /// Small values (e.g., less than 10,000) are best for
        /// interactive indexing, as this limits the length of
        /// pauses while indexing to a few seconds.  Larger values
        /// are best for batched indexing and speedier
        /// searches.</p>
        /// 
        /// <p>The default value is {@link Integer#MAX_VALUE}.</p>
        /// 
        /// <p>Note that this method is a convenience method: it
        /// just calls mergePolicy.SetMaxMergeDocs as long as
        /// mergePolicy is an instance of {@link LogMergePolicy}.
        /// Otherwise an System.ArgumentException is thrown.</p>
        /// 
        /// <p>The default merge policy ({@link
        /// LogByteSizeMergePolicy}) also allows you to set this
        /// limit by net size (in MB) of the segment, using {@link
        /// LogByteSizeMergePolicy#setMaxMergeMB}.</p>
        /// </summary>
        public virtual void SetMaxMergeDocs(int maxMergeDocs)
        {
            GetLogMergePolicy().SetMaxMergeDocs(maxMergeDocs);
        }

        /// <summary> <p>Returns the largest segment (measured by document
        /// count) that may be merged with other segments.</p>
        /// 
        /// <p>Note that this method is a convenience method: it
        /// just calls mergePolicy.GetMaxMergeDocs as long as
        /// mergePolicy is an instance of {@link LogMergePolicy}.
        /// Otherwise an System.ArgumentException is thrown.</p>
        /// 
        /// </summary>
        /// <seealso cref="setMaxMergeDocs">
        /// </seealso>
        public virtual int GetMaxMergeDocs()
        {
            return GetLogMergePolicy().GetMaxMergeDocs();
        }

        /// <summary> The maximum number of terms that will be indexed for a single field in a
        /// document.  This limits the amount of memory required for indexing, so that
        /// collections with very large files will not crash the indexing process by
        /// running out of memory.  This setting refers to the number of running terms,
        /// not to the number of different terms.<p/>
        /// <strong>Note:</strong> this silently truncates large documents, excluding from the
        /// index all terms that occur further in the document.  If you know your source
        /// documents are large, be sure to set this value high enough to accomodate
        /// the expected size.  If you set it to Integer.MAX_VALUE, then the only limit
        /// is your memory, but you should anticipate an System.OutOfMemoryException.<p/>
        /// By default, no more than DEFAULT_MAX_FIELD_LENGTH terms will be indexed for a field.
        /// </summary>
        public virtual void SetMaxFieldLength(int maxFieldLength)
        {
            EnsureOpen();
            this.maxFieldLength = maxFieldLength;
            docWriter.SetMaxFieldLength(maxFieldLength);
            if (infoStream != null)
                Message("setMaxFieldLength " + maxFieldLength);
        }

        /// <summary> Returns the maximum number of terms that will be
        /// indexed for a single field in a document.
        /// </summary>
        /// <seealso cref="setMaxFieldLength">
        /// </seealso>
        public virtual int GetMaxFieldLength()
        {
            EnsureOpen();
            return maxFieldLength;
        }

        /// <summary>Determines the minimal number of documents required
        /// before the buffered in-memory documents are flushed as
        /// a new Segment.  Large values generally gives faster
        /// indexing.
        /// 
        /// <p>When this is set, the writer will flush every
        /// maxBufferedDocs added documents.  Pass in {@link
        /// #DISABLE_AUTO_FLUSH} to prevent triggering a flush due
        /// to number of buffered documents.  Note that if flushing
        /// by RAM usage is also enabled, then the flush will be
        /// triggered by whichever comes first.</p>
        /// 
        /// <p>Disabled by default (writer flushes by RAM usage).</p>
        /// 
        /// </summary>
        /// <throws>  System.ArgumentException if maxBufferedDocs is </throws>
        /// <summary> enabled but smaller than 2, or it disables maxBufferedDocs
        /// when ramBufferSize is already disabled
        /// </summary>
        /// <seealso cref="setRAMBufferSizeMB">
        /// </seealso>
        public virtual void SetMaxBufferedDocs(int maxBufferedDocs)
        {
            EnsureOpen();
            if (maxBufferedDocs != DISABLE_AUTO_FLUSH && maxBufferedDocs < 2)
                throw new System.ArgumentException("maxBufferedDocs must at least be 2 when enabled");
            if (maxBufferedDocs == DISABLE_AUTO_FLUSH && GetRAMBufferSizeMB() == DISABLE_AUTO_FLUSH)
                throw new System.ArgumentException("at least one of ramBufferSize and maxBufferedDocs must be enabled");
            docWriter.SetMaxBufferedDocs(maxBufferedDocs);
            PushMaxBufferedDocs();
            if (infoStream != null)
                Message("setMaxBufferedDocs " + maxBufferedDocs);
        }

        /// <summary> If we are flushing by doc count (not by RAM usage), and
        /// using LogDocMergePolicy then push maxBufferedDocs down
        /// as its minMergeDocs, to keep backwards compatibility.
        /// </summary>
        private void PushMaxBufferedDocs()
        {
            if (docWriter.GetMaxBufferedDocs() != DISABLE_AUTO_FLUSH)
            {
                MergePolicy mp = mergePolicy;
                if (mp is LogDocMergePolicy)
                {
                    LogDocMergePolicy lmp = (LogDocMergePolicy)mp;
                    int maxBufferedDocs = docWriter.GetMaxBufferedDocs();
                    if (lmp.GetMinMergeDocs() != maxBufferedDocs)
                    {
                        if (infoStream != null)
                            Message("now push maxBufferedDocs " + maxBufferedDocs + " to LogDocMergePolicy");
                        lmp.SetMinMergeDocs(maxBufferedDocs);
                    }
                }
            }
        }

        /// <summary> Returns the number of buffered added documents that will
        /// trigger a flush if enabled.
        /// </summary>
        /// <seealso cref="setMaxBufferedDocs">
        /// </seealso>
        public virtual int GetMaxBufferedDocs()
        {
            EnsureOpen();
            return docWriter.GetMaxBufferedDocs();
        }

        /// <summary>Determines the amount of RAM that may be used for
        /// buffering added documents before they are flushed as a
        /// new Segment.  Generally for faster indexing performance
        /// it's best to flush by RAM usage instead of document
        /// count and use as large a RAM buffer as you can.
        /// 
        /// <p>When this is set, the writer will flush whenever
        /// buffered documents use this much RAM.  Pass in {@link
        /// #DISABLE_AUTO_FLUSH} to prevent triggering a flush due
        /// to RAM usage.  Note that if flushing by document count
        /// is also enabled, then the flush will be triggered by
        /// whichever comes first.</p>
        /// 
        /// <p> The default value is {@link #DEFAULT_RAM_BUFFER_SIZE_MB}.</p>
        /// 
        /// </summary>
        /// <throws>  System.ArgumentException if ramBufferSize is </throws>
        /// <summary> enabled but non-positive, or it disables ramBufferSize
        /// when maxBufferedDocs is already disabled
        /// </summary>
        public virtual void SetRAMBufferSizeMB(double mb)
        {
            if (mb != DISABLE_AUTO_FLUSH && mb <= 0.0)
                throw new System.ArgumentException("ramBufferSize should be > 0.0 MB when enabled");
            if (mb == DISABLE_AUTO_FLUSH && GetMaxBufferedDocs() == DISABLE_AUTO_FLUSH)
                throw new System.ArgumentException("at least one of ramBufferSize and maxBufferedDocs must be enabled");
            docWriter.SetRAMBufferSizeMB(mb);
            if (infoStream != null)
                Message("setRAMBufferSizeMB " + mb);
        }

        /// <summary> Returns the value set by {@link #setRAMBufferSizeMB} if enabled.</summary>
        public virtual double GetRAMBufferSizeMB()
        {
            return docWriter.GetRAMBufferSizeMB();
        }

        /// <summary> <p>Determines the minimal number of delete terms required before the buffered
        /// in-memory delete terms are applied and flushed. If there are documents
        /// buffered in memory at the time, they are merged and a new segment is
        /// created.</p>
        /// <p>Disabled by default (writer flushes by RAM usage).</p>
        /// 
        /// </summary>
        /// <throws>  System.ArgumentException if maxBufferedDeleteTerms </throws>
        /// <summary> is enabled but smaller than 1
        /// </summary>
        /// <seealso cref="setRAMBufferSizeMB">
        /// </seealso>
        public virtual void SetMaxBufferedDeleteTerms(int maxBufferedDeleteTerms)
        {
            EnsureOpen();
            if (maxBufferedDeleteTerms != DISABLE_AUTO_FLUSH && maxBufferedDeleteTerms < 1)
                throw new System.ArgumentException("maxBufferedDeleteTerms must at least be 1 when enabled");
            docWriter.SetMaxBufferedDeleteTerms(maxBufferedDeleteTerms);
            if (infoStream != null)
                Message("setMaxBufferedDeleteTerms " + maxBufferedDeleteTerms);
        }

        /// <summary> Returns the number of buffered deleted terms that will
        /// trigger a flush if enabled.
        /// </summary>
        /// <seealso cref="setMaxBufferedDeleteTerms">
        /// </seealso>
        public virtual int GetMaxBufferedDeleteTerms()
        {
            EnsureOpen();
            return docWriter.GetMaxBufferedDeleteTerms();
        }

        /// <summary>Determines how often segment indices are merged by addDocument().  With
        /// smaller values, less RAM is used while indexing, and searches on
        /// unoptimized indices are faster, but indexing speed is slower.  With larger
        /// values, more RAM is used during indexing, and while searches on unoptimized
        /// indices are slower, indexing is faster.  Thus larger values (> 10) are best
        /// for batch index creation, and smaller values (< 10) for indices that are
        /// interactively maintained.
        /// 
        /// <p>Note that this method is a convenience method: it
        /// just calls mergePolicy.SetMergeFactor as long as
        /// mergePolicy is an instance of {@link LogMergePolicy}.
        /// Otherwise an System.ArgumentException is thrown.</p>
        /// 
        /// <p>This must never be less than 2.  The default value is 10.
        /// </summary>
        public virtual void SetMergeFactor(int mergeFactor)
        {
            GetLogMergePolicy().SetMergeFactor(mergeFactor);
        }

        /// <summary> <p>Returns the number of segments that are merged at
        /// once and also controls the total number of segments
        /// allowed to accumulate in the index.</p>
        /// 
        /// <p>Note that this method is a convenience method: it
        /// just calls mergePolicy.GetMergeFactor as long as
        /// mergePolicy is an instance of {@link LogMergePolicy}.
        /// Otherwise an System.ArgumentException is thrown.</p>
        /// 
        /// </summary>
        /// <seealso cref="setMergeFactor">
        /// </seealso>
        public virtual int GetMergeFactor()
        {
            return GetLogMergePolicy().GetMergeFactor();
        }

        /// <summary>
        /// Expert: returns max delay inserted before syncing a
        /// commit point.  On Windows, at least, pausing before
        /// syncing can increase net indexing throughput.  The
        /// delay is variable based on size of the segment's files,
        /// and is only inserted when using ConcurrentMergeScheduler for merges.
        /// </summary>
        [System.Obsolete("This will be removed in 3.0, when autoCommit=true is removed from IndexWriter.")]
        public double GetMaxSyncPauseSeconds()
        {
            return maxSyncPauseSeconds;
        }

        /// <summary>Expert: sets the max delay before syncing a commit point.</summary>
        /// <param name="seconds"></param>
        /// <para>See GetMaxSyncPauseSeconds()</para>
        [System.Obsolete("This will be removed in 3.0, when autoCommit=true is removed from IndexWriter.")]
        public void SetMaxSyncPauseSeconds(double seconds)
        {
            maxSyncPauseSeconds = seconds;
        }

        /// <summary>If non-null, this will be the default infoStream used
        /// by a newly instantiated IndexWriter.
        /// </summary>
        /// <seealso cref="setInfoStream">
        /// </seealso>
        public static void SetDefaultInfoStream(System.IO.TextWriter infoStream)
        {
            IndexWriter.defaultInfoStream = infoStream;
        }

        /// <summary> Returns the current default infoStream for newly
        /// instantiated IndexWriters.
        /// </summary>
        /// <seealso cref="setDefaultInfoStream">
        /// </seealso>
        public static System.IO.TextWriter GetDefaultInfoStream()
        {
            return IndexWriter.defaultInfoStream;
        }

        /// <summary>If non-null, information about merges, deletes and a
        /// message when maxFieldLength is reached will be printed
        /// to this.
        /// </summary>
        public virtual void SetInfoStream(System.IO.TextWriter infoStream)
        {
            EnsureOpen();
            SetMessageID(infoStream);
            docWriter.SetInfoStream(infoStream);
            deleter.SetInfoStream(infoStream);
            if (infoStream != null)
                MessageState();
        }

        private void MessageState()
        {
            Message(
                "setInfoStream: dir=" + directory +
                " autoCommit=" + autoCommit +
                " mergePolicy=" + mergePolicy +
                " mergeScheduler=" + mergeScheduler +
                " ramBufferSizeMB=" + docWriter.GetRAMBufferSizeMB() +
                " maxBufferedDocs=" + docWriter.GetMaxBufferedDocs() +
                " maxBuffereDeleteTerms=" + docWriter.GetMaxBufferedDeleteTerms() +
                " maxFieldLength=" + maxFieldLength +
                " index=" + SegString()
                );
        }

        /// <summary> Returns the current infoStream in use by this writer.</summary>
        /// <seealso cref="setInfoStream">
        /// </seealso>
        public virtual System.IO.TextWriter GetInfoStream()
        {
            EnsureOpen();
            return infoStream;
        }

        /// <seealso cref="">
        /// </seealso>
        /// <seealso cref="setDefaultWriteLockTimeout to change the default value for all instances of IndexWriter.">
        /// </seealso>
        public virtual void SetWriteLockTimeout(long writeLockTimeout)
        {
            EnsureOpen();
            this.writeLockTimeout = writeLockTimeout;
        }

        /// <summary> Returns allowed timeout when acquiring the write lock.</summary>
        /// <seealso cref="setWriteLockTimeout">
        /// </seealso>
        public virtual long GetWriteLockTimeout()
        {
            EnsureOpen();
            return writeLockTimeout;
        }

        /// <summary> Sets the default (for any instance of IndexWriter) maximum time to wait for a write lock (in
        /// milliseconds).
        /// </summary>
        public static void SetDefaultWriteLockTimeout(long writeLockTimeout)
        {
            IndexWriter.WRITE_LOCK_TIMEOUT = writeLockTimeout;
        }

        /// <summary> Returns default write lock timeout for newly
        /// instantiated IndexWriters.
        /// </summary>
        /// <seealso cref="setDefaultWriteLockTimeout">
        /// </seealso>
        public static long GetDefaultWriteLockTimeout()
        {
            return IndexWriter.WRITE_LOCK_TIMEOUT;
        }

        /// <summary>
        /// Commits all changes to an index and closes all
        /// associated files.  Note that this may be a costly
        /// operation, so try to re-use a single writer instead of
        /// closing and opening a new one.  See Commit() for
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
        /// some disk space) then you can call Close() again.
        /// Failing that, if you want to force the write lock to be
        /// released (dangerous, because you may then lose buffered
        /// docs in the IndexWriter instance) then you can do
        /// something like this:</p>
        /// 
        /// <pre>
        /// try
        /// {
        ///     writer.Close();
        /// }
        /// finally
        /// {
        ///     if (IndexWriter.isLocked(directory))
        ///     {
        ///         IndexWriter.unlock(directory);
        ///     }
        /// }
        /// </pre>
        /// 
        /// after which, you must be certain not to use the writer
        /// instance anymore.</p>
        /// </summary>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
        public virtual void Close()
        {
            Close(true);
        }

        /// <summary> Closes the index with or without waiting for currently
        /// running merges to finish.  This is only meaningful when
        /// using a MergeScheduler that runs merges in background
        /// threads.
        /// </summary>
        /// <param name="waitForMerges">if true, this call will block
        /// until all merges complete; else, it will ask all
        /// running merges to abort, wait until those merges have
        /// finished (which should be at most a few seconds), and
        /// then return.
        /// </param>
        public virtual void Close(bool waitForMerges)
        {
            // If any methods have hit System.OutOfMemoryException, then abort
            // on close, in case theinternal state of IndexWriter
            // or DocumentsWriter is corrupt
            if (hitOOM)
            {
                Rollback();
                return;
            }

            // ensure that only one thread actually gets to do the closing
            if (ShouldClose())
                CloseInternal(waitForMerges);
        }

        // returns true if this thread should attempt to close, or
        // false if IndexWriter is now closed; else, waits until another
        // thread finishes closing
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
                            // another thread is presently trying to close;
                            // wait until it finishes (successfully or not)
                            DoWait();
                        }
                    }
                    else
                        return false;
                }
            }
        }

        private void CloseInternal(bool waitForMerges)
        {
            docWriter.PauseAllThreads();

            try
            {
                if (infoStream != null)
                    Message("now flush at close");

                docWriter.Close();

                // Only allow a new merge to be triggered if we are
                // going to wait for merges:
                Flush(waitForMerges, true, true);

                if (waitForMerges)
                    // Give merge scheduler last chance to run, in case
                    // any pending merges are waiting
                    mergeScheduler.Merge(this);

                mergePolicy.Close();

                FinishMerges(waitForMerges);

                mergeScheduler.Close();

                if (infoStream != null)
                    Message("now call final commit");

                Commit(0);

                if (infoStream != null)
                    Message("at close: " + SegString());

                lock (this)
                {
                    docWriter = null;
                    deleter.Close();
                }

                if (closeDir)
                    directory.Close();

                if (writeLock != null)
                {
                    writeLock.Release(); // release write lock
                    writeLock = null;
                }

                lock (this)
                {
                    closed = true;
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
            finally
            {
                lock (this)
                {
                    closing = false;
                    System.Threading.Monitor.PulseAll(this);
                    if (!closed)
                    {
                        if (docWriter != null)
                            docWriter.ResumeAllThreads();
                        closing = false;
                        if (infoStream != null)
                            Message("hit exception while closing");
                    }
                }
            }
        }

        /// <summary>Tells the docWriter to close its currently open shared
        /// doc stores (stored fields & vectors files).
        /// Return value specifices whether new doc store files are compound or not.
        /// </summary>
        private bool FlushDocStores()
        {
            lock (this)
            {
                bool useCompoundDocStore = false;

                string docStoreSegment;

                bool success = false;
                try
                {
                    docStoreSegment = docWriter.CloseDocStore();
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        if (infoStream != null)
                            Message("hit exception closing doc store segment");
                    }
                }

                useCompoundDocStore = mergePolicy.UseCompoundDocStore(segmentInfos);

                if (useCompoundDocStore && docStoreSegment != null && docWriter.ClosedFiles().Count != 0)
                {
                    // Now build compound doc store file

                    success = false;

                    int numSegments = segmentInfos.Count;
                    string compoundFileName = docStoreSegment + "." + IndexFileNames.COMPOUND_FILE_STORE_EXTENSION;

                    try
                    {
                        CompoundFileWriter cfsWriter = new CompoundFileWriter(directory, compoundFileName);
                        IEnumerator<string> it = docWriter.ClosedFiles().GetEnumerator();
                        while (it.MoveNext())
                            cfsWriter.AddFile(it.Current);

                        // Perform the merge
                        cfsWriter.Close();
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            if (infoStream != null)
                                Message("hit exception building compound file doc store for segment " + docStoreSegment);
                            deleter.DeleteFile(compoundFileName);
                        }
                    }
                    for (int i = 0; i < numSegments; i++)
                    {
                        SegmentInfo si = segmentInfos.Info(i);
                        if (si.GetDocStoreOffset() != -1 && si.GetDocStoreSegment().Equals(docStoreSegment))
                            si.SetDocStoreIsCompoundFile(true);
                    }

                    Checkpoint();

                    // in case the files we just merged int a CFS were not previously checkpointed:
                    deleter.DeleteNewFiles(docWriter.ClosedFiles());
                }

                return useCompoundDocStore;
            }
        }

        /// <summary>Release the write lock, if needed. </summary>
        ~IndexWriter()
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
            }
        }

        /// <summary>Returns the Directory used by this index. </summary>
        public virtual Directory GetDirectory()
        {
            // pass false because the flush during closing calls GetDirectory
            EnsureOpen(false);
            return directory;
        }

        /// <summary>Returns the analyzer used by this index. </summary>
        public virtual Analyzer GetAnalyzer()
        {
            EnsureOpen();
            return analyzer;
        }

        /// <summary>Returns the number of documents currently in this index, not counting deletions. </summary>
        [System.Obsolete("Please use MaxDoc() (same as this method) or NumDocs() (takes deletions into account)")]
        public virtual int DocCount()
        {
            lock (this)
            {
                EnsureOpen();
                return MaxDoc();
            }
        }

        /// <summary>
        /// Returns total number of docs in this index, including docs not yet flushed (still in the RAM buffer),
        /// without regard for deletions (see NumDocs()).
        /// </summary>
        /// <returns></returns>
        public int MaxDoc()
        {
            lock (this)
            {
                int count;
                if (docWriter != null)
                    count = docWriter.GetNumDocsInRAM();
                else
                    count = 0;

                for (int i = 0; i < segmentInfos.Count; i++)
                    count += segmentInfos.Info(i).docCount;

                return count;
            }
        }

        /// <summary>
        /// Returns total number of docs in this index, including docs not yet flushed (still in the RAM buffer),
        /// with regard for deletions.  NOTE: Buffered deletions are not excluded.  If these need to be excluded,
        /// call Commit() first.
        /// </summary>
        /// <returns></returns>
        public int NumDocs()
        {
            lock (this)
            {
                int count;
                if (docWriter != null)
                    count = docWriter.GetNumDocsInRAM();
                else
                    count = 0;

                for (int i = 0; i < segmentInfos.Count; i++)
                {
                    SegmentInfo info = segmentInfos.Info(i);
                    count += info.docCount - info.GetDelCount();
                }

                return count;
            }
        }

        public bool HasDeletions()
        {
            lock (this)
            {
                EnsureOpen();
                if (docWriter.HasDeletes())
                    return true;
                for (int i = 0; i < segmentInfos.Count; i++)
                    if (segmentInfos.Info(i).HasDeletions())
                        return true;
                return false;
            }
        }

        /// <summary> The maximum number of terms that will be indexed for a single field in a
        /// document.  This limits the amount of memory required for indexing, so that
        /// collections with very large files will not crash the indexing process by
        /// running out of memory.<p/>
        /// Note that this effectively truncates large documents, excluding from the
        /// index terms that occur further in the document.  If you know your source
        /// documents are large, be sure to set this value high enough to accomodate
        /// the expected size.  If you set it to Integer.MAX_VALUE, then the only limit
        /// is your memory, but you should anticipate an System.OutOfMemoryException.<p/>
        /// By default, no more than 10,000 terms will be indexed for a field.
        /// </summary>
        /// <see cref="MaxFieldLength"/>
        private int maxFieldLength;

        /// <summary> Adds a document to this index.  If the document contains more than
        /// {@link #SetMaxFieldLength(int)} terms for a given field, the remainder are
        /// discarded.
        /// 
        /// <p> Note that if an Exception is hit (for example disk full)
        /// then the index will be consistent, but this document
        /// may not have been added.  Furthermore, it's possible
        /// the index will have one segment in non-compound format
        /// even when using compound files (when a merge has
        /// partially succeeded).</p>
        /// 
        /// <p> This method periodically flushes pending documents
        /// to the Directory (see Flush(), above),
        /// and also periodically triggers segment merges in the index
        /// according to the MergePolicy in use.
        /// 
        /// <p>Merges temporarily consume space in the directory.  The amout of space
        /// required is up to 1X the size of all segments being merged, when no
        /// readers/searchers are open against the index, and up to 2X the size of 
        /// all segments being merged when readers/searchers are open against the
        /// index (see Optimize() for details).  The sequence of primitive merge
        /// operations performed is governed by the merge policy.
        /// 
        /// <p>Note that each term in the document can be no longer
        /// than 16383 characters, otherwise an
        /// ArgumentException will be thrown.</p>
        /// 
        /// <para>
        /// Note that it's possible to create an invalid Unicode string in Java if a 
        /// UTF16 surrogate pair is malformed.  In this case, the invalid characters
        /// are silently replaced with the Unicode replacement character U+FFFD.
        /// </para>
        /// </summary>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
        public virtual void AddDocument(Document doc)
        {
            AddDocument(doc, analyzer);
        }

        /// <summary> Adds a document to this index, using the provided analyzer instead of the
        /// value of {@link #GetAnalyzer()}.  If the document contains more than
        /// {@link #SetMaxFieldLength(int)} terms for a given field, the remainder are
        /// discarded.
        /// 
        /// <p>See {@link #AddDocument(Document)} for details on
        /// index and IndexWriter state after an Exception, and
        /// flushing/merging temporary free space requirements.</p>
        /// 
        /// </summary>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
        public virtual void AddDocument(Document doc, Analyzer analyzer)
        {
            EnsureOpen();
            bool doFlush = false;
            bool success = false;
            try
            {
                try
                {
                    doFlush = docWriter.AddDocument(doc, analyzer);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {

                        if (infoStream != null)
                            Message("hit exception adding document");

                        lock (this)
                        {
                            // If docWriter has some aborted files that were
                            // never incref'd, then we clean them up here
                            if (docWriter != null)
                            {
                                ICollection<string> files = docWriter.AbortedFiles();
                                if (files != null)
                                    deleter.DeleteNewFiles(files);
                            }
                        }
                    }
                }
                if (doFlush)
                    Flush(true, false, false);
            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
        }

        /// <summary> Deletes the document(s) containing <code>term</code>.</summary>
        /// <param name="term">the term to identify the documents to be deleted
        /// </param>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
        public virtual void DeleteDocuments(Term term)
        {
            EnsureOpen();
            try
            {
                bool doFlush = docWriter.BufferDeleteTerm(term);
                if (doFlush)
                    Flush(true, false, false);
            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
        }

        /// <summary> Deletes the document(s) containing any of the
        /// terms. All deletes are flushed at the same time.
        /// </summary>
        /// <param name="terms">array of terms to identify the documents
        /// to be deleted
        /// </param>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
        public virtual void DeleteDocuments(Term[] terms)
        {
            EnsureOpen();
            try
            {
                bool doFlush = docWriter.BufferDeleteTerms(terms);
                if (doFlush)
                    Flush(true, false, false);
            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
        }

        /// <summary>
        /// Deletes the document(s) matching the provided query.
        /// @param query the query to identify the documents to be deleted
        /// @throws CorruptIndexException if the index is corrupt
        /// @throws System.IO.IOException if there is a low-level IO error
        /// </summary>
        public void DeleteDocuments(Query query)
        {
            EnsureOpen();
            bool doFlush = docWriter.BufferDeleteQuery(query);
            if (doFlush)
                Flush(true, false, false);
        }

        /// <summary>
        /// Deletes the document(s) matching any of the provided queries.
        /// All deletes are flushed at the same time.
        /// @param queries array of queries to identify the documents
        /// to be deleted
        /// @throws CorruptIndexException if the index is corrupt
        /// @throws System.IO.IOException if there is a low-level IO error
        /// </summary>
        public void DeleteDocuments(Query[] queries)
        {
            EnsureOpen();
            bool doFlush = docWriter.BufferDeleteQueries(queries);
            if (doFlush)
                Flush(true, false, false);
        }

        /// <summary> Updates a document by first deleting the document(s)
        /// containing <code>term</code> and then adding the new
        /// document.  The delete and then add are atomic as seen
        /// by a reader on the same index (flush may happen only after
        /// the add).
        /// </summary>
        /// <param name="term">the term to identify the document(s) to be
        /// deleted
        /// </param>
        /// <param name="doc">the document to be added
        /// </param>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
        public virtual void UpdateDocument(Term term, Document doc)
        {
            EnsureOpen();
            UpdateDocument(term, doc, GetAnalyzer());
        }

        /// <summary> Updates a document by first deleting the document(s)
        /// containing <code>term</code> and then adding the new
        /// document.  The delete and then add are atomic as seen
        /// by a reader on the same index (flush may happen only after
        /// the add).
        /// </summary>
        /// <param name="term">the term to identify the document(s) to be
        /// deleted
        /// </param>
        /// <param name="doc">the document to be added
        /// </param>
        /// <param name="analyzer">the analyzer to use when analyzing the document
        /// </param>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
        public virtual void UpdateDocument(Term term, Document doc, Analyzer analyzer)
        {
            EnsureOpen();
            try
            {
                bool doFlush = false;
                bool success = false;
                try
                {
                    doFlush = docWriter.UpdateDocument(term, doc, analyzer);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {

                        if (infoStream != null)
                            Message("hit exception updating document");

                        lock (this)
                        {
                            // If docWriter has some aborted files that were
                            // never incref'd, then we clean them up here
                            ICollection<string> files = docWriter.AbortedFiles();
                            if (files != null)
                                deleter.DeleteNewFiles(files);
                        }
                    }
                }
                if (doFlush)
                    Flush(true, false, false);

            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
        }

        // for test purpose
        public /*internal*/ int GetSegmentCount()
        {
            lock (this)
            {
                return segmentInfos.Count;
            }
        }

        // for test purpose
        public /*internal*/ int GetNumBufferedDocuments()
        {
            lock (this)
            {
                return docWriter.GetNumDocsInRAM();
            }
        }

        // for test purpose
        public /*internal*/ int GetDocCount(int i)
        {
            lock (this)
            {
                if (i >= 0 && i < segmentInfos.Count)
                {
                    return segmentInfos.Info(i).docCount;
                }
                else
                {
                    return -1;
                }
            }
        }

        // for test purpose
        public int GetFlushCount()
        {
            lock (this) { return flushCount; }
        }

        // for test purpose
        public int GetFlushDeletesCount()
        {
            lock (this) { return flushDeletesCount; }
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
                return "_" + SupportClass.Number.ToString(segmentInfos.counter++);
            }
        }

        /// <summary>If non-null, information about merges will be printed to this.</summary>
        private System.IO.TextWriter infoStream = null;
        private static System.IO.TextWriter defaultInfoStream = null;

        /// <summary> Requests an "optimize" operation on an index, priming the index
        /// for the fastest available search. Traditionally this has meant
        /// merging all segments into a single segment as is done in the
        /// default merge policy, but individaul merge policies may implement
        /// optimize in different ways.
        /// 
        /// </summary>
        /// <seealso cref="LogMergePolicy.findMergesForOptimize">
        /// 
        /// <p>It is recommended that this method be called upon completion of indexing.  In
        /// environments with frequent updates, optimize is best done during low volume times, if at all. 
        /// 
        /// </p>
        /// <p>See http://www.gossamer-threads.com/lists/lucene/java-dev/47895 for more discussion. </p>
        /// 
        /// <p>Note that this can require substantial temporary free
        /// space in the Directory (see <a target="_top"
        /// href="http://issues.apache.org/jira/browse/LUCENE-764">LUCENE-764</a>
        /// for details):</p>
        /// 
        /// <ul>
        /// <li>
        /// 
        /// <p>If no readers/searchers are open against the index,
        /// then free space required is up to 1X the total size of
        /// the starting index.  For example, if the starting
        /// index is 10 GB, then you must have up to 10 GB of free
        /// space before calling optimize.</p>
        /// 
        /// <li>
        /// 
        /// <p>If readers/searchers are using the index, then free
        /// space required is up to 2X the size of the starting
        /// index.  This is because in addition to the 1X used by
        /// optimize, the original 1X of the starting index is
        /// still consuming space in the Directory as the readers
        /// are holding the segments files open.  Even on Unix,
        /// where it will appear as if the files are gone ("ls"
        /// won't list them), they still consume storage due to
        /// "delete on last close" semantics.</p>
        /// 
        /// <p>Furthermore, if some but not all readers re-open
        /// while the optimize is underway, this will cause > 2X
        /// temporary space to be consumed as those new readers
        /// will then hold open the partially optimized segments at
        /// that time.  It is best not to re-open readers while
        /// optimize is running.</p>
        /// 
        /// </ul>
        /// 
        /// <p>The actual temporary usage could be much less than
        /// these figures (it depends on many factors).</p>
        /// 
        /// <p>In general, once the optimize completes, the total size of the
        /// index will be less than the size of the starting index.
        /// It could be quite a bit smaller (if there were many
        /// pending deletes) or just slightly smaller.</p>
        /// 
        /// <p>If an Exception is hit during Optimize(), for example
        /// due to disk full, the index will not be corrupt and no
        /// documents will have been lost.  However, it may have
        /// been partially optimized (some segments were merged but
        /// not all), and it's possible that one of the segments in
        /// the index will be in non-compound format even when
        /// using compound file format.  This will occur when the
        /// Exception is hit during conversion of the segment into
        /// compound format.</p>
        /// 
        /// <p>This call will optimize those segments present in
        /// the index when the call started.  If other threads are
        /// still adding documents and flushing segments, those
        /// newly created segments will not be optimized unless you
        /// call optimize again.</p>
        /// 
        /// </seealso>
        /// <throws>  CorruptIndexException if the index is corrupt </throws>
        /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
        public virtual void Optimize()
        {
            Optimize(true);
        }

        /// <summary> Optimize the index down to <= maxNumSegments.  If
        /// maxNumSegments==1 then this is the same as {@link
        /// #Optimize()}.
        /// </summary>
        /// <param name="maxNumSegments">maximum number of segments left
        /// in the index after optimization finishes
        /// </param>
        public virtual void Optimize(int maxNumSegments)
        {
            Optimize(maxNumSegments, true);
        }

        /// <summary>Just like {@link #Optimize()}, except you can specify
        /// whether the call should block until the optimize
        /// completes.  This is only meaningful with a
        /// {@link MergeScheduler} that is able to run merges in
        /// background threads. 
        /// </summary>
        public virtual void Optimize(bool doWait)
        {
            Optimize(1, doWait);
        }

        /// <summary>Just like {@link #Optimize(int)}, except you can
        /// specify whether the call should block until the
        /// optimize completes.  This is only meaningful with a
        /// {@link MergeScheduler} that is able to run merges in
        /// background threads. 
        /// </summary>
        public virtual void Optimize(int maxNumSegments, bool doWait)
        {
            EnsureOpen();

            if (maxNumSegments < 1)
                throw new System.ArgumentException("maxNumSegments must be >= 1; got " + maxNumSegments);

            if (infoStream != null)
                Message("optimize: index now " + SegString());

            Flush(true, false, true);

            lock (this)
            {
                ResetMergeExceptions();
                segmentsToOptimize = new Dictionary<SegmentInfo, SegmentInfo>();
                int numSegments = segmentInfos.Count;
                for (int i = 0; i < numSegments; i++)
                    if (!segmentsToOptimize.ContainsKey(segmentInfos.Info(i)))
                        segmentsToOptimize[segmentInfos.Info(i)] = segmentInfos.Info(i);

                // Now mark all pending & running merges as optimize
                // merge:
                IEnumerator<MergePolicy.OneMerge> it = pendingMerges.GetEnumerator();
                while (it.MoveNext())
                {
                    MergePolicy.OneMerge merge = it.Current;
                    merge.optimize = true;
                    merge.maxNumSegmentsOptimize = maxNumSegments;
                }

                it = runningMerges.Keys.GetEnumerator();
                while (it.MoveNext())
                {
                    MergePolicy.OneMerge merge = it.Current;
                    merge.optimize = true;
                    merge.maxNumSegmentsOptimize = maxNumSegments;
                }
            }

            MaybeMerge(maxNumSegments, true);

            if (doWait)
            {
                lock (this)
                {
                    while (true)
                    {
                        if (mergeExceptions.Count > 0)
                        {
                            // Forward any exceptions in background merge
                            // threads to the current thread:
                            int size = mergeExceptions.Count;
                            for (int i = 0; i < size; i++)
                            {
                                MergePolicy.OneMerge merge = (MergePolicy.OneMerge)mergeExceptions[0];
                                if (merge.optimize)
                                {
                                    System.IO.IOException err;
                                    System.Exception t = merge.GetException();
                                    if (t != null)
                                        err = new System.IO.IOException("background merge hit exception: " + merge.SegString(directory), t);
                                    else
                                        err = new System.IO.IOException("background merge hit exception: " + merge.SegString(directory));
                                    throw err;
                                }
                            }
                        }

                        if (OptimizeMergesPending())
                            DoWait();
                        else
                            break;
                    }
                }

                // if close is called while we are still running, throw an exception so the calling
                // thread will know the optimize did not complete
                EnsureOpen();
            }

            // NOTE: in the ConcurrentMergeScheduler case, when
            // doWait is false, we can return immediately while
            // background threads accomplish the optimization
        }

        /// <summary>Returns true if any merges in pendingMerges or
        /// runningMerges are optimization merges. 
        /// </summary>
        private bool OptimizeMergesPending()
        {
            lock (this)
            {
                IEnumerator<MergePolicy.OneMerge> it = pendingMerges.GetEnumerator();
                while (it.MoveNext())
                {
                    if (it.Current.optimize)
                        return true;
                }

                it = runningMerges.Keys.GetEnumerator();
                while (it.MoveNext())
                {
                    if (it.Current.optimize)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Just like {@link #expungeDeletes()}, except you can
        /// specify whether the call should block until the
        /// operation completes.  This is only meaningful with a
        /// {@link MergeScheduler} that is able to run merges in
        /// background threads.
        /// </summary>
        public void ExpungeDeletes(bool doWait)
        {
            EnsureOpen();

            if (infoStream != null)
                Message("expungeDeletes: index now " + SegString());

            MergePolicy.MergeSpecification spec;

            lock (this)
            {
                spec = mergePolicy.FindMergesToExpungeDeletes(segmentInfos, this);
                if (spec != null)
                {
                    int numMerges = spec.merges.Count;
                    for (int i = 0; i < numMerges; i++)
                        RegisterMerge((MergePolicy.OneMerge)spec.merges[i]);
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

                        // Check each merge that MergePolicy asked us to
                        // do, to see if any of them are still running and
                        // if any of them have hit an exception.
                        running = false;
                        for (int i = 0; i < numMerges; i++)
                        {
                            MergePolicy.OneMerge merge = (MergePolicy.OneMerge)spec.merges[i];
                            if (pendingMerges.Contains(merge) || runningMerges.ContainsKey(merge))
                                running = true;
                            System.Exception t = merge.GetException();
                            if (t != null)
                            {
                                System.IO.IOException ioe = new System.IO.IOException("background merge hit exception: " + merge.SegString(directory), t);
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
            // background threads accomplish the optimization
        }

        /// <summary>
        /// Expunges all deletes from the index.  When an index
        /// has many document deletions (or updates to existing
        /// documents), it's best to either call optimize or
        /// expungeDeletes to remove all unused data in the index
        /// associated with the deleted documents.  To see how
        /// many deletions you have pending in your index, call
        /// {@link IndexReader#numDeletedDocs}
        /// This saves disk space and memory usage while
        /// searching.  expungeDeletes should be somewhat faster
        /// than optimize since it does not insist on reducing the
        /// index to a single segment (though, this depends on the
        /// {@link MergePolicy}; see {@link
        /// MergePolicy#findMergesToExpungeDeletes}.). Note that
        /// this call does not first commit any buffered
        /// documents, so you must do so yourself if necessary.
        /// See also {@link #expungeDeletes(bool)}
        /// </summary>
        public void ExpungeDeletes()
        {
            ExpungeDeletes(true);
        }

        /// <summary> Expert: asks the mergePolicy whether any merges are
        /// necessary now and if so, runs the requested merges and
        /// then iterate (test again if merges are needed) until no
        /// more merges are returned by the mergePolicy.
        /// 
        /// Explicit calls to MaybeMerge() are usually not
        /// necessary. The most common case is when merge policy
        /// parameters have changed.
        /// </summary>
        public void MaybeMerge()
        {
            MaybeMerge(false);
        }

        private void MaybeMerge(bool optimize)
        {
            MaybeMerge(1, optimize);
        }

        private void MaybeMerge(int maxNumSegmentsOptimize, bool optimize)
        {
            UpdatePendingMerges(maxNumSegmentsOptimize, optimize);
            mergeScheduler.Merge(this);
        }

        private void UpdatePendingMerges(int maxNumSegmentsOptimize, bool optimize)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(!optimize || maxNumSegmentsOptimize > 0);

                if (stopMerges)
                    return;

                MergePolicy.MergeSpecification spec;
                if (optimize)
                {
                    spec = mergePolicy.FindMergesForOptimize(segmentInfos, this, maxNumSegmentsOptimize, segmentsToOptimize);

                    if (spec != null)
                    {
                        int numMerges = spec.merges.Count;
                        for (int i = 0; i < numMerges; i++)
                        {
                            MergePolicy.OneMerge merge = ((MergePolicy.OneMerge)spec.merges[i]);
                            merge.optimize = true;
                            merge.maxNumSegmentsOptimize = maxNumSegmentsOptimize;
                        }
                    }
                }
                else
                    spec = mergePolicy.FindMerges(segmentInfos, this);

                if (spec != null)
                {
                    int numMerges = spec.merges.Count;
                    for (int i = 0; i < numMerges; i++)
                        RegisterMerge((MergePolicy.OneMerge)spec.merges[i]);
                }
            }
        }

        /// <summary>Expert: the {@link MergeScheduler} calls this method
        /// to retrieve the next merge requested by the
        /// MergePolicy 
        /// </summary>
        public /*internal*/ virtual MergePolicy.OneMerge GetNextMerge()
        {
            lock (this)
            {
                if (pendingMerges.Count == 0)
                    return null;
                else
                {
                    // Advance the merge from pending to running
                    object tempobject;
                    tempobject = pendingMerges[0];
                    pendingMerges.RemoveAt(0);
                    MergePolicy.OneMerge merge = (MergePolicy.OneMerge)tempobject;
                    runningMerges[merge] = merge;
                    return merge;
                }
            }
        }

        /// <summary>
        /// Like getNextMerge() except only returns a merge if it's external.
        /// </summary>
        private MergePolicy.OneMerge GetNextExternalMerge()
        {
            lock (this)
            {
                if (pendingMerges.Count == 0)
                    return null;
                else
                {
                    IEnumerator<MergePolicy.OneMerge> it = pendingMerges.GetEnumerator();
                    while (it.MoveNext())
                    {
                        MergePolicy.OneMerge merge = it.Current;
                        if (merge.isExternal)
                        {
                            // Advance the merge from pending to running
                            pendingMerges.Remove(merge);
                            runningMerges[merge] = merge;
                            return merge;
                        }
                    }

                    // All existing merges do not involve external segments
                    return null;
                }
            }
        }

        /// <summary>
        /// Begin a transaction.  During a transaction, any segment
        /// merges that happen (or ram segments flushed) will not
        /// write a new segments file and will not remove any files
        /// that were present at the start of the transaction.  You
        /// must make a matched (try/finally) call to
        /// CommitTransaction() or RollbackTransaction() to finish
        /// the transaction.
        ///
        /// Note that buffered documents and delete terms are not handled
        /// within the transactions, so they must be flushed before the
        /// transaction is started.
        /// </summary>
        private void StartTransaction(bool haveWriteLock)
        {
            lock (this)
            {
                bool success = false;
                try
                {
                    if (infoStream != null)
                        Message("now start transaction");

                    System.Diagnostics.Debug.Assert(docWriter.GetNumBufferedDeleteTerms() == 0,
                        "calling StartTransaction with buffered delete terms not supported: numBufferedDeleteTerms=" + docWriter.GetNumBufferedDeleteTerms());
                    System.Diagnostics.Debug.Assert(docWriter.GetNumDocsInRAM() == 0,
                        "calling StartTransaction with buffered documents not supported: nuDocsInRAM=" + docWriter.GetNumDocsInRAM());

                    EnsureOpen();

                    // if a transaction is trying to roll back (because AddIndexes() hit an exception)
                    // then wait here until that is done
                    lock (this)
                    {
                        while (stopMerges)
                            DoWait();
                    }
                    success = true;
                }
                finally
                {
                    // release the write lock, if our caller held it, on hitting an exception
                    if (!success && haveWriteLock)
                        ReleaseWrite();
                }

                if (!haveWriteLock)
                    AcquireWrite();

                success = false;

                try
                {
                    localRollbackSegmentInfos = (SegmentInfos)segmentInfos.Clone();

                    System.Diagnostics.Debug.Assert(!HasExternalSegments(segmentInfos));

                    localAutoCommit = autoCommit;
                    localFlushedDocCount = docWriter.GetFlushedDocCount();

                    if (localAutoCommit)
                    {

                        if (infoStream != null)
                            Message("flush at StartTransaction");

                        Flush(true, false, false);
                        // Turn off auto-commit during our local transaction:
                        autoCommit = false;
                    }
                    else
                    {
                        // We must "protect" our files at this point from
                        // deletion in case we need to rollback:
                        deleter.IncRef(segmentInfos, false);
                    }

                    success = true;
                }
                finally
                {
                    if (!success)
                        FinishAddIndexes();
                }
            }
        }

        /*
        * Rolls back the transaction and restores state to where
        * we were at the start.
        */
        private void RollbackTransaction()
        {
            lock (this)
            {
                if (infoStream != null)
                    Message("now rollback transaction");

                // First restore autoCommit in case we hit an exception below:
                autoCommit = localAutoCommit;
                docWriter.SetFlushedDocCount(localFlushedDocCount);

                // must finish merges before rolling back segmentInfos
                // so merges don't hit exceptions on trying to commit
                // themselves, don't get files deleted out from under
                // them, etc
                FinishMerges(false);

                // Keep the same segmentInfos instance but replace all
                // of its SegmentInfo instances.  This is so the next
                // attempt to commit using this instance of IndexWriter
                // will always write to a new generation ("write once").
                segmentInfos.Clear();
                segmentInfos.AddRange(localRollbackSegmentInfos);
                localRollbackSegmentInfos = null;

                // this must come after we rollback segmentInfos, so
                // that if a Commit() kicks off it does not see the
                // segmentInfos with external segments
                FinishAddIndexes();

                // Ask deleter to locate unreferenced files we had
                // created & remove them:
                deleter.Checkpoint(segmentInfos, false);

                if (!autoCommit)
                    // Remove the incRef we did in StartTransaction:
                    deleter.DecRef(segmentInfos);

                // also ask deleter to remove any newly created files
                // that were never incref'd; this "garbage" is created
                // when a merge kicks off but aborts part way through
                // before it had a chance to incRef the files it had 
                // partially created
                deleter.Refresh();

                System.Threading.Monitor.PulseAll(this);

                System.Diagnostics.Debug.Assert(!HasExternalSegments());
            }
        }

        /*
        * Commits the transaction.  This will write the new
        * segments file and remove and pending deletions we have
        * accumulated during the transaction
        */
        private void CommitTransaction()
        {
            lock (this)
            {
                if (infoStream != null)
                    Message("now commit transaction");

                // First restore autoCommit in case we hit an exception below:
                autoCommit = localAutoCommit;

                // give deleter a chance to remove files now:
                Checkpoint();

                if (autoCommit)
                {
                    bool success = false;
                    try
                    {
                        Commit(0);
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            if (infoStream != null)
                                Message("hit exception committing transaction");
                            RollbackTransaction();
                        }
                    }
                }
                else
                {
                    // remove the infRef we did in StartTransaction()
                    deleter.DecRef(localRollbackSegmentInfos);
                }

                localRollbackSegmentInfos = null;

                System.Diagnostics.Debug.Assert(!HasExternalSegments());

                FinishAddIndexes();
            }
        }

        [System.Obsolete("Plese use Rollback() instead")]
        public void Abort()
        {
            Rollback();
        }

        /// <summary> Close the <code>IndexWriter</code> without committing
        /// any of the changes that have occurred since it was
        /// opened. This removes any temporary files that had been
        /// created, after which the state of the index will be the
        /// same as it was when this writer was first opened.  This
        /// can only be called when this IndexWriter was opened
        /// with <code>autoCommit=false</code>.  This also clears a previous
        /// call to PrepareCommit().
        /// </summary>
        /// <throws>  System.Exception if this is called when </throws>
        /// <summary>  the writer was opened with <code>autoCommit=true</code>.
        /// </summary>
        /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
        public virtual void Rollback()
        {
            EnsureOpen();
            if (autoCommit)
                throw new System.SystemException("Rollback() can only be called when IndexWriter was opened with autoCommit=false");

            if (ShouldClose())
                RollbackInternal();
        }

        private void RollbackInternal()
        {
            bool success = false;

            docWriter.PauseAllThreads();

            try
            {
                FinishMerges(false);

                // Must pre-close these two, in case they
                // increment changeCount, so that we can then set it to
                // false before calling closeInternal
                mergePolicy.Close();
                mergeScheduler.Close();

                lock (this)
                {
                    if (pendingCommit != null)
                    {
                        pendingCommit.RollbackCommit(directory);
                        deleter.DecRef(pendingCommit);
                        pendingCommit = null;
                        System.Threading.Monitor.PulseAll(this);
                    }

                    // Keep the same segmentInfos instance but replace all
                    // of its SegmentInfo instances.  This is so the next
                    // attempt to commit using this instance of IndexWriter
                    // will always write to a new generation ("write
                    // once").
                    segmentInfos.Clear();
                    segmentInfos.AddRange(rollbackSegmentInfos);

                    System.Diagnostics.Debug.Assert(!HasExternalSegments());

                    docWriter.Abort();

                    System.Diagnostics.Debug.Assert(TestPoint("rollback before checkpoint"));

                    // Ask deleter to locate unreferenced files & remove
                    // them:
                    deleter.Checkpoint(segmentInfos, false);
                    deleter.Refresh();
                }

                lastCommitChangeCount = changeCount;

                success = true;
            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
            finally
            {
                lock (this)
                {
                    if (!success)
                    {
                        docWriter.ResumeAllThreads();
                        closing = false;
                        System.Threading.Monitor.PulseAll(this);
                        if (infoStream != null)
                            Message("hit exception during rollback");
                    }
                }
            }

            CloseInternal(false);
        }

        private void FinishMerges(bool waitForMerges)
        {
            lock (this)
            {
                if (!waitForMerges)
                {

                    stopMerges = true;

                    // Abort all pending & running merges:
                    IEnumerator<MergePolicy.OneMerge> it = pendingMerges.GetEnumerator();
                    while (it.MoveNext())
                    {
                        MergePolicy.OneMerge merge = it.Current;
                        if (infoStream != null)
                            Message("now abort pending merge " + merge.SegString(directory));
                        merge.Abort();
                        MergeFinish(merge);
                    }
                    pendingMerges.Clear();

                    it = runningMerges.Keys.GetEnumerator();
                    while (it.MoveNext())
                    {
                        MergePolicy.OneMerge merge = it.Current;
                        if (infoStream != null)
                            Message("now abort running merge " + merge.SegString(directory));
                        merge.Abort();
                    }

                    // ensure any running AddIndexes() finishes.  It's fine
                    // if a new one attempts to start because its merges
                    // will quickly see the stopMerges == true and abort
                    AcquireRead();
                    ReleaseRead();

                    // These merges periodically check whether they have
                    // been aborted, and stop if so.  We wait here to make
                    // sure they all stop.  It should not take very long
                    // because the merge threads periodically check if
                    // they are aborted.
                    while (runningMerges.Count > 0)
                    {
                        if (infoStream != null)
                            Message("now wait for " + runningMerges.Count + " running merge to abort");

                        DoWait();
                    }

                    stopMerges = false;
                    System.Threading.Monitor.PulseAll(this);

                    System.Diagnostics.Debug.Assert(0 == mergingSegments.Count);

                    if (infoStream != null)
                        Message("all running merges have aborted");
                }
                else
                {
                    // ensure any running AddIndexes Finishes.  It's fine
                    // if a new one attempts to start because from our
                    // caller above the call will see that we are in the 
                    // process of closing, and will throw an AlreadyClosedException
                    AcquireRead();
                    ReleaseRead();
                    while (pendingMerges.Count > 0 || runningMerges.Count > 0)
                    {
                        DoWait();
                    }
                    System.Diagnostics.Debug.Assert(0 == mergingSegments.Count);
                }
            }
        }

        /*
        * Called whenever the SegmentInfos has been updated and
        * the index files referenced exist (correctly) in the
        * index directory.
        */
        private void Checkpoint()
        {
            lock (this)
            {
                changeCount++;
                deleter.Checkpoint(segmentInfos, false);
            }
        }

        private void FinishAddIndexes()
        {
            ReleaseWrite();
        }

        private void BlockAddIndexes(bool includePendingClose)
        {

            AcquireRead();

            bool success = false;
            try
            {

                // Make sure we are still open since we could have
                // waited quite a while for last AddIndexes to finish
                EnsureOpen(includePendingClose);
                success = true;
            }
            finally
            {
                if (!success)
                    ReleaseRead();
            }
        }

        private void ResumeAddIndexes()
        {
            ReleaseRead();
        }

        /** Merges all segments from an array of indexes into this index.
         * @deprecated Use {@link #addIndexesNoOptimize} instead,
         * then separately call {@link #optimize} afterwards if
         * you need to.
         * @throws CorruptIndexException if the index is corrupt
         * @throws System.IO.IOException if there is a low-level IO error
         */
        public void AddIndexes(Directory[] dirs)
        {

            EnsureOpen();

            NoDupDirs(dirs);

            // Do not allow add docs or deletes while we are running:
            docWriter.PauseAllThreads();

            try
            {

                if (infoStream != null)
                    Message("flush at AddIndexes");
                Flush(true, false, true);

                bool success = false;

                StartTransaction(false);

                try
                {

                    int docCount = 0;
                    lock (this)
                    {
                        EnsureOpen();
                        for (int i = 0; i < dirs.Length; i++)
                        {
                            SegmentInfos sis = new SegmentInfos();	  // read infos from dir
                            sis.Read(dirs[i]);
                            for (int j = 0; j < sis.Count; j++)
                            {
                                SegmentInfo info = sis.Info(j);
                                docCount += info.docCount;
                                System.Diagnostics.Debug.Assert(!segmentInfos.Contains(info));
                                segmentInfos.Add(info);	  // add each info
                            }
                        }
                    }

                    // Notify DocumentsWriter that the flushed count just increased
                    docWriter.UpdateFlushedDocCount(docCount);

                    Optimize();

                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        CommitTransaction();
                    }
                    else
                    {
                        RollbackTransaction();
                    }
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
            finally
            {
                docWriter.ResumeAllThreads();
            }
        }

        private void ResetMergeExceptions()
        {
            lock (this)
            {
                mergeExceptions = new System.Collections.ArrayList();
                mergeGen++;
            }
        }

        private void NoDupDirs(Directory[] dirs)
        {
            Dictionary<Directory, Directory> dups = new Dictionary<Directory, Directory>();
            for (int i = 0; i < dirs.Length; i++)
            {
                if (dups.ContainsKey(dirs[i]))
                    throw new System.ArgumentException("Directory " + dirs[i] + " appears more than once");
                if (dirs[i] == directory)
                    throw new System.ArgumentException("Cannot add directory to itself");
                dups[dirs[i]] = dirs[i];
            }
        }

        /**
         * Merges all segments from an array of indexes into this
         * index.
         *
         * <p>This may be used to parallelize batch indexing.  A large document
         * collection can be broken into sub-collections.  Each sub-collection can be
         * indexed in parallel, on a different thread, process or machine.  The
         * complete index can then be created by merging sub-collection indexes
         * with this method.
         *
         * <p><b>NOTE:</b> the index in each Directory must not be
         * changed (opened by a writer) while this method is
         * running.  This method does not acquire a write lock in
         * each input Directory, so it is up to the caller to
         * enforce this.
         *
         * <p><b>NOTE:</b> while this is running, any attempts to
         * add or delete documents (with another thread) will be
         * paused until this method completes.
         *
         * <p>This method is transactional in how Exceptions are
         * handled: it does not commit a new segments_N file until
         * all indexes are added.  This means if an Exception
         * occurs (for example disk full), then either no indexes
         * will have been added or they all will have been.</p>
         *
         * <p>Note that this requires temporary free space in the
         * Directory up to 2X the sum of all input indexes
         * (including the starting index).  If readers/searchers
         * are open against the starting index, then temporary
         * free space required will be higher by the size of the
         * starting index (see {@link #Optimize()} for details).
         * </p>
         *
         * <p>Once this completes, the final size of the index
         * will be less than the sum of all input index sizes
         * (including the starting index).  It could be quite a
         * bit smaller (if there were many pending deletes) or
         * just slightly smaller.</p>
         * 
         * <p>
         * This requires this index not be among those to be added.
         *
         * @throws CorruptIndexException if the index is corrupt
         * @throws System.IO.IOException if there is a low-level IO error
         */
        public void AddIndexesNoOptimize(Directory[] dirs)
        {


            EnsureOpen();

            NoDupDirs(dirs);

            // Do not allow add docs or deletes while we are running:
            docWriter.PauseAllThreads();

            try
            {
                if (infoStream != null)
                    Message("flush at addIndexesNoOptimize");
                Flush(true, false, true);

                bool success = false;

                StartTransaction(false);

                try
                {

                    int docCount = 0;
                    lock (this)
                    {
                        EnsureOpen();

                        for (int i = 0; i < dirs.Length; i++)
                        {
                            if (directory == dirs[i])
                            {
                                // cannot add this index: segments may be deleted in merge before added
                                throw new System.ArgumentException("Cannot add this index to itself");
                            }

                            SegmentInfos sis = new SegmentInfos(); // read infos from dir
                            sis.Read(dirs[i]);
                            for (int j = 0; j < sis.Count; j++)
                            {
                                SegmentInfo info = sis.Info(j);
                                System.Diagnostics.Debug.Assert(!segmentInfos.Contains(info), "dup info dir=" + info.dir + " name=" + info.name);
                                docCount += info.docCount;
                                segmentInfos.Add(info); // add each info
                            }
                        }
                    }

                    // Notify DocumentsWriter that the flushed count just increased
                    docWriter.UpdateFlushedDocCount(docCount);

                    MaybeMerge();

                    EnsureOpen();

                    // If after merging there remain segments in the index
                    // that are in a different directory, just copy these
                    // over into our index.  This is necessary (before
                    // finishing the transaction) to avoid leaving the
                    // index in an unusable (inconsistent) state.
                    ResolveExternalSegments();

                    EnsureOpen();

                    success = true;

                }
                finally
                {
                    if (success)
                    {
                        CommitTransaction();
                    }
                    else
                    {
                        RollbackTransaction();
                    }
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
            finally
            {
                docWriter.ResumeAllThreads();
            }
        }

        private bool HasExternalSegments()
        {
            return HasExternalSegments(segmentInfos);
        }

        private bool HasExternalSegments(SegmentInfos infos)
        {
            int numSegments = infos.Count;
            for (int i = 0; i < numSegments; i++)
                if (infos.Info(i).dir != directory)
                    return true;
            return false;
        }

        /* If any of our segments are using a directory != ours
         * then we have to either copy them over one by one, merge
         * them (if merge policy has chosen to) or wait until
         * currently running merges (in the background) complete.
         * We don't return until the SegmentInfos has no more
         * external segments.  Currently this is only used by
         * AddIndexesNoOptimize(). */
        private void ResolveExternalSegments()
        {

            bool any = false;

            bool done = false;

            while (!done)
            {
                SegmentInfo info = null;
                MergePolicy.OneMerge merge = null;
                lock (this)
                {

                    if (stopMerges)
                        throw new MergePolicy.MergeAbortedException("rollback() was called or AddIndexes* hit an unhandled exception");

                    int numSegments = segmentInfos.Count;

                    done = true;
                    for (int i = 0; i < numSegments; i++)
                    {
                        info = segmentInfos.Info(i);
                        if (info.dir != directory)
                        {
                            done = false;
                            MergePolicy.OneMerge newMerge = new MergePolicy.OneMerge(segmentInfos.Range(i, 1 + i), info.GetUseCompoundFile());

                            // Returns true if no running merge conflicts
                            // with this one (and, records this merge as
                            // pending), ie, this segment is not currently
                            // being merged:
                            if (RegisterMerge(newMerge))
                            {
                                merge = newMerge;

                                // If this segment is not currently being
                                // merged, then advance it to running & run
                                // the merge ourself (below):
                                pendingMerges.Remove(merge);
                                runningMerges[merge] = merge;
                                break;
                            }
                        }
                    }

                    if (!done && merge == null)
                        // We are not yet done (external segments still
                        // exist in segmentInfos), yet, all such segments
                        // are currently "covered" by a pending or running
                        // merge.  We now try to grab any pending merge
                        // that involves external segments:
                        merge = GetNextExternalMerge();

                    if (!done && merge == null)
                        // We are not yet done, and, all external segments
                        // fall under merges that the merge scheduler is
                        // currently running.  So, we now wait and check
                        // back to see if the merge has completed.
                        DoWait();
                }

                if (merge != null)
                {
                    any = true;
                    Merge(merge);
                }
            }

            if (any)
                // Sometimes, on copying an external segment over,
                // more merges may become necessary:
                mergeScheduler.Merge(this);
        }

        /** Merges the provided indexes into this index.
         * <p>After this completes, the index is optimized. </p>
         * <p>The provided IndexReaders are not closed.</p>

         * <p><b>NOTE:</b> the index in each Directory must not be
         * changed (opened by a writer) while this method is
         * running.  This method does not acquire a write lock in
         * each input Directory, so it is up to the caller to
         * enforce this.
         *
         * <p><b>NOTE:</b> while this is running, any attempts to
         * add or delete documents (with another thread) will be
         * paused until this method completes.
         *
         * <p>See {@link #AddIndexesNoOptimize(Directory[])} for
         * details on transactional semantics, temporary free
         * space required in the Directory, and non-CFS segments
         * on an Exception.</p>
         * @throws CorruptIndexException if the index is corrupt
         * @throws System.IO.IOException if there is a low-level IO error
         */
        public void AddIndexes(IndexReader[] readers)
        {

            EnsureOpen();

            // Do not allow add docs or deletes while we are running:
            docWriter.PauseAllThreads();

            // We must pre-acquire the write lock here (and not in
            // StartTransaction below) so that no other AddIndexes
            // is allowed to start up after we have flushed &
            // optimized but before we then start our transaction.
            // This is because the merging below requires that only
            // one segment is present in the index:
            AcquireWrite();

            try
            {

                bool success = false;
                SegmentInfo info = null;
                string mergedName = null;
                SegmentMerger merger = null;

                try
                {
                    Flush(true, false, true);
                    Optimize();					  // start with zero or 1 seg
                    success = true;
                }
                finally
                {
                    // Take care to release the write lock if we hit an
                    // exception before starting the transaction
                    if (!success)
                        ReleaseWrite();
                }

                success = false;

                // true means we already have write lock; if this call
                // hits an exception it will release the write lock:
                StartTransaction(true);

                try
                {
                    mergedName = NewSegmentName();
                    merger = new SegmentMerger(this, mergedName, null);

                    IndexReader sReader = null;
                    lock (this)
                    {
                        if (segmentInfos.Count == 1)
                        { // add existing index, if any
                            sReader = SegmentReader.Get(true, segmentInfos.Info(0));
                        }
                    }

                    try
                    {
                        if (sReader != null)
                            merger.Add(sReader);

                        for (int i = 0; i < readers.Length; i++)      // add new indexes
                            merger.Add(readers[i]);

                        int docCount = merger.Merge();                // merge 'em

                        if (sReader != null)
                        {
                            sReader.Close();
                            sReader = null;
                        }

                        lock (this)
                        {
                            segmentInfos.Clear();                      // pop old infos & add new
                            info = new SegmentInfo(mergedName, docCount, directory, false, true,
                                                   -1, null, false, merger.HasProx());
                            segmentInfos.Add(info);
                        }

                        // Notify DocumentsWriter that the flushed count just increased
                        docWriter.UpdateFlushedDocCount(docCount);

                        success = true;

                    }
                    finally
                    {
                        if (sReader != null)
                        {
                            sReader.Close();
                        }
                    }
                }
                finally
                {
                    if (!success)
                    {
                        if (infoStream != null)
                            Message("hit exception in AddIndexes during merge");
                        RollbackTransaction();
                    }
                    else
                    {
                        CommitTransaction();
                    }
                }

                if (mergePolicy is LogMergePolicy && GetUseCompoundFile())
                {

                    List<string> files = null;

                    lock (this)
                    {
                        // Must incRef our files so that if another thread
                        // is running merge/optimize, it doesn't delete our
                        // segment's files before we have a change to
                        // finish making the compound file.
                        if (segmentInfos.Contains(info))
                        {
                            files = info.Files();
                            deleter.IncRef(files);
                        }
                    }

                    if (files != null)
                    {

                        success = false;

                        StartTransaction(false);

                        try
                        {
                            merger.CreateCompoundFile(mergedName + ".cfs");
                            lock (this)
                            {
                                info.SetUseCompoundFile(true);
                            }

                            success = true;

                        }
                        finally
                        {

                            deleter.DecRef(files);

                            if (!success)
                            {
                                if (infoStream != null)
                                    Message("hit exception building compound file in AddIndexes during merge");

                                RollbackTransaction();
                            }
                            else
                            {
                                CommitTransaction();
                            }
                        }
                    }
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
            finally
            {
                docWriter.ResumeAllThreads();
            }
        }

        // This is called after pending added and deleted
        // documents have been flushed to the Directory but before
        // the change is committed (new segments_N file written).
        protected internal virtual void DoAfterFlush()
        {
        }

        /**
         * Flush all in-memory buffered updates (adds and deletes)
         * to the Directory. 
         * <p>Note: while this will force buffered docs to be
         * pushed into the index, it will not make these docs
         * visible to a reader.  Use {@link #Commit()} instead
         * @throws CorruptIndexException if the index is corrupt
         * @throws System.IO.IOException if there is a low-level IO error
         * @deprecated please call {@link #Commit()}) instead
         */
        public void Flush()
        {
            Flush(true, false, true);
        }

        /** <p>Expert: prepare for commit.  This does the first
         *  phase of 2-phase commit.  You can only call this when
         *  autoCommit is false.  This method does all steps
         *  necessary to commit changes since this writer was
         *  opened: flushes pending added and deleted docs, syncs
         *  the index files, writes most of next segments_N file.
         *  After calling this you must call either {@link
         *  #Commit()} to finish the commit, or {@link
         *  #rollback()} to revert the commit and undo all changes
         *  done since the writer was opened.</p>
         *
         * You can also just call {@link #Commit()} directly
         * without prepareCommit first in which case that method
         * will internally call prepareCommit.
         */
        public void PrepareCommit()
        {
            EnsureOpen();
            PrepareCommit(false);
        }

        private void PrepareCommit(bool internal_Renamed)
        {

            if (hitOOM)
                throw new System.Exception("this writer hit an System.OutOfMemoryException; cannot commit");

            if (autoCommit && !internal_Renamed)
                throw new System.Exception("this method can only be used when autoCommit is false");

            if (!autoCommit && pendingCommit != null)
                throw new System.Exception("prepareCommit was already called with no corresponding call to commit");

            Message("prepareCommit: flush");

            Flush(true, true, true);

            StartCommit(0);
        }

        private void Commit(long sizeInBytes)
        {
            StartCommit(sizeInBytes);
            FinishCommit();
        }

        private bool committing;

        private void WaitForCommit()
        {
            lock (this)
            {
                // Only allow a single thread to do the commit, at a time:
                while (committing)
                    DoWait();
                committing = true;
            }
        }

        private void DoneCommit()
        {
            lock (this)
            {
                committing = false;
                System.Threading.Monitor.PulseAll(this);
            }
        }

        /**
         * <p>Commits all pending updates (added & deleted
         * documents) to the index, and syncs all referenced index
         * files, such that a reader will see the changes and the
         * index updates will survive an OS or machine crash or
         * power loss.  Note that this does not wait for any
         * running background merges to finish.  This may be a
         * costly operation, so you should test the cost in your
         * application and do it only when really necessary.</p>
         *
         * <p> Note that this operation calls Directory.sync on
         * the index files.  That call should not return until the
         * file contents & metadata are on stable storage.  For
         * FSDirectory, this calls the OS's fsync.  But, beware:
         * some hardware devices may in fact cache writes even
         * during fsync, and return before the bits are actually
         * on stable storage, to give the appearance of faster
         * performance.  If you have such a device, and it does
         * not have a battery backup (for example) then on power
         * loss it may still lose data.  Lucene cannot guarantee
         * consistency on such devices.  </p>
         *
         * @see #prepareCommit
         */

        public void Commit()
        {

            EnsureOpen();

            // Only let one thread do the prepare/finish at a time
            WaitForCommit();

            try
            {
                Message("commit: start");

                if (autoCommit || pendingCommit == null)
                {
                    Message("commit: now prepare");
                    PrepareCommit(true);
                }
                else
                    Message("commit: already prepared");

                FinishCommit();
            }
            finally
            {
                DoneCommit();
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
                        Message("commit: pendingCommit != null");
                        pendingCommit.FinishCommit(directory);
                        lastCommitChangeCount = pendingCommitChangeCount;
                        segmentInfos.UpdateGeneration(pendingCommit);
                        SetRollbackSegmentInfos(pendingCommit);
                        deleter.Checkpoint(pendingCommit, true);
                    }
                    finally
                    {
                        deleter.DecRef(pendingCommit);
                        pendingCommit = null;
                        System.Threading.Monitor.PulseAll(this);
                    }

                }
                else
                    Message("commit: pendingCommit == null; skip");

                Message("commit: done");
            }
        }

        /**
         * Flush all in-memory buffered udpates (adds and deletes)
         * to the Directory.
         * @param triggerMerge if true, we may merge segments (if
         *  deletes or docs were flushed) if necessary
         * @param flushDocStores if false we are allowed to keep
         *  doc stores open to share with the next segment
         * @param flushDeletes whether pending deletes should also
         *  be flushed
         */
        public /* for nunit tests, was: protected */ void Flush(bool triggerMerge, bool flushDocStores, bool flushDeletes)
        {
            // We can be called during close, when closing==true, so we must pass false to EnsureOpen:
            EnsureOpen(false);
            if (DoFlush(flushDocStores, flushDeletes) && triggerMerge)
                MaybeMerge();
        }

        // TODO: this method should not have to be entirely
        // synchronized, ie, merges should be allowed to commit
        // even while a flush is happening
        private bool DoFlush(bool flushDocStores, bool flushDeletes)
        {
            lock (this)
            {

                EnsureOpen(false);

                System.Diagnostics.Debug.Assert(TestPoint("startDoFlush"));

                flushCount++;

                // Make sure no threads are actively adding a document

                flushDeletes |= docWriter.DeletesFull();

                // When autoCommit=true we must always flush deletes
                // when flushing a segment; otherwise deletes may become
                // visible before their corresponding added document
                // from an updateDocument call
                flushDeletes |= autoCommit;

                // Returns true if docWriter is currently aborting, in
                // which case we skip flushing this segment
                if (docWriter.PauseAllThreads())
                {
                    docWriter.ResumeAllThreads();
                    return false;
                }

                try
                {

                    SegmentInfo newSegment = null;

                    int numDocs = docWriter.GetNumDocsInRAM();

                    // Always flush docs if there are any
                    bool flushDocs = numDocs > 0;

                    // With autoCommit=true we always must flush the doc
                    // stores when we flush
                    flushDocStores |= autoCommit;
                    string docStoreSegment = docWriter.GetDocStoreSegment();
                    if (docStoreSegment == null)
                        flushDocStores = false;

                    int docStoreOffset = docWriter.GetDocStoreOffset();

                    // docStoreOffset should only be non-zero when
                    // autoCommit == false
                    System.Diagnostics.Debug.Assert(!autoCommit || 0 == docStoreOffset);

                    bool docStoreIsCompoundFile = false;

                    if (infoStream != null)
                    {
                        Message("  flush: segment=" + docWriter.GetSegment() +
                                " docStoreSegment=" + docWriter.GetDocStoreSegment() +
                                " docStoreOffset=" + docStoreOffset +
                                " flushDocs=" + flushDocs +
                                " flushDeletes=" + flushDeletes +
                                " flushDocStores=" + flushDocStores +
                                " numDocs=" + numDocs +
                                " numBufDelTerms=" + docWriter.GetNumBufferedDeleteTerms());
                        Message("  index before flush " + SegString());
                    }

                    // Check if the doc stores must be separately flushed
                    // because other segments, besides the one we are about
                    // to flush, reference it
                    if (flushDocStores && (!flushDocs || !docWriter.GetSegment().Equals(docWriter.GetDocStoreSegment())))
                    {
                        // We must separately flush the doc store
                        if (infoStream != null)
                            Message("  flush shared docStore segment " + docStoreSegment);

                        docStoreIsCompoundFile = FlushDocStores();
                        flushDocStores = false;
                    }

                    string segment = docWriter.GetSegment();

                    // If we are flushing docs, segment must not be null:
                    System.Diagnostics.Debug.Assert(segment != null || !flushDocs);

                    if (flushDocs)
                    {

                        bool success = false;
                        int flushedDocCount;

                        try
                        {
                            flushedDocCount = docWriter.Flush(flushDocStores);
                            success = true;
                        }
                        finally
                        {
                            if (!success)
                            {
                                if (infoStream != null)
                                    Message("hit exception flushing segment " + segment);
                                deleter.Refresh(segment);
                            }
                        }

                        if (0 == docStoreOffset && flushDocStores)
                        {
                            // This means we are flushing private doc stores
                            // with this segment, so it will not be shared
                            // with other segments
                            System.Diagnostics.Debug.Assert(docStoreSegment != null);
                            System.Diagnostics.Debug.Assert(docStoreSegment.Equals(segment));
                            docStoreOffset = -1;
                            docStoreIsCompoundFile = false;
                            docStoreSegment = null;
                        }

                        // Create new SegmentInfo, but do not add to our
                        // segmentInfos until deletes are flushed
                        // successfully.
                        newSegment = new SegmentInfo(segment,
                                                     flushedDocCount,
                                                     directory, false, true,
                                                     docStoreOffset, docStoreSegment,
                                                     docStoreIsCompoundFile,
                                                     docWriter.HasProx());
                    }

                    docWriter.PushDeletes();

                    if (flushDocs)
                        segmentInfos.Add(newSegment);

                    if (flushDeletes)
                    {
                        flushDeletesCount++;
                        ApplyDeletes();
                    }

                    DoAfterFlush();

                    if (flushDocs)
                        Checkpoint();

                    if (flushDocs && mergePolicy.UseCompoundFile(segmentInfos, newSegment))
                    {
                        // Now build compound file
                        bool success = false;
                        try
                        {
                            docWriter.CreateCompoundFile(segment);
                            success = true;
                        }
                        finally
                        {
                            if (!success)
                            {
                                if (infoStream != null)
                                    Message("hit exception creating compound file for newly flushed segment " + segment);
                                deleter.DeleteFile(segment + "." + IndexFileNames.COMPOUND_FILE_EXTENSION);
                            }
                        }

                        newSegment.SetUseCompoundFile(true);
                        Checkpoint();
                    }

                    return flushDocs;

                }
                catch (System.OutOfMemoryException oom)
                {
                    hitOOM = true;
                    throw oom;
                }
                finally
                {
                    docWriter.ClearFlushPending();
                    docWriter.ResumeAllThreads();
                }
            }
        }

        /** Expert:  Return the total size of all index files currently cached in memory.
         * Useful for size management with flushRamDocs()
         */
        public long RamSizeInBytes()
        {
            EnsureOpen();
            return docWriter.GetRAMUsed();
        }

        /** Expert:  Return the number of documents currently
         *  buffered in RAM. */
        public int NumRamDocs()
        {
            lock (this)
            {
                EnsureOpen();
                return docWriter.GetNumDocsInRAM();
            }
        }

        private int EnsureContiguousMerge(MergePolicy.OneMerge merge)
        {

            int first = segmentInfos.IndexOf(merge.segments.Info(0));
            if (first == -1)
                throw new MergePolicy.MergeException("could not find segment " + merge.segments.Info(0).name + " in current segments", directory);

            int numSegments = segmentInfos.Count;

            int numSegmentsToMerge = merge.segments.Count;
            for (int i = 0; i < numSegmentsToMerge; i++)
            {
                SegmentInfo info = merge.segments.Info(i);

                if (first + i >= numSegments || !segmentInfos.Info(first + i).Equals(info))
                {
                    if (segmentInfos.IndexOf(info) == -1)
                        throw new MergePolicy.MergeException("MergePolicy selected a segment (" + info.name + ") that is not in the index", directory);
                    else
                        throw new MergePolicy.MergeException("MergePolicy selected non-contiguous segments to merge (" + merge.SegString(directory) + " vs " + SegString() + "), which IndexWriter (currently) cannot handle",
                                                             directory);
                }
            }

            return first;
        }

        /** Carefully merges deletes for the segments we just
         *  merged.  This is tricky because, although merging will
         *  clear all deletes (compacts the documents), new
         *  deletes may have been flushed to the segments since
         *  the merge was started.  This method "carries over"
         *  such new deletes onto the newly merged segment, and
         *  saves the resulting deletes file (incrementing the
         *  delete generation for merge.info).  If no deletes were
         *  flushed, no new deletes file is saved. */
        private void CommitMergedDeletes(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(TestPoint("startCommitMergeDeletes"));

                SegmentInfos sourceSegmentsClone = merge.segmentsClone;
                SegmentInfos sourceSegments = merge.segments;

                if (infoStream != null)
                    Message("commitMergeDeletes " + merge.SegString(directory));

                // Carefully merge deletes that occurred after we
                // started merging:

                BitVector deletes = null;
                int docUpto = 0;
                int delCount = 0;

                int numSegmentsToMerge = sourceSegments.Count;
                for (int i = 0; i < numSegmentsToMerge; i++)
                {
                    SegmentInfo previousInfo = sourceSegmentsClone.Info(i);
                    SegmentInfo currentInfo = sourceSegments.Info(i);

                    System.Diagnostics.Debug.Assert(currentInfo.docCount == previousInfo.docCount);

                    int docCount = currentInfo.docCount;

                    if (previousInfo.HasDeletions())
                    {

                        // There were deletes on this segment when the merge
                        // started.  The merge has collapsed away those
                        // deletes, but, if new deletes were flushed since
                        // the merge started, we must now carefully keep any
                        // newly flushed deletes but mapping them to the new
                        // docIDs.

                        System.Diagnostics.Debug.Assert(currentInfo.HasDeletions());

                        // Load deletes present @ start of merge, for this segment:
                        BitVector previousDeletes = new BitVector(previousInfo.dir, previousInfo.GetDelFileName());

                        if (!currentInfo.GetDelFileName().Equals(previousInfo.GetDelFileName()))
                        {
                            // This means this segment has had new deletes
                            // committed since we started the merge, so we
                            // must merge them:
                            if (deletes == null)
                                deletes = new BitVector(merge.info.docCount);

                            BitVector currentDeletes = new BitVector(currentInfo.dir, currentInfo.GetDelFileName());
                            for (int j = 0; j < docCount; j++)
                            {
                                if (previousDeletes.Get(j))
                                    System.Diagnostics.Debug.Assert(currentDeletes.Get(j));
                                else
                                {
                                    if (currentDeletes.Get(j))
                                    {
                                        deletes.Set(docUpto);
                                        delCount++;
                                    }
                                    docUpto++;
                                }
                            }
                        }
                        else
                            docUpto += docCount - previousDeletes.Count();

                    }
                    else if (currentInfo.HasDeletions())
                    {
                        // This segment had no deletes before but now it
                        // does:
                        if (deletes == null)
                            deletes = new BitVector(merge.info.docCount);
                        BitVector currentDeletes = new BitVector(directory, currentInfo.GetDelFileName());

                        for (int j = 0; j < docCount; j++)
                        {
                            if (currentDeletes.Get(j))
                            {
                                deletes.Set(docUpto);
                                delCount++;
                            }
                            docUpto++;
                        }

                    }
                    else
                        // No deletes before or after
                        docUpto += currentInfo.docCount;
                }

                if (deletes != null)
                {
                    merge.info.AdvanceDelGen();
                    Message("commit merge deletes to " + merge.info.GetDelFileName());
                    deletes.Write(directory, merge.info.GetDelFileName());
                    merge.info.SetDelCount(delCount);
                    System.Diagnostics.Debug.Assert(delCount == deletes.Count());
                }
            }
        }

        /* FIXME if we want to support non-contiguous segment merges */
        private bool CommitMerge(MergePolicy.OneMerge merge, SegmentMerger merger, int mergedDocCount)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(TestPoint("startCommitMerge"));

                if (hitOOM)
                    return false;

                if (infoStream != null)
                    Message("commitMerge: " + merge.SegString(directory) + " index=" + SegString());

                System.Diagnostics.Debug.Assert(merge.registerDone);

                // If merge was explicitly aborted, or, if rollback() or
                // RollbackTransaction() had been called since our merge
                // started (which results in an unqualified
                // deleter.Refresh() call that will remove any index
                // file that current segments does not reference), we
                // abort this merge
                if (merge.IsAborted())
                {
                    if (infoStream != null)
                        Message("commitMerge: skipping merge " + merge.SegString(directory) + ": it was aborted");

                    deleter.Refresh(merge.info.name);
                    return false;
                }

                int start = EnsureContiguousMerge(merge);

                CommitMergedDeletes(merge);

                docWriter.RemapDeletes(segmentInfos, merger.GetDocMaps(), merger.GetDelCounts(), merge, mergedDocCount);

                // Simple optimization: if the doc store we are using
                // has been closed and is in now compound format (but
                // wasn't when we started), then we will switch to the
                // compound format as well:
                string mergeDocStoreSegment = merge.info.GetDocStoreSegment();
                if (mergeDocStoreSegment != null && !merge.info.GetDocStoreIsCompoundFile())
                {
                    int size = segmentInfos.Count;
                    for (int i = 0; i < size; i++)
                    {
                        SegmentInfo info = segmentInfos.Info(i);
                        string docStoreSegment = info.GetDocStoreSegment();
                        if (docStoreSegment != null &&
                            docStoreSegment.Equals(mergeDocStoreSegment) &&
                            info.GetDocStoreIsCompoundFile())
                        {
                            merge.info.SetDocStoreIsCompoundFile(true);
                            break;
                        }
                    }
                }

                merge.info.SetHasProx(merger.HasProx());

                //segmentInfos.RemoveRange(start, start + merge.segments.Count);
                segmentInfos.RemoveRange(start, merge.segments.Count);
                System.Diagnostics.Debug.Assert(!segmentInfos.Contains(merge.info));
                segmentInfos.Insert(start, merge.info);

                // Must checkpoint before decrefing so any newly
                // referenced files in the new merge.info are incref'd
                // first:
                Checkpoint();

                DecrefMergeSegments(merge);

                if (merge.optimize)
                    segmentsToOptimize[merge.info] = merge.info;
                return true;
            }
        }

        private void DecrefMergeSegments(MergePolicy.OneMerge merge)
        {
            SegmentInfos sourceSegmentsClone = merge.segmentsClone;
            int numSegmentsToMerge = sourceSegmentsClone.Count;
            System.Diagnostics.Debug.Assert(merge.increfDone);
            merge.increfDone = false;
            for (int i = 0; i < numSegmentsToMerge; i++)
            {
                SegmentInfo previousInfo = sourceSegmentsClone.Info(i);
                // Decref all files for this SegmentInfo (this
                // matches the incref in mergeInit):
                if (previousInfo.dir == directory)
                    deleter.DecRef(previousInfo.Files());
            }
        }

        private void HandleMergeException(System.Exception t, MergePolicy.OneMerge merge)
        {
            // Set the exception on the merge, so if
            // Optimize() is waiting on us it sees the root
            // cause exception:
            merge.SetException(t);
            AddMergeException(merge);

            if (t is MergePolicy.MergeAbortedException)
            {
                // We can ignore this exception (it happens when
                // close(false) or rollback is called), unless the
                // merge involves segments from external directories,
                // in which case we must throw it so, for example, the
                // rollbackTransaction code in AddIndexes* is
                // executed.
                if (merge.isExternal)
                    throw (MergePolicy.MergeAbortedException)t;
            }
            else if (t is System.IO.IOException)
                throw (System.IO.IOException)t;
            else if (t is System.SystemException)
                throw (System.SystemException)t;
            else if (t is System.Exception)
                throw (System.Exception)t;
            else
                // Should not get here
                throw new System.SystemException(null, t);
        }

        /**
         * Merges the indicated segments, replacing them in the stack with a
         * single segment.
         */

        public /* for nunit test, was: internal */ void Merge(MergePolicy.OneMerge merge)
        {

            bool success = false;

            try
            {
                try
                {
                    try
                    {
                        MergeInit(merge);

                        if (infoStream != null)
                            Message("now merge\n  merge=" + merge.SegString(directory) + "\n  merge=" + merge + "\n  index=" + SegString());

                        MergeMiddle(merge);
                        success = true;
                    }
                    catch (System.Exception t)
                    {
                        HandleMergeException(t, merge);
                    }
                }
                finally
                {
                    lock (this)
                    {
                        try
                        {

                            MergeFinish(merge);

                            if (!success)
                            {
                                if (infoStream != null)
                                    Message("hit exception during merge");
                                if (merge.info != null && !segmentInfos.Contains(merge.info))
                                    deleter.Refresh(merge.info.name);
                            }

                            // This merge (and, generally, any change to the
                            // segments) may now enable new merges, so we call
                            // merge policy & update pending merges.
                            if (success && !merge.IsAborted() && !closed && !closing)
                                UpdatePendingMerges(merge.maxNumSegmentsOptimize, merge.optimize);
                        }
                        finally
                        {
                            runningMerges.Remove(merge);
                        }
                    }
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
        }

        /** Checks whether this merge involves any segments
         *  already participating in a merge.  If not, this merge
         *  is "registered", meaning we record that its segments
         *  are now participating in a merge, and true is
         *  returned.  Else (the merge conflicts) false is
         *  returned. */
        internal bool RegisterMerge(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                if (merge.registerDone)
                    return true;

                if (stopMerges)
                {
                    merge.Abort();
                    throw new MergePolicy.MergeAbortedException("merge is aborted: " + merge.SegString(directory));
                }

                int count = merge.segments.Count;
                bool isExternal = false;
                for (int i = 0; i < count; i++)
                {
                    SegmentInfo info = merge.segments.Info(i);
                    if (mergingSegments.ContainsKey(info))
                        return false;
                    if (segmentInfos.IndexOf(info) == -1)
                        return false;
                    if (info.dir != directory)
                        isExternal = true;
                }

                EnsureContiguousMerge(merge);

                pendingMerges.Add(merge);

                if (infoStream != null)
                    Message("add merge to pendingMerges: " + merge.SegString(directory) + " [total " + pendingMerges.Count + " pending]");

                merge.mergeGen = mergeGen;
                merge.isExternal = isExternal;

                // OK it does not conflict; now record that this merge
                // is running (while synchronized) to avoid race
                // condition where two conflicting merges from different
                // threads, start
                for (int i = 0; i < count; i++)
                    mergingSegments[merge.segments.Info(i)] = merge.segments.Info(i);

                // Merge is now registered
                merge.registerDone = true;
                return true;
            }
        }

        /** Does initial setup for a merge, which is fast but holds
         *  the synchronized lock on IndexWriter instance.  */
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
                        MergeFinish(merge);
                        runningMerges.Remove(merge);
                    }
                }
            }
        }

        private void _MergeInit(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(TestPoint("startMergeInit"));

                System.Diagnostics.Debug.Assert(merge.registerDone);
                System.Diagnostics.Debug.Assert(!merge.optimize || merge.maxNumSegmentsOptimize > 0);

                if (merge.info != null)
                    // mergeInit already done
                    return;

                if (merge.IsAborted())
                    return;

                bool changed = ApplyDeletes();

                // If autoCommit == true then all deletes should have
                // been flushed when we flushed the last segment
                System.Diagnostics.Debug.Assert(!changed || !autoCommit);

                SegmentInfos sourceSegments = merge.segments;
                int end = sourceSegments.Count;

                // Check whether this merge will allow us to skip
                // merging the doc stores (stored field & vectors).
                // This is a very substantial optimization (saves tons
                // of IO) that can only be applied with
                // autoCommit=false.

                Directory lastDir = directory;
                string lastDocStoreSegment = null;
                int next = -1;

                bool mergeDocStores = false;
                bool doFlushDocStore = false;
                string currentDocStoreSegment = docWriter.GetDocStoreSegment();

                // Test each segment to be merged: check if we need to
                // flush/merge doc stores
                for (int i = 0; i < end; i++)
                {
                    SegmentInfo si = sourceSegments.Info(i);

                    // If it has deletions we must merge the doc stores
                    if (si.HasDeletions())
                        mergeDocStores = true;

                    // If it has its own (private) doc stores we must
                    // merge the doc stores
                    if (-1 == si.GetDocStoreOffset())
                        mergeDocStores = true;

                    // If it has a different doc store segment than
                    // previous segments, we must merge the doc stores
                    string docStoreSegment_Renamed = si.GetDocStoreSegment();
                    if (docStoreSegment_Renamed == null)
                        mergeDocStores = true;
                    else if (lastDocStoreSegment == null)
                        lastDocStoreSegment = docStoreSegment_Renamed;
                    else if (!lastDocStoreSegment.Equals(docStoreSegment_Renamed))
                        mergeDocStores = true;

                    // Segments' docScoreOffsets must be in-order,
                    // contiguous.  For the default merge policy now
                    // this will always be the case but for an arbitrary
                    // merge policy this may not be the case
                    if (-1 == next)
                        next = si.GetDocStoreOffset() + si.docCount;
                    else if (next != si.GetDocStoreOffset())
                        mergeDocStores = true;
                    else
                        next = si.GetDocStoreOffset() + si.docCount;

                    // If the segment comes from a different directory
                    // we must merge
                    if (lastDir != si.dir)
                        mergeDocStores = true;

                    // If the segment is referencing the current "live"
                    // doc store outputs then we must merge
                    if (si.GetDocStoreOffset() != -1 && currentDocStoreSegment != null && si.GetDocStoreSegment().Equals(currentDocStoreSegment))
                    {
                        doFlushDocStore = true;
                    }
                }

                int docStoreOffset;
                string docStoreSegment;
                bool docStoreIsCompoundFile;

                if (mergeDocStores)
                {
                    docStoreOffset = -1;
                    docStoreSegment = null;
                    docStoreIsCompoundFile = false;
                }
                else
                {
                    SegmentInfo si = sourceSegments.Info(0);
                    docStoreOffset = si.GetDocStoreOffset();
                    docStoreSegment = si.GetDocStoreSegment();
                    docStoreIsCompoundFile = si.GetDocStoreIsCompoundFile();
                }

                if (mergeDocStores && doFlushDocStore)
                {
                    // SegmentMerger intends to merge the doc stores
                    // (stored fields, vectors), and at least one of the
                    // segments to be merged refers to the currently
                    // live doc stores.

                    // TODO: if we know we are about to merge away these
                    // newly flushed doc store files then we should not
                    // make compound file out of them...
                    if (infoStream != null)
                        Message("now flush at merge");
                    DoFlush(true, false);
                    //Flush(false, true, false);
                }

                // We must take a full copy at this point so that we can
                // properly merge deletes in CommitMerge()
                merge.segmentsClone = (SegmentInfos)merge.segments.Clone();

                for (int i = 0; i < end; i++)
                {
                    SegmentInfo si = merge.segmentsClone.Info(i);

                    // IncRef all files for this segment info to make sure
                    // they are not removed while we are trying to merge.
                    if (si.dir == directory)
                        deleter.IncRef(si.Files());
                }

                merge.increfDone = true;

                merge.mergeDocStores = mergeDocStores;

                // Bind a new segment name here so even with
                // ConcurrentMergePolicy we keep deterministic segment
                // names.
                merge.info = new SegmentInfo(NewSegmentName(), 0,
                                             directory, false, true,
                                             docStoreOffset,
                                             docStoreSegment,
                                             docStoreIsCompoundFile,
                                             false);

                // Also enroll the merged segment into mergingSegments;
                // this prevents it from getting selected for a merge
                // after our merge is done but while we are building the
                // CFS:
                mergingSegments[merge.info] = merge.info;
            }
        }

        /** This is called after merging a segment and before
         *  building its CFS.  Return true if the files should be
         *  sync'd.  If you return false, then the source segment
         *  files that were merged cannot be deleted until the CFS
         *  file is built & sync'd.  So, returning false consumes
         *  more transient disk space, but saves performance of
         *  not having to sync files which will shortly be deleted
         *  anyway.
         * @deprecated -- this will be removed in 3.0 when
         * autoCommit is hardwired to false */
        [System.Obsolete("this will be removed in 3.0 when autoCommit is hardwired to false")]
        private bool DoCommitBeforeMergeCFS(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                long freeableBytes = 0;
                int size = merge.segments.Count;
                for (int i = 0; i < size; i++)
                {
                    SegmentInfo info = merge.segments.Info(i);
                    // It's only important to sync if the most recent
                    // commit actually references this segment, because if
                    // it doesn't, even without syncing we will free up
                    // the disk space:
                    if (rollbackSegments.ContainsKey(info))
                    {
                        int loc = rollbackSegments[info];
                        SegmentInfo oldInfo = rollbackSegmentInfos.Info(loc);
                        if (oldInfo.GetUseCompoundFile() != info.GetUseCompoundFile())
                            freeableBytes += info.SizeInBytes();
                    }
                }
                // If we would free up more than 1/3rd of the index by
                // committing now, then do so:
                long totalBytes = 0;
                int numSegments = segmentInfos.Count;
                for (int i = 0; i < numSegments; i++)
                    totalBytes += segmentInfos.Info(i).SizeInBytes();
                if (3 * freeableBytes > totalBytes)
                    return true;
                else
                    return false;
            }
        }

        /** Does fininishing for a merge, which is fast but holds
         *  the synchronized lock on IndexWriter instance. */
        internal void MergeFinish(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                // Optimize, AddIndexes or finishMerges may be waiting
                // on merges to finish.
                System.Threading.Monitor.PulseAll(this);

                if (merge.increfDone)
                    DecrefMergeSegments(merge);

                System.Diagnostics.Debug.Assert(merge.registerDone);

                SegmentInfos sourceSegments = merge.segments;
                int end = sourceSegments.Count;
                for (int i = 0; i < end; i++)
                    mergingSegments.Remove(sourceSegments.Info(i));
                if (merge.info != null) mergingSegments.Remove(merge.info);
                merge.registerDone = false;
            }
        }

        /** Does the actual (time-consuming) work of the merge,
         *  but without holding synchronized lock on IndexWriter
         *  instance */
        private int MergeMiddle(MergePolicy.OneMerge merge)
        {

            merge.CheckAborted(directory);

            string mergedName = merge.info.name;

            SegmentMerger merger = null;

            int mergedDocCount = 0;

            SegmentInfos sourceSegments = merge.segments;
            SegmentInfos sourceSegmentsClone = merge.segmentsClone;
            int numSegments = sourceSegments.Count;

            if (infoStream != null)
                Message("merging " + merge.SegString(directory));

            merger = new SegmentMerger(this, mergedName, merge);

            bool success = false;

            // This is try/finally to make sure merger's readers are
            // closed:
            try
            {
                int totDocCount = 0;

                for (int i = 0; i < numSegments; i++)
                {
                    SegmentInfo si = sourceSegmentsClone.Info(i);
                    IndexReader reader = SegmentReader.Get(true, si, MERGE_READ_BUFFER_SIZE, merge.mergeDocStores); // no need to set deleter (yet)
                    merger.Add(reader);
                    totDocCount += reader.NumDocs();
                }
                if (infoStream != null)
                {
                    Message("merge: total " + totDocCount + " docs");
                }

                merge.CheckAborted(directory);

                // This is where all the work happens:
                mergedDocCount = merge.info.docCount = merger.Merge(merge.mergeDocStores);

                System.Diagnostics.Debug.Assert(mergedDocCount == totDocCount);

                success = true;

            }
            finally
            {
                // close readers before we attempt to delete
                // now-obsolete segments
                if (merger != null)
                {
                    merger.CloseReaders();
                }
            }

            if (!CommitMerge(merge, merger, mergedDocCount))
                // commitMerge will return false if this merge was aborted
                return 0;

            if (merge.useCompoundFile)
            {

                // Maybe force a sync here to allow reclaiming of the
                // disk space used by the segments we just merged:
                if (autoCommit && DoCommitBeforeMergeCFS(merge))
                {
                    long size;
                    lock (this)
                    {
                        size = merge.info.SizeInBytes();
                    }
                    Commit(size);
                }

                success = false;
                string compoundFileName = mergedName + "." + IndexFileNames.COMPOUND_FILE_EXTENSION;

                try
                {
                    merger.CreateCompoundFile(compoundFileName);
                    success = true;
                }
                catch (System.IO.IOException ioe)
                {
                    lock (this)
                    {
                        if (merge.IsAborted())
                        {
                            // This can happen if rollback or close(false)
                            // is called -- fall through to logic below to
                            // remove the partially created CFS:
                            success = true;
                        }
                        else
                            HandleMergeException(ioe, merge);
                    }
                }
                catch (System.Exception t)
                {
                    HandleMergeException(t, merge);
                }
                finally
                {
                    if (!success)
                    {
                        if (infoStream != null)
                            Message("hit exception creating compound file during merge");
                        lock (this)
                        {
                            deleter.DeleteFile(compoundFileName);
                        }
                    }
                }

                if (merge.IsAborted())
                {
                    if (infoStream != null)
                        Message("abort merge after building CFS");
                    deleter.DeleteFile(compoundFileName);
                    return 0;
                }

                lock (this)
                {
                    if (segmentInfos.IndexOf(merge.info) == -1 || merge.IsAborted())
                    {
                        // Our segment (committed in non-compound
                        // format) got merged away while we were
                        // building the compound format.
                        deleter.DeleteFile(compoundFileName);
                    }
                    else
                    {
                        merge.info.SetUseCompoundFile(true);
                        Checkpoint();
                    }
                }
            }

            // Force a sync after commiting the merge.  Once this
            // sync completes then all index files referenced by the
            // current segmentInfos are on stable storage so if the
            // OS/machine crashes, or power cord is yanked, the
            // index will be intact.  Note that this is just one
            // (somewhat arbitrary) policy; we could try other
            // policies like only sync if it's been > X minutes or
            // more than Y bytes have been written, etc.
            if (autoCommit)
            {
                long size;
                lock (this)
                {
                    size = merge.info.SizeInBytes();
                }
                Commit(size);
            }

            return mergedDocCount;
        }

        internal void AddMergeException(MergePolicy.OneMerge merge)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(merge.GetException() != null);
                if (!mergeExceptions.Contains(merge) && mergeGen == merge.mergeGen)
                    mergeExceptions.Add(merge);
            }
        }

        // Apply buffered deletes to all segments.
        private bool ApplyDeletes()
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(TestPoint("startApplyDeletes"));
                SegmentInfos rollback = (SegmentInfos)segmentInfos.Clone();
                bool success = false;
                bool changed;
                try
                {
                    changed = docWriter.ApplyDeletes(segmentInfos);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        if (infoStream != null)
                            Message("hit exception flushing deletes");

                        // Carefully remove any partially written .del
                        // files
                        int size = rollback.Count;
                        for (int i = 0; i < size; i++)
                        {
                            string newDelFileName = segmentInfos.Info(i).GetDelFileName();
                            string delFileName = rollback.Info(i).GetDelFileName();
                            if (newDelFileName != null && !newDelFileName.Equals(delFileName))
                                deleter.DeleteFile(newDelFileName);
                        }

                        // Fully replace the segmentInfos since flushed
                        // deletes could have changed any of the
                        // SegmentInfo instances:
                        segmentInfos.Clear();
                        for (int i = 0; i < rollback.Count; i++) 
                            segmentInfos.Add(rollback.Info(i));
                    }
                }

                if (changed)
                    Checkpoint();
                return changed;
            }
        }

        // For test purposes.
        public int GetBufferedDeleteTermsSize()
        {
            lock (this) { return docWriter.GetBufferedDeleteTerms().Count; }
        }

        // For test purposes.
        public int GetNumBufferedDeleteTerms()
        {
            lock (this) { return docWriter.GetNumBufferedDeleteTerms(); }
        }

        // utility routines for tests
        public SegmentInfo NewestSegment()
        {
            return segmentInfos.Info(segmentInfos.Count - 1);
        }

        public string SegString()
        {
            lock (this) { return SegString(segmentInfos); }
        }

        private string SegString(SegmentInfos infos)
        {
            lock (this)
            {
                System.Text.StringBuilder buffer = new System.Text.StringBuilder();
                int count = infos.Count;
                for (int i = 0; i < count; i++)
                {
                    if (i > 0)
                    {
                        buffer.Append(' ');
                    }
                    SegmentInfo info = infos.Info(i);
                    buffer.Append(info.SegString(directory));
                    if (info.dir != directory)
                        buffer.Append("**");
                }
                return buffer.ToString();
            }
        }

        // Files that have been sync'd already
        private Dictionary<string, string> synced = new Dictionary<string, string>();

        // Files that are now being sync'd
        private Dictionary<string, string> syncing = new Dictionary<string, string>();

        private bool StartSync(string fileName, ICollection<string> pending)
        {
            lock (synced)
            {
                if (!synced.ContainsKey(fileName))
                {
                    if (!syncing.ContainsKey(fileName))
                    {
                        syncing[fileName] = fileName;
                        return true;
                    }
                    else
                    {
                        pending.Add(fileName);
                        return false;
                    }
                }
                else
                    return false;
            }
        }

        private void FinishSync(string fileName, bool success)
        {
            lock (synced)
            {
                System.Diagnostics.Debug.Assert(syncing.ContainsKey(fileName));
                syncing.Remove(fileName);
                if (success)
                    synced[fileName] = fileName;
                System.Threading.Monitor.PulseAll(synced);
            }
        }

        /** Blocks until all files in syncing are sync'd */
        private bool WaitForAllSynced(ICollection<string> syncing)
        {
            lock (synced)
            {
                IEnumerator<string> it = syncing.GetEnumerator();
                while (it.MoveNext())
                {
                    string fileName = it.Current;
                    while (!synced.ContainsKey(fileName))
                    {
                        if (!syncing.Contains(fileName))
                            // There was an error because a file that was
                            // previously syncing failed to appear in synced
                            return false;
                        else
                            try
                            {
                                System.Threading.Monitor.Wait(synced);
                            }
                            catch (System.Threading.ThreadInterruptedException)
                            {
                                continue;
                            }
                    }
                }
                return true;
            }
        }

        /** Pauses before syncing.  On Windows, at least, it's
         *  best (performance-wise) to pause in order to let OS
         *  flush writes to disk on its own, before forcing a
         *  sync.
         */
        [System.Obsolete("this will be removed in 3.0 when autoCommit is hardwired to false")]
        private void SyncPause(long sizeInBytes)
        {
            if (mergeScheduler is ConcurrentMergeScheduler && maxSyncPauseSeconds > 0)
            {
                // Rough heuristic: for every 10 MB, we pause for 1
                // second, up until the max
                long pauseTime = (long)(1000 * sizeInBytes / 10 / 1024 / 1024);
                long maxPauseTime = (long)(maxSyncPauseSeconds * 1000);
                if (pauseTime > maxPauseTime)
                    pauseTime = maxPauseTime;
                int sleepCount = (int)(pauseTime / 100);
                for (int i = 0; i < sleepCount; i++)
                {
                    lock (this)
                    {
                        if (stopMerges || closing)
                            break;
                    }
                    try
                    {


                        System.Threading.Thread.Sleep(100);
                    }
                    catch (System.Threading.ThreadInterruptedException)
                    {
                        System.Threading.Thread.CurrentThread.Interrupt();
                    }
                }
            }
        }

        private void DoWait()
        {
            lock (this)
            {
                try
                {
                    // NOTE: the callers of this method should in theory
                    // be able to do simply wait(), but, as a defense
                    // against thread timing hazards where notifyAll()
                    // falls to be called, we wait for at most 1 second
                    // and then return so caller can check if wait
                    // conditions are satisified:
                    System.Threading.Monitor.Wait(this, 1000);
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                    System.Threading.Thread.CurrentThread.Interrupt();
                }
            }
        }

        /** Walk through all files referenced by the current
         *  segmentInfos and ask the Directory to sync each file,
         *  if it wasn't already.  If that succeeds, then we
         *  prepare a new segments_N file but do not fully commit
         *  it. */
        private void StartCommit(long sizeInBytes)
        {

            System.Diagnostics.Debug.Assert(TestPoint("startStartCommit"));

            if (hitOOM)
                return;

            try
            {

                if (infoStream != null)
                    Message("StartCommit(): start sizeInBytes=" + sizeInBytes);

                if (sizeInBytes > 0)
                    SyncPause(sizeInBytes);

                SegmentInfos toSync = null;
                long myChangeCount;

                lock (this)
                {

                    // sizeInBytes > 0 means this is an autoCommit at
                    // the end of a merge.  If at this point stopMerges
                    // is true (which means a rollback() or
                    // RollbackTransaction() is waiting for us to
                    // finish), we skip the commit to avoid deadlock
                    if (sizeInBytes > 0 && stopMerges)
                        return;

                    // Wait for any running AddIndexes to complete
                    // first, then block any from running until we've
                    // copied the segmentInfos we intend to sync:
                    BlockAddIndexes(false);

                    System.Diagnostics.Debug.Assert(!HasExternalSegments());

                    try
                    {

                        System.Diagnostics.Debug.Assert(lastCommitChangeCount <= changeCount);

                        if (changeCount == lastCommitChangeCount)
                        {
                            if (infoStream != null)
                                Message("  skip StartCommit(): no changes pending");
                            return;
                        }

                        // First, we clone & incref the segmentInfos we intend
                        // to sync, then, without locking, we sync() each file
                        // referenced by toSync, in the background.  Multiple
                        // threads can be doing this at once, if say a large
                        // merge and a small merge finish at the same time:

                        if (infoStream != null)
                            Message("startCommit index=" + SegString(segmentInfos) + " changeCount=" + changeCount);

                        toSync = (SegmentInfos)segmentInfos.Clone();
                        deleter.IncRef(toSync, false);
                        myChangeCount = changeCount;
                    }
                    finally
                    {
                        ResumeAddIndexes();
                    }
                }

                System.Diagnostics.Debug.Assert(TestPoint("midStartCommit"));

                bool setPending = false;

                try
                {

                    // Loop until all files toSync references are sync'd:
                    while (true)
                    {

                        ICollection<string> pending = new List<string>();

                        for (int i = 0; i < toSync.Count; i++)
                        {
                            SegmentInfo info = toSync.Info(i);
                            List<string> files = info.Files();
                            for (int j = 0; j < files.Count; j++)
                            {
                                string fileName = files[j];
                                if (StartSync(fileName, pending))
                                {
                                    bool success = false;
                                    try
                                    {
                                        // Because we incRef'd this commit point, above,
                                        // the file had better exist:
                                        System.Diagnostics.Debug.Assert(directory.FileExists(fileName), "file '" + fileName + "' does not exist dir=" + directory);
                                        Message("now sync " + fileName);
                                        directory.Sync(fileName);
                                        success = true;
                                    }
                                    finally
                                    {
                                        FinishSync(fileName, success);
                                    }
                                }
                            }
                        }

                        // All files that I require are either synced or being
                        // synced by other threads.  If they are being synced,
                        // we must at this point block until they are done.
                        // If this returns false, that means an error in
                        // another thread resulted in failing to actually
                        // sync one of our files, so we repeat:
                        if (WaitForAllSynced(pending))
                            break;
                    }

                    System.Diagnostics.Debug.Assert(TestPoint("midStartCommit2"));

                    lock (this)
                    {
                        // If someone saved a newer version of segments file
                        // since I first started syncing my version, I can
                        // safely skip saving myself since I've been
                        // superseded:
                        if (myChangeCount > lastCommitChangeCount && (pendingCommit == null || myChangeCount > pendingCommitChangeCount))
                        {

                            // Wait now for any current pending commit to complete:
                            while (pendingCommit != null)
                            {
                                Message("wait for existing pendingCommit to finish...");
                                DoWait();
                            }

                            if (segmentInfos.GetGeneration() > toSync.GetGeneration())
                                toSync.UpdateGeneration(segmentInfos);

                            bool success = false;
                            try
                            {

                                // Exception here means nothing is prepared
                                // (this method unwinds everything it did on
                                // an exception)
                                try
                                {
                                    toSync.PrepareCommit(directory);
                                }
                                finally
                                {
                                    // Have our master segmentInfos record the
                                    // generations we just prepared.  We do this
                                    // on error or success so we don't
                                    // double-write a segments_N file.
                                    segmentInfos.UpdateGeneration(toSync);
                                }

                                System.Diagnostics.Debug.Assert(pendingCommit == null);
                                setPending = true;
                                pendingCommit = toSync;
                                // {{dougsale-2.4.0}}:
                                // see pendingCommitChangeCount declaration
                                pendingCommitChangeCount = (uint) myChangeCount;
                                success = true;
                            }
                            finally
                            {
                                if (!success)
                                    Message("hit exception committing segments file");
                            }
                        }
                        else
                            Message("sync superseded by newer infos");
                    }

                    Message("done all syncs");

                    System.Diagnostics.Debug.Assert(TestPoint("midStartCommitSuccess"));

                }
                finally
                {
                    lock (this)
                    {
                        if (!setPending)
                            deleter.DecRef(toSync);
                    }
                }
            }
            catch (System.OutOfMemoryException oom)
            {
                hitOOM = true;
                throw oom;
            }
            System.Diagnostics.Debug.Assert(TestPoint("finishStartCommit"));
        }

        /**
         * Returns <code>true</code> iff the index in the named directory is
         * currently locked.
         * @param directory the directory to check for a lock
         * @throws System.IO.IOException if there is a low-level IO error
         */
        public static bool IsLocked(Directory directory)
        {
            return directory.MakeLock(WRITE_LOCK_NAME).IsLocked();
        }

        /**
         * Returns <code>true</code> iff the index in the named directory is
         * currently locked.
         * @param directory the directory to check for a lock
         * @throws System.IO.IOException if there is a low-level IO error
         */
        public static bool IsLocked(string directory)
        {
            Directory dir = FSDirectory.GetDirectory(directory);
            try
            {
                return IsLocked(dir);
            }
            finally
            {
                dir.Close();
            }
        }

        /**
         * Forcibly unlocks the index in the named directory.
         * <P>
         * Caution: this should only be used by failure recovery code,
         * when it is known that no other process nor thread is in fact
         * currently accessing this index.
         */
        public static void Unlock(Directory directory)
        {
            directory.MakeLock(IndexWriter.WRITE_LOCK_NAME).Release();
        }

        /**
         * Specifies maximum field length in {@link IndexWriter} constructors.
         * {@link #setMaxFieldLength(int)} overrides the value set by
         * the constructor.
         */
        public sealed class MaxFieldLength
        {

            private int limit;
            private string name;

            /**
             * Private type-safe-enum-pattern constructor.
             * 
             * @param name instance name
             * @param limit maximum field length
             */
            private MaxFieldLength(string name, int limit)
            {
                this.name = name;
                this.limit = limit;
            }

            /**
             * Public constructor to allow users to specify the maximum field size limit.
             * 
             * @param limit The maximum field length
             */
            public MaxFieldLength(int limit)
                : this("User-specified", limit)
            {
            }

            public int GetLimit()
            {
                return limit;
            }

            public override string ToString()
            {
                return name + ":" + limit;
            }

            /** Sets the maximum field length to {@link Integer#MAX_VALUE}. */
            public static readonly MaxFieldLength UNLIMITED = new MaxFieldLength("UNLIMITED", int.MaxValue);

            /**
             *  Sets the maximum field length to 
             * {@link #DEFAULT_MAX_FIELD_LENGTH} 
             * */
            public static readonly MaxFieldLength LIMITED = new MaxFieldLength("LIMITED", DEFAULT_MAX_FIELD_LENGTH);
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
        //   startApplyDeletes
        //   DocumentsWriter.ThreadState.init start
        protected internal virtual bool TestPoint(string name)
        {
            return true;
        }
    }
}
   /// <summary>Merges all segments from an array of indexes into this index.
    //    /// 
    //    /// <p>This may be used to parallelize batch indexing.  A large document
    //    /// collection can be broken into sub-collections.  Each sub-collection can be
    //    /// indexed in parallel, on a different thread, process or machine.  The
    //    /// complete index can then be created by merging sub-collection indexes
    //    /// with this method.
    //    /// 
    //    /// <p><b>NOTE:</b> the index in each Directory must not be
    //    /// changed (opened by a writer) while this method is
    //    /// running.  This method does not acquire a write lock in
    //    /// each input Directory, so it is up to the caller to
    //    /// enforce this.
    //    /// 
    //    /// <p><b>NOTE:</b> while this is running, any attempts to
    //    /// add or delete documents (with another thread) will be
    //    /// paused until this method completes.
    //    /// 
    //    /// <p>After this completes, the index is optimized.
    //    /// 
    //    /// <p>This method is transactional in how Exceptions are
    //    /// handled: it does not commit a new segments_N file until
    //    /// all indexes are added.  This means if an Exception
    //    /// occurs (for example disk full), then either no indexes
    //    /// will have been added or they all will have been.</p>
    //    /// 
    //    /// <p>If an Exception is hit, it's still possible that all
    //    /// indexes were successfully added.  This happens when the
    //    /// Exception is hit when trying to build a CFS file.  In
    //    /// this case, one segment in the index will be in non-CFS
    //    /// format, even when using compound file format.</p>
    //    /// 
    //    /// <p>Also note that on an Exception, the index may still
    //    /// have been partially or fully optimized even though none
    //    /// of the input indexes were added. </p>
    //    /// 
    //    /// <p>Note that this requires temporary free space in the
    //    /// Directory up to 2X the sum of all input indexes
    //    /// (including the starting index).  If readers/searchers
    //    /// are open against the starting index, then temporary
    //    /// free space required will be higher by the size of the
    //    /// starting index (see {@link #Optimize()} for details).
    //    /// </p>
    //    /// 
    //    /// <p>Once this completes, the final size of the index
    //    /// will be less than the sum of all input index sizes
    //    /// (including the starting index).  It could be quite a
    //    /// bit smaller (if there were many pending deletes) or
    //    /// just slightly smaller.</p>
    //    /// 
    //    /// <p>See <a target="_top"
    //    /// href="http://issues.apache.org/jira/browse/LUCENE-702">LUCENE-702</a>
    //    /// for details.</p>
    //    /// </summary>
    //    /// <throws>  CorruptIndexException if the index is corrupt </throws>
    //    /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
    //    public virtual void  AddIndexes(Directory[] dirs)
    //    {

    //        EnsureOpen();
        
    //        // Do not allow add docs or deletes while we are running:
    //        docWriter.PauseAllThreads();

    //        try
    //        {
    //            if (infoStream != null)
    //                Message("flush at AddIndexes");
    //            Flush();

    //            bool success = false;

    //            StartTransaction();

    //            try
    //            {
    //                lock (this)
    //                {
    //                    for (int i = 0; i < dirs.Length; i++)
    //                    {
    //                        SegmentInfos sis = new SegmentInfos(); // read infos from dir
    //                        sis.Read(dirs[i]);
    //                        for (int j = 0; j < sis.Count; j++)
    //                        {
    //                            SegmentInfo info = sis.Info(j);
    //                            segmentInfos.Add(sis.Info(j)); // add each info
    //                        }
    //                    }
    //                }

    //                Optimize();

    //                success = true;
    //            }
    //            finally
    //            {
    //                if (success)
    //                {
    //                    CommitTransaction();
    //                }
    //                else
    //                {
    //                    RollbackTransaction();
    //                }
    //            }
    //        }
    //        catch (System.OutOfMemoryException oom)
    //        {
    //            hitOOM = true;
    //            throw oom;
    //        }
    //        finally
    //        {
    //            docWriter.ResumeAllThreads();
    //        }
    //    }
		
    //    private void  ResetMergeExceptions()
    //    {
    //        lock (this)
    //        {
    //            mergeExceptions = new System.Collections.ArrayList();
    //            mergeGen++;
    //        }
    //    }
		
    //    /// <summary> Merges all segments from an array of indexes into this index.
    //    /// <p>
    //    /// This is similar to AddIndexes(Directory[]). However, no Optimize()
    //    /// is called either at the beginning or at the end. Instead, merges
    //    /// are carried out as necessary.
    //    /// 
    //    /// <p><b>NOTE:</b> the index in each Directory must not be
    //    /// changed (opened by a writer) while this method is
    //    /// running.  This method does not acquire a write lock in
    //    /// each input Directory, so it is up to the caller to
    //    /// enforce this.
    //    /// 
    //    /// <p><b>NOTE:</b> while this is running, any attempts to
    //    /// add or delete documents (with another thread) will be
    //    /// paused until this method completes.
    //    /// 
    //    /// <p>
    //    /// This requires this index not be among those to be added, and the
    //    /// upper bound* of those segment doc counts not exceed maxMergeDocs.
    //    /// 
    //    /// <p>See {@link #AddIndexes(Directory[])} for
    //    /// details on transactional semantics, temporary free
    //    /// space required in the Directory, and non-CFS segments
    //    /// on an Exception.</p>
    //    /// </summary>
    //    /// <throws>  CorruptIndexException if the index is corrupt </throws>
    //    /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
    //    public virtual void  AddIndexesNoOptimize(Directory[] dirs)
    //    {

    //        EnsureOpen();

    //        // Do not allow add socs or deletes while we are running:
    //        docWriter.PauseAllThreads();

    //        try
    //        {
    //            if (infoStream != null)
    //                Message("flush at addIndexesNoOptimize");
    //            Flush();

    //            bool success = false;

    //            StartTransaction();

    //            try
    //            {

    //                lock (this)
    //                {
    //                    for (int i = 0; i < dirs.Length; i++)
    //                    {
    //                        if (directory == dirs[i])
    //                        {
    //                            // cannot add this index: segments may be deleted in merge before added
    //                            throw new System.ArgumentException("Cannot add this index to itself");
    //                        }

    //                        SegmentInfos sis = new SegmentInfos(); // read infos from dir
    //                        sis.Read(dirs[i]);
    //                        for (int j = 0; j < sis.Count; j++)
    //                        {
    //                            SegmentInfo info = sis.Info(j);
    //                            segmentInfos.Add(info); // add each info
    //                        }
    //                    }
    //                }

    //                MaybeMerge();

    //                // If after merging there remain segments in the index
    //                // that are in a different directory, just copy these
    //                // over into our index.  This is necessary (before
    //                // finishing the transaction) to avoid leaving the
    //                // index in an unusable (inconsistent) state.
    //                CopyExternalSegments();

    //                success = true;
    //            }
    //            finally
    //            {
    //                if (success)
    //                {
    //                    CommitTransaction();
    //                }
    //                else
    //                {
    //                    RollbackTransaction();
    //                }
    //            }
    //        }
    //        catch (System.OutOfMemoryException oom)
    //        {
    //            hitOOM = true;
    //            throw oom;
    //        }
    //        finally
    //        {
    //            docWriter.ResumeAllThreads();
    //        }
    //    }
		
    //    /* If any of our segments are using a directory != ours
    //    * then copy them over.  Currently this is only used by
    //    * AddIndexesNoOptimize(). */
    //    private void  CopyExternalSegments()
    //    {
    //        bool any = false;

    //        while (true)
    //        {
    //            SegmentInfo info = null;
    //            MergePolicy.OneMerge merge = null;

    //            lock (this)
    //            {
    //                int numSegments = segmentInfos.Count;
    //                for (int i = 0; i < numSegments; i++)
    //                {
    //                    info = segmentInfos.Info(i);
    //                    if (info.dir != directory)
    //                    {
    //                        merge = new MergePolicy.OneMerge(segmentInfos.Range(i, 1 + i), info.GetUseCompoundFile());
    //                        break;
    //                    }
    //                }
    //            }
    //            if (merge != null)
    //            {
    //                if (RegisterMerge(merge))
    //                {
    //                    pendingMerges.Remove(merge);
    //                    runningMerges.Add(merge, merge);
    //                    any = true;
    //                    Merge(merge);
    //                }
    //                else
    //                {
    //                    // This means there is a bug in the
    //                    // MergeScheduler.  MergeSchedulers in general are
    //                    // not allowed to run a merge involving segments
    //                    // external to this IndexWriter's directory in the
    //                    // background because this would put the index
    //                    // into an inconsistent state (where segmentInfos
    //                    // has been written with such external segments
    //                    // that an IndexReader would fail to load).
    //                    throw new MergePolicy.MergeException("segment \"" + info.name + " exists in external directory yet the MergeScheduler executed the merge in a separate thread");
    //                }
    //            }
    //            else
    //            {
    //                // No more external segments
    //                break;
    //            }
    //        }

    //        if (any)
    //            // Sometimes, on copying an external segment over,
    //            // more merges may become necessary:
    //            mergeScheduler.Merge(this);
    //    }
		
    //    /// <summary>Merges the provided indexes into this index.
    //    /// <p>After this completes, the index is optimized. </p>
    //    /// <p>The provided IndexReaders are not closed.</p>
    //    /// 
    //    /// <p><b>NOTE:</b> the index in each Directory must not be
    //    /// changed (opened by a writer) while this method is
    //    /// running.  Thiw method does not acquire a write lock in
    //    /// each input Directory, so it is up to the caller to
    //    /// enforce this.
    //    /// </p>
    //    /// 
    //    /// <p><b>NOTE:</b> while this is running, any attempts to
    //    /// add or delete documents (with another thread) will be 
    //    /// paused until this method completes.</p>
    //    /// 
    //    /// <p>See {@link #AddIndexes(Directory[])} for
    //    /// details on transactional semantics, temporary free
    //    /// space required in the Directory, and non-CFS segments
    //    /// on an Exception.</p>
    //    /// </summary>
    //    /// <throws>  CorruptIndexException if the index is corrupt </throws>
    //    /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
    //    public virtual void  AddIndexes(IndexReader[] readers)
    //    {
		
    //        EnsureOpen();

    //        // Do not allow add docs or deletes while we are running:
    //        docWriter.PauseAllThreads();

    //        try
    //        {
    //            Optimize(); // start with zero or 1 seg

    //            string mergedName = NewSegmentName();
    //            SegmentMerger merger = new SegmentMerger(this, mergedName, null);

    //            SegmentInfo info;

    //            IndexReader sReader = null;
    //            try
    //            {
    //                lock (this)
    //                {
    //                    if (segmentInfos.Count == 1)
    //                    {
    //                        // add existing index, if any
    //                        sReader = SegmentReader.Get(segmentInfos.Info(0));
    //                        merger.Add(sReader);
    //                    }
    //                }


    //                for (int i = 0; i < readers.Length; i++)
    //                    // add new indexes
    //                    merger.Add(readers[i]);

    //                bool success = false;

    //                StartTransaction();

    //                try
    //                {
    //                    int docCount = merger.Merge(); // merge 'em

    //                    if (sReader != null)
    //                    {
    //                        sReader.Close();
    //                        sReader = null;
    //                    }

    //                    lock (this)
    //                    {
    //                        segmentInfos.RemoveRange(0, segmentInfos.Count); // pop old infos & add new
    //                        info = new SegmentInfo(mergedName, docCount, directory, false, true, -1, null, false);
    //                        segmentInfos.Add(info);
    //                    }
    //                    success = true;
    //                }
    //                finally
    //                {
    //                    if (!success)
    //                    {
    //                        if (infoStream != null)
    //                            Message("hit exception in AddIndexes during merge");

    //                        RollbackTransaction();
    //                    }
    //                    else
    //                    {
    //                        CommitTransaction();
    //                    }
    //                }
    //            }
    //            finally
    //            {
    //                if (sReader != null)
    //                {
    //                    sReader.Close();
    //                }
    //            }

    //            if (mergePolicy is LogMergePolicy && GetUseCompoundFile())
    //            {

    //                bool success = false;

    //                StartTransaction();

    //                try
    //                {
    //                    merger.CreateCompoundFile(mergedName + ".cfs");
    //                    lock (this)
    //                    {
    //                        info.SetUseCompoundFile(true);
    //                    }
    //                }
    //                finally
    //                {
    //                    if (!success)
    //                    {
    //                        if (infoStream != null)
    //                            Message("hit exception building compound file in AddIndexes during merge");

    //                        RollbackTransaction();
    //                    }
    //                    else
    //                    {
    //                        CommitTransaction();
    //                    }
    //                }
    //            }
    //        }
    //        catch (System.OutOfMemoryException oom)
    //        {
    //            hitOOM = true;
    //            throw oom;
    //        }
    //        finally
    //        {
    //            docWriter.ResumeAllThreads();
    //        }
    //    }
		
    //    // This is called after pending added and deleted
    //    // documents have been flushed to the Directory but before
    //    // the change is committed (new segments_N file written).
    //    protected virtual void  DoAfterFlush()
    //    {
    //    }
		
    //    /// <summary> Flush all in-memory buffered updates (adds and deletes)
    //    /// to the Directory. 
    //    /// <p>Note: if <code>autoCommit=false</code>, flushed data would still 
    //    /// not be visible to readers, until {@link #close} is called.
    //    /// </summary>
    //    /// <throws>  CorruptIndexException if the index is corrupt </throws>
    //    /// <throws>  System.IO.IOException if there is a low-level IO error </throws>
    //    public void  Flush()
    //    {
    //        Flush(true, false);
    //    }
		
    //    /// <summary> Flush all in-memory buffered udpates (adds and deletes)
    //    /// to the Directory.
    //    /// </summary>
    //    /// <param name="triggerMerge">if true, we may merge segments (if
    //    /// deletes or docs were flushed) if necessary
    //    /// </param>
    //    /// <param name="flushDocStores">if false we are allowed to keep
    //    /// doc stores open to share with the next segment
    //    /// </param>
    //    public /*protected internal*/ void  Flush(bool triggerMerge, bool flushDocStores)
    //    {
    //        EnsureOpen();
			
    //        if (DoFlush(flushDocStores) && triggerMerge)
    //            MaybeMerge();
    //    }
		
    //    private bool DoFlush(bool flushDocStores)
    //    {
    //        lock (this)
    //        {
				
    //            // Make sure no threads are actively adding a document
				
    //            // Returns true if docWriter is currently aborting, in
    //            // which case we skip flushing this segment
    //            if (docWriter.PauseAllThreads())
    //            {
    //                docWriter.ResumeAllThreads();
    //                return false;
    //            }

    //            try
    //            {

    //                SegmentInfo newSegment = null;

    //                int numDocs = docWriter.GetNumDocsInRAM();

    //                // Always flush docs if there are any
    //                bool flushDocs = numDocs > 0;

    //                // With autoCommit=true we always must flush the doc
    //                // stores when we flush
    //                flushDocStores |= autoCommit;
    //                string docStoreSegment = docWriter.GetDocStoreSegment();
    //                if (docStoreSegment == null)
    //                    flushDocStores = false;

    //                // Always flush deletes if there are any delete terms.
    //                // TODO: when autoCommit=false we don't have to flush
    //                // deletes with every flushed segment; we can save
    //                // CPU/IO by buffering longer & flushing deletes only
    //                // when they are full or writer is being closed.  We
    //                // have to fix the "applyDeletesSelectively" logic to
    //                // apply to more than just the last flushed segment
    //                bool flushDeletes = docWriter.HasDeletes();

    //                if (infoStream != null)
    //                {
    //                    Message("  flush: segment=" + docWriter.GetSegment() + " docStoreSegment=" + docWriter.GetDocStoreSegment() + " docStoreOffset=" + docWriter.GetDocStoreOffset() + " flushDocs=" + flushDocs + " flushDeletes=" + flushDeletes + " flushDocStores=" + flushDocStores + " numDocs=" + numDocs + " numBufDelTerms=" + docWriter.GetNumBufferedDeleteTerms());
    //                    Message("  index before flush " + SegString());
    //                }

    //                int docStoreOffset = docWriter.GetDocStoreOffset();

    //                // docStoreOffset should only be non-zero when
    //                // autoCommit == false
    //                System.Diagnostics.Debug.Assert(!autoCommit || 0 == docStoreOffset);

    //                bool docStoreIsCompoundFile = false;

    //                // Check if the doc stores must be separately flushed
    //                // because other segments, besides the one we are about
    //                // to flush, reference it
    //                if (flushDocStores && (!flushDocs || !docWriter.GetSegment().Equals(docWriter.GetDocStoreSegment())))
    //                {
    //                    // We must separately flush the doc store
    //                    if (infoStream != null)
    //                        Message("  flush shared docStore segment " + docStoreSegment);

    //                    docStoreIsCompoundFile = FlushDocStores();
    //                    flushDocStores = false;
    //                }

    //                string segment = docWriter.GetSegment();

    //                // If we are flushing docs, segment must not be null:
    //                System.Diagnostics.Debug.Assert(segment != null || !flushDocs);

    //                if (flushDocs || flushDeletes)
    //                {

    //                    SegmentInfos rollback = null;

    //                    if (flushDeletes)
    //                        rollback = (SegmentInfos)segmentInfos.Clone();

    //                    bool success = false;

    //                    try
    //                    {
    //                        if (flushDocs)
    //                        {

    //                            if (0 == docStoreOffset && flushDocStores)
    //                            {
    //                                // This means we are flushing private doc stores
    //                                // with this segment, so it will not be shared
    //                                // with other segments
    //                                System.Diagnostics.Debug.Assert(docStoreSegment != null);
    //                                System.Diagnostics.Debug.Assert(docStoreSegment.Equals(segment));
    //                                docStoreOffset = -1;
    //                                docStoreIsCompoundFile = false;
    //                                docStoreSegment = null;
    //                            }

    //                            int flushedDocCount = docWriter.Flush(flushDocStores);

    //                            newSegment = new SegmentInfo(segment, flushedDocCount, directory, false, true, docStoreOffset, docStoreSegment, docStoreIsCompoundFile);
    //                            segmentInfos.Add(newSegment);
    //                        }

    //                        if (flushDeletes)
    //                        {
    //                            // we should be able to change this so we can
    //                            // buffer deletes longer and then flush them to
    //                            // multiple flushed segments, when
    //                            // autoCommit=false
    //                            ApplyDeletes(flushDocs);
    //                        }

    //                        DoAfterFlush();

    //                        Checkpoint();
    //                        success = true;
    //                    }
    //                    finally
    //                    {
    //                        if (!success)
    //                        {

    //                            if (infoStream != null)
    //                                Message("hit exception flushing segment " + segment);

    //                            if (flushDeletes)
    //                            {

    //                                // Carefully check if any partial .del files
    //                                // should be removed:
    //                                int size = rollback.Count;
    //                                for (int i = 0; i < size; i++)
    //                                {
    //                                    string newDelFileName = segmentInfos.Info(i).GetDelFileName();
    //                                    string delFileName = rollback.Info(i).GetDelFileName();
    //                                    if (newDelFileName != null && !newDelFileName.Equals(delFileName))
    //                                        deleter.DeleteFile(newDelFileName);
    //                                }

    //                                // Fully replace the segmentInfos since flushed
    //                                // deletes could have changed any of the
    //                                // SegmentInfo instances:
    //                                segmentInfos.Clear();
    //                                segmentInfos.AddRange(rollback);
    //                            }
    //                            else
    //                            {
    //                                // Remove segment we added, if any:
    //                                if (newSegment != null && segmentInfos.Count > 0 && segmentInfos.Info(segmentInfos.Count - 1) == newSegment)
    //                                    segmentInfos.RemoveAt(segmentInfos.Count - 1);
    //                            }
    //                            if (flushDocs)
    //                                docWriter.Abort(null);
    //                            DeletePartialSegmentsFile();
    //                            deleter.Checkpoint(segmentInfos, false);

    //                            if (segment != null)
    //                                deleter.Refresh(segment);
    //                        }
    //                    }

    //                    deleter.Checkpoint(segmentInfos, autoCommit);

    //                    if (flushDocs && mergePolicy.UseCompoundFile(segmentInfos, newSegment))
    //                    {
    //                        success = false;
    //                        try
    //                        {
    //                            docWriter.CreateCompoundFile(segment);
    //                            newSegment.SetUseCompoundFile(true);
    //                            Checkpoint();
    //                            success = true;
    //                        }
    //                        finally
    //                        {
    //                            if (!success)
    //                            {
    //                                if (infoStream != null)
    //                                    Message("hit exception creating compound file for newly flushed segment " + segment);
    //                                newSegment.SetUseCompoundFile(false);
    //                                deleter.DeleteFile(segment + "." + IndexFileNames.COMPOUND_FILE_EXTENSION);
    //                                DeletePartialSegmentsFile();
    //                            }
    //                        }

    //                        deleter.Checkpoint(segmentInfos, autoCommit);
    //                    }

    //                    return true;
    //                }
    //                else
    //                {
    //                    return false;
    //                }
    //            }
    //            catch (System.OutOfMemoryException oom)
    //            {
    //                hitOOM = true;
    //                throw oom;
    //            }
    //            finally
    //            {
    //                docWriter.ClearFlushPending();
    //                docWriter.ResumeAllThreads();
    //            }
    //        }
    //    }
		
    //    /// <summary>Expert:  Return the total size of all index files currently cached in memory.
    //    /// Useful for size management with flushRamDocs()
    //    /// </summary>
    //    public long RamSizeInBytes()
    //    {
    //        EnsureOpen();
    //        return docWriter.GetRAMUsed();
    //    }
		
    //    /// <summary>Expert:  Return the number of documents whose segments are currently cached in memory.
    //    /// Useful when calling Flush()
    //    /// </summary>
    //    public int NumRamDocs()
    //    {
    //        lock (this)
    //        {
    //            EnsureOpen();
    //            return docWriter.GetNumDocsInRAM();
    //        }
    //    }
		
    //    private int EnsureContiguousMerge(MergePolicy.OneMerge merge)
    //    {
			
    //        int first = segmentInfos.IndexOf(merge.segments.Info(0));
    //        if (first == - 1)
    //            throw new MergePolicy.MergeException("could not find segment " + merge.segments.Info(0).name + " in current segments");
			
    //        int numSegments = segmentInfos.Count;
			
    //        int numSegmentsToMerge = merge.segments.Count;
    //        for (int i = 0; i < numSegmentsToMerge; i++)
    //        {
    //            SegmentInfo info = merge.segments.Info(i);
				
    //            if (first + i >= numSegments || !segmentInfos.Info(first + i).Equals(info))
    //            {
    //                if (segmentInfos.IndexOf(info) == - 1)
    //                    throw new MergePolicy.MergeException("MergePolicy selected a segment (" + info.name + ") that is not in the index");
    //                else
    //                {
    //                    throw new MergePolicy.MergeException("MergePolicy selected non-contiguous segments to merge (" + merge + " vs " + SegString() + "), which IndexWriter (currently) cannot handle");
    //                }
    //            }
    //        }
			
    //        return first;
    //    }
		
    //    /* FIXME if we want to support non-contiguous segment merges */
    //    private bool CommitMerge(MergePolicy.OneMerge merge)
    //    {
    //        lock (this)
    //        {
				
    //            System.Diagnostics.Debug.Assert(merge.registerDone);

    //            if (hitOOM)
    //                return false;

    //            if (infoStream != null)
    //                Message("CommitMerge: " + merge.SegString(directory));
				
    //            // If merge was explicitly aborted, or, if Abort() or
    //            // RollbackTransaction() had been called since our merge
    //            // started (which results in an unqualified
    //            // deleter.Refresh() call that will remove any index
    //            // file that current segments does not reference), we
    //            // abort this merge
    //            if (merge.IsAborted())
    //            {
    //                if (infoStream != null)
    //                    Message("commitMerge: skipping merge " + merge.SegString(directory) + ": it was aborted");
					
    //                System.Diagnostics.Debug.Assert(merge.increfDone);
    //                DecrefMergeSegments(merge);
    //                deleter.Refresh(merge.info.name);
    //                return false;
    //            }
				
    //            bool success = false;
				
    //            int start;
				
    //            try
    //            {
    //                SegmentInfos sourceSegmentsClone = merge.segmentsClone;
    //                SegmentInfos sourceSegments = merge.segments;
					
    //                start = EnsureContiguousMerge(merge);
    //                if (infoStream != null)
    //                    Message("commitMerge " + merge.SegString(directory));
					
    //                // Carefully merge deletes that occurred after we
    //                // started merging:
					
    //                BitVector deletes = null;
    //                int docUpto = 0;
					
    //                int numSegmentsToMerge = sourceSegments.Count;
    //                for (int i = 0; i < numSegmentsToMerge; i++)
    //                {
    //                    SegmentInfo previousInfo = sourceSegmentsClone.Info(i);
    //                    SegmentInfo currentInfo = sourceSegments.Info(i);
						
    //                    System.Diagnostics.Debug.Assert(currentInfo.docCount == previousInfo.docCount);
						
    //                    int docCount = currentInfo.docCount;
						
    //                    if (previousInfo.HasDeletions())
    //                    {
							
    //                        // There were deletes on this segment when the merge
    //                        // started.  The merge has collapsed away those
    //                        // deletes, but, if new deletes were flushed since
    //                        // the merge started, we must now carefully keep any
    //                        // newly flushed deletes but mapping them to the new
    //                        // docIDs.
							
    //                        System.Diagnostics.Debug.Assert(currentInfo.HasDeletions());
							
    //                        // Load deletes present @ start of merge, for this segment:
    //                        BitVector previousDeletes = new BitVector(previousInfo.dir, previousInfo.GetDelFileName());
							
    //                        if (!currentInfo.GetDelFileName().Equals(previousInfo.GetDelFileName()))
    //                        {
    //                            // This means this segment has had new deletes
    //                            // committed since we started the merge, so we
    //                            // must merge them:
    //                            if (deletes == null)
    //                                deletes = new BitVector(merge.info.docCount);
								
    //                            BitVector currentDeletes = new BitVector(currentInfo.dir, currentInfo.GetDelFileName());
    //                            for (int j = 0; j < docCount; j++)
    //                            {
    //                                if (previousDeletes.Get(j))
    //                                    System.Diagnostics.Debug.Assert(currentDeletes.Get(j));
    //                                else
    //                                {
    //                                    if (currentDeletes.Get(j))
    //                                        deletes.Set(docUpto);
    //                                    docUpto++;
    //                                }
    //                            }
    //                        }
    //                        else
    //                            docUpto += docCount - previousDeletes.Count();
    //                    }
    //                    else if (currentInfo.HasDeletions())
    //                    {
    //                        // This segment had no deletes before but now it
    //                        // does:
    //                        if (deletes == null)
    //                            deletes = new BitVector(merge.info.docCount);
    //                        BitVector currentDeletes = new BitVector(directory, currentInfo.GetDelFileName());
							
    //                        for (int j = 0; j < docCount; j++)
    //                        {
    //                            if (currentDeletes.Get(j))
    //                                deletes.Set(docUpto);
    //                            docUpto++;
    //                        }
    //                    }
    //                    // No deletes before or after
    //                    else
    //                        docUpto += currentInfo.docCount;
						
    //                    merge.CheckAborted(directory);
    //                }
					
    //                if (deletes != null)
    //                {
    //                    merge.info.AdvanceDelGen();
    //                    deletes.Write(directory, merge.info.GetDelFileName());
    //                }
    //                success = true;
    //            }
    //            finally
    //            {
    //                if (!success)
    //                {
    //                    if (infoStream != null)
    //                        Message("hit exception creating merged deletes file");
    //                    deleter.Refresh(merge.info.name);
    //                }
    //            }
				
    //            // Simple optimization: if the doc store we are using
    //            // has been closed and is in now compound format (but
    //            // wasn't when we started), then we will switch to the
    //            // compound format as well:
    //            string mergeDocStoreSegment = merge.info.GetDocStoreSegment();
    //            if (mergeDocStoreSegment != null && !merge.info.GetDocStoreIsCompoundFile())
    //            {
    //                int size = segmentInfos.Count;
    //                for (int i = 0; i < size; i++)
    //                {
    //                    SegmentInfo info = segmentInfos.Info(i);
    //                    string docStoreSegment = info.GetDocStoreSegment();
    //                    if (docStoreSegment != null && docStoreSegment.Equals(mergeDocStoreSegment) && info.GetDocStoreIsCompoundFile())
    //                    {
    //                        merge.info.SetDocStoreIsCompoundFile(true);
    //                        break;
    //                    }
    //                }
    //            }
				
    //            success = false;
    //            SegmentInfos rollback = null;
    //            try
    //            {
    //                rollback = (SegmentInfos) segmentInfos.Clone();
    //                ((System.Collections.IList) ((System.Collections.ArrayList) segmentInfos).GetRange(start, start + merge.segments.Count - start)).Clear();
    //                segmentInfos.Insert(start, merge.info);
    //                Checkpoint();
    //                success = true;
    //            }
    //            finally
    //            {
    //                if (!success && rollback != null)
    //                {
    //                    if (infoStream != null)
    //                        Message("hit exception when checkpointing after merge");
    //                    segmentInfos.Clear();
    //                    segmentInfos.AddRange(rollback);
    //                    DeletePartialSegmentsFile();
    //                    deleter.Refresh(merge.info.name);
    //                }
    //            }
				
    //            if (merge.optimize)
    //                segmentsToOptimize.Add(merge.info, merge.info);
				
    //            // Must checkpoint before decrefing so any newly
    //            // referenced files in the new merge.info are incref'd
    //            // first:
    //            deleter.Checkpoint(segmentInfos, autoCommit);
				
    //            DecrefMergeSegments(merge);
				
    //            return true;
    //        }
    //    }
		
    //    private void  DecrefMergeSegments(MergePolicy.OneMerge merge)
    //    {
    //        SegmentInfos sourceSegmentsClone = merge.segmentsClone;
    //        int numSegmentsToMerge = sourceSegmentsClone.Count;
    //        System.Diagnostics.Debug.Assert(merge.increfDone);
    //        merge.increfDone = false;
    //        for (int i = 0; i < numSegmentsToMerge; i++)
    //        {
    //            SegmentInfo previousInfo = sourceSegmentsClone.Info(i);
    //            // Decref all files for this SegmentInfo (this
    //            // matches the incref in mergeInit):
    //            if (previousInfo.dir == directory)
    //                deleter.DecRef(previousInfo.Files());
    //        }
    //    }
		
    //    /// <summary> Merges the indicated segments, replacing them in the stack with a
    //    /// single segment.
    //    /// </summary>
		
    //    public /*internal*/ void  Merge(MergePolicy.OneMerge merge)
    //    {
			
    //        System.Diagnostics.Debug.Assert(merge.registerDone);
    //        System.Diagnostics.Debug.Assert(!merge.optimize || merge.maxNumSegmentsOptimize > 0);
			
    //        bool success = false;

    //        try
    //        {
    //            try
    //            {
    //                try
    //                {
    //                    if (merge.info == null)
    //                        MergeInit(merge);

    //                    if (infoStream != null)
    //                        Message("now merge\n  merge=" + merge.SegString(directory) + "\n  index=" + SegString());

    //                    MergeMiddle(merge);
    //                    success = true;
    //                }
    //                catch (MergePolicy.MergeAbortedException e)
    //                {
    //                    merge.SetException(e);
    //                    AddMergeException(merge);
    //                    // We can ignore this exception, unless the merge
    //                    // involves segments from external directories, in
    //                    // which case we must throw it so, for example, the
    //                    // rollbackTransaction code in AddIndexes* is
    //                    // executed.
    //                    if (merge.isExternal)
    //                        throw e;
    //                }
    //            }
    //            finally
    //            {
    //                lock (this)
    //                {
    //                    try
    //                    {
    //                        MergeFinish(merge);

    //                        if (!success)
    //                        {
    //                            if (infoStream != null)
    //                                Message("hit exception during merge");
    //                            AddMergeException(merge);
    //                            if (merge.info != null && !segmentInfos.Contains(merge.info))
    //                                deleter.Refresh(merge.info.name);
    //                        }

    //                        // This merge (and, generally, any change to the
    //                        // segments) may now enable new merges, so we call
    //                        // merge policy & update pending merges.
    //                        if (success && !merge.IsAborted() && !closed && !closing)
    //                            UpdatePendingMerges(merge.maxNumSegmentsOptimize, merge.optimize);
    //                    }
    //                    finally
    //                    {
    //                        runningMerges.Remove(merge);
    //                        // Optimize may be waiting on the final optimize
    //                        // merge to finish; and finishMerges() may be
    //                        // waiting for all merges to finish:
    //                        System.Threading.Monitor.PulseAll(this);
    //                    }
    //                }
    //            }
    //        }
    //        catch (System.OutOfMemoryException oom)
    //        {
    //            hitOOM = true;
    //            throw oom;
    //        }
    //    }
		
    //    /// <summary>Checks whether this merge involves any segments
    //    /// already participating in a merge.  If not, this merge
    //    /// is "registered", meaning we record that its segments
    //    /// are now participating in a merge, and true is
    //    /// returned.  Else (the merge conflicts) false is
    //    /// returned. 
    //    /// </summary>
    //    internal bool RegisterMerge(MergePolicy.OneMerge merge)
    //    {
    //        lock (this)
    //        {
				
    //            if (merge.registerDone)
    //                return true;
				
    //            int count = merge.segments.Count;
    //            bool isExternal = false;
    //            for (int i = 0; i < count; i++)
    //            {
    //                SegmentInfo info = merge.segments.Info(i);
    //                if (mergingSegments.Contains(info))
    //                    return false;
    //                if (segmentInfos.IndexOf(info) == - 1)
    //                    return false;
    //                if (info.dir != directory)
    //                    isExternal = true;
    //            }
				
    //            pendingMerges.Add(merge);
				
    //            if (infoStream != null)
    //                Message("add merge to pendingMerges: " + merge.SegString(directory) + " [total " + pendingMerges.Count + " pending]");
				
    //            merge.mergeGen = mergeGen;
    //            merge.isExternal = isExternal;
				
    //            // OK it does not conflict; now record that this merge
    //            // is running (while synchronized) to avoid race
    //            // condition where two conflicting merges from different
    //            // threads, start
    //            for (int i = 0; i < count; i++)
    //                if (!mergingSegments.Contains(merge.segments.Info(i)))
    //                    mergingSegments.Add(merge.segments.Info(i), merge.segments.Info(i));
				
    //            // Merge is now registered
    //            merge.registerDone = true;
    //            return true;
    //        }
    //    }
		
    //    /// <summary>Does initial setup for a merge, which is fast but holds
    //    /// the synchronized lock on IndexWriter instance. 
    //    /// </summary>
    //    internal void MergeInit(MergePolicy.OneMerge merge)
    //    {
    //        lock (this)
    //        {
    //            bool success = false;
    //            try
    //            {
    //                _MergeInit(merge);
    //                success = true;
    //            }
    //            finally
    //            {
    //                if (!success)
    //                {
    //                    MergeFinish(merge);
    //                    runningMerges.Remove(merge);
    //                }
    //            }
    //        }
    //    }
				
    //    internal void  _MergeInit(MergePolicy.OneMerge merge)
    //    {
    //        lock (this)
    //        {
    //            System.Diagnostics.Debug.Assert(TestPoint("startMergeInit"));

    //            System.Diagnostics.Debug.Assert(merge.registerDone);

    //            if (merge.info != null)
    //                // mergeInit already done
    //                return;

    //            if (merge.IsAborted())
    //                return ;
				
    //            SegmentInfos sourceSegments = merge.segments;
    //            int end = sourceSegments.Count;
				
    //            EnsureContiguousMerge(merge);
				
    //            // Check whether this merge will allow us to skip
    //            // merging the doc stores (stored field & vectors).
    //            // This is a very substantial optimization (saves tons
    //            // of IO) that can only be applied with
    //            // autoCommit=false.
				
    //            Directory lastDir = directory;
    //            string lastDocStoreSegment = null;
    //            int next = - 1;
				
    //            bool mergeDocStores = false;
    //            bool doFlushDocStore = false;
    //            string currentDocStoreSegment = docWriter.GetDocStoreSegment();
				
    //            // Test each segment to be merged: check if we need to
    //            // flush/merge doc stores
    //            for (int i = 0; i < end; i++)
    //            {
    //                SegmentInfo si = sourceSegments.Info(i);
					
    //                // If it has deletions we must merge the doc stores
    //                if (si.HasDeletions())
    //                    mergeDocStores = true;
					
    //                // If it has its own (private) doc stores we must
    //                // merge the doc stores
    //                if (- 1 == si.GetDocStoreOffset())
    //                    mergeDocStores = true;
					
    //                // If it has a different doc store segment than
    //                // previous segments, we must merge the doc stores
    //                string docStoreSegment = si.GetDocStoreSegment();
    //                if (docStoreSegment == null)
    //                    mergeDocStores = true;
    //                else if (lastDocStoreSegment == null)
    //                    lastDocStoreSegment = docStoreSegment;
    //                else if (!lastDocStoreSegment.Equals(docStoreSegment))
    //                    mergeDocStores = true;
					
    //                // Segments' docScoreOffsets must be in-order,
    //                // contiguous.  For the default merge policy now
    //                // this will always be the case but for an arbitrary
    //                // merge policy this may not be the case
    //                if (- 1 == next)
    //                    next = si.GetDocStoreOffset() + si.docCount;
    //                else if (next != si.GetDocStoreOffset())
    //                    mergeDocStores = true;
    //                else
    //                    next = si.GetDocStoreOffset() + si.docCount;
					
    //                // If the segment comes from a different directory
    //                // we must merge
    //                if (lastDir != si.dir)
    //                    mergeDocStores = true;
					
    //                // If the segment is referencing the current "live"
    //                // doc store outputs then we must merge
    //                if (si.GetDocStoreOffset() != - 1 && currentDocStoreSegment != null && si.GetDocStoreSegment().Equals(currentDocStoreSegment))
    //                    doFlushDocStore = true;
    //            }
				
    //            int docStoreOffset;
    //            string docStoreSegment2;
    //            bool docStoreIsCompoundFile;
				
    //            if (mergeDocStores)
    //            {
    //                docStoreOffset = - 1;
    //                docStoreSegment2 = null;
    //                docStoreIsCompoundFile = false;
    //            }
    //            else
    //            {
    //                SegmentInfo si = sourceSegments.Info(0);
    //                docStoreOffset = si.GetDocStoreOffset();
    //                docStoreSegment2 = si.GetDocStoreSegment();
    //                docStoreIsCompoundFile = si.GetDocStoreIsCompoundFile();
    //            }
				
    //            if (mergeDocStores && doFlushDocStore)
    //            {
    //                // SegmentMerger intends to merge the doc stores
    //                // (stored fields, vectors), and at least one of the
    //                // segments to be merged refers to the currently
    //                // live doc stores.
					
    //                // TODO: if we know we are about to merge away these
    //                // newly flushed doc store files then we should not
    //                // make compound file out of them...
    //                if (infoStream != null)
    //                    Message("flush at merge");
    //                Flush(false, true);
    //            }
				
    //            // We must take a full copy at this point so that we can
    //            // properly merge deletes in CommitMerge()
    //            merge.segmentsClone = (SegmentInfos) merge.segments.Clone();
				
    //            for (int i = 0; i < end; i++)
    //            {
    //                SegmentInfo si = merge.segmentsClone.Info(i);
					
    //                // IncRef all files for this segment info to make sure
    //                // they are not removed while we are trying to merge.
    //                if (si.dir == directory)
    //                    deleter.IncRef(si.Files());
    //            }
				
    //            merge.increfDone = true;
				
    //            merge.mergeDocStores = mergeDocStores;
				
    //            // Bind a new segment name here so even with
    //            // ConcurrentMergePolicy we keep deterministic segment
    //            // names.
    //            merge.info = new SegmentInfo(NewSegmentName(), 0, directory, false, true, docStoreOffset, docStoreSegment2, docStoreIsCompoundFile);
    //            // Also enroll the merged segment into mergingSegments;
    //            // this prevents it from getting selected for a merge
    //            // after our merge is done but while we are building the
    //            // CFS:
    //            mergingSegments.Add(merge.info, merge.info);
    //        }
    //    }
		
    //    /// <summary>Does fininishing for a merge, which is fast but holds
    //    /// the synchronized lock on IndexWriter instance. 
    //    /// </summary>
    //    internal void  MergeFinish(MergePolicy.OneMerge merge)
    //    {
    //        lock (this)
    //        {
				
    //            if (merge.increfDone)
    //                DecrefMergeSegments(merge);
				
    //            System.Diagnostics.Debug.Assert(merge.registerDone);
				
    //            SegmentInfos sourceSegments = merge.segments;
    //            int end = sourceSegments.Count;
    //            for (int i = 0; i < end; i++)
    //                mergingSegments.Remove(sourceSegments.Info(i));
    //            if (merge.info != null)
    //                mergingSegments.Remove(merge.info);
    //            merge.registerDone = false;
    //        }
    //    }
		
    //    /// <summary>Does the actual (time-consuming) work of the merge,
    //    /// but without holding synchronized lock on IndexWriter
    //    /// instance 
    //    /// </summary>
    //    private int MergeMiddle(MergePolicy.OneMerge merge)
    //    {
			
    //        merge.CheckAborted(directory);
			
    //        string mergedName = merge.info.name;
			
    //        SegmentMerger merger = null;
			
    //        int mergedDocCount = 0;
			
    //        SegmentInfos sourceSegments = merge.segments;
    //        SegmentInfos sourceSegmentsClone = merge.segmentsClone;
    //        int numSegments = sourceSegments.Count;
			
    //        if (infoStream != null)
    //            Message("merging " + merge.SegString(directory));
			
    //        merger = new SegmentMerger(this, mergedName, merge);
			
    //        // This is try/finally to make sure merger's readers are
    //        // closed:
			
    //        bool success = false;
			
    //        try
    //        {
    //            int totDocCount = 0;
				
    //            for (int i = 0; i < numSegments; i++)
    //            {
    //                SegmentInfo si = sourceSegmentsClone.Info(i);
    //                IndexReader reader = SegmentReader.Get(si, MERGE_READ_BUFFER_SIZE, merge.mergeDocStores); // no need to set deleter (yet)
    //                merger.Add(reader);
    //                totDocCount += reader.NumDocs();
    //            }
    //            if (infoStream != null)
    //            {
    //                Message("merge: total " + totDocCount + " docs");
    //            }
				
    //            merge.CheckAborted(directory);
				
    //            mergedDocCount = merge.info.docCount = merger.Merge(merge.mergeDocStores);
				
    //            System.Diagnostics.Debug.Assert(mergedDocCount == totDocCount);
				
    //            success = true;
    //        }
    //        finally
    //        {
    //            // close readers before we attempt to delete
    //            // now-obsolete segments
    //            if (merger != null)
    //            {
    //                merger.CloseReaders();
    //            }
    //            if (!success)
    //            {
    //                if (infoStream != null)
    //                    Message("hit exception during merge; now refresh deleter on segment " + mergedName);
    //                lock (this)
    //                {
    //                    AddMergeException(merge);
    //                    deleter.Refresh(mergedName);
    //                }
    //            }
    //        }
			
    //        if (!CommitMerge(merge))
    //        // commitMerge will return false if this merge was aborted
    //            return 0;
			
    //        if (merge.useCompoundFile)
    //        {
				
    //            success = false;
    //            bool skip = false;
    //            string compoundFileName = mergedName + "." + IndexFileNames.COMPOUND_FILE_EXTENSION;
				
    //            try
    //            {
    //                try
    //                {
    //                    merger.CreateCompoundFile(compoundFileName);
    //                    success = true;
    //                }
    //                catch (System.IO.IOException ioe)
    //                {
    //                    lock (this)
    //                    {
    //                        if (segmentInfos.IndexOf(merge.info) == - 1)
    //                        {
    //                            // If another merge kicked in and merged our
    //                            // new segment away while we were trying to
    //                            // build the compound file, we can hit a
    //                            // FileNotFoundException and possibly
    //                            // System.IO.IOException over NFS.  We can tell this has
    //                            // happened because our SegmentInfo is no
    //                            // longer in the segments; if this has
    //                            // happened it is safe to ignore the exception
    //                            // & skip finishing/committing our compound
    //                            // file creating.
    //                            if (infoStream != null)
    //                                Message("hit exception creating compound file; ignoring it because our info (segment " + merge.info.name + ") has been merged away");
    //                            skip = true;
    //                        }
    //                        else
    //                            throw ioe;
    //                    }
    //                }
    //            }
    //            finally
    //            {
    //                if (!success)
    //                {
    //                    if (infoStream != null)
    //                        Message("hit exception creating compound file during merge: skip=" + skip);
						
    //                    lock (this)
    //                    {
    //                        if (!skip)
    //                            AddMergeException(merge);
    //                        deleter.DeleteFile(compoundFileName);
    //                    }
    //                }
    //            }
				
    //            if (!skip)
    //            {
					
    //                lock (this)
    //                {
    //                    if (skip || segmentInfos.IndexOf(merge.info) == - 1 || merge.IsAborted())
    //                    {
    //                        // Our segment (committed in non-compound
    //                        // format) got merged away while we were
    //                        // building the compound format.
    //                        deleter.DeleteFile(compoundFileName);
    //                    }
    //                    else
    //                    {
    //                        success = false;
    //                        try
    //                        {
    //                            merge.info.SetUseCompoundFile(true);
    //                            Checkpoint();
    //                            success = true;
    //                        }
    //                        finally
    //                        {
    //                            if (!success)
    //                            {
    //                                if (infoStream != null)
    //                                    Message("hit exception checkpointing compound file during merge");
									
    //                                // Must rollback:
    //                                AddMergeException(merge);
    //                                merge.info.SetUseCompoundFile(false);
    //                                DeletePartialSegmentsFile();
    //                                deleter.DeleteFile(compoundFileName);
    //                            }
    //                        }
							
    //                        // Give deleter a chance to remove files now.
    //                        deleter.Checkpoint(segmentInfos, autoCommit);
    //                    }
    //                }
    //            }
    //        }
			
    //        return mergedDocCount;
    //    }
		
    //    internal virtual void  AddMergeException(MergePolicy.OneMerge merge)
    //    {
    //        lock (this)
    //        {
    //            if (!mergeExceptions.Contains(merge) && mergeGen == merge.mergeGen)
    //                mergeExceptions.Add(merge);
    //        }
    //    }
		
    //    private void  DeletePartialSegmentsFile()
    //    {
    //        if (segmentInfos.GetLastGeneration() != segmentInfos.GetGeneration())
    //        {
    //            string segmentFileName = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", segmentInfos.GetGeneration());
    //            if (infoStream != null)
    //                Message("now delete partial segments file \"" + segmentFileName + "\"");
				
    //            deleter.DeleteFile(segmentFileName);
    //        }
    //    }
		
    //    // Called during flush to apply any buffered deletes.  If
    //    // flushedNewSegment is true then a new segment was just
    //    // created and flushed from the ram segments, so we will
    //    // selectively apply the deletes to that new segment.
    //    private void  ApplyDeletes(bool flushedNewSegment)
    //    {
			
    //        System.Collections.Hashtable bufferedDeleteTerms = docWriter.GetBufferedDeleteTerms();
    //        System.Collections.IList bufferedDeleteDocIDs = docWriter.GetBufferedDeleteDocIDs();
			
    //        if (infoStream != null)
    //            Message("flush " + docWriter.GetNumBufferedDeleteTerms() + " buffered deleted terms and " + bufferedDeleteDocIDs.Count + " deleted docIDs on " + segmentInfos.Count + " segments.");
			
    //        if (flushedNewSegment)
    //        {
    //            IndexReader reader = null;
    //            try
    //            {
    //                // Open readers w/o opening the stored fields /
    //                // vectors because these files may still be held
    //                // open for writing by docWriter
    //                reader = SegmentReader.Get(segmentInfos.Info(segmentInfos.Count - 1), false);
					
    //                // Apply delete terms to the segment just flushed from ram
    //                // apply appropriately so that a delete term is only applied to
    //                // the documents buffered before it, not those buffered after it.
    //                ApplyDeletesSelectively(bufferedDeleteTerms, bufferedDeleteDocIDs, reader);
    //            }
    //            finally
    //            {
    //                if (reader != null)
    //                {
    //                    try
    //                    {
    //                        reader.DoCommit();
    //                    }
    //                    finally
    //                    {
    //                        reader.DoClose();
    //                    }
    //                }
    //            }
    //        }
			
    //        int infosEnd = segmentInfos.Count;
    //        if (flushedNewSegment)
    //        {
    //            infosEnd--;
    //        }
			
    //        for (int i = 0; i < infosEnd; i++)
    //        {
    //            IndexReader reader = null;
    //            try
    //            {
    //                reader = SegmentReader.Get(segmentInfos.Info(i), false);
					
    //                // Apply delete terms to disk segments
    //                // except the one just flushed from ram.
    //                ApplyDeletes(bufferedDeleteTerms, reader);
    //            }
    //            finally
    //            {
    //                if (reader != null)
    //                {
    //                    try
    //                    {
    //                        reader.DoCommit();
    //                    }
    //                    finally
    //                    {
    //                        reader.DoClose();
    //                    }
    //                }
    //            }
    //        }
			
    //        // Clean up bufferedDeleteTerms.
    //        docWriter.ClearBufferedDeletes();
    //    }
		
    //    // For test purposes.
    //    public /*internal*/ int GetBufferedDeleteTermsSize()
    //    {
    //        lock (this)
    //        {
    //            return docWriter.GetBufferedDeleteTerms().Count;
    //        }
    //    }
		
    //    // For test purposes.
    //    public /*internal*/ int GetNumBufferedDeleteTerms()
    //    {
    //        lock (this)
    //        {
    //            return docWriter.GetNumBufferedDeleteTerms();
    //        }
    //    }
		
    //    // Apply buffered delete terms to the segment just flushed from ram
    //    // apply appropriately so that a delete term is only applied to
    //    // the documents buffered before it, not those buffered after it.
    //    private void  ApplyDeletesSelectively(System.Collections.Hashtable deleteTerms, System.Collections.IList deleteIds, IndexReader reader)
    //    {
    //        System.Collections.IEnumerator iter = new System.Collections.Hashtable(deleteTerms).GetEnumerator();
    //        while (iter.MoveNext())
    //        {
    //            System.Collections.DictionaryEntry entry = (System.Collections.DictionaryEntry) iter.Current;
    //            Term term = (Term) entry.Key;
				
    //            TermDocs docs = reader.TermDocs(term);
    //            if (docs != null)
    //            {
    //                int num = ((DocumentsWriter.Num) entry.Value).GetNum();
    //                try
    //                {
    //                    while (docs.Next())
    //                    {
    //                        int doc = docs.Doc();
    //                        if (doc >= num)
    //                        {
    //                            break;
    //                        }
    //                        reader.DeleteDocument(doc);
    //                    }
    //                }
    //                finally
    //                {
    //                    docs.Close();
    //                }
    //            }
    //        }
			
    //        if (deleteIds.Count > 0)
    //        {
    //            iter = deleteIds.GetEnumerator();
    //            while (iter.MoveNext())
    //            {
    //                reader.DeleteDocument(((System.Int32) iter.Current));
    //            }
    //        }
    //    }
		
    //    // Apply buffered delete terms to this reader.
    //    private void  ApplyDeletes(System.Collections.Hashtable deleteTerms, IndexReader reader)
    //    {
    //        System.Collections.IEnumerator iter = new System.Collections.Hashtable(deleteTerms).GetEnumerator();
    //        while (iter.MoveNext())
    //        {
    //            System.Collections.DictionaryEntry entry = (System.Collections.DictionaryEntry) iter.Current;
    //            reader.DeleteDocuments((Term) entry.Key);
    //        }
    //    }
		
    //    // utility routines for tests
    //    public /*internal*/ virtual SegmentInfo NewestSegment()
    //    {
    //        return segmentInfos.Info(segmentInfos.Count - 1);
    //    }
		
    //    public virtual string SegString()
    //    {
    //        lock (this)
    //        {
    //            System.Text.StringBuilder buffer = new System.Text.StringBuilder();
    //            for (int i = 0; i < segmentInfos.Count; i++)
    //            {
    //                if (i > 0)
    //                {
    //                    buffer.Append(' ');
    //                }
    //                buffer.Append(segmentInfos.Info(i).SegString(directory));
    //            }
				
    //            return buffer.ToString();
    //        }
    //    }
    //    static IndexWriter()
    //    {
    //        DEFAULT_MERGE_FACTOR = LogMergePolicy.DEFAULT_MERGE_FACTOR;
    //        DEFAULT_MAX_MERGE_DOCS = LogDocMergePolicy.DEFAULT_MAX_MERGE_DOCS;
    //        MAX_TERM_LENGTH = DocumentsWriter.MAX_TERM_LENGTH;
    //        if (Constants.WINDOWS)
    //            DEFAULT_MAX_SYNC_PAUSE_SECONDS = 10.0;
    //        else
    //            DEFAULT_MAX_SYNC_PAUSE_SECONDS = 0.0;
    //    }

    //    // Used only by assert for testing.
    //    virtual protected internal bool TestPoint(string name)
    //    {
    //        return true;
    //    }
    //}
