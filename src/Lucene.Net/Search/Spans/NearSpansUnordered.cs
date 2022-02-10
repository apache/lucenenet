using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JCG = J2N.Collections.Generic;

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
    using IBits = Lucene.Net.Util.IBits;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// Similar to <see cref="NearSpansOrdered"/>, but for the unordered case.
    /// <para/>
    /// Expert:
    /// Only public for subclassing.  Most implementations should not need this class
    /// </summary>
    public class NearSpansUnordered : Spans
    {
        private readonly SpanNearQuery query; // LUCENENET: marked readonly

        private readonly IList<SpansCell> ordered = new JCG.List<SpansCell>(); // spans in query order // LUCENENET: marked readonly
        private readonly Spans[] subSpans; // LUCENENET: marked readonly
        private readonly int slop; // from query // LUCENENET: marked readonly

        private SpansCell first; // linked list of spans
        private SpansCell last; // sorted by doc only

        private int totalLength; // sum of current lengths

        private readonly CellQueue queue; // sorted queue of spans // LUCENENET: marked readonly
        private SpansCell max; // max element in queue

        private bool more = true; // true iff not done
        private bool firstTime = true; // true before first next()

        private class CellQueue : Util.PriorityQueue<SpansCell>
        {
            public CellQueue(int size)
                : base(size)
            {
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
        /// Wraps a <see cref="Spans"/>, and can be used to form a linked list. </summary>
        private class SpansCell : Spans
        {
            private readonly NearSpansUnordered outerInstance;

            internal Spans spans;
            internal SpansCell next;
            private int length = -1;
            private readonly int index; // LUCENENET: marked readonly

            public SpansCell(NearSpansUnordered outerInstance, Spans spans, int index)
            {
                this.outerInstance = outerInstance;
                this.spans = spans;
                this.index = index;
            }

            public override bool MoveNext()
            {
                return Adjust(spans.MoveNext());
            }

            public override bool SkipTo(int target)
            {
                return Adjust(spans.SkipTo(target));
            }

            private bool Adjust(bool condition)
            {
                if (length != -1)
                {
                    outerInstance.totalLength -= length; // subtract old length
                }
                if (condition)
                {
                    length = End - Start;
                    outerInstance.totalLength += length; // add new length

                    if (outerInstance.max is null || Doc > outerInstance.max.Doc || (Doc == outerInstance.max.Doc) && (End > outerInstance.max.End))
                    {
                        outerInstance.max = this;
                    }
                }
                outerInstance.more = condition;
                return condition;
            }

            public override int Doc => spans.Doc;

            public override int Start => spans.Start;

            // TODO: Remove warning after API has been finalized
            public override int End => spans.End;

            public override ICollection<byte[]> GetPayload()
            {
                return new JCG.List<byte[]>(spans.GetPayload());
            }

            // TODO: Remove warning after API has been finalized
            public override bool IsPayloadAvailable => spans.IsPayloadAvailable;

            public override long GetCost()
            {
                return spans.GetCost();
            }

            public override string ToString()
            {
                return spans.ToString() + "#" + index;
            }
        }

        public NearSpansUnordered(SpanNearQuery query, AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            this.query = query;
            this.slop = query.Slop;

            SpanQuery[] clauses = query.GetClauses();
            queue = new CellQueue(clauses.Length);
            subSpans = new Spans[clauses.Length];
            for (int i = 0; i < clauses.Length; i++)
            {
                SpansCell cell = new SpansCell(this, clauses[i].GetSpans(context, acceptDocs, termContexts), i);
                ordered.Add(cell);
                subSpans[i] = cell.spans;
            }
        }

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual Spans[] SubSpans => subSpans;

        public override bool MoveNext()
        {
            if (firstTime)
            {
                InitList(true);
                ListToQueue(); // initialize queue
                firstTime = false;
            }
            else if (more)
            {
                if (Min.MoveNext()) // trigger further scanning
                {
                    queue.UpdateTop(); // maintain queue
                }
                else
                {
                    more = false;
                }
            }

            while (more)
            {
                bool queueStale = false;

                if (Min.Doc != max.Doc) // maintain list
                {
                    QueueToList();
                    queueStale = true;
                }

                // skip to doc w/ all clauses

                while (more && first.Doc < last.Doc)
                {
                    more = first.SkipTo(last.Doc); // skip first upto last
                    FirstToLast(); // and move it to the end
                    queueStale = true;
                }

                if (!more)
                {
                    return false;
                }

                // found doc w/ all clauses

                if (queueStale) // maintain the queue
                {
                    ListToQueue();
                    //queueStale = false; // LUCENENET: IDE0059: Remove unnecessary value assignment (this is reset to false at the beginning of the loop)
                }

                if (AtMatch)
                {
                    return true;
                }

                more = Min.MoveNext();
                if (more)
                {
                    queue.UpdateTop(); // maintain queue
                }
            }
            return false; // no more matches
        }

        public override bool SkipTo(int target)
        {
            if (firstTime) // initialize
            {
                InitList(false);
                for (SpansCell cell = first; more && cell != null; cell = cell.next)
                {
                    more = cell.SkipTo(target); // skip all
                }
                if (more)
                {
                    ListToQueue();
                }
                firstTime = false;
            } // normal case
            else
            {
                while (more && Min.Doc < target) // skip as needed
                {
                    if (Min.SkipTo(target))
                    {
                        queue.UpdateTop();
                    }
                    else
                    {
                        more = false;
                    }
                }
            }
            return more && (AtMatch || MoveNext());
        }

        private SpansCell Min => queue.Top;

        public override int Doc => Min.Doc;

        public override int Start => Min.Start;

        public override int End => max.End;

        // TODO: Remove warning after API has been finalized
        /// <summary>
        /// WARNING: The List is not necessarily in order of the the positions </summary>
        /// <returns> Collection of <see cref="T:byte[]"/> payloads </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public override ICollection<byte[]> GetPayload()
        {
            var matchPayload = new JCG.HashSet<byte[]>();
            for (var cell = first; cell != null; cell = cell.next)
            {
                if (cell.IsPayloadAvailable)
                {
                    matchPayload.UnionWith(cell.GetPayload());
                }
            }
            return matchPayload;
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

        public override long GetCost()
        {
            long minCost = long.MaxValue;
            for (int i = 0; i < subSpans.Length; i++)
            {
                minCost = Math.Min(minCost, subSpans[i].GetCost());
            }
            return minCost;
        }

        public override string ToString()
        {
            return this.GetType().Name + "(" + query.ToString() + ")@" + (firstTime ? "START" : (more ? (Doc + ":" + Start + "-" + End) : "END"));
        }

        private void InitList(bool next)
        {
            for (int i = 0; more && i < ordered.Count; i++)
            {
                SpansCell cell = ordered[i];
                if (next)
                {
                    more = cell.MoveNext(); // move to first entry
                }
                if (more)
                {
                    AddToList(cell); // add to list
                }
            }
        }

        private void AddToList(SpansCell cell)
        {
            if (last != null) // add next to end of list
            {
                last.next = cell;
            }
            else
            {
                first = cell;
            }
            last = cell;
            cell.next = null;
        }

        private void FirstToLast()
        {
            last.next = first; // move first to end of list
            last = first;
            first = first.next;
            last.next = null;
        }

        private void QueueToList()
        {
            last = first = null;
            while (queue.Top != null)
            {
                AddToList(queue.Pop());
            }
        }

        private void ListToQueue()
        {
            queue.Clear(); // rebuild queue
            for (SpansCell cell = first; cell != null; cell = cell.next)
            {
                queue.Add(cell); // add to queue from list
            }
        }

        private bool AtMatch => (Min.Doc == max.Doc) && ((max.End - Min.Start - totalLength) <= slop);
    }
}