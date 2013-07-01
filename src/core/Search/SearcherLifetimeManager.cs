using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
    public class SearcherLifetimeManager : IDisposable
    {
        internal static readonly double NANOS_PER_SEC = 1000000000.0;

        private class SearcherTracker : IComparable<SearcherTracker>, IDisposable
        {
            private readonly IndexSearcher searcher;
            public IndexSearcher Searcher { get { return searcher; } }

            private readonly double recordTimeSec;
            public double RecordTimeSec { get { return recordTimeSec; } }

            private readonly long version;
            public long Version { get { return version; } }

            public SearcherTracker(IndexSearcher searcher)
            {
                this.searcher = searcher;
                version = ((DirectoryReader) searcher.IndexReader).Version;
                searcher.IndexReader.IncRef();
                recordTimeSec = DateTime.Now.ToFileTime()/100.0d/NANOS_PER_SEC;
            }

            public int CompareTo(SearcherTracker other)
            {
                return recordTimeSec.CompareTo(other.recordTimeSec);
            }

            #region IDisposable
            public void Dispose()
            {
                lock (this)
                {
                    searcher.IndexReader.DecRef();
                }
            }
            #endregion
        }

        private volatile bool disposed;

        private readonly ConcurrentHashMap<long, SearcherTracker> searchers =
            new ConcurrentHashMap<long, SearcherTracker>();

        private void EnsureOpen()
        {
            if (disposed)
            {
                throw new AlreadyClosedException("this SearcherLifetimeManager instance is disposed");
            }
        }

        public long Record(IndexSearcher searcher)
        {
            EnsureOpen();

            var version = ((DirectoryReader) searcher.IndexReader).Version;
            var tracker = searchers[version];
            if (tracker == null)
            {
                tracker = new SearcherTracker(searcher);
                if (searchers.AddIfAbsent(version, tracker) != null)
                {
                    tracker.Dispose();
                }
            }
            else if (tracker.Searcher != searcher)
            {
                throw new ArgumentException("the provided searcher has the same underlying reader version yet the searcher instance differs from before (new=" + searcher + " vs old=" + tracker.Searcher);
            }

            return version;
        }

        public IndexSearcher Acquire(long version)
        {
            EnsureOpen();
            var tracker = searchers[version];
            if (tracker != null && tracker.Searcher.IndexReader.TryIncRef())
            {
                return tracker.Searcher;
            }
            return null;
        }

        public void Release(IndexSearcher searcher)
        {
            searcher.IndexReader.DecRef();
        }

        public interface Pruner
        {
            bool DoPrune(double ageSec, IndexSearcher searcher);
        }

        public sealed class PruneByAge : Pruner
        {
            private readonly double maxAgeSec;

            public PruneByAge(double maxAgeSec)
            {
                if (maxAgeSec < 0)
                    throw new ArgumentException("maxAgeSec must be > 0 (got " + maxAgeSec + ")");

                this.maxAgeSec = maxAgeSec;
            }

            public bool DoPrune(double ageSec, IndexSearcher searcher)
            {
                return ageSec > maxAgeSec;
            }
        }

        public void Prune(Pruner pruner)
        {
            lock (this)
            {
                var trackers = searchers.Values.ToList();
                Collections.Sort(trackers, null);
                var lastRecordTimeSec = 0.0d;
                var now = DateTime.Now.ToFileTime()/100.0d/NANOS_PER_SEC;
                foreach (var tracker in trackers)
                {
                    double ageSec;
                    if (lastRecordTimeSec == 0.0)
                    {
                        ageSec = 0.0;
                    }
                    else
                    {
                        ageSec = now - lastRecordTimeSec;
                    }

                    if (pruner.DoPrune(ageSec, tracker.Searcher))
                    {
                        searchers.Remove(tracker.Version);
                        tracker.Dispose();
                    }
                    lastRecordTimeSec = tracker.RecordTimeSec;
                }
            }
        }

        #region IDisposable
        public void Dispose()
        {
            lock (this)
            {
                disposed = true;
                var toDispose = new List<SearcherTracker>(searchers.Values);

                foreach (var tracker in toDispose)
                {
                    searchers.Remove(tracker.Version);
                }

                IOUtils.Close(toDispose);

                if (searchers.Count != 0)
                    throw new InvalidOperationException("another thread called record while this SearcherLifetimeManager instance was being disposed; not all searchers were disposed");
            }
        }
        #endregion
    }
}
