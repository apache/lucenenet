using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using InPlaceMergeSorter = Lucene.Net.Util.InPlaceMergeSorter;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// A <see cref="Spans"/> that is formed from the ordered subspans of a <see cref="SpanNearQuery"/>
    /// where the subspans do not overlap and have a maximum slop between them.
    /// <para/>
    /// The formed spans only contains minimum slop matches.
    /// <para/>
    /// The matching slop is computed from the distance(s) between
    /// the non overlapping matching <see cref="Spans"/>.
    /// <para/>
    /// Successive matches are always formed from the successive <see cref="Spans"/>
    /// of the <see cref="SpanNearQuery"/>.
    /// <para/>
    /// The formed spans may contain overlaps when the slop is at least 1.
    /// For example, when querying using
    /// <c>t1 t2 t3</c>
    /// with slop at least 1, the fragment:
    /// <c>t1 t2 t1 t3 t2 t3</c>
    /// matches twice:
    /// <c>t1 t2 .. t3      </c>
    /// <c>      t1 .. t2 t3</c>
    ///
    /// <para/>
    /// Expert:
    /// Only public for subclassing.  Most implementations should not need this class
    /// </summary>
    public class NearSpansOrdered : Spans
    {
        private readonly int allowedSlop;
        private bool firstTime = true;
        private bool more = false;

        /// <summary>
        /// The spans in the same order as the <see cref="SpanNearQuery"/> </summary>
        private readonly Spans[] subSpans;

        /// <summary>
        /// Indicates that all subSpans have same <see cref="Doc"/> </summary>
        private bool inSameDoc = false;

        private int matchDoc = -1;
        private int matchStart = -1;
        private int matchEnd = -1;
        private readonly JCG.List<byte[]> matchPayload; // LUCENENET: marked readonly

        private readonly Spans[] subSpansByDoc;

        // Even though the array is probably almost sorted, InPlaceMergeSorter will likely
        // perform better since it has a lower overhead than TimSorter for small arrays
        private readonly InPlaceMergeSorter sorter;

        private sealed class InPlaceMergeSorterAnonymousClass : InPlaceMergeSorter
        {
            private readonly NearSpansOrdered outerInstance;

            public InPlaceMergeSorterAnonymousClass(NearSpansOrdered outerInstance)
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

        private readonly SpanNearQuery query; // LUCENENET: marked readonly
        private readonly bool collectPayloads = true; // LUCENENET: marked readonly

        public NearSpansOrdered(SpanNearQuery spanNearQuery, AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts)
            : this(spanNearQuery, context, acceptDocs, termContexts, true)
        {
        }

        public NearSpansOrdered(SpanNearQuery spanNearQuery, AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts, bool collectPayloads)
        {
            // LUCENENET: Added guard clauses for null
            if (spanNearQuery is null)
                throw new ArgumentNullException(nameof(spanNearQuery));

            sorter = new InPlaceMergeSorterAnonymousClass(this);
            if (spanNearQuery.GetClauses().Length < 2)
            {
                throw new ArgumentException("Less than 2 clauses: " + spanNearQuery);
            }
            this.collectPayloads = collectPayloads;
            allowedSlop = spanNearQuery.Slop;
            SpanQuery[] clauses = spanNearQuery.GetClauses();
            subSpans = new Spans[clauses.Length];
            matchPayload = new JCG.List<byte[]>();
            subSpansByDoc = new Spans[clauses.Length];
            for (int i = 0; i < clauses.Length; i++)
            {
                subSpans[i] = clauses[i].GetSpans(context, acceptDocs, termContexts);
                subSpansByDoc[i] = subSpans[i]; // used in toSameDoc()
            }
            query = spanNearQuery; // kept for toString() only.
        }

        /// <summary>
        /// Returns the document number of the current match.  Initially invalid. </summary>
        public override int Doc => matchDoc;

        /// <summary>
        /// Returns the start position of the current match.  Initially invalid. </summary>
        public override int Start => matchStart;

        /// <summary>
        /// Returns the end position of the current match.  Initially invalid. </summary>
        public override int End => matchEnd;

        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual Spans[] SubSpans => subSpans;

        // TODO: Remove warning after API has been finalized
        // TODO: Would be nice to be able to lazy load payloads
        public override ICollection<byte[]> GetPayload()
        {
            return matchPayload;
        }

        // TODO: Remove warning after API has been finalized
        public override bool IsPayloadAvailable => matchPayload.Count == 0 == false;

        public override long GetCost()
        {
            long minCost = long.MaxValue;
            for (int i = 0; i < subSpans.Length; i++)
            {
                minCost = Math.Min(minCost, subSpans[i].GetCost());
            }
            return minCost;
        }

        /// <summary>
        /// Move to the next match, returning true iff any such exists. </summary>
        public override bool MoveNext()
        {
            if (firstTime)
            {
                firstTime = false;
                for (int i = 0; i < subSpans.Length; i++)
                {
                    if (!subSpans[i].MoveNext())
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

        /// <summary>
        /// Skips to the first match beyond the current, whose document number is
        /// greater than or equal to <i>target</i>.
        /// <para/>The behavior of this method is <b>undefined</b> when called with
        /// <c> target &lt;= current</c>, or after the iterator has exhausted.
        /// Both cases may result in unpredicted behavior.
        /// <para/>Returns <c>true</c> if there is such
        /// a match.  
        /// <para/>Behaves as if written: 
        /// <code>
        ///     bool SkipTo(int target) 
        ///     {
        ///         do 
        ///         {
        ///             if (!Next())
        ///                 return false;
        ///         } while (target > Doc);
        ///         return true;
        ///     }
        /// </code>
        /// Most implementations are considerably more efficient than that.
        /// </summary>
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
        /// Advances the <see cref="SubSpans"/> to just after an ordered match with a minimum slop
        /// that is smaller than the slop allowed by the <see cref="SpanNearQuery"/>. </summary>
        /// <returns> <c>true</c> if there is such a match. </returns>
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
        /// Advance the <see cref="SubSpans"/> to the same document </summary>
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
                if (Debugging.AssertsEnabled) Debugging.Assert(subSpansByDoc[i].Doc == maxDoc, " NearSpansOrdered.ToSameDoc() spans {0}\n at doc {1}, but should be at {2}", subSpansByDoc[0], subSpansByDoc[i].Doc, maxDoc);
            }
            inSameDoc = true;
            return true;
        }

        /// <summary>
        /// Check whether two <see cref="Spans"/> in the same document are ordered. </summary>
        /// <returns> <c>true</c> if <paramref name="spans1"/> starts before <paramref name="spans2"/>
        ///              or the spans start at the same position,
        ///              and <paramref name="spans1"/> ends before <paramref name="spans2"/>. </returns>
        internal static bool DocSpansOrdered(Spans spans1, Spans spans2)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(spans1.Doc == spans2.Doc,"doc1 {0} != doc2 {1}", spans1.Doc, spans2.Doc);
            int start1 = spans1.Start;
            int start2 = spans2.Start;
            /* Do not call docSpansOrdered(int,int,int,int) to avoid invoking .end() : */
            return (start1 == start2) ? (spans1.End < spans2.End) : (start1 < start2);
        }

        /// <summary>
        /// Like <see cref="DocSpansOrdered(Spans, Spans)"/>, but use the spans
        /// starts and ends as parameters.
        /// </summary>
        private static bool DocSpansOrdered(int start1, int end1, int start2, int end2)
        {
            return (start1 == start2) ? (end1 < end2) : (start1 < start2);
        }

        /// <summary>
        /// Order the <see cref="SubSpans"/> within the same document by advancing all later spans
        /// after the previous one.
        /// </summary>
        private bool StretchToOrder()
        {
            matchDoc = subSpans[0].Doc;
            for (int i = 1; inSameDoc && (i < subSpans.Length); i++)
            {
                while (!DocSpansOrdered(subSpans[i - 1], subSpans[i]))
                {
                    if (!subSpans[i].MoveNext())
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
        /// The <see cref="SubSpans"/> are ordered in the same doc, so there is a possible match.
        /// Compute the slop while making the match as short as possible by advancing
        /// all <see cref="SubSpans"/> except the last one in reverse order.
        /// </summary>
        private bool ShrinkToAfterShortestMatch()
        {
            matchStart = subSpans[subSpans.Length - 1].Start;
            matchEnd = subSpans[subSpans.Length - 1].End;
            var possibleMatchPayloads = new JCG.HashSet<byte[]>();
            if (subSpans[subSpans.Length - 1].IsPayloadAvailable)
            {
                possibleMatchPayloads.UnionWith(subSpans[subSpans.Length - 1].GetPayload());
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
                    possiblePayload = new JCG.List<byte[]>(prevSpans.GetPayload()); // LUCENENET specific - using copy constructor instead of AddRange()
                }

                int prevStart = prevSpans.Start;
                int prevEnd = prevSpans.End;
                while (true) // Advance prevSpans until after (lastStart, lastEnd)
                {
                    if (!prevSpans.MoveNext())
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
                                possiblePayload = new JCG.List<byte[]>(prevSpans.GetPayload()); // LUCENENET specific - using copy constructor instead of AddRange()
                            }
                        }
                    }
                }

                if (collectPayloads && possiblePayload != null)
                {
                    possibleMatchPayloads.UnionWith(possiblePayload);
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(prevStart <= matchStart);
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