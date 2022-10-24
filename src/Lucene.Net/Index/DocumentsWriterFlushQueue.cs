using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    using FlushedSegment = Lucene.Net.Index.DocumentsWriterPerThread.FlushedSegment;

    /// <summary>
    /// @lucene.internal
    /// </summary>
    internal class DocumentsWriterFlushQueue
    {
        private readonly Queue<FlushTicket> queue = new Queue<FlushTicket>();

        // we track tickets separately since count must be present even before the ticket is
        // constructed ie. queue.size would not reflect it.
        private readonly AtomicInt32 ticketCount = new AtomicInt32();

        private readonly ReentrantLock purgeLock = new ReentrantLock();

        internal virtual void AddDeletes(DocumentsWriterDeleteQueue deleteQueue)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                IncTickets(); // first inc the ticket count - freeze opens
                // a window for #anyChanges to fail
                bool success = false;
                try
                {
                    queue.Enqueue(new GlobalDeletesTicket(deleteQueue.FreezeGlobalBuffer(null)));
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        DecTickets();
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void IncTickets()
        {
            int numTickets = ticketCount.IncrementAndGet();
            if (Debugging.AssertsEnabled) Debugging.Assert(numTickets > 0);
        }

        private void DecTickets()
        {
            int numTickets = ticketCount.DecrementAndGet();
            if (Debugging.AssertsEnabled) Debugging.Assert(numTickets >= 0);
        }

        internal virtual SegmentFlushTicket AddFlushTicket(DocumentsWriterPerThread dwpt)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // Each flush is assigned a ticket in the order they acquire the ticketQueue
                // lock
                IncTickets();
                bool success = false;
                try
                {
                    // prepare flush freezes the global deletes - do in synced block!
                    SegmentFlushTicket ticket = new SegmentFlushTicket(dwpt.PrepareFlush());
                    queue.Enqueue(ticket);
                    success = true;
                    return ticket;
                }
                finally
                {
                    if (!success)
                    {
                        DecTickets();
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal virtual void AddSegment(SegmentFlushTicket ticket, FlushedSegment segment)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // the actual flush is done asynchronously and once done the FlushedSegment
                // is passed to the flush ticket
                ticket.SetSegment(segment);
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal virtual void MarkTicketFailed(SegmentFlushTicket ticket)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                // to free the queue we mark tickets as failed just to clean up the queue.
                ticket.SetFailed();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal virtual bool HasTickets
        {
            get
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(ticketCount >= 0,"ticketCount should be >= 0 but was: {0}", ticketCount);
                return ticketCount != 0;
            }
        }

        private int InnerPurge(IndexWriter writer)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(purgeLock.IsHeldByCurrentThread);
            int numPurged = 0;
            while (true)
            {
                FlushTicket head;
                bool canPublish;
                UninterruptableMonitor.Enter(this);
                try
                {
                    head = queue.Count <= 0 ? null : queue.Peek();
                    canPublish = head != null && head.CanPublish; // do this synced
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
                if (canPublish)
                {
                    numPurged++;
                    try
                    {
                        /*
                         * if we block on publish -> lock IW -> lock BufferedDeletes we don't block
                         * concurrent segment flushes just because they want to append to the queue.
                         * the downside is that we need to force a purge on fullFlush since ther could
                         * be a ticket still in the queue.
                         */
                        head.Publish(writer);
                    }
                    finally
                    {
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            // finally remove the published ticket from the queue
                            FlushTicket poll = queue.Dequeue();
                            ticketCount.DecrementAndGet();
                            if (Debugging.AssertsEnabled) Debugging.Assert(poll == head);
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            return numPurged;
        }

        internal virtual int ForcePurge(IndexWriter writer)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(!UninterruptableMonitor.IsEntered(this));
                Debugging.Assert(!UninterruptableMonitor.IsEntered(writer));
            }
            purgeLock.@Lock();
            try
            {
                return InnerPurge(writer);
            }
            finally
            {
                purgeLock.Unlock();
            }
        }

        internal virtual int TryPurge(IndexWriter writer)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(!UninterruptableMonitor.IsEntered(this));
                Debugging.Assert(!UninterruptableMonitor.IsEntered(writer));
            }
            if (purgeLock.TryLock())
            {
                try
                {
                    return InnerPurge(writer);
                }
                finally
                {
                    purgeLock.Unlock();
                }
            }
            return 0;
        }

        public virtual int TicketCount => ticketCount;

        internal virtual void Clear()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                queue.Clear();
                ticketCount.Value = 0;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        internal abstract class FlushTicket
        {
            protected FrozenBufferedUpdates m_frozenUpdates;
            protected bool m_published = false;

            protected FlushTicket(FrozenBufferedUpdates frozenUpdates)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(frozenUpdates != null);
                this.m_frozenUpdates = frozenUpdates;
            }

            protected internal abstract void Publish(IndexWriter writer);

            protected internal abstract bool CanPublish { get; }

            /// <summary>
            /// Publishes the flushed segment, segment private deletes (if any) and its
            /// associated global delete (if present) to <see cref="IndexWriter"/>.  The actual
            /// publishing operation is synced on IW -> BDS so that the <see cref="SegmentInfo"/>'s
            /// delete generation is always <see cref="FrozenBufferedUpdates.DelGen"/> (<paramref name="globalPacket"/>) + 1
            /// </summary>
            protected static void PublishFlushedSegment(IndexWriter indexWriter, FlushedSegment newSegment, FrozenBufferedUpdates globalPacket) // LUCENENET: CA1822: Mark members as static
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(newSegment != null);
                    Debugging.Assert(newSegment.segmentInfo != null);
                }
                FrozenBufferedUpdates segmentUpdates = newSegment.segmentUpdates;
                //System.out.println("FLUSH: " + newSegment.segmentInfo.info.name);
                if (indexWriter.infoStream.IsEnabled("DW"))
                {
                    indexWriter.infoStream.Message("DW", "publishFlushedSegment seg-private updates=" + segmentUpdates);
                }

                if (segmentUpdates != null && indexWriter.infoStream.IsEnabled("DW"))
                {
                    indexWriter.infoStream.Message("DW", "flush: push buffered seg private updates: " + segmentUpdates);
                }
                // now publish!
                indexWriter.PublishFlushedSegment(newSegment.segmentInfo, segmentUpdates, globalPacket);
            }

            [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
            protected void FinishFlush(IndexWriter indexWriter, FlushedSegment newSegment, FrozenBufferedUpdates bufferedUpdates)
            {
                // Finish the flushed segment and publish it to IndexWriter
                if (newSegment is null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(bufferedUpdates != null);
                    if (bufferedUpdates != null && bufferedUpdates.Any())
                    {
                        indexWriter.PublishFrozenUpdates(bufferedUpdates);
                        if (indexWriter.infoStream.IsEnabled("DW"))
                        {
                            indexWriter.infoStream.Message("DW", "flush: push buffered updates: " + bufferedUpdates);
                        }
                    }
                }
                else
                {
                    PublishFlushedSegment(indexWriter, newSegment, bufferedUpdates);
                }
            }
        }

        internal sealed class GlobalDeletesTicket : FlushTicket
        {
            internal GlobalDeletesTicket(FrozenBufferedUpdates frozenUpdates) // LUCENENET NOTE: Made internal rather than protected because class is sealed
                : base(frozenUpdates)
            {
            }

            protected internal override void Publish(IndexWriter writer)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!m_published, "ticket was already publised - can not publish twice");
                m_published = true;
                // its a global ticket - no segment to publish
                FinishFlush(writer, null, m_frozenUpdates);
            }

            protected internal override bool CanPublish => true;
        }

        internal sealed class SegmentFlushTicket : FlushTicket
        {
            internal FlushedSegment segment;
            internal bool failed = false;

            internal SegmentFlushTicket(FrozenBufferedUpdates frozenDeletes) // LUCENENET NOTE: Made internal rather than protected because class is sealed
                : base(frozenDeletes)
            {
            }

            protected internal override void Publish(IndexWriter writer)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!m_published, "ticket was already publised - can not publish twice");
                m_published = true;
                FinishFlush(writer, segment, m_frozenUpdates);
            }

            internal void SetSegment(FlushedSegment segment) // LUCENENET NOTE: Made internal rather than protected because class is sealed
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(!failed);
                this.segment = segment;
            }

            internal void SetFailed() // LUCENENET NOTE: Made internal rather than protected because class is sealed
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(segment is null);
                failed = true;
            }

            protected internal override bool CanPublish => segment != null || failed;
        }
    }
}