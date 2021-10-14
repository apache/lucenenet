using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System.Collections.Generic;

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

    using InfoStream = Lucene.Net.Util.InfoStream;
    using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;

    /// <summary>
    /// <see cref="FlushPolicy"/> controls when segments are flushed from a RAM resident
    /// internal data-structure to the <see cref="IndexWriter"/>s <see cref="Store.Directory"/>.
    /// <para/>
    /// Segments are traditionally flushed by:
    /// <list type="bullet">
    ///     <item><description>RAM consumption - configured via
    ///         <see cref="LiveIndexWriterConfig.RAMBufferSizeMB"/></description></item>
    ///     <item><description>Number of RAM resident documents - configured via
    ///         <see cref="LiveIndexWriterConfig.MaxBufferedDocs"/></description></item>
    /// </list>
    /// The policy also applies pending delete operations (by term and/or query),
    /// given the threshold set in
    /// <see cref="LiveIndexWriterConfig.MaxBufferedDeleteTerms"/>.
    /// <para/>
    /// <see cref="IndexWriter"/> consults the provided <seea cref="FlushPolicy"/> to control the
    /// flushing process. The policy is informed for each added or updated document
    /// as well as for each delete term. Based on the <see cref="FlushPolicy"/>, the
    /// information provided via <see cref="ThreadState"/> and
    /// <see cref="DocumentsWriterFlushControl"/>, the <see cref="FlushPolicy"/> decides if a
    /// <see cref="DocumentsWriterPerThread"/> needs flushing and mark it as flush-pending
    /// via <see cref="DocumentsWriterFlushControl.SetFlushPending(ThreadState)"/>, or if deletes need
    /// to be applied.
    /// </summary>
    /// <seealso cref="ThreadState"/>
    /// <seealso cref="DocumentsWriterFlushControl"/>
    /// <seealso cref="DocumentsWriterPerThread"/>
    /// <seealso cref="IndexWriterConfig.FlushPolicy"/>
    internal abstract class FlushPolicy // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        protected LiveIndexWriterConfig m_indexWriterConfig;
        protected InfoStream m_infoStream;

        /// <summary>
        /// Called for each delete term. If this is a delete triggered due to an update
        /// the given <see cref="ThreadState"/> is non-null.
        /// <para/>
        /// Note: this method is called synchronized on the given
        /// <see cref="DocumentsWriterFlushControl"/> and it is guaranteed that the calling
        /// thread holds the lock on the given <see cref="ThreadState"/>
        /// </summary>
        public abstract void OnDelete(DocumentsWriterFlushControl control, ThreadState state);

        /// <summary>
        /// Called for each document update on the given <see cref="ThreadState"/>'s
        /// <see cref="DocumentsWriterPerThread"/>.
        /// <para/>
        /// Note: this method is called  synchronized on the given
        /// <see cref="DocumentsWriterFlushControl"/> and it is guaranteed that the calling
        /// thread holds the lock on the given <see cref="ThreadState"/>
        /// </summary>
        public virtual void OnUpdate(DocumentsWriterFlushControl control, ThreadState state)
        {
            OnInsert(control, state);
            OnDelete(control, state);
        }

        /// <summary>
        /// Called for each document addition on the given <see cref="ThreadState"/>s
        /// <see cref="DocumentsWriterPerThread"/>.
        /// <para/>
        /// Note: this method is synchronized by the given
        /// <see cref="DocumentsWriterFlushControl"/> and it is guaranteed that the calling
        /// thread holds the lock on the given <see cref="ThreadState"/>
        /// </summary>
        public abstract void OnInsert(DocumentsWriterFlushControl control, ThreadState state);

        /// <summary>
        /// Called by <see cref="DocumentsWriter"/> to initialize the <see cref="FlushPolicy"/>
        /// </summary>
        protected internal virtual void Init(LiveIndexWriterConfig indexWriterConfig)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                this.m_indexWriterConfig = indexWriterConfig;
                m_infoStream = indexWriterConfig.InfoStream;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns the current most RAM consuming non-pending <see cref="ThreadState"/> with
        /// at least one indexed document.
        /// <para/>
        /// This method will never return <c>null</c>
        /// </summary>
        protected virtual ThreadState FindLargestNonPendingWriter(DocumentsWriterFlushControl control, ThreadState perThreadState)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(perThreadState.dwpt.NumDocsInRAM > 0);
            long maxRamSoFar = perThreadState.bytesUsed;
            // the dwpt which needs to be flushed eventually
            ThreadState maxRamUsingThreadState = perThreadState;
            if (Debugging.AssertsEnabled) Debugging.Assert(!perThreadState.flushPending, "DWPT should have flushed");
            IEnumerator<ThreadState> activePerThreadsIterator = control.AllActiveThreadStates();
            while (activePerThreadsIterator.MoveNext())
            {
                ThreadState next = activePerThreadsIterator.Current;
                if (!next.flushPending)
                {
                    long nextRam = next.bytesUsed;
                    if (nextRam > maxRamSoFar && next.dwpt.NumDocsInRAM > 0)
                    {
                        maxRamSoFar = nextRam;
                        maxRamUsingThreadState = next;
                    }
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(AssertMessage("set largest ram consuming thread pending on lower watermark"));
            return maxRamUsingThreadState;
        }

        private bool AssertMessage(string s)
        {
            if (m_infoStream.IsEnabled("FP"))
            {
                m_infoStream.Message("FP", s);
            }
            return true;
        }

        public virtual object Clone()
        {
            FlushPolicy clone;

            clone = (FlushPolicy)base.MemberwiseClone();
            clone.m_indexWriterConfig = null;
            clone.m_infoStream = null;
            return clone;
        }
    }
}