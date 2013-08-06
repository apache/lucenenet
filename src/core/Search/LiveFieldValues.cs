using System;
using System.Collections.Generic;
using Lucene.Net.Support;

namespace Lucene.Net.Search
{
    public abstract class LiveFieldValues<T> : ReferenceManager.RefreshListener, IDisposable
        where T : class
    {
        private volatile IDictionary<string, T> current = new ConcurrentHashMap<string, T>();
        private volatile IDictionary<string, T> old = new ConcurrentHashMap<string, T>();
        private readonly ReferenceManager<IndexSearcher> mgr;
        private readonly T missingValue;

        public LiveFieldValues(ReferenceManager<IndexSearcher> mgr, T missingValue)
        {
            this.missingValue = missingValue;
            this.mgr = mgr;
            mgr.AddListener(this);
        }

        public void BeforeRefresh()
        {
            old = current;
            // Start sending all updates after this point to the new
            // map.  While reopen is running, any lookup will first
            // try this new map, then fallback to old, then to the
            // current searcher:
            current = new ConcurrentHashMap<string, T>();
        }

        public void AfterRefresh(bool didRefresh)
        {
            // Now drop all the old values because they are now
            // visible via the searcher that was just opened; if
            // didRefresh is false, it's possible old has some
            // entries in it, which is fine: it means they were
            // actually already included in the previously opened
            // reader.  So we can safely clear old here:
            old = new ConcurrentHashMap<string, T>();
        }

        public void Add(string id, T value)
        {
            current.Add(id, value);
        }

        public void Delete(string id)
        {
            current.Add(id, missingValue);
        }

        public int Size
        {
            get { return current.Count + old.Count; }
        }

        public T this[string id]
        {
            get
            {
                // First try to get the "live" value:
                var value = current[id];
                if (value == missingValue)
                {
                    // Deleted but the deletion is not yet reflected in
                    // the reader:
                    return null;
                }
                else if (value != null)
                {
                    return value;
                }
                else
                {
                    value = old[id];
                    if (value == missingValue)
                    {
                        // Deleted but the deletion is not yet reflected in
                        // the reader:
                        return null;
                    }
                    else if (value != null)
                    {
                        return value;
                    }
                    else
                    {
                        // It either does not exist in the index, or, it was
                        // already flushed & NRT reader was opened on the
                        // segment, so fallback to current searcher:
                        var s = mgr.Acquire();
                        try
                        {
                            return LookupFromSearcher(s, id);
                        }
                        finally
                        {
                            mgr.Release(s);
                        }
                    }
                }
            }
        }

        protected abstract T LookupFromSearcher(IndexSearcher s, String id);

        #region IDisposable
        public void Dispose()
        {
            mgr.RemoveListener(this);
        }
        #endregion
    }
}
