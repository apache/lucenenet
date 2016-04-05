using System;
using System.Text;

namespace Lucene.Net.Index
{
    using Lucene.Net.Util;
    using System.IO;

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
    using InfoStream = Lucene.Net.Util.InfoStream;
    using PrintStreamInfoStream = Lucene.Net.Util.PrintStreamInfoStream;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;

    /// <summary>
    /// Holds all the configuration that is used to create an <seealso cref="IndexWriter"/>.
    /// Once <seealso cref="IndexWriter"/> has been created with this object, changes to this
    /// object will not affect the <seealso cref="IndexWriter"/> instance. For that, use
    /// <seealso cref="LiveIndexWriterConfig"/> that is returned from <seealso cref="IndexWriter#getConfig()"/>.
    ///
    /// <p>
    /// All setter methods return <seealso cref="IndexWriterConfig"/> to allow chaining
    /// settings conveniently, for example:
    ///
    /// <pre class="prettyprint">
    /// IndexWriterConfig conf = new IndexWriterConfig(analyzer);
    /// conf.setter1().setter2();
    /// </pre>
    /// </summary>
    /// <seealso cref= IndexWriter#getConfig()
    ///
    /// @since 3.1 </seealso>
    public sealed class IndexWriterConfig : LiveIndexWriterConfig
    {
        /// <summary>
        /// Specifies the open mode for <seealso cref="IndexWriter"/>.
        /// </summary>
        public enum OpenMode_e
        {
            /// <summary>
            /// Creates a new index or overwrites an existing one.
            /// </summary>
            CREATE,

            /// <summary>
            /// Opens an existing index.
            /// </summary>
            APPEND,

            /// <summary>
            /// Creates a new index if one does not exist,
            /// otherwise it opens the index and documents will be appended.
            /// </summary>
            CREATE_OR_APPEND
        }

        /// <summary>
        /// Default value is 32. Change using <seealso cref="#setTermIndexInterval(int)"/>. </summary>
        public const int DEFAULT_TERM_INDEX_INTERVAL = 32; // TODO: this should be private to the codec, not settable here

        /// <summary>
        /// Denotes a flush trigger is disabled. </summary>
        public const int DISABLE_AUTO_FLUSH = -1;

        /// <summary>
        /// Disabled by default (because IndexWriter flushes by RAM usage by default). </summary>
        public const int DEFAULT_MAX_BUFFERED_DELETE_TERMS = DISABLE_AUTO_FLUSH;

        /// <summary>
        /// Disabled by default (because IndexWriter flushes by RAM usage by default). </summary>
        public const int DEFAULT_MAX_BUFFERED_DOCS = DISABLE_AUTO_FLUSH;

        /// <summary>
        /// Default value is 16 MB (which means flush when buffered docs consume
        /// approximately 16 MB RAM).
        /// </summary>
        public const double DEFAULT_RAM_BUFFER_SIZE_MB = 16.0;

        /// <summary>
        /// Default value for the write lock timeout (1,000 ms).
        /// </summary>
        /// <seealso cref= #setDefaultWriteLockTimeout(long) </seealso>
        public static long WRITE_LOCK_TIMEOUT = 1000;

        /// <summary>
        /// Default setting for <seealso cref="#setReaderPooling"/>. </summary>
        public const bool DEFAULT_READER_POOLING = false;

        /// <summary>
        /// Default value is 1. Change using <seealso cref="#setReaderTermsIndexDivisor(int)"/>. </summary>
        public const int DEFAULT_READER_TERMS_INDEX_DIVISOR = DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR;

        /// <summary>
        /// Default value is 1945. Change using <seealso cref="#setRAMPerThreadHardLimitMB(int)"/> </summary>
        public const int DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB = 1945;

        /// <summary>
        /// The maximum number of simultaneous threads that may be
        ///  indexing documents at once in IndexWriter; if more
        ///  than this many threads arrive they will wait for
        ///  others to finish. Default value is 8.
        /// </summary>
        public const int DEFAULT_MAX_THREAD_STATES = 8;

        /// <summary>
        /// Default value for compound file system for newly written segments
        ///  (set to <code>true</code>). For batch indexing with very large
        ///  ram buffers use <code>false</code>
        /// </summary>
        public const bool DEFAULT_USE_COMPOUND_FILE_SYSTEM = true;

