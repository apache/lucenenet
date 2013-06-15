using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Threading;

namespace Lucene.Net.Search
{
	public abstract class ReferenceManager<TG> : IDisposable
        where TG : class
	{
		private static String REFERENCE_MANAGER_IS_CLOSED_MSG = "this ReferenceManager is closed";

		protected volatile TG Current;

		//private Lock refreshLock = new ReentrantLock();
		private readonly object refreshLock = new object();
		private List<RefreshListener> refreshListeners = new List<RefreshListener>();

		private void EnsureOpen()
		{
			if (Current == null)
			{
				throw new AlreadyClosedException(REFERENCE_MANAGER_IS_CLOSED_MSG);
			}
		}

		private void SwapReference(TG newReference)
		{
			lock (this)
			{
				EnsureOpen();
				TG oldReference = Current;
				Current = newReference;
				release(oldReference);
			}
		}

		/**
		 * Decrement reference counting on the given reference. 
		 * @throws IOException if reference decrement on the given resource failed.
		 * */
		protected abstract void DecRef(TG reference);

		/**
		 * Refresh the given reference if needed. Returns {@code null} if no refresh
		 * was needed, otherwise a new refreshed reference.
		 * @throws AlreadyClosedException if the reference manager has been {@link #close() closed}.
		 * @throws IOException if the refresh operation failed
		 */
		protected abstract TG RefreshIfNeeded(TG referenceToRefresh);

		/**
		 * Try to increment reference counting on the given reference. Return true if
		 * the operation was successful.
		 * @throws AlreadyClosedException if the reference manager has been {@link #close() closed}. 
		 */
		protected abstract bool TryIncRef(TG reference);

		/**
		 * Obtain the current reference. You must match every call to acquire with one
		 * call to {@link #release}; it's best to do so in a finally clause, and set
		 * the reference to {@code null} to prevent accidental usage after it has been
		 * released.
		 * @throws AlreadyClosedException if the reference manager has been {@link #close() closed}. 
		 */
		public TG Acquire()
		{
			TG refG;
			do
			{
				if ((refG = Current) == null)
				{
					throw new AlreadyClosedException(REFERENCE_MANAGER_IS_CLOSED_MSG);
				}
			} while (!TryIncRef(refG));
			return refG;
		}

		/**
		  * <p>
		  * Closes this ReferenceManager to prevent future {@link #acquire() acquiring}. A
		  * reference manager should be closed if the reference to the managed resource
		  * should be disposed or the application using the {@link ReferenceManager}
		  * is shutting down. The managed resource might not be released immediately,
		  * if the {@link ReferenceManager} user is holding on to a previously
		  * {@link #acquire() acquired} reference. The resource will be released once
		  * when the last reference is {@link #release(Object) released}. Those
		  * references can still be used as if the manager was still active.
		  * </p>
		  * <p>
		  * Applications should not {@link #acquire() acquire} new references from this
		  * manager once this method has been called. {@link #acquire() Acquiring} a
		  * resource on a closed {@link ReferenceManager} will throw an
		  * {@link AlreadyClosedException}.
		  * </p>
		  * 
		  * @throws IOException
		  *           if the underlying reader of the current reference could not be closed
		 */

		public void Close()
		{
			lock (this)
			{
				if (Current != null)
				{
					// make sure we can call this more than once
					// closeable javadoc says:
					// if this is already closed then invoking this method has no effect.
					SwapReference(null);
					AfterClose();
				}
			}
		}

		/**
		 *  Called after close(), so subclass can free any resources.
		 *  @throws IOException if the after close operation in a sub-class throws an {@link IOException} 
		 * */
		protected void AfterClose()
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
			//refreshLock.lock();
			Monitor.Enter(refreshLock);
			bool refreshed = false;
			try
			{
				TG reference = Acquire();
				try
				{
					notifyRefreshListenersBefore();
					TG newReference = RefreshIfNeeded(reference);
					if (newReference != null)
					{
						//assert newReference != reference : "refreshIfNeeded should return null if refresh wasn't needed";
						try
						{
							SwapReference(newReference);
							refreshed = true;
						}
						finally
						{
							if (!refreshed)
							{
								release(newReference);
							}
						}
					}
				}
				finally
				{
					release(reference);
					notifyRefreshListenersRefreshed(refreshed);
				}
				AfterMaybeRefresh();
			}
			finally
			{
				//refreshLock.unlock();
				Monitor.Exit(refreshLock);
			}
		}

