using System;
using System.Collections.Generic;
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
	using IndexReader = Lucene.Net.Index.IndexReader;
	using Term = Lucene.Net.Index.Term;
	using TermContext = Lucene.Net.Index.TermContext;
	using Bits = Lucene.Net.Util.Bits;
	using ToStringUtils = Lucene.Net.Util.ToStringUtils;

	/// <summary>
	/// Matches spans which are near one another.  One can specify <i>slop</i>, the
	/// maximum number of intervening unmatched positions, as well as whether
	/// matches are required to be in-order. 
	/// </summary>
	public class SpanNearQuery : SpanQuery, ICloneable
	{
	  protected internal IList<SpanQuery> Clauses_Renamed;
	  protected internal int Slop_Renamed;
	  protected internal bool InOrder_Renamed;

	  protected internal string Field_Renamed;
	  private bool CollectPayloads;

	  /// <summary>
	  /// Construct a SpanNearQuery.  Matches spans matching a span from each
	  /// clause, with up to <code>slop</code> total unmatched positions between
	  /// them.  * When <code>inOrder</code> is true, the spans from each clause
	  /// must be * ordered as in <code>clauses</code>. </summary>
	  /// <param name="clauses"> the clauses to find near each other </param>
	  /// <param name="slop"> The slop value </param>
	  /// <param name="inOrder"> true if order is important
	  ///  </param>
	  public SpanNearQuery(SpanQuery[] clauses, int slop, bool inOrder) : this(clauses, slop, inOrder, true)
	  {
	  }

	  public SpanNearQuery(SpanQuery[] clauses, int slop, bool inOrder, bool collectPayloads)
	  {

		// copy clauses array into an ArrayList
		this.Clauses_Renamed = new List<>(clauses.Length);
		for (int i = 0; i < clauses.Length; i++)
		{
		  SpanQuery clause = clauses[i];
		  if (Field_Renamed == null) // check field
		  {
			Field_Renamed = clause.Field;
		  }
		  else if (clause.Field != null && !clause.Field.Equals(Field_Renamed))
		  {
			throw new System.ArgumentException("Clauses must have same field.");
		  }
		  this.Clauses_Renamed.Add(clause);
		}
		this.CollectPayloads = collectPayloads;
		this.Slop_Renamed = slop;
		this.InOrder_Renamed = inOrder;
	  }

	  /// <summary>
	  /// Return the clauses whose spans are matched. </summary>
	  public virtual SpanQuery[] Clauses
	  {
		  get
		  {
			return Clauses_Renamed.ToArray();
		  }
	  }

	  /// <summary>
	  /// Return the maximum number of intervening unmatched positions permitted. </summary>
	  public virtual int Slop
	  {
		  get
		  {
			  return Slop_Renamed;
		  }
	  }

	  /// <summary>
	  /// Return true if matches are required to be in-order. </summary>
	  public virtual bool InOrder
	  {
		  get
		  {
			  return InOrder_Renamed;
		  }
	  }

	  public override string Field
	  {
		  get
		  {
			  return Field_Renamed;
		  }
	  }
	  public override void ExtractTerms(Set<Term> terms)
	  {
		foreach (SpanQuery clause in Clauses_Renamed)
		{
		  clause.extractTerms(terms);
		}
	  }


	  public override string ToString(string field)
	  {
		StringBuilder buffer = new StringBuilder();
		buffer.Append("spanNear([");
		IEnumerator<SpanQuery> i = Clauses_Renamed.GetEnumerator();
		while (i.MoveNext())
		{
		  SpanQuery clause = i.Current;
		  buffer.Append(clause.ToString(field));
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  if (i.hasNext())
		  {
			buffer.Append(", ");
		  }
		}
		buffer.Append("], ");
		buffer.Append(Slop_Renamed);
		buffer.Append(", ");
		buffer.Append(InOrder_Renamed);
		buffer.Append(")");
		buffer.Append(ToStringUtils.Boost(Boost));
		return buffer.ToString();
	  }

	  public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
	  {
		if (Clauses_Renamed.Count == 0) // optimize 0-clause case
		{
		  return (new SpanOrQuery(Clauses)).GetSpans(context, acceptDocs, termContexts);
		}

		if (Clauses_Renamed.Count == 1) // optimize 1-clause case
		{
		  return Clauses_Renamed[0].GetSpans(context, acceptDocs, termContexts);
		}

		return InOrder_Renamed ? (Spans) new NearSpansOrdered(this, context, acceptDocs, termContexts, CollectPayloads) : (Spans) new NearSpansUnordered(this, context, acceptDocs, termContexts);
	  }

	  public override Query Rewrite(IndexReader reader)
	  {
		SpanNearQuery clone = null;
		for (int i = 0 ; i < Clauses_Renamed.Count; i++)
		{
		  SpanQuery c = Clauses_Renamed[i];
		  SpanQuery query = (SpanQuery) c.Rewrite(reader);
		  if (query != c) // clause rewrote: must clone
		  {
			if (clone == null)
			{
			  clone = this.Clone();
			}
			clone.Clauses_Renamed[i] = query;
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

	  public override SpanNearQuery Clone()
	  {
		int sz = Clauses_Renamed.Count;
		SpanQuery[] newClauses = new SpanQuery[sz];

		for (int i = 0; i < sz; i++)
		{
		  newClauses[i] = (SpanQuery) Clauses_Renamed[i].Clone();
		}
		SpanNearQuery spanNearQuery = new SpanNearQuery(newClauses, Slop_Renamed, InOrder_Renamed);
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

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SpanNearQuery spanNearQuery = (SpanNearQuery) o;
		SpanNearQuery spanNearQuery = (SpanNearQuery) o;

		if (InOrder_Renamed != spanNearQuery.InOrder_Renamed)
		{
			return false;
		}
		if (Slop_Renamed != spanNearQuery.Slop_Renamed)
		{
			return false;
		}
		if (!Clauses_Renamed.Equals(spanNearQuery.Clauses_Renamed))
		{
			return false;
		}

		return Boost == spanNearQuery.Boost;
	  }

	  public override int HashCode()
	  {
		int result;
		result = Clauses_Renamed.HashCode();
		// Mix bits before folding in things like boost, since it could cancel the
		// last element of clauses.  this particular mix also serves to
		// differentiate SpanNearQuery hashcodes from others.
		result ^= (result << 14) | ((int)((uint)result >> 19)); // reversible
		result += float.floatToRawIntBits(Boost);
		result += Slop_Renamed;
		result ^= (InOrder_Renamed ? 0x99AFD3BD : 0);
		return result;
	  }
	}

}