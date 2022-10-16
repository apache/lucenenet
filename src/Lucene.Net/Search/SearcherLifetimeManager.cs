using J2N;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// Keeps track of current plus old <see cref="IndexSearcher"/>s, disposing
    /// the old ones once they have timed out.
    ///
    /// Use it like this:
    ///
    /// <code>
    ///     SearcherLifetimeManager mgr = new SearcherLifetimeManager();
    /// </code>
    ///
    /// Per search-request, if it's a "new" search request, then
    /// obtain the latest searcher you have (for example, by
    /// using <see cref="SearcherManager"/>), and then record this
    /// searcher:
    ///
    /// <code>
    ///     // Record the current searcher, and save the returend
    ///     // token into user's search results (eg as a  hidden
    ///     // HTML form field):
    ///     long token = mgr.Record(searcher);
    /// </code>
    ///
    /// When a follow-up search arrives, for example the user
    /// clicks next page, drills down/up, etc., take the token
    /// that you saved from the previous search and:
    ///
    /// <code>
    ///     // If possible, obtain the same searcher as the last
    ///     // search:
    ///     IndexSearcher searcher = mgr.Acquire(token);
    ///     if (searcher != null) 
    ///     {
    ///         // Searcher is still here
    ///         try 
    ///         {
    ///             // do searching...
    ///         } 
    ///         finally 
    ///         {
    ///             mgr.Release(searcher);
    ///             // Do not use searcher after this!
    ///             searcher = null;
    ///         }
    ///     } 
    ///     else 
    ///     {
    ///         // Searcher was pruned -- notify user session timed
    ///         // out, or, pull fresh searcher again
    ///     }
    /// </code>
    ///
    /// Finally, in a separate thread, ideally the same thread
    /// that's periodically reopening your searchers, you should
    /// periodically prune old searchers:
    ///
    /// <code>
    ///     mgr.Prune(new PruneByAge(600.0));
    /// </code>
    ///
    /// <para><b>NOTE</b>: keeping many searchers around means
    /// you'll use more resources (open files, RAM) than a single
    /// searcher.  However, as long as you are using 
    /// <see cref="DirectoryReader.OpenIfChanged(DirectoryReader)"/>, the searchers
    /// will usually share almost all segments and the added resource usage
    /// is contained.  When a large merge has completed, and
    /// you reopen, because that is a large change, the new
    /// searcher will use higher additional RAM than other
    /// searchers; but large merges don't complete very often and
    /// it's unlikely you'll hit two of them in your expiration
    /// window.  Still you should budget plenty of heap in the
    /// runtime to have a good safety margin.</para>
    /// </summary>

    public class SearcherLifetimeManager : IDisposable
    {
        internal const double NANOS_PER_SEC = 1000000000.0;

        private sealed class SearcherTracker : IComparable<SearcherTracker>, IDisposable
        {
            public IndexSearcher Searcher { get; private set; }
            public double RecordTimeSec { get; private set; }
            public long Version { get; private set; }

            public SearcherTracker(IndexSearcher searcher)
            {
                Searcher = searcher;
                Version = ((DirectoryReader)searcher.IndexReader).Version;
                searcher.IndexReader.IncRef();
                // Use nanoTime not currentTimeMillis since it [in
                // theory] reduces risk from clock shift
                RecordTimeSec = Time.NanoTime() / NANOS_PER_SEC;
            }

            // Newer searchers are sort before older ones:
            public int CompareTo(SearcherTracker other)
            {
                return other.RecordTimeSec.CompareTo(RecordTimeSec);
            }

            public void Dispose()
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    Searcher.IndexReader.DecRef();
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        private volatile bool _closed;

        // TODO: we could get by w/ just a "set"; need to have
        // Tracker hash by its version and have compareTo(Long)
        // compare to its version
        // LUCENENET specific - use Lazy<T> to make the create operation atomic. See #417.
        private readonly ConcurrentDictionary<long, Lazy<SearcherTracker>> _searchers = new ConcurrentDictionary<long, Lazy<SearcherTracker>>();

        private void EnsureOpen()
        {
            if (_closed)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this SearcherLifetimeManager instance is disposed.");
            }
        }

        /// <summary>
        /// Records that you are now using this <see cref="IndexSearcher"/>.
        /// Always call this when you've obtained a possibly new
        /// <see cref="IndexSearcher"/>, for example from 
        /// <see cref="SearcherManager"/>.  It's fine if you already passed the
        /// same searcher to this method before.
        ///
        /// <para>This returns the <see cref="long"/> token that you can later pass
        /// to <see cref="Acquire(long)"/> to retrieve the same <see cref="IndexSearcher"/>.
        /// You should record this <see cref="long"/> token in the search results
        /// sent to your user, such that if the user performs a
        /// follow-on action (clicks next page, drills down, etc.)
        /// the token is returned.</para>
        /// </summary>
        public virtual long Record(IndexSearcher searcher)
        {
            EnsureOpen();
            // TODO: we don't have to use IR.getVersion to track;
            // could be risky (if it's buggy); we could get better
            // bug isolation if we assign our own private ID:
            var version = ((DirectoryReader)searcher.IndexReader).Version;
            var factoryMethodCalled = false;
            var tracker = _searchers.GetOrAdd(version, l => new Lazy<SearcherTracker>(() => { factoryMethodCalled = true; return new SearcherTracker(searcher); })).Value;
            if (!factoryMethodCalled && tracker.Searcher != searcher)
            {
                throw new ArgumentException("the provided searcher has the same underlying reader version yet the searcher instance differs from before (new=" + searcher + " vs old=" + tracker.Searcher);
            }

            return version;
        }

        /// <summary>
        /// Retrieve a previously recorded <see cref="IndexSearcher"/>, if it
        /// has not yet been closed.
        ///
        /// <para><b>NOTE</b>: this may return <c>null</c> when the
        /// requested searcher has already timed out.  When this
        /// happens you should notify your user that their session
        /// timed out and that they'll have to restart their
        /// search.</para>
        ///
        /// <para>If this returns a non-null result, you must match
        /// later call <see cref="Release(IndexSearcher)"/> on this searcher, best
        /// from a finally clause.</para>
        /// </summary>
        public virtual IndexSearcher Acquire(long version)
        {
            EnsureOpen();
            if (_searchers.TryGetValue(version, out Lazy<SearcherTracker> tracker) && tracker.IsValueCreated && tracker.Value.Searcher.IndexReader.TryIncRef())
            {
                return tracker.Value.Searcher;
            }

            return null;
        }

        /// <summary>
        /// Release a searcher previously obtained from 
        /// <see cref="Acquire(long)"/>.
        ///
        /// <para/><b>NOTE</b>: it's fine to call this after Dispose().
        /// </summary>
        public virtual void Release(IndexSearcher s)
        {
            s.IndexReader.DecRef();
        }

        /// <summary>
        /// See <see cref="Prune(IPruner)"/>. </summary>
        public interface IPruner
        {
            /// <summary>
            /// Return <c>true</c> if this searcher should be removed. </summary>
            ///  <param name="ageSec"> How much time has passed since this
            ///         searcher was the current (live) searcher </param>
            ///  <param name="searcher"> Searcher </param>
            bool DoPrune(double ageSec, IndexSearcher searcher);
        }

        /// <summary>
        /// Simple pruner that drops any searcher older by
        /// more than the specified seconds, than the newest
        /// searcher.
        /// </summary>
        public sealed class PruneByAge : IPruner
        {
            private readonly double maxAgeSec;

            public PruneByAge(double maxAgeSec)
            {
                if (maxAgeSec < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxAgeSec), "maxAgeSec must be > 0 (got " + maxAgeSec + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                this.maxAgeSec = maxAgeSec;
            }

            public bool DoPrune(double ageSec, IndexSearcher searcher)
            {
                return ageSec > maxAgeSec;
            }
        }

        /// <summary>
        /// Calls provided <see cref="IPruner"/> to prune entries.  The
        /// entries are passed to the <see cref="IPruner"/> in sorted (newest to
        /// oldest <see cref="IndexSearcher"/>) order.
        ///
        /// <para/><b>NOTE</b>: you must peridiocally call this, ideally
        /// from the same background thread that opens new
        /// searchers.
        /// </summary>
        public virtual void Prune(IPruner pruner)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // Cannot just pass searchers.values() to ArrayList ctor
                // (not thread-safe since the values can change while
                // ArrayList is init'ing itself); must instead iterate
                // ourselves:
                var trackers = _searchers.Values.Select(item => item.Value).ToList();
                trackers.Sort();
                var lastRecordTimeSec = 0.0;
                double now = Time.NanoTime() / NANOS_PER_SEC;
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
                    // First tracker is always age 0.0 sec, since it's
                    // still "live"; second tracker's age (= seconds since
                    // it was "live") is now minus first tracker's
                    // recordTime, etc:
                    if (pruner.DoPrune(ageSec, tracker.Searcher))
                    {
                        //System.out.println("PRUNE version=" + tracker.version + " age=" + ageSec + " ms=" + Time.CurrentTimeMilliseconds());
                        Lazy<SearcherTracker> _;
                        _searchers.TryRemove(tracker.Version, out _);
                        tracker.Dispose();
                    }
                    lastRecordTimeSec = tracker.RecordTimeSec;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Close this to future searching; any searches still in
        /// process in other threads won't be affected, and they
        /// should still call <see cref="Release(IndexSearcher)"/> after they are
        /// done.
        ///
        /// <para/><b>NOTE</b>: you must ensure no other threads are
        /// calling <see cref="Record(IndexSearcher)"/> while you call Dispose();
        /// otherwise it's possible not all searcher references
        /// will be freed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by the <see cref="SearcherLifetimeManager"/> and
        /// if overridden in a derived class, optionally releases unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>

        // LUCENENET specific - implemented proper dispose pattern
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    _closed = true;
                    IList<SearcherTracker> toClose = new JCG.List<SearcherTracker>(_searchers.Values.Select(item => item.Value));

                    // Remove up front in case exc below, so we don't
                    // over-decRef on double-close:
                    foreach (var tracker in toClose)
                    {
                        _searchers.TryRemove(tracker.Version, out Lazy<SearcherTracker> _);
                    }

                    IOUtils.Dispose(toClose);

                    // Make some effort to catch mis-use:
                    if (_searchers.Count != 0)
                    {
                        throw IllegalStateException.Create("another thread called record while this SearcherLifetimeManager instance was being disposed; not all searchers were disposed");
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }
    }
}