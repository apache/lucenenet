/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
using IndexReader = Lucene.Net.Index.IndexReader;
using Query = Lucene.Net.Search.Query;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search.Spans
{
	
	/// <summary>Matches spans which are near one another.  One can specify <i>slop</i>, the
	/// maximum number of intervening unmatched positions, as well as whether
	/// matches are required to be in-order. 
	/// </summary>
	[Serializable]
	public class SpanNearQuery : SpanQuery
	{
		private System.Collections.ArrayList clauses;
		private int slop;
		private bool inOrder;
		
		private System.String field;
		
		/// <summary>Construct a SpanNearQuery.  Matches spans matching a span from each
		/// clause, with up to <code>slop</code> total unmatched positions between
		/// them.  * When <code>inOrder</code> is true, the spans from each clause
		/// must be * ordered as in <code>clauses</code>. 
		/// </summary>
		public SpanNearQuery(SpanQuery[] clauses, int slop, bool inOrder)
		{
			
			// copy clauses array into an ArrayList
			this.clauses = new System.Collections.ArrayList(clauses.Length);
			for (int i = 0; i < clauses.Length; i++)
			{
				SpanQuery clause = clauses[i];
				if (i == 0)
				{
					// check field
					field = clause.GetField();
				}
				else if (!clause.GetField().Equals(field))
				{
					throw new System.ArgumentException("Clauses must have same field.");
				}
				this.clauses.Add(clause);
			}
			
			this.slop = slop;
			this.inOrder = inOrder;
		}
		
		/// <summary>Return the clauses whose spans are matched. </summary>
		public virtual SpanQuery[] GetClauses()
		{
			return (SpanQuery[]) clauses.ToArray(typeof(SpanQuery));
		}
		
		/// <summary>Return the maximum number of intervening unmatched positions permitted.</summary>
		public virtual int GetSlop()
		{
			return slop;
		}
		
		/// <summary>Return true if matches are required to be in-order.</summary>
		public virtual bool IsInOrder()
		{
			return inOrder;
		}
		
		public override System.String GetField()
		{
			return field;
		}
		
		public override System.Collections.ICollection GetTerms()
		{
			System.Collections.ArrayList terms = new System.Collections.ArrayList();
			System.Collections.IEnumerator i = clauses.GetEnumerator();
			while (i.MoveNext())
			{
				SpanQuery clause = (SpanQuery) i.Current;
				terms.AddRange(clause.GetTerms());
			}
			return terms;
		}
		
		public override System.String ToString(System.String field)
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			buffer.Append("spanNear([");
			System.Collections.IEnumerator i = clauses.GetEnumerator();
			while (i.MoveNext())
			{
				SpanQuery clause = (SpanQuery) i.Current;
				buffer.Append(clause.ToString(field));
				if (i.MoveNext())
				{
					buffer.Append(", ");
				}
			}
			buffer.Append("], ");
			buffer.Append(slop);
			buffer.Append(", ");
			buffer.Append(inOrder);
			buffer.Append(")");
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}
		
		public override Spans GetSpans(IndexReader reader)
		{
			if (clauses.Count == 0)
			// optimize 0-clause case
				return new SpanOrQuery(GetClauses()).GetSpans(reader);
			
			if (clauses.Count == 1)
			// optimize 1-clause case
				return ((SpanQuery) clauses[0]).GetSpans(reader);
			
			return new NearSpans(this, reader);
		}
		
		public override Query Rewrite(IndexReader reader)
		{
			SpanNearQuery clone = null;
			for (int i = 0; i < clauses.Count; i++)
			{
				SpanQuery c = (SpanQuery) clauses[i];
				SpanQuery query = (SpanQuery) c.Rewrite(reader);
				if (query != c)
				{
					// clause rewrote: must clone
					if (clone == null)
						clone = (SpanNearQuery) this.Clone();
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
		
		/// <summary>Returns true iff <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (this == o)
				return true;
			if (o == null || GetType() != o.GetType())
				return false;
			
			SpanNearQuery spanNearQuery = (SpanNearQuery) o;
			
			if (inOrder != spanNearQuery.inOrder)
				return false;
			if (slop != spanNearQuery.slop)
				return false;
			if (!clauses.Equals(spanNearQuery.clauses))
				return false;
			if (!field.Equals(spanNearQuery.field))
				return false;
			
			return GetBoost() == spanNearQuery.GetBoost();
		}
		
		public override int GetHashCode()
		{
			int result;
			result = clauses.GetHashCode();
			result += slop * 29;
			result += (inOrder?1:0);
			result ^= field.GetHashCode();
			return result;
		}
	}
}