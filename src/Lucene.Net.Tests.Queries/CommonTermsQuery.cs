/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries
{
	/// <summary>
	/// A query that executes high-frequency terms in a optional sub-query to prevent
	/// slow queries due to "common" terms like stopwords.
	/// </summary>
	/// <remarks>
	/// A query that executes high-frequency terms in a optional sub-query to prevent
	/// slow queries due to "common" terms like stopwords. This query
	/// builds 2 queries off the
	/// <see cref="Add(Org.Apache.Lucene.Index.Term)">added</see>
	/// terms: low-frequency
	/// terms are added to a required boolean clause and high-frequency terms are
	/// added to an optional boolean clause. The optional clause is only executed if
	/// the required "low-frequency" clause matches. Scores produced by this query
	/// will be slightly different than plain
	/// <see cref="Org.Apache.Lucene.Search.BooleanQuery">Org.Apache.Lucene.Search.BooleanQuery
	/// 	</see>
	/// scorer mainly due to
	/// differences in the
	/// <see cref="Org.Apache.Lucene.Search.Similarities.Similarity.Coord(int, int)">number of leaf queries
	/// 	</see>
	/// in the required boolean clause. In most cases, high-frequency terms are
	/// unlikely to significantly contribute to the document score unless at least
	/// one of the low-frequency terms are matched.  This query can improve
	/// query execution times significantly if applicable.
	/// <p>
	/// <see cref="CommonTermsQuery">CommonTermsQuery</see>
	/// has several advantages over stopword filtering at
	/// index or query time since a term can be "classified" based on the actual
	/// document frequency in the index and can prevent slow queries even across
	/// domains without specialized stopword files.
	/// </p>
	/// <p>
	/// <b>Note:</b> if the query only contains high-frequency terms the query is
	/// rewritten into a plain conjunction query ie. all high-frequency terms need to
	/// match in order to match a document.
	/// </p>
	/// </remarks>
	public class CommonTermsQuery : Query
	{
		protected internal readonly IList<Term> terms = new AList<Term>();

		protected internal readonly bool disableCoord;

		protected internal readonly float maxTermFrequency;

		protected internal readonly BooleanClause.Occur lowFreqOccur;

		protected internal readonly BooleanClause.Occur highFreqOccur;

		protected internal float lowFreqBoost = 1.0f;

		protected internal float highFreqBoost = 1.0f;

		protected internal float lowFreqMinNrShouldMatch = 0;

		protected internal float highFreqMinNrShouldMatch = 0;

		/// <summary>
		/// Creates a new
		/// <see cref="CommonTermsQuery">CommonTermsQuery</see>
		/// </summary>
		/// <param name="highFreqOccur">
		/// <see cref="Org.Apache.Lucene.Search.BooleanClause.Occur">Org.Apache.Lucene.Search.BooleanClause.Occur
		/// 	</see>
		/// used for high frequency terms
		/// </param>
		/// <param name="lowFreqOccur">
		/// <see cref="Org.Apache.Lucene.Search.BooleanClause.Occur">Org.Apache.Lucene.Search.BooleanClause.Occur
		/// 	</see>
		/// used for low frequency terms
		/// </param>
		/// <param name="maxTermFrequency">
		/// a value in [0..1) (or absolute number &gt;=1) representing the
		/// maximum threshold of a terms document frequency to be considered a
		/// low frequency term.
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <see cref="Org.Apache.Lucene.Search.BooleanClause.Occur.MUST_NOT">Org.Apache.Lucene.Search.BooleanClause.Occur.MUST_NOT
		/// 	</see>
		/// is pass as lowFreqOccur or
		/// highFreqOccur
		/// </exception>
		public CommonTermsQuery(BooleanClause.Occur highFreqOccur, BooleanClause.Occur lowFreqOccur
			, float maxTermFrequency) : this(highFreqOccur, lowFreqOccur, maxTermFrequency, 
			false)
		{
		}

		/// <summary>
		/// Creates a new
		/// <see cref="CommonTermsQuery">CommonTermsQuery</see>
		/// </summary>
		/// <param name="highFreqOccur">
		/// <see cref="Org.Apache.Lucene.Search.BooleanClause.Occur">Org.Apache.Lucene.Search.BooleanClause.Occur
		/// 	</see>
		/// used for high frequency terms
		/// </param>
		/// <param name="lowFreqOccur">
		/// <see cref="Org.Apache.Lucene.Search.BooleanClause.Occur">Org.Apache.Lucene.Search.BooleanClause.Occur
		/// 	</see>
		/// used for low frequency terms
		/// </param>
		/// <param name="maxTermFrequency">
		/// a value in [0..1) (or absolute number &gt;=1) representing the
		/// maximum threshold of a terms document frequency to be considered a
		/// low frequency term.
		/// </param>
		/// <param name="disableCoord">
		/// disables
		/// <see cref="Org.Apache.Lucene.Search.Similarities.Similarity.Coord(int, int)">Org.Apache.Lucene.Search.Similarities.Similarity.Coord(int, int)
		/// 	</see>
		/// in scoring for the low
		/// / high frequency sub-queries
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <see cref="Org.Apache.Lucene.Search.BooleanClause.Occur.MUST_NOT">Org.Apache.Lucene.Search.BooleanClause.Occur.MUST_NOT
		/// 	</see>
		/// is pass as lowFreqOccur or
		/// highFreqOccur
		/// </exception>
		public CommonTermsQuery(BooleanClause.Occur highFreqOccur, BooleanClause.Occur lowFreqOccur
			, float maxTermFrequency, bool disableCoord)
		{
			if (highFreqOccur == BooleanClause.Occur.MUST_NOT)
			{
				throw new ArgumentException("highFreqOccur should be MUST or SHOULD but was MUST_NOT"
					);
			}
			if (lowFreqOccur == BooleanClause.Occur.MUST_NOT)
			{
				throw new ArgumentException("lowFreqOccur should be MUST or SHOULD but was MUST_NOT"
					);
			}
			this.disableCoord = disableCoord;
			this.highFreqOccur = highFreqOccur;
			this.lowFreqOccur = lowFreqOccur;
			this.maxTermFrequency = maxTermFrequency;
		}

		/// <summary>
		/// Adds a term to the
		/// <see cref="CommonTermsQuery">CommonTermsQuery</see>
		/// </summary>
		/// <param name="term">the term to add</param>
		public virtual void Add(Term term)
		{
			if (term == null)
			{
				throw new ArgumentException("Term must not be null");
			}
			this.terms.AddItem(term);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			if (this.terms.IsEmpty())
			{
				return new BooleanQuery();
			}
			else
			{
				if (this.terms.Count == 1)
				{
					Query tq = NewTermQuery(this.terms[0], null);
					tq.SetBoost(GetBoost());
					return tq;
				}
			}
			IList<AtomicReaderContext> leaves = reader.Leaves();
			int maxDoc = reader.MaxDoc();
			TermContext[] contextArray = new TermContext[terms.Count];
			Term[] queryTerms = Sharpen.Collections.ToArray(this.terms, new Term[0]);
			CollectTermContext(reader, leaves, contextArray, queryTerms);
			return BuildQuery(maxDoc, contextArray, queryTerms);
		}

		protected internal virtual int CalcLowFreqMinimumNumberShouldMatch(int numOptional
			)
		{
			return MinNrShouldMatch(lowFreqMinNrShouldMatch, numOptional);
		}

		protected internal virtual int CalcHighFreqMinimumNumberShouldMatch(int numOptional
			)
		{
			return MinNrShouldMatch(highFreqMinNrShouldMatch, numOptional);
		}

		private int MinNrShouldMatch(float minNrShouldMatch, int numOptional)
		{
			if (minNrShouldMatch >= 1.0f || minNrShouldMatch == 0.0f)
			{
				return (int)minNrShouldMatch;
			}
			return Math.Round(minNrShouldMatch * numOptional);
		}

		protected internal virtual Query BuildQuery(int maxDoc, TermContext[] contextArray
			, Term[] queryTerms)
		{
			BooleanQuery lowFreq = new BooleanQuery(disableCoord);
			BooleanQuery highFreq = new BooleanQuery(disableCoord);
			highFreq.SetBoost(highFreqBoost);
			lowFreq.SetBoost(lowFreqBoost);
			BooleanQuery query = new BooleanQuery(true);
			for (int i = 0; i < queryTerms.Length; i++)
			{
				TermContext termContext = contextArray[i];
				if (termContext == null)
				{
					lowFreq.Add(NewTermQuery(queryTerms[i], null), lowFreqOccur);
				}
				else
				{
					if ((maxTermFrequency >= 1f && termContext.DocFreq() > maxTermFrequency) || (termContext
						.DocFreq() > (int)Math.Ceil(maxTermFrequency * (float)maxDoc)))
					{
						highFreq.Add(NewTermQuery(queryTerms[i], termContext), highFreqOccur);
					}
					else
					{
						lowFreq.Add(NewTermQuery(queryTerms[i], termContext), lowFreqOccur);
					}
				}
			}
			int numLowFreqClauses = lowFreq.Clauses().Count;
			int numHighFreqClauses = highFreq.Clauses().Count;
			if (lowFreqOccur == BooleanClause.Occur.SHOULD && numLowFreqClauses > 0)
			{
				int minMustMatch = CalcLowFreqMinimumNumberShouldMatch(numLowFreqClauses);
				lowFreq.SetMinimumNumberShouldMatch(minMustMatch);
			}
			if (highFreqOccur == BooleanClause.Occur.SHOULD && numHighFreqClauses > 0)
			{
				int minMustMatch = CalcHighFreqMinimumNumberShouldMatch(numHighFreqClauses);
				highFreq.SetMinimumNumberShouldMatch(minMustMatch);
			}
			if (lowFreq.Clauses().IsEmpty())
			{
				if (highFreq.GetMinimumNumberShouldMatch() == 0 && highFreqOccur != BooleanClause.Occur
					.MUST)
				{
					foreach (BooleanClause booleanClause in highFreq)
					{
						booleanClause.SetOccur(BooleanClause.Occur.MUST);
					}
				}
				highFreq.SetBoost(GetBoost());
				return highFreq;
			}
			else
			{
				if (highFreq.Clauses().IsEmpty())
				{
					// only do low freq terms - we don't have high freq terms
					lowFreq.SetBoost(GetBoost());
					return lowFreq;
				}
				else
				{
					query.Add(highFreq, BooleanClause.Occur.SHOULD);
					query.Add(lowFreq, BooleanClause.Occur.MUST);
					query.SetBoost(GetBoost());
					return query;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void CollectTermContext(IndexReader reader, IList<AtomicReaderContext
			> leaves, TermContext[] contextArray, Term[] queryTerms)
		{
			TermsEnum termsEnum = null;
			foreach (AtomicReaderContext context in leaves)
			{
				Fields fields = ((AtomicReader)context.Reader()).Fields();
				if (fields == null)
				{
					// reader has no fields
					continue;
				}
				for (int i = 0; i < queryTerms.Length; i++)
				{
					Term term = queryTerms[i];
					TermContext termContext = contextArray[i];
					Terms terms = fields.Terms(term.Field());
					if (terms == null)
					{
						// field does not exist
						continue;
					}
					termsEnum = terms.Iterator(termsEnum);
					if (termsEnum != null == TermsEnum.EMPTY)
					{
						continue;
					}
					if (termsEnum.SeekExact(term.Bytes()))
					{
						if (termContext == null)
						{
							contextArray[i] = new TermContext(reader.GetContext(), termsEnum.TermState(), context
								.ord, termsEnum.DocFreq(), termsEnum.TotalTermFreq());
						}
						else
						{
							termContext.Register(termsEnum.TermState(), context.ord, termsEnum.DocFreq(), termsEnum
								.TotalTermFreq());
						}
					}
				}
			}
		}

		/// <summary>
		/// Returns true iff
		/// <see cref="Org.Apache.Lucene.Search.Similarities.Similarity.Coord(int, int)">Org.Apache.Lucene.Search.Similarities.Similarity.Coord(int, int)
		/// 	</see>
		/// is disabled in scoring
		/// for the high and low frequency query instance. The top level query will
		/// always disable coords.
		/// </summary>
		public virtual bool IsCoordDisabled()
		{
			return disableCoord;
		}

		/// <summary>
		/// Specifies a minimum number of the low frequent optional BooleanClauses which must be
		/// satisfied in order to produce a match on the low frequency terms query
		/// part.
		/// </summary>
		/// <remarks>
		/// Specifies a minimum number of the low frequent optional BooleanClauses which must be
		/// satisfied in order to produce a match on the low frequency terms query
		/// part. This method accepts a float value in the range [0..1) as a fraction
		/// of the actual query terms in the low frequent clause or a number
		/// <tt>&gt;=1</tt> as an absolut number of clauses that need to match.
		/// <p>
		/// By default no optional clauses are necessary for a match (unless there are
		/// no required clauses). If this method is used, then the specified number of
		/// clauses is required.
		/// </p>
		/// </remarks>
		/// <param name="min">the number of optional clauses that must match</param>
		public virtual void SetLowFreqMinimumNumberShouldMatch(float min)
		{
			this.lowFreqMinNrShouldMatch = min;
		}

		/// <summary>
		/// Gets the minimum number of the optional low frequent BooleanClauses which must be
		/// satisfied.
		/// </summary>
		/// <remarks>
		/// Gets the minimum number of the optional low frequent BooleanClauses which must be
		/// satisfied.
		/// </remarks>
		public virtual float GetLowFreqMinimumNumberShouldMatch()
		{
			return lowFreqMinNrShouldMatch;
		}

		/// <summary>
		/// Specifies a minimum number of the high frequent optional BooleanClauses which must be
		/// satisfied in order to produce a match on the low frequency terms query
		/// part.
		/// </summary>
		/// <remarks>
		/// Specifies a minimum number of the high frequent optional BooleanClauses which must be
		/// satisfied in order to produce a match on the low frequency terms query
		/// part. This method accepts a float value in the range [0..1) as a fraction
		/// of the actual query terms in the low frequent clause or a number
		/// <tt>&gt;=1</tt> as an absolut number of clauses that need to match.
		/// <p>
		/// By default no optional clauses are necessary for a match (unless there are
		/// no required clauses). If this method is used, then the specified number of
		/// clauses is required.
		/// </p>
		/// </remarks>
		/// <param name="min">the number of optional clauses that must match</param>
		public virtual void SetHighFreqMinimumNumberShouldMatch(float min)
		{
			this.highFreqMinNrShouldMatch = min;
		}

		/// <summary>
		/// Gets the minimum number of the optional high frequent BooleanClauses which must be
		/// satisfied.
		/// </summary>
		/// <remarks>
		/// Gets the minimum number of the optional high frequent BooleanClauses which must be
		/// satisfied.
		/// </remarks>
		public virtual float GetHighFreqMinimumNumberShouldMatch()
		{
			return highFreqMinNrShouldMatch;
		}

		public override void ExtractTerms(ICollection<Term> terms)
		{
			Sharpen.Collections.AddAll(terms, this.terms);
		}

		public override string ToString(string field)
		{
			StringBuilder buffer = new StringBuilder();
			bool needParens = (GetBoost() != 1.0) || (GetLowFreqMinimumNumberShouldMatch() > 
				0);
			if (needParens)
			{
				buffer.Append("(");
			}
			for (int i = 0; i < terms.Count; i++)
			{
				Term t = terms[i];
				buffer.Append(NewTermQuery(t, null).ToString());
				if (i != terms.Count - 1)
				{
					buffer.Append(", ");
				}
			}
			if (needParens)
			{
				buffer.Append(")");
			}
			if (GetLowFreqMinimumNumberShouldMatch() > 0 || GetHighFreqMinimumNumberShouldMatch
				() > 0)
			{
				buffer.Append('~');
				buffer.Append("(");
				buffer.Append(GetLowFreqMinimumNumberShouldMatch());
				buffer.Append(GetHighFreqMinimumNumberShouldMatch());
				buffer.Append(")");
			}
			if (GetBoost() != 1.0f)
			{
				buffer.Append(ToStringUtils.Boost(GetBoost()));
			}
			return buffer.ToString();
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result = prime * result + (disableCoord ? 1231 : 1237);
			result = prime * result + Sharpen.Runtime.FloatToIntBits(highFreqBoost);
			result = prime * result + ((highFreqOccur == null) ? 0 : highFreqOccur.GetHashCode
				());
			result = prime * result + Sharpen.Runtime.FloatToIntBits(lowFreqBoost);
			result = prime * result + ((lowFreqOccur == null) ? 0 : lowFreqOccur.GetHashCode(
				));
			result = prime * result + Sharpen.Runtime.FloatToIntBits(maxTermFrequency);
			result = prime * result + Sharpen.Runtime.FloatToIntBits(lowFreqMinNrShouldMatch);
			result = prime * result + Sharpen.Runtime.FloatToIntBits(highFreqMinNrShouldMatch
				);
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
			if (GetType() != obj.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.CommonTermsQuery other = (Org.Apache.Lucene.Queries.CommonTermsQuery
				)obj;
			if (disableCoord != other.disableCoord)
			{
				return false;
			}
			if (Sharpen.Runtime.FloatToIntBits(highFreqBoost) != Sharpen.Runtime.FloatToIntBits
				(other.highFreqBoost))
			{
				return false;
			}
			if (highFreqOccur != other.highFreqOccur)
			{
				return false;
			}
			if (Sharpen.Runtime.FloatToIntBits(lowFreqBoost) != Sharpen.Runtime.FloatToIntBits
				(other.lowFreqBoost))
			{
				return false;
			}
			if (lowFreqOccur != other.lowFreqOccur)
			{
				return false;
			}
			if (Sharpen.Runtime.FloatToIntBits(maxTermFrequency) != Sharpen.Runtime.FloatToIntBits
				(other.maxTermFrequency))
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
			else
			{
				if (!terms.Equals(other.terms))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Builds a new TermQuery instance.</summary>
		/// <remarks>
		/// Builds a new TermQuery instance.
		/// <p>This is intended for subclasses that wish to customize the generated queries.</p>
		/// </remarks>
		/// <param name="term">term</param>
		/// <param name="context">the TermContext to be used to create the low level term query. Can be <code>null</code>.
		/// 	</param>
		/// <returns>new TermQuery instance</returns>
		protected internal virtual Query NewTermQuery(Term term, TermContext context)
		{
			return context == null ? new TermQuery(term) : new TermQuery(term, context);
		}
	}
}
