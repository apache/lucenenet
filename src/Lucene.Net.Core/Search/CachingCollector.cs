using System.Collections.Generic;

namespace Lucene.Net.Search
{
    using System;

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
    public abstract class CachingCollector : Collector
    {
        // Max out at 512K arrays
        private const int MAX_ARRAY_SIZE = 512 * 1024;

        private const int INITIAL_ARRAY_SIZE = 128;
        private static readonly int[] EMPTY_INT_ARRAY = new int[0];

        private class SegStart
        {
            public readonly AtomicReaderContext ReaderContext;
            public readonly int End;

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
            internal int Doc;

            internal float Score_Renamed;

            internal CachedScorer()
                : base(null)
            {
            }

            public override float Score()
            {
                return Score_Renamed;
            }

            public override int Advance(int target)
            {
                throw new System.NotSupportedException();
            }

            public override int DocID()
            {
                return Doc;
            }

            public override int Freq()
            {
                throw new System.NotSupportedException();
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
            internal readonly CachedScorer CachedScorer;
            internal readonly IList<float[]> CachedScores;

            internal Scorer Scorer_Renamed;
            internal float[] CurScores;

            internal ScoreCachingCollector(Collector other, double maxRAMMB)
                : base(other, maxRAMMB, true)
            {
                CachedScorer = new CachedScorer();
                CachedScores = new List<float[]>();
                CurScores = new float[INITIAL_ARRAY_SIZE];
                CachedScores.Add(CurScores);
            }

            internal ScoreCachingCollector(Collector other, int maxDocsToCache)
                : base(other, maxDocsToCache)
            {
                CachedScorer = new CachedScorer();
                CachedScores = new List<float[]>();
                CurScores = new float[INITIAL_ARRAY_SIZE];
                CachedScores.Add(CurScores);
            }

            public override void Collect(int doc)
            {
                if (CurDocs == null)
                {
                    // Cache was too large
                    CachedScorer.Score_Renamed = Scorer_Renamed.Score();
                    CachedScorer.Doc = doc;
                    Other.Collect(doc);
                    return;
                }

                // Allocate a bigger array or abort caching
                if (Upto == CurDocs.Length)
                {
                    @base += Upto;

                    // Compute next array length - don't allocate too big arrays
                    int nextLength = 8 * CurDocs.Length;
                    if (nextLength > MAX_ARRAY_SIZE)
                    {
                        nextLength = MAX_ARRAY_SIZE;
                    }

                    if (@base + nextLength > MaxDocsToCache)
                    {
                        // try to allocate a smaller array
                        nextLength = MaxDocsToCache - @base;
                        if (nextLength <= 0)
                        {
                            // Too many docs to collect -- clear cache
                            CurDocs = null;
                            CurScores = null;
                            CachedSegs.Clear();
                            CachedDocs.Clear();
                            CachedScores.Clear();
                            CachedScorer.Score_Renamed = Scorer_Renamed.Score();
                            CachedScorer.Doc = doc;
                            Other.Collect(doc);
                            return;
                        }
                    }

                    CurDocs = new int[nextLength];
                    CachedDocs.Add(CurDocs);
                    CurScores = new float[nextLength];
                    CachedScores.Add(CurScores);
                    Upto = 0;
                }

                CurDocs[Upto] = doc;
                CachedScorer.Score_Renamed = CurScores[Upto] = Scorer_Renamed.Score();
                Upto++;
                CachedScorer.Doc = doc;
                Other.Collect(doc);
            }

            public override void Replay(Collector other)
            {
                ReplayInit(other);

                int curUpto = 0;
                int curBase = 0;
                int chunkUpto = 0;
                CurDocs = EMPTY_INT_ARRAY;
                foreach (SegStart seg in CachedSegs)
                {
                    other.NextReader = seg.ReaderContext;
                    other.Scorer = CachedScorer;
                    while (curBase + curUpto < seg.End)
                    {
                        if (curUpto == CurDocs.Length)
                        {
                            curBase += CurDocs.Length;
                            CurDocs = CachedDocs[chunkUpto];
                            CurScores = CachedScores[chunkUpto];
                            chunkUpto++;
                            curUpto = 0;
                        }
                        CachedScorer.Score_Renamed = CurScores[curUpto];
                        CachedScorer.Doc = CurDocs[curUpto];
                        other.Collect(CurDocs[curUpto++]);
                    }
                }
            }

            public override Scorer Scorer
            {
                set
                {
                    this.Scorer_Renamed = value;
                    Other.Scorer = CachedScorer;
                }
            }

            public override string ToString()
            {
                if (Cached)
                {
                    return "CachingCollector (" + (@base + Upto) + " docs & scores cached)";
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
            internal NoScoreCachingCollector(Collector other, double maxRAMMB)
                : base(other, maxRAMMB, false)
            {
            }

            internal NoScoreCachingCollector(Collector other, int maxDocsToCache)
                : base(other, maxDocsToCache)
            {
            }

            public override void Collect(int doc)
            {
                if (CurDocs == null)
                {
                    // Cache was too large
                    Other.Collect(doc);
                    return;
                }

                // Allocate a bigger array or abort caching
                if (Upto == CurDocs.Length)
                {
                    @base += Upto;

                    // Compute next array length - don't allocate too big arrays
                    int nextLength = 8 * CurDocs.Length;
                    if (nextLength > MAX_ARRAY_SIZE)
                    {
                        nextLength = MAX_ARRAY_SIZE;
                    }

                    if (@base + nextLength > MaxDocsToCache)
                    {
                        // try to allocate a smaller array
                        nextLength = MaxDocsToCache - @base;
                        if (nextLength <= 0)
                        {
                            // Too many docs to collect -- clear cache
                            CurDocs = null;
                            CachedSegs.Clear();
                            CachedDocs.Clear();
                            Other.Collect(doc);
                            return;
                        }
                    }

                    CurDocs = new int[nextLength];
                    CachedDocs.Add(CurDocs);
                    Upto = 0;
                }

                CurDocs[Upto] = doc;
                Upto++;
                Other.Collect(doc);
            }

            public override void Replay(Collector other)
            {
                ReplayInit(other);

                int curUpto = 0;
                int curbase = 0;
                int chunkUpto = 0;
                CurDocs = EMPTY_INT_ARRAY;
                foreach (SegStart seg in CachedSegs)
                {
                    other.NextReader = seg.ReaderContext;
                    while (curbase + curUpto < seg.End)
                    {
                        if (curUpto == CurDocs.Length)
                        {
                            curbase += CurDocs.Length;
                            CurDocs = CachedDocs[chunkUpto];
                            chunkUpto++;
                            curUpto = 0;
                        }
                        other.Collect(CurDocs[curUpto++]);
                    }
                }
            }

            public override Scorer Scorer
            {
                set
                {
                    Other.Scorer = value;
                }
            }

            public override string ToString()
            {
                if (Cached)
                {
                    return "CachingCollector (" + (@base + Upto) + " docs cached)";
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
        protected readonly Collector Other;

        protected readonly int MaxDocsToCache;
        private readonly IList<SegStart> CachedSegs = new List<SegStart>();
        protected readonly IList<int[]> CachedDocs;

        private AtomicReaderContext LastReaderContext;

        protected int[] CurDocs;
        protected int Upto;
        protected int @base;
        protected int LastDocBase;

        /// <summary>
        /// Creates a <seealso cref="CachingCollector"/> which does not wrap another collector.
        /// The cached documents and scores can later be {@link #replay(Collector)
        /// replayed}.
        /// </summary>
        /// <param name="acceptDocsOutOfOrder">
        ///          whether documents are allowed to be collected out-of-order </param>
        public static CachingCollector Create(bool acceptDocsOutOfOrder, bool cacheScores, double maxRAMMB)
        {
            Collector other = new CollectorAnonymousInnerClassHelper(acceptDocsOutOfOrder);
            return Create(other, cacheScores, maxRAMMB);
        }

        private class CollectorAnonymousInnerClassHelper : Collector
        {
            private bool AcceptDocsOutOfOrder;

            public CollectorAnonymousInnerClassHelper(bool acceptDocsOutOfOrder)
            {
                this.AcceptDocsOutOfOrder = acceptDocsOutOfOrder;
            }

            public override bool AcceptsDocsOutOfOrder()
            {
                return AcceptDocsOutOfOrder;
            }

            public override Scorer Scorer
            {
                set
                {
                }
            }

            public override void Collect(int doc)
            {
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                }
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
        public static CachingCollector Create(Collector other, bool cacheScores, double maxRAMMB)
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
        public static CachingCollector Create(Collector other, bool cacheScores, int maxDocsToCache)
        {
            return cacheScores ? (CachingCollector)new ScoreCachingCollector(other, maxDocsToCache) : new NoScoreCachingCollector(other, maxDocsToCache);
        }

        // Prevent extension from non-internal classes
        private CachingCollector(Collector other, double maxRAMMB, bool cacheScores)
        {
            this.Other = other;

            CachedDocs = new List<int[]>();
            CurDocs = new int[INITIAL_ARRAY_SIZE];
            CachedDocs.Add(CurDocs);

            int bytesPerDoc = RamUsageEstimator.NUM_BYTES_INT;
            if (cacheScores)
            {
                bytesPerDoc += RamUsageEstimator.NUM_BYTES_FLOAT;
            }
            MaxDocsToCache = (int)((maxRAMMB * 1024 * 1024) / bytesPerDoc);
        }

        private CachingCollector(Collector other, int maxDocsToCache)
        {
            this.Other = other;

            CachedDocs = new List<int[]>();
            CurDocs = new int[INITIAL_ARRAY_SIZE];
            CachedDocs.Add(CurDocs);
            this.MaxDocsToCache = maxDocsToCache;
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return Other.AcceptsDocsOutOfOrder();
        }

        // LUCENENET TODO: Rename to IsCached
        public virtual bool Cached
        {
            get
            {
                return CurDocs != null;
            }
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                Other.NextReader = value;
                if (LastReaderContext != null)
                {
                    CachedSegs.Add(new SegStart(LastReaderContext, @base + Upto));
                }
                LastReaderContext = value;
            }
        }

        /// <summary>
        /// Reused by the specialized inner classes. </summary>
        internal virtual void ReplayInit(Collector other)
        {
            if (!Cached)
            {
                throw new InvalidOperationException("cannot replay: cache was cleared because too much RAM was required");
            }

            if (!other.AcceptsDocsOutOfOrder() && this.Other.AcceptsDocsOutOfOrder())
            {
                throw new System.ArgumentException("cannot replay: given collector does not support " + "out-of-order collection, while the wrapped collector does. " + "Therefore cached documents may be out-of-order.");
            }

            //System.out.println("CC: replay totHits=" + (upto + base));
            if (LastReaderContext != null)
            {
                CachedSegs.Add(new SegStart(LastReaderContext, @base + Upto));
                LastReaderContext = null;
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
        public abstract void Replay(Collector other);
    }
}