        /// <summary>
        /// Default value for calling <seealso cref="AtomicReader#checkIntegrity()"/> before
        ///  merging segments (set to <code>false</code>). You can set this
        ///  to <code>true</code> for additional safety.
        /// </summary>
        public const bool DEFAULT_CHECK_INTEGRITY_AT_MERGE = false;

        /// <summary>
        /// Sets the default (for any instance) maximum time to wait for a write lock
        /// (in milliseconds).
        /// </summary>
        public static long DefaultWriteLockTimeout
        {
            set
            {
                WRITE_LOCK_TIMEOUT = value;
            }
            get
            {
                return WRITE_LOCK_TIMEOUT;
            }
        }

        // indicates whether this config instance is already attached to a writer.
        // not final so that it can be cloned properly.
        private SetOnce<IndexWriter> Writer = new SetOnce<IndexWriter>();

        /// <summary>
        /// Sets the <seealso cref="IndexWriter"/> this config is attached to.
        /// </summary>
        /// <exception cref="AlreadySetException">
        ///           if this config is already attached to a writer. </exception>
        internal IndexWriterConfig SetIndexWriter(IndexWriter writer)
        {
            this.Writer.Set(writer);
            return this;
        }

        /// <summary>
        /// Creates a new config that with defaults that match the specified
        /// <seealso cref="LuceneVersion"/> as well as the default {@link
        /// Analyzer}. If matchVersion is >= {@link
        /// Version#LUCENE_32}, <seealso cref="TieredMergePolicy"/> is used
        /// for merging; else <seealso cref="LogByteSizeMergePolicy"/>.
        /// Note that <seealso cref="TieredMergePolicy"/> is free to select
        /// non-contiguous merges, which means docIDs may not
        /// remain monotonic over time.  If this is a problem you
        /// should switch to <seealso cref="LogByteSizeMergePolicy"/> or
        /// <seealso cref="LogDocMergePolicy"/>.
        /// </summary>
        public IndexWriterConfig(LuceneVersion matchVersion, Analyzer analyzer)
            : base(analyzer, matchVersion)
        {
        }

        public object Clone()
        {
            try
            {
                IndexWriterConfig clone = (IndexWriterConfig)this.MemberwiseClone();

                clone.Writer = (SetOnce<IndexWriter>)Writer.Clone();

                // Mostly shallow clone, but do a deepish clone of
                // certain objects that have state that cannot be shared
                // across IW instances:
                clone.delPolicy = (IndexDeletionPolicy)delPolicy.Clone();
                clone.flushPolicy = (FlushPolicy)flushPolicy.Clone();
                clone.indexerThreadPool = (DocumentsWriterPerThreadPool)indexerThreadPool.Clone();
                // we clone the infoStream because some impls might have state variables
                // such as line numbers, message throughput, ...
                clone.infoStream = (InfoStream)infoStream.Clone();
                clone.mergePolicy = (MergePolicy)mergePolicy.Clone();
                clone.mergeScheduler = (MergeScheduler)mergeScheduler.Clone();

                return clone;
            }
            catch
            {
                // .NET port: no need to deal with checked exceptions here
                throw;
            }
        }

        /// <summary>
        /// Specifies <seealso cref="OpenMode"/> of the index.
        ///
        /// <p>Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetOpenMode(OpenMode_e? openMode)
        {
            if (openMode == null)
            {
                throw new System.ArgumentException("openMode must not be null");
            }
            this.openMode = openMode;
            return this;
        }

        public override OpenMode_e? OpenMode
        {
            get
            {
                return openMode;
            }
        }

        /// <summary>
        /// Expert: allows an optional <seealso cref="IndexDeletionPolicy"/> implementation to be
        /// specified. You can use this to control when prior commits are deleted from
        /// the index. The default policy is <seealso cref="KeepOnlyLastCommitDeletionPolicy"/>
        /// which removes all prior commits as soon as a new commit is done (this
        /// matches behavior before 2.2). Creating your own policy can allow you to
        /// explicitly keep previous "point in time" commits alive in the index for
        /// some time, to allow readers to refresh to the new commit without having the
        /// old commit deleted out from under them. this is necessary on filesystems
        /// like NFS that do not support "delete on last close" semantics, which
        /// Lucene's "point in time" search normally relies on.
        /// <p>
        /// <b>NOTE:</b> the deletion policy cannot be null.
        ///
        /// <p>Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetIndexDeletionPolicy(IndexDeletionPolicy deletionPolicy)
        {
            if (deletionPolicy == null)
            {
                throw new System.ArgumentException("indexDeletionPolicy must not be null");
            }
            this.delPolicy = deletionPolicy;
            return this;
        }

