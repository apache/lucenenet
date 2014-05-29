using System;
using System.Diagnostics;

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
	using RewriteMethod = Lucene.Net.Search.MultiTermQuery.RewriteMethod;

	using ArrayUtil = Lucene.Net.Util.ArrayUtil;
	using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using BytesRefHash = Lucene.Net.Util.BytesRefHash;
	using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
	using DirectBytesStartArray = Lucene.Net.Util.BytesRefHash.DirectBytesStartArray;

	/// <summary>
	/// Base rewrite method that translates each term into a query, and keeps
	/// the scores as computed by the query.
	/// <p>
	/// @lucene.internal Only public to be accessible by spans package. 
	/// </summary>
	public abstract class ScoringRewrite<Q> : TermCollectingRewrite<Q> where Q : Query
	{

	  /// <summary>
	  /// A rewrite method that first translates each term into
	  ///  <seealso cref="BooleanClause.Occur#SHOULD"/> clause in a
	  ///  BooleanQuery, and keeps the scores as computed by the
	  ///  query.  Note that typically such scores are
	  ///  meaningless to the user, and require non-trivial CPU
	  ///  to compute, so it's almost always better to use {@link
	  ///  MultiTermQuery#CONSTANT_SCORE_AUTO_REWRITE_DEFAULT} instead.
	  /// 
	  ///  <p><b>NOTE</b>: this rewrite method will hit {@link
	  ///  BooleanQuery.TooManyClauses} if the number of terms
	  ///  exceeds <seealso cref="BooleanQuery#getMaxClauseCount"/>.
	  /// </summary>
	  ///  <seealso cref= MultiTermQuery#setRewriteMethod  </seealso>
	  public static readonly ScoringRewrite<BooleanQuery> SCORING_BOOLEAN_QUERY_REWRITE = new ScoringRewriteAnonymousInnerClassHelper();

	  private class ScoringRewriteAnonymousInnerClassHelper : ScoringRewrite<BooleanQuery>
	  {
		  public ScoringRewriteAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override BooleanQuery TopLevelQuery
		  {
			  get
			  {
				return new BooleanQuery(true);
			  }
		  }

		  protected internal override void AddClause(BooleanQuery topLevel, Term term, int docCount, float boost, TermContext states)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TermQuery tq = new TermQuery(term, states);
			TermQuery tq = new TermQuery(term, states);
			tq.Boost = boost;
			topLevel.Add(tq, BooleanClause.Occur_e.SHOULD);
		  }

		  protected internal override void CheckMaxClauseCount(int count)
		  {
			if (count > BooleanQuery.MaxClauseCount)
			{
			  throw new BooleanQuery.TooManyClauses();
			}
		  }
	  }

	  /// <summary>
	  /// Like <seealso cref="#SCORING_BOOLEAN_QUERY_REWRITE"/> except
	  ///  scores are not computed.  Instead, each matching
	  ///  document receives a constant score equal to the
	  ///  query's boost.
	  /// 
	  ///  <p><b>NOTE</b>: this rewrite method will hit {@link
	  ///  BooleanQuery.TooManyClauses} if the number of terms
	  ///  exceeds <seealso cref="BooleanQuery#getMaxClauseCount"/>.
	  /// </summary>
	  ///  <seealso cref= MultiTermQuery#setRewriteMethod  </seealso>
	  public static readonly RewriteMethod CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE = new RewriteMethodAnonymousInnerClassHelper();

	  private class RewriteMethodAnonymousInnerClassHelper : RewriteMethod
	  {
		  public RewriteMethodAnonymousInnerClassHelper()
		  {
		  }

		  public override Query Rewrite(IndexReader reader, MultiTermQuery query)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final BooleanQuery bq = SCORING_BOOLEAN_QUERY_REWRITE.rewrite(reader, query);
			BooleanQuery bq = SCORING_BOOLEAN_QUERY_REWRITE.rewrite(reader, query);
			// strip the scores off
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Query result = new ConstantScoreQuery(bq);
			Query result = new ConstantScoreQuery(bq);
			result.Boost = query.Boost;
			return result;
		  }
	  }

	  /// <summary>
	  /// this method is called after every new term to check if the number of max clauses
	  /// (e.g. in BooleanQuery) is not exceeded. Throws the corresponding <seealso cref="RuntimeException"/>. 
	  /// </summary>
	  protected internal abstract void CheckMaxClauseCount(int count);

	  public override Q Rewrite(IndexReader reader, MultiTermQuery query)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Q result = getTopLevelQuery();
		Q result = TopLevelQuery;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ParallelArraysTermCollector col = new ParallelArraysTermCollector();
		ParallelArraysTermCollector col = new ParallelArraysTermCollector(this);
		collectTerms(reader, query, col);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int size = col.terms.size();
		int size = col.Terms.size();
		if (size > 0)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int sort[] = col.terms.sort(col.termsEnum.getComparator());
		  int[] sort = col.Terms.sort(col.TermsEnum.Comparator);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float[] boost = col.array.boost;
		  float[] boost = col.Array.boost;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.TermContext[] termStates = col.array.termState;
		  TermContext[] termStates = col.Array.termState;
		  for (int i = 0; i < size; i++)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int pos = sort[i];
			int pos = sort[i];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Term term = new Lucene.Net.Index.Term(query.getField(), col.terms.get(pos, new Lucene.Net.Util.BytesRef()));
			Term term = new Term(query.Field, col.Terms.get(pos, new BytesRef()));
			Debug.Assert(reader.DocFreq(term) == termStates[pos].DocFreq());
			addClause(result, term, termStates[pos].DocFreq(), query.Boost * boost[pos], termStates[pos]);
		  }
		}
		return result;
	  }

	  internal sealed class ParallelArraysTermCollector : TermCollector
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal void InitializeInstanceFields()
		  {
			  Terms = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectAllocator()), 16, Array);
		  }

		  private readonly ScoringRewrite OuterInstance;

		  public ParallelArraysTermCollector(ScoringRewrite outerInstance)
		  {
			  this.OuterInstance = outerInstance;

			  if (!InstanceFieldsInitialized)
			  {
				  InitializeInstanceFields();
				  InstanceFieldsInitialized = true;
			  }
		  }

		internal readonly TermFreqBoostByteStart Array = new TermFreqBoostByteStart(16);
		internal BytesRefHash Terms;
		internal TermsEnum TermsEnum;

		internal BoostAttribute BoostAtt;

		public override TermsEnum NextEnum
		{
			set
			{
			  this.TermsEnum = value;
			  this.BoostAtt = value.Attributes().addAttribute(typeof(BoostAttribute));
			}
		}

		public override bool Collect(BytesRef bytes)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int e = terms.add(bytes);
		  int e = Terms.Add(bytes);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.TermState state = termsEnum.termState();
		  TermState state = TermsEnum.TermState();
		  Debug.Assert(state != null);
		  if (e < 0)
		  {
			// duplicate term: update docFreq
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int pos = (-e)-1;
			int pos = (-e) - 1;
			Array.TermState[pos].register(state, readerContext.ord, TermsEnum.DocFreq(), TermsEnum.TotalTermFreq());
			Debug.Assert(Array.Boost[pos] == BoostAtt.Boost, "boost should be equal in all segment TermsEnums");
		  }
		  else
		  {
			// new entry: we populate the entry initially
			Array.Boost[e] = BoostAtt.Boost;
			Array.TermState[e] = new TermContext(topReaderContext, state, readerContext.ord, TermsEnum.DocFreq(), TermsEnum.TotalTermFreq());
			OuterInstance.checkMaxClauseCount(Terms.Size());
		  }
		  return true;
		}
	  }

	  /// <summary>
	  /// Special implementation of BytesStartArray that keeps parallel arrays for boost and docFreq </summary>
	  internal sealed class TermFreqBoostByteStart : BytesRefHash.DirectBytesStartArray
	  {
		internal float[] Boost;
		internal TermContext[] TermState;

		public TermFreqBoostByteStart(int initSize) : base(initSize)
		{
		}

		public override int[] Init()
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] ord = base.init();
		  int[] ord = base.Init();
		  Boost = new float[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_FLOAT)];
		  TermState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
		  Debug.Assert(TermState.Length >= ord.Length && Boost.Length >= ord.Length);
		  return ord;
		}

		public override int[] Grow()
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] ord = base.grow();
		  int[] ord = base.Grow();
		  Boost = ArrayUtil.Grow(Boost, ord.Length);
		  if (TermState.Length < ord.Length)
		  {
			TermContext[] tmpTermState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
			Array.Copy(TermState, 0, tmpTermState, 0, TermState.Length);
			TermState = tmpTermState;
		  }
		  Debug.Assert(TermState.Length >= ord.Length && Boost.Length >= ord.Length);
		  return ord;
		}

		public override int[] Clear()
		{
		 Boost = null;
		 TermState = null;
		 return base.Clear();
		}

	  }
	}

}