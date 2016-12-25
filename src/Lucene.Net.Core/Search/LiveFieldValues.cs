using System;
using System.Collections.Concurrent;
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

    /// <summary>
    /// Tracks live field values across NRT reader reopens.
    ///  this holds a map for all updated ids since
    ///  the last reader reopen.  Once the NRT reader is reopened,
    ///  it prunes the map.  this means you must reopen your NRT
    ///  reader periodically otherwise the RAM consumption of
    ///  this class will grow unbounded!
    ///
    ///  <p>NOTE: you must ensure the same id is never updated at
    ///  the same time by two threads, because in this case you
    ///  cannot in general know which thread "won".
    /// </summary>

    public abstract class LiveFieldValues<S, T> : ReferenceManager.IRefreshListener, IDisposable
        where S : class
    {
        private volatile IDictionary<string, T> current = new ConcurrentDictionary<string, T>();
        private volatile IDictionary<string, T> old = new ConcurrentDictionary<string, T>();
        private readonly ReferenceManager<S> mgr;
        private readonly T missingValue;

        public LiveFieldValues(ReferenceManager<S> mgr, T missingValue)
        {
            this.missingValue = missingValue;
            this.mgr = mgr;
            mgr.AddListener(this);
        }

        public void Dispose()
        {
            mgr.RemoveListener(this);
        }

        public virtual void BeforeRefresh()
        {
            old = current;
            // Start sending all updates after this point to the new
            // map.  While reopen is running, any lookup will first
            // try this new map, then fallback to old, then to the
            // current searcher:
            current = new ConcurrentDictionary<string, T>();
        }

        public virtual void AfterRefresh(bool didRefresh)
        {
            // Now drop all the old values because they are now
            // visible via the searcher that was just opened; if
            // didRefresh is false, it's possible old has some
            // entries in it, which is fine: it means they were
            // actually already included in the previously opened
            // reader.  So we can safely clear old here:
            old = new ConcurrentDictionary<string, T>();
        }

        /// <summary>
        /// Call this after you've successfully added a document
        ///  to the index, to record what value you just set the
        ///  field to.
        /// </summary>
        public virtual void Add(string id, T value)
        {
            current[id] = value;
        }

        /// <summary>
        /// Call this after you've successfully deleted a document
        ///  from the index.
        /// </summary>
        public virtual void Delete(string id)
        {
            current[id] = missingValue;
        }

        /// <summary>
        /// Returns the [approximate] number of id/value pairs
        ///  buffered in RAM.
        /// </summary>
        public virtual int Size // LUCENENET TODO: rename Count
        {
            get { return current.Count + old.Count; }
        }

        /// <summary>
        /// Returns the current value for this id, or null if the
        ///  id isn't in the index or was deleted.
        /// </summary>
        public virtual T Get(string id)
        {
            // First try to get the "live" value:
            T value;
            current.TryGetValue(id, out value);
            if (EqualityComparer<T>.Default.Equals(value, missingValue))
            {
                // Deleted but the deletion is not yet reflected in
                // the reader:
                return default(T);
            }
            else if (!EqualityComparer<T>.Default.Equals(value, default(T)))
            {
                return value;
            }
            else
            {
                old.TryGetValue(id, out value);
                if (EqualityComparer<T>.Default.Equals(value, missingValue))
                {
                    // Deleted but the deletion is not yet reflected in
                    // the reader:
                    return default(T);
                }
                else if (!EqualityComparer<T>.Default.Equals(value, default(T)))
                {
                    return value;
                }
                else
                {
                    // It either does not exist in the index, or, it was
                    // already flushed & NRT reader was opened on the
                    // segment, so fallback to current searcher:
                    S s = mgr.Acquire();
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

        /// <summary>
        /// this is called when the id/value was already flushed & opened
        ///  in an NRT IndexSearcher.  You must implement this to
        ///  go look up the value (eg, via doc values, field cache,
        ///  stored fields, etc.).
        /// </summary>
        protected abstract T LookupFromSearcher(S s, string id);
    }
}