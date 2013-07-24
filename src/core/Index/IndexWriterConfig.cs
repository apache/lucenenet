using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using IndexingChain = Lucene.Net.Index.DocumentsWriterPerThread.IndexingChain;
using IndexReaderWarmer = Lucene.Net.Index.IndexWriter.IndexReaderWarmer;

namespace Lucene.Net.Index
{
    public sealed class IndexWriterConfig : LiveIndexWriterConfig, ICloneable
    {
        public enum OpenMode
        {
            CREATE,
            APPEND,
            CREATE_OR_APPEND
        }

        public const int DEFAULT_TERM_INDEX_INTERVAL = 32;

        public const int DISABLE_AUTO_FLUSH = -1;

        public const int DEFAULT_MAX_BUFFERED_DELETE_TERMS = DISABLE_AUTO_FLUSH;

        public const int DEFAULT_MAX_BUFFERED_DOCS = DISABLE_AUTO_FLUSH;

        public const double DEFAULT_RAM_BUFFER_SIZE_MB = 16.0;

        public static long WRITE_LOCK_TIMEOUT = 1000;

        public static readonly bool DEFAULT_READER_POOLING = false;

        public const int DEFAULT_READER_TERMS_INDEX_DIVISOR = DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR;

        public const int DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB = 1945;

        public const int DEFAULT_MAX_THREAD_STATES = 8;

        public static long DefaultWriteLockTimeout
        {
            get { return WRITE_LOCK_TIMEOUT; }
            set { WRITE_LOCK_TIMEOUT = value; }
        }

        public IndexWriterConfig(Lucene.Net.Util.Version matchVersion, Analyzer analyzer)
            : base(analyzer, matchVersion)
        {
        }

        public object Clone()
        {
            try
            {
                IndexWriterConfig clone = (IndexWriterConfig)this.MemberwiseClone();

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

        public IndexWriterConfig SetOpenMode(OpenMode openMode)
        {
            if (openMode == null)
            {
                throw new ArgumentException("openMode must not be null");
            }
            this.openMode = openMode;
            return this;
        }

        public override OpenMode OpenModeValue
        {
            get
            {
                return openMode;
            }
        }

        public IndexWriterConfig SetIndexDeletionPolicy(IndexDeletionPolicy delPolicy)
        {
            if (delPolicy == null)
            {
                throw new ArgumentException("indexDeletionPolicy must not be null");
            }
            this.delPolicy = delPolicy;
            return this;
        }

        public override IndexDeletionPolicy IndexDeletionPolicy
        {
            get
            {
                return delPolicy;
            }
        }

        public IndexWriterConfig SetIndexCommit(IndexCommit commit)
        {
            this.commit = commit;
            return this;
        }

        public override IndexCommit IndexCommit
        {
            get
            {
                return commit;
            }
        }

        public IndexWriterConfig SetSimilarity(Similarity similarity)
        {
            if (similarity == null)
            {
                throw new ArgumentException("similarity must not be null");
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

        public IndexWriterConfig SetMergeScheduler(MergeScheduler mergeScheduler)
        {
            if (mergeScheduler == null)
            {
                throw new ArgumentException("mergeScheduler must not be null");
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

        public IndexWriterConfig SetWriteLockTimeout(long writeLockTimeout)
        {
            Interlocked.Exchange(ref this.writeLockTimeout, writeLockTimeout);
            return this;
        }

        public override long WriteLockTimeout
        {
            get
            {
                return Interlocked.Read(ref writeLockTimeout);
            }
        }

        public IndexWriterConfig SetMergePolicy(MergePolicy mergePolicy)
        {
            if (mergePolicy == null)
            {
                throw new ArgumentException("mergePolicy must not be null");
            }
            this.mergePolicy = mergePolicy;
            return this;
        }

        public IndexWriterConfig SetCodec(Codec codec)
        {
            if (codec == null)
            {
                throw new ArgumentException("codec must not be null");
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

        internal IndexWriterConfig SetIndexerThreadPool(DocumentsWriterPerThreadPool threadPool)
        {
            if (threadPool == null)
            {
                throw new ArgumentException("threadPool must not be null");
            }
            this.indexerThreadPool = threadPool;
            return this;
        }

        internal override DocumentsWriterPerThreadPool IndexerThreadPool
        {
            get
            {
                return indexerThreadPool;
            }
        }

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
                catch (InvalidCastException cce)
                {
                    throw new InvalidOperationException(cce.Message);
                }
            }
        }

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

        internal IndexWriterConfig SetIndexingChain(IndexingChain indexingChain)
        {
            if (indexingChain == null)
            {
                throw new ArgumentException("indexingChain must not be null");
            }
            this.indexingChain = indexingChain;
            return this;
        }

        internal override IndexingChain IndexingChain
        {
            get
            {
                return indexingChain;
            }
        }

        internal IndexWriterConfig SetFlushPolicy(FlushPolicy flushPolicy)
        {
            if (flushPolicy == null)
            {
                throw new ArgumentException("flushPolicy must not be null");
            }
            this.flushPolicy = flushPolicy;
            return this;
        }

        public IndexWriterConfig SetRAMPerThreadHardLimitMB(int perThreadHardLimitMB)
        {
            if (perThreadHardLimitMB <= 0 || perThreadHardLimitMB >= 2048)
            {
                throw new ArgumentException("PerThreadHardLimit must be greater than 0 and less than 2048MB");
            }
            this.perThreadHardLimitMB = perThreadHardLimitMB;
            return this;
        }

        public override int RAMPerThreadHardLimitMB
        {
            get
            {
                return perThreadHardLimitMB;
            }
        }

        internal override FlushPolicy FlushPolicy
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

        public IndexWriterConfig SetInfoStream(InfoStream infoStream)
        {
            if (infoStream == null)
            {
                throw new ArgumentException("Cannot set InfoStream implementation to null. " +
                  "To disable logging use InfoStream.NO_OUTPUT");
            }
            this.infoStream = infoStream;
            return this;
        }

        public IndexWriterConfig SetInfoStream(System.IO.TextWriter printStream)
        {
            if (printStream == null)
            {
                throw new ArgumentException("printStream must not be null");
            }
            return SetInfoStream(new PrintStreamInfoStream(printStream));
        }

        public override LiveIndexWriterConfig SetMaxBufferedDeleteTerms(int maxBufferedDeleteTerms)
        {
            return (IndexWriterConfig)base.SetMaxBufferedDeleteTerms(maxBufferedDeleteTerms);
        }

        public override LiveIndexWriterConfig SetMaxBufferedDocs(int maxBufferedDocs)
        {
            return (IndexWriterConfig)base.SetMaxBufferedDocs(maxBufferedDocs);
        }

        public override LiveIndexWriterConfig SetMergedSegmentWarmer(IndexReaderWarmer mergeSegmentWarmer)
        {
            return (IndexWriterConfig)base.SetMergedSegmentWarmer(mergeSegmentWarmer);
        }

        public override LiveIndexWriterConfig SetRAMBufferSizeMB(double ramBufferSizeMB)
        {
            return (IndexWriterConfig)base.SetRAMBufferSizeMB(ramBufferSizeMB);
        }

        public override LiveIndexWriterConfig SetReaderTermsIndexDivisor(int divisor)
        {
            return (IndexWriterConfig)base.SetReaderTermsIndexDivisor(divisor);
        }

        public override LiveIndexWriterConfig SetTermIndexInterval(int interval)
        {
            return (IndexWriterConfig)base.SetTermIndexInterval(interval);
        }
    }
}
