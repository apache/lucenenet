using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Lucene.Net.Index
{
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

    using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState; //javadoc

    /// <summary>
    /// A <seealso cref="DocumentsWriterPerThreadPool"/> implementation that tries to assign an
    /// indexing thread to the same <seealso cref="ThreadState"/> each time the thread tries to
    /// obtain a <seealso cref="ThreadState"/>. Once a new <seealso cref="ThreadState"/> is created it is
    /// associated with the creating thread. Subsequently, if the threads associated
    /// <seealso cref="ThreadState"/> is not in use it will be associated with the requesting
    /// thread. Otherwise, if the <seealso cref="ThreadState"/> is used by another thread
    /// <seealso cref="ThreadAffinityDocumentsWriterThreadPool"/> tries to find the currently
    /// minimal contended <seealso cref="ThreadState"/>.
    /// </summary>
    internal class ThreadAffinityDocumentsWriterThreadPool : DocumentsWriterPerThreadPool
    {
        private IDictionary<Thread, ThreadState> threadBindings = new ConcurrentDictionary<Thread, ThreadState>();

        /// <summary>
        /// Creates a new <seealso cref="ThreadAffinityDocumentsWriterThreadPool"/> with a given maximum of <seealso cref="ThreadState"/>s.
        /// </summary>
        public ThreadAffinityDocumentsWriterThreadPool(int maxNumPerThreads)
            : base(maxNumPerThreads)
        {
            Debug.Assert(MaxThreadStates >= 1);
        }

        public override ThreadState GetAndLock(Thread requestingThread, DocumentsWriter documentsWriter)
        {
            ThreadState threadState;
            threadBindings.TryGetValue(requestingThread, out threadState);
            if (threadState != null && threadState.TryLock())
            {
                return threadState;
            }
            ThreadState minThreadState = null;

            /* TODO -- another thread could lock the minThreadState we just got while
             we should somehow prevent this. */
            // Find the state that has minimum number of threads waiting
            minThreadState = MinContendedThreadState();
            if (minThreadState == null || minThreadState.HasQueuedThreads)
            {
                ThreadState newState = NewThreadState(); // state is already locked if non-null
                if (newState != null)
                {
                    //Debug.Assert(newState.HeldByCurrentThread);
                    threadBindings[requestingThread] = newState;
                    return newState;
                }
                else if (minThreadState == null)
                {
                    /*
                     * no new threadState available we just take the minContented one
                     * this must return a valid thread state since we accessed the
                     * synced context in newThreadState() above.
                     */
                    minThreadState = MinContendedThreadState();
                }
            }
            Debug.Assert(minThreadState != null, "ThreadState is null");

            minThreadState.@Lock();
            return minThreadState;
        }

        public override object Clone()
        {
            ThreadAffinityDocumentsWriterThreadPool clone = (ThreadAffinityDocumentsWriterThreadPool)base.Clone();
            clone.threadBindings = new ConcurrentDictionary<Thread, ThreadState>();
            return clone;
        }
    }
}