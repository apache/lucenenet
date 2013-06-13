using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Lucene.Net.Search
{
    public abstract class ReferenceManager<G> : IDisposable
    {
        private static readonly string REFERENCE_MANAGER_IS_CLOSED_MSG = "this ReferenceManager is closed";

        protected  G current;

        private readonly Lock refreshLock = new ReentrantLock();

        private readonly List<RefreshListener> refreshListeners = new CopyOnWriteArrayList<RefreshListener>();
        
        private void ensureOpen() 
        {
            if (current == null) {
                throw new ObjectDisposedException(REFERENCE_MANAGER_IS_CLOSED_MSG);
            }
        }
  
        private void swapReference(G newReference) {
            lock (this)
            {
                ensureOpen();
                G oldReference = current;
                current = newReference;
                release(oldReference);
            }
        }

        protected abstract void decRef(G reference);

        protected abstract G refreshIfNeeded(G referenceToRefresh);

        protected abstract bool tryIncRef(G reference); 

        public G acquire() 
        {
            G ref;
            do {
              if ((ref = current) == null) {
                throw new ObjectDisposedException(REFERENCE_MANAGER_IS_CLOSED_MSG);
              }
            } while (!tryIncRef(ref));
            return ref;
          }

        public void release(G reference) 
        {
            reference != null;
            decRef(reference);
        }

        public void Dispose()
        {
            lock (this)
            {
                if (current != null)
                {
                    // make sure we can call this more than once
                    // closeable javadoc says:
                    // if this is already closed then invoking this method has no effect.
                    swapReference(null);
                    afterClose();
                }
            }
        }

        protected void afterClose()
        {
            
        }

        private void doMaybeRefresh() 
        {
            // it's ok to call lock() here (blocking) because we're supposed to get here
            // from either maybeRefreh() or maybeRefreshBlocking(), after the lock has
            // already been obtained. Doing that protects us from an accidental bug
            // where this method will be called outside the scope of refreshLock.
            // Per ReentrantLock's javadoc, calling lock() by the same thread more than
            // once is ok, as long as unlock() is called a matching number of times.
            refreshLock.lock();
            bool refreshed = false;
            try 
            {
                G reference = acquire();
                try {
                notifyRefreshListenersBefore();
                G newReference = refreshIfNeeded(reference);
                if (newReference != null) {
                    //assert newReference != reference : "refreshIfNeeded should return null if refresh wasn't needed";
                    try {
                    swapReference(newReference);
                    refreshed = true;
                    } finally {
                    if (!refreshed) {
                        release(newReference);
                    }
                    }
                }
                } finally {
                release(reference);
                notifyRefreshListenersRefreshed(refreshed);
                }
                afterMaybeRefresh();
            } 
            finally 
            {
                refreshLock.unlock();
            }
        }
        
        private void notifyRefreshListenersBefore() 
        {
            for (RefreshListener refreshListener : refreshListeners) {
                refreshListener.beforeRefresh();
            }
        }

        private void notifyRefreshListenersRefreshed(boolean didRefresh) 
        {
            for (RefreshListener refreshListener : refreshListeners) 
            {
                refreshListener.afterRefresh(didRefresh);
            }
        }

        public void addListener(RefreshListener listener) {
    if (listener == null) {
      throw new NullPointerException("Listener cannot be null");
    }
    refreshListeners.add(listener);
  }

  /**
   * Remove a listener added with {@link #addListener(RefreshListener)}.
   */
  public void removeListener(RefreshListener listener) {
    if (listener == null) {
      throw new NullPointerException("Listener cannot be null");
    }
    refreshListeners.remove(listener);
  }

  public interface RefreshListener 
  {

    void beforeRefresh() throws IOException;

    void afterRefresh(boolean didRefresh) throws IOException;
  }

    }
}
