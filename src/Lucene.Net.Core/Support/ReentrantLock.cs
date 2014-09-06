using System.Threading;

namespace Lucene.Net.Support
{
    public class ReentrantLock
    {
        // .NET Port: lock object used to emulate ReentrantLock
        private readonly object _lock = new object();

        // .NET Port: Estimated monitor queue length
        private int _queueLength = 0;

        // .NET Port: mimic ReentrantLock -- Monitor is re-entrant
        public void Lock()
        {
            // note about queue length: in java's ReentrantLock, getQueueLength() returns the number
            // of threads waiting on entering the lock. So here, we're incrementing the count before trying to enter,
            // meaning that until enter has completed the thread is waiting so the queue is incremented. Once
            // we enter the lock, then we immediately decrement it because that thread is no longer in the queue.
            // Due to race conditions, the queue length is an estimate only.
            Interlocked.Increment(ref _queueLength);
            Monitor.Enter(_lock);
            Interlocked.Decrement(ref _queueLength);
        }

        // .NET Port: mimic ReentrantLock -- Monitor is re-entrant
        public void Unlock()
        {
            Monitor.Exit(_lock);
        }

        public bool TryLock()
        {
            Interlocked.Increment(ref _queueLength);
            bool success = Monitor.TryEnter(_lock);
            Interlocked.Decrement(ref _queueLength);

            return success;
        }

        public int QueueLength
        {
            get
            {
                // hold onto the estimate for the length of this method
                int estimate = _queueLength;

                // should never be < 0, but just in case, as a negative number doesn't make sense.
                return estimate <= 0 ? 0 : estimate;
            }
        }

        public bool HasQueuedThreads
        {
            get { return _queueLength > 0; }
        }
    }
}