		/**
		 * You must call this (or {@link #maybeRefreshBlocking()}), periodically, if
		 * you want that {@link #acquire()} will return refreshed instances.
		 * 
		 * <p>
		 * <b>Threads</b>: it's fine for more than one thread to call this at once.
		 * Only the first thread will attempt the refresh; subsequent threads will see
		 * that another thread is already handling refresh and will return
		 * immediately. Note that this means if another thread is already refreshing
		 * then subsequent threads will return right away without waiting for the
		 * refresh to complete.
		 * 
		 * <p>
		 * If this method returns true it means the calling thread either refreshed or
		 * that there were no changes to refresh. If it returns false it means another
		 * thread is currently refreshing.
		 * </p>
		 * @throws IOException if refreshing the resource causes an {@link IOException}
		 * @throws AlreadyClosedException if the reference manager has been {@link #close() closed}. 
		 */
		public bool MaybeRefresh()
		{
			EnsureOpen();

			// Ensure only 1 thread does refresh at once; other threads just return immediately:
			//bool doTryRefresh = refreshLock.tryLock();
			bool doTryRefresh = Monitor.TryEnter(refreshLock);
			if (doTryRefresh)
			{
				try
				{
					DoMaybeRefresh();
				}
				finally
				{
					//refreshLock.unlock();
					Monitor.Exit(refreshLock);
				}
			}

			return doTryRefresh;
		}

		/**
		 * You must call this (or {@link #maybeRefresh()}), periodically, if you want
		 * that {@link #acquire()} will return refreshed instances.
		 * 
		 * <p>
		 * <b>Threads</b>: unlike {@link #maybeRefresh()}, if another thread is
		 * currently refreshing, this method blocks until that thread completes. It is
		 * useful if you want to guarantee that the next call to {@link #acquire()}
		 * will return a refreshed instance. Otherwise, consider using the
		 * non-blocking {@link #maybeRefresh()}.
		 * @throws IOException if refreshing the resource causes an {@link IOException}
		 * @throws AlreadyClosedException if the reference manager has been {@link #close() closed}. 
		 */
		public void MaybeRefreshBlocking()
		{
			EnsureOpen();

			// Ensure only 1 thread does refresh at once
			//refreshLock.lock();
			Monitor.Enter(refreshLock);
			try
			{
				DoMaybeRefresh();
			}
			finally
			{
				//refreshLock.unlock();
				Monitor.Exit(refreshLock);
			}
		}

		/** Called after a refresh was attempted, regardless of
		 *  whether a new reference was in fact created.
		 *  @throws IOException if a low level I/O exception occurs  
		 **/
		protected void AfterMaybeRefresh()
		{
		}

		/**
		 * Release the reference previously obtained via {@link #acquire()}.
		 * <p>
		 * <b>NOTE:</b> it's safe to call this after {@link #close()}.
		 * @throws IOException if the release operation on the given resource throws an {@link IOException}
		 */
		public void release(TG reference)
		{
			reference != null;
			DecRef(reference);
		}

		private void notifyRefreshListenersBefore()
		{
			foreach (RefreshListener refreshListener in refreshListeners)
			{
				refreshListener.beforeRefresh();
			}
		}

		private void notifyRefreshListenersRefreshed(bool didRefresh)
		{
			foreach (RefreshListener refreshListener in refreshListeners)
			{
				refreshListener.afterRefresh(didRefresh);
			}
		}

		/**
		 * Adds a listener, to be notified when a reference is refreshed/swapped.
		 */
		public void AddListener(RefreshListener listener)
		{
			if (listener == null)
			{
				throw new NullReferenceException("Listener cannot be null");
			}
			refreshListeners.Add(listener);
		}

		/**
		 * Remove a listener added with {@link #addListener(RefreshListener)}.
		 */
		public void RemoveListener(RefreshListener listener)
		{
			if (listener == null)
			{
				throw new NullReferenceException("Listener cannot be null");
			}
			refreshListeners.Remove(listener);
		}

		/** Use to receive notification when a refresh has
		 *  finished.  See {@link #addListener}. */
		public interface RefreshListener
		{

			/** Called right before a refresh attempt starts. */
			void beforeRefresh();

			/** Called after the attempted refresh; if the refresh
			 * did open a new reference then didRefresh will be true
			 * and {@link #acquire()} is guaranteed to return the new
			 * reference. */
			void afterRefresh(bool didRefresh);
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
