using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.IO;

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
    /// Utility class to safely share instances of a certain type across multiple
    /// threads, while periodically refreshing them. This class ensures each
    /// reference is closed only once all threads have finished using it. It is
    /// recommended to consult the documentation of <see cref="ReferenceManager{G}"/>
    /// implementations for their <see cref="MaybeRefresh()"/> semantics.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <typeparam name="G">The concrete type that will be <see cref="Acquire()"/>d and
    ///          <see cref="Release(G)"/>d.</typeparam>
    public abstract class ReferenceManager<G> : IDisposable
        where G : class //Make G nullable
    {
        private const string REFERENCE_MANAGER_IS_CLOSED_MSG = "this ReferenceManager is disposed.";

        // LUCENENET NOTE: changed this to be a private volatile field
        // with a property to set/get it, since protected volatile 
        // fields are not CLS compliant
        private volatile G current;

        /// <summary>
        /// The current reference
        /// </summary>
        protected G Current
        {
            get => current;
            set => current = value;
        }

        private readonly ReentrantLock refreshLock = new ReentrantLock();

        private readonly ISet<ReferenceManager.IRefreshListener> refreshListeners = new ConcurrentHashSet<ReferenceManager.IRefreshListener>();

        private void EnsureOpen()
        {
            if (current is null)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, REFERENCE_MANAGER_IS_CLOSED_MSG);
            }
        }

        private void SwapReference(G newReference)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                EnsureOpen();
                G oldReference = current;
                current = newReference;
                Release(oldReference);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Decrement reference counting on the given reference. </summary>
        /// <exception cref="IOException"> If reference decrement on the given resource failed.</exception>
        protected abstract void DecRef(G reference);

        /// <summary>
        /// Refresh the given reference if needed. Returns <c>null</c> if no refresh
        /// was needed, otherwise a new refreshed reference. </summary>
        /// <exception cref="ObjectDisposedException"> If the reference manager has been <see cref="Dispose()"/>d. </exception>
        /// <exception cref="IOException"> If the refresh operation failed </exception>
        protected abstract G RefreshIfNeeded(G referenceToRefresh);

        /// <summary>
        /// Try to increment reference counting on the given reference. Returns <c>true</c> if
        /// the operation was successful. </summary>
        /// <exception cref="ObjectDisposedException"> if the reference manager has been <see cref="Dispose()"/>d.  </exception>
        protected abstract bool TryIncRef(G reference);

        /// <summary>
        /// Obtain the current reference. You must match every call to acquire with one
        /// call to <see cref="Release(G)"/>; it's best to do so in a finally clause, and set
        /// the reference to <c>null</c> to prevent accidental usage after it has been
        /// released. </summary>
        /// <exception cref="ObjectDisposedException"> If the reference manager has been <see cref="Dispose()"/>d.  </exception>
        public G Acquire()
        {
            G @ref;

            do
            {
                if ((@ref = current) is null)
                {
                    throw AlreadyClosedException.Create(this.GetType().FullName, REFERENCE_MANAGER_IS_CLOSED_MSG);
                }
                if (TryIncRef(@ref))
                {
                    return @ref;
                }
                if (GetRefCount(@ref) == 0 && (object)current == (object)@ref)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(@ref != null);
                    /* if we can't increment the reader but we are
                       still the current reference the RM is in a
                       illegal states since we can't make any progress
                       anymore. The reference is closed but the RM still
                       holds on to it as the actual instance.
                       this can only happen if somebody outside of the RM
                       decrements the refcount without a corresponding increment
                       since the RM assigns the new reference before counting down
                       the reference. */
                    throw IllegalStateException.Create("The managed reference has already closed - this is likely a bug when the reference count is modified outside of the ReferenceManager");
                }
            } while (true);
        }

        /// <summary>
        /// <para>
        /// Closes this ReferenceManager to prevent future <see cref="Acquire()"/>ing. A
        /// reference manager should be disposed if the reference to the managed resource
        /// should be disposed or the application using the <see cref="ReferenceManager{G}"/>
        /// is shutting down. The managed resource might not be released immediately,
        /// if the <see cref="ReferenceManager{G}"/> user is holding on to a previously
        /// <see cref="Acquire()"/>d reference. The resource will be released once
        /// when the last reference is <see cref="Release(G)"/>d. Those
        /// references can still be used as if the manager was still active.
        /// </para>
        /// <para>
        /// Applications should not <see cref="Acquire()"/> new references from this
        /// manager once this method has been called. <see cref="Acquire()"/>ing a
        /// resource on a disposed <see cref="ReferenceManager{G}"/> will throw an
        /// <seealso cref="ObjectDisposedException"/>.
        /// </para>
        /// </summary>
        /// <exception cref="IOException">
        ///           If the underlying reader of the current reference could not be disposed </exception>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns the current reference count of the given reference.
        /// </summary>
        protected abstract int GetRefCount(G reference);

        /// <summary>
        /// Called after <see cref="Dispose()"/>, so subclass can free any resources.
        /// <para/>
        /// When overriding, be sure to include a call to <c>base.Dispose(disposing)</c> in your implementation.</summary>
        /// <exception cref="IOException"> if the after dispose operation in a sub-class throws an <see cref="IOException"/>
        /// </exception>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (current != null)
                    {
                        // make sure we can call this more than once
                        // closeable javadoc says:
                        // if this is already closed then invoking this method has no effect.
                        SwapReference(null);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        private void DoMaybeRefresh()
        {
            // it's ok to call lock() here (blocking) because we're supposed to get here
            // from either maybeRefreh() or maybeRefreshBlocking(), after the lock has
            // already been obtained. Doing that protects us from an accidental bug
            // where this method will be called outside the scope of refreshLock.
            // Per ReentrantLock's javadoc, calling lock() by the same thread more than
            // once is ok, as long as unlock() is called a matching number of times.
            refreshLock.Lock();
            bool refreshed = false;
            try
            {
                G reference = Acquire();
                try
                {
                    NotifyRefreshListenersBefore();
                    G newReference = RefreshIfNeeded(reference);
                    if (newReference != null)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(!ReferenceEquals(newReference, reference), "refreshIfNeeded should return null if refresh wasn't needed");
                        try
                        {
                            SwapReference(newReference);
                            refreshed = true;
                        }
                        finally
                        {
                            if (!refreshed)
                            {
                                Release(newReference);
                            }
                        }
                    }
                }
                finally
                {
                    Release(reference);
                    NotifyRefreshListenersRefreshed(refreshed);
                }
                AfterMaybeRefresh();
            }
            finally
            {
                refreshLock.Unlock();
            }
        }

        /// <summary>
        /// You must call this (or <see cref="MaybeRefreshBlocking()"/>), periodically, if
        /// you want that <see cref="Acquire()"/> will return refreshed instances.
        ///
        /// <para>
        /// <b>Threads</b>: it's fine for more than one thread to call this at once.
        /// Only the first thread will attempt the refresh; subsequent threads will see
        /// that another thread is already handling refresh and will return
        /// immediately. Note that this means if another thread is already refreshing
        /// then subsequent threads will return right away without waiting for the
        /// refresh to complete.
        /// </para>
        /// <para>
        /// If this method returns <c>true</c> it means the calling thread either refreshed or
        /// that there were no changes to refresh. If it returns <c>false</c> it means another
        /// thread is currently refreshing.
        /// </para> </summary>
        /// <exception cref="IOException"> If refreshing the resource causes an <see cref="IOException"/> </exception>
        /// <exception cref="ObjectDisposedException"> If the reference manager has been <see cref="Dispose()"/>d.  </exception>
        public bool MaybeRefresh()
        {
            EnsureOpen();

            // Ensure only 1 thread does refresh at once; other threads just return immediately:
            bool doTryRefresh = refreshLock.TryLock();
            if (doTryRefresh)
            {
                try
                {
                    DoMaybeRefresh();
                }
                finally
                {
                    refreshLock.Unlock();
                }
            }

            return doTryRefresh;
        }

        /// <summary>
        /// You must call this (or <see cref="MaybeRefresh()"/>), periodically, if you want
        /// that <see cref="Acquire()"/> will return refreshed instances.
        ///
        /// <para/>
        /// <b>Threads</b>: unlike <see cref="MaybeRefresh()"/>, if another thread is
        /// currently refreshing, this method blocks until that thread completes. It is
        /// useful if you want to guarantee that the next call to <see cref="Acquire()"/>
        /// will return a refreshed instance. Otherwise, consider using the
        /// non-blocking <see cref="MaybeRefresh()"/>. </summary>
        /// <exception cref="IOException"> If refreshing the resource causes an <see cref="IOException"/> </exception>
        /// <exception cref="ObjectDisposedException"> If the reference manager has been <see cref="Dispose()"/>d.  </exception>
        public void MaybeRefreshBlocking()
        {
            EnsureOpen();

            // Ensure only 1 thread does refresh at once
            refreshLock.Lock();
            try
            {
                DoMaybeRefresh();
            }
            finally
            {
                refreshLock.Unlock();
            }
        }

        /// <summary>
        /// Called after a refresh was attempted, regardless of
        /// whether a new reference was in fact created. </summary>
        /// <exception cref="IOException"> if a low level I/O exception occurs</exception>
        protected virtual void AfterMaybeRefresh()
        {
        }

        /// <summary>
        /// Release the reference previously obtained via <see cref="Acquire()"/>.
        /// <para/>
        /// <b>NOTE:</b> it's safe to call this after <see cref="Dispose()"/>. </summary>
        /// <exception cref="IOException"> If the release operation on the given resource throws an <see cref="IOException"/> </exception>
        public void Release(G reference)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(!(reference is null));
            DecRef(reference);
        }

        private void NotifyRefreshListenersBefore()
        {
            foreach (ReferenceManager.IRefreshListener refreshListener in refreshListeners)
            {
                refreshListener.BeforeRefresh();
            }
        }

        private void NotifyRefreshListenersRefreshed(bool didRefresh)
        {
            foreach (ReferenceManager.IRefreshListener refreshListener in refreshListeners)
            {
                refreshListener.AfterRefresh(didRefresh);
            }
        }

        /// <summary>
        /// Adds a listener, to be notified when a reference is refreshed/swapped.
        /// </summary>
        public virtual void AddListener(ReferenceManager.IRefreshListener listener)
        {
            if (listener is null)
            {
                throw new ArgumentNullException(nameof(listener), "Listener cannot be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            refreshListeners.Add(listener);
        }

        /// <summary>
        /// Remove a listener added with <see cref="AddListener(ReferenceManager.IRefreshListener)"/>.
        /// </summary>
        public virtual void RemoveListener(ReferenceManager.IRefreshListener listener)
        {
            if (listener is null)
            {
                throw new ArgumentNullException(nameof(listener), "Listener cannot be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            refreshListeners.Remove(listener);
        }
    }

    /// <summary>
    /// LUCENENET specific class used to provide static access to <see cref="ReferenceManager.IRefreshListener"/>
    /// without having to specifiy the generic closing type of <see cref="ReferenceManager{G}"/>.
    /// </summary>
    public static class ReferenceManager
    {
        /// <summary>
        /// Use to receive notification when a refresh has
        /// finished.  See <see cref="ReferenceManager{G}.AddListener(IRefreshListener)"/>.
        /// </summary>
        public interface IRefreshListener
        {
            /// <summary>
            /// Called right before a refresh attempt starts. </summary>
            void BeforeRefresh();

            /// <summary>
            /// Called after the attempted refresh; if the refresh
            /// did open a new reference then didRefresh will be <c>true</c>
            /// and <see cref="ReferenceManager{G}.Acquire()"/> is guaranteed to return the new
            /// reference.
            /// </summary>
            void AfterRefresh(bool didRefresh);
        }
    }
}