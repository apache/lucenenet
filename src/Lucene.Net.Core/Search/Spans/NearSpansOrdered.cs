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
    using Bits = Lucene.Net.Util.Bits;
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
        private readonly int AllowedSlop;
        private bool FirstTime = true;
        private bool More = false;

        /// <summary>
        /// The spans in the same order as the SpanNearQuery </summary>
        private readonly Spans[] subSpans;

        /// <summary>
        /// Indicates that all subSpans have same doc() </summary>
        private bool InSameDoc = false;

        private int MatchDoc = -1;
        private int MatchStart = -1;
        private int MatchEnd = -1;
        private List<byte[]> MatchPayload;

        private readonly Spans[] SubSpansByDoc;

        // Even though the array is probably almost sorted, InPlaceMergeSorter will likely
        // perform better since it has a lower overhead than TimSorter for small arrays
        private readonly InPlaceMergeSorter sorter;

        private class InPlaceMergeSorterAnonymousInnerClassHelper : InPlaceMergeSorter
        {
            private readonly NearSpansOrdered OuterInstance;

            public InPlaceMergeSorterAnonymousInnerClassHelper(NearSpansOrdered outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            protected override void Swap(int i, int j)
            {
                ArrayUtil.Swap(OuterInstance.SubSpansByDoc, i, j);
            }

            protected override int Compare(int i, int j)
            {
                return OuterInstance.SubSpansByDoc[i].Doc() - OuterInstance.SubSpansByDoc[j].Doc();
            }
        }

        private SpanNearQuery Query;
        private bool CollectPayloads = true;

        public NearSpansOrdered(SpanNearQuery spanNearQuery, AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
            : this(spanNearQuery, context, acceptDocs, termContexts, true)
        {
        }

        public NearSpansOrdered(SpanNearQuery spanNearQuery, AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts, bool collectPayloads)
        {
            sorter = new InPlaceMergeSorterAnonymousInnerClassHelper(this);
            if (spanNearQuery.Clauses.Length < 2)
            {
                throw new System.ArgumentException("Less than 2 clauses: " + spanNearQuery);
            }
            this.CollectPayloads = collectPayloads;
            AllowedSlop = spanNearQuery.Slop;
            SpanQuery[] clauses = spanNearQuery.Clauses;
            subSpans = new Spans[clauses.Length];
            MatchPayload = new List<byte[]>();
            SubSpansByDoc = new Spans[clauses.Length];
            for (int i = 0; i < clauses.Length; i++)
            {
                subSpans[i] = clauses[i].GetSpans(context, acceptDocs, termContexts);
                SubSpansByDoc[i] = subSpans[i]; // used in toSameDoc()
            }
            Query = spanNearQuery; // kept for toString() only.
        }

        // inherit javadocs
        public override int Doc()
        // inherit javadocs
        {
            return MatchDoc;
        }

        public override int Start()
        // inherit javadocs
        {
            return MatchStart;
        }

        public override int End()
        {
            return MatchEnd;
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
                return MatchPayload;
            }
        }

        // TODO: Remove warning after API has been finalized
        public override bool PayloadAvailable
        {
            get
            {
                return MatchPayload.Count == 0 == false;
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
            if (FirstTime)
            {
                FirstTime = false;
                for (int i = 0; i < subSpans.Length; i++)
                {
                    if (!subSpans[i].Next())
                    {
                        More = false;
                        return false;
                    }
                }
                More = true;
            }
            if (CollectPayloads)
            {
                MatchPayload.Clear();
            }
            return AdvanceAfterOrdered();
        }

        // inherit javadocs
        public override bool SkipTo(int target)
        {
            if (FirstTime)
            {
                FirstTime = false;
                for (int i = 0; i < subSpans.Length; i++)
                {
                    if (!subSpans[i].SkipTo(target))
                    {
                        More = false;
                        return false;
                    }
                }
                More = true;
            }
            else if (More && (subSpans[0].Doc() < target))
            {
                if (subSpans[0].SkipTo(target))
                {
                    InSameDoc = false;
                }
                else
                {
                    More = false;
                    return false;
                }
            }
            if (CollectPayloads)
            {
                MatchPayload.Clear();
            }
            return AdvanceAfterOrdered();
        }

        /// <summary>
        /// Advances the subSpans to just after an ordered match with a minimum slop
        /// that is smaller than the slop allowed by the SpanNearQuery. </summary>
        /// <returns> true iff there is such a match. </returns>
        private bool AdvanceAfterOrdered()
        {
            while (More && (InSameDoc || ToSameDoc()))
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
            sorter.Sort(0, SubSpansByDoc.Length);
            int firstIndex = 0;
            int maxDoc = SubSpansByDoc[SubSpansByDoc.Length - 1].Doc();
            while (SubSpansByDoc[firstIndex].Doc() != maxDoc)
            {
                if (!SubSpansByDoc[firstIndex].SkipTo(maxDoc))
                {
                    More = false;
                    InSameDoc = false;
                    return false;
                }
                maxDoc = SubSpansByDoc[firstIndex].Doc();
                if (++firstIndex == SubSpansByDoc.Length)
                {
                    firstIndex = 0;
                }
            }
            for (int i = 0; i < SubSpansByDoc.Length; i++)
            {
                Debug.Assert((SubSpansByDoc[i].Doc() == maxDoc), " NearSpansOrdered.toSameDoc() spans " + SubSpansByDoc[0] + "\n at doc " + SubSpansByDoc[i].Doc() + ", but should be at " + maxDoc);
            }
            InSameDoc = true;
            return true;
        }

        /// <summary>
        /// Check whether two Spans in the same document are ordered. </summary>
        /// <returns> true iff spans1 starts before spans2
        ///              or the spans start at the same position,
        ///              and spans1 ends before spans2. </returns>
        internal static bool DocSpansOrdered(Spans spans1, Spans spans2)
        {
            Debug.Assert(spans1.Doc() == spans2.Doc(), "doc1 " + spans1.Doc() + " != doc2 " + spans2.Doc());
            int start1 = spans1.Start();
            int start2 = spans2.Start();
            /* Do not call docSpansOrdered(int,int,int,int) to avoid invoking .end() : */
            return (start1 == start2) ? (spans1.End() < spans2.End()) : (start1 < start2);
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
            MatchDoc = subSpans[0].Doc();
            for (int i = 1; InSameDoc && (i < subSpans.Length); i++)
            {
                while (!DocSpansOrdered(subSpans[i - 1], subSpans[i]))
                {
                    if (!subSpans[i].Next())
                    {
                        InSameDoc = false;
                        More = false;
                        break;
                    }
                    else if (MatchDoc != subSpans[i].Doc())
                    {
                        InSameDoc = false;
                        break;
                    }
                }
            }
            return InSameDoc;
        }

        /// <summary>
        /// The subSpans are ordered in the same doc, so there is a possible match.
        /// Compute the slop while making the match as short as possible by advancing
        /// all subSpans except the last one in reverse order.
        /// </summary>
        private bool ShrinkToAfterShortestMatch()
        {
            MatchStart = subSpans[subSpans.Length - 1].Start();
            MatchEnd = subSpans[subSpans.Length - 1].End();
            var possibleMatchPayloads = new HashSet<byte[]>();
            if (subSpans[subSpans.Length - 1].PayloadAvailable)
            {
                //LUCENE TO-DO UnionWith or AddAll(Set<>, IEnumerable<>)
                possibleMatchPayloads.UnionWith(subSpans[subSpans.Length - 1].Payload);
            }

            IList<byte[]> possiblePayload = null;

            int matchSlop = 0;
            int lastStart = MatchStart;
            int lastEnd = MatchEnd;
            for (int i = subSpans.Length - 2; i >= 0; i--)
            {
                Spans prevSpans = subSpans[i];
                if (CollectPayloads && prevSpans.PayloadAvailable)
                {
                    var payload = prevSpans.Payload;
                    possiblePayload = new List<byte[]>(payload.Count);
                    possiblePayload.AddRange(payload);
                }

                int prevStart = prevSpans.Start();
                int prevEnd = prevSpans.End();
                while (true) // Advance prevSpans until after (lastStart, lastEnd)
                {
                    if (!prevSpans.Next())
                    {
                        InSameDoc = false;
                        More = false;
                        break; // Check remaining subSpans for final match.
                    }
                    else if (MatchDoc != prevSpans.Doc())
                    {
                        InSameDoc = false; // The last subSpans is not advanced here.
                        break; // Check remaining subSpans for last match in this document.
                    }
                    else
                    {
                        int ppStart = prevSpans.Start();
                        int ppEnd = prevSpans.End(); // Cannot avoid invoking .end()
                        if (!DocSpansOrdered(ppStart, ppEnd, lastStart, lastEnd))
                        {
                            break; // Check remaining subSpans.
                        } // prevSpans still before (lastStart, lastEnd)
                        else
                        {
                            prevStart = ppStart;
                            prevEnd = ppEnd;
                            if (CollectPayloads && prevSpans.PayloadAvailable)
                            {
                                var payload = prevSpans.Payload;
                                possiblePayload = new List<byte[]>(payload.Count);
                                possiblePayload.AddRange(payload);
                            }
                        }
                    }
                }

                if (CollectPayloads && possiblePayload != null)
                {
                    possibleMatchPayloads.UnionWith(possiblePayload);
                }

                Debug.Assert(prevStart <= MatchStart);
                if (MatchStart > prevEnd) // Only non overlapping spans add to slop.
                {
                    matchSlop += (MatchStart - prevEnd);
                }

                /* Do not break on (matchSlop > allowedSlop) here to make sure
                 * that subSpans[0] is advanced after the match, if any.
                 */
                MatchStart = prevStart;
                lastStart = prevStart;
                lastEnd = prevEnd;
            }

            bool match = matchSlop <= AllowedSlop;

            if (CollectPayloads && match && possibleMatchPayloads.Count > 0)
            {
                MatchPayload.AddRange(possibleMatchPayloads);
            }

            return match; // ordered and allowed slop
        }

        public override string ToString()
        {
            return this.GetType().Name + "(" + Query.ToString() + ")@" + (FirstTime ? "START" : (More ? (Doc() + ":" + Start() + "-" + End()) : "END"));
        }
    }
}