using Lucene.Net.Codecs;
using Lucene.Net.Search.Similarities;

namespace Lucene.Net.Index.Extensions
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

    /// <summary>
    /// Extension methods that can be used to provide similar
    /// <see cref="Index.IndexWriterConfig"/> syntax as Java Lucene.
    /// (config.SetCheckIntegrityAtMerge(100).SetMaxBufferedDocs(1000);)
    /// </summary>
    public static class IndexWriterConfigExtensions
    {
        // -- LiveIndexWriterConfig.cs

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.TermIndexInterval"/>.
        /// </summary>
        /// <param name="config">this <see cref="LiveIndexWriterConfig"/> instance</param>
        /// <param name="interval"></param>
        /// <returns>this <see cref="LiveIndexWriterConfig"/> instance</returns>
        public static LiveIndexWriterConfig SetTermIndexInterval(this LiveIndexWriterConfig config, int interval)
        {
            config.TermIndexInterval = interval;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.MaxBufferedDeleteTerms"/>.
        /// </summary>
        /// <param name="config">this <see cref="LiveIndexWriterConfig"/> instance</param>
        /// <param name="maxBufferedDeleteTerms"></param>
        /// <returns>this <see cref="LiveIndexWriterConfig"/> instance</returns>
        public static LiveIndexWriterConfig SetMaxBufferedDeleteTerms(this LiveIndexWriterConfig config, int maxBufferedDeleteTerms)
        {
            config.MaxBufferedDeleteTerms = maxBufferedDeleteTerms;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.RAMBufferSizeMB"/>.
        /// </summary>
        /// <param name="config">this <see cref="LiveIndexWriterConfig"/> instance</param>
        /// <param name="ramBufferSizeMB"></param>
        /// <returns>this <see cref="LiveIndexWriterConfig"/> instance</returns>
        public static LiveIndexWriterConfig SetRAMBufferSizeMB(this LiveIndexWriterConfig config, double ramBufferSizeMB)
        {
            config.RAMBufferSizeMB = ramBufferSizeMB;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.MaxBufferedDocs"/>.
        /// </summary>
        /// <param name="config">this <see cref="LiveIndexWriterConfig"/> instance</param>
        /// <param name="maxBufferedDocs"></param>
        /// <returns>this <see cref="LiveIndexWriterConfig"/> instance</returns>
        public static LiveIndexWriterConfig SetMaxBufferedDocs(this LiveIndexWriterConfig config, int maxBufferedDocs)
        {
            config.MaxBufferedDocs = maxBufferedDocs;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.MergedSegmentWarmer"/>.
        /// </summary>
        /// <param name="config">this <see cref="LiveIndexWriterConfig"/> instance</param>
        /// <param name="mergeSegmentWarmer"></param>
        /// <returns>this <see cref="LiveIndexWriterConfig"/> instance</returns>
        public static LiveIndexWriterConfig SetMergedSegmentWarmer(this LiveIndexWriterConfig config, IndexWriter.IndexReaderWarmer mergeSegmentWarmer)
        {
            config.MergedSegmentWarmer = mergeSegmentWarmer;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.ReaderTermsIndexDivisor"/>.
        /// </summary>
        /// <param name="config">this <see cref="LiveIndexWriterConfig"/> instance</param>
        /// <param name="divisor"></param>
        /// <returns>this <see cref="LiveIndexWriterConfig"/> instance</returns>
        public static LiveIndexWriterConfig SetReaderTermsIndexDivisor(this LiveIndexWriterConfig config, int divisor)
        {
            config.ReaderTermsIndexDivisor = divisor;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.UseCompoundFile"/>.
        /// </summary>
        /// <param name="config">this <see cref="LiveIndexWriterConfig"/> instance</param>
        /// <param name="useCompoundFile"></param>
        /// <returns>this <see cref="LiveIndexWriterConfig"/> instance</returns>
        public static LiveIndexWriterConfig SetUseCompoundFile(this LiveIndexWriterConfig config, bool useCompoundFile)
        {
            config.UseCompoundFile = useCompoundFile;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.CheckIntegrityAtMerge"/>.
        /// </summary>
        /// <param name="config">this <see cref="LiveIndexWriterConfig"/> instance</param>
        /// <param name="checkIntegrityAtMerge"></param>
        /// <returns>this <see cref="LiveIndexWriterConfig"/> instance</returns>
        public static LiveIndexWriterConfig SetCheckIntegrityAtMerge(this LiveIndexWriterConfig config, bool checkIntegrityAtMerge)
        {
            config.CheckIntegrityAtMerge = checkIntegrityAtMerge;
            return config;
        }

        // -- IndexWriterConfig.cs (overrides)


        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.TermIndexInterval"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="interval"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetTermIndexInterval(this IndexWriterConfig config, int interval)
        {
            config.TermIndexInterval = interval;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.MaxBufferedDeleteTerms"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="maxBufferedDeleteTerms"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetMaxBufferedDeleteTerms(this IndexWriterConfig config, int maxBufferedDeleteTerms)
        {
            config.MaxBufferedDeleteTerms = maxBufferedDeleteTerms;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.RAMBufferSizeMB"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="ramBufferSizeMB"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetRAMBufferSizeMB(this IndexWriterConfig config, double ramBufferSizeMB)
        {
            config.RAMBufferSizeMB = ramBufferSizeMB;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.MaxBufferedDocs"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="maxBufferedDocs"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetMaxBufferedDocs(this IndexWriterConfig config, int maxBufferedDocs)
        {
            config.MaxBufferedDocs = maxBufferedDocs;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.MergedSegmentWarmer"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="mergeSegmentWarmer"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetMergedSegmentWarmer(this IndexWriterConfig config, IndexWriter.IndexReaderWarmer mergeSegmentWarmer)
        {
            config.MergedSegmentWarmer = mergeSegmentWarmer;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.ReaderTermsIndexDivisor"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="divisor"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetReaderTermsIndexDivisor(this IndexWriterConfig config, int divisor)
        {
            config.ReaderTermsIndexDivisor = divisor;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.UseCompoundFile"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="useCompoundFile"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetUseCompoundFile(this IndexWriterConfig config, bool useCompoundFile)
        {
            config.UseCompoundFile = useCompoundFile;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="LiveIndexWriterConfig.CheckIntegrityAtMerge"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="checkIntegrityAtMerge"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetCheckIntegrityAtMerge(this IndexWriterConfig config, bool checkIntegrityAtMerge)
        {
            config.CheckIntegrityAtMerge = checkIntegrityAtMerge;
            return config;
        }

        // -- IndexWriterConfig.cs (members)

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.DefaultWriteLockTimeout"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="writeLockTimeout"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetDefaultWriteLockTimeout(this IndexWriterConfig config, long writeLockTimeout)
        {
            IndexWriterConfig.DefaultWriteLockTimeout = writeLockTimeout;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.OpenMode"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="openMode"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetOpenMode(this IndexWriterConfig config, OpenMode openMode)
        {
            config.OpenMode = openMode;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.IndexDeletionPolicy"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="deletionPolicy"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetIndexDeletionPolicy(this IndexWriterConfig config, IndexDeletionPolicy deletionPolicy)
        {
            config.IndexDeletionPolicy = deletionPolicy;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.IndexCommit"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="commit"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetIndexCommit(this IndexWriterConfig config, IndexCommit commit)
        {
            config.IndexCommit = commit;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.Similarity"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="similarity"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetSimilarity(this IndexWriterConfig config, Similarity similarity)
        {
            config.Similarity = similarity;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.MergeScheduler"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="mergeScheduler"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetMergeScheduler(this IndexWriterConfig config, IMergeScheduler mergeScheduler)
        {
            config.MergeScheduler = mergeScheduler;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.WriteLockTimeout"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="writeLockTimeout"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetWriteLockTimeout(this IndexWriterConfig config, long writeLockTimeout)
        {
            config.WriteLockTimeout = writeLockTimeout;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.MergePolicy"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="mergePolicy"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetMergePolicy(this IndexWriterConfig config, MergePolicy mergePolicy)
        {
            config.MergePolicy = mergePolicy;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.Codec"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="codec"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetCodec(this IndexWriterConfig config, Codec codec)
        {
            config.Codec = codec;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.IndexerThreadPool"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="threadPool"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        internal static IndexWriterConfig SetIndexerThreadPool(this IndexWriterConfig config, DocumentsWriterPerThreadPool threadPool)
        {
            config.IndexerThreadPool = threadPool;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.MaxThreadStates"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="maxThreadStates"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetMaxThreadStates(this IndexWriterConfig config, int maxThreadStates)
        {
            config.MaxThreadStates = maxThreadStates;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.UseReaderPooling"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="readerPooling"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetReaderPooling(this IndexWriterConfig config, bool readerPooling)
        {
            config.UseReaderPooling = readerPooling;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.IndexingChain"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="indexingChain"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        internal static IndexWriterConfig SetIndexingChain(this IndexWriterConfig config, DocumentsWriterPerThread.IndexingChain indexingChain)
        {
            config.IndexingChain = indexingChain;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.FlushPolicy"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="flushPolicy"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        internal static IndexWriterConfig SetFlushPolicy(this IndexWriterConfig config, FlushPolicy flushPolicy)
        {
            config.FlushPolicy = flushPolicy;
            return config;
        }

        /// <summary>
        /// Builder method for <see cref="IndexWriterConfig.RAMPerThreadHardLimitMB"/>.
        /// </summary>
        /// <param name="config">this <see cref="IndexWriterConfig"/> instance</param>
        /// <param name="perThreadHardLimitMB"></param>
        /// <returns>this <see cref="IndexWriterConfig"/> instance</returns>
        public static IndexWriterConfig SetRAMPerThreadHardLimitMB(this IndexWriterConfig config, int perThreadHardLimitMB)
        {
            config.RAMPerThreadHardLimitMB = perThreadHardLimitMB;
            return config;
        }
    }
}
