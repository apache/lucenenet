using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public abstract class CachingCollector : Collector
    {
        // Max out at 512K arrays
        private const int MAX_ARRAY_SIZE = 512 * 1024;
        private const int INITIAL_ARRAY_SIZE = 128;
        private static readonly int[] EMPTY_INT_ARRAY = new int[0];

        public class SegStart
        {
            public readonly AtomicReaderContext readerContext;
            public readonly int end;

            public SegStart(AtomicReaderContext readerContext, int end)
            {
                this.readerContext = readerContext;
                this.end = end;
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

            public override float Score()
            {
                return score;
            }

            public override int Advance(int target)
            {
                throw new NotSupportedException();
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int Freq
            {
                get { throw new NotSupportedException(); }
            }

            public override int NextDoc()
            {
                throw new NotSupportedException();
            }

            public override long Cost
            {
                get { return 1; }
            }
        }

        private sealed class ScoreCachingCollector : CachingCollector
        {
            private readonly CachedScorer cachedScorer;
            private readonly IList<float[]> cachedScores;

            private Scorer scorer;
            private float[] curScores;

            internal ScoreCachingCollector(Collector other, double maxRAMMB)
                : base(other, maxRAMMB, true)
            {
                cachedScorer = new CachedScorer();
                cachedScores = new List<float[]>();
                curScores = new float[INITIAL_ARRAY_SIZE];
                cachedScores.Add(curScores);
            }

            internal ScoreCachingCollector(Collector other, int maxDocsToCache)
                : base(other, maxDocsToCache)
            {
                cachedScorer = new CachedScorer();
                cachedScores = new List<float[]>();
                curScores = new float[INITIAL_ARRAY_SIZE];
                cachedScores.Add(curScores);
            }

            public override void Collect(int doc)
            {
                if (curDocs == null)
                {
                    // Cache was too large
                    cachedScorer.score = scorer.Score();
                    cachedScorer.doc = doc;
                    other.Collect(doc);
                    return;
                }

                // Allocate a bigger array or abort caching
                if (upto == curDocs.Length)
                {
                    base_renamed += upto;

                    // Compute next array length - don't allocate too big arrays
                    int nextLength = 8 * curDocs.Length;
                    if (nextLength > MAX_ARRAY_SIZE)
                    {
                        nextLength = MAX_ARRAY_SIZE;
                    }

                    if (base_renamed + nextLength > maxDocsToCache)
                    {
                        // try to allocate a smaller array
                        nextLength = maxDocsToCache - base_renamed;
                        if (nextLength <= 0)
                        {
                            // Too many docs to collect -- clear cache
                            curDocs = null;
                            curScores = null;
                            cachedSegs.Clear();
                            cachedDocs.Clear();
                            cachedScores.Clear();
                            cachedScorer.score = scorer.Score();
                            cachedScorer.doc = doc;
                            other.Collect(doc);
                            return;
                        }
                    }

                    curDocs = new int[nextLength];
                    cachedDocs.Add(curDocs);
                    curScores = new float[nextLength];
                    cachedScores.Add(curScores);
                    upto = 0;
                }

                curDocs[upto] = doc;
                cachedScorer.score = curScores[upto] = scorer.Score();
                upto++;
                cachedScorer.doc = doc;
                other.Collect(doc);
            }

            public override void Replay(Collector other)
            {
                ReplayInit(other);

                int curUpto = 0;
                int curBase = 0;
                int chunkUpto = 0;
                curDocs = EMPTY_INT_ARRAY;
                foreach (SegStart seg in cachedSegs)
                {
                    other.SetNextReader(seg.readerContext);
                    other.SetScorer(cachedScorer);
                    while (curBase + curUpto < seg.end)
                    {
                        if (curUpto == curDocs.Length)
                        {
                            curBase += curDocs.Length;
                            curDocs = cachedDocs[chunkUpto];
                            curScores = cachedScores[chunkUpto];
                            chunkUpto++;
                            curUpto = 0;
                        }
                        cachedScorer.score = curScores[curUpto];
                        cachedScorer.doc = curDocs[curUpto];
                        other.Collect(curDocs[curUpto++]);
                    }
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                this.scorer = scorer;
                other.SetScorer(cachedScorer);
            }

            public override string ToString()
            {
                if (IsCached)
                {
                    return "CachingCollector (" + (base_renamed + upto) + " docs & scores cached)";
                }
                else
                {
                    return "CachingCollector (cache was cleared)";
                }
            }
        }

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
                if (curDocs == null)
                {
                    // Cache was too large
                    other.Collect(doc);
                    return;
                }

                // Allocate a bigger array or abort caching
                if (upto == curDocs.Length)
                {
                    base_renamed += upto;

                    // Compute next array length - don't allocate too big arrays
                    int nextLength = 8 * curDocs.Length;
                    if (nextLength > MAX_ARRAY_SIZE)
                    {
                        nextLength = MAX_ARRAY_SIZE;
                    }

                    if (base_renamed + nextLength > maxDocsToCache)
                    {
                        // try to allocate a smaller array
                        nextLength = maxDocsToCache - base_renamed;
                        if (nextLength <= 0)
                        {
                            // Too many docs to collect -- clear cache
                            curDocs = null;
                            cachedSegs.Clear();
                            cachedDocs.Clear();
                            other.Collect(doc);
                            return;
                        }
                    }

                    curDocs = new int[nextLength];
                    cachedDocs.Add(curDocs);
                    upto = 0;
                }

                curDocs[upto] = doc;
                upto++;
                other.Collect(doc);
            }

            public override void Replay(Collector other)
            {
                ReplayInit(other);

                int curUpto = 0;
                int curbase = 0;
                int chunkUpto = 0;
                curDocs = EMPTY_INT_ARRAY;
                foreach (SegStart seg in cachedSegs)
                {
                    other.SetNextReader(seg.readerContext);
                    while (curbase + curUpto < seg.end)
                    {
                        if (curUpto == curDocs.Length)
                        {
                            curbase += curDocs.Length;
                            curDocs = cachedDocs[chunkUpto];
                            chunkUpto++;
                            curUpto = 0;
                        }
                        other.Collect(curDocs[curUpto++]);
                    }
                }
            }

            public override void SetScorer(Scorer scorer)
            {
                other.SetScorer(scorer);
            }

            public override string ToString()
            {
                if (IsCached)
                {
                    return "CachingCollector (" + (base_renamed + upto) + " docs cached)";
                }
                else
                {
                    return "CachingCollector (cache was cleared)";
                }
            }
        }

        // TODO: would be nice if a collector defined a
        // needsScores() method so we can specialize / do checks
        // up front. This is only relevant for the ScoreCaching
        // version -- if the wrapped Collector does not need
        // scores, it can avoid cachedScorer entirely.
        protected readonly Collector other;

        protected readonly int maxDocsToCache;
        protected readonly IList<SegStart> cachedSegs = new List<SegStart>();
        protected readonly IList<int[]> cachedDocs;

        private AtomicReaderContext lastReaderContext;

        protected int[] curDocs;
        protected int upto;
        protected int base_renamed;
        protected int lastDocBase;

        public static CachingCollector Create(bool acceptDocsOutOfOrder, bool cacheScores, double maxRAMMB)
        {
            Collector other = new AnonymousEmptyCollector(acceptDocsOutOfOrder);
            return Create(other, cacheScores, maxRAMMB);
        }

        private sealed class AnonymousEmptyCollector : Collector
        {
            private readonly bool acceptDocsOutOfOrder;

            public AnonymousEmptyCollector(bool acceptDocsOutOfOrder)
            {
                this.acceptDocsOutOfOrder = acceptDocsOutOfOrder;
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get { return acceptDocsOutOfOrder; }
            }

            public override void SetScorer(Scorer scorer) { }

            public override void Collect(int doc) { }

            public override void SetNextReader(AtomicReaderContext context) { }
        }

        public static CachingCollector Create(Collector other, bool cacheScores, double maxRAMMB)
        {
            return cacheScores ? (CachingCollector)new ScoreCachingCollector(other, maxRAMMB) : new NoScoreCachingCollector(other, maxRAMMB);
        }

        public static CachingCollector Create(Collector other, bool cacheScores, int maxDocsToCache)
        {
            return cacheScores ? (CachingCollector)new ScoreCachingCollector(other, maxDocsToCache) : new NoScoreCachingCollector(other, maxDocsToCache);
        }

        // Prevent extension from non-internal classes
        private CachingCollector(Collector other, double maxRAMMB, bool cacheScores)
        {
            this.other = other;

            cachedDocs = new List<int[]>();
            curDocs = new int[INITIAL_ARRAY_SIZE];
            cachedDocs.Add(curDocs);

            int bytesPerDoc = RamUsageEstimator.NUM_BYTES_INT;
            if (cacheScores)
            {
                bytesPerDoc += RamUsageEstimator.NUM_BYTES_FLOAT;
            }
            maxDocsToCache = (int)((maxRAMMB * 1024 * 1024) / bytesPerDoc);
        }

        private CachingCollector(Collector other, int maxDocsToCache)
        {
            this.other = other;

            cachedDocs = new List<int[]>();
            curDocs = new int[INITIAL_ARRAY_SIZE];
            cachedDocs.Add(curDocs);
            this.maxDocsToCache = maxDocsToCache;
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return other.AcceptsDocsOutOfOrder; }
        }

        public bool IsCached
        {
            get { return curDocs != null; }
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
            other.SetNextReader(context);
            if (lastReaderContext != null)
            {
                cachedSegs.Add(new SegStart(lastReaderContext, base_renamed + upto));
            }
            lastReaderContext = context;
        }

        internal virtual void ReplayInit(Collector other)
        {
            if (!IsCached)
            {
                throw new InvalidOperationException("cannot replay: cache was cleared because too much RAM was required");
            }

            if (!other.AcceptsDocsOutOfOrder && this.other.AcceptsDocsOutOfOrder)
            {
                throw new InvalidOperationException(
                    "cannot replay: given collector does not support "
                        + "out-of-order collection, while the wrapped collector does. "
                        + "Therefore cached documents may be out-of-order.");
            }

            //System.out.println("CC: replay totHits=" + (upto + base));
            if (lastReaderContext != null)
            {
                cachedSegs.Add(new SegStart(lastReaderContext, base_renamed + upto));
                lastReaderContext = null;
            }
        }

        public abstract void Replay(Collector other);
    }
}
