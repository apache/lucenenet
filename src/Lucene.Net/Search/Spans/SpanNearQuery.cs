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
    /// Matches spans which are near one another.  One can specify <i>slop</i>, the
    /// maximum number of intervening unmatched positions, as well as whether
    /// matches are required to be in-order.
    /// </summary>
    public class SpanNearQuery : SpanQuery // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        protected readonly IList<SpanQuery> m_clauses;
        protected int m_slop;
        protected bool m_inOrder;

        protected string m_field;
        private readonly bool collectPayloads; // LUCENENET: marked readonly

        /// <summary>
        /// Construct a <see cref="SpanNearQuery"/>.  Matches spans matching a span from each
        /// clause, with up to <paramref name="slop"/> total unmatched positions between
        /// them.  * When <paramref name="inOrder"/> is <c>true</c>, the spans from each clause
        /// must be * ordered as in <paramref name="clauses"/>. </summary>
        /// <param name="clauses"> The clauses to find near each other </param>
        /// <param name="slop"> The slop value </param>
        /// <param name="inOrder"> <c>true</c> if order is important</param>
        public SpanNearQuery(SpanQuery[] clauses, int slop, bool inOrder)
            : this(clauses, slop, inOrder, true)
        {
        }

        public SpanNearQuery(SpanQuery[] clauses, int slop, bool inOrder, bool collectPayloads)
        {
            // copy clauses array into an ArrayList
            this.m_clauses = new JCG.List<SpanQuery>(clauses.Length);
            for (int i = 0; i < clauses.Length; i++)
            {
                SpanQuery clause = clauses[i];
                if (m_field is null) // check field
                {
                    m_field = clause.Field;
                }
                else if (clause.Field != null && !clause.Field.Equals(m_field, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Clauses must have same field.");
                }
                this.m_clauses.Add(clause);
            }
            this.collectPayloads = collectPayloads;
            this.m_slop = slop;
            this.m_inOrder = inOrder;
        }

        /// <summary>
        /// Return the clauses whose spans are matched. </summary>
        public virtual SpanQuery[] GetClauses()
        {
            return m_clauses.ToArray();
        }

        /// <summary>
        /// Return the maximum number of intervening unmatched positions permitted. </summary>
        public virtual int Slop => m_slop;

        /// <summary>
        /// Return <c>true</c> if matches are required to be in-order. </summary>
        public virtual bool IsInOrder => m_inOrder;

        public override string Field => m_field;

        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (SpanQuery clause in m_clauses)
            {
                clause.ExtractTerms(terms);
            }
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("SpanNear([");
            bool isFirst = true;
            foreach (SpanQuery clause in m_clauses)
            {
                if (!isFirst)
                {
                    buffer.Append(", ");
                }
                buffer.Append(clause.ToString(field));
                isFirst = false;
            }
            buffer.Append("], ");
            buffer.Append(m_slop);
            buffer.Append(", ");
            buffer.Append(m_inOrder);
            buffer.Append(')');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override Spans GetSpans(AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            if (m_clauses.Count == 0) // optimize 0-clause case
            {
                return (new SpanOrQuery(GetClauses())).GetSpans(context, acceptDocs, termContexts);
            }

            if (m_clauses.Count == 1) // optimize 1-clause case
            {
                return m_clauses[0].GetSpans(context, acceptDocs, termContexts);
            }

            return m_inOrder ? (Spans)new NearSpansOrdered(this, context, acceptDocs, termContexts, collectPayloads) : (Spans)new NearSpansUnordered(this, context, acceptDocs, termContexts);
        }

        public override Query Rewrite(IndexReader reader)
        {
            SpanNearQuery clone = null;
            for (int i = 0; i < m_clauses.Count; i++)
            {
                SpanQuery c = m_clauses[i];
                SpanQuery query = (SpanQuery)c.Rewrite(reader);
                if (query != c) // clause rewrote: must clone
                {
                    if (clone is null)
                    {
                        clone = (SpanNearQuery)this.Clone();
                    }
                    clone.m_clauses[i] = query;
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

        public override object Clone()
        {
            int sz = m_clauses.Count;
            SpanQuery[] newClauses = new SpanQuery[sz];

            for (int i = 0; i < sz; i++)
            {
                newClauses[i] = (SpanQuery)m_clauses[i].Clone();
            }
            SpanNearQuery spanNearQuery = new SpanNearQuery(newClauses, m_slop, m_inOrder);
            spanNearQuery.Boost = Boost;
            return spanNearQuery;
        }

        /// <summary>
        /// Returns true iff <code>o</code> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!(o is SpanNearQuery spanNearQuery))
            {
                return false;
            }

            if (m_inOrder != spanNearQuery.m_inOrder)
            {
                return false;
            }
            if (m_slop != spanNearQuery.m_slop)
            {
                return false;
            }
            if (!JCG.ListEqualityComparer<SpanQuery>.Default.Equals(m_clauses, spanNearQuery.m_clauses))
            {
                return false;
            }

            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return NumericUtils.SingleToSortableInt32(Boost) == NumericUtils.SingleToSortableInt32(spanNearQuery.Boost);
        }

        public override int GetHashCode()
        {
            int result;
            result = JCG.ListEqualityComparer<SpanQuery>.Default.GetHashCode(m_clauses);
            // Mix bits before folding in things like boost, since it could cancel the
            // last element of clauses.  this particular mix also serves to
            // differentiate SpanNearQuery hashcodes from others.
            result ^= (result << 14) | (result.TripleShift(19)); // reversible
            result += J2N.BitConversion.SingleToRawInt32Bits(Boost);
            result += m_slop;
            result ^= (m_inOrder ? unchecked((int)0x99AFD3BD) : 0);
            return result;
        }
    }
}