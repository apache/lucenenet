using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using FlushedSegment = Lucene.Net.Index.DocumentsWriterPerThread.FlushedSegment;

namespace Lucene.Net.Index
{
    internal class DocumentsWriterFlushQueue
    {
        private readonly Queue<FlushTicket> queue = new Queue<FlushTicket>();
        // we track tickets separately since count must be present even before the ticket is
        // constructed ie. queue.size would not reflect it.
        private int ticketCount = 0;
        private readonly ReentrantLock purgeLock = new ReentrantLock();

        internal void AddDeletesAndPurge(DocumentsWriter writer, DocumentsWriterDeleteQueue deleteQueue)
        {
            lock (this)
            {
                IncTickets();// first inc the ticket count - freeze opens
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
            // don't hold the lock on the FlushQueue when forcing the purge - this blocks and deadlocks 
            // if we hold the lock.
            ForcePurge(writer);
        }

        private void IncTickets()
        {
            Interlocked.Increment(ref ticketCount);
            //assert numTickets > 0;
        }

        private void DecTickets()
        {
            Interlocked.Decrement(ref ticketCount);
            //assert numTickets >= 0;
        }

        internal SegmentFlushTicket AddFlushTicket(DocumentsWriterPerThread dwpt)
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
        }

        internal void AddSegment(SegmentFlushTicket ticket, FlushedSegment segment)
        {
            lock (this)
            {
                // the actual flush is done asynchronously and once done the FlushedSegment
                // is passed to the flush ticket
                ticket.SetSegment(segment);
            }
        }

        internal void MarkTicketFailed(SegmentFlushTicket ticket)
        {
            lock (this)
            {
                // to free the queue we mark tickets as failed just to clean up the queue.
                ticket.SetFailed();
            }
        }

        internal bool HasTickets
        {
            get
            {
                //assert ticketCount.get() >= 0 : "ticketCount should be >= 0 but was: " + ticketCount.get();
                return ticketCount != 0;
            }
        }

        private void InnerPurge(DocumentsWriter writer)
        {
            //assert purgeLock.isHeldByCurrentThread();
            while (true)
            {
                FlushTicket head;
                bool canPublish;
                lock (this)
                {
                    head = queue.Peek();
                    canPublish = head != null && head.CanPublish; // do this synced 
                }
                if (canPublish)
                {
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
                            FlushTicket poll = queue.Dequeue();
                            Interlocked.Decrement(ref ticketCount);
                            //assert poll == head;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }

        internal void ForcePurge(DocumentsWriter writer)
        {
            //assert !Thread.holdsLock(this);
            purgeLock.Lock();
            try
            {
                InnerPurge(writer);
            }
            finally
            {
                purgeLock.Unlock();
            }
        }

        internal void TryPurge(DocumentsWriter writer)
        {
            //assert !Thread.holdsLock(this);
            if (purgeLock.TryLock())
            {
                try
                {
                    InnerPurge(writer);
                }
                finally
                {
                    purgeLock.Unlock();
                }
            }
        }

        public int TicketCount
        {
            get { return ticketCount; }
        }

        internal void Clear()
        {
            lock (this)
            {
                queue.Clear();
                Interlocked.Exchange(ref ticketCount, 0);
            }
        }

        internal abstract class FlushTicket
        {
            protected FrozenBufferedDeletes frozenDeletes;
            protected bool published = false;

            protected FlushTicket(FrozenBufferedDeletes frozenDeletes)
            {
                //assert frozenDeletes != null;
                this.frozenDeletes = frozenDeletes;
            }

            public abstract void Publish(DocumentsWriter writer);
            public abstract bool CanPublish { get; }
        }

        internal sealed class GlobalDeletesTicket : FlushTicket
        {
            public GlobalDeletesTicket(FrozenBufferedDeletes frozenDeletes)
                : base(frozenDeletes)
            {
            }

            public override void Publish(DocumentsWriter writer)
            {
                //assert !published : "ticket was already publised - can not publish twice";
                published = true;
                // its a global ticket - no segment to publish
                writer.FinishFlush(null, frozenDeletes);
            }

            public override bool CanPublish
            {
                get { return true; }
            }
        }

        internal sealed class SegmentFlushTicket : FlushTicket
        {
            private FlushedSegment segment;
            private bool failed = false;

            public SegmentFlushTicket(FrozenBufferedDeletes frozenDeletes)
                : base(frozenDeletes)
            {
            }

            public override void Publish(DocumentsWriter writer)
            {
                //assert !published : "ticket was already publised - can not publish twice";
                published = true;
                writer.FinishFlush(segment, frozenDeletes);
            }

            public void SetSegment(FlushedSegment segment)
            {
                //assert !failed;
                this.segment = segment;
            }

            public void SetFailed()
            {
                //assert segment == null;
                failed = true;
            }

            public override bool CanPublish
            {
                get { return segment != null || failed; }
            }
        }
    }
}
