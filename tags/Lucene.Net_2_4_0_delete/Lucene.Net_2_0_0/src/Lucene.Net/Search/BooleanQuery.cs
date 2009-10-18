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
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search
{
	
	/// <summary>A Query that matches documents matching boolean combinations of other
	/// queries, e.g. {@link TermQuery}s, {@link PhraseQuery}s or other
	/// BooleanQuerys.
	/// </summary>
	[Serializable]
	public class BooleanQuery : Query, System.ICloneable
	{
		private class AnonymousClassSimilarityDelegator : SimilarityDelegator
		{
			private void  InitBlock(BooleanQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private BooleanQuery enclosingInstance;
			public BooleanQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal AnonymousClassSimilarityDelegator(BooleanQuery enclosingInstance, Lucene.Net.Search.Similarity Param1):base(Param1)
			{
				InitBlock(enclosingInstance);
			}
			public override float Coord(int overlap, int maxOverlap)
			{
				return 1.0f;
			}
		}
		
		
		private static int maxClauseCount = 1024;
		
		/// <summary>Thrown when an attempt is made to add more than {@link
		/// #GetMaxClauseCount()} clauses. This typically happens if
		/// a PrefixQuery, FuzzyQuery, WildcardQuery, or RangeQuery 
		/// is expanded to many terms during search. 
		/// </summary>
		[Serializable]
		public class TooManyClauses : System.SystemException
		{
		}
		
		/// <summary>Return the maximum number of clauses permitted, 1024 by default.
		/// Attempts to add more than the permitted number of clauses cause {@link
		/// TooManyClauses} to be thrown.
		/// </summary>
		/// <seealso cref="SetMaxClauseCount(int)">
		/// </seealso>
		public static int GetMaxClauseCount()
		{
			return maxClauseCount;
		}
		
		/// <summary>Set the maximum number of clauses permitted per BooleanQuery.
		/// Default value is 1024.
		/// <p>TermQuery clauses are generated from for example prefix queries and
		/// fuzzy queries. Each TermQuery needs some buffer space during search,
		/// so this parameter indirectly controls the maximum buffer requirements for
		/// query search.
		/// <p>When this parameter becomes a bottleneck for a Query one can use a
		/// Filter. For example instead of a {@link RangeQuery} one can use a
		/// {@link RangeFilter}.
		/// <p>Normally the buffers are allocated by the JVM. When using for example
		/// {@link Lucene.Net.store.MMapDirectory} the buffering is left to
		/// the operating system.
		/// </summary>
		public static void  SetMaxClauseCount(int maxClauseCount)
		{
			if (maxClauseCount < 1)
				throw new System.ArgumentException("maxClauseCount must be >= 1");
			BooleanQuery.maxClauseCount = maxClauseCount;
		}
		
		private System.Collections.ArrayList clauses = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
		private bool disableCoord;
		
		/// <summary>Constructs an empty boolean query. </summary>
		public BooleanQuery()
		{
		}
		
		/// <summary>Constructs an empty boolean query.
		/// 
		/// {@link Similarity#Coord(int,int)} may be disabled in scoring, as
		/// appropriate. For example, this score factor does not make sense for most
		/// automatically generated queries, like {@link WildcardQuery} and {@link
		/// FuzzyQuery}.
		/// 
		/// </summary>
		/// <param name="disableCoord">disables {@link Similarity#Coord(int,int)} in scoring.
		/// </param>
		public BooleanQuery(bool disableCoord)
		{
			this.disableCoord = disableCoord;
		}
		
		/// <summary>Returns true iff {@link Similarity#Coord(int,int)} is disabled in
		/// scoring for this query instance.
		/// </summary>
		/// <seealso cref="BooleanQuery(boolean)">
		/// </seealso>
		public virtual bool IsCoordDisabled()
		{
			return disableCoord;
		}
		
		// Implement coord disabling.
		// Inherit javadoc.
		public override Similarity GetSimilarity(Searcher searcher)
		{
			Similarity result = base.GetSimilarity(searcher);
			if (disableCoord)
			{
				// disable coord as requested
				result = new AnonymousClassSimilarityDelegator(this, result);
			}
			return result;
		}
		
		/// <summary> Specifies a minimum number of the optional BooleanClauses
		/// which must be satisifed.
		/// 
		/// <p>
		/// By default no optional clauses are neccessary for a match
		/// (unless there are no required clauses).  If this method is used,
		/// then the specified numebr of clauses is required.
		/// </p>
		/// <p>
		/// Use of this method is totally independant of specifying that
		/// any specific clauses are required (or prohibited).  This number will
		/// only be compared against the number of matching optional clauses.
		/// </p>
		/// <p>
		/// EXPERT NOTE: Using this method will force the use of BooleanWeight2,
		/// regardless of wether setUseScorer14(true) has been called.
		/// </p>
		/// 
		/// </summary>
		/// <param name="min">the number of optional clauses that must match
		/// </param>
		/// <seealso cref="setUseScorer14">
		/// </seealso>
		public virtual void  SetMinimumNumberShouldMatch(int min)
		{
			this.minNrShouldMatch = min;
		}
		protected internal int minNrShouldMatch = 0;
		
		/// <summary> Gets the minimum number of the optional BooleanClauses
		/// which must be satisifed.
		/// </summary>
		public virtual int GetMinimumNumberShouldMatch()
		{
			return minNrShouldMatch;
		}
		
		/// <summary>Adds a clause to a boolean query.
		/// 
		/// </summary>
		/// <throws>  TooManyClauses if the new number of clauses exceeds the maximum clause number </throws>
		/// <seealso cref="GetMaxClauseCount()">
		/// </seealso>
		public virtual void  Add(Query query, BooleanClause.Occur occur)
		{
			Add(new BooleanClause(query, occur));
		}
		
		/// <summary>Adds a clause to a boolean query.</summary>
		/// <throws>  TooManyClauses if the new number of clauses exceeds the maximum clause number </throws>
		/// <seealso cref="GetMaxClauseCount()">
		/// </seealso>
		public virtual void  Add(BooleanClause clause)
		{
			if (clauses.Count >= maxClauseCount)
				throw new TooManyClauses();
			
			clauses.Add(clause);
		}
		
		/// <summary>Returns the set of clauses in this query. </summary>
		public virtual BooleanClause[] GetClauses()
		{
			return (BooleanClause[]) clauses.ToArray(typeof(BooleanClause));
		}
		
		[Serializable]
		private class BooleanWeight : Weight
		{
			private void  InitBlock(BooleanQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private BooleanQuery enclosingInstance;
			public BooleanQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			protected internal Similarity similarity;
			protected internal System.Collections.ArrayList weights = System.Collections.ArrayList.Synchronized(new System.Collections.ArrayList(10));
			
			public BooleanWeight(BooleanQuery enclosingInstance, Searcher searcher)
			{
				InitBlock(enclosingInstance);
				this.similarity = Enclosing_Instance.GetSimilarity(searcher);
				for (int i = 0; i < Enclosing_Instance.clauses.Count; i++)
				{
					BooleanClause c = (BooleanClause) Enclosing_Instance.clauses[i];
					weights.Add(c.GetQuery().CreateWeight(searcher));
				}
			}
			
			public virtual Query GetQuery()
			{
				return Enclosing_Instance;
			}
			public virtual float GetValue()
			{
				return Enclosing_Instance.GetBoost();
			}
			
			public virtual float SumOfSquaredWeights()
			{
				float sum = 0.0f;
				for (int i = 0; i < weights.Count; i++)
				{
					BooleanClause c = (BooleanClause) Enclosing_Instance.clauses[i];
					Weight w = (Weight) weights[i];
					if (!c.IsProhibited())
						sum += w.SumOfSquaredWeights(); // sum sub weights
				}
				
				sum *= Enclosing_Instance.GetBoost() * Enclosing_Instance.GetBoost(); // boost each sub-weight
				
				return sum;
			}
			
			
			public virtual void  Normalize(float norm)
			{
				norm *= Enclosing_Instance.GetBoost(); // incorporate boost
				for (int i = 0; i < weights.Count; i++)
				{
					BooleanClause c = (BooleanClause) Enclosing_Instance.clauses[i];
					Weight w = (Weight) weights[i];
					if (!c.IsProhibited())
						w.Normalize(norm);
				}
			}
			
			/// <returns> A good old 1.4 Scorer 
			/// </returns>
			public virtual Scorer Scorer(IndexReader reader)
			{
				// First see if the (faster) ConjunctionScorer will work.  This can be
				// used when all clauses are required.  Also, at this point a
				// BooleanScorer cannot be embedded in a ConjunctionScorer, as the hits
				// from a BooleanScorer are not always sorted by document number (sigh)
				// and hence BooleanScorer cannot implement skipTo() correctly, which is
				// required by ConjunctionScorer.
				bool allRequired = true;
				bool noneBoolean = true;
				for (int i = 0; i < weights.Count; i++)
				{
					BooleanClause c = (BooleanClause) Enclosing_Instance.clauses[i];
					if (!c.IsRequired())
						allRequired = false;
					if (c.GetQuery() is BooleanQuery)
						noneBoolean = false;
				}
				
				if (allRequired && noneBoolean)
				{
					// ConjunctionScorer is okay
					ConjunctionScorer result = new ConjunctionScorer(similarity);
					for (int i = 0; i < weights.Count; i++)
					{
						Weight w = (Weight) weights[i];
						Scorer subScorer = w.Scorer(reader);
						if (subScorer == null)
							return null;
						result.Add(subScorer);
					}
					return result;
				}
				
				// Use good-old BooleanScorer instead.
				BooleanScorer result2 = new BooleanScorer(similarity);
				
				for (int i = 0; i < weights.Count; i++)
				{
					BooleanClause c = (BooleanClause) Enclosing_Instance.clauses[i];
					Weight w = (Weight) weights[i];
					Scorer subScorer = w.Scorer(reader);
					if (subScorer != null)
						result2.Add(subScorer, c.IsRequired(), c.IsProhibited());
					else if (c.IsRequired())
						return null;
				}
				
				return result2;
			}
			
			public virtual Explanation Explain(IndexReader reader, int doc)
			{
				Explanation sumExpl = new Explanation();
				sumExpl.SetDescription("sum of:");
				int coord = 0;
				int maxCoord = 0;
				float sum = 0.0f;
				for (int i = 0; i < weights.Count; i++)
				{
					BooleanClause c = (BooleanClause) Enclosing_Instance.clauses[i];
					Weight w = (Weight) weights[i];
					Explanation e = w.Explain(reader, doc);
					if (!c.IsProhibited())
						maxCoord++;
					if (e.GetValue() > 0)
					{
						if (!c.IsProhibited())
						{
							sumExpl.AddDetail(e);
							sum += e.GetValue();
							coord++;
						}
						else
						{
							return new Explanation(0.0f, "match prohibited");
						}
					}
					else if (c.IsRequired())
					{
						return new Explanation(0.0f, "match required");
					}
				}
				sumExpl.SetValue(sum);
				
				if (coord == 1)
				// only one clause matched
					sumExpl = sumExpl.GetDetails()[0]; // eliminate wrapper
				
				float coordFactor = similarity.Coord(coord, maxCoord);
				if (coordFactor == 1.0f)
				// coord is no-op
					return sumExpl;
				// eliminate wrapper
				else
				{
					Explanation result = new Explanation();
					result.SetDescription("product of:");
					result.AddDetail(sumExpl);
					result.AddDetail(new Explanation(coordFactor, "coord(" + coord + "/" + maxCoord + ")"));
					result.SetValue(sum * coordFactor);
					return result;
				}
			}
		}
		
		[Serializable]
		private class BooleanWeight2 : BooleanWeight
		{
			private void  InitBlock(BooleanQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private BooleanQuery enclosingInstance;
			public new BooleanQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			/* Merge into BooleanWeight in case the 1.4 BooleanScorer is dropped */
			public BooleanWeight2(BooleanQuery enclosingInstance, Searcher searcher):base(enclosingInstance, searcher)
			{
				InitBlock(enclosingInstance);
			}
			
			/// <returns> An alternative Scorer that uses and provides skipTo(),
			/// and scores documents in document number order.
			/// </returns>
			public override Scorer Scorer(IndexReader reader)
			{
				BooleanScorer2 result = new BooleanScorer2(similarity, Enclosing_Instance.minNrShouldMatch);
				
				for (int i = 0; i < weights.Count; i++)
				{
					BooleanClause c = (BooleanClause) Enclosing_Instance.clauses[i];
					Weight w = (Weight) weights[i];
					Scorer subScorer = w.Scorer(reader);
					if (subScorer != null)
						result.Add(subScorer, c.IsRequired(), c.IsProhibited());
					else if (c.IsRequired())
						return null;
				}
				
				return result;
			}
		}
		
		/// <summary>Indicates whether to use good old 1.4 BooleanScorer. </summary>
		private static bool useScorer14 = false;
		
		public static void  SetUseScorer14(bool use14)
		{
			useScorer14 = use14;
		}
		
		public static bool GetUseScorer14()
		{
			return useScorer14;
		}
		
		protected internal override Weight CreateWeight(Searcher searcher)
		{
			
			if (0 < minNrShouldMatch)
			{
				// :TODO: should we throw an exception if getUseScorer14 ?
				return new BooleanWeight2(this, searcher);
			}
			
			return GetUseScorer14() ? (Weight) new BooleanWeight(this, searcher) : (Weight) new BooleanWeight2(this, searcher);
		}
		
		public override Query Rewrite(IndexReader reader)
		{
			if (clauses.Count == 1)
			{
				// optimize 1-clause queries
				BooleanClause c = (BooleanClause) clauses[0];
				if (!c.IsProhibited())
				{
					// just return clause
					
					Query query = c.GetQuery().Rewrite(reader); // rewrite first
					
					if (GetBoost() != 1.0f)
					{
						// incorporate boost
						if (query == c.GetQuery())
						// if rewrite was no-op
							query = (Query) query.Clone(); // then clone before boost
						query.SetBoost(GetBoost() * query.GetBoost());
					}
					
					return query;
				}
			}
			
			BooleanQuery clone = null; // recursively rewrite
			for (int i = 0; i < clauses.Count; i++)
			{
				BooleanClause c = (BooleanClause) clauses[i];
				Query query = c.GetQuery().Rewrite(reader);
				if (query != c.GetQuery())
				{
					// clause rewrote: must clone
					if (clone == null)
						clone = (BooleanQuery) this.Clone();
					clone.clauses[i] = new BooleanClause(query, c.GetOccur());
				}
			}
			if (clone != null)
			{
				return clone; // some clauses rewrote
			}
			else
				return this; // no clauses rewrote
		}
		
		// inherit javadoc
		public override void  ExtractTerms(System.Collections.Hashtable terms)
		{
			for (System.Collections.IEnumerator i = clauses.GetEnumerator(); i.MoveNext(); )
			{
				BooleanClause clause = (BooleanClause) i.Current;
				clause.GetQuery().ExtractTerms(terms);
			}
		}
		
		public override System.Object Clone()
		{
			BooleanQuery clone = (BooleanQuery) base.Clone();
			clone.clauses = (System.Collections.ArrayList) this.clauses.Clone();
			return clone;
		}
		
		/// <summary>Prints a user-readable version of this query. </summary>
		public override System.String ToString(System.String field)
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			bool needParens = (GetBoost() != 1.0) || (GetMinimumNumberShouldMatch() > 0);
			if (needParens)
			{
				buffer.Append("(");
			}
			
			for (int i = 0; i < clauses.Count; i++)
			{
				BooleanClause c = (BooleanClause) clauses[i];
				if (c.IsProhibited())
					buffer.Append("-");
				else if (c.IsRequired())
					buffer.Append("+");
				
				Query subQuery = c.GetQuery();
				if (subQuery is BooleanQuery)
				{
					// wrap sub-bools in parens
					buffer.Append("(");
					buffer.Append(c.GetQuery().ToString(field));
					buffer.Append(")");
				}
				else
					buffer.Append(c.GetQuery().ToString(field));
				
				if (i != clauses.Count - 1)
					buffer.Append(" ");
			}
			
			if (needParens)
			{
				buffer.Append(")");
			}
			
			if (GetMinimumNumberShouldMatch() > 0)
			{
				buffer.Append('~');
				buffer.Append(GetMinimumNumberShouldMatch());
			}
			
			if (GetBoost() != 1.0f)
			{
				buffer.Append(ToStringUtils.Boost(GetBoost()));
			}
			
			return buffer.ToString();
		}
		
		/// <summary>Returns true iff <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (!(o is BooleanQuery))
				return false;
			BooleanQuery other = (BooleanQuery) o;
            if (this.GetBoost() != other.GetBoost())
                return false;
            if (this.clauses.Count != other.clauses.Count)
                return false;
            for (int i = 0; i < this.clauses.Count; i++)
            {
                if (this.clauses[i].Equals(other.clauses[i]) == false)
                    return false;
            }
			return this.GetMinimumNumberShouldMatch() == other.GetMinimumNumberShouldMatch();
		}
		
		/// <summary>Returns a hash code value for this object.</summary>
		public override int GetHashCode()
		{
            int hashCode = 0;

            for (int i = 0; i < clauses.Count; i++)
            {
                hashCode += clauses[i].GetHashCode();
            }

			return BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0) ^ hashCode + GetMinimumNumberShouldMatch();
		}
	}
}