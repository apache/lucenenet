using Lucene.Net.Util;
using System;
using System.Text;

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
    using Codec = Lucene.Net.Codecs.Codec;
    using IndexingChain = Lucene.Net.Index.DocumentsWriterPerThread.IndexingChain;
    using IndexReaderWarmer = Lucene.Net.Index.IndexWriter.IndexReaderWarmer;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;

    /// <summary>
    /// Holds all the configuration used by <see cref="IndexWriter"/> with few setters for
    /// settings that can be changed on an <see cref="IndexWriter"/> instance "live".
    ///
    /// @since 4.0
    /// </summary>
    public class LiveIndexWriterConfig
    {
        private readonly Analyzer analyzer;

        private volatile int maxBufferedDocs;
        private double ramBufferSizeMB;
        private volatile int maxBufferedDeleteTerms;
        private volatile int readerTermsIndexDivisor;
        private volatile IndexReaderWarmer mergedSegmentWarmer;
        private volatile int termIndexInterval; // TODO: this should be private to the codec, not settable here

        // LUCENENET specific: Volatile fields are not CLS compliant,
        // so we are making them internal. This class cannot be inherited
        // from outside of the assembly anyway, since it has no public 
        // constructors, so protected members are moot.

        // modified by IndexWriterConfig
        /// <summary>
        /// <see cref="Index.IndexDeletionPolicy"/> controlling when commit
        /// points are deleted.
        /// </summary>
        internal volatile IndexDeletionPolicy delPolicy;

        /// <summary>
        /// <see cref="Index.IndexCommit"/> that <see cref="IndexWriter"/> is
        /// opened on.
        /// </summary>
        internal volatile IndexCommit commit;

        /// <summary>
        /// <see cref="Index.OpenMode"/> that <see cref="IndexWriter"/> is opened
        /// with.
        /// </summary>
        internal volatile OpenMode openMode;

        /// <summary>
        /// <see cref="Search.Similarities.Similarity"/> to use when encoding norms. </summary>
        internal volatile Similarity similarity;

        /// <summary>
        /// <see cref="IMergeScheduler"/> to use for running merges. </summary>
        internal volatile IMergeScheduler mergeScheduler;

        /// <summary>
        /// Timeout when trying to obtain the write lock on init. </summary>
        internal long writeLockTimeout;

        /// <summary>
        /// <see cref="DocumentsWriterPerThread.IndexingChain"/> that determines how documents are
        /// indexed.
        /// </summary>
        internal volatile IndexingChain indexingChain; // LUCENENET specific - made internal because IndexingChain is internal

        /// <summary>
        /// <see cref="Codecs.Codec"/> used to write new segments. </summary>
        internal volatile Codec codec;

        /// <summary>
        /// <see cref="Util.InfoStream"/> for debugging messages. </summary>
        internal volatile InfoStream infoStream;

        /// <summary>
        /// <see cref="Index.MergePolicy"/> for selecting merges. </summary>
        internal volatile MergePolicy mergePolicy;

        /// <summary>
        /// <see cref="DocumentsWriterPerThreadPool"/> to control how
        /// threads are allocated to <see cref="DocumentsWriterPerThread"/>.
        /// </summary>
        internal volatile DocumentsWriterPerThreadPool indexerThreadPool; // LUCENENET specific - made internal because DocumentsWriterPerThreadPool is internal

        /// <summary>
        /// True if readers should be pooled. </summary>
        internal volatile bool readerPooling;

        /// <summary>
        /// <see cref="Index.FlushPolicy"/> to control when segments are
        /// flushed.
        /// </summary>
        internal volatile FlushPolicy flushPolicy; // LUCENENET specific - made internal because FlushPolicy is internal

        /// <summary>
        /// Sets the hard upper bound on RAM usage for a single
        /// segment, after which the segment is forced to flush.
        /// </summary>
        internal volatile int perThreadHardLimitMB;

        /// <summary>
        /// <see cref="LuceneVersion"/> that <see cref="IndexWriter"/> should emulate. </summary>
        internal readonly LuceneVersion matchVersion;

        /// <summary>
        /// True if segment flushes should use compound file format </summary>
        internal volatile bool useCompoundFile = IndexWriterConfig.DEFAULT_USE_COMPOUND_FILE_SYSTEM;

        /// <summary>
        /// True if merging should check integrity of segments before merge </summary>
        internal volatile bool checkIntegrityAtMerge = IndexWriterConfig.DEFAULT_CHECK_INTEGRITY_AT_MERGE;

        // used by IndexWriterConfig
        internal LiveIndexWriterConfig(Analyzer analyzer, LuceneVersion matchVersion)
        {
            this.analyzer = analyzer;
            this.matchVersion = matchVersion;
            ramBufferSizeMB = IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB;
            maxBufferedDocs = IndexWriterConfig.DEFAULT_MAX_BUFFERED_DOCS;
            maxBufferedDeleteTerms = IndexWriterConfig.DEFAULT_MAX_BUFFERED_DELETE_TERMS;
            readerTermsIndexDivisor = IndexWriterConfig.DEFAULT_READER_TERMS_INDEX_DIVISOR;
            mergedSegmentWarmer = null;
            termIndexInterval = IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL; // TODO: this should be private to the codec, not settable here
            delPolicy = new KeepOnlyLastCommitDeletionPolicy();
            commit = null;
            useCompoundFile = IndexWriterConfig.DEFAULT_USE_COMPOUND_FILE_SYSTEM;
            openMode = Index.OpenMode.CREATE_OR_APPEND;
            similarity = IndexSearcher.DefaultSimilarity;
            mergeScheduler = new ConcurrentMergeScheduler();
            writeLockTimeout = IndexWriterConfig.WRITE_LOCK_TIMEOUT;
            indexingChain = DocumentsWriterPerThread.DefaultIndexingChain;
            codec = Codec.Default;
            if (codec is null)
            {
                throw new NullReferenceException();
            }
            infoStream = Util.InfoStream.Default;
            mergePolicy = new TieredMergePolicy();
            flushPolicy = new FlushByRamOrCountsPolicy();
            readerPooling = IndexWriterConfig.DEFAULT_READER_POOLING;
            indexerThreadPool = new DocumentsWriterPerThreadPool(IndexWriterConfig.DEFAULT_MAX_THREAD_STATES);
            perThreadHardLimitMB = IndexWriterConfig.DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB;
        }

        /// <summary>
        /// Creates a new config that that handles the live <see cref="IndexWriter"/>
        /// settings.
        /// </summary>
        internal LiveIndexWriterConfig(IndexWriterConfig config)
        {
            maxBufferedDeleteTerms = config.MaxBufferedDeleteTerms;
            maxBufferedDocs = config.MaxBufferedDocs;
            mergedSegmentWarmer = config.MergedSegmentWarmer;
            ramBufferSizeMB = config.RAMBufferSizeMB;
            readerTermsIndexDivisor = config.ReaderTermsIndexDivisor;
            termIndexInterval = config.TermIndexInterval;
            matchVersion = config.matchVersion;
            analyzer = config.Analyzer;
            delPolicy = config.IndexDeletionPolicy;
            commit = config.IndexCommit;
            openMode = config.OpenMode;
            similarity = config.Similarity;
            mergeScheduler = config.MergeScheduler;
            writeLockTimeout = config.WriteLockTimeout;
            indexingChain = config.IndexingChain;
            codec = config.Codec;
            infoStream = config.InfoStream;
            mergePolicy = config.MergePolicy;
            indexerThreadPool = config.IndexerThreadPool;
            readerPooling = config.UseReaderPooling;
            flushPolicy = config.FlushPolicy;
            perThreadHardLimitMB = config.RAMPerThreadHardLimitMB;
            useCompoundFile = config.UseCompoundFile;
            checkIntegrityAtMerge = config.CheckIntegrityAtMerge;
        }

        /// <summary>
        /// Gets the default analyzer to use for indexing documents. </summary>
        public virtual Analyzer Analyzer => analyzer;

        /// <summary>
        /// Expert: Gets or sets the interval between indexed terms. Large values cause less
        /// memory to be used by <see cref="IndexReader"/>, but slow random-access to terms. Small
        /// values cause more memory to be used by an <see cref="IndexReader"/>, and speed
        /// random-access to terms.
        /// <para/>
        /// This parameter determines the amount of computation required per query
        /// term, regardless of the number of documents that contain that term. In
        /// particular, it is the maximum number of other terms that must be scanned
        /// before a term is located and its frequency and position information may be
        /// processed. In a large index with user-entered query terms, query processing
        /// time is likely to be dominated not by term lookup but rather by the
        /// processing of frequency and positional data. In a small index or when many
        /// uncommon query terms are generated (e.g., by wildcard queries) term lookup
        /// may become a dominant cost.
        /// <para/>
        /// In particular, <c>numUniqueTerms/interval</c> terms are read into
        /// memory by an <see cref="IndexReader"/>, and, on average, <c>interval/2</c> terms
        /// must be scanned for each random term access.
        ///
        /// <para/>
        /// Takes effect immediately, but only applies to newly flushed/merged
        /// segments.
        ///
        /// <para/>
        /// <b>NOTE:</b> this parameter does not apply to all <see cref="Codecs.PostingsFormat"/> implementations,
        /// including the default one in this release. It only makes sense for term indexes
        /// that are implemented as a fixed gap between terms. For example,
        /// <see cref="Codecs.Lucene41.Lucene41PostingsFormat"/> implements the term index instead based upon how
        /// terms share prefixes. To configure its parameters (the minimum and maximum size
        /// for a block), you would instead use <see cref="Codecs.Lucene41.Lucene41PostingsFormat.Lucene41PostingsFormat(int, int)"/>.
        /// which can also be configured on a per-field basis:
        /// <code>
        /// public class MyLucene45Codec : Lucene45Codec
        /// {
        ///     //customize Lucene41PostingsFormat, passing minBlockSize=50, maxBlockSize=100
        ///     private readonly PostingsFormat tweakedPostings = new Lucene41PostingsFormat(50, 100);
        /// 
        ///     public override PostingsFormat GetPostingsFormatForField(string field)
        ///     {
        ///         if (field.Equals("fieldWithTonsOfTerms", StringComparison.Ordinal))
        ///             return tweakedPostings;
        ///         else
        ///             return base.GetPostingsFormatForField(field);
        ///     }
        /// }
        /// ...
        /// 
        /// iwc.Codec = new MyLucene45Codec();
        /// </code>
        /// Note that other implementations may have their own parameters, or no parameters at all.
        /// </summary>
        /// <seealso cref="IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL"/>
        public virtual int TermIndexInterval
        {
            get => termIndexInterval;
            set => this.termIndexInterval = value; // TODO: this should be private to the codec, not settable here
        }

        /// <summary>
        /// Gets or sets a value that determines the maximum number of delete-by-term operations that will be
        /// buffered before both the buffered in-memory delete terms and queries are
        /// applied and flushed.
        /// <para/>
        /// Disabled by default (writer flushes by RAM usage).
        /// <para/>
        /// NOTE: this setting won't trigger a segment flush.
        ///
        /// <para/>
        /// Takes effect immediately, but only the next time a document is added,
        /// updated or deleted. Also, if you only delete-by-query, this setting has no
        /// effect, i.e. delete queries are buffered until the next segment is flushed.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///           if maxBufferedDeleteTerms is enabled but smaller than 1
        /// </exception>
        /// <seealso cref="RAMBufferSizeMB"/>
        public virtual int MaxBufferedDeleteTerms
        {
            get => maxBufferedDeleteTerms;
            set
            {
                if (value != IndexWriterConfig.DISABLE_AUTO_FLUSH && value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxBufferedDeleteTerms), "maxBufferedDeleteTerms must at least be 1 when enabled"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                this.maxBufferedDeleteTerms = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that determines the amount of RAM that may be used for buffering added documents
        /// and deletions before they are flushed to the <see cref="Store.Directory"/>. Generally for
        /// faster indexing performance it's best to flush by RAM usage instead of
        /// document count and use as large a RAM buffer as you can.
        /// <para/>
        /// When this is set, the writer will flush whenever buffered documents and
        /// deletions use this much RAM. Pass in
        /// <see cref="IndexWriterConfig.DISABLE_AUTO_FLUSH"/> to prevent triggering a flush
        /// due to RAM usage. Note that if flushing by document count is also enabled,
        /// then the flush will be triggered by whichever comes first.
        /// <para/>
        /// The maximum RAM limit is inherently determined by the runtime's available
        /// memory. Yet, an <see cref="IndexWriter"/> session can consume a significantly
        /// larger amount of memory than the given RAM limit since this limit is just
        /// an indicator when to flush memory resident documents to the <see cref="Store.Directory"/>.
        /// Flushes are likely happen concurrently while other threads adding documents
        /// to the writer. For application stability the available memory in the runtime
        /// should be significantly larger than the RAM buffer used for indexing.
        /// <para/>
        /// <b>NOTE</b>: the account of RAM usage for pending deletions is only
        /// approximate. Specifically, if you delete by <see cref="Search.Query"/>, Lucene currently has no
        /// way to measure the RAM usage of individual Queries so the accounting will
        /// under-estimate and you should compensate by either calling <see cref="IndexWriter.Commit()"/>
        /// periodically yourself, or by setting <see cref="MaxBufferedDeleteTerms"/>
        /// to flush and apply buffered deletes by count instead of RAM usage (for each
        /// buffered delete <see cref="Search.Query"/> a constant number of bytes is used to estimate RAM
        /// usage). Note that enabling <see cref="MaxBufferedDeleteTerms"/> will not
        /// trigger any segment flushes.
        /// <para/>
        /// <b>NOTE</b>: It's not guaranteed that all memory resident documents are
        /// flushed once this limit is exceeded. Depending on the configured
        /// <seealso cref="FlushPolicy"/> only a subset of the buffered documents are flushed and
        /// therefore only parts of the RAM buffer is released.
        /// <para/>
        ///
        /// The default value is <see cref="IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB"/>.
        ///
        /// <para/>
        /// Takes effect immediately, but only the next time a document is added,
        /// updated or deleted.
        /// </summary>
        /// <seealso cref="IndexWriterConfig.RAMPerThreadHardLimitMB"/>
        /// <exception cref="ArgumentException">
        ///           if ramBufferSizeMB is enabled but non-positive, or it disables
        ///           ramBufferSizeMB when maxBufferedDocs is already disabled </exception>
        public virtual double RAMBufferSizeMB
        {
            get => ramBufferSizeMB;
            set
            {
                if (value != IndexWriterConfig.DISABLE_AUTO_FLUSH && value <= 0.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(RAMBufferSizeMB), "ramBufferSizeMB should be > 0.0 MB when enabled"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                if (value == IndexWriterConfig.DISABLE_AUTO_FLUSH && maxBufferedDocs == IndexWriterConfig.DISABLE_AUTO_FLUSH)
                {
                    throw new ArgumentException("at least one of ramBufferSizeMB and maxBufferedDocs must be enabled");
                }
                this.ramBufferSizeMB = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that determines the minimal number of documents required before the buffered
        /// in-memory documents are flushed as a new Segment. Large values generally
        /// give faster indexing.
        ///
        /// <para/>
        /// When this is set, the writer will flush every maxBufferedDocs added
        /// documents. Pass in <see cref="IndexWriterConfig.DISABLE_AUTO_FLUSH"/> to prevent
        /// triggering a flush due to number of buffered documents. Note that if
        /// flushing by RAM usage is also enabled, then the flush will be triggered by
        /// whichever comes first.
        ///
        /// <para/>
        /// Disabled by default (writer flushes by RAM usage).
        ///
        /// <para/>
        /// Takes effect immediately, but only the next time a document is added,
        /// updated or deleted.
        /// </summary>
        /// <seealso cref="RAMBufferSizeMB"/>
        /// <exception cref="ArgumentException">
        ///           if maxBufferedDocs is enabled but smaller than 2, or it disables
        ///           maxBufferedDocs when ramBufferSizeMB is already disabled </exception>
        public virtual int MaxBufferedDocs
        {
            get => maxBufferedDocs;
            set
            {
                if (value != IndexWriterConfig.DISABLE_AUTO_FLUSH && value < 2)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxBufferedDocs), "maxBufferedDocs must at least be 2 when enabled"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                if (value == IndexWriterConfig.DISABLE_AUTO_FLUSH && ramBufferSizeMB == IndexWriterConfig.DISABLE_AUTO_FLUSH)
                {
                    throw new ArgumentException("at least one of ramBufferSizeMB and maxBufferedDocs must be enabled");
                }
                this.maxBufferedDocs = value;
            }
        }

        /// <summary>
        /// Gets or sets the merged segment warmer. See <see cref="IndexReaderWarmer"/>.
        /// <para/>
        /// Takes effect on the next merge.
        /// </summary>
        public virtual IndexReaderWarmer MergedSegmentWarmer
        {
            get => mergedSegmentWarmer;
            set => this.mergedSegmentWarmer = value;
        }

        /// <summary>
        /// Gets or sets the termsIndexDivisor passed to any readers that <see cref="IndexWriter"/> opens,
        /// for example when applying deletes or creating a near-real-time reader in
        /// <see cref="DirectoryReader.Open(IndexWriter, bool)"/>. If you pass -1, the
        /// terms index won't be loaded by the readers. This is only useful in advanced
        /// situations when you will only .Next() through all terms; attempts to seek
        /// will hit an exception.
        ///
        /// <para/>
        /// Takes effect immediately, but only applies to readers opened after this
        /// call
        /// <para/>
        /// <b>NOTE:</b> divisor settings &gt; 1 do not apply to all <see cref="Codecs.PostingsFormat"/>
        /// implementations, including the default one in this release. It only makes
        /// sense for terms indexes that can efficiently re-sample terms at load time.
        /// </summary>
        public virtual int ReaderTermsIndexDivisor
        {
            get => readerTermsIndexDivisor;
            set
            {
                if (value <= 0 && value != -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(ReaderTermsIndexDivisor), "divisor must be >= 1, or -1 (got " + value + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                readerTermsIndexDivisor = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="Index.OpenMode"/> set by <see cref="IndexWriterConfig.OpenMode"/> setter. </summary>
        public virtual OpenMode OpenMode => openMode;

        /// <summary>
        /// Gets the <see cref="Index.IndexDeletionPolicy"/> specified in
        /// <see cref="IndexWriterConfig.IndexDeletionPolicy"/> setter or
        /// the default <see cref="KeepOnlyLastCommitDeletionPolicy"/>
        /// </summary>
        public virtual IndexDeletionPolicy IndexDeletionPolicy => delPolicy;

        /// <summary>
        /// Gets the <see cref="IndexCommit"/> as specified in
        /// <see cref="IndexWriterConfig.IndexCommit"/> setter or the default,
        /// <c>null</c> which specifies to open the latest index commit point.
        /// </summary>
        public virtual IndexCommit IndexCommit => commit;

        /// <summary>
        /// Expert: returns the <see cref="Search.Similarities.Similarity"/> implementation used by this
        /// <see cref="IndexWriter"/>.
        /// </summary>
        public virtual Similarity Similarity => similarity;

        /// <summary>
        /// Returns the <see cref="IMergeScheduler"/> that was set by
        /// <see cref="IndexWriterConfig.MergeScheduler"/> setter.
        /// </summary>
        public virtual IMergeScheduler MergeScheduler => mergeScheduler;

        /// <summary>
        /// Returns allowed timeout when acquiring the write lock.
        /// </summary>
        /// <seealso cref="IndexWriterConfig.WriteLockTimeout"/>
        public virtual long WriteLockTimeout => writeLockTimeout;

        /// <summary>
        /// Returns the current <see cref="Codecs.Codec"/>. </summary>
        public virtual Codec Codec => codec;

        /// <summary>
        /// Returns the current <see cref="Index.MergePolicy"/> in use by this writer.
        /// </summary>
        /// <seealso cref="IndexWriterConfig.MergePolicy"/>
        public virtual MergePolicy MergePolicy => mergePolicy;

        /// <summary>
        /// Returns the configured <see cref="DocumentsWriterPerThreadPool"/> instance.
        /// </summary>
        /// <seealso cref="IndexWriterConfig.IndexerThreadPool"/>
        internal virtual DocumentsWriterPerThreadPool IndexerThreadPool => indexerThreadPool;

        /// <summary>
        /// Returns the max number of simultaneous threads that may be indexing
        /// documents at once in <see cref="IndexWriter"/>.
        /// </summary>
        // LUCENENET: Changes brought over from 4.8.1 mean there is no chance of a cast failure
        public virtual int MaxThreadStates => indexerThreadPool.MaxThreadStates;

        /// <summary>
        /// Returns <c>true</c> if <see cref="IndexWriter"/> should pool readers even if
        /// <see cref="DirectoryReader.Open(IndexWriter, bool)"/> has not been called.
        /// </summary>
        public virtual bool UseReaderPooling => readerPooling;

        /// <summary>
        /// Returns the indexing chain set on
        /// <see cref="IndexWriterConfig.IndexingChain"/>.
        /// </summary>
        internal virtual IndexingChain IndexingChain => indexingChain;

        /// <summary>
        /// Returns the max amount of memory each <see cref="DocumentsWriterPerThread"/> can
        /// consume until forcefully flushed.
        /// </summary>
        /// <seealso cref="IndexWriterConfig.RAMPerThreadHardLimitMB"/>
        public virtual int RAMPerThreadHardLimitMB => perThreadHardLimitMB;

        /// <seealso cref="IndexWriterConfig.FlushPolicy"/>
        internal virtual FlushPolicy FlushPolicy => flushPolicy;

        /// <summary>
        /// Returns <see cref="Util.InfoStream"/> used for debugging.
        /// </summary>
        /// <seealso cref="IndexWriterConfig.SetInfoStream(InfoStream)"/>
        public virtual InfoStream InfoStream => infoStream;

        /// <summary>
        /// Gets or sets if the <see cref="IndexWriter"/> should pack newly written segments in a
        /// compound file. Default is <c>true</c>.
        /// <para>
        /// Use <c>false</c> for batch indexing with very large RAM buffer
        /// settings.
        /// </para>
        /// <para>
        /// <b>Note: To control compound file usage during segment merges see
        /// <seealso cref="MergePolicy.NoCFSRatio"/> and
        /// <seealso cref="MergePolicy.MaxCFSSegmentSizeMB"/>. This setting only
        /// applies to newly created segments.</b>
        /// </para>
        /// </summary>
        public virtual bool UseCompoundFile
        {
            get => useCompoundFile;
            set => this.useCompoundFile = value;
        }

        /// <summary>
        /// Gets or sets if <see cref="IndexWriter"/> should call <see cref="AtomicReader.CheckIntegrity()"/>
        /// on existing segments before merging them into a new one.
        /// <para>
        /// Use <c>true</c> to enable this safety check, which can help
        /// reduce the risk of propagating index corruption from older segments
        /// into new ones, at the expense of slower merging.
        /// </para>
        /// </summary>
        public virtual bool CheckIntegrityAtMerge
        {
            get => checkIntegrityAtMerge;
            set => this.checkIntegrityAtMerge = value;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("matchVersion=").Append(matchVersion).Append("\n");
            sb.Append("analyzer=").Append(analyzer is null ? "null" : analyzer.GetType().Name).Append("\n");
            sb.Append("ramBufferSizeMB=").Append(RAMBufferSizeMB).Append("\n");
            sb.Append("maxBufferedDocs=").Append(MaxBufferedDocs).Append("\n");
            sb.Append("maxBufferedDeleteTerms=").Append(MaxBufferedDeleteTerms).Append("\n");
            sb.Append("mergedSegmentWarmer=").Append(MergedSegmentWarmer).Append("\n");
            sb.Append("readerTermsIndexDivisor=").Append(ReaderTermsIndexDivisor).Append("\n");
            sb.Append("termIndexInterval=").Append(TermIndexInterval).Append("\n"); // TODO: this should be private to the codec, not settable here
            sb.Append("delPolicy=").Append(IndexDeletionPolicy.GetType().Name).Append("\n");
            IndexCommit commit = IndexCommit;
            sb.Append("commit=").Append(commit is null ? "null" : commit.ToString()).Append("\n");
            sb.Append("openMode=").Append(OpenMode).Append("\n");
            sb.Append("similarity=").Append(Similarity.GetType().Name).Append("\n");
            sb.Append("mergeScheduler=").Append(MergeScheduler).Append("\n");
            sb.Append("default WRITE_LOCK_TIMEOUT=").Append(IndexWriterConfig.WRITE_LOCK_TIMEOUT).Append("\n");
            sb.Append("writeLockTimeout=").Append(WriteLockTimeout).Append("\n");
            sb.Append("codec=").Append(Codec).Append("\n");
            sb.Append("infoStream=").Append(InfoStream.GetType().Name).Append("\n");
            sb.Append("mergePolicy=").Append(MergePolicy).Append("\n");
            sb.Append("indexerThreadPool=").Append(IndexerThreadPool).Append("\n");
            sb.Append("readerPooling=").Append(UseReaderPooling).Append("\n");
            sb.Append("perThreadHardLimitMB=").Append(RAMPerThreadHardLimitMB).Append("\n");
            sb.Append("useCompoundFile=").Append(UseCompoundFile).Append("\n");
            sb.Append("checkIntegrityAtMerge=").Append(CheckIntegrityAtMerge).Append("\n");
            return sb.ToString();
        }
    }
}