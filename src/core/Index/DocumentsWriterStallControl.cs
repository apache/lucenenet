using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Index
{
    internal sealed class DocumentsWriterStallControl
    {
        private volatile bool stalled;
        private int numWaiting; // only with assert
        private bool wasStalled; // only with assert
        //private readonly IDictionary<Thread, Boolean> waiting = new IdentityHashMap<Thread, Boolean>(); // only with assert

        internal void UpdateStalled(bool stalled)
        {
            lock (this)
            {
                this.stalled = stalled;
                if (stalled)
                {
                    wasStalled = true;
                }
                Monitor.PulseAll(this);
            }
        }

        internal void WaitIfStalled()
        {
            if (stalled)
            {
                lock (this)
                {
                    if (stalled)
                    { 
                        // react on the first wakeup call!
                        // don't loop here, higher level logic will re-stall!
                        try
                        {
                            //assert incWaiters();
                            Monitor.Wait(this); // .NET port: is this correct?
                            //assert  decrWaiters();
                        }
                        catch (ThreadInterruptedException)
                        {
                            // .NET port: this try/catch isn't really needed, only used for checked exceptions in java
                            throw;
                        }
                    }
                }
            }
        }

        internal bool AnyStalledThreads
        {
            get { return stalled; }
        }

        private bool IncWaiters()
        {
            numWaiting++;
            //assert waiting.put(Thread.currentThread(), Boolean.TRUE) == null;

            return numWaiting > 0;
        }

        private bool DecrWaiters()
        {
            numWaiting--;
            //assert waiting.remove(Thread.currentThread()) != null;
            return numWaiting >= 0;
        }

        internal bool HasBlocked
        {
            get
            {
                lock (this)
                {
                    // for tests
                    return numWaiting > 0;
                }
            }
        }

        internal bool IsHealthy
        {
            get
            {
                // for tests
                return !stalled; // volatile read!
            }
        }

        //synchronized boolean isThreadQueued(Thread t) { // for tests
        //  return waiting.containsKey(t);
        //}

        //synchronized boolean wasStalled() { // for tests
        //  return wasStalled;
        //}
    }
}
