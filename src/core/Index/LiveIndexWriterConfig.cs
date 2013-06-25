using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IndexingChain = Lucene.Net.Index.DocumentsWriterPerThread.IndexingChain;
using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;

namespace Lucene.Net.Index
{
    public class LiveIndexWriterConfig
    {
        private readonly Analyzer analyzer;

        private volatile int maxBufferedDocs;
        private volatile double ramBufferSizeMB;
        private volatile int maxBufferedDeleteTerms;
        private volatile int readerTermsIndexDivisor;
        private volatile IndexReaderWarmer mergedSegmentWarmer;
        private volatile int termIndexInterval; // TODO: this should be private to the codec, not settable here

        protected volatile IndexDeletionPolicy delPolicy;

        protected volatile IndexCommit commit;

        protected volatile OpenMode openMode;

        protected volatile Similarity similarity;

        protected volatile MergeScheduler mergeScheduler;

        protected volatile long writeLockTimeout;

        protected volatile IndexingChain indexingChain;

        protected volatile Codec codec;

        protected volatile InfoStream infoStream;

        protected volatile MergePolicy mergePolicy;

        protected volatile DocumentsWriterPerThreadPool indexerThreadPool;

        protected volatile bool readerPooling;

        protected volatile FlushPolicy flushPolicy;

        protected volatile int perThreadHardLimitMB;

        protected readonly Lucene.Net.Util.Version matchVersion;

        public LiveIndexWriterConfig(Analyzer analyzer, Lucene.Net.Util.Version matchVersion)
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
            openMode = OpenMode.CREATE_OR_APPEND;
            similarity = IndexSearcher.DefaultSimilarity;
            mergeScheduler = new ConcurrentMergeScheduler();
            writeLockTimeout = IndexWriterConfig.WRITE_LOCK_TIMEOUT;
            indexingChain = DocumentsWriterPerThread.defaultIndexingChain;
            codec = Codec.Default;
            if (codec == null)
            {
                throw new NullReferenceException();
            }
            infoStream = InfoStream.Default;
            mergePolicy = new TieredMergePolicy();
            flushPolicy = new FlushByRamOrCountsPolicy();
            readerPooling = IndexWriterConfig.DEFAULT_READER_POOLING;
            indexerThreadPool = new ThreadAffinityDocumentsWriterThreadPool(IndexWriterConfig.DEFAULT_MAX_THREAD_STATES);
            perThreadHardLimitMB = IndexWriterConfig.DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB;
        }

        public LiveIndexWriterConfig(IndexWriterConfig config)
        {
            maxBufferedDeleteTerms = config.getMaxBufferedDeleteTerms();
            maxBufferedDocs = config.getMaxBufferedDocs();
            mergedSegmentWarmer = config.getMergedSegmentWarmer();
            ramBufferSizeMB = config.getRAMBufferSizeMB();
            readerTermsIndexDivisor = config.getReaderTermsIndexDivisor();
            termIndexInterval = config.getTermIndexInterval();
            matchVersion = config.matchVersion;
            analyzer = config.getAnalyzer();
            delPolicy = config.getIndexDeletionPolicy();
            commit = config.getIndexCommit();
            openMode = config.getOpenMode();
            similarity = config.getSimilarity();
            mergeScheduler = config.getMergeScheduler();
            writeLockTimeout = config.getWriteLockTimeout();
            indexingChain = config.getIndexingChain();
            codec = config.getCodec();
            infoStream = config.getInfoStream();
            mergePolicy = config.getMergePolicy();
            indexerThreadPool = config.getIndexerThreadPool();
            readerPooling = config.getReaderPooling();
            flushPolicy = config.getFlushPolicy();
            perThreadHardLimitMB = config.getRAMPerThreadHardLimitMB();
        }

        public virtual Analyzer Analyzer
        {
            get { return analyzer; }
        }

        public virtual LiveIndexWriterConfig SetTermIndexInterval(int interval)
        { 
            // TODO: this should be private to the codec, not settable here
            this.termIndexInterval = interval;
            return this;
        }

        public virtual int TermIndexInterval
        {
            get
            {
                // TODO: this should be private to the codec, not settable here
                return termIndexInterval;
            }
        }

        public virtual LiveIndexWriterConfig SetMaxBufferedDeleteTerms(int maxBufferedDeleteTerms)
        {
            if (maxBufferedDeleteTerms != IndexWriterConfig.DISABLE_AUTO_FLUSH && maxBufferedDeleteTerms < 1)
            {
                throw new ArgumentException("maxBufferedDeleteTerms must at least be 1 when enabled");
            }
            this.maxBufferedDeleteTerms = maxBufferedDeleteTerms;
            return this;
        }

        public virtual int MaxBufferedDeleteTerms
        {
            get
            {
                return maxBufferedDeleteTerms;
            }
        }

        public virtual LiveIndexWriterConfig SetRAMBufferSizeMB(double ramBufferSizeMB)
        {
            if (ramBufferSizeMB != IndexWriterConfig.DISABLE_AUTO_FLUSH && ramBufferSizeMB <= 0.0)
            {
                throw new ArgumentException("ramBufferSize should be > 0.0 MB when enabled");
            }
            if (ramBufferSizeMB == IndexWriterConfig.DISABLE_AUTO_FLUSH
                && maxBufferedDocs == IndexWriterConfig.DISABLE_AUTO_FLUSH)
            {
                throw new ArgumentException("at least one of ramBufferSize and maxBufferedDocs must be enabled");
            }
            this.ramBufferSizeMB = ramBufferSizeMB;
            return this;
        }

