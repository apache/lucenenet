using System.Collections.Generic;
using System.Text;

namespace org.apache.lucene.queries
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


	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using Term = org.apache.lucene.index.Term;
	using FunctionQuery = org.apache.lucene.queries.function.FunctionQuery;
	using ValueSource = org.apache.lucene.queries.function.ValueSource;
	using ComplexExplanation = org.apache.lucene.search.ComplexExplanation;
	using Explanation = org.apache.lucene.search.Explanation;
	using Query = org.apache.lucene.search.Query;
	using Weight = org.apache.lucene.search.Weight;
	using Scorer = org.apache.lucene.search.Scorer;
	using IndexSearcher = org.apache.lucene.search.IndexSearcher;
	using Bits = org.apache.lucene.util.Bits;
	using ToStringUtils = org.apache.lucene.util.ToStringUtils;

	/// <summary>
	/// Query that sets document score as a programmatic function of several (sub) scores:
	/// <ol>
	///    <li>the score of its subQuery (any query)</li>
	///    <li>(optional) the score of its <seealso cref="FunctionQuery"/> (or queries).</li>
	/// </ol>
	/// Subclasses can modify the computation by overriding <seealso cref="#getCustomScoreProvider"/>.
	/// 
	/// @lucene.experimental
	/// </summary>
	public class CustomScoreQuery : Query
	{

	  private Query subQuery;
	  private Query[] scoringQueries; // never null (empty array if there are no valSrcQueries).
	  private bool strict = false; // if true, valueSource part of query does not take part in weights normalization.

	  /// <summary>
	  /// Create a CustomScoreQuery over input subQuery. </summary>
	  /// <param name="subQuery"> the sub query whose scored is being customized. Must not be null.  </param>
	  public CustomScoreQuery(Query subQuery) : this(subQuery, new FunctionQuery[0])
	  {
	  }

	  /// <summary>
	  /// Create a CustomScoreQuery over input subQuery and a <seealso cref="org.apache.lucene.queries.function.FunctionQuery"/>. </summary>
	  /// <param name="subQuery"> the sub query whose score is being customized. Must not be null. </param>
	  /// <param name="scoringQuery"> a value source query whose scores are used in the custom score
	  /// computation.  This parameter is optional - it can be null. </param>
	  public CustomScoreQuery(Query subQuery, FunctionQuery scoringQuery) : this(subQuery, scoringQuery != null ? new FunctionQuery[] {scoringQuery} : new FunctionQuery[0]); / / don't want an array that contains a single null..
	  {
	  }

	  /// <summary>
	  /// Create a CustomScoreQuery over input subQuery and a <seealso cref="org.apache.lucene.queries.function.FunctionQuery"/>. </summary>
	  /// <param name="subQuery"> the sub query whose score is being customized. Must not be null. </param>
	  /// <param name="scoringQueries"> value source queries whose scores are used in the custom score
	  /// computation.  This parameter is optional - it can be null or even an empty array. </param>
	  public CustomScoreQuery(Query subQuery, params FunctionQuery[] scoringQueries)
	  {
		this.subQuery = subQuery;
		this.scoringQueries = scoringQueries != null? scoringQueries : new Query[0];
		if (subQuery == null)
		{
			throw new System.ArgumentException("<subquery> must not be null!");
		}
	  }

	  /*(non-Javadoc) @see org.apache.lucene.search.Query#rewrite(org.apache.lucene.index.IndexReader) */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.Query rewrite(org.apache.lucene.index.IndexReader reader) throws java.io.IOException
	  public override Query rewrite(IndexReader reader)
	  {
		CustomScoreQuery clone = null;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.Query sq = subQuery.rewrite(reader);
		Query sq = subQuery.rewrite(reader);
		if (sq != subQuery)
		{
		  clone = clone();
		  clone.subQuery = sq;
		}

		for (int i = 0; i < scoringQueries.Length; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.Query v = scoringQueries[i].rewrite(reader);
		  Query v = scoringQueries[i].rewrite(reader);
		  if (v != scoringQueries[i])
		  {
			if (clone == null)
			{
				clone = clone();
			}
			clone.scoringQueries[i] = v;
		  }
		}

		return (clone == null) ? this : clone;
	  }

	  /*(non-Javadoc) @see org.apache.lucene.search.Query#extractTerms(java.util.Set) */
	  public override void extractTerms(HashSet<Term> terms)
	  {
		subQuery.extractTerms(terms);
		foreach (Query scoringQuery in scoringQueries)
		{
		  scoringQuery.extractTerms(terms);
		}
	  }

	  /*(non-Javadoc) @see org.apache.lucene.search.Query#clone() */
	  public override CustomScoreQuery clone()
	  {
		CustomScoreQuery clone = (CustomScoreQuery)base.clone();
		clone.subQuery = subQuery.clone();
		clone.scoringQueries = new Query[scoringQueries.Length];
		for (int i = 0; i < scoringQueries.Length; i++)
		{
		  clone.scoringQueries[i] = scoringQueries[i].clone();
		}
		return clone;
	  }

	  /* (non-Javadoc) @see org.apache.lucene.search.Query#toString(java.lang.String) */
	  public override string ToString(string field)
	  {
		StringBuilder sb = (new StringBuilder(name())).Append("(");
		sb.Append(subQuery.ToString(field));
		foreach (Query scoringQuery in scoringQueries)
		{
		  sb.Append(", ").Append(scoringQuery.ToString(field));
		}
		sb.Append(")");
		sb.Append(strict?" STRICT" : "");
		return sb.ToString() + ToStringUtils.boost(Boost);
	  }

	  /// <summary>
	  /// Returns true if <code>o</code> is equal to this. </summary>
	  public override bool Equals(object o)
	  {
		if (this == o)
		{
		  return true;
		}
		if (!base.Equals(o))
		{
		  return false;
		}
		if (this.GetType() != o.GetType())
		{
		  return false;
		}
		CustomScoreQuery other = (CustomScoreQuery)o;
		if (this.Boost != other.Boost || !this.subQuery.Equals(other.subQuery) || this.strict != other.strict || this.scoringQueries.Length != other.scoringQueries.Length)
		{
		  return false;
		}
		return Arrays.Equals(scoringQueries, other.scoringQueries);
	  }

	  /// <summary>
	  /// Returns a hash code value for this object. </summary>
	  public override int GetHashCode()
	  {
		return (this.GetType().GetHashCode() + subQuery.GetHashCode() + Arrays.GetHashCode(scoringQueries)) ^ float.floatToIntBits(Boost) ^ (strict ? 1234 : 4321);
	  }

	  /// <summary>
	  /// Returns a <seealso cref="CustomScoreProvider"/> that calculates the custom scores
	  /// for the given <seealso cref="IndexReader"/>. The default implementation returns a default
	  /// implementation as specified in the docs of <seealso cref="CustomScoreProvider"/>.
	  /// @since 2.9.2
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected CustomScoreProvider getCustomScoreProvider(org.apache.lucene.index.AtomicReaderContext context) throws java.io.IOException
	  protected internal virtual CustomScoreProvider getCustomScoreProvider(AtomicReaderContext context)
	  {
		return new CustomScoreProvider(context);
	  }

	  //=========================== W E I G H T ============================

	  private class CustomWeight : Weight
	  {
		  private readonly CustomScoreQuery outerInstance;

		internal Weight subQueryWeight;
		internal Weight[] valSrcWeights;
		internal bool qStrict;
		internal float queryWeight;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public CustomWeight(org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
		public CustomWeight(CustomScoreQuery outerInstance, IndexSearcher searcher)
		{
			this.outerInstance = outerInstance;
		  this.subQueryWeight = outerInstance.subQuery.createWeight(searcher);
		  this.valSrcWeights = new Weight[outerInstance.scoringQueries.Length];
		  for (int i = 0; i < outerInstance.scoringQueries.Length; i++)
		  {
			this.valSrcWeights[i] = outerInstance.scoringQueries[i].createWeight(searcher);
		  }
		  this.qStrict = outerInstance.strict;
		}

		/*(non-Javadoc) @see org.apache.lucene.search.Weight#getQuery() */
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
			  float sum = subQueryWeight.ValueForNormalization;
			  foreach (Weight valSrcWeight in valSrcWeights)
			  {
				if (qStrict)
				{
				  valSrcWeight.ValueForNormalization; // do not include ValueSource part in the query normalization
				}
				else
				{
				  sum += valSrcWeight.ValueForNormalization;
				}
			  }
			  return sum;
			}
		}

		/*(non-Javadoc) @see org.apache.lucene.search.Weight#normalize(float) */
		public override void normalize(float norm, float topLevelBoost)
		{
		  // note we DONT incorporate our boost, nor pass down any topLevelBoost 
		  // (e.g. from outer BQ), as there is no guarantee that the CustomScoreProvider's 
		  // function obeys the distributive law... it might call sqrt() on the subQuery score
		  // or some other arbitrary function other than multiplication.
		  // so, instead boosts are applied directly in score()
		  subQueryWeight.normalize(norm, 1f);
		  foreach (Weight valSrcWeight in valSrcWeights)
		  {
			if (qStrict)
			{
			  valSrcWeight.normalize(1, 1); // do not normalize the ValueSource part
			}
			else
			{
			  valSrcWeight.normalize(norm, 1f);
			}
		  }
		  queryWeight = topLevelBoost * Boost;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.Scorer scorer(org.apache.lucene.index.AtomicReaderContext context, org.apache.lucene.util.Bits acceptDocs) throws java.io.IOException
		public override Scorer scorer(AtomicReaderContext context, Bits acceptDocs)
		{
		  Scorer subQueryScorer = subQueryWeight.scorer(context, acceptDocs);
		  if (subQueryScorer == null)
		  {
			return null;
		  }
		  Scorer[] valSrcScorers = new Scorer[valSrcWeights.Length];
		  for (int i = 0; i < valSrcScorers.Length; i++)
		  {
			 valSrcScorers[i] = valSrcWeights[i].scorer(context, acceptDocs);
		  }
		  return new CustomScorer(outerInstance, outerInstance.getCustomScoreProvider(context), this, queryWeight, subQueryScorer, valSrcScorers);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.Explanation explain(org.apache.lucene.index.AtomicReaderContext context, int doc) throws java.io.IOException
		public override Explanation explain(AtomicReaderContext context, int doc)
		{
		  Explanation explain = doExplain(context, doc);
		  return explain == null ? new Explanation(0.0f, "no matching docs") : explain;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.search.Explanation doExplain(org.apache.lucene.index.AtomicReaderContext info, int doc) throws java.io.IOException
		internal virtual Explanation doExplain(AtomicReaderContext info, int doc)
		{
		  Explanation subQueryExpl = subQueryWeight.explain(info, doc);
		  if (!subQueryExpl.Match)
		  {
			return subQueryExpl;
		  }
		  // match
		  Explanation[] valSrcExpls = new Explanation[valSrcWeights.Length];
		  for (int i = 0; i < valSrcWeights.Length; i++)
		  {
			valSrcExpls[i] = valSrcWeights[i].explain(info, doc);
		  }
		  Explanation customExp = outerInstance.getCustomScoreProvider(info).customExplain(doc,subQueryExpl,valSrcExpls);
		  float sc = Boost * customExp.Value;
		  Explanation res = new ComplexExplanation(true, sc, outerInstance.ToString() + ", product of:");
		  res.addDetail(customExp);
		  res.addDetail(new Explanation(Boost, "queryBoost")); // actually using the q boost as q weight (== weight value)
		  return res;
		}

		public override bool scoresDocsOutOfOrder()
		{
		  return false;
		}

	  }


	  //=========================== S C O R E R ============================

	  /// <summary>
	  /// A scorer that applies a (callback) function on scores of the subQuery.
	  /// </summary>
	  private class CustomScorer : Scorer
	  {
		  private readonly CustomScoreQuery outerInstance;

		internal readonly float qWeight;
		internal readonly Scorer subQueryScorer;
		internal readonly Scorer[] valSrcScorers;
		internal readonly CustomScoreProvider provider;
		internal readonly float[] vScores; // reused in score() to avoid allocating this array for each doc

		// constructor
		internal CustomScorer(CustomScoreQuery outerInstance, CustomScoreProvider provider, CustomWeight w, float qWeight, Scorer subQueryScorer, Scorer[] valSrcScorers) : base(w)
		{
			this.outerInstance = outerInstance;
		  this.qWeight = qWeight;
		  this.subQueryScorer = subQueryScorer;
		  this.valSrcScorers = valSrcScorers;
		  this.vScores = new float[valSrcScorers.Length];
		  this.provider = provider;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int nextDoc() throws java.io.IOException
		public override int nextDoc()
		{
		  int doc = subQueryScorer.nextDoc();
		  if (doc != NO_MORE_DOCS)
		  {
			foreach (Scorer valSrcScorer in valSrcScorers)
			{
			  valSrcScorer.advance(doc);
			}
		  }
		  return doc;
		}

		public override int docID()
		{
		  return subQueryScorer.docID();
		}

		/*(non-Javadoc) @see org.apache.lucene.search.Scorer#score() */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public float score() throws java.io.IOException
		public override float score()
		{
		  for (int i = 0; i < valSrcScorers.Length; i++)
		  {
			vScores[i] = valSrcScorers[i].score();
		  }
		  return qWeight * provider.customScore(subQueryScorer.docID(), subQueryScorer.score(), vScores);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int freq() throws java.io.IOException
		public override int freq()
		{
		  return subQueryScorer.freq();
		}

		public override ICollection<ChildScorer> Children
		{
			get
			{
			  return Collections.singleton(new ChildScorer(subQueryScorer, "CUSTOM"));
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(int target) throws java.io.IOException
		public override int advance(int target)
		{
		  int doc = subQueryScorer.advance(target);
		  if (doc != NO_MORE_DOCS)
		  {
			foreach (Scorer valSrcScorer in valSrcScorers)
			{
			  valSrcScorer.advance(doc);
			}
		  }
		  return doc;
		}

		public override long cost()
		{
		  return subQueryScorer.cost();
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.Weight createWeight(org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override Weight createWeight(IndexSearcher searcher)
	  {
		return new CustomWeight(this, searcher);
	  }

	  /// <summary>
	  /// Checks if this is strict custom scoring.
	  /// In strict custom scoring, the <seealso cref="ValueSource"/> part does not participate in weight normalization.
	  /// This may be useful when one wants full control over how scores are modified, and does 
	  /// not care about normalizing by the <seealso cref="ValueSource"/> part.
	  /// One particular case where this is useful if for testing this query.   
	  /// <P>
	  /// Note: only has effect when the <seealso cref="ValueSource"/> part is not null.
	  /// </summary>
	  public virtual bool Strict
	  {
		  get
		  {
			return strict;
		  }
		  set
		  {
			this.strict = value;
		  }
	  }


	  /// <summary>
	  /// The sub-query that CustomScoreQuery wraps, affecting both the score and which documents match. </summary>
	  public virtual Query SubQuery
	  {
		  get
		  {
			return subQuery;
		  }
	  }

	  /// <summary>
	  /// The scoring queries that only affect the score of CustomScoreQuery. </summary>
	  public virtual Query[] ScoringQueries
	  {
		  get
		  {
			return scoringQueries;
		  }
	  }

	  /// <summary>
	  /// A short name of this query, used in <seealso cref="#toString(String)"/>.
	  /// </summary>
	  public virtual string name()
	  {
		return "custom";
	  }

	}

}