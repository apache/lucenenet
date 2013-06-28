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
	
	/// <summary>Matches the union of its clauses.</summary>
	[Serializable]
	public class SpanOrQuery : SpanQuery, ICloneable
	{
		private class AnonymousClassSpans : Spans
		{
			public AnonymousClassSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts, SpanOrQuery enclosingInstance)
			{
			    this.context = context;
			    this.acceptDocs = acceptDocs;
			    this.termContexts = termContexts;
                this.enclosingInstance = enclosingInstance;
			}
			private AtomicReaderContext context;
		    private Bits acceptDocs;
		    private IDictionary<Term, TermContext> termContexts; 
			private SpanOrQuery enclosingInstance;
			public SpanOrQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private SpanQueue queue = null;
		    private long cost;

			private bool InitSpanQueue(int target)
			{
				queue = new SpanQueue(enclosingInstance, Enclosing_Instance.clauses.Count);
				var i = Enclosing_Instance.clauses.GetEnumerator();
				while (i.MoveNext())
				{
					var spans = i.Current.GetSpans(context, acceptDocs, termContexts);
					if (((target == - 1) && spans.Next()) || ((target != - 1) && spans.SkipTo(target)))
					{
						queue.Add(spans);
					}
				}
				return queue.Size != 0;
			}
			
			public override bool Next()
			{
				if (queue == null)
				{
					return InitSpanQueue(-1);
				}
				
				if (queue.Size == 0)
				{
					// all done
					return false;
				}
				
				if (Top().Next())
				{
					// move to next
					queue.UpdateTop();
					return true;
				}
				
				queue.Pop(); // exhausted a clause
				return queue.Size != 0;
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
				
				var skipCalled = false;
				while (queue.Size != 0 && Top().Doc() < target)
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
					return queue.Size != 0;
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

            public override long Cost()
            {
                return cost;
            }

		    public override ICollection<sbyte[]> GetPayload()
		    {
		        ICollection<sbyte[]> result = null;
		        var theTop = Top();
		        if (theTop != null && theTop.IsPayloadAvailable())
		        {
		            result = theTop.GetPayload();
		        }
		        return result;
		    }

		    public override bool IsPayloadAvailable()
		    {
		        var top = Top();
		        return top != null && top.IsPayloadAvailable();
		    }

		    public override string ToString()
			{
				return "spans(" + Enclosing_Instance + ")@" + ((queue == null)?"START":(queue.Size > 0?(Doc() + ":" + Start() + "-" + End()):"END"));
			}
		}

		private IList<SpanQuery> clauses;
		private string field;
		
		/// <summary>Construct a SpanOrQuery merging the provided clauses. </summary>
		public SpanOrQuery(params SpanQuery[] clauses)
		{
			
			// copy clauses array into an ArrayList
			this.clauses = new EquatableList<SpanQuery>(clauses.Length);
            this.clauses.AddRange(clauses);
		}
		
        /// <summary>
        /// Adds a clause to this query.
        /// </summary>
        /// <param name="clause"></param>
        public void AddClause(SpanQuery clause)
        {
            if (field == null)
            {
                field = clause.Field;
            }
            else if (!clause.Field.Equals(field))
            {
                throw new ArgumentException("Clauses must have same field.");
            }
            this.clauses.Add(clause);
        }

		/// <summary>Return the clauses whose spans are matched. </summary>
		public virtual SpanQuery[] GetClauses()
		{
			return clauses.ToArray();
		}

	    public override string Field
	    {
	        get { return field; }
	    }

	    public override void  ExtractTerms(ISet<Term> terms)
		{
			foreach(var clause in clauses)
            {
				clause.ExtractTerms(terms);
			}
		}
		
		public override object Clone()
		{
			var sz = clauses.Count;
			var newClauses = new SpanQuery[sz];
			
			for (var i = 0; i < sz; i++)
			{
                newClauses[i] = (SpanQuery) clauses[i].Clone();
			}
			var soq = new SpanOrQuery(newClauses) {Boost = Boost};
		    return soq;
		}
		
		public override Query Rewrite(IndexReader reader)
		{
			SpanOrQuery clone = null;
			for (var i = 0; i < clauses.Count; i++)
			{
				var c = clauses[i];
				var query = (SpanQuery) c.Rewrite(reader);
				if (query != c)
				{
					// clause rewrote: must clone
					if (clone == null)
						clone = (SpanOrQuery) this.Clone();
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
			var buffer = new StringBuilder();
			buffer.Append("spanOr([");
			var i = clauses.GetEnumerator();
            var j = 0;
			while (i.MoveNext())
			{
                j++;
				var clause = i.Current;
				buffer.Append(clause.ToString(field));
                if (j < clauses.Count)
                {
                    buffer.Append(", ");
                }
			}
			buffer.Append("])");
			buffer.Append(ToStringUtils.Boost(Boost));
			return buffer.ToString();
		}
		
		public override bool Equals(object o)
		{
			if (this == o)
				return true;
			if (o == null || GetType() != o.GetType())
				return false;
			
			var that = (SpanOrQuery) o;
			
			if (!clauses.Equals(that.clauses))
				return false;
			if (clauses.Any() && !field.Equals(that.field))
				return false;
			
			return Boost == that.Boost;
		}
		
		public override int GetHashCode()
		{
			var h = clauses.GetHashCode();
			h ^= ((h << 10) | (Number.URShift(h, 23)));
		    h ^= Number.FloatToIntBits(Boost);
			return h;
		}
		
		
		private class SpanQueue : Util.PriorityQueue<Spans>
		{
			private SpanOrQuery enclosingInstance;
			public SpanOrQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			public SpanQueue(SpanOrQuery enclosingInstance, int size) : base(size)
			{
                this.enclosingInstance = enclosingInstance;
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
			if (clauses.Count == 1)
			// optimize 1-clause case
				return (clauses[0]).GetSpans(context, acceptDocs, termContexts);
			
			return new AnonymousClassSpans(context, acceptDocs, termContexts, this);
		}
	}
}