/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Mlt;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Mlt
{
	/// <summary>
	/// A simple wrapper for MoreLikeThis for use in scenarios where a Query object is required eg
	/// in custom QueryParser extensions.
	/// </summary>
	/// <remarks>
	/// A simple wrapper for MoreLikeThis for use in scenarios where a Query object is required eg
	/// in custom QueryParser extensions. At query.rewrite() time the reader is used to construct the
	/// actual MoreLikeThis object and obtain the real Query object.
	/// </remarks>
	public class MoreLikeThisQuery : Query
	{
		private string likeText;

		private string[] moreLikeFields;

		private Analyzer analyzer;

		private readonly string fieldName;

		private float percentTermsToMatch = 0.3f;

		private int minTermFrequency = 1;

		private int maxQueryTerms = 5;

		private ICollection<object> stopWords = null;

		private int minDocFreq = -1;

		/// <param name="moreLikeFields">fields used for similarity measure</param>
		public MoreLikeThisQuery(string likeText, string[] moreLikeFields, Analyzer analyzer
			, string fieldName)
		{
			this.likeText = likeText;
			this.moreLikeFields = moreLikeFields;
			this.analyzer = analyzer;
			this.fieldName = fieldName;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			MoreLikeThis mlt = new MoreLikeThis(reader);
			mlt.SetFieldNames(moreLikeFields);
			mlt.SetAnalyzer(analyzer);
			mlt.SetMinTermFreq(minTermFrequency);
			if (minDocFreq >= 0)
			{
				mlt.SetMinDocFreq(minDocFreq);
			}
			mlt.SetMaxQueryTerms(maxQueryTerms);
			mlt.SetStopWords(stopWords);
			BooleanQuery bq = (BooleanQuery)mlt.Like(new StringReader(likeText), fieldName);
			BooleanClause[] clauses = bq.GetClauses();
			//make at least half the terms match
			bq.SetMinimumNumberShouldMatch((int)(clauses.Length * percentTermsToMatch));
			return bq;
		}

		public override string ToString(string field)
		{
			return "like:" + likeText;
		}

		public virtual float GetPercentTermsToMatch()
		{
			return percentTermsToMatch;
		}

		public virtual void SetPercentTermsToMatch(float percentTermsToMatch)
		{
			this.percentTermsToMatch = percentTermsToMatch;
		}

		public virtual Analyzer GetAnalyzer()
		{
			return analyzer;
		}

		public virtual void SetAnalyzer(Analyzer analyzer)
		{
			this.analyzer = analyzer;
		}

		public virtual string GetLikeText()
		{
			return likeText;
		}

		public virtual void SetLikeText(string likeText)
		{
			this.likeText = likeText;
		}

		public virtual int GetMaxQueryTerms()
		{
			return maxQueryTerms;
		}

		public virtual void SetMaxQueryTerms(int maxQueryTerms)
		{
			this.maxQueryTerms = maxQueryTerms;
		}

		public virtual int GetMinTermFrequency()
		{
			return minTermFrequency;
		}

		public virtual void SetMinTermFrequency(int minTermFrequency)
		{
			this.minTermFrequency = minTermFrequency;
		}

		public virtual string[] GetMoreLikeFields()
		{
			return moreLikeFields;
		}

		public virtual void SetMoreLikeFields(string[] moreLikeFields)
		{
			this.moreLikeFields = moreLikeFields;
		}

		public virtual ICollection<object> GetStopWords()
		{
			return stopWords;
		}

		public virtual void SetStopWords<_T0>(ICollection<_T0> stopWords)
		{
			this.stopWords = stopWords;
		}

		public virtual int GetMinDocFreq()
		{
			return minDocFreq;
		}

		public virtual void SetMinDocFreq(int minDocFreq)
		{
			this.minDocFreq = minDocFreq;
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result = prime * result + ((analyzer == null) ? 0 : analyzer.GetHashCode());
			result = prime * result + ((fieldName == null) ? 0 : fieldName.GetHashCode());
			result = prime * result + ((likeText == null) ? 0 : likeText.GetHashCode());
			result = prime * result + maxQueryTerms;
			result = prime * result + minDocFreq;
			result = prime * result + minTermFrequency;
			result = prime * result + Arrays.HashCode(moreLikeFields);
			result = prime * result + Sharpen.Runtime.FloatToIntBits(percentTermsToMatch);
			result = prime * result + ((stopWords == null) ? 0 : stopWords.GetHashCode());
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
			Org.Apache.Lucene.Queries.Mlt.MoreLikeThisQuery other = (Org.Apache.Lucene.Queries.Mlt.MoreLikeThisQuery
				)obj;
			if (analyzer == null)
			{
				if (other.analyzer != null)
				{
					return false;
				}
			}
			else
			{
				if (!analyzer.Equals(other.analyzer))
				{
					return false;
				}
			}
			if (fieldName == null)
			{
				if (other.fieldName != null)
				{
					return false;
				}
			}
			else
			{
				if (!fieldName.Equals(other.fieldName))
				{
					return false;
				}
			}
			if (likeText == null)
			{
				if (other.likeText != null)
				{
					return false;
				}
			}
			else
			{
				if (!likeText.Equals(other.likeText))
				{
					return false;
				}
			}
			if (maxQueryTerms != other.maxQueryTerms)
			{
				return false;
			}
			if (minDocFreq != other.minDocFreq)
			{
				return false;
			}
			if (minTermFrequency != other.minTermFrequency)
			{
				return false;
			}
			if (!Arrays.Equals(moreLikeFields, other.moreLikeFields))
			{
				return false;
			}
			if (Sharpen.Runtime.FloatToIntBits(percentTermsToMatch) != Sharpen.Runtime.FloatToIntBits
				(other.percentTermsToMatch))
			{
				return false;
			}
			if (stopWords == null)
			{
				if (other.stopWords != null)
				{
					return false;
				}
			}
			else
			{
				if (!stopWords.Equals(other.stopWords))
				{
					return false;
				}
			}
			return true;
		}
	}
}
