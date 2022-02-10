using Lucene.Net.Util;
using System;
using System.IO;
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
    using InfoStream = Lucene.Net.Util.InfoStream;
    using TextWriterInfoStream = Lucene.Net.Util.TextWriterInfoStream;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;

    /// <summary>
    /// Holds all the configuration that is used to create an <see cref="IndexWriter"/>.
    /// Once <see cref="IndexWriter"/> has been created with this object, changes to this
    /// object will not affect the <see cref="IndexWriter"/> instance. For that, use
    /// <see cref="LiveIndexWriterConfig"/> that is returned from <see cref="IndexWriter.Config"/>.
    ///
    /// <para/>
    /// LUCENENET NOTE: Unlike Lucene, we use property setters instead of setter methods.
    /// In C#, this allows you to initialize the <see cref="IndexWriterConfig"/>
    /// using the language features of C#, for example:
    /// <code>
    ///     IndexWriterConfig conf = new IndexWriterConfig(analyzer)
    ///     {
    ///         Codec = Lucene46Codec(),
    ///         OpenMode = OpenMode.CREATE
    ///     };
    /// </code>
    /// 
    /// However, if you prefer to match the syntax of Lucene using chained setter methods, 
    /// there are extension methods in the Lucene.Net.Index.Extensions namespace. Example usage:
    /// <code>
    ///     using Lucene.Net.Index.Extensions;
    ///     
    ///     ..
    ///     
    ///     IndexWriterConfig conf = new IndexWriterConfig(analyzer)
    ///         .SetCodec(new Lucene46Codec())
    ///         .SetOpenMode(OpenMode.CREATE);
    /// </code>
    /// 
    /// @since 3.1
    /// </summary>
    /// <seealso cref="IndexWriter.Config"/>
    public sealed class IndexWriterConfig : LiveIndexWriterConfig // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        // LUCENENET specific: De-nested OpenMode enum from this class to prevent naming conflict

        /// <summary>
        /// Default value is 32. Change using <see cref="LiveIndexWriterConfig.TermIndexInterval"/> setter. </summary>
        public static readonly int DEFAULT_TERM_INDEX_INTERVAL = 32; // TODO: this should be private to the codec, not settable here

        /// <summary>
        /// Denotes a flush trigger is disabled. </summary>
        public static readonly int DISABLE_AUTO_FLUSH = -1;

        /// <summary>
        /// Disabled by default (because IndexWriter flushes by RAM usage by default). </summary>
        public static readonly int DEFAULT_MAX_BUFFERED_DELETE_TERMS = DISABLE_AUTO_FLUSH;

        /// <summary>
        /// Disabled by default (because IndexWriter flushes by RAM usage by default). </summary>
        public static readonly int DEFAULT_MAX_BUFFERED_DOCS = DISABLE_AUTO_FLUSH;

        /// <summary>
        /// Default value is 16 MB (which means flush when buffered docs consume
        /// approximately 16 MB RAM).
        /// </summary>
        public static readonly double DEFAULT_RAM_BUFFER_SIZE_MB = 16.0;

        /// <summary>
        /// Default value for the write lock timeout (1,000 ms).
        /// </summary>
        /// <see cref="DefaultWriteLockTimeout"/>
        public static long WRITE_LOCK_TIMEOUT = 1000;

        /// <summary>
        /// Default setting for <see cref="UseReaderPooling"/>. </summary>
        public static readonly bool DEFAULT_READER_POOLING = false;

        /// <summary>
        /// Default value is 1. Change using <see cref="LiveIndexWriterConfig.ReaderTermsIndexDivisor"/> setter. </summary>
        public static readonly int DEFAULT_READER_TERMS_INDEX_DIVISOR = DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR;

        /// <summary>
        /// Default value is 1945. Change using <see cref="RAMPerThreadHardLimitMB"/> setter. </summary>
        public static readonly int DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB = 1945;

        /// <summary>
        /// The maximum number of simultaneous threads that may be
        /// indexing documents at once in <see cref="IndexWriter"/>; if more
        /// than this many threads arrive they will wait for
        /// others to finish. Default value is 8.
        /// </summary>
        public static readonly int DEFAULT_MAX_THREAD_STATES = 8;

        /// <summary>
        /// Default value for compound file system for newly written segments
        /// (set to <c>true</c>). For batch indexing with very large
        /// ram buffers use <c>false</c>
        /// </summary>
        public static readonly bool DEFAULT_USE_COMPOUND_FILE_SYSTEM = true;

        /// <summary>
        /// Default value for calling <see cref="AtomicReader.CheckIntegrity()"/> before
        /// merging segments (set to <c>false</c>). You can set this
        /// to <c>true</c> for additional safety.
        /// </summary>
        public static readonly bool DEFAULT_CHECK_INTEGRITY_AT_MERGE = false;

        /// <summary>
        /// Gets or sets the default (for any instance) maximum time to wait for a write lock
        /// (in milliseconds).
        /// </summary>
        public static long DefaultWriteLockTimeout
        {
            get => WRITE_LOCK_TIMEOUT;
            set => WRITE_LOCK_TIMEOUT = value;
        }

        // indicates whether this config instance is already attached to a writer.
        // not final so that it can be cloned properly.
        private SetOnce<IndexWriter> writer = new SetOnce<IndexWriter>();

        /// <summary>
        /// Gets or sets the <see cref="IndexWriter"/> this config is attached to.
        /// </summary>
        /// <exception cref="Util.AlreadySetException">
        ///           if this config is already attached to a writer. </exception>
        internal IndexWriterConfig SetIndexWriter(IndexWriter writer)
        {
            this.writer.Set(writer);
            return this;
        }

        /// <summary>
        /// Creates a new config that with defaults that match the specified
        /// <see cref="LuceneVersion"/> as well as the default 
        /// <see cref="Analyzer"/>. If <paramref name="matchVersion"/> is &gt;= 
        /// <see cref="LuceneVersion.LUCENE_32"/>, <see cref="TieredMergePolicy"/> is used
        /// for merging; else <see cref="LogByteSizeMergePolicy"/>.
        /// Note that <see cref="TieredMergePolicy"/> is free to select
        /// non-contiguous merges, which means docIDs may not
        /// remain monotonic over time.  If this is a problem you
        /// should switch to <see cref="LogByteSizeMergePolicy"/> or
        /// <see cref="LogDocMergePolicy"/>.
        /// </summary>
        public IndexWriterConfig(LuceneVersion matchVersion, Analyzer analyzer)
            : base(analyzer, matchVersion)
        {
        }

        public object Clone()
        {
            IndexWriterConfig clone = (IndexWriterConfig)this.MemberwiseClone();

            clone.writer = (SetOnce<IndexWriter>)writer.Clone();

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
            clone.mergeScheduler = (IMergeScheduler)mergeScheduler.Clone();

            return clone;

            // LUCENENET specific - no need to deal with checked exceptions here
        }

        /// <summary>
        /// Specifies <see cref="Index.OpenMode"/> of the index.
        ///
        /// <para/>Only takes effect when <see cref="IndexWriter"/> is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public OpenMode OpenMode
        {
            get => openMode;
            set =>
                // LUCENENET specific - making non-nullable, so we don't need to worry about this.
                //if (value is null)
                //{
                //    throw new ArgumentException("openMode must not be null");
                //}
                this.openMode = value;
        }

        /// <summary>
        /// Expert: allows an optional <see cref="Index.IndexDeletionPolicy"/> implementation to be
        /// specified. You can use this to control when prior commits are deleted from
        /// the index. The default policy is <see cref="KeepOnlyLastCommitDeletionPolicy"/>
        /// which removes all prior commits as soon as a new commit is done (this
        /// matches behavior before 2.2). Creating your own policy can allow you to
        /// explicitly keep previous "point in time" commits alive in the index for
        /// some time, to allow readers to refresh to the new commit without having the
        /// old commit deleted out from under them. This is necessary on filesystems
        /// like NFS that do not support "delete on last close" semantics, which
        /// Lucene's "point in time" search normally relies on.
        /// <para/>
        /// <b>NOTE:</b> the deletion policy cannot be <c>null</c>.
        ///
        /// <para/>Only takes effect when IndexWriter is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public IndexDeletionPolicy IndexDeletionPolicy
        {
            get => delPolicy;
            set => delPolicy = value ?? throw new ArgumentNullException(nameof(IndexDeletionPolicy), "IndexDeletionPolicy must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Expert: allows to open a certain commit point. The default is <c>null</c> which
        /// opens the latest commit point.
        ///
        /// <para/>Only takes effect when <see cref="IndexWriter"/> is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public IndexCommit IndexCommit
        {
            get => commit;
            set => this.commit = value;
        }

        /// <summary>
        /// Expert: set the <see cref="Search.Similarities.Similarity"/> implementation used by this <see cref="IndexWriter"/>.
        /// <para/>
        /// <b>NOTE:</b> the similarity cannot be <c>null</c>.
        ///
        /// <para/>Only takes effect when <see cref="IndexWriter"/> is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public Similarity Similarity
        {
            get => similarity;
            set => similarity = value ?? throw new ArgumentNullException(nameof(Similarity), "Similarity must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Expert: Gets or sets the merge scheduler used by this writer. The default is
        /// <see cref="ConcurrentMergeScheduler"/>.
        /// <para/>
        /// <b>NOTE:</b> the merge scheduler cannot be <c>null</c>.
        ///
        /// <para/>Only takes effect when <see cref="IndexWriter"/> is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public IMergeScheduler MergeScheduler
        {
            get => mergeScheduler;
            set => mergeScheduler = value ?? throw new ArgumentNullException(nameof(MergeScheduler), "MergeScheduler must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Gets or sets the maximum time to wait for a write lock (in milliseconds) for this
        /// instance. You can change the default value for all instances by calling the
        /// <see cref="DefaultWriteLockTimeout"/> setter.
        ///
        /// <para/>Only takes effect when <see cref="IndexWriter"/> is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public long WriteLockTimeout
        {
            get => writeLockTimeout;
            set => this.writeLockTimeout = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="Codecs.Codec"/>.
        /// <para/>
        /// Only takes effect when <see cref="IndexWriter"/> is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public Codec Codec
        {
            get => codec;
            set => codec = value ?? throw new ArgumentNullException(nameof(Codec), "Codec must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Expert: <see cref="Index.MergePolicy"/> is invoked whenever there are changes to the
        /// segments in the index. Its role is to select which merges to do, if any,
        /// and return a <see cref="MergePolicy.MergeSpecification"/> describing the merges.
        /// It also selects merges to do for <see cref="IndexWriter.ForceMerge(int)"/>.
        ///
        /// <para/>Only takes effect when <see cref="IndexWriter"/> is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public MergePolicy MergePolicy
        {
            get => mergePolicy;
            set => mergePolicy = value ?? throw new ArgumentNullException(nameof(MergePolicy), "MergePolicy must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Expert: Gets or sets the <see cref="DocumentsWriterPerThreadPool"/> instance used by the
        /// <see cref="IndexWriter"/> to assign thread-states to incoming indexing threads. If no
        /// <see cref="DocumentsWriterPerThreadPool"/> is set <see cref="IndexWriter"/> will use
        /// <see cref="DocumentsWriterPerThreadPool"/> with max number of
        /// thread-states set to <see cref="DEFAULT_MAX_THREAD_STATES"/> (see
        /// <see cref="DEFAULT_MAX_THREAD_STATES"/>).
        /// <para>
        /// NOTE: The given <see cref="DocumentsWriterPerThreadPool"/> instance must not be used with
        /// other <see cref="IndexWriter"/> instances once it has been initialized / associated with an
        /// <see cref="IndexWriter"/>.
        /// </para>
        /// <para>
        /// NOTE: this only takes effect when <see cref="IndexWriter"/> is first created.</para>
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new internal DocumentsWriterPerThreadPool IndexerThreadPool
        {
            get => indexerThreadPool;
            set => indexerThreadPool = value ?? throw new ArgumentNullException(nameof(IndexerThreadPool), "IndexerThreadPool must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Gets or sets the max number of simultaneous threads that may be indexing documents
        /// at once in <see cref="IndexWriter"/>. Values &lt; 1 are invalid and if passed
        /// <c>maxThreadStates</c> will be set to
        /// <see cref="DEFAULT_MAX_THREAD_STATES"/>.
        ///
        /// <para/>Only takes effect when <see cref="IndexWriter"/> is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public int MaxThreadStates
        {
            // LUCENENET: Changes brought over from 4.8.1 mean there is no chance of a cast failure
            get => indexerThreadPool.MaxThreadStates;
            set => this.indexerThreadPool = new DocumentsWriterPerThreadPool(value);
        }

        /// <summary>
        /// By default, <see cref="IndexWriter"/> does not pool the
        /// <see cref="SegmentReader"/>s it must open for deletions and
        /// merging, unless a near-real-time reader has been
        /// obtained by calling <see cref="DirectoryReader.Open(IndexWriter, bool)"/>.
        /// this setting lets you enable pooling without getting a
        /// near-real-time reader.  NOTE: if you set this to
        /// <c>false</c>, <see cref="IndexWriter"/> will still pool readers once
        /// <see cref="DirectoryReader.Open(IndexWriter, bool)"/> is called.
        ///
        /// <para/>Only takes effect when <see cref="IndexWriter"/> is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public bool UseReaderPooling 
        {
            get => readerPooling;
            set => this.readerPooling = value;
        }

        /// <summary>
        /// Expert: Gets or sets the <see cref="DocConsumer"/> chain to be used to process documents.
        ///
        /// <para/>Only takes effect when <see cref="IndexWriter"/> is first created.
        /// </summary>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new internal IndexingChain IndexingChain
        {
            get => indexingChain;
            set => indexingChain = value ?? throw new ArgumentNullException(nameof(IndexingChain), "IndexingChain must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        /// <summary>
        /// Expert: Gets or sets the maximum memory consumption per thread triggering a forced
        /// flush if exceeded. A <see cref="DocumentsWriterPerThread"/> is forcefully flushed
        /// once it exceeds this limit even if the <see cref="LiveIndexWriterConfig.RAMBufferSizeMB"/> has
        /// not been exceeded. This is a safety limit to prevent a
        /// <see cref="DocumentsWriterPerThread"/> from address space exhaustion due to its
        /// internal 32 bit signed integer based memory addressing.
        /// The given value must be less that 2GB (2048MB).
        /// </summary>
        /// <seealso cref="DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB"/>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new public int RAMPerThreadHardLimitMB
        {
            get => perThreadHardLimitMB;
            set
            {
                if (value <= 0 || value >= 2048)
                {
                    throw new ArgumentOutOfRangeException(nameof(RAMPerThreadHardLimitMB), "PerThreadHardLimit must be greater than 0 and less than 2048MB"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                this.perThreadHardLimitMB = value;
            }
        }

        /// <summary>
        /// Expert: Controls when segments are flushed to disk during indexing.
        /// The <see cref="Index.FlushPolicy"/> initialized during <see cref="IndexWriter"/> instantiation and once initialized
        /// the given instance is bound to this <see cref="IndexWriter"/> and should not be used with another writer.
        /// </summary>
        /// <seealso cref="LiveIndexWriterConfig.MaxBufferedDeleteTerms"/>
        /// <seealso cref="LiveIndexWriterConfig.MaxBufferedDocs"/>
        /// <seealso cref="LiveIndexWriterConfig.RAMBufferSizeMB"/>
        // LUCENENET NOTE: We cannot override a getter and add a setter, 
        // so must declare it new. See: http://stackoverflow.com/q/82437
        new internal FlushPolicy FlushPolicy
        {
            get => flushPolicy;
            set => flushPolicy = value ?? throw new ArgumentNullException(nameof(FlushPolicy), "FlushPolicy must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        // LUCENENT NOTE: The following properties would be pointless,
        // since they are already inherited by the base class.
        //public override InfoStream InfoStream
        //{
        //    get
        //    {
        //        return infoStream;
        //    }
        //}

        //public override Analyzer Analyzer
        //{
        //    get
        //    {
        //        return base.Analyzer;
        //    }
        //}

        //public override int MaxBufferedDeleteTerms
        //{
        //    get
        //    {
        //        return base.MaxBufferedDeleteTerms;
        //    }
        //}

        //public override int MaxBufferedDocs
        //{
        //    get
        //    {
        //        return base.MaxBufferedDocs;
        //    }
        //}

        //public override IndexReaderWarmer MergedSegmentWarmer
        //{
        //    get
        //    {
        //        return base.MergedSegmentWarmer;
        //    }
        //}

        //public override double RAMBufferSizeMB
        //{
        //    get
        //    {
        //        return base.RAMBufferSizeMB;
        //    }
        //}

        //public override int ReaderTermsIndexDivisor
        //{
        //    get
        //    {
        //        return base.ReaderTermsIndexDivisor;
        //    }
        //}

        //public override int TermIndexInterval
        //{
        //    get
        //    {
        //        return base.TermIndexInterval;
        //    }
        //}

        /// <summary>
        /// Information about merges, deletes and a
        /// message when maxFieldLength is reached will be printed
        /// to this. Must not be <c>null</c>, but <see cref="InfoStream.NO_OUTPUT"/>
        /// may be used to supress output.
        /// </summary>
        public IndexWriterConfig SetInfoStream(InfoStream infoStream)
        {
            this.infoStream = infoStream ?? throw new ArgumentNullException(nameof(infoStream),
                    "Cannot set InfoStream implementation to null. " + 
                    "To disable logging use InfoStream.NO_OUTPUT");
            return this;
        }

        /// <summary>
        /// Convenience method that uses <see cref="TextWriterInfoStream"/> to write to the passed in <see cref="TextWriter"/>. 
        /// Must not be <c>null</c>.
        /// </summary>
        public IndexWriterConfig SetInfoStream(TextWriter printStream)
        {
            if (printStream is null)
            {
                throw new ArgumentNullException("printStream must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            return SetInfoStream(new TextWriterInfoStream(printStream));
        }

        // LUCENENET NOTE: These were only here for casting purposes, but since we are
        // using property setters, they are not needed

        //new public IndexWriterConfig SetMaxBufferedDeleteTerms(int maxBufferedDeleteTerms)
        //{
        //    return (IndexWriterConfig)base.SetMaxBufferedDeleteTerms(maxBufferedDeleteTerms);
        //}

        //new public IndexWriterConfig SetMaxBufferedDocs(int maxBufferedDocs)
        //{
        //    return (IndexWriterConfig)base.SetMaxBufferedDocs(maxBufferedDocs);
        //}

        //new public IndexWriterConfig SetMergedSegmentWarmer(IndexReaderWarmer mergeSegmentWarmer)
        //{
        //    return (IndexWriterConfig)base.SetMergedSegmentWarmer(mergeSegmentWarmer);
        //}

        //new public IndexWriterConfig SetRAMBufferSizeMB(double ramBufferSizeMB)
        //{
        //    return (IndexWriterConfig)base.SetRAMBufferSizeMB(ramBufferSizeMB);
        //}

        //new public IndexWriterConfig SetReaderTermsIndexDivisor(int divisor)
        //{
        //    return (IndexWriterConfig)base.SetReaderTermsIndexDivisor(divisor);
        //}

        //new public IndexWriterConfig SetTermIndexInterval(int interval)
        //{
        //    return (IndexWriterConfig)base.SetTermIndexInterval(interval);
        //}

        //new public IndexWriterConfig SetUseCompoundFile(bool useCompoundFile)
        //{
        //    return (IndexWriterConfig)base.SetUseCompoundFile(useCompoundFile);
        //}

        //new public IndexWriterConfig SetCheckIntegrityAtMerge(bool checkIntegrityAtMerge)
        //{
        //    return (IndexWriterConfig)base.SetCheckIntegrityAtMerge(checkIntegrityAtMerge);
        //}

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.Append("writer=").Append(writer).Append("\n");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Specifies the open mode for <see cref="IndexWriter"/>.
    /// </summary>
    public enum OpenMode // LUCENENET specific: De-nested from IndexWriterConfig to prevent naming conflict
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
}