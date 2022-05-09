using Lucene.Net.Support.Threading;
using System.Runtime.CompilerServices;

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
    /// A <see cref="MergeScheduler"/> that simply does each merge
    /// sequentially, using the current thread.
    /// </summary>
    public class SerialMergeScheduler : MergeScheduler
    {
        /// <summary>
        /// Sole constructor. </summary>
        public SerialMergeScheduler()
        {
        }

        /// <summary>
        /// Just do the merges in sequence. We do this
        /// "synchronized" so that even if the application is using
        /// multiple threads, only one merge may run at a time.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound) // LUCENENET NOTE: This was internal in the original, but the base class is public so there isn't much choice here
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                while (true)
                {
                    MergePolicy.OneMerge merge = writer.NextMerge();
                    if (merge is null)
                    {
                        break;
                    }
                    writer.Merge(merge);
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}