using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;

    /// <summary>
    /// Utility class to safely share instances of a certain type across multiple
    /// threads, while periodically refreshing them. this class ensures each
    /// reference is closed only once all threads have finished using it. It is
    /// recommended to consult the documentation of <seealso cref="ReferenceManager"/>
    /// implementations for their <seealso cref="#maybeRefresh()"/> semantics.
    /// </summary>
    /// @param <G>
    ///          the concrete type that will be <seealso cref="#acquire() acquired"/> and
    ///          <seealso cref="#release(Object) released"/>.
    ///
    /// @lucene.experimental </param>
    public abstract class ReferenceManager<G> : IDisposable
        where G : class //Make G nullable
    {
        private const string REFERENCE_MANAGER_IS_CLOSED_MSG = "this ReferenceManager is closed";

        // LUCENENET NOTE: changed this to be a private volatile field
        // with a property to set/get it, since protected volatile 
        // fields are not CLS compliant
        private volatile G current;

        protected G Current
        {
            get { return current; }
            set { current = value; }
        }

        private readonly ReentrantLock refreshLock = new ReentrantLock();

        private readonly ISet<ReferenceManager.IRefreshListener> refreshListeners = new ConcurrentHashSet<ReferenceManager.IRefreshListener>();

        private void EnsureOpen()
        {
            if (current == null)
            {
                throw new AlreadyClosedException(REFERENCE_MANAGER_IS_CLOSED_MSG);
            }
        }

        private void SwapReference(G newReference)
        {
            lock (this)
            {
                EnsureOpen();
                G oldReference = current;
                current = newReference;
                Release(oldReference);
            }
        }

        /// <summary>
        /// Decrement reference counting on the given reference. </summary>
        /// <exception cref="IOException"> if reference decrement on the given resource failed.
        ///  </exception>
        protected abstract void DecRef(G reference);

        /// <summary>
        /// Refresh the given reference if needed. Returns {@code null} if no refresh
        /// was needed, otherwise a new refreshed reference. </summary>
        /// <exception cref="AlreadyClosedException"> if the reference manager has been <seealso cref="#close() closed"/>. </exception>
        /// <exception cref="IOException"> if the refresh operation failed </exception>
        protected abstract G RefreshIfNeeded(G referenceToRefresh);

        /// <summary>
        /// Try to increment reference counting on the given reference. Return true if
        /// the operation was successful. </summary>
        /// <exception cref="AlreadyClosedException"> if the reference manager has been <seealso cref="#close() closed"/>.  </exception>
        protected abstract bool TryIncRef(G reference);

        /// <summary>
        /// Obtain the current reference. You must match every call to acquire with one
        /// call to <seealso cref="#release"/>; it's best to do so in a finally clause, and set
        /// the reference to {@code null} to prevent accidental usage after it has been
        /// released. </summary>
        /// <exception cref="AlreadyClosedException"> if the reference manager has been <seealso cref="#close() closed"/>.  </exception>
        public G Acquire()
        {
            G @ref;

            do
            {
                if ((@ref = current) == null)
                {
                    throw new AlreadyClosedException(REFERENCE_MANAGER_IS_CLOSED_MSG);
                }
                if (TryIncRef(@ref))
                {
                    return @ref;
                }
                if (GetRefCount(@ref) == 0 && (object)current == (object)@ref)
                {
                    Debug.Assert(@ref != null);
                    /* if we can't increment the reader but we are
                       still the current reference the RM is in a
                       illegal states since we can't make any progress
                       anymore. The reference is closed but the RM still
                       holds on to it as the actual instance.
                       this can only happen if somebody outside of the RM
                       decrements the refcount without a corresponding increment
                       since the RM assigns the new reference before counting down
                       the reference. */
                    throw new InvalidOperationException("The managed reference has already closed - this is likely a bug when the reference count is modified outside of the ReferenceManager");
                }
            } while (true);
        }

        /// <summary>
        /// <p>
        /// Closes this ReferenceManager to prevent future <seealso cref="#acquire() acquiring"/>. A
        /// reference manager should be closed if the reference to the managed resource
        /// should be disposed or the application using the <seealso cref="ReferenceManager"/>
        /// is shutting down. The managed resource might not be released immediately,
        /// if the <seealso cref="ReferenceManager"/> user is holding on to a previously
        /// <seealso cref="#acquire() acquired"/> reference. The resource will be released once
        /// when the last reference is <seealso cref="#release(Object) released"/>. Those
        /// references can still be used as if the manager was still active.
        /// </p>
        /// <p>
        /// Applications should not <seealso cref="#acquire() acquire"/> new references from this
        /// manager once this method has been called. <seealso cref="#acquire() Acquiring"/> a
        /// resource on a closed <seealso cref="ReferenceManager"/> will throw an
        /// <seealso cref="AlreadyClosedException"/>.
        /// </p>
        /// </summary>
        /// <exception cref="IOException">
        ///           if the underlying reader of the current reference could not be closed </exception>
        public void Dispose()
        {
            lock (this)
            {
                if (current != null)
                {
                    // make sure we can call this more than once
                    // closeable javadoc says:
                    // if this is already closed then invoking this method has no effect.
                    SwapReference(null);
                    AfterClose();
                }
            }
        }

        /// <summary>
        /// Returns the current reference count of the given reference.
        /// </summary>
        protected abstract int GetRefCount(G reference);

        /// <summary>
        ///  Called after close(), so subclass can free any resources. </summary>
        ///  <exception cref="IOException"> if the after close operation in a sub-class throws an <seealso cref="IOException"/>
        ///  </exception>
        protected internal virtual void AfterClose()
        {
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
                        Debug.Assert((object)newReference != (object)reference, "refreshIfNeeded should return null if refresh wasn't needed");
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
        /// You must call this (or <seealso cref="#maybeRefreshBlocking()"/>), periodically, if
        /// you want that <seealso cref="#acquire()"/> will return refreshed instances.
        ///
        /// <p>
        /// <b>Threads</b>: it's fine for more than one thread to call this at once.
        /// Only the first thread will attempt the refresh; subsequent threads will see
        /// that another thread is already handling refresh and will return
        /// immediately. Note that this means if another thread is already refreshing
        /// then subsequent threads will return right away without waiting for the
        /// refresh to complete.
        ///
        /// <p>
        /// If this method returns true it means the calling thread either refreshed or
        /// that there were no changes to refresh. If it returns false it means another
        /// thread is currently refreshing.
        /// </p> </summary>
        /// <exception cref="IOException"> if refreshing the resource causes an <seealso cref="IOException"/> </exception>
        /// <exception cref="AlreadyClosedException"> if the reference manager has been <seealso cref="#close() closed"/>.  </exception>
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
        /// You must call this (or <seealso cref="#maybeRefresh()"/>), periodically, if you want
        /// that <seealso cref="#acquire()"/> will return refreshed instances.
        ///
        /// <p>
        /// <b>Threads</b>: unlike <seealso cref="#maybeRefresh()"/>, if another thread is
        /// currently refreshing, this method blocks until that thread completes. It is
        /// useful if you want to guarantee that the next call to <seealso cref="#acquire()"/>
        /// will return a refreshed instance. Otherwise, consider using the
        /// non-blocking <seealso cref="#maybeRefresh()"/>. </summary>
        /// <exception cref="IOException"> if refreshing the resource causes an <seealso cref="IOException"/> </exception>
        /// <exception cref="AlreadyClosedException"> if the reference manager has been <seealso cref="#close() closed"/>.  </exception>
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
        ///  whether a new reference was in fact created. </summary>
        ///  <exception cref="IOException"> if a low level I/O exception occurs
        ///  </exception>
        protected virtual void AfterMaybeRefresh()
        {
        }

        /// <summary>
        /// Release the reference previously obtained via <seealso cref="#acquire()"/>.
        /// <p>
        /// <b>NOTE:</b> it's safe to call this after <seealso cref="#close()"/>. </summary>
        /// <exception cref="IOException"> if the release operation on the given resource throws an <seealso cref="IOException"/> </exception>
        public void Release(G reference)
        {
            Debug.Assert(reference != null);
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
            if (listener == null)
            {
                throw new System.NullReferenceException("Listener cannot be null");
            }
            refreshListeners.Add(listener);
        }

        /// <summary>
        /// Remove a listener added with <seealso cref="#addListener(RefreshListener)"/>.
        /// </summary>
        public virtual void RemoveListener(ReferenceManager.IRefreshListener listener)
        {
            if (listener == null)
            {
                throw new System.NullReferenceException("Listener cannot be null");
            }
            refreshListeners.Remove(listener);
        }
    }

    // .NET Port: non-generic type to hold RefreshListener
    public static class ReferenceManager
    {
        /// <summary>
        /// Use to receive notification when a refresh has
        ///  finished.  See <seealso cref="#addListener"/>.
        /// </summary>
        public interface IRefreshListener
        {
            /// <summary>
            /// Called right before a refresh attempt starts. </summary>
            void BeforeRefresh();

            /// <summary>
            /// Called after the attempted refresh; if the refresh
            /// did open a new reference then didRefresh will be true
            /// and <seealso cref="#acquire()"/> is guaranteed to return the new
            /// reference.
            /// </summary>
            void AfterRefresh(bool didRefresh);
        }
    }
}