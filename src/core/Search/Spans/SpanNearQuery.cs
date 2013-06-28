/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using IndexReader = Lucene.Net.Index.IndexReader;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search.Spans
{

    /// <summary>Matches spans which are near one another.  One can specify <i>slop</i>, the
    /// maximum number of intervening unmatched positions, as well as whether
    /// matches are required to be in-order. 
    /// </summary>
    [Serializable]
    public class SpanNearQuery : SpanQuery, ICloneable
    {
        protected internal IList<SpanQuery> clauses;
        protected internal int internalSlop;
        protected internal bool inOrder;

        protected internal string internalField;
        private readonly bool collectPayloads;

        /// <summary>Construct a SpanNearQuery.  Matches spans matching a span from each
        /// clause, with up to <c>slop</c> total unmatched positions between
        /// them.  * When <c>inOrder</c> is true, the spans from each clause
        /// must be * ordered as in <c>clauses</c>. 
        /// </summary>
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
                var clause = clauses[i];
                if (i == 0)
                {
                    // check field
                    internalField = clause.Field;
                }
                else if (!clause.Field.Equals(internalField))
                {
                    throw new ArgumentException("Clauses must have same field.");
                }
                this.clauses.Add(clause);
            }
            this.collectPayloads = collectPayloads;
            this.internalSlop = slop;
            this.inOrder = inOrder;
        }

        /// <summary>Return the clauses whose spans are matched. </summary>
        public virtual SpanQuery[] GetClauses()
        {
            // Return a copy
            return clauses.ToArray();
        }

        /// <summary>Return the maximum number of intervening unmatched positions permitted.</summary>
        public virtual int Slop
        {
            get { return internalSlop; }
        }

        /// <summary>Return true if matches are required to be in-order.</summary>
        public virtual bool IsInOrder
        {
            get { return inOrder; }
        }

        public override string Field
        {
            get { return internalField; }
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (var clause in clauses)
            {
                clause.ExtractTerms(terms);
            }
        }

        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
            buffer.Append("spanNear([");
            var i = clauses.GetEnumerator();
            while (i.MoveNext())
            {
                var clause = i.Current;
                buffer.Append(clause.ToString(field));
                buffer.Append(", ");
            }
            if (clauses.Count > 0) buffer.Length -= 2;
            buffer.Append("], ");
            buffer.Append(internalSlop);
            buffer.Append(", ");
            buffer.Append(inOrder);
            buffer.Append(")");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            if (clauses.Count == 0)
                // optimize 0-clause case
                return new SpanOrQuery(GetClauses()).GetSpans(context, acceptDocs, termContexts);

            if (clauses.Count == 1)
                // optimize 1-clause case
                return clauses[0].GetSpans(context, acceptDocs, termContexts);

            return inOrder ? (Spans)new NearSpansOrdered(this, context, collectPayloads) : (Spans)new NearSpansUnordered(this, context);
        }

        public override Query Rewrite(IndexReader reader)
        {
            SpanNearQuery clone = null;
            for (var i = 0; i < clauses.Count; i++)
            {
                SpanQuery c = clauses[i];
                var query = (SpanQuery)c.Rewrite(reader);
                if (query != c)
                {
                    // clause rewrote: must clone
                    if (clone == null)
                        clone = (SpanNearQuery)this.Clone();
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
            var sz = clauses.Count;
            var newClauses = new SpanQuery[sz];

            for (var i = 0; i < sz; i++)
            {
                var clause = clauses[i];
                newClauses[i] = (SpanQuery)clause.Clone();
            }
            var spanNearQuery = new SpanNearQuery(newClauses, internalSlop, inOrder) { Boost = Boost };
            return spanNearQuery;
        }

        /// <summary>Returns true iff <c>o</c> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (this == o)
                return true;
            if (!(o is SpanNearQuery))
                return false;

            var spanNearQuery = (SpanNearQuery)o;

            if (inOrder != spanNearQuery.inOrder)
                return false;
            if (internalSlop != spanNearQuery.internalSlop)
                return false;
            if (clauses.Count != spanNearQuery.clauses.Count)
                return false;

            return Boost == spanNearQuery.Boost;
        }

        public override int GetHashCode()
        {
            var result = clauses.GetHashCode();

            result ^= (result << 14) | Number.URShift(result, 19); // reversible
            result += Number.FloatToIntBits(Boost);
            result += internalSlop;
            result ^= (int)(inOrder ? 0x99AFD3BD : 0);
            return result;
        }
    }
}