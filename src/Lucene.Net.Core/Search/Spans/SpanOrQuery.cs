using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Spans
{
    using Lucene.Net.Support;

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
    /// Matches the union of its clauses. </summary>
    public class SpanOrQuery : SpanQuery
    {
        private readonly IList<SpanQuery> clauses;
        private string field;

        /// <summary>
        /// Construct a SpanOrQuery merging the provided clauses. </summary>
        public SpanOrQuery(params SpanQuery[] clauses)
        {
            // copy clauses array into an ArrayList
            this.clauses = new List<SpanQuery>(clauses.Length);
            for (int i = 0; i < clauses.Length; i++)
            {
                AddClause(clauses[i]);
            }
        }

        /// <summary>
        /// Adds a clause to this query </summary>
        public void AddClause(SpanQuery clause)
        {
            if (field == null)
            {
                field = clause.Field;
            }
            else if (clause.Field != null && !clause.Field.Equals(field))
            {
                throw new System.ArgumentException("Clauses must have same field.");
            }
            this.clauses.Add(clause);
        }

        /// <summary>
        /// Return the clauses whose spans are matched. </summary>
        public virtual SpanQuery[] Clauses
        {
            get
            {
                return clauses.ToArray();
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
                    if (clone == null)
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
            IEnumerator<SpanQuery> i = clauses.GetEnumerator();
            while (i.MoveNext())
            {
                SpanQuery clause = i.Current;
                buffer.Append(clause.ToString(field));
                buffer.Append(", ");
            }
            //LUCENE TO-DO
            if (clauses.Count > 0)
                buffer.Remove(buffer.Length - 2, 2);
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
            if (o == null || this.GetType() != o.GetType())
            {
                return false;
            }

            SpanOrQuery that = (SpanOrQuery)o;

            if (!clauses.SequenceEqual(that.clauses))
            {
                return false;
            }

            return Boost == that.Boost;
        }

        public override int GetHashCode()
        {
            //If this doesn't work, hash all elemnts together instead. This version was used to reduce time complexity
            int h = clauses.Count == 0 ? 0 : HashHelpers.CombineHashCodes(clauses.First().GetHashCode(), clauses.Last().GetHashCode(), clauses.Count);
            h ^= (h << 10) | ((int)(((uint)h) >> 23));
            h ^= Number.FloatToIntBits(Boost);
            return h;
        }

        private class SpanQueue : Util.PriorityQueue<Spans>
        {
            private readonly SpanOrQuery OuterInstance;

            public SpanQueue(SpanOrQuery outerInstance, int size)
                : base(size)
            {
                this.OuterInstance = outerInstance;
            }

            public override bool LessThan(Spans spans1, Spans spans2)
            {
                if (spans1.Doc() == spans2.Doc())
                {
                    if (spans1.Start() == spans2.Start())
                    {
                        return spans1.End() < spans2.End();
                    }
                    else
                    {
                        return spans1.Start() < spans2.Start();
                    }
                }
                else
                {
                    return spans1.Doc() < spans2.Doc();
                }
            }
        }

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            if (clauses.Count == 1) // optimize 1-clause case
            {
                return (clauses[0]).GetSpans(context, acceptDocs, termContexts);
            }

            return new SpansAnonymousInnerClassHelper(this, context, acceptDocs, termContexts);
        }

        private class SpansAnonymousInnerClassHelper : Spans
        {
            private readonly SpanOrQuery OuterInstance;

            private AtomicReaderContext Context;
            private Bits AcceptDocs;
            private IDictionary<Term, TermContext> TermContexts;

            public SpansAnonymousInnerClassHelper(SpanOrQuery outerInstance, AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
            {
                this.OuterInstance = outerInstance;
                this.Context = context;
                this.AcceptDocs = acceptDocs;
                this.TermContexts = termContexts;
                queue = null;
            }

            private SpanQueue queue;
            private long cost;

            private bool InitSpanQueue(int target)
            {
                queue = new SpanQueue(OuterInstance, OuterInstance.clauses.Count);
                IEnumerator<SpanQuery> i = OuterInstance.clauses.GetEnumerator();
                while (i.MoveNext())
                {
                    Spans spans = i.Current.GetSpans(Context, AcceptDocs, TermContexts);
                    cost += spans.Cost();
                    if (((target == -1) && spans.Next()) || ((target != -1) && spans.SkipTo(target)))
                    {
                        queue.Add(spans);
                    }
                }
                return queue.Size() != 0;
            }

            public override bool Next()
            {
                if (queue == null)
                {
                    return InitSpanQueue(-1);
                }

                if (queue.Size() == 0) // all done
                {
                    return false;
                }

                if (Top().Next()) // move to next
                {
                    queue.UpdateTop();
                    return true;
                }

                queue.Pop(); // exhausted a clause
                return queue.Size() != 0;
            }

            private Spans Top()
            {
                return queue.Top();
            }

            public override bool SkipTo(int target)
            {
                if (queue == null)
                {
                    return InitSpanQueue(target);
                }

                bool skipCalled = false;
                while (queue.Size() != 0 && Top().Doc() < target)
                {
                    if (Top().SkipTo(target))
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
                    return queue.Size() != 0;
                }
                return Next();
            }

            public override int Doc()
            {
                return Top().Doc();
            }

            public override int Start()
            {
                return Top().Start();
            }

            public override int End()
            {
                return Top().End();
            }

            public override ICollection<byte[]> Payload
            {
                get
                {
                    List<byte[]> result = null;
                    Spans theTop = Top();
                    if (theTop != null && theTop.PayloadAvailable)
                    {
                        result = new List<byte[]>(theTop.Payload);
                    }
                    return result;
                }
            }

            public override bool PayloadAvailable
            {
                get
                {
                    Spans top = Top();
                    return top != null && top.PayloadAvailable;
                }
            }

            public override string ToString()
            {
                return "spans(" + OuterInstance + ")@" + ((queue == null) ? "START" : (queue.Size() > 0 ? (Doc() + ":" + Start() + "-" + End()) : "END"));
            }

            public override long Cost()
            {
                return cost;
            }
        }
    }
}