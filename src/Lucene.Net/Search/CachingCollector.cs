using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;

    /// <summary>
    /// Caches all docs, and optionally also scores, coming from
    /// a search, and is then able to replay them to another
    /// collector.  You specify the max RAM this class may use.
    /// Once the collection is done, call <see cref="IsCached"/>. If
    /// this returns <c>true</c>, you can use <see cref="Replay(ICollector)"/>
    /// against a new collector.  If it returns <c>false</c>, this means
    /// too much RAM was required and you must instead re-run the
    /// original search.
    ///
    /// <para/><b>NOTE</b>: this class consumes 4 (or 8 bytes, if
    /// scoring is cached) per collected document.  If the result
    /// set is large this can easily be a very substantial amount
    /// of RAM!
    ///
    /// <para/><b>NOTE</b>: this class caches at least 128 documents
    /// before checking RAM limits.
    ///
    /// <para>See the Lucene <c>modules/grouping</c> module for more
    /// details including a full code example.</para>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class CachingCollector : ICollector
    {
        // Max out at 512K arrays
        private const int MAX_ARRAY_SIZE = 512 * 1024;

        private const int INITIAL_ARRAY_SIZE = 128;

        /// <summary>
        /// NOTE: This was EMPTY_INT_ARRAY in Lucene
        /// </summary>
        private static readonly int[] EMPTY_INT32_ARRAY = Arrays.Empty<int>();

        private class SegStart
        {
            public AtomicReaderContext ReaderContext { get; private set; }
            public int End { get; private set; }

            public SegStart(AtomicReaderContext readerContext, int end)
            {
                this.ReaderContext = readerContext;
                this.End = end;
            }
        }

        private sealed class CachedScorer : Scorer
        {
            // NOTE: these members are package-private b/c that way accessing them from
            // the outer class does not incur access check by the JVM. The same
            // situation would be if they were defined in the outer class as private
            // members.
            internal int doc;

            internal float score;

            internal CachedScorer()
                : base(null)
            {
            }

            public override float GetScore()
            {
                return score;
            }

            public override int Advance(int target)
            {
                throw UnsupportedOperationException.Create();
            }

            public override int DocID => doc;

            public override int Freq => throw UnsupportedOperationException.Create();

            public override int NextDoc()
            {
                throw UnsupportedOperationException.Create();
            }

            public override long GetCost()
            {
                return 1;
            }
        }

        /// <summary>
        /// A <see cref="CachingCollector"/> which caches scores
        /// </summary>
        private sealed class ScoreCachingCollector : CachingCollector
        {
            private readonly CachedScorer cachedScorer;
            private readonly IList<float[]> cachedScores;

            private Scorer scorer;
            private float[] curScores;

            internal ScoreCachingCollector(ICollector other, double maxRAMMB)
                : base(other, maxRAMMB, true)
            {
                cachedScorer = new CachedScorer();
                cachedScores = new JCG.List<float[]>();
                curScores = new float[INITIAL_ARRAY_SIZE];
                cachedScores.Add(curScores);
            }

            internal ScoreCachingCollector(ICollector other, int maxDocsToCache)
                : base(other, maxDocsToCache)
            {
                cachedScorer = new CachedScorer();
                cachedScores = new JCG.List<float[]>();
                curScores = new float[INITIAL_ARRAY_SIZE];
                cachedScores.Add(curScores);
            }

            public override void Collect(int doc)
            {
                if (m_curDocs is null)
                {
                    // Cache was too large
                    cachedScorer.score = scorer.GetScore();
                    cachedScorer.doc = doc;
                    m_other.Collect(doc);
                    return;
                }

                // Allocate a bigger array or abort caching
                if (m_upto == m_curDocs.Length)
                {
                    m_base += m_upto;

                    // Compute next array length - don't allocate too big arrays
                    int nextLength = 8 * m_curDocs.Length;
                    if (nextLength > MAX_ARRAY_SIZE)
                    {
                        nextLength = MAX_ARRAY_SIZE;
                    }

                    if (m_base + nextLength > m_maxDocsToCache)
                    {
                        // try to allocate a smaller array
                        nextLength = m_maxDocsToCache - m_base;
                        if (nextLength <= 0)
                        {
                            // Too many docs to collect -- clear cache
                            m_curDocs = null;
                            curScores = null;
                            m_cachedSegs.Clear();
                            m_cachedDocs.Clear();
                            cachedScores.Clear();
                            cachedScorer.score = scorer.GetScore();
                            cachedScorer.doc = doc;
                            m_other.Collect(doc);
                            return;
                        }
                    }

                    m_curDocs = new int[nextLength];
                    m_cachedDocs.Add(m_curDocs);
                    curScores = new float[nextLength];
                    cachedScores.Add(curScores);
                    m_upto = 0;
                }

                m_curDocs[m_upto] = doc;
                cachedScorer.score = curScores[m_upto] = scorer.GetScore();
                m_upto++;
                cachedScorer.doc = doc;
                m_other.Collect(doc);
            }

            public override void Replay(ICollector other)
            {
                ReplayInit(other);

                int curUpto = 0;
                int curBase = 0;
                int chunkUpto = 0;
                m_curDocs = EMPTY_INT32_ARRAY;
                foreach (SegStart seg in m_cachedSegs)
                {
                    other.SetNextReader(seg.ReaderContext);
                    other.SetScorer(cachedScorer);
                    while (curBase + curUpto < seg.End)
                    {
                        if (curUpto == m_curDocs.Length)
                        {
                            curBase += m_curDocs.Length;
                            m_curDocs = m_cachedDocs[chunkUpto];
                            curScores = cachedScores[chunkUpto];
                            chunkUpto++;
                            curUpto = 0;
                        }
                        cachedScorer.score = curScores[curUpto];
                        cachedScorer.doc = m_curDocs[curUpto];
                        other.Collect(m_curDocs[curUpto++]);
                    }
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
                m_other.SetScorer(cachedScorer);
            }

            public override string ToString()
            {
                if (IsCached)
                {
                    return "CachingCollector (" + (m_base + m_upto) + " docs & scores cached)";
                }
                else
                {
                    return "CachingCollector (cache was cleared)";
                }
            }
        }

        /// <summary>
        /// A <see cref="CachingCollector"/> which does not cache scores
        /// </summary>
        private sealed class NoScoreCachingCollector : CachingCollector
        {
            internal NoScoreCachingCollector(ICollector other, double maxRAMMB)
                : base(other, maxRAMMB, false)
            {
            }

            internal NoScoreCachingCollector(ICollector other, int maxDocsToCache)
                : base(other, maxDocsToCache)
            {
            }

            public override void Collect(int doc)
            {
                if (m_curDocs is null)
                {
                    // Cache was too large
                    m_other.Collect(doc);
                    return;
                }

                // Allocate a bigger array or abort caching
                if (m_upto == m_curDocs.Length)
                {
                    m_base += m_upto;

                    // Compute next array length - don't allocate too big arrays
                    int nextLength = 8 * m_curDocs.Length;
                    if (nextLength > MAX_ARRAY_SIZE)
                    {
                        nextLength = MAX_ARRAY_SIZE;
                    }

                    if (m_base + nextLength > m_maxDocsToCache)
                    {
                        // try to allocate a smaller array
                        nextLength = m_maxDocsToCache - m_base;
                        if (nextLength <= 0)
                        {
                            // Too many docs to collect -- clear cache
                            m_curDocs = null;
                            m_cachedSegs.Clear();
                            m_cachedDocs.Clear();
                            m_other.Collect(doc);
                            return;
                        }
                    }

                    m_curDocs = new int[nextLength];
                    m_cachedDocs.Add(m_curDocs);
                    m_upto = 0;
                }

                m_curDocs[m_upto] = doc;
                m_upto++;
                m_other.Collect(doc);
            }

            public override void Replay(ICollector other)
            {
                ReplayInit(other);

                int curUpto = 0;
                int curbase = 0;
                int chunkUpto = 0;
                m_curDocs = EMPTY_INT32_ARRAY;
                foreach (SegStart seg in m_cachedSegs)
                {
                    other.SetNextReader(seg.ReaderContext);
                    while (curbase + curUpto < seg.End)
                    {
                        if (curUpto == m_curDocs.Length)
                        {
                            curbase += m_curDocs.Length;
                            m_curDocs = m_cachedDocs[chunkUpto];
                            chunkUpto++;
                            curUpto = 0;
                        }
                        other.Collect(m_curDocs[curUpto++]);
                    }
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                m_other.SetScorer(scorer);
            }

            public override string ToString()
            {
                if (IsCached)
                {
                    return "CachingCollector (" + (m_base + m_upto) + " docs cached)";
                }
                else
                {
                    return "CachingCollector (cache was cleared)";
                }
            }
        }

        // TODO: would be nice if a collector defined a
        // needsScores() method so we can specialize / do checks
        // up front. this is only relevant for the ScoreCaching
        // version -- if the wrapped Collector does not need
        // scores, it can avoid cachedScorer entirely.
        protected readonly ICollector m_other;

        protected readonly int m_maxDocsToCache;
        private readonly IList<SegStart> m_cachedSegs = new JCG.List<SegStart>();
        protected readonly IList<int[]> m_cachedDocs;

        private AtomicReaderContext lastReaderContext;

        protected int[] m_curDocs;
        protected int m_upto;
        protected int m_base;
        protected int m_lastDocBase;

        /// <summary>
        /// Creates a <see cref="CachingCollector"/> which does not wrap another collector.
        /// The cached documents and scores can later be replayed (<see cref="Replay(ICollector)"/>).
        /// </summary>
        /// <param name="acceptDocsOutOfOrder">
        ///          whether documents are allowed to be collected out-of-order </param>
        public static CachingCollector Create(bool acceptDocsOutOfOrder, bool cacheScores, double maxRAMMB)
        {
            ICollector other = new CollectorAnonymousClass(acceptDocsOutOfOrder);
            return Create(other, cacheScores, maxRAMMB);
        }

        private sealed class CollectorAnonymousClass : ICollector
        {
            private readonly bool acceptDocsOutOfOrder;

            public CollectorAnonymousClass(bool acceptDocsOutOfOrder)
            {
                this.acceptDocsOutOfOrder = acceptDocsOutOfOrder;
            }

            public bool AcceptsDocsOutOfOrder => acceptDocsOutOfOrder;

            public void SetScorer(Scorer scorer)
            {
            }

            public void Collect(int doc)
            {
            }

            public void SetNextReader(AtomicReaderContext context)
            {
            }
        }

        /// <summary>
        /// Create a new <see cref="CachingCollector"/> that wraps the given collector and
        /// caches documents and scores up to the specified RAM threshold.
        /// </summary>
        /// <param name="other">
        ///          The <see cref="ICollector"/> to wrap and delegate calls to. </param>
        /// <param name="cacheScores">
        ///          Whether to cache scores in addition to document IDs. Note that
        ///          this increases the RAM consumed per doc. </param>
        /// <param name="maxRAMMB">
        ///          The maximum RAM in MB to consume for caching the documents and
        ///          scores. If the collector exceeds the threshold, no documents and
        ///          scores are cached. </param>
        public static CachingCollector Create(ICollector other, bool cacheScores, double maxRAMMB)
        {
            return cacheScores ? (CachingCollector)new ScoreCachingCollector(other, maxRAMMB) : new NoScoreCachingCollector(other, maxRAMMB);
        }

        /// <summary>
        /// Create a new <see cref="CachingCollector"/> that wraps the given collector and
        /// caches documents and scores up to the specified max docs threshold.
        /// </summary>
        /// <param name="other">
        ///          The <see cref="ICollector"/> to wrap and delegate calls to. </param>
        /// <param name="cacheScores">
        ///          Whether to cache scores in addition to document IDs. Note that
        ///          this increases the RAM consumed per doc. </param>
        /// <param name="maxDocsToCache">
        ///          The maximum number of documents for caching the documents and
        ///          possible the scores. If the collector exceeds the threshold,
        ///          no documents and scores are cached. </param>
        public static CachingCollector Create(ICollector other, bool cacheScores, int maxDocsToCache)
        {
            return cacheScores ? (CachingCollector)new ScoreCachingCollector(other, maxDocsToCache) : new NoScoreCachingCollector(other, maxDocsToCache);
        }

        // Prevent extension from non-internal classes
        private CachingCollector(ICollector other, double maxRAMMB, bool cacheScores)
        {
            this.m_other = other;

            m_cachedDocs = new JCG.List<int[]>();
            m_curDocs = new int[INITIAL_ARRAY_SIZE];
            m_cachedDocs.Add(m_curDocs);

            int bytesPerDoc = RamUsageEstimator.NUM_BYTES_INT32;
            if (cacheScores)
            {
                bytesPerDoc += RamUsageEstimator.NUM_BYTES_SINGLE;
            }
            m_maxDocsToCache = (int)((maxRAMMB * 1024 * 1024) / bytesPerDoc);
        }

        private CachingCollector(ICollector other, int maxDocsToCache)
        {
            this.m_other = other;

            m_cachedDocs = new JCG.List<int[]>();
            m_curDocs = new int[INITIAL_ARRAY_SIZE];
            m_cachedDocs.Add(m_curDocs);
            this.m_maxDocsToCache = maxDocsToCache;
        }

        public virtual bool AcceptsDocsOutOfOrder => m_other.AcceptsDocsOutOfOrder;

        public virtual bool IsCached => m_curDocs != null;

        public virtual void SetNextReader(AtomicReaderContext context)
        {
            m_other.SetNextReader(context);
            if (lastReaderContext != null)
            {
                m_cachedSegs.Add(new SegStart(lastReaderContext, m_base + m_upto));
            }
            lastReaderContext = context;
        }

        // LUCENENET specific - we need to implement these here, since our abstract base class
        // is now an interface.
        /// <summary>
        /// Called before successive calls to <see cref="Collect(int)"/>. Implementations
        /// that need the score of the current document (passed-in to
        /// <also cref="Collect(int)"/>), should save the passed-in <see cref="Scorer"/> and call
        /// <see cref="Scorer.GetScore()"/> when needed.
        /// </summary>
        public abstract void SetScorer(Scorer scorer);

        /// <summary>
        /// Called once for every document matching a query, with the unbased document
        /// number.
        /// <para/>Note: The collection of the current segment can be terminated by throwing
        /// a <see cref="CollectionTerminatedException"/>. In this case, the last docs of the
        /// current <see cref="AtomicReaderContext"/> will be skipped and <see cref="IndexSearcher"/>
        /// will swallow the exception and continue collection with the next leaf.
        /// <para/>
        /// Note: this is called in an inner search loop. For good search performance,
        /// implementations of this method should not call <see cref="IndexSearcher.Doc(int)"/> or
        /// <see cref="Lucene.Net.Index.IndexReader.Document(int)"/> on every hit.
        /// Doing so can slow searches by an order of magnitude or more.
        /// </summary>
        public abstract void Collect(int doc);

        /// <summary>
        /// Reused by the specialized inner classes. </summary>
        internal virtual void ReplayInit(ICollector other)
        {
            if (!IsCached)
            {
                throw IllegalStateException.Create("cannot replay: cache was cleared because too much RAM was required");
            }

            if (!other.AcceptsDocsOutOfOrder && this.m_other.AcceptsDocsOutOfOrder)
            {
                throw new ArgumentException("cannot replay: given collector does not support " +
                    "out-of-order collection, while the wrapped collector does. " +
                    "Therefore cached documents may be out-of-order.");
            }

            //System.out.println("CC: replay totHits=" + (upto + base));
            if (lastReaderContext != null)
            {
                m_cachedSegs.Add(new SegStart(lastReaderContext, m_base + m_upto));
                lastReaderContext = null;
            }
        }

        /// <summary>
        /// Replays the cached doc IDs (and scores) to the given <see cref="ICollector"/>. If this
        /// instance does not cache scores, then <see cref="Scorer"/> is not set on
        /// <c>other.SetScorer(Scorer)</c> as well as scores are not replayed.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///           If this collector is not cached (i.e., if the RAM limits were too
        ///           low for the number of documents + scores to cache). </exception>
        /// <exception cref="ArgumentException">
        ///           If the given Collect's does not support out-of-order collection,
        ///           while the collector passed to the ctor does. </exception>
        public abstract void Replay(ICollector other);
    }
}