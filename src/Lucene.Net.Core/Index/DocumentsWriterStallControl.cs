using Lucene.Net.Support;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Lucene.Net.Index
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
    /// Controls the health status of a <seealso cref="DocumentsWriter"/> sessions. this class
    /// used to block incoming indexing threads if flushing significantly slower than
    /// indexing to ensure the <seealso cref="DocumentsWriter"/>s healthiness. If flushing is
    /// significantly slower than indexing the net memory used within an
    /// <seealso cref="IndexWriter"/> session can increase very quickly and easily exceed the
    /// JVM's available memory.
    /// <p>
    /// To prevent OOM Errors and ensure IndexWriter's stability this class blocks
    /// incoming threads from indexing once 2 x number of available
    /// <seealso cref="ThreadState"/>s in <seealso cref="DocumentsWriterPerThreadPool"/> is exceeded.
    /// Once flushing catches up and the number of flushing DWPT is equal or lower
    /// than the number of active <seealso cref="ThreadState"/>s threads are released and can
    /// continue indexing.
    /// </summary>
    internal sealed class DocumentsWriterStallControl
    {
        private volatile bool stalled;
        private int numWaiting; // only with assert
        private bool wasStalled; // only with assert
        private readonly IDictionary<ThreadClass, bool?> waiting = new IdentityHashMap<ThreadClass, bool?>(); // only with assert

        /// <summary>
        /// Update the stalled flag status. this method will set the stalled flag to
        /// <code>true</code> iff the number of flushing
        /// <seealso cref="DocumentsWriterPerThread"/> is greater than the number of active
        /// <seealso cref="DocumentsWriterPerThread"/>. Otherwise it will reset the
        /// <seealso cref="DocumentsWriterStallControl"/> to healthy and release all threads
        /// waiting on <seealso cref="#waitIfStalled()"/>
        /// </summary>
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

        /// <summary>
        /// Blocks if documents writing is currently in a stalled state.
        ///
        /// </summary>
        internal void WaitIfStalled()
        {
            if (stalled)
            {
                lock (this)
                {
                    if (stalled) // react on the first wakeup call!
                    {
                        // don't loop here, higher level logic will re-stall!
#if !NETSTANDARD
                        try
                        {
#endif
                            // make sure not to run IncWaiters / DecrWaiters in Debug.Assert as that gets 
                            // removed at compile time if built in Release mode
                            var result = IncWaiters();
                            Debug.Assert(result);
                            Monitor.Wait(this);
                            result = DecrWaiters();
                            Debug.Assert(result);
#if !NETSTANDARD
                        }
                        catch (ThreadInterruptedException e)
                        {
                            throw new ThreadInterruptedException("Thread Interrupted Exception", e);
                        }
#endif
                    }
                }
            }
        }

        internal bool AnyStalledThreads()
        {
            return stalled;
        }

        private bool IncWaiters()
        {
            numWaiting++;
            bool existed = waiting.ContainsKey(ThreadClass.Current());
            Debug.Assert(!existed);
            waiting[ThreadClass.Current()] = true;

            return numWaiting > 0;
        }

        private bool DecrWaiters()
        {
            numWaiting--;
            bool removed = waiting.Remove(ThreadClass.Current());
            Debug.Assert(removed);

            return numWaiting >= 0;
        }

        internal bool HasBlocked // for tests
        {
            get
            {
                lock (this)
                {
                    return numWaiting > 0;
                }
            }
        }

        internal bool IsHealthy
        {
            get
            {
                return !stalled; // volatile read!
            }
        }

        internal bool IsThreadQueued(ThreadClass t) // for tests
        {
            lock (this)
            {
                return waiting.ContainsKey(t);
            }
        }

        internal bool WasStalled // for tests
        {
            get
            {
                lock (this)
                {
                    return wasStalled;
                }
            }
        }
    }
}