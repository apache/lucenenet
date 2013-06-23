using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;

namespace Lucene.Net.Index
{
    internal class ThreadAffinityDocumentsWriterThreadPool : DocumentsWriterPerThreadPool
    {
        private IDictionary<Thread, ThreadState> threadBindings = new ConcurrentHashMap<Thread, ThreadState>();

        public ThreadAffinityDocumentsWriterThreadPool(int maxNumPerThreads)
            : base(maxNumPerThreads)
        {
            //assert getMaxThreadStates() >= 1;
        }

        internal override ThreadState GetAndLock(Thread requestingThread, DocumentsWriter documentsWriter)
        {
            ThreadState threadState = threadBindings[requestingThread];
            if (threadState != null && threadState.TryLock())
            {
                return threadState;
            }
            ThreadState minThreadState = null;


            /* TODO -- another thread could lock the minThreadState we just got while 
             we should somehow prevent this. */
            // Find the state that has minimum number of threads waiting
            minThreadState = MinContendedThreadState;
            if (minThreadState == null || minThreadState.HasQueuedThreads)
            {
                ThreadState newState = NewThreadState(); // state is already locked if non-null
                if (newState != null)
                {
                    //assert newState.isHeldByCurrentThread();
                    threadBindings[requestingThread] = newState;
                    return newState;
                }
                else if (minThreadState == null)
                {
                    /*
                     * no new threadState available we just take the minContented one
                     * This must return a valid thread state since we accessed the 
                     * synced context in newThreadState() above.
                     */
                    minThreadState = MinContendedThreadState;
                }
            }
            //assert minThreadState != null: "ThreadState is null";

            minThreadState.Lock();
            return minThreadState;
        }

        public override object Clone()
        {
            ThreadAffinityDocumentsWriterThreadPool clone = (ThreadAffinityDocumentsWriterThreadPool)base.Clone();
            clone.threadBindings = new ConcurrentHashMap<Thread, ThreadState>();
            return clone;
        }
    }
}
