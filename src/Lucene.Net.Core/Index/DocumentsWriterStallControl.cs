using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;

    /*
         * Licensed to the Apache Software Foundation (ASF) under one or more
         * contributor license agreements. See the NOTICE file distributed with
         * this work for additional information regarding copyright ownership.
         * The ASF licenses this file to You under the Apache License, Version 2.0
         * (the "License"); you may not use this file except in compliance with
         * the License. You may obtain a copy of the License at
         *
         * http://www.apache.org/licenses/LICENSE-2.0
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
    public sealed class DocumentsWriterStallControl
    {
        private volatile bool Stalled;
        private int NumWaiting; // only with assert
        private bool WasStalled_Renamed; // only with assert
        private readonly IDictionary<ThreadClass, bool?> Waiting = new IdentityHashMap<ThreadClass, bool?>(); // only with assert

        /// <summary>
        /// Update the stalled flag status. this method will set the stalled flag to
        /// <code>true</code> iff the number of flushing
        /// <seealso cref="DocumentsWriterPerThread"/> is greater than the number of active
        /// <seealso cref="DocumentsWriterPerThread"/>. Otherwise it will reset the
        /// <seealso cref="DocumentsWriterStallControl"/> to healthy and release all threads
        /// waiting on <seealso cref="#waitIfStalled()"/>
        /// </summary>
        public void UpdateStalled(bool stalled)
        {
            lock (this)
            {
                this.Stalled = stalled;
                if (stalled)
                {
                    WasStalled_Renamed = true;
                }
                Monitor.PulseAll(this);
            }
        }

        /// <summary>
        /// Blocks if documents writing is currently in a stalled state.
        ///
        /// </summary>
        public void WaitIfStalled()
        {
            if (Stalled)
            {
                lock (this)
                {
                    if (Stalled) // react on the first wakeup call!
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

        public bool AnyStalledThreads()
        {
            return Stalled;
        }

        private bool IncWaiters()
        {
            NumWaiting++;
            bool existed = Waiting.ContainsKey(ThreadClass.Current());
            Debug.Assert(!existed);
            Waiting[ThreadClass.Current()] = true;

            return NumWaiting > 0;
        }

        private bool DecrWaiters()
        {
            NumWaiting--;
            bool removed = Waiting.Remove(ThreadClass.Current());
            Debug.Assert(removed);

            return NumWaiting >= 0;
        }

        public bool HasBlocked() // for tests
        {
            lock (this)
            {
                return NumWaiting > 0;
            }
        }

        public bool Healthy
        {
            get
            {
                return !Stalled; // volatile read!
            }
        }

        public bool IsThreadQueued(ThreadClass t) // for tests
        {
            lock (this)
            {
                return Waiting.ContainsKey(t);
            }
        }

        public bool WasStalled() // for tests
        {
            lock (this)
            {
                return WasStalled_Renamed;
            }
        }
    }
}