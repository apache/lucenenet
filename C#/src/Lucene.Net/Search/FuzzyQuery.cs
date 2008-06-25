/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using PriorityQueue = Lucene.Net.Util.PriorityQueue;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search
{
	
	/// <summary>Implements the fuzzy search query. The similiarity measurement
	/// is based on the Levenshtein (edit distance) algorithm.
	/// </summary>
	[Serializable]
	public class FuzzyQuery : MultiTermQuery
	{
		
		public const float defaultMinSimilarity = 0.5f;
		public const int defaultPrefixLength = 0;
		
		private float minimumSimilarity;
		private int prefixLength;
		
		/// <summary> Create a new FuzzyQuery that will match terms with a similarity 
		/// of at least <code>minimumSimilarity</code> to <code>term</code>.
		/// If a <code>prefixLength</code> &gt; 0 is specified, a common prefix
		/// of that length is also required.
		/// 
		/// </summary>
		/// <param name="term">the term to search for
		/// </param>
		/// <param name="minimumSimilarity">a value between 0 and 1 to set the required similarity
		/// between the query term and the matching terms. For example, for a
		/// <code>minimumSimilarity</code> of <code>0.5</code> a term of the same length
		/// as the query term is considered similar to the query term if the edit distance
		/// between both terms is less than <code>length(term)*0.5</code>
		/// </param>
		/// <param name="prefixLength">length of common (non-fuzzy) prefix
		/// </param>
		/// <throws>  IllegalArgumentException if minimumSimilarity is &gt;= 1 or &lt; 0 </throws>
		/// <summary> or if prefixLength &lt; 0
		/// </summary>
		public FuzzyQuery(Term term, float minimumSimilarity, int prefixLength):base(term)
		{
			
			if (minimumSimilarity >= 1.0f)
				throw new System.ArgumentException("minimumSimilarity >= 1");
			else if (minimumSimilarity < 0.0f)
				throw new System.ArgumentException("minimumSimilarity < 0");
			if (prefixLength < 0)
				throw new System.ArgumentException("prefixLength < 0");
			
			this.minimumSimilarity = minimumSimilarity;
			this.prefixLength = prefixLength;
		}
		
		/// <summary> Calls {@link #FuzzyQuery(Term, float) FuzzyQuery(term, minimumSimilarity, 0)}.</summary>
		public FuzzyQuery(Term term, float minimumSimilarity):this(term, minimumSimilarity, defaultPrefixLength)
		{
		}
		
		/// <summary> Calls {@link #FuzzyQuery(Term, float) FuzzyQuery(term, 0.5f, 0)}.</summary>
		public FuzzyQuery(Term term):this(term, defaultMinSimilarity, defaultPrefixLength)
		{
		}
		
		/// <summary> Returns the minimum similarity that is required for this query to match.</summary>
		/// <returns> float value between 0.0 and 1.0
		/// </returns>
		public virtual float GetMinSimilarity()
		{
			return minimumSimilarity;
		}
		
		/// <summary> Returns the non-fuzzy prefix length. This is the number of characters at the start
		/// of a term that must be identical (not fuzzy) to the query term if the query
		/// is to match that term. 
		/// </summary>
		public virtual int GetPrefixLength()
		{
			return prefixLength;
		}
		
		protected internal override FilteredTermEnum GetEnum(IndexReader reader)
		{
			return new FuzzyTermEnum(reader, GetTerm(), minimumSimilarity, prefixLength);
		}
		
		public override Query Rewrite(IndexReader reader)
		{
			FilteredTermEnum enumerator = GetEnum(reader);
			int maxClauseCount = BooleanQuery.GetMaxClauseCount();
			ScoreTermQueue stQueue = new ScoreTermQueue(maxClauseCount);
			ScoreTerm reusableST = null;
			
			try
			{
				do 
				{
					float score = 0.0f;
					Term t = enumerator.Term();
					if (t != null)
					{
						score = enumerator.Difference();
						if (reusableST == null)
						{
							reusableST = new ScoreTerm(t, score);
						}
						else if (score >= reusableST.score)
						{
							// reusableST holds the last "rejected" entry, so, if
							// this new score is not better than that, there's no
							// need to try inserting it
							reusableST.score = score;
							reusableST.term = t;
						}
						else
						{
							continue;
						}
						
						reusableST = (ScoreTerm) stQueue.InsertWithOverflow(reusableST);
					}
				}
				while (enumerator.Next());
			}
			finally
			{
				enumerator.Close();
			}
			
			BooleanQuery query = new BooleanQuery(true);
			int size = stQueue.Size();
			for (int i = 0; i < size; i++)
			{
				ScoreTerm st = (ScoreTerm) stQueue.Pop();
				TermQuery tq = new TermQuery(st.term); // found a match
				tq.SetBoost(GetBoost() * st.score); // set the boost
				query.Add(tq, BooleanClause.Occur.SHOULD); // add to query
			}
			
			return query;
		}
		
		public override System.String ToString(System.String field)
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			Term term = GetTerm();
			if (!term.Field().Equals(field))
			{
				buffer.Append(term.Field());
				buffer.Append(":");
			}
			buffer.Append(term.Text());
			buffer.Append('~');
			buffer.Append(SupportClass.Single.ToString(minimumSimilarity));
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}
		
		protected internal class ScoreTerm
		{
			public Term term;
			public float score;
			
			public ScoreTerm(Term term, float score)
			{
				this.term = term;
				this.score = score;
			}
		}
		
		protected internal class ScoreTermQueue:PriorityQueue
		{
			
			public ScoreTermQueue(int size)
			{
				Initialize(size);
			}
			
			/* (non-Javadoc)
			* @see Lucene.Net.Util.PriorityQueue#lessThan(java.lang.Object, java.lang.Object)
			*/
			public override bool LessThan(System.Object a, System.Object b)
			{
				ScoreTerm termA = (ScoreTerm) a;
				ScoreTerm termB = (ScoreTerm) b;
				if (termA.score == termB.score)
					return termA.term.CompareTo(termB.term) > 0;
				else
					return termA.score < termB.score;
			}
		}
		
		public  override bool Equals(System.Object o)
		{
			if (this == o)
				return true;
			if (!(o is FuzzyQuery))
				return false;
			if (!base.Equals(o))
				return false;
			
			FuzzyQuery fuzzyQuery = (FuzzyQuery) o;
			
			if (minimumSimilarity != fuzzyQuery.minimumSimilarity)
				return false;
			if (prefixLength != fuzzyQuery.prefixLength)
				return false;
			
			return true;
		}
		
		public override int GetHashCode()
		{
			int result = base.GetHashCode();
			result = 29 * result + minimumSimilarity != + 0.0f ? BitConverter.ToInt32(BitConverter.GetBytes(minimumSimilarity), 0) : 0;
			result = 29 * result + prefixLength;
			return result;
		}
	}
}