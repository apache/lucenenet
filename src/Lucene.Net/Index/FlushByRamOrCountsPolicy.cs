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

    using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState;

    /// <summary>
    /// Default <see cref="FlushPolicy"/> implementation that flushes new segments based on
    /// RAM used and document count depending on the <see cref="IndexWriter"/>'s
    /// <see cref="IndexWriterConfig"/>. It also applies pending deletes based on the
    /// number of buffered delete terms.
    ///
    /// <list type="bullet">
    ///     <item><description>
    ///         <see cref="OnDelete(DocumentsWriterFlushControl, DocumentsWriterPerThreadPool.ThreadState)"/>
    ///         - applies pending delete operations based on the global number of buffered
    ///         delete terms iff <see cref="LiveIndexWriterConfig.MaxBufferedDeleteTerms"/> is
    ///         enabled
    ///     </description></item>
    ///     <item><description>
    ///         <see cref="OnInsert(DocumentsWriterFlushControl, DocumentsWriterPerThreadPool.ThreadState)"/>
    ///         - flushes either on the number of documents per
    ///         <see cref="DocumentsWriterPerThread"/> (
    ///         <see cref="DocumentsWriterPerThread.NumDocsInRAM"/>) or on the global active
    ///         memory consumption in the current indexing session iff
    ///         <see cref="LiveIndexWriterConfig.MaxBufferedDocs"/> or
    ///         <see cref="LiveIndexWriterConfig.RAMBufferSizeMB"/> is enabled respectively
    ///     </description></item>
    ///     <item><description>
    ///         <see cref="FlushPolicy.OnUpdate(DocumentsWriterFlushControl, DocumentsWriterPerThreadPool.ThreadState)"/>
    ///         - calls
    ///         <see cref="OnInsert(DocumentsWriterFlushControl, DocumentsWriterPerThreadPool.ThreadState)"/>
    ///         and
    ///         <see cref="OnDelete(DocumentsWriterFlushControl, DocumentsWriterPerThreadPool.ThreadState)"/>
    ///         in order
    ///     </description></item>
    /// </list>
    /// All <see cref="IndexWriterConfig"/> settings are used to mark
    /// <see cref="DocumentsWriterPerThread"/> as flush pending during indexing with
    /// respect to their live updates.
    /// <para/>
    /// If <see cref="LiveIndexWriterConfig.RAMBufferSizeMB"/> (setter) is enabled, the
    /// largest ram consuming <see cref="DocumentsWriterPerThread"/> will be marked as
    /// pending iff the global active RAM consumption is &gt;= the configured max RAM
    /// buffer.
    /// </summary>
    internal class FlushByRamOrCountsPolicy : FlushPolicy
    {
        public override void OnDelete(DocumentsWriterFlushControl control, ThreadState state)
        {
            if (FlushOnDeleteTerms)
            {
                // Flush this state by num del terms
                int maxBufferedDeleteTerms = m_indexWriterConfig.MaxBufferedDeleteTerms;
                if (control.NumGlobalTermDeletes >= maxBufferedDeleteTerms)
                {
                    control.SetApplyAllDeletes();
                }
            }
            if ((FlushOnRAM && control.DeleteBytesUsed > (1024 * 1024 * m_indexWriterConfig.RAMBufferSizeMB)))
            {
                control.SetApplyAllDeletes();
                if (m_infoStream.IsEnabled("FP"))
                {
                    m_infoStream.Message("FP", "force apply deletes bytesUsed=" + control.DeleteBytesUsed + " vs ramBuffer=" + (1024 * 1024 * m_indexWriterConfig.RAMBufferSizeMB));
                }
            }
        }

        public override void OnInsert(DocumentsWriterFlushControl control, ThreadState state)
        {
            if (FlushOnDocCount && state.dwpt.NumDocsInRAM >= m_indexWriterConfig.MaxBufferedDocs)
            {
                // Flush this state by num docs
                control.SetFlushPending(state);
            } // flush by RAM
            else if (FlushOnRAM)
            {
                long limit = (long)(m_indexWriterConfig.RAMBufferSizeMB * 1024d * 1024d);
                long totalRam = control.ActiveBytes + control.DeleteBytesUsed;
                if (totalRam >= limit)
                {
                    if (m_infoStream.IsEnabled("FP"))
                    {
                        m_infoStream.Message("FP", "flush: activeBytes=" + control.ActiveBytes + " deleteBytes=" + control.DeleteBytesUsed + " vs limit=" + limit);
                    }
                    MarkLargestWriterPending(control, state, totalRam);
                }
            }
        }

        /// <summary>
        /// Marks the most ram consuming active <see cref="DocumentsWriterPerThread"/> flush
        /// pending
        /// </summary>
        protected virtual void MarkLargestWriterPending(DocumentsWriterFlushControl control, ThreadState perThreadState, long currentBytesPerThread)
        {
            control.SetFlushPending(FindLargestNonPendingWriter(control, perThreadState));
        }

        /// <summary>
        /// Returns <c>true</c> if this <see cref="FlushPolicy"/> flushes on
        /// <see cref="LiveIndexWriterConfig.MaxBufferedDocs"/>, otherwise
        /// <c>false</c>.
        /// </summary>
        protected internal virtual bool FlushOnDocCount
            => m_indexWriterConfig.MaxBufferedDocs != IndexWriterConfig.DISABLE_AUTO_FLUSH;

        /// <summary>
        /// Returns <c>true</c> if this <see cref="FlushPolicy"/> flushes on
        /// <see cref="LiveIndexWriterConfig.MaxBufferedDeleteTerms"/>, otherwise
        /// <c>false</c>.
        /// </summary>
        protected internal virtual bool FlushOnDeleteTerms
            => m_indexWriterConfig.MaxBufferedDeleteTerms != IndexWriterConfig.DISABLE_AUTO_FLUSH;

        /// <summary>
        /// Returns <c>true</c> if this <see cref="FlushPolicy"/> flushes on
        /// <see cref="LiveIndexWriterConfig.RAMBufferSizeMB"/>, otherwise
        /// <c>false</c>.
        /// </summary>
        protected internal virtual bool FlushOnRAM
            => m_indexWriterConfig.RAMBufferSizeMB != IndexWriterConfig.DISABLE_AUTO_FLUSH;
    }
}