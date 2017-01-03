using Lucene.Net.Support;
using System.Collections.Generic;
using System.Diagnostics;

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
        private readonly LinkedList<FlushTicket> queue = new LinkedList<FlushTicket>();

        // we track tickets separately since count must be present even before the ticket is
        // constructed ie. queue.size would not reflect it.
        private readonly AtomicInteger ticketCount = new AtomicInteger();

        private readonly ReentrantLock purgeLock = new ReentrantLock();

        internal virtual void AddDeletes(DocumentsWriterDeleteQueue deleteQueue)
        {
            lock (this)
            {
                IncTickets(); // first inc the ticket count - freeze opens
                // a window for #anyChanges to fail
                bool success = false;
                try
                {
                    queue.AddLast(new GlobalDeletesTicket(deleteQueue.FreezeGlobalBuffer(null)));
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
        }

        private void IncTickets()
        {
            int numTickets = ticketCount.IncrementAndGet();
            Debug.Assert(numTickets > 0);
        }

        private void DecTickets()
        {
            int numTickets = ticketCount.DecrementAndGet();
            Debug.Assert(numTickets >= 0);
        }

        internal virtual SegmentFlushTicket AddFlushTicket(DocumentsWriterPerThread dwpt)
        {
            lock (this)
            {
                // Each flush is assigned a ticket in the order they acquire the ticketQueue
                // lock
                IncTickets();
                bool success = false;
                try
                {
                    // prepare flush freezes the global deletes - do in synced block!
                    SegmentFlushTicket ticket = new SegmentFlushTicket(dwpt.PrepareFlush());
                    queue.AddLast(ticket);
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
        }

        internal virtual void AddSegment(SegmentFlushTicket ticket, FlushedSegment segment)
        {
            lock (this)
            {
                // the actual flush is done asynchronously and once done the FlushedSegment
                // is passed to the flush ticket
                ticket.SetSegment(segment);
            }
        }

        internal virtual void MarkTicketFailed(SegmentFlushTicket ticket)
        {
            lock (this)
            {
                // to free the queue we mark tickets as failed just to clean up the queue.
                ticket.SetFailed();
            }
        }

        internal virtual bool HasTickets
        {
            get
            {
                Debug.Assert(ticketCount.Get() >= 0, "ticketCount should be >= 0 but was: " + ticketCount.Get());
                return ticketCount.Get() != 0;
            }
        }

        private int InnerPurge(IndexWriter writer)
        {
            //Debug.Assert(PurgeLock.HeldByCurrentThread);
            int numPurged = 0;
            while (true)
            {
                FlushTicket head;
                bool canPublish;
                lock (this)
                {
                    head = queue.Count <= 0 ? null : queue.First.Value;
                    canPublish = head != null && head.CanPublish; // do this synced
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
                        lock (this)
                        {
                            // finally remove the published ticket from the queue
                            FlushTicket poll = queue.First.Value;
                            queue.RemoveFirst();
                            ticketCount.DecrementAndGet();
                            Debug.Assert(poll == head);
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
            //Debug.Assert(!Thread.HoldsLock(this));
            //Debug.Assert(!Thread.holdsLock(writer));
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
            //Debug.Assert(!Thread.holdsLock(this));
            //Debug.Assert(!Thread.holdsLock(writer));
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

        public virtual int TicketCount
        {
            get
            {
                return ticketCount.Get();
            }
        }

        internal virtual void Clear()
        {
            lock (this)
            {
                queue.Clear();
                ticketCount.Set(0);
            }
        }

        internal abstract class FlushTicket
        {
            protected internal FrozenBufferedUpdates frozenUpdates;
            protected internal bool published = false;

            protected FlushTicket(FrozenBufferedUpdates frozenUpdates)
            {
                Debug.Assert(frozenUpdates != null);
                this.frozenUpdates = frozenUpdates;
            }

            protected internal abstract void Publish(IndexWriter writer);

            protected internal abstract bool CanPublish { get; }

            /// <summary>
            /// Publishes the flushed segment, segment private deletes (if any) and its
            /// associated global delete (if present) to IndexWriter.  The actual
            /// publishing operation is synced on IW -> BDS so that the <seealso cref="SegmentInfo"/>'s
            /// delete generation is always GlobalPacket_deleteGeneration + 1
            /// </summary>
            protected void PublishFlushedSegment(IndexWriter indexWriter, FlushedSegment newSegment, FrozenBufferedUpdates globalPacket)
            {
                Debug.Assert(newSegment != null);
                Debug.Assert(newSegment.SegmentInfo != null);
                FrozenBufferedUpdates segmentUpdates = newSegment.SegmentUpdates;
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
                indexWriter.PublishFlushedSegment(newSegment.SegmentInfo, segmentUpdates, globalPacket);
            }

            protected void FinishFlush(IndexWriter indexWriter, FlushedSegment newSegment, FrozenBufferedUpdates bufferedUpdates)
            {
                // Finish the flushed segment and publish it to IndexWriter
                if (newSegment == null)
                {
                    Debug.Assert(bufferedUpdates != null);
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
                Debug.Assert(!published, "ticket was already publised - can not publish twice");
                published = true;
                // its a global ticket - no segment to publish
                FinishFlush(writer, null, frozenUpdates);
            }

            protected internal override bool CanPublish
            {
                get { return true; }
            }
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
                Debug.Assert(!published, "ticket was already publised - can not publish twice");
                published = true;
                FinishFlush(writer, segment, frozenUpdates);
            }

            internal void SetSegment(FlushedSegment segment) // LUCENENET NOTE: Made internal rather than protected because class is sealed
            {
                Debug.Assert(!failed);
                this.segment = segment;
            }

            internal void SetFailed() // LUCENENET NOTE: Made internal rather than protected because class is sealed
            {
                Debug.Assert(segment == null);
                failed = true;
            }

            protected internal override bool CanPublish
            {
                get { return segment != null || failed; }
            }
        }
    }
}