        public virtual double RAMBufferSizeMB
        {
            get { return ramBufferSizeMB; }
        }

        public virtual LiveIndexWriterConfig SetMaxBufferedDocs(int maxBufferedDocs)
        {
            if (maxBufferedDocs != IndexWriterConfig.DISABLE_AUTO_FLUSH && maxBufferedDocs < 2)
            {
                throw new ArgumentException("maxBufferedDocs must at least be 2 when enabled");
            }
            if (maxBufferedDocs == IndexWriterConfig.DISABLE_AUTO_FLUSH
                && ramBufferSizeMB == IndexWriterConfig.DISABLE_AUTO_FLUSH)
            {
                throw new ArgumentException("at least one of ramBufferSize and maxBufferedDocs must be enabled");
            }
            this.maxBufferedDocs = maxBufferedDocs;
            return this;
        }

        public virtual int MaxBufferedDocs
        {
            get { return maxBufferedDocs; }
        }

        public virtual LiveIndexWriterConfig SetMergedSegmentWarmer(IndexReaderWarmer mergeSegmentWarmer)
        {
            this.mergedSegmentWarmer = mergeSegmentWarmer;
            return this;
        }

        public virtual IndexReaderWarmer MergedSegmentWarmer
        {
            get { return mergedSegmentWarmer; }
        }

        public virtual LiveIndexWriterConfig SetReaderTermsIndexDivisor(int divisor)
        {
            if (divisor <= 0 && divisor != -1)
            {
                throw new ArgumentException("divisor must be >= 1, or -1 (got " + divisor + ")");
            }
            readerTermsIndexDivisor = divisor;
            return this;
        }

        public virtual int ReaderTermsIndexDivisor
        {
            get { return readerTermsIndexDivisor; }
        }

        public virtual OpenMode OpenMode
        {
            get { return openMode; }
        }

        public virtual IndexDeletionPolicy IndexDeletionPolicy
        {
            get { return delPolicy; }
        }

        public virtual IndexCommit IndexCommit
        {
            get { return commit; }
        }

        public virtual Similarity Similarity
        {
            get { return similarity; }
        }

        public virtual MergeScheduler MergeScheduler
        {
            get { return mergeScheduler; }
        }

        public virtual long WriteLockTimeout
        {
            get { return writeLockTimeout; }
        }

        public virtual Codec Codec
        {
            get { return codec; }
        }

        public virtual MergePolicy MergePolicy
        {
            get { return mergePolicy; }
        }

        internal virtual DocumentsWriterPerThreadPool IndexerThreadPool
        {
            get { return indexerThreadPool; }
        }

        public virtual int MaxThreadStates
        {
            get
            {
                try
                {
                    return ((ThreadAffinityDocumentsWriterThreadPool)indexerThreadPool).MaxThreadStates;
                }
                catch (InvalidCastException cce)
                {
                    throw new InvalidOperationException(cce.Message);
                }
            }
        }

        public virtual bool ReaderPooling
        {
            get { return readerPooling; }
        }

        internal virtual IndexingChain IndexingChain
        {
            get { return indexingChain; }
        }

        public virtual int RAMPerThreadHardLimitMB
        {
            get { return perThreadHardLimitMB; }
        }

        internal virtual FlushPolicy FlushPolicy
        {
            get { return flushPolicy; }
        }

        public virtual InfoStream InfoStream
        {
            get { return infoStream; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("matchVersion=").Append(matchVersion).Append("\n");
            sb.Append("analyzer=").Append(analyzer == null ? "null" : analyzer.GetType().Name).Append("\n");
            sb.Append("ramBufferSizeMB=").Append(RAMBufferSizeMB).Append("\n");
            sb.Append("maxBufferedDocs=").Append(MaxBufferedDocs).Append("\n");
            sb.Append("maxBufferedDeleteTerms=").Append(MaxBufferedDeleteTerms).Append("\n");
            sb.Append("mergedSegmentWarmer=").Append(MergedSegmentWarmer).Append("\n");
            sb.Append("readerTermsIndexDivisor=").Append(ReaderTermsIndexDivisor).Append("\n");
            sb.Append("termIndexInterval=").Append(TermIndexInterval).Append("\n"); // TODO: this should be private to the codec, not settable here
            sb.Append("delPolicy=").Append(IndexDeletionPolicy.GetType().Name).Append("\n");
            IndexCommit commit = IndexCommit;
            sb.Append("commit=").Append(commit == null ? "null" : commit.ToString()).Append("\n");
            sb.Append("openMode=").Append(OpenMode).Append("\n");
            sb.Append("similarity=").Append(Similarity.GetType().Name).Append("\n");
            sb.Append("mergeScheduler=").Append(MergeScheduler).Append("\n");
            sb.Append("default WRITE_LOCK_TIMEOUT=").Append(IndexWriterConfig.WRITE_LOCK_TIMEOUT).Append("\n");
            sb.Append("writeLockTimeout=").Append(WriteLockTimeout).Append("\n");
            sb.Append("codec=").Append(Codec).Append("\n");
            sb.Append("infoStream=").Append(InfoStream.GetType().Name).Append("\n");
            sb.Append("mergePolicy=").Append(MergePolicy).Append("\n");
            sb.Append("indexerThreadPool=").Append(IndexerThreadPool).Append("\n");
            sb.Append("readerPooling=").Append(ReaderPooling).Append("\n");
            sb.Append("perThreadHardLimitMB=").Append(RAMPerThreadHardLimitMB).Append("\n");
            return sb.ToString();
        }
    }
}
