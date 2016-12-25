using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search.Spans
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// Similar to <seealso cref="NearSpansOrdered"/>, but for the unordered case.
    ///
    /// Expert:
    /// Only public for subclassing.  Most implementations should not need this class
    /// </summary>
    public class NearSpansUnordered : Spans
    {
        private SpanNearQuery Query;

        private IList<SpansCell> Ordered = new List<SpansCell>(); // spans in query order
        private Spans[] subSpans;
        private int Slop; // from query

        private SpansCell First; // linked list of spans
        private SpansCell Last; // sorted by doc only

        private int TotalLength; // sum of current lengths

        private CellQueue Queue; // sorted queue of spans
        private SpansCell Max; // max element in queue

        private bool More = true; // true iff not done
        private bool FirstTime = true; // true before first next()

        private class CellQueue : PriorityQueue<SpansCell>
        {
            private readonly NearSpansUnordered OuterInstance;

            public CellQueue(NearSpansUnordered outerInstance, int size)
                : base(size)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override bool LessThan(SpansCell spans1, SpansCell spans2)
            {
                if (spans1.Doc == spans2.Doc)
                {
                    return NearSpansOrdered.DocSpansOrdered(spans1, spans2);
                }
                else
                {
                    return spans1.Doc < spans2.Doc;
                }
            }
        }

        /// <summary>
        /// Wraps a Spans, and can be used to form a linked list. </summary>
        private class SpansCell : Spans
        {
            private readonly NearSpansUnordered outerInstance;

            internal Spans spans;
            internal SpansCell next;
            private int length = -1;
            private int index;

            public SpansCell(NearSpansUnordered outerInstance, Spans spans, int index)
            {
                this.outerInstance = outerInstance;
                this.spans = spans;
                this.index = index;
            }

            public override bool Next()
            {
                return Adjust(spans.Next());
            }

            public override bool SkipTo(int target)
            {
                return Adjust(spans.SkipTo(target));
            }

            private bool Adjust(bool condition)
            {
                if (length != -1)
                {
                    outerInstance.TotalLength -= length; // subtract old length
                }
                if (condition)
                {
                    length = End - Start;
                    outerInstance.TotalLength += length; // add new length

                    if (outerInstance.Max == null || Doc > outerInstance.Max.Doc || (Doc == outerInstance.Max.Doc) && (End > outerInstance.Max.End))
                    {
                        outerInstance.Max = this;
                    }
                }
                outerInstance.More = condition;
                return condition;
            }

            public override int Doc
            {
                get { return spans.Doc; }
            }

            public override int Start
            {
                get { return spans.Start; }
            }

            public override int End
            // TODO: Remove warning after API has been finalized
            {
                get { return spans.End; }
            }

            public override ICollection<byte[]> Payload
            {
                get
                {
                    return new List<byte[]>(spans.Payload);
                }
            }

            // TODO: Remove warning after API has been finalized
            public override bool IsPayloadAvailable
            {
                get
                {
                    return spans.IsPayloadAvailable;
                }
            }

            public override long Cost()
            {
                return spans.Cost();
            }

            public override string ToString()
            {
                return spans.ToString() + "#" + index;
            }
        }

        public NearSpansUnordered(SpanNearQuery query, AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            this.Query = query;
            this.Slop = query.Slop;

            SpanQuery[] clauses = query.Clauses;
            Queue = new CellQueue(this, clauses.Length);
            subSpans = new Spans[clauses.Length];
            for (int i = 0; i < clauses.Length; i++)
            {
                SpansCell cell = new SpansCell(this, clauses[i].GetSpans(context, acceptDocs, termContexts), i);
                Ordered.Add(cell);
                subSpans[i] = cell.spans;
            }
        }

        public virtual Spans[] SubSpans // LUCENENET TODO: Change to GetSubSpans (array)
        {
            get
            {
                return subSpans;
            }
        }

        public override bool Next()
        {
            if (FirstTime)
            {
                InitList(true);
                ListToQueue(); // initialize queue
                FirstTime = false;
            }
            else if (More)
            {
                if (Min.Next()) // trigger further scanning
                {
                    Queue.UpdateTop(); // maintain queue
                }
                else
                {
                    More = false;
                }
            }

            while (More)
            {
                bool queueStale = false;

                if (Min.Doc != Max.Doc) // maintain list
                {
                    QueueToList();
                    queueStale = true;
                }

                // skip to doc w/ all clauses

                while (More && First.Doc < Last.Doc)
                {
                    More = First.SkipTo(Last.Doc); // skip first upto last
                    FirstToLast(); // and move it to the end
                    queueStale = true;
                }

                if (!More)
                {
                    return false;
                }

                // found doc w/ all clauses

                if (queueStale) // maintain the queue
                {
                    ListToQueue();
                    queueStale = false;
                }

                if (AtMatch)
                {
                    return true;
                }

                More = Min.Next();
                if (More)
                {
                    Queue.UpdateTop(); // maintain queue
                }
            }
            return false; // no more matches
        }

        public override bool SkipTo(int target)
        {
            if (FirstTime) // initialize
            {
                InitList(false);
                for (SpansCell cell = First; More && cell != null; cell = cell.next)
                {
                    More = cell.SkipTo(target); // skip all
                }
                if (More)
                {
                    ListToQueue();
                }
                FirstTime = false;
            } // normal case
            else
            {
                while (More && Min.Doc < target) // skip as needed
                {
                    if (Min.SkipTo(target))
                    {
                        Queue.UpdateTop();
                    }
                    else
                    {
                        More = false;
                    }
                }
            }
            return More && (AtMatch || Next());
        }

        private SpansCell Min
        {
            get { return Queue.Top(); }
        }

        public override int Doc
        {
            get { return Min.Doc; }
        }

        public override int Start
        {
            get { return Min.Start; }
        }

        // TODO: Remove warning after API has been finalized
        /// <summary>
        /// WARNING: The List is not necessarily in order of the the positions </summary>
        /// <returns> Collection of <code>byte[]</code> payloads </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public override int End
        
        {
            get { return Max.End; }
        }

        public override ICollection<byte[]> Payload
        {
            get
            {
                var matchPayload = new HashSet<byte[]>();
                for (var cell = First; cell != null; cell = cell.next)
                {
                    if (cell.IsPayloadAvailable)
                    {
                        matchPayload.UnionWith(cell.Payload);
                    }
                }
                return matchPayload;
            }
        }

        // TODO: Remove warning after API has been finalized
        public override bool IsPayloadAvailable
        {
            get
            {
                SpansCell pointer = Min;
                while (pointer != null)
                {
                    if (pointer.IsPayloadAvailable)
                    {
                        return true;
                    }
                    pointer = pointer.next;
                }

                return false;
            }
        }

        public override long Cost()
        {
            long minCost = long.MaxValue;
            for (int i = 0; i < subSpans.Length; i++)
            {
                minCost = Math.Min(minCost, subSpans[i].Cost());
            }
            return minCost;
        }

        public override string ToString()
        {
            return this.GetType().Name + "(" + Query.ToString() + ")@" + (FirstTime ? "START" : (More ? (Doc + ":" + Start + "-" + End) : "END"));
        }

        private void InitList(bool next)
        {
            for (int i = 0; More && i < Ordered.Count; i++)
            {
                SpansCell cell = Ordered[i];
                if (next)
                {
                    More = cell.Next(); // move to first entry
                }
                if (More)
                {
                    AddToList(cell); // add to list
                }
            }
        }

        private void AddToList(SpansCell cell)
        {
            if (Last != null) // add next to end of list
            {
                Last.next = cell;
            }
            else
            {
                First = cell;
            }
            Last = cell;
            cell.next = null;
        }

        private void FirstToLast()
        {
            Last.next = First; // move first to end of list
            Last = First;
            First = First.next;
            Last.next = null;
        }

        private void QueueToList()
        {
            Last = First = null;
            while (Queue.Top() != null)
            {
                AddToList(Queue.Pop());
            }
        }

        private void ListToQueue()
        {
            Queue.Clear(); // rebuild queue
            for (SpansCell cell = First; cell != null; cell = cell.next)
            {
                Queue.Add(cell); // add to queue from list
            }
        }

        private bool AtMatch
        {
            get { return (Min.Doc == Max.Doc) && ((Max.End - Min.Start - TotalLength) <= Slop); }
        }
    }
}