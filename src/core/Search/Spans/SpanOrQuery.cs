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
	using Lucene.Net.Util;
	using ToStringUtils = Lucene.Net.Util.ToStringUtils;

	/// <summary>
	/// Matches the union of its clauses. </summary>
	public class SpanOrQuery : SpanQuery, ICloneable
	{
	  private IList<SpanQuery> Clauses_Renamed;
	  private string Field_Renamed;

	  /// <summary>
	  /// Construct a SpanOrQuery merging the provided clauses. </summary>
	  public SpanOrQuery(params SpanQuery[] clauses)
	  {

		// copy clauses array into an ArrayList
		this.Clauses_Renamed = new List<>(clauses.Length);
		for (int i = 0; i < clauses.Length; i++)
		{
		  AddClause(clauses[i]);
		}
	  }

	  /// <summary>
	  /// Adds a clause to this query </summary>
	  public void AddClause(SpanQuery clause)
	  {
		if (Field_Renamed == null)
		{
		  Field_Renamed = clause.Field;
		}
		else if (clause.Field != null && !clause.Field.Equals(Field_Renamed))
		{
		  throw new System.ArgumentException("Clauses must have same field.");
		}
		this.Clauses_Renamed.Add(clause);
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

	  public override SpanOrQuery Clone()
	  {
		int sz = Clauses_Renamed.Count;
		SpanQuery[] newClauses = new SpanQuery[sz];

		for (int i = 0; i < sz; i++)
		{
		  newClauses[i] = (SpanQuery) Clauses_Renamed[i].Clone();
		}
		SpanOrQuery soq = new SpanOrQuery(newClauses);
		soq.Boost = Boost;
		return soq;
	  }

	  public override Query Rewrite(IndexReader reader)
	  {
		SpanOrQuery clone = null;
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

	  public override string ToString(string field)
	  {
		StringBuilder buffer = new StringBuilder();
		buffer.Append("spanOr([");
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

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SpanOrQuery that = (SpanOrQuery) o;
		SpanOrQuery that = (SpanOrQuery) o;

		if (!Clauses_Renamed.Equals(that.Clauses_Renamed))
		{
			return false;
		}

		return Boost == that.Boost;
	  }

	  public override int HashCode()
	  {
		int h = Clauses_Renamed.HashCode();
		h ^= (h << 10) | ((int)((uint)h >> 23));
		h ^= float.floatToRawIntBits(Boost);
		return h;
	  }


	  private class SpanQueue : PriorityQueue<Spans>
	  {
		  private readonly SpanOrQuery OuterInstance;

		public SpanQueue(SpanOrQuery outerInstance, int size) : base(size)
		{
			this.OuterInstance = outerInstance;
		}

		protected internal override bool LessThan(Spans spans1, Spans spans2)
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
		if (Clauses_Renamed.Count == 1) // optimize 1-clause case
		{
		  return (Clauses_Renamed[0]).GetSpans(context, acceptDocs, termContexts);
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
			queue = new SpanQueue(OuterInstance, OuterInstance.Clauses_Renamed.Count);
			IEnumerator<SpanQuery> i = OuterInstance.Clauses_Renamed.GetEnumerator();
			while (i.MoveNext())
			{
			  Spans spans = i.Current.getSpans(Context, AcceptDocs, TermContexts);
			  cost += spans.Cost();
			  if (((target == -1) && spans.Next()) || ((target != -1) && spans.SkipTo(target)))
			  {
				queue.add(spans);
			  }
			}
			return queue.size() != 0;
		  }

		  public override bool Next()
		  {
			if (queue == null)
			{
			  return initSpanQueue(-1);
			}

			if (queue.size() == 0) // all done
			{
			  return false;
			}

			if (top().next()) // move to next
			{
			  queue.updateTop();
			  return true;
			}

			queue.pop(); // exhausted a clause
			return queue.size() != 0;
		  }

		  private Spans Top()
		  {
			  return queue.top();
		  }

		  public override bool SkipTo(int target)
		  {
			if (queue == null)
			{
			  return initSpanQueue(target);
			}

			bool skipCalled = false;
			while (queue.size() != 0 && top().doc() < target)
			{
			  if (top().skipTo(target))
			  {
				queue.updateTop();
			  }
			  else
			  {
				queue.pop();
			  }
			  skipCalled = true;
			}

			if (skipCalled)
			{
			  return queue.size() != 0;
			}
			return next();
		  }

		  public override int Doc()
		  {
			  return top().doc();
		  }
		  public override int Start()
		  {
			  return top().start();
		  }
		  public override int End()
		  {
			  return top().end();
		  }
		  public override ICollection<sbyte[]> Payload
		  {
			  get
			  {
			  List<sbyte[]> result = null;
			  Spans theTop = top();
			  if (theTop != null && theTop.PayloadAvailable)
			  {
				result = new List<>(theTop.Payload);
			  }
			  return result;
			  }
		  }

		public override bool PayloadAvailable
		{
			get
			{
			  Spans top = top();
			  return top != null && top.PayloadAvailable;
			}
		}

		public override string ToString()
		{
			return "spans(" + OuterInstance + ")@" + ((queue == null)?"START" :(queue.size() > 0?(doc() + ":" + start() + "-" + end()):"END"));
		}

		public override long Cost()
		{
		  return cost;
		}

	  }

	}

}