using System.Threading;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Abstract base class that provides a synchronization interface
    /// for derived lock types
    /// </summary>
    public abstract class ThreadLock
    {
        public abstract void Enter(object obj);
        public abstract void Exit(object obj);

        private static readonly ThreadLock _nullLock = new NullThreadLock();
        private static readonly ThreadLock _monitorLock = new MonitorThreadLock();
        
        /// <summary>
        /// A ThreadLock class that actually does no locking
        /// Used in ParallelMultiSearcher/MultiSearcher
        /// </summary>
        public static ThreadLock NullLock
        {
            get { return _nullLock; }
        }

        /// <summary>
        /// Wrapper class for the Monitor Enter/Exit methods
        /// using the <see cref="ThreadLock"/> interface
        /// </summary>
        public static ThreadLock MonitorLock
        {
            get { return _monitorLock; }
        }

        private sealed class NullThreadLock : ThreadLock
        {
            public override void Enter(object obj)
            {
                // Do nothing
            }

            public override void Exit(object obj)
            {
                // Do nothing
            }
        }

        private sealed class MonitorThreadLock : ThreadLock
        {
            public override void Enter(object obj)
            {
                Monitor.Enter(obj);
            }

            public override void Exit(object obj)
            {
                Monitor.Exit(obj);
            }
        }
    }
}