        public override IndexDeletionPolicy DelPolicy
        {
            get
            {
                return delPolicy;
            }
        }

        /// <summary>
        /// Expert: allows to open a certain commit point. The default is null which
        /// opens the latest commit point.
        ///
        /// <p>Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetIndexCommit(IndexCommit commit)
        {
            this.Commit = commit;
            return this;
        }

        public override IndexCommit IndexCommit
        {
            get
            {
                return Commit;
            }
        }

        /// <summary>
        /// Expert: set the <seealso cref="Similarity"/> implementation used by this IndexWriter.
        /// <p>
        /// <b>NOTE:</b> the similarity cannot be null.
        ///
        /// <p>Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetSimilarity(Similarity similarity)
        {
            if (similarity == null)
            {
                throw new System.ArgumentException("similarity must not be null");
            }
            this.similarity = similarity;
            return this;
        }

        public override Similarity Similarity
        {
            get
            {
                return similarity;
            }
        }

        /// <summary>
        /// Expert: sets the merge scheduler used by this writer. The default is
        /// <seealso cref="ConcurrentMergeScheduler"/>.
        /// <p>
        /// <b>NOTE:</b> the merge scheduler cannot be null.
        ///
        /// <p>Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetMergeScheduler(MergeScheduler mergeScheduler)
        {
            if (mergeScheduler == null)
            {
                throw new System.ArgumentException("mergeScheduler must not be null");
            }
            this.mergeScheduler = mergeScheduler;
            return this;
        }

        public override MergeScheduler MergeScheduler
        {
            get
            {
                return mergeScheduler;
            }
        }

        /// <summary>
        /// Sets the maximum time to wait for a write lock (in milliseconds) for this
        /// instance. You can change the default value for all instances by calling
        /// <seealso cref="#setDefaultWriteLockTimeout(long)"/>.
        ///
        /// <p>Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetWriteLockTimeout(long writeLockTimeout)
        {
            this.writeLockTimeout = writeLockTimeout;
            return this;
        }

        public override long WriteLockTimeout
        {
            get
            {
                return writeLockTimeout;
            }
        }

        /// <summary>
        /// Expert: <seealso cref="MergePolicy"/> is invoked whenever there are changes to the
        /// segments in the index. Its role is to select which merges to do, if any,
        /// and return a <seealso cref="MergePolicy.MergeSpecification"/> describing the merges.
        /// It also selects merges to do for forceMerge.
        ///
        /// <p>Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetMergePolicy(MergePolicy mergePolicy)
        {
            if (mergePolicy == null)
            {
                throw new System.ArgumentException("mergePolicy must not be null");
            }
            this.mergePolicy = mergePolicy;
            return this;
        }

        /// <summary>
        /// Set the <seealso cref="Codec"/>.
        ///
        /// <p>
        /// Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetCodec(Codec codec)
        {
            if (codec == null)
            {
                throw new System.ArgumentException("codec must not be null");
            }
            this.codec = codec;
            return this;
        }

        public override Codec Codec
        {
            get
            {
                return codec;
            }
        }

        public override MergePolicy MergePolicy
        {
            get
            {
                return mergePolicy;
            }
        }

        /// <summary>
        /// Expert: Sets the <seealso cref="DocumentsWriterPerThreadPool"/> instance used by the
        /// IndexWriter to assign thread-states to incoming indexing threads. If no
        /// <seealso cref="DocumentsWriterPerThreadPool"/> is set <seealso cref="IndexWriter"/> will use
        /// <seealso cref="ThreadAffinityDocumentsWriterThreadPool"/> with max number of
        /// thread-states set to <seealso cref="#DEFAULT_MAX_THREAD_STATES"/> (see
        /// <seealso cref="#DEFAULT_MAX_THREAD_STATES"/>).
        /// </p>
        /// <p>
        /// NOTE: The given <seealso cref="DocumentsWriterPerThreadPool"/> instance must not be used with
        /// other <seealso cref="IndexWriter"/> instances once it has been initialized / associated with an
        /// <seealso cref="IndexWriter"/>.
        /// </p>
        /// <p>
        /// NOTE: this only takes effect when IndexWriter is first created.</p>
        /// </summary>
        public IndexWriterConfig SetIndexerThreadPool(DocumentsWriterPerThreadPool threadPool)
        {
            if (threadPool == null)
            {
                throw new System.ArgumentException("threadPool must not be null");
            }
            this.indexerThreadPool = threadPool;
            return this;
        }

