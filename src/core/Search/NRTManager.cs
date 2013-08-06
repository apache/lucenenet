using System;
using System.Collections.Generic;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;

namespace Lucene.Net.Search
{
    public class NRTManager : ReferenceManager<IndexSearcher>
    {
        private const long MAX_SEARCHER_GEN = long.MaxValue;
        private readonly TrackingIndexWriter writer;
        private readonly ISet<IWaitingListener> waitingListeners = new ConcurrentHashSet<IWaitingListener>();
        private readonly SearcherFactory searcherFactory;

        private long searchingGen;

        public NRTManager(TrackingIndexWriter writer, SearcherFactory searcherFactory) : this(writer, searcherFactory, true)
        {
        }

        public NRTManager(TrackingIndexWriter writer, SearcherFactory searcherFactory, bool applyAllDeletes)
        {
            this.writer = writer;
            if (searcherFactory == null)
            {
                searcherFactory = new SearcherFactory();
            }
            this.searcherFactory = searcherFactory;
            current = SearcherManager.GetSearcher(searcherFactory, DirectoryReader.Open(writer.IndexWriter, applyAllDeletes));
        }

        protected override void DecRef(IndexSearcher reference)
        {
            reference.IndexReader.DecRef();
        }

        protected override bool TryIncRef(IndexSearcher reference)
        {
            return reference.IndexReader.TryIncRef();
        }

        public interface IWaitingListener
        {
            void Waiting(long targetGen);
        }

        public virtual void AddWaitingListener(IWaitingListener listener)
        {
            waitingListeners.Add(listener);
        }

        public virtual void RemoveWaitingListener(IWaitingListener listener)
        {
            waitingListeners.Remove(listener);
        }

        public class TrackingIndexWriter
        {
            private readonly IndexWriter writer;
            private long indexingGen = 1;

            public TrackingIndexWriter(IndexWriter writer)
            {
                this.writer = writer;
            }

