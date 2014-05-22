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
	/// Removes matches which overlap with another SpanQuery or 
	/// within a x tokens before or y tokens after another SpanQuery. 
	/// </summary>
	public class SpanNotQuery : SpanQuery, ICloneable
	{
	  private SpanQuery Include_Renamed;
	  private SpanQuery Exclude_Renamed;
	  private readonly int Pre;
	  private readonly int Post;

	  /// <summary>
	  /// Construct a SpanNotQuery matching spans from <code>include</code> which
	  /// have no overlap with spans from <code>exclude</code>.
	  /// </summary>
	  public SpanNotQuery(SpanQuery include, SpanQuery exclude) : this(include, exclude, 0, 0)
	  {
	  }


	  /// <summary>
	  /// Construct a SpanNotQuery matching spans from <code>include</code> which
	  /// have no overlap with spans from <code>exclude</code> within 
	  /// <code>dist</code> tokens of <code>include</code>. 
	  /// </summary>
	  public SpanNotQuery(SpanQuery include, SpanQuery exclude, int dist) : this(include, exclude, dist, dist)
	  {
	  }

	  /// <summary>
	  /// Construct a SpanNotQuery matching spans from <code>include</code> which
	  /// have no overlap with spans from <code>exclude</code> within 
	  /// <code>pre</code> tokens before or <code>post</code> tokens of <code>include</code>. 
	  /// </summary>
	  public SpanNotQuery(SpanQuery include, SpanQuery exclude, int pre, int post)
	  {
		this.Include_Renamed = include;
		this.Exclude_Renamed = exclude;
		this.Pre = (pre >= 0) ? pre : 0;
		this.Post = (post >= 0) ? post : 0;

		if (include.Field != null && exclude.Field != null && !include.Field.Equals(exclude.Field))
		{
		  throw new System.ArgumentException("Clauses must have same field.");
		}
	  }

	  /// <summary>
	  /// Return the SpanQuery whose matches are filtered. </summary>
	  public virtual SpanQuery Include
	  {
		  get
		  {
			  return Include_Renamed;
		  }
	  }

	  /// <summary>
	  /// Return the SpanQuery whose matches must not overlap those returned. </summary>
	  public virtual SpanQuery Exclude
	  {
		  get
		  {
			  return Exclude_Renamed;
		  }
	  }

	  public override string Field
	  {
		  get
		  {
			  return Include_Renamed.Field;
		  }
	  }
	  public override void ExtractTerms(Set<Term> terms)
	  {
		  Include_Renamed.ExtractTerms(terms);
	  }
	  public override string ToString(string field)
	  {
		StringBuilder buffer = new StringBuilder();
		buffer.Append("spanNot(");
		buffer.Append(Include_Renamed.ToString(field));
		buffer.Append(", ");
		buffer.Append(Exclude_Renamed.ToString(field));
		buffer.Append(", ");
		buffer.Append(Convert.ToString(Pre));
		buffer.Append(", ");
		buffer.Append(Convert.ToString(Post));
		buffer.Append(")");
		buffer.Append(ToStringUtils.Boost(Boost));
		return buffer.ToString();
	  }

	  public override SpanNotQuery Clone()
	  {
		SpanNotQuery spanNotQuery = new SpanNotQuery((SpanQuery)Include_Renamed.Clone(), (SpanQuery) Exclude_Renamed.Clone(), Pre, Post);
		spanNotQuery.Boost = Boost;
		return spanNotQuery;
	  }

	  public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
	  {
		return new SpansAnonymousInnerClassHelper(this, context, acceptDocs, termContexts);
	  }

	  private class SpansAnonymousInnerClassHelper : Spans
	  {
		  private readonly SpanNotQuery OuterInstance;

		  private AtomicReaderContext Context;
		  private Bits AcceptDocs;
		  private IDictionary<Term, TermContext> TermContexts;

		  public SpansAnonymousInnerClassHelper(SpanNotQuery outerInstance, AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
		  {
			  this.OuterInstance = outerInstance;
			  this.Context = context;
			  this.AcceptDocs = acceptDocs;
			  this.TermContexts = termContexts;
			  includeSpans = outerInstance.Include_Renamed.GetSpans(context, acceptDocs, termContexts);
			  moreInclude = true;
			  excludeSpans = outerInstance.Exclude_Renamed.GetSpans(context, acceptDocs, termContexts);
			  moreExclude = excludeSpans.next();
		  }

		  private Spans includeSpans;
		  private bool moreInclude;

		  private Spans excludeSpans;
		  private bool moreExclude;

		  public override bool Next()
		  {
			if (moreInclude) // move to next include
			{
			  moreInclude = includeSpans.next();
			}

			while (moreInclude && moreExclude)
			{

			  if (includeSpans.doc() > excludeSpans.doc()) // skip exclude
			  {
				moreExclude = excludeSpans.skipTo(includeSpans.doc());
			  }

			  while (moreExclude && includeSpans.doc() == excludeSpans.doc() && excludeSpans.end() <= includeSpans.start() - OuterInstance.Pre) // while exclude is before
			  {
				moreExclude = excludeSpans.next(); // increment exclude
			  }

			  if (!moreExclude || includeSpans.doc() != excludeSpans.doc() || includeSpans.end() + OuterInstance.Post <= excludeSpans.start()) // if no intersection
			  {
				break; // we found a match
			  }

			  moreInclude = includeSpans.next(); // intersected: keep scanning
			}
			return moreInclude;
		  }

		  public override bool SkipTo(int target)
		  {
			if (moreInclude) // skip include
			{
			  moreInclude = includeSpans.skipTo(target);
			}

			if (!moreInclude)
			{
			  return false;
			}

			if (moreExclude && includeSpans.doc() > excludeSpans.doc()) // skip exclude
			{
			  moreExclude = excludeSpans.skipTo(includeSpans.doc());
			}

			while (moreExclude && includeSpans.doc() == excludeSpans.doc() && excludeSpans.end() <= includeSpans.start() - OuterInstance.Pre) // while exclude is before
			{
			  moreExclude = excludeSpans.next(); // increment exclude
			}

			if (!moreExclude || includeSpans.doc() != excludeSpans.doc() || includeSpans.end() + OuterInstance.Post <= excludeSpans.start()) // if no intersection
			{
			  return true; // we found a match
			}

			return next(); // scan to next match
		  }

		  public override int Doc()
		  {
			  return includeSpans.doc();
		  }
		  public override int Start()
		  {
			  return includeSpans.start();
		  }
		  public override int End()
		// TODO: Remove warning after API has been finalized
		  {
			  return includeSpans.end();
		  }
		  public override ICollection<sbyte[]> Payload
		  {
			  get
			  {
			  List<sbyte[]> result = null;
			  if (includeSpans.PayloadAvailable)
			  {
				result = new List<>(includeSpans.Payload);
			  }
			  return result;
			  }
		  }

		// TODO: Remove warning after API has been finalized
		public override bool PayloadAvailable
		{
			get
			{
			  return includeSpans.PayloadAvailable;
			}
		}

		public override long Cost()
		{
		  return includeSpans.cost();
		}

		public override string ToString()
		{
			return "spans(" + OuterInstance.ToString() + ")";
		}

	  }

	  public override Query Rewrite(IndexReader reader)
	  {
		SpanNotQuery clone = null;

		SpanQuery rewrittenInclude = (SpanQuery) Include_Renamed.Rewrite(reader);
		if (rewrittenInclude != Include_Renamed)
		{
		  clone = this.Clone();
		  clone.Include_Renamed = rewrittenInclude;
		}
		SpanQuery rewrittenExclude = (SpanQuery) Exclude_Renamed.Rewrite(reader);
		if (rewrittenExclude != Exclude_Renamed)
		{
		  if (clone == null)
		  {
			  clone = this.Clone();
		  }
		  clone.Exclude_Renamed = rewrittenExclude;
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

		/// <summary>
		/// Returns true iff <code>o</code> is equal to this. </summary>
	  public override bool Equals(object o)
	  {
		if (!base.Equals(o))
		{
		  return false;
		}

		SpanNotQuery other = (SpanNotQuery)o;
		return this.Include_Renamed.Equals(other.Include_Renamed) && this.Exclude_Renamed.Equals(other.Exclude_Renamed) && this.Pre == other.Pre && this.Post == other.Post;
	  }

	  public override int HashCode()
	  {
		int h = base.HashCode();
		h = int.rotateLeft(h, 1);
		h ^= Include_Renamed.HashCode();
		h = int.rotateLeft(h, 1);
		h ^= Exclude_Renamed.HashCode();
		h = int.rotateLeft(h, 1);
		h ^= Pre;
		h = int.rotateLeft(h, 1);
		h ^= Post;
		return h;
	  }

	}
}