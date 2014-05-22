using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Search
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


	using IndexReader = Lucene.Net.Index.IndexReader;
	using Term = Lucene.Net.Index.Term;
	using TermContext = Lucene.Net.Index.TermContext;
	using TermState = Lucene.Net.Index.TermState;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using ArrayUtil = Lucene.Net.Util.ArrayUtil;
	using BytesRef = Lucene.Net.Util.BytesRef;

	/// <summary>
	/// Base rewrite method for collecting only the top terms
	/// via a priority queue.
	/// @lucene.internal Only public to be accessible by spans package.
	/// </summary>
	public abstract class TopTermsRewrite<Q> : TermCollectingRewrite<Q> where Q : Query
	{

	  private readonly int Size_Renamed;

	  /// <summary>
	  /// Create a TopTermsBooleanQueryRewrite for 
	  /// at most <code>size</code> terms.
	  /// <p>
	  /// NOTE: if <seealso cref="BooleanQuery#getMaxClauseCount"/> is smaller than 
	  /// <code>size</code>, then it will be used instead. 
	  /// </summary>
	  public TopTermsRewrite(int size)
	  {
		this.Size_Renamed = size;
	  }

	  /// <summary>
	  /// return the maximum priority queue size </summary>
	  public virtual int Size
	  {
		  get
		  {
			return Size_Renamed;
		  }
	  }

	  /// <summary>
	  /// return the maximum size of the priority queue (for boolean rewrites this is BooleanQuery#getMaxClauseCount). </summary>
	  protected internal abstract int MaxSize {get;}

	  public override Q Rewrite(IndexReader reader, MultiTermQuery query)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxSize = Math.min(size, getMaxSize());
		int maxSize = Math.Min(Size_Renamed, MaxSize);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.PriorityQueue<ScoreTerm> stQueue = new java.util.PriorityQueue<>();
		PriorityQueue<ScoreTerm> stQueue = new PriorityQueue<ScoreTerm>();
		collectTerms(reader, query, new TermCollectorAnonymousInnerClassHelper(this, maxSize, stQueue));

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Q q = getTopLevelQuery();
		Q q = TopLevelQuery;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ScoreTerm[] scoreTerms = stQueue.toArray(new ScoreTerm[stQueue.size()]);
		ScoreTerm[] scoreTerms = stQueue.toArray(new ScoreTerm[stQueue.size()]);
		ArrayUtil.TimSort(scoreTerms, scoreTermSortByTermComp);

		foreach (ScoreTerm st in scoreTerms)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Term term = new Lucene.Net.Index.Term(query.field, st.bytes);
		  Term term = new Term(query.Field_Renamed, st.bytes);
		  Debug.Assert(reader.DocFreq(term) == st.termState.docFreq(), "reader DF is " + reader.DocFreq(term) + " vs " + st.termState.docFreq() + " term=" + term);
		  addClause(q, term, st.termState.docFreq(), query.Boost * st.boost, st.termState); // add to query
		}
		return q;
	  }

	  private class TermCollectorAnonymousInnerClassHelper : TermCollector
	  {
		  private readonly TopTermsRewrite OuterInstance;

		  private int MaxSize;
		  private PriorityQueue<ScoreTerm> StQueue;

		  public TermCollectorAnonymousInnerClassHelper(TopTermsRewrite outerInstance, int maxSize, PriorityQueue<ScoreTerm> stQueue)
		  {
			  this.outerInstance = outerInstance;
			  this.MaxSize = maxSize;
			  this.StQueue = stQueue;
			  maxBoostAtt = attributes.addAttribute(typeof(MaxNonCompetitiveBoostAttribute));
			  visitedTerms = new Dictionary<>();
		  }

		  private readonly MaxNonCompetitiveBoostAttribute maxBoostAtt;

		  private readonly IDictionary<BytesRef, ScoreTerm> visitedTerms;

		  private TermsEnum termsEnum;
		  private IComparer<BytesRef> termComp;
		  private BoostAttribute boostAtt;
		  private ScoreTerm st;

		  public override TermsEnum NextEnum
		  {
			  set
			  {
				this.termsEnum = value;
				this.termComp = value.Comparator;
    
				Debug.Assert(compareToLastTerm(null));
    
				// lazy init the initial ScoreTerm because comparator is not known on ctor:
				if (st == null)
				{
				  st = new ScoreTerm(this.termComp, new TermContext(topReaderContext));
				}
				boostAtt = value.Attributes().addAttribute(typeof(BoostAttribute));
			  }
		  }

		  // for assert:
		  private BytesRef lastTerm;
		  private bool CompareToLastTerm(BytesRef t)
		  {
			if (lastTerm == null && t != null)
			{
			  lastTerm = BytesRef.DeepCopyOf(t);
			}
			else if (t == null)
			{
			  lastTerm = null;
			}
			else
			{
			  Debug.Assert(termsEnum.Comparator.compare(lastTerm, t) < 0, "lastTerm=" + lastTerm + " t=" + t);
			  lastTerm.copyBytes(t);
			}
			return true;
		  }

		  public override bool Collect(BytesRef bytes)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float boost = boostAtt.getBoost();
			float boost = boostAtt.Boost;

			// make sure within a single seg we always collect
			// terms in order
			Debug.Assert(compareToLastTerm(bytes));

			//System.out.println("TTR.collect term=" + bytes.utf8ToString() + " boost=" + boost + " ord=" + readerContext.ord);
			// ignore uncompetitive hits
			if (StQueue.size() == MaxSize)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ScoreTerm t = stQueue.peek();
			  ScoreTerm t = StQueue.peek();
			  if (boost < t.Boost)
			  {
				return true;
			  }
			  if (boost == t.Boost && termComp.compare(bytes, t.Bytes) > 0)
			  {
				return true;
			  }
			}
			ScoreTerm t = visitedTerms.get(bytes);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.TermState state = termsEnum.termState();
			TermState state = termsEnum.termState();
			Debug.Assert(state != null);
			if (t != null)
			{
			  // if the term is already in the PQ, only update docFreq of term in PQ
			  Debug.Assert(t.Boost == boost, "boost should be equal in all segment TermsEnums");
			  t.TermState.register(state, readerContext.ord, termsEnum.docFreq(), termsEnum.totalTermFreq());
			}
			else
			{
			  // add new entry in PQ, we must clone the term, else it may get overwritten!
			  st.bytes.copyBytes(bytes);
			  st.boost = boost;
			  visitedTerms.put(st.bytes, st);
			  Debug.Assert(st.termState.docFreq() == 0);
			  st.termState.register(state, readerContext.ord, termsEnum.docFreq(), termsEnum.totalTermFreq());
			  StQueue.offer(st);
			  // possibly drop entries from queue
			  if (StQueue.size() > MaxSize)
			  {
				st = StQueue.poll();
				visitedTerms.remove(st.bytes);
				st.termState.clear(); // reset the termstate!
			  }
			  else
			  {
				st = new ScoreTerm(termComp, new TermContext(topReaderContext));
			  }
			  Debug.Assert(StQueue.size() <= MaxSize, "the PQ size must be limited to maxSize");
			  // set maxBoostAtt with values to help FuzzyTermsEnum to optimize
			  if (StQueue.size() == MaxSize)
			  {
				t = StQueue.peek();
				maxBoostAtt.MaxNonCompetitiveBoost = t.Boost;
				maxBoostAtt.CompetitiveTerm = t.Bytes;
			  }
			}

			return true;
		  }
	  }

	  public override int HashCode()
	  {
		return 31 * Size_Renamed;
	  }

	  public override bool Equals(object obj)
	  {
		if (this == obj)
		{
			return true;
		}
		if (obj == null)
		{
			return false;
		}
		if (this.GetType() != obj.GetType())
		{
			return false;
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TopTermsRewrite<?> other = (TopTermsRewrite<?>) obj;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
		TopTermsRewrite<?> other = (TopTermsRewrite<?>) obj;
		if (Size_Renamed != other.Size_Renamed)
		{
			return false;
		}
		return true;
	  }

	  private static readonly IComparer<ScoreTerm> scoreTermSortByTermComp = new ComparatorAnonymousInnerClassHelper();

	  private class ComparatorAnonymousInnerClassHelper : IComparer<ScoreTerm>
	  {
		  public ComparatorAnonymousInnerClassHelper()
		  {
		  }

		  public virtual int Compare(ScoreTerm st1, ScoreTerm st2)
		  {
			Debug.Assert(st1.TermComp == st2.TermComp, "term comparator should not change between segments");
			return st1.TermComp.Compare(st1.Bytes, st2.Bytes);
		  }
	  }

	  internal sealed class ScoreTerm : IComparable<ScoreTerm>
	  {
		public readonly IComparer<BytesRef> TermComp;
		public readonly BytesRef Bytes = new BytesRef();
		public float Boost;
		public readonly TermContext TermState;
		public ScoreTerm(IComparer<BytesRef> termComp, TermContext termState)
		{
		  this.TermComp = termComp;
		  this.TermState = termState;
		}

		public int CompareTo(ScoreTerm other)
		{
		  if (this.Boost == other.Boost)
		  {
			return TermComp.Compare(other.Bytes, this.Bytes);
		  }
		  else
		  {
			return this.Boost.CompareTo(other.Boost);
		  }
		}
	  }
	}

}