            public virtual long UpdateDocument(Term t, IEnumerable<IIndexableField> d, Analyzer a)
            {
                writer.UpdateDocument(t, d, a);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long UpdateDocument(Term t, IEnumerable<IIndexableField> d)
            {
                writer.UpdateDocument(t, d);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long UpdateDocuments(Term t, IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer a)
            {
                writer.UpdateDocuments(t, docs, a);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long UpdateDocuments(Term t, IEnumerable<IEnumerable<IIndexableField>> docs)
            {
                writer.UpdateDocuments(t, docs);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long DeleteDocuments(Term t)
            {
                writer.DeleteDocuments(t);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long DeleteDocuments(Term[] terms)
            {
                writer.DeleteDocuments(terms);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long DeleteDocuments(Query q)
            {
                writer.DeleteDocuments(q);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long DeleteDocuments(Query[] queries)
            {
                writer.DeleteDocuments(queries);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long DeleteAll()
            {
                writer.DeleteAll();
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long AddDocument(IEnumerable<IIndexableField> d, Analyzer a)
            {
                writer.AddDocument(d, a);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer a)
            {
                writer.AddDocuments(docs, a);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long AddDocument(IEnumerable<IIndexableField> d)
            {
                writer.AddDocument(d);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs)
            {
                writer.AddDocuments(docs);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long AddIndexes(Directory[] dirs)
            {
                writer.AddIndexes(dirs);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long AddIndexes(IndexReader[] readers)
            {
                writer.AddIndexes(readers);
                return Interlocked.Read(ref indexingGen);
            }

            public virtual long Generation
            {
                get { return Interlocked.Read(ref indexingGen); }
            }

            public virtual IndexWriter IndexWriter
            {
                get { return writer; }
            }

            internal virtual long GetAndIncrementGeneration()
            {
                return Interlocked.Increment(ref indexingGen);
            }

            public virtual long TryDeleteDocument(IndexReader reader, int docID)
            {
                if (writer.TryDeleteDocument(reader, docID))
                {
                    return Interlocked.Read(ref indexingGen);
                }
                else
                {
                    return -1;
                }
            }
        }

        public virtual void WaitForGeneration(long targetGen)
        {
            WaitForGeneration(targetGen, -1, TimeUnit.Nanoseconds);
        }

        public virtual void WaitForGeneration(long targetGen, long time, TimeUnit unit)
        {
            try
            {
                var curGen = writer.Generation;
                if (targetGen > curGen)
                {
                    throw new ArgumentException("targetGen=" + targetGen + " was never returned by this NRTManager instance (current gen=" + curGen + ")");
                }
                Monitor.Enter(this);
                try
                {
                    if (targetGen > Interlocked.Read(ref searchingGen))
                    {
                        foreach (var listener in waitingListeners)
                        {
                            listener.Waiting(targetGen);
                        }
                        while (targetGen > Interlocked.Read(ref searchingGen))
                        {
                            if (!WaitOnGenCondition(time, unit)) return;
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(this);
                }
            }
            catch (ThreadInterruptedException)
            {
                throw;
            }
        }

        private bool WaitOnGenCondition(long time, TimeUnit unit)
        {
            // assert genLock.isHeldByCurrentThread();
            if (time < 0)
            {
                Monitor.Wait(this);
                return true;
            }
            else
            {
                return Monitor.Wait(this, TimeToTimeSpan(time, unit));
            }
        }

        private static TimeSpan TimeToTimeSpan(long time, TimeUnit unit)
        {
            switch (unit)
            {
                case TimeUnit.Seconds: return TimeSpan.FromSeconds(time);
                case TimeUnit.Milliseconds: return TimeSpan.FromMilliseconds(time);
                case TimeUnit.Microseconds: return TimeSpan.FromMilliseconds(time / 1000);
                case TimeUnit.Nanoseconds: return TimeSpan.FromMilliseconds(time / 1000000);
                default: throw new ArgumentException("Unsupported time unit. Must be seconds, milliseconds, microseconds, or nanoseconds.");
            }
        }

        public long CurrentSearchingGen
        {
            get { return Interlocked.Read(ref searchingGen); }
        }

        private long lastRefreshGen;

        protected override IndexSearcher RefreshIfNeeded(IndexSearcher referenceToRefresh)
        {
            lastRefreshGen = writer.GetAndIncrementGeneration();
            var r = referenceToRefresh.IndexReader;
            var reader = r as DirectoryReader;
            if (reader == null) throw new ArgumentException("searcher's IndexReader should be DirectoryReader, but got " + r);
            IndexSearcher newSearcher = null;
            if (!reader.IsCurrent)
            {
                var newReader = DirectoryReader.OpenIfChanged(reader);
                if (newReader != null)
                {
                    newSearcher = SearcherManager.GetSearcher(searcherFactory, newReader);
                }
            }

            return newSearcher;
        }

        protected override void AfterMaybeRefresh()
        {
            Monitor.Enter(this);
            try
            {
                if (Interlocked.Read(ref searchingGen) != MAX_SEARCHER_GEN)
                {
                    // assert lastRefreshGen > = searchingGen;
                    Interlocked.Exchange(ref searchingGen, lastRefreshGen);
                }
                Monitor.PulseAll(this);
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        protected override void AfterClose()
        {
            Monitor.Enter(this);
            try
            {
                Interlocked.Exchange(ref searchingGen, MAX_SEARCHER_GEN);
                Monitor.PulseAll(this);
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        public bool IsSearcherCurrent()
        {
            var searcher = Acquire();
            try
            {
                var r = searcher.IndexReader;
                var reader = r as DirectoryReader;
                if (reader == null)
                    throw new ArgumentException("searcher's IndexReader should be a DirectoryReader, but got " + r);
                return reader.IsCurrent;
            }
            finally
            {
                Release(searcher);
            }
        }
    }
}
