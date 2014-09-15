using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace org.apache.lucene.queries.function
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

	using org.apache.lucene.search;
	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using Term = org.apache.lucene.index.Term;
	using Bits = org.apache.lucene.util.Bits;
	using ToStringUtils = org.apache.lucene.util.ToStringUtils;


	/// <summary>
	/// Query that is boosted by a ValueSource
	/// </summary>
	// TODO: BoostedQuery and BoostingQuery in the same module? 
	// something has to give
	public class BoostedQuery : Query
	{
	  private Query q;
	  private readonly ValueSource boostVal; // optional, can be null

	  public BoostedQuery(Query subQuery, ValueSource boostVal)
	  {
		this.q = subQuery;
		this.boostVal = boostVal;
	  }

	  public virtual Query Query
	  {
		  get
		  {
			  return q;
		  }
	  }
	  public virtual ValueSource ValueSource
	  {
		  get
		  {
			  return boostVal;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public Query rewrite(org.apache.lucene.index.IndexReader reader) throws java.io.IOException
	  public override Query rewrite(IndexReader reader)
	  {
		Query newQ = q.rewrite(reader);
		if (newQ == q)
		{
			return this;
		}
		BoostedQuery bq = (BoostedQuery)this.MemberwiseClone();
		bq.q = newQ;
		return bq;
	  }

	  public override void extractTerms(HashSet<Term> terms)
	  {
		q.extractTerms(terms);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public Weight createWeight(IndexSearcher searcher) throws java.io.IOException
	  public override Weight createWeight(IndexSearcher searcher)
	  {
		return new BoostedQuery.BoostedWeight(this, searcher);
	  }

	  private class BoostedWeight : Weight
	  {
		  private readonly BoostedQuery outerInstance;

		internal readonly IndexSearcher searcher;
		internal Weight qWeight;
		internal IDictionary fcontext;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public BoostedWeight(IndexSearcher searcher) throws java.io.IOException
		public BoostedWeight(BoostedQuery outerInstance, IndexSearcher searcher)
		{
			this.outerInstance = outerInstance;
		  this.searcher = searcher;
		  this.qWeight = outerInstance.q.createWeight(searcher);
		  this.fcontext = ValueSource.newContext(searcher);
		  outerInstance.boostVal.createWeight(fcontext,searcher);
		}

		public override Query Query
		{
			get
			{
			  return outerInstance;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public float getValueForNormalization() throws java.io.IOException
		public override float ValueForNormalization
		{
			get
			{
			  float sum = qWeight.ValueForNormalization;
			  sum *= Boost * Boost;
			  return sum;
			}
		}

		public override void normalize(float norm, float topLevelBoost)
		{
		  topLevelBoost *= Boost;
		  qWeight.normalize(norm, topLevelBoost);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public Scorer scorer(org.apache.lucene.index.AtomicReaderContext context, org.apache.lucene.util.Bits acceptDocs) throws java.io.IOException
		public override Scorer scorer(AtomicReaderContext context, Bits acceptDocs)
		{
		  Scorer subQueryScorer = qWeight.scorer(context, acceptDocs);
		  if (subQueryScorer == null)
		  {
			return null;
		  }
		  return new BoostedQuery.CustomScorer(outerInstance, context, this, Boost, subQueryScorer, outerInstance.boostVal);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public Explanation explain(org.apache.lucene.index.AtomicReaderContext readerContext, int doc) throws java.io.IOException
		public override Explanation explain(AtomicReaderContext readerContext, int doc)
		{
		  Explanation subQueryExpl = qWeight.explain(readerContext,doc);
		  if (!subQueryExpl.Match)
		  {
			return subQueryExpl;
		  }
		  FunctionValues vals = outerInstance.boostVal.getValues(fcontext, readerContext);
		  float sc = subQueryExpl.Value * vals.floatVal(doc);
		  Explanation res = new ComplexExplanation(true, sc, outerInstance.ToString() + ", product of:");
		  res.addDetail(subQueryExpl);
		  res.addDetail(vals.explain(doc));
		  return res;
		}
	  }


	  private class CustomScorer : Scorer
	  {
		  private readonly BoostedQuery outerInstance;

		internal readonly BoostedQuery.BoostedWeight weight;
		internal readonly float qWeight;
		internal readonly Scorer scorer;
		internal readonly FunctionValues vals;
		internal readonly AtomicReaderContext readerContext;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private CustomScorer(org.apache.lucene.index.AtomicReaderContext readerContext, BoostedQuery.BoostedWeight w, float qWeight, Scorer scorer, ValueSource vs) throws java.io.IOException
		internal CustomScorer(BoostedQuery outerInstance, AtomicReaderContext readerContext, BoostedQuery.BoostedWeight w, float qWeight, Scorer scorer, ValueSource vs) : base(w)
		{
			this.outerInstance = outerInstance;
		  this.weight = w;
		  this.qWeight = qWeight;
		  this.scorer = scorer;
		  this.readerContext = readerContext;
		  this.vals = vs.getValues(weight.fcontext, readerContext);
		}

		public override int docID()
		{
		  return scorer.docID();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
		public override int advance(int target)
		{
		  return scorer.advance(target);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int nextDoc() throws java.io.IOException
		public override int nextDoc()
		{
		  return scorer.nextDoc();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public float score() throws java.io.IOException
		public override float score()
		{
		  float score = qWeight * scorer.score() * vals.floatVal(scorer.docID());

		  // Current Lucene priority queues can't handle NaN and -Infinity, so
		  // map to -Float.MAX_VALUE. This conditional handles both -infinity
		  // and NaN since comparisons with NaN are always false.
		  return score > float.NegativeInfinity ? score : -float.MaxValue;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int freq() throws java.io.IOException
		public override int freq()
		{
		  return scorer.freq();
		}

		public override ICollection<ChildScorer> Children
		{
			get
			{
			  return Collections.singleton(new ChildScorer(scorer, "CUSTOM"));
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public Explanation explain(int doc) throws java.io.IOException
		public virtual Explanation explain(int doc)
		{
		  Explanation subQueryExpl = weight.qWeight.explain(readerContext,doc);
		  if (!subQueryExpl.Match)
		  {
			return subQueryExpl;
		  }
		  float sc = subQueryExpl.Value * vals.floatVal(doc);
		  Explanation res = new ComplexExplanation(true, sc, outerInstance.ToString() + ", product of:");
		  res.addDetail(subQueryExpl);
		  res.addDetail(vals.explain(doc));
		  return res;
		}

		public override long cost()
		{
		  return scorer.cost();
		}
	  }


	  public override string ToString(string field)
	  {
		StringBuilder sb = new StringBuilder();
		sb.Append("boost(").Append(q.ToString(field)).Append(',').Append(boostVal).Append(')');
		sb.Append(ToStringUtils.boost(Boost));
		return sb.ToString();
	  }

	  public override bool Equals(object o)
	  {
	  if (!base.Equals(o))
	  {
		  return false;
	  }
		BoostedQuery other = (BoostedQuery)o;
		return this.q.Equals(other.q) && this.boostVal.Equals(other.boostVal);
	  }

	  public override int GetHashCode()
	  {
		int h = q.GetHashCode();
		h ^= (h << 17) | ((int)((uint)h >> 16));
		h += boostVal.GetHashCode();
		h ^= (h << 8) | ((int)((uint)h >> 25));
		h += float.floatToIntBits(Boost);
		return h;
	  }

	}

}