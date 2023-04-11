using J2N.Collections.Generic.Extensions;
using J2N.Numerics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Text;
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
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// Matches the union of its clauses. </summary>
    public class SpanOrQuery : SpanQuery // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private readonly IList<SpanQuery> clauses;
        private string field;

        /// <summary>
        /// Construct a <see cref="SpanOrQuery"/> merging the provided <paramref name="clauses"/>. </summary>
        public SpanOrQuery(params SpanQuery[] clauses) : this((IList<SpanQuery>)clauses) { }

        // LUCENENET specific overload.
        // LUCENENET TODO: API - This constructor was added to eliminate casting with PayloadSpanUtil. Make public?
        // It would be more useful if the type was an IEnumerable<SpanQuery>, but
        // need to rework the allocation below. It would also be better to change AddClause() to Add() to make
        // the C# collection initializer function.
        internal SpanOrQuery(IList<SpanQuery> clauses)
        {
            // copy clauses array into an ArrayList
            this.clauses = new JCG.List<SpanQuery>(clauses.Count);
            for (int i = 0; i < clauses.Count; i++)
            {
                AddClause(clauses[i]);
            }
        }

        /// <summary>
        /// Adds a <paramref name="clause"/> to this query </summary>
        public void AddClause(SpanQuery clause)
        {
            if (field is null)
            {
                field = clause.Field;
            }
            else if (clause.Field != null && !clause.Field.Equals(field, StringComparison.Ordinal))
            {
                throw new ArgumentException("Clauses must have same field.");
            }
            this.clauses.Add(clause);
        }

        /// <summary>
        /// Return the clauses whose spans are matched. </summary>
        public virtual SpanQuery[] GetClauses()
        {
            return clauses.ToArray();
        }

        public override string Field => field;

        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (SpanQuery clause in clauses)
            {
                clause.ExtractTerms(terms);
            }
        }

        public override object Clone()
        {
            int sz = clauses.Count;
            SpanQuery[] newClauses = new SpanQuery[sz];

            for (int i = 0; i < sz; i++)
            {
                newClauses[i] = (SpanQuery)clauses[i].Clone();
            }
            SpanOrQuery soq = new SpanOrQuery(newClauses);
            soq.Boost = Boost;
            return soq;
        }

        public override Query Rewrite(IndexReader reader)
        {
            SpanOrQuery clone = null;
            for (int i = 0; i < clauses.Count; i++)
            {
                SpanQuery c = clauses[i];
                SpanQuery query = (SpanQuery)c.Rewrite(reader);
                if (query != c) // clause rewrote: must clone
                {
                    if (clone is null)
                    {
                        clone = (SpanOrQuery)this.Clone();
                    }
                    clone.clauses[i] = query;
                }
            }
            if (clone != null)
            {
                return clone; // some clauses rewrote
            }
            else
            {
                return this; // no clauses rewrote
            }
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("spanOr([");
            bool first = true;
            foreach (SpanQuery clause in clauses)
            {
                if (!first) buffer.Append(", ");
                buffer.Append(clause.ToString(field));
                first = false;
            }
            buffer.Append("])");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (o is null || this.GetType() != o.GetType())
            {
                return false;
            }

            SpanOrQuery that = (SpanOrQuery)o;

            if (!clauses.Equals(that.clauses))
            {
                return false;
            }

            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return NumericUtils.SingleToSortableInt32(Boost) == NumericUtils.SingleToSortableInt32(that.Boost);
        }

        public override int GetHashCode()
        {
            //If this doesn't work, hash all elemnts together instead. This version was used to reduce time complexity
            int h = clauses.GetHashCode();
            h ^= (h << 10) | (h.TripleShift(23));
            h ^= J2N.BitConversion.SingleToRawInt32Bits(Boost);
            return h;
        }

        private class SpanQueue : PriorityQueue<Spans>
        {
            public SpanQueue(int size)
                : base(size)
            {
            }

            protected internal override bool LessThan(Spans spans1, Spans spans2)
            {
                if (spans1.Doc == spans2.Doc)
                {
                    if (spans1.Start == spans2.Start)
                    {
                        return spans1.End < spans2.End;
                    }
                    else
                    {
                        return spans1.Start < spans2.Start;
                    }
                }
                else
                {
                    return spans1.Doc < spans2.Doc;
                }
            }
        }

        public override Spans GetSpans(AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            if (clauses.Count == 1) // optimize 1-clause case
            {
                return (clauses[0]).GetSpans(context, acceptDocs, termContexts);
            }

            return new SpansAnonymousClass(this, context, acceptDocs, termContexts);
        }

        private sealed class SpansAnonymousClass : Spans
        {
            private readonly SpanOrQuery outerInstance;

            private readonly AtomicReaderContext context;
            private readonly IBits acceptDocs;
            private readonly IDictionary<Term, TermContext> termContexts;

            public SpansAnonymousClass(SpanOrQuery outerInstance, AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts)
            {
                this.outerInstance = outerInstance;
                this.context = context;
                this.acceptDocs = acceptDocs;
                this.termContexts = termContexts;
                queue = null;
            }

            private SpanQueue queue;
            private long cost;

            private bool InitSpanQueue(int target)
            {
                queue = new SpanQueue(outerInstance.clauses.Count);
                foreach (var clause in outerInstance.clauses)
                {
                    Spans spans = clause.GetSpans(context, acceptDocs, termContexts);
                    cost += spans.GetCost();
                    if (((target == -1) && spans.MoveNext()) || ((target != -1) && spans.SkipTo(target)))
                    {
                        queue.Add(spans);
                    }
                }
                return queue.Count != 0;
            }

            public override bool MoveNext()
            {
                if (queue is null)
                {
                    return InitSpanQueue(-1);
                }

                if (queue.Count == 0) // all done
                {
                    return false;
                }

                if (Top.MoveNext()) // move to next
                {
                    queue.UpdateTop();
                    return true;
                }

                queue.Pop(); // exhausted a clause
                return queue.Count != 0;
            }

            private Spans Top => queue.Top;

            public override bool SkipTo(int target)
            {
                if (queue is null)
                {
                    return InitSpanQueue(target);
                }

                bool skipCalled = false;
                while (queue.Count != 0 && Top.Doc < target)
                {
                    if (Top.SkipTo(target))
                    {
                        queue.UpdateTop();
                    }
                    else
                    {
                        queue.Pop();
                    }
                    skipCalled = true;
                }

                if (skipCalled)
                {
                    return queue.Count != 0;
                }
                return MoveNext();
            }

            public override int Doc => Top.Doc;

            public override int Start => Top.Start;

            public override int End => Top.End;

            public override ICollection<byte[]> GetPayload()
            {
                JCG.List<byte[]> result = null;
                Spans theTop = Top;
                if (theTop != null && theTop.IsPayloadAvailable)
                {
                    result = new JCG.List<byte[]>(theTop.GetPayload());
                }
                return result;
            }

            public override bool IsPayloadAvailable
            {
                get
                {
                    Spans top = Top;
                    return top != null && top.IsPayloadAvailable;
                }
            }

            public override string ToString()
            {
                return "spans(" + outerInstance + ")@" + ((queue is null) ? "START" : (queue.Count > 0 ? (Doc + ":" + Start + "-" + End) : "END"));
            }

            public override long GetCost()
            {
                return cost;
            }
        }
    }
}