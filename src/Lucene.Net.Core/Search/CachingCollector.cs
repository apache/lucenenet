using System;
using System.Collections.Generic;

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
    /// Once the collection is done, call <seealso cref="#isCached"/>. If
    /// this returns true, you can use <seealso cref="#replay(Collector)"/>
    /// against a new collector.  If it returns false, this means
    /// too much RAM was required and you must instead re-run the
    /// original search.
    ///
    /// <p><b>NOTE</b>: this class consumes 4 (or 8 bytes, if
    /// scoring is cached) per collected document.  If the result
    /// set is large this can easily be a very substantial amount
    /// of RAM!
    ///
    /// <p><b>NOTE</b>: this class caches at least 128 documents
    /// before checking RAM limits.
    ///
    /// <p>See the Lucene <tt>modules/grouping</tt> module for more
    /// details including a full code example.</p>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class CachingCollector : ICollector
    {
        // Max out at 512K arrays
        private const int MAX_ARRAY_SIZE = 512 * 1024;

        private const int INITIAL_ARRAY_SIZE = 128;
        private static readonly int[] EMPTY_INT_ARRAY = new int[0];

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
                throw new System.NotSupportedException();
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int Freq
            {
                get { throw new System.NotSupportedException(); }
            }

            public override int NextDoc()
            {
                throw new System.NotSupportedException();
            }

            public override long Cost()
            {
                return 1;
            }
        }

        // A CachingCollector which caches scores
        private sealed class ScoreCachingCollector : CachingCollector
        {
            private new readonly CachedScorer cachedScorer;
            private readonly IList<float[]> cachedScores;

            private Scorer scorer;
            private float[] curScores;

            internal ScoreCachingCollector(ICollector other, double maxRAMMB)
                : base(other, maxRAMMB, true)
            {
                cachedScorer = new CachedScorer();
                cachedScores = new List<float[]>();
                curScores = new float[INITIAL_ARRAY_SIZE];
                cachedScores.Add(curScores);
            }

            internal ScoreCachingCollector(ICollector other, int maxDocsToCache)
                : base(other, maxDocsToCache)
            {
                cachedScorer = new CachedScorer();
                cachedScores = new List<float[]>();
                curScores = new float[INITIAL_ARRAY_SIZE];
                cachedScores.Add(curScores);
            }

            public override void Collect(int doc)
            {
                if (m_curDocs == null)
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
                m_curDocs = EMPTY_INT_ARRAY;
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

        // A CachingCollector which does not cache scores
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
                if (m_curDocs == null)
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
                m_curDocs = EMPTY_INT_ARRAY;
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
        private readonly IList<SegStart> m_cachedSegs = new List<SegStart>();
        protected readonly IList<int[]> m_cachedDocs;

        private AtomicReaderContext lastReaderContext;

        protected int[] m_curDocs;
        protected int m_upto;
        protected int m_base;
        protected int m_lastDocBase;

        /// <summary>
        /// Creates a <seealso cref="CachingCollector"/> which does not wrap another collector.
        /// The cached documents and scores can later be {@link #replay(Collector)
        /// replayed}.
        /// </summary>
        /// <param name="acceptDocsOutOfOrder">
        ///          whether documents are allowed to be collected out-of-order </param>
        public static CachingCollector Create(bool acceptDocsOutOfOrder, bool cacheScores, double maxRAMMB)
        {
            ICollector other = new CollectorAnonymousInnerClassHelper(acceptDocsOutOfOrder);
            return Create(other, cacheScores, maxRAMMB);
        }

        private class CollectorAnonymousInnerClassHelper : ICollector
        {
            private bool acceptDocsOutOfOrder;

            public CollectorAnonymousInnerClassHelper(bool acceptDocsOutOfOrder)
            {
                this.acceptDocsOutOfOrder = acceptDocsOutOfOrder;
            }

            public virtual bool AcceptsDocsOutOfOrder
            {
                get { return acceptDocsOutOfOrder; }
            }

            public virtual void SetScorer(Scorer scorer)
            {
            }

            public virtual void Collect(int doc)
            {
            }

            public virtual void SetNextReader(AtomicReaderContext context)
            {
            }
        }

        /// <summary>
        /// Create a new <seealso cref="CachingCollector"/> that wraps the given collector and
        /// caches documents and scores up to the specified RAM threshold.
        /// </summary>
        /// <param name="other">
        ///          the Collector to wrap and delegate calls to. </param>
        /// <param name="cacheScores">
        ///          whether to cache scores in addition to document IDs. Note that
        ///          this increases the RAM consumed per doc </param>
        /// <param name="maxRAMMB">
        ///          the maximum RAM in MB to consume for caching the documents and
        ///          scores. If the collector exceeds the threshold, no documents and
        ///          scores are cached. </param>
        public static CachingCollector Create(ICollector other, bool cacheScores, double maxRAMMB)
        {
            return cacheScores ? (CachingCollector)new ScoreCachingCollector(other, maxRAMMB) : new NoScoreCachingCollector(other, maxRAMMB);
        }

        /// <summary>
        /// Create a new <seealso cref="CachingCollector"/> that wraps the given collector and
        /// caches documents and scores up to the specified max docs threshold.
        /// </summary>
        /// <param name="other">
        ///          the Collector to wrap and delegate calls to. </param>
        /// <param name="cacheScores">
        ///          whether to cache scores in addition to document IDs. Note that
        ///          this increases the RAM consumed per doc </param>
        /// <param name="maxDocsToCache">
        ///          the maximum number of documents for caching the documents and
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

            m_cachedDocs = new List<int[]>();
            m_curDocs = new int[INITIAL_ARRAY_SIZE];
            m_cachedDocs.Add(m_curDocs);

            int bytesPerDoc = RamUsageEstimator.NUM_BYTES_INT;
            if (cacheScores)
            {
                bytesPerDoc += RamUsageEstimator.NUM_BYTES_FLOAT;
            }
            m_maxDocsToCache = (int)((maxRAMMB * 1024 * 1024) / bytesPerDoc);
        }

        private CachingCollector(ICollector other, int maxDocsToCache)
        {
            this.m_other = other;

            m_cachedDocs = new List<int[]>();
            m_curDocs = new int[INITIAL_ARRAY_SIZE];
            m_cachedDocs.Add(m_curDocs);
            this.m_maxDocsToCache = maxDocsToCache;
        }

        public virtual bool AcceptsDocsOutOfOrder
        {
            get { return m_other.AcceptsDocsOutOfOrder; }
        }

        public virtual bool IsCached
        {
            get
            {
                return m_curDocs != null;
            }
        }

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
        public abstract void SetScorer(Scorer scorer); // LUCENENET TODO: Copy documentation from ICollector

        public abstract void Collect(int doc); // LUCENENET TODO: Copy documentation from ICollector

        /// <summary>
        /// Reused by the specialized inner classes. </summary>
        internal virtual void ReplayInit(ICollector other)
        {
            if (!IsCached)
            {
                throw new InvalidOperationException("cannot replay: cache was cleared because too much RAM was required");
            }

            if (!other.AcceptsDocsOutOfOrder && this.m_other.AcceptsDocsOutOfOrder)
            {
                throw new System.ArgumentException("cannot replay: given collector does not support " + "out-of-order collection, while the wrapped collector does. " + "Therefore cached documents may be out-of-order.");
            }

            //System.out.println("CC: replay totHits=" + (upto + base));
            if (lastReaderContext != null)
            {
                m_cachedSegs.Add(new SegStart(lastReaderContext, m_base + m_upto));
                lastReaderContext = null;
            }
        }

        /// <summary>
        /// Replays the cached doc IDs (and scores) to the given Collector. If this
        /// instance does not cache scores, then Scorer is not set on
        /// {@code other.setScorer} as well as scores are not replayed.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///           if this collector is not cached (i.e., if the RAM limits were too
        ///           low for the number of documents + scores to cache). </exception>
        /// <exception cref="IllegalArgumentException">
        ///           if the given Collect's does not support out-of-order collection,
        ///           while the collector passed to the ctor does. </exception>
        public abstract void Replay(ICollector other);
    }
}