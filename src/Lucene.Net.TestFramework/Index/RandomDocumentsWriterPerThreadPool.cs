using System;
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

    ///
    /// <summary>
    /// A <code>DocumentsWriterPerThreadPool<code> that selects thread states at random.
    ///
    /// @lucene.internal
    /// @lucene.experimental
    /// </summary>
    internal class RandomDocumentsWriterPerThreadPool : DocumentsWriterPerThreadPool
    {
        private readonly ThreadState[] states;
        private readonly Random random;
        private readonly int maxRetry;

        public RandomDocumentsWriterPerThreadPool(int maxNumPerThreads, Random random)
            : base(maxNumPerThreads)
        {
            Debug.Assert(MaxThreadStates >= 1);
            states = new ThreadState[maxNumPerThreads];
            this.random = new Random(random.Next());
            this.maxRetry = 1 + random.Next(10);
        }

        public override ThreadState GetAndLock(Thread requestingThread, DocumentsWriter documentsWriter)
        {
            ThreadState threadState = null;
            if (NumThreadStatesActive == 0)
            {
                lock (this)
                {
                    if (NumThreadStatesActive == 0)
                    {
                        threadState = states[0] = NewThreadState();
                        return threadState;
                    }
                }
            }
            Debug.Assert(NumThreadStatesActive > 0);
            for (int i = 0; i < maxRetry; i++)
            {
                int ord = random.Next(NumThreadStatesActive);
                lock (this)
                {
                    threadState = states[ord];
                    Debug.Assert(threadState != null);
                }

                if (threadState.TryLock())
                {
                    return threadState;
                }
                if (random.Next(20) == 0)
                {
                    break;
                }
            }
            /*
             * only try to create a new threadstate if we can not lock the randomly
             * selected state. this is important since some tests rely on a single
             * threadstate in the single threaded case. Eventually it would be nice if
             * we would not have this limitation but for now we just make sure we only
             * allocate one threadstate if indexing is single threaded
             */

            lock (this)
            {
                ThreadState newThreadState = NewThreadState();
                if (newThreadState != null) // did we get a new state?
                {
                    threadState = states[NumThreadStatesActive - 1] = newThreadState;
                    //Debug.Assert(threadState.HeldByCurrentThread);
                    return threadState;
                }
                // if no new state is available lock the random one
            }
            Debug.Assert(threadState != null);
            threadState.@Lock();
            return threadState;
        }
    }
}