        public override DocumentsWriterPerThreadPool IndexerThreadPool
        {
            get
            {
                return indexerThreadPool;
            }
        }

        /// <summary>
        /// Sets the max number of simultaneous threads that may be indexing documents
        /// at once in IndexWriter. Values &lt; 1 are invalid and if passed
        /// <code>maxThreadStates</code> will be set to
        /// <seealso cref="#DEFAULT_MAX_THREAD_STATES"/>.
        ///
        /// <p>Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetMaxThreadStates(int maxThreadStates)
        {
            this.indexerThreadPool = new ThreadAffinityDocumentsWriterThreadPool(maxThreadStates);
            return this;
        }

        public override int MaxThreadStates
        {
            get
            {
                try
                {
                    return ((ThreadAffinityDocumentsWriterThreadPool)indexerThreadPool).MaxThreadStates;
                }
                catch (System.InvalidCastException cce)
                {
                    throw new InvalidOperationException(cce.Message, cce);
                }
            }
        }

        /// <summary>
        /// By default, IndexWriter does not pool the
        ///  SegmentReaders it must open for deletions and
        ///  merging, unless a near-real-time reader has been
        ///  obtained by calling <seealso cref="DirectoryReader#open(IndexWriter, boolean)"/>.
        ///  this method lets you enable pooling without getting a
        ///  near-real-time reader.  NOTE: if you set this to
        ///  false, IndexWriter will still pool readers once
        ///  <seealso cref="DirectoryReader#open(IndexWriter, boolean)"/> is called.
        ///
        /// <p>Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetReaderPooling(bool readerPooling)
        {
            this.readerPooling = readerPooling;
            return this;
        }

        public override bool ReaderPooling
        {
            get
            {
                return readerPooling;
            }
        }

        /// <summary>
        /// Expert: sets the <seealso cref="DocConsumer"/> chain to be used to process documents.
        ///
        /// <p>Only takes effect when IndexWriter is first created.
        /// </summary>
        public IndexWriterConfig SetIndexingChain(IndexingChain indexingChain)
        {
            if (indexingChain == null)
            {
                throw new System.ArgumentException("indexingChain must not be null");
            }
            this.indexingChain = indexingChain;
            return this;
        }

        public override IndexingChain IndexingChain
        {
            get
            {
                return indexingChain;
            }
        }

        /// <summary>
        /// Expert: Controls when segments are flushed to disk during indexing.
        /// The <seealso cref="FlushPolicy"/> initialized during <seealso cref="IndexWriter"/> instantiation and once initialized
        /// the given instance is bound to this <seealso cref="IndexWriter"/> and should not be used with another writer. </summary>
        /// <seealso cref= #setMaxBufferedDeleteTerms(int) </seealso>
        /// <seealso cref= #setMaxBufferedDocs(int) </seealso>
        /// <seealso cref= #setRAMBufferSizeMB(double) </seealso>
        public IndexWriterConfig SetFlushPolicy(FlushPolicy flushPolicy)
        {
            if (flushPolicy == null)
            {
                throw new System.ArgumentException("flushPolicy must not be null");
            }
            this.flushPolicy = flushPolicy;
            return this;
        }

        /// <summary>
        /// Expert: Sets the maximum memory consumption per thread triggering a forced
        /// flush if exceeded. A <seealso cref="DocumentsWriterPerThread"/> is forcefully flushed
        /// once it exceeds this limit even if the <seealso cref="#getRAMBufferSizeMB()"/> has
        /// not been exceeded. this is a safety limit to prevent a
        /// <seealso cref="DocumentsWriterPerThread"/> from address space exhaustion due to its
        /// internal 32 bit signed integer based memory addressing.
        /// The given value must be less that 2GB (2048MB)
        /// </summary>
        /// <seealso cref= #DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB </seealso>
        public IndexWriterConfig SetRAMPerThreadHardLimitMB(int perThreadHardLimitMB)
        {
            if (perThreadHardLimitMB <= 0 || perThreadHardLimitMB >= 2048)
            {
                throw new System.ArgumentException("PerThreadHardLimit must be greater than 0 and less than 2048MB");
            }
            this.PerThreadHardLimitMB = perThreadHardLimitMB;
            return this;
        }

