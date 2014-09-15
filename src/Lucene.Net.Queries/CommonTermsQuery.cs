using System;
using System.Diagnostics;
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
	using Fields = org.apache.lucene.index.Fields;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using Term = org.apache.lucene.index.Term;
	using TermContext = org.apache.lucene.index.TermContext;
	using Terms = org.apache.lucene.index.Terms;
	using TermsEnum = org.apache.lucene.index.TermsEnum;
	using BooleanClause = org.apache.lucene.search.BooleanClause;
	using Occur = org.apache.lucene.search.BooleanClause.Occur;
	using BooleanQuery = org.apache.lucene.search.BooleanQuery;
	using Query = org.apache.lucene.search.Query;
	using TermQuery = org.apache.lucene.search.TermQuery;
	using Similarity = org.apache.lucene.search.similarities.Similarity;
	using ToStringUtils = org.apache.lucene.util.ToStringUtils;


	/// <summary>
	/// A query that executes high-frequency terms in a optional sub-query to prevent
	/// slow queries due to "common" terms like stopwords. This query
	/// builds 2 queries off the <seealso cref="#add(Term) added"/> terms: low-frequency
	/// terms are added to a required boolean clause and high-frequency terms are
	/// added to an optional boolean clause. The optional clause is only executed if
	/// the required "low-frequency" clause matches. Scores produced by this query
	/// will be slightly different than plain <seealso cref="BooleanQuery"/> scorer mainly due to
	/// differences in the <seealso cref="Similarity#coord(int,int) number of leaf queries"/>
	/// in the required boolean clause. In most cases, high-frequency terms are
	/// unlikely to significantly contribute to the document score unless at least
	/// one of the low-frequency terms are matched.  This query can improve
	/// query execution times significantly if applicable.
	/// <para>
	/// <seealso cref="CommonTermsQuery"/> has several advantages over stopword filtering at
	/// index or query time since a term can be "classified" based on the actual
	/// document frequency in the index and can prevent slow queries even across
	/// domains without specialized stopword files.
	/// </para>
	/// <para>
	/// <b>Note:</b> if the query only contains high-frequency terms the query is
	/// rewritten into a plain conjunction query ie. all high-frequency terms need to
	/// match in order to match a document.
	/// </para>
	/// </summary>
	public class CommonTermsQuery : Query
	{
	  /*
	   * TODO maybe it would make sense to abstract this even further and allow to
	   * rewrite to dismax rather than boolean. Yet, this can already be subclassed
	   * to do so.
	   */
	  protected internal readonly IList<Term> terms = new List<Term>();
	  protected internal readonly bool disableCoord;
	  protected internal readonly float maxTermFrequency;
	  protected internal readonly BooleanClause.Occur lowFreqOccur;
	  protected internal readonly BooleanClause.Occur highFreqOccur;
	  protected internal float lowFreqBoost = 1.0f;
	  protected internal float highFreqBoost = 1.0f;
	  protected internal float lowFreqMinNrShouldMatch = 0;
	  protected internal float highFreqMinNrShouldMatch = 0;

	  /// <summary>
	  /// Creates a new <seealso cref="CommonTermsQuery"/>
	  /// </summary>
	  /// <param name="highFreqOccur">
	  ///          <seealso cref="Occur"/> used for high frequency terms </param>
	  /// <param name="lowFreqOccur">
	  ///          <seealso cref="Occur"/> used for low frequency terms </param>
	  /// <param name="maxTermFrequency">
	  ///          a value in [0..1) (or absolute number >=1) representing the
	  ///          maximum threshold of a terms document frequency to be considered a
	  ///          low frequency term. </param>
	  /// <exception cref="IllegalArgumentException">
	  ///           if <seealso cref="Occur#MUST_NOT"/> is pass as lowFreqOccur or
	  ///           highFreqOccur </exception>
	  public CommonTermsQuery(BooleanClause.Occur highFreqOccur, BooleanClause.Occur lowFreqOccur, float maxTermFrequency) : this(highFreqOccur, lowFreqOccur, maxTermFrequency, false)
	  {
	  }

	  /// <summary>
	  /// Creates a new <seealso cref="CommonTermsQuery"/>
	  /// </summary>
	  /// <param name="highFreqOccur">
	  ///          <seealso cref="Occur"/> used for high frequency terms </param>
	  /// <param name="lowFreqOccur">
	  ///          <seealso cref="Occur"/> used for low frequency terms </param>
	  /// <param name="maxTermFrequency">
	  ///          a value in [0..1) (or absolute number >=1) representing the
	  ///          maximum threshold of a terms document frequency to be considered a
	  ///          low frequency term. </param>
	  /// <param name="disableCoord">
	  ///          disables <seealso cref="Similarity#coord(int,int)"/> in scoring for the low
	  ///          / high frequency sub-queries </param>
	  /// <exception cref="IllegalArgumentException">
	  ///           if <seealso cref="Occur#MUST_NOT"/> is pass as lowFreqOccur or
	  ///           highFreqOccur </exception>
	  public CommonTermsQuery(BooleanClause.Occur highFreqOccur, BooleanClause.Occur lowFreqOccur, float maxTermFrequency, bool disableCoord)
	  {
		if (highFreqOccur == BooleanClause.Occur.MUST_NOT)
		{
		  throw new System.ArgumentException("highFreqOccur should be MUST or SHOULD but was MUST_NOT");
		}
		if (lowFreqOccur == BooleanClause.Occur.MUST_NOT)
		{
		  throw new System.ArgumentException("lowFreqOccur should be MUST or SHOULD but was MUST_NOT");
		}
		this.disableCoord = disableCoord;
		this.highFreqOccur = highFreqOccur;
		this.lowFreqOccur = lowFreqOccur;
		this.maxTermFrequency = maxTermFrequency;
	  }

	  /// <summary>
	  /// Adds a term to the <seealso cref="CommonTermsQuery"/>
	  /// </summary>
	  /// <param name="term">
	  ///          the term to add </param>
	  public virtual void add(Term term)
	  {
		if (term == null)
		{
		  throw new System.ArgumentException("Term must not be null");
		}
		this.terms.Add(term);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.Query rewrite(org.apache.lucene.index.IndexReader reader) throws java.io.IOException
	  public override Query rewrite(IndexReader reader)
	  {
		if (this.terms.Count == 0)
		{
		  return new BooleanQuery();
		}
		else if (this.terms.Count == 1)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.Query tq = newTermQuery(this.terms.get(0), null);
		  Query tq = newTermQuery(this.terms[0], null);
		  tq.Boost = Boost;
		  return tq;
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<org.apache.lucene.index.AtomicReaderContext> leaves = reader.leaves();
		IList<AtomicReaderContext> leaves = reader.leaves();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxDoc = reader.maxDoc();
		int maxDoc = reader.maxDoc();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.TermContext[] contextArray = new org.apache.lucene.index.TermContext[terms.size()];
		TermContext[] contextArray = new TermContext[terms.Count];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.Term[] queryTerms = this.terms.toArray(new org.apache.lucene.index.Term[0]);
		Term[] queryTerms = this.terms.ToArray();
		collectTermContext(reader, leaves, contextArray, queryTerms);
		return buildQuery(maxDoc, contextArray, queryTerms);
	  }

	  protected internal virtual int calcLowFreqMinimumNumberShouldMatch(int numOptional)
	  {
		return minNrShouldMatch(lowFreqMinNrShouldMatch, numOptional);
	  }

	  protected internal virtual int calcHighFreqMinimumNumberShouldMatch(int numOptional)
	  {
		return minNrShouldMatch(highFreqMinNrShouldMatch, numOptional);
	  }

	  private int minNrShouldMatch(float minNrShouldMatch, int numOptional)
	  {
		if (minNrShouldMatch >= 1.0f || minNrShouldMatch == 0.0f)
		{
		  return (int) minNrShouldMatch;
		}
		return Math.Round(minNrShouldMatch * numOptional);
	  }

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: protected org.apache.lucene.search.Query buildQuery(final int maxDoc, final org.apache.lucene.index.TermContext[] contextArray, final org.apache.lucene.index.Term[] queryTerms)
	  protected internal virtual Query buildQuery(int maxDoc, TermContext[] contextArray, Term[] queryTerms)
	  {
		BooleanQuery lowFreq = new BooleanQuery(disableCoord);
		BooleanQuery highFreq = new BooleanQuery(disableCoord);
		highFreq.Boost = highFreqBoost;
		lowFreq.Boost = lowFreqBoost;
		BooleanQuery query = new BooleanQuery(true);
		for (int i = 0; i < queryTerms.Length; i++)
		{
		  TermContext termContext = contextArray[i];
		  if (termContext == null)
		  {
			lowFreq.add(newTermQuery(queryTerms[i], null), lowFreqOccur);
		  }
		  else
		  {
			if ((maxTermFrequency >= 1f && termContext.docFreq() > maxTermFrequency) || (termContext.docFreq() > (int) Math.Ceiling(maxTermFrequency * (float) maxDoc)))
			{
			  highFreq.add(newTermQuery(queryTerms[i], termContext), highFreqOccur);
			}
			else
			{
			  lowFreq.add(newTermQuery(queryTerms[i], termContext), lowFreqOccur);
			}
		  }

		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numLowFreqClauses = lowFreq.clauses().size();
		int numLowFreqClauses = lowFreq.clauses().size();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int numHighFreqClauses = highFreq.clauses().size();
		int numHighFreqClauses = highFreq.clauses().size();
		if (lowFreqOccur == BooleanClause.Occur.SHOULD && numLowFreqClauses > 0)
		{
		  int minMustMatch = calcLowFreqMinimumNumberShouldMatch(numLowFreqClauses);
		  lowFreq.MinimumNumberShouldMatch = minMustMatch;
		}
		if (highFreqOccur == BooleanClause.Occur.SHOULD && numHighFreqClauses > 0)
		{
		  int minMustMatch = calcHighFreqMinimumNumberShouldMatch(numHighFreqClauses);
		  highFreq.MinimumNumberShouldMatch = minMustMatch;
		}
		if (lowFreq.clauses().Empty)
		{
		  /*
		   * if lowFreq is empty we rewrite the high freq terms in a conjunction to
		   * prevent slow queries.
		   */
		  if (highFreq.MinimumNumberShouldMatch == 0 && highFreqOccur != BooleanClause.Occur.MUST)
		  {
			foreach (BooleanClause booleanClause in highFreq)
			{
				booleanClause.Occur = BooleanClause.Occur.MUST;
			}
		  }
		  highFreq.Boost = Boost;
		  return highFreq;
		}
		else if (highFreq.clauses().Empty)
		{
		  // only do low freq terms - we don't have high freq terms
		  lowFreq.Boost = Boost;
		  return lowFreq;
		}
		else
		{
		  query.add(highFreq, BooleanClause.Occur.SHOULD);
		  query.add(lowFreq, BooleanClause.Occur.MUST);
		  query.Boost = Boost;
		  return query;
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void collectTermContext(org.apache.lucene.index.IndexReader reader, java.util.List<org.apache.lucene.index.AtomicReaderContext> leaves, org.apache.lucene.index.TermContext[] contextArray, org.apache.lucene.index.Term[] queryTerms) throws java.io.IOException
	  public virtual void collectTermContext(IndexReader reader, IList<AtomicReaderContext> leaves, TermContext[] contextArray, Term[] queryTerms)
	  {
		TermsEnum termsEnum = null;
		foreach (AtomicReaderContext context in leaves)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.Fields fields = context.reader().fields();
		  Fields fields = context.reader().fields();
		  if (fields == null)
		  {
			// reader has no fields
			continue;
		  }
		  for (int i = 0; i < queryTerms.Length; i++)
		  {
			Term term = queryTerms[i];
			TermContext termContext = contextArray[i];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.Terms terms = fields.terms(term.field());
			Terms terms = fields.terms(term.field());
			if (terms == null)
			{
			  // field does not exist
			  continue;
			}
			termsEnum = terms.iterator(termsEnum);
			Debug.Assert(termsEnum != null);

			if (termsEnum == TermsEnum.EMPTY)
			{
				continue;
			}
			if (termsEnum.seekExact(term.bytes()))
			{
			  if (termContext == null)
			  {
				contextArray[i] = new TermContext(reader.Context, termsEnum.termState(), context.ord, termsEnum.docFreq(), termsEnum.totalTermFreq());
			  }
			  else
			  {
				termContext.register(termsEnum.termState(), context.ord, termsEnum.docFreq(), termsEnum.totalTermFreq());
			  }

			}

		  }
		}
	  }

	  /// <summary>
	  /// Returns true iff <seealso cref="Similarity#coord(int,int)"/> is disabled in scoring
	  /// for the high and low frequency query instance. The top level query will
	  /// always disable coords.
	  /// </summary>
	  public virtual bool CoordDisabled
	  {
		  get
		  {
			return disableCoord;
		  }
	  }

	  /// <summary>
	  /// Specifies a minimum number of the low frequent optional BooleanClauses which must be
	  /// satisfied in order to produce a match on the low frequency terms query
	  /// part. This method accepts a float value in the range [0..1) as a fraction
	  /// of the actual query terms in the low frequent clause or a number
	  /// <tt>&gt;=1</tt> as an absolut number of clauses that need to match.
	  /// 
	  /// <para>
	  /// By default no optional clauses are necessary for a match (unless there are
	  /// no required clauses). If this method is used, then the specified number of
	  /// clauses is required.
	  /// </para>
	  /// </summary>
	  /// <param name="min">
	  ///          the number of optional clauses that must match </param>
	  public virtual float LowFreqMinimumNumberShouldMatch
	  {
		  set
		  {
			this.lowFreqMinNrShouldMatch = value;
		  }
		  get
		  {
			return lowFreqMinNrShouldMatch;
		  }
	  }


	  /// <summary>
	  /// Specifies a minimum number of the high frequent optional BooleanClauses which must be
	  /// satisfied in order to produce a match on the low frequency terms query
	  /// part. This method accepts a float value in the range [0..1) as a fraction
	  /// of the actual query terms in the low frequent clause or a number
	  /// <tt>&gt;=1</tt> as an absolut number of clauses that need to match.
	  /// 
	  /// <para>
	  /// By default no optional clauses are necessary for a match (unless there are
	  /// no required clauses). If this method is used, then the specified number of
	  /// clauses is required.
	  /// </para>
	  /// </summary>
	  /// <param name="min">
	  ///          the number of optional clauses that must match </param>
	  public virtual float HighFreqMinimumNumberShouldMatch
	  {
		  set
		  {
			this.highFreqMinNrShouldMatch = value;
		  }
		  get
		  {
			return highFreqMinNrShouldMatch;
		  }
	  }


	  public override void extractTerms(HashSet<Term> terms)
	  {
		terms.addAll(this.terms);
	  }

	  public override string ToString(string field)
	  {
		StringBuilder buffer = new StringBuilder();
		bool needParens = (Boost != 1.0) || (LowFreqMinimumNumberShouldMatch > 0);
		if (needParens)
		{
		  buffer.Append("(");
		}
		for (int i = 0; i < terms.Count; i++)
		{
		  Term t = terms[i];
		  buffer.Append(newTermQuery(t, null).ToString());

		  if (i != terms.Count - 1)
		  {
			  buffer.Append(", ");
		  }
		}
		if (needParens)
		{
		  buffer.Append(")");
		}
		if (LowFreqMinimumNumberShouldMatch > 0 || HighFreqMinimumNumberShouldMatch > 0)
		{
		  buffer.Append('~');
		  buffer.Append("(");
		  buffer.Append(LowFreqMinimumNumberShouldMatch);
		  buffer.Append(HighFreqMinimumNumberShouldMatch);
		  buffer.Append(")");
		}
		if (Boost != 1.0f)
		{
		  buffer.Append(ToStringUtils.boost(Boost));
		}
		return buffer.ToString();
	  }

	  public override int GetHashCode()
	  {
		const int prime = 31;
		int result = base.GetHashCode();
		result = prime * result + (disableCoord ? 1231 : 1237);
		result = prime * result + float.floatToIntBits(highFreqBoost);
		result = prime * result + ((highFreqOccur == null) ? 0 : highFreqOccur.GetHashCode());
		result = prime * result + float.floatToIntBits(lowFreqBoost);
		result = prime * result + ((lowFreqOccur == null) ? 0 : lowFreqOccur.GetHashCode());
		result = prime * result + float.floatToIntBits(maxTermFrequency);
		result = prime * result + float.floatToIntBits(lowFreqMinNrShouldMatch);
		result = prime * result + float.floatToIntBits(highFreqMinNrShouldMatch);
		result = prime * result + ((terms == null) ? 0 : terms.GetHashCode());
		return result;
	  }

	  public override bool Equals(object obj)
	  {
		if (this == obj)
		{
			return true;
		}
		if (!base.Equals(obj))
		{
			return false;
		}
		if (this.GetType() != obj.GetType())
		{
			return false;
		}
		CommonTermsQuery other = (CommonTermsQuery) obj;
		if (disableCoord != other.disableCoord)
		{
			return false;
		}
		if (float.floatToIntBits(highFreqBoost) != float.floatToIntBits(other.highFreqBoost))
		{
			return false;
		}
		if (highFreqOccur != other.highFreqOccur)
		{
			return false;
		}
		if (float.floatToIntBits(lowFreqBoost) != float.floatToIntBits(other.lowFreqBoost))
		{
			return false;
		}
		if (lowFreqOccur != other.lowFreqOccur)
		{
			return false;
		}
		if (float.floatToIntBits(maxTermFrequency) != float.floatToIntBits(other.maxTermFrequency))
		{
			return false;
		}
		if (lowFreqMinNrShouldMatch != other.lowFreqMinNrShouldMatch)
		{
			return false;
		}
		if (highFreqMinNrShouldMatch != other.highFreqMinNrShouldMatch)
		{
			return false;
		}
		if (terms == null)
		{
		  if (other.terms != null)
		  {
			  return false;
		  }
		}
		else if (!terms.Equals(other.terms))
		{
			return false;
		}
		return true;
	  }

	  /// <summary>
	  /// Builds a new TermQuery instance.
	  /// <para>This is intended for subclasses that wish to customize the generated queries.</para> </summary>
	  /// <param name="term"> term </param>
	  /// <param name="context"> the TermContext to be used to create the low level term query. Can be <code>null</code>. </param>
	  /// <returns> new TermQuery instance </returns>
	  protected internal virtual Query newTermQuery(Term term, TermContext context)
	  {
		return context == null ? new TermQuery(term) : new TermQuery(term, context);
	  }
	}

}