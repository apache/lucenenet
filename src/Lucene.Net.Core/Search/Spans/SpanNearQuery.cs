using Lucene.Net.Support;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// Matches spans which are near one another.  One can specify <i>slop</i>, the
    /// maximum number of intervening unmatched positions, as well as whether
    /// matches are required to be in-order.
    /// </summary>
    public class SpanNearQuery : SpanQuery
    {
        protected internal readonly IList<SpanQuery> clauses; // LUCENENET TODO: rename
        protected internal int slop; // LUCENENET TODO: rename
        protected internal bool inOrder; // LUCENENET TODO: rename

        protected internal string field; // LUCENENET TODO: rename
        private bool CollectPayloads; // LUCENENET TODO: rename camelCase

        /// <summary>
        /// Construct a SpanNearQuery.  Matches spans matching a span from each
        /// clause, with up to <code>slop</code> total unmatched positions between
        /// them.  * When <code>inOrder</code> is true, the spans from each clause
        /// must be * ordered as in <code>clauses</code>. </summary>
        /// <param name="clauses"> the clauses to find near each other </param>
        /// <param name="slop"> The slop value </param>
        /// <param name="inOrder"> true if order is important
        ///  </param>
        public SpanNearQuery(SpanQuery[] clauses, int slop, bool inOrder)
            : this(clauses, slop, inOrder, true)
        {
        }

        public SpanNearQuery(SpanQuery[] clauses, int slop, bool inOrder, bool collectPayloads)
        {
            // copy clauses array into an ArrayList
            this.clauses = new List<SpanQuery>(clauses.Length);
            for (int i = 0; i < clauses.Length; i++)
            {
                SpanQuery clause = clauses[i];
                if (field == null) // check field
                {
                    field = clause.Field;
                }
                else if (clause.Field != null && !clause.Field.Equals(field))
                {
                    throw new System.ArgumentException("Clauses must have same field.");
                }
                this.clauses.Add(clause);
            }
            this.CollectPayloads = collectPayloads;
            this.slop = slop;
            this.inOrder = inOrder;
        }

        /// <summary>
        /// Return the clauses whose spans are matched. </summary>
        public virtual SpanQuery[] Clauses // LUCENENET TODO: Change to GetClauses() (array, conversion)
        {
            get
            {
                return clauses.ToArray();
            }
        }

        /// <summary>
        /// Return the maximum number of intervening unmatched positions permitted. </summary>
        public virtual int Slop
        {
            get
            {
                return slop;
            }
        }

        /// <summary>
        /// Return true if matches are required to be in-order. </summary>
        public virtual bool InOrder // LUCENENET TODO: Change to IsInOrder
        {
            get
            {
                return inOrder;
            }
        }

        public override string Field
        {
            get
            {
                return field;
            }
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (SpanQuery clause in clauses)
            {
                clause.ExtractTerms(terms);
            }
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("spanNear([");
            IEnumerator<SpanQuery> i = clauses.GetEnumerator();
            while (i.MoveNext())
            {
                SpanQuery clause = i.Current;
                buffer.Append(clause.ToString(field));
                buffer.Append(", ");
            }
            //LUCENENET TODO: Change logic above to skip this instead of removing it
            if (clauses.Count > 0)
                buffer.Remove(buffer.Length - 2, 2);
            buffer.Append("], ");
            buffer.Append(slop);
            buffer.Append(", ");
            buffer.Append(inOrder);
            buffer.Append(")");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            if (clauses.Count == 0) // optimize 0-clause case
            {
                return (new SpanOrQuery(Clauses)).GetSpans(context, acceptDocs, termContexts);
            }

            if (clauses.Count == 1) // optimize 1-clause case
            {
                return clauses[0].GetSpans(context, acceptDocs, termContexts);
            }

            return inOrder ? (Spans)new NearSpansOrdered(this, context, acceptDocs, termContexts, CollectPayloads) : (Spans)new NearSpansUnordered(this, context, acceptDocs, termContexts);
        }

        public override Query Rewrite(IndexReader reader)
        {
            SpanNearQuery clone = null;
            for (int i = 0; i < clauses.Count; i++)
            {
                SpanQuery c = clauses[i];
                SpanQuery query = (SpanQuery)c.Rewrite(reader);
                if (query != c) // clause rewrote: must clone
                {
                    if (clone == null)
                    {
                        clone = (SpanNearQuery)this.Clone();
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

        public override object Clone()
        {
            int sz = clauses.Count;
            SpanQuery[] newClauses = new SpanQuery[sz];

            for (int i = 0; i < sz; i++)
            {
                newClauses[i] = (SpanQuery)clauses[i].Clone();
            }
            SpanNearQuery spanNearQuery = new SpanNearQuery(newClauses, slop, inOrder);
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
            if (!(o is SpanNearQuery))
            {
                return false;
            }

            SpanNearQuery spanNearQuery = (SpanNearQuery)o;

            if (inOrder != spanNearQuery.inOrder)
            {
                return false;
            }
            if (slop != spanNearQuery.slop)
            {
                return false;
            }
            if (!clauses.SequenceEqual(spanNearQuery.clauses))
            {
                return false;
            }

            return Boost == spanNearQuery.Boost;
        }

        public override int GetHashCode() // LUCENENET TODO: Check whether this hash code algorithm is close enough to the original to work
        {
            int result;
            //If this doesn't work, hash all elements together. This version was used to improve the speed of hashing
            result = HashHelpers.CombineHashCodes(clauses.First().GetHashCode(), clauses.Last().GetHashCode(), clauses.Count);
            // Mix bits before folding in things like boost, since it could cancel the
            // last element of clauses.  this particular mix also serves to
            // differentiate SpanNearQuery hashcodes from others.
            result ^= (result << 14) | ((int)((uint)result >> 19)); // reversible
            result += Number.FloatToIntBits(Boost); // LUCENENET TODO: This was FloatToRawIntBits in the original
            result += slop;
            result ^= (inOrder ? unchecked((int)0x99AFD3BD) : 0);
            return result;
        }
    }
}