        public override int RAMPerThreadHardLimitMB
        {
            get
            {
                return PerThreadHardLimitMB;
            }
        }

        public override FlushPolicy FlushPolicy
        {
            get
            {
                return flushPolicy;
            }
        }

        public override InfoStream InfoStream
        {
            get
            {
                return infoStream;
            }
        }

        public override Analyzer Analyzer
        {
            get
            {
                return base.Analyzer;
            }
        }

        public override int MaxBufferedDeleteTerms
        {
            get
            {
                return base.MaxBufferedDeleteTerms;
            }
        }

        public override int MaxBufferedDocs
        {
            get
            {
                return base.MaxBufferedDocs;
            }
        }

        public override IndexReaderWarmer MergedSegmentWarmer
        {
            get
            {
                return base.MergedSegmentWarmer;
            }
        }

        public override double RAMBufferSizeMB
        {
            get
            {
                return base.RAMBufferSizeMB;
            }
        }

        public override int ReaderTermsIndexDivisor
        {
            get
            {
                return base.ReaderTermsIndexDivisor;
            }
        }

        public override int TermIndexInterval
        {
            get
            {
                return base.TermIndexInterval;
            }
        }

        /// <summary>
        /// Information about merges, deletes and a
        /// message when maxFieldLength is reached will be printed
        /// to this. Must not be null, but <seealso cref="InfoStream#NO_OUTPUT"/>
        /// may be used to supress output.
        /// </summary>
        public IndexWriterConfig SetInfoStream(InfoStream infoStream)
        {
            if (infoStream == null)
            {
                throw new System.ArgumentException("Cannot set InfoStream implementation to null. " + "To disable logging use InfoStream.NO_OUTPUT");
            }
            this.infoStream = infoStream;
            return this;
        }

        /// <summary>
        /// Convenience method that uses <seealso cref="PrintStreamInfoStream"/>.  Must not be null.
        /// </summary>
        public IndexWriterConfig SetInfoStream(TextWriter printStream)
        {
            if (printStream == null)
            {
                throw new System.ArgumentException("printStream must not be null");
            }
            return SetInfoStream(new PrintStreamInfoStream(printStream));
        }

        public IndexWriterConfig SetMaxBufferedDeleteTerms(int maxBufferedDeleteTerms)
        {
            return (IndexWriterConfig)base.SetMaxBufferedDeleteTerms(maxBufferedDeleteTerms);
        }

        public IndexWriterConfig SetMaxBufferedDocs(int maxBufferedDocs)
        {
            return (IndexWriterConfig)base.SetMaxBufferedDocs(maxBufferedDocs);
        }

        public IndexWriterConfig SetMergedSegmentWarmer(IndexReaderWarmer mergeSegmentWarmer)
        {
            return (IndexWriterConfig)base.SetMergedSegmentWarmer(mergeSegmentWarmer);
        }

        public IndexWriterConfig SetRAMBufferSizeMB(double ramBufferSizeMB)
        {
            return (IndexWriterConfig)base.SetRAMBufferSizeMB(ramBufferSizeMB);
        }

        public IndexWriterConfig SetReaderTermsIndexDivisor(int divisor)
        {
            return (IndexWriterConfig)base.SetReaderTermsIndexDivisor(divisor);
        }

        public IndexWriterConfig SetTermIndexInterval(int interval)
        {
            return (IndexWriterConfig)base.SetTermIndexInterval(interval);
        }

        public IndexWriterConfig SetUseCompoundFile(bool useCompoundFile)
        {
            return (IndexWriterConfig)base.SetUseCompoundFile(useCompoundFile);
        }

        public IndexWriterConfig SetCheckIntegrityAtMerge(bool checkIntegrityAtMerge)
        {
            return (IndexWriterConfig)base.SetCheckIntegrityAtMerge(checkIntegrityAtMerge);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.Append("writer=").Append(Writer).Append("\n");
            return sb.ToString();
        }
    }
}