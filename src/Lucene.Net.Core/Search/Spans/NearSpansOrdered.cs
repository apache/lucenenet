using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using InPlaceMergeSorter = Lucene.Net.Util.InPlaceMergeSorter;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// A Spans that is formed from the ordered subspans of a SpanNearQuery
    /// where the subspans do not overlap and have a maximum slop between them.
    /// <p>
    /// The formed spans only contains minimum slop matches.<br>
    /// The matching slop is computed from the distance(s) between
    /// the non overlapping matching Spans.<br>
    /// Successive matches are always formed from the successive Spans
    /// of the SpanNearQuery.
    /// <p>
    /// The formed spans may contain overlaps when the slop is at least 1.
    /// For example, when querying using
    /// <pre>t1 t2 t3</pre>
    /// with slop at least 1, the fragment:
    /// <pre>t1 t2 t1 t3 t2 t3</pre>
    /// matches twice:
    /// <pre>t1 t2 .. t3      </pre>
    /// <pre>      t1 .. t2 t3</pre>
    ///
    ///
    /// Expert:
    /// Only public for subclassing.  Most implementations should not need this class
    /// </summary>
    public class NearSpansOrdered : Spans
    {
        private readonly int allowedSlop;
        private bool firstTime = true;
        private bool more = false;

        /// <summary>
        /// The spans in the same order as the SpanNearQuery </summary>
        private readonly Spans[] subSpans;

        /// <summary>
        /// Indicates that all subSpans have same doc() </summary>
        private bool inSameDoc = false;

        private int matchDoc = -1;
        private int matchStart = -1;
        private int matchEnd = -1;
        private List<byte[]> matchPayload;

        private readonly Spans[] subSpansByDoc;

        // Even though the array is probably almost sorted, InPlaceMergeSorter will likely
        // perform better since it has a lower overhead than TimSorter for small arrays
        private readonly InPlaceMergeSorter sorter;

        private class InPlaceMergeSorterAnonymousInnerClassHelper : InPlaceMergeSorter
        {
            private readonly NearSpansOrdered outerInstance;

            public InPlaceMergeSorterAnonymousInnerClassHelper(NearSpansOrdered outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override void Swap(int i, int j)
            {
                ArrayUtil.Swap(outerInstance.subSpansByDoc, i, j);
            }

            protected override int Compare(int i, int j)
            {
                return outerInstance.subSpansByDoc[i].Doc - outerInstance.subSpansByDoc[j].Doc;
            }
        }

        private SpanNearQuery query;
        private bool collectPayloads = true;

        public NearSpansOrdered(SpanNearQuery spanNearQuery, AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts)
            : this(spanNearQuery, context, acceptDocs, termContexts, true)
        {
        }

        public NearSpansOrdered(SpanNearQuery spanNearQuery, AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts, bool collectPayloads)
        {
            sorter = new InPlaceMergeSorterAnonymousInnerClassHelper(this);
            if (spanNearQuery.Clauses.Length < 2)
            {
                throw new System.ArgumentException("Less than 2 clauses: " + spanNearQuery);
            }
            this.collectPayloads = collectPayloads;
            allowedSlop = spanNearQuery.Slop;
            SpanQuery[] clauses = spanNearQuery.Clauses;
            subSpans = new Spans[clauses.Length];
            matchPayload = new List<byte[]>();
            subSpansByDoc = new Spans[clauses.Length];
            for (int i = 0; i < clauses.Length; i++)
            {
                subSpans[i] = clauses[i].GetSpans(context, acceptDocs, termContexts);
                subSpansByDoc[i] = subSpans[i]; // used in toSameDoc()
            }
            query = spanNearQuery; // kept for toString() only.
        }

        // inherit javadocs
        public override int Doc
        // inherit javadocs
        {
            get { return matchDoc; }
        }

        public override int Start
        // inherit javadocs
        {
            get { return matchStart; }
        }

        public override int End
        {
            get { return matchEnd; }
        }

        public virtual Spans[] SubSpans // LUCENENET TODO: Make GetSubSpans() (properties shouldn't return array)
        {
            get
            {
                return subSpans;
            }
        }

        // TODO: Remove warning after API has been finalized
        // TODO: Would be nice to be able to lazy load payloads
        public override ICollection<byte[]> Payload
        {
            get
            {
                return matchPayload;
            }
        }

        // TODO: Remove warning after API has been finalized
        public override bool IsPayloadAvailable
        {
            get
            {
                return matchPayload.Count == 0 == false;
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

        // inherit javadocs
        public override bool Next()
        {
            if (firstTime)
            {
                firstTime = false;
                for (int i = 0; i < subSpans.Length; i++)
                {
                    if (!subSpans[i].Next())
                    {
                        more = false;
                        return false;
                    }
                }
                more = true;
            }
            if (collectPayloads)
            {
                matchPayload.Clear();
            }
            return AdvanceAfterOrdered();
        }

        // inherit javadocs
        public override bool SkipTo(int target)
        {
            if (firstTime)
            {
                firstTime = false;
                for (int i = 0; i < subSpans.Length; i++)
                {
                    if (!subSpans[i].SkipTo(target))
                    {
                        more = false;
                        return false;
                    }
                }
                more = true;
            }
            else if (more && (subSpans[0].Doc < target))
            {
                if (subSpans[0].SkipTo(target))
                {
                    inSameDoc = false;
                }
                else
                {
                    more = false;
                    return false;
                }
            }
            if (collectPayloads)
            {
                matchPayload.Clear();
            }
            return AdvanceAfterOrdered();
        }

        /// <summary>
        /// Advances the subSpans to just after an ordered match with a minimum slop
        /// that is smaller than the slop allowed by the SpanNearQuery. </summary>
        /// <returns> true iff there is such a match. </returns>
        private bool AdvanceAfterOrdered()
        {
            while (more && (inSameDoc || ToSameDoc()))
            {
                if (StretchToOrder() && ShrinkToAfterShortestMatch())
                {
                    return true;
                }
            }
            return false; // no more matches
        }

        /// <summary>
        /// Advance the subSpans to the same document </summary>
        private bool ToSameDoc()
        {
            sorter.Sort(0, subSpansByDoc.Length);
            int firstIndex = 0;
            int maxDoc = subSpansByDoc[subSpansByDoc.Length - 1].Doc;
            while (subSpansByDoc[firstIndex].Doc != maxDoc)
            {
                if (!subSpansByDoc[firstIndex].SkipTo(maxDoc))
                {
                    more = false;
                    inSameDoc = false;
                    return false;
                }
                maxDoc = subSpansByDoc[firstIndex].Doc;
                if (++firstIndex == subSpansByDoc.Length)
                {
                    firstIndex = 0;
                }
            }
            for (int i = 0; i < subSpansByDoc.Length; i++)
            {
                Debug.Assert((subSpansByDoc[i].Doc == maxDoc), " NearSpansOrdered.toSameDoc() spans " + subSpansByDoc[0] + "\n at doc " + subSpansByDoc[i].Doc + ", but should be at " + maxDoc);
            }
            inSameDoc = true;
            return true;
        }

        /// <summary>
        /// Check whether two Spans in the same document are ordered. </summary>
        /// <returns> true iff spans1 starts before spans2
        ///              or the spans start at the same position,
        ///              and spans1 ends before spans2. </returns>
        internal static bool DocSpansOrdered(Spans spans1, Spans spans2)
        {
            Debug.Assert(spans1.Doc == spans2.Doc, "doc1 " + spans1.Doc + " != doc2 " + spans2.Doc);
            int start1 = spans1.Start;
            int start2 = spans2.Start;
            /* Do not call docSpansOrdered(int,int,int,int) to avoid invoking .end() : */
            return (start1 == start2) ? (spans1.End < spans2.End) : (start1 < start2);
        }

        /// <summary>
        /// Like <seealso cref="#docSpansOrdered(Spans,Spans)"/>, but use the spans
        /// starts and ends as parameters.
        /// </summary>
        private static bool DocSpansOrdered(int start1, int end1, int start2, int end2)
        {
            return (start1 == start2) ? (end1 < end2) : (start1 < start2);
        }

        /// <summary>
        /// Order the subSpans within the same document by advancing all later spans
        /// after the previous one.
        /// </summary>
        private bool StretchToOrder()
        {
            matchDoc = subSpans[0].Doc;
            for (int i = 1; inSameDoc && (i < subSpans.Length); i++)
            {
                while (!DocSpansOrdered(subSpans[i - 1], subSpans[i]))
                {
                    if (!subSpans[i].Next())
                    {
                        inSameDoc = false;
                        more = false;
                        break;
                    }
                    else if (matchDoc != subSpans[i].Doc)
                    {
                        inSameDoc = false;
                        break;
                    }
                }
            }
            return inSameDoc;
        }

        /// <summary>
        /// The subSpans are ordered in the same doc, so there is a possible match.
        /// Compute the slop while making the match as short as possible by advancing
        /// all subSpans except the last one in reverse order.
        /// </summary>
        private bool ShrinkToAfterShortestMatch()
        {
            matchStart = subSpans[subSpans.Length - 1].Start;
            matchEnd = subSpans[subSpans.Length - 1].End;
            var possibleMatchPayloads = new HashSet<byte[]>();
            if (subSpans[subSpans.Length - 1].IsPayloadAvailable)
            {
                //LUCENE TO-DO UnionWith or AddAll(Set<>, IEnumerable<>)
                possibleMatchPayloads.UnionWith(subSpans[subSpans.Length - 1].Payload);
            }

            IList<byte[]> possiblePayload = null;

            int matchSlop = 0;
            int lastStart = matchStart;
            int lastEnd = matchEnd;
            for (int i = subSpans.Length - 2; i >= 0; i--)
            {
                Spans prevSpans = subSpans[i];
                if (collectPayloads && prevSpans.IsPayloadAvailable)
                {
                    var payload = prevSpans.Payload;
                    possiblePayload = new List<byte[]>(payload.Count);
                    possiblePayload.AddRange(payload);
                }

                int prevStart = prevSpans.Start;
                int prevEnd = prevSpans.End;
                while (true) // Advance prevSpans until after (lastStart, lastEnd)
                {
                    if (!prevSpans.Next())
                    {
                        inSameDoc = false;
                        more = false;
                        break; // Check remaining subSpans for final match.
                    }
                    else if (matchDoc != prevSpans.Doc)
                    {
                        inSameDoc = false; // The last subSpans is not advanced here.
                        break; // Check remaining subSpans for last match in this document.
                    }
                    else
                    {
                        int ppStart = prevSpans.Start;
                        int ppEnd = prevSpans.End; // Cannot avoid invoking .end()
                        if (!DocSpansOrdered(ppStart, ppEnd, lastStart, lastEnd))
                        {
                            break; // Check remaining subSpans.
                        } // prevSpans still before (lastStart, lastEnd)
                        else
                        {
                            prevStart = ppStart;
                            prevEnd = ppEnd;
                            if (collectPayloads && prevSpans.IsPayloadAvailable)
                            {
                                var payload = prevSpans.Payload;
                                possiblePayload = new List<byte[]>(payload.Count);
                                possiblePayload.AddRange(payload);
                            }
                        }
                    }
                }

                if (collectPayloads && possiblePayload != null)
                {
                    possibleMatchPayloads.UnionWith(possiblePayload);
                }

                Debug.Assert(prevStart <= matchStart);
                if (matchStart > prevEnd) // Only non overlapping spans add to slop.
                {
                    matchSlop += (matchStart - prevEnd);
                }

                /* Do not break on (matchSlop > allowedSlop) here to make sure
                 * that subSpans[0] is advanced after the match, if any.
                 */
                matchStart = prevStart;
                lastStart = prevStart;
                lastEnd = prevEnd;
            }

            bool match = matchSlop <= allowedSlop;

            if (collectPayloads && match && possibleMatchPayloads.Count > 0)
            {
                matchPayload.AddRange(possibleMatchPayloads);
            }

            return match; // ordered and allowed slop
        }

        public override string ToString()
        {
            return this.GetType().Name + "(" + query.ToString() + ")@" + (firstTime ? "START" : (more ? (Doc + ":" + Start + "-" + End) : "END"));
        }
    }
}