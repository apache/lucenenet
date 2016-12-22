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
        private readonly LinkedList<FlushTicket> Queue = new LinkedList<FlushTicket>();

        // we track tickets separately since count must be present even before the ticket is
        // constructed ie. queue.size would not reflect it.
        private readonly AtomicInteger TicketCount_Renamed = new AtomicInteger();

        private readonly ReentrantLock PurgeLock = new ReentrantLock();

        internal virtual void AddDeletes(DocumentsWriterDeleteQueue deleteQueue)
        {
            lock (this)
            {
                IncTickets(); // first inc the ticket count - freeze opens
                // a window for #anyChanges to fail
                bool success = false;
                try
                {
                    Queue.AddLast(new GlobalDeletesTicket(deleteQueue.FreezeGlobalBuffer(null)));
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
            int numTickets = TicketCount_Renamed.IncrementAndGet();
            Debug.Assert(numTickets > 0);
        }

        private void DecTickets()
        {
            int numTickets = TicketCount_Renamed.DecrementAndGet();
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
                    Queue.AddLast(ticket);
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
                Debug.Assert(TicketCount_Renamed.Get() >= 0, "ticketCount should be >= 0 but was: " + TicketCount_Renamed.Get());
                return TicketCount_Renamed.Get() != 0;
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
                    head = Queue.Count <= 0 ? null : Queue.First.Value;
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
                            FlushTicket poll = Queue.First.Value;
                            Queue.RemoveFirst();
                            TicketCount_Renamed.DecrementAndGet();
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
            PurgeLock.@Lock();
            try
            {
                return InnerPurge(writer);
            }
            finally
            {
                PurgeLock.Unlock();
            }
        }

        internal virtual int TryPurge(IndexWriter writer)
        {
            //Debug.Assert(!Thread.holdsLock(this));
            //Debug.Assert(!Thread.holdsLock(writer));
            if (PurgeLock.TryLock())
            {
                try
                {
                    return InnerPurge(writer);
                }
                finally
                {
                    PurgeLock.Unlock();
                }
            }
            return 0;
        }

        public virtual int TicketCount
        {
            get
            {
                return TicketCount_Renamed.Get();
            }
        }

        internal virtual void Clear()
        {
            lock (this)
            {
                Queue.Clear();
                TicketCount_Renamed.Set(0);
            }
        }

        internal abstract class FlushTicket
        {
            protected internal FrozenBufferedUpdates FrozenUpdates;
            protected internal bool Published = false;

            protected FlushTicket(FrozenBufferedUpdates frozenUpdates)
            {
                Debug.Assert(frozenUpdates != null);
                this.FrozenUpdates = frozenUpdates;
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
                Debug.Assert(!Published, "ticket was already publised - can not publish twice");
                Published = true;
                // its a global ticket - no segment to publish
                FinishFlush(writer, null, FrozenUpdates);
            }

            protected internal override bool CanPublish
            {
                get { return true; }
            }
        }

        internal sealed class SegmentFlushTicket : FlushTicket
        {
            internal FlushedSegment Segment_Renamed;
            internal bool Failed = false;

            internal SegmentFlushTicket(FrozenBufferedUpdates frozenDeletes) // LUCENENET NOTE: Made internal rather than protected because class is sealed
                : base(frozenDeletes)
            {
            }

            protected internal override void Publish(IndexWriter writer)
            {
                Debug.Assert(!Published, "ticket was already publised - can not publish twice");
                Published = true;
                FinishFlush(writer, Segment_Renamed, FrozenUpdates);
            }

            internal void SetSegment(FlushedSegment segment) // LUCENENET NOTE: Made internal rather than protected because class is sealed
            {
                Debug.Assert(!Failed);
                this.Segment_Renamed = segment;
            }

            internal void SetFailed() // LUCENENET NOTE: Made internal rather than protected because class is sealed
            {
                Debug.Assert(Segment_Renamed == null);
                Failed = true;
            }

            protected internal override bool CanPublish
            {
                get { return Segment_Renamed != null || Failed; }
            }
        }
    }
}