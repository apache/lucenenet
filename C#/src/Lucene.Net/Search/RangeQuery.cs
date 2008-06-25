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

using Term = Lucene.Net.Index.Term;
using TermEnum = Lucene.Net.Index.TermEnum;
using IndexReader = Lucene.Net.Index.IndexReader;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search
{
	
	/// <summary> A Query that matches documents within an exclusive range. A RangeQuery
	/// is built by QueryParser for input like <code>[010 TO 120]</code> but only if the QueryParser has 
	/// the useOldRangeQuery property set to true. The QueryParser default behaviour is to use
	/// the newer ConstantScoreRangeQuery class. This is generally preferable because:
	/// <ul>
	/// <li>It is faster than RangeQuery</li>
	/// <li>Unlike RangeQuery, it does not cause a BooleanQuery.TooManyClauses exception if the range of values is large</li>
	/// <li>Unlike RangeQuery it does not influence scoring based on the scarcity of individual terms that may match</li>
	/// </ul>
	/// 
	/// 
	/// </summary>
	/// <seealso cref="ConstantScoreRangeQuery">
	/// 
	/// 
	/// </seealso>
	/// <version>  $Id: RangeQuery.java 520891 2007-03-21 13:58:47Z yonik $
	/// </version>
	[Serializable]
	public class RangeQuery : Query
	{
		private Term lowerTerm;
		private Term upperTerm;
		private bool inclusive;
		
		/// <summary>Constructs a query selecting all terms greater than
		/// <code>lowerTerm</code> but less than <code>upperTerm</code>.
		/// There must be at least one term and either term may be null,
		/// in which case there is no bound on that side, but if there are
		/// two terms, both terms <b>must</b> be for the same field.
		/// </summary>
		public RangeQuery(Term lowerTerm, Term upperTerm, bool inclusive)
		{
			if (lowerTerm == null && upperTerm == null)
			{
				throw new System.ArgumentException("At least one term must be non-null");
			}
			if (lowerTerm != null && upperTerm != null && lowerTerm.Field() != upperTerm.Field())
			{
				throw new System.ArgumentException("Both terms must be for the same field");
			}
			
			// if we have a lowerTerm, start there. otherwise, start at beginning
			if (lowerTerm != null)
			{
				this.lowerTerm = lowerTerm;
			}
			else
			{
				this.lowerTerm = new Term(upperTerm.Field(), "");
			}
			
			this.upperTerm = upperTerm;
			this.inclusive = inclusive;
		}
		
		public override Query Rewrite(IndexReader reader)
		{
			
			BooleanQuery query = new BooleanQuery(true);
			TermEnum enumerator = reader.Terms(lowerTerm);
			
			try
			{
				
				bool checkLower = false;
				if (!inclusive)
				// make adjustments to set to exclusive
					checkLower = true;
				
				System.String testField = GetField();
				
				do 
				{
					Term term = enumerator.Term();
					if (term != null && term.Field() == testField)
					{
						// interned comparison
						if (!checkLower || String.CompareOrdinal(term.Text(), lowerTerm.Text()) > 0)
						{
							checkLower = false;
							if (upperTerm != null)
							{
								int compare = String.CompareOrdinal(upperTerm.Text(), term.Text());
								/* if beyond the upper term, or is exclusive and
								* this is equal to the upper term, break out */
								if ((compare < 0) || (!inclusive && compare == 0))
									break;
							}
							TermQuery tq = new TermQuery(term); // found a match
							tq.SetBoost(GetBoost()); // set the boost
							query.Add(tq, BooleanClause.Occur.SHOULD); // add to query
						}
					}
					else
					{
						break;
					}
				}
				while (enumerator.Next());
			}
			finally
			{
				enumerator.Close();
			}
			return query;
		}
		
		/// <summary>Returns the field name for this query </summary>
		public virtual System.String GetField()
		{
			return (lowerTerm != null ? lowerTerm.Field() : upperTerm.Field());
		}
		
		/// <summary>Returns the lower term of this range query </summary>
		public virtual Term GetLowerTerm()
		{
			return lowerTerm;
		}
		
		/// <summary>Returns the upper term of this range query </summary>
		public virtual Term GetUpperTerm()
		{
			return upperTerm;
		}
		
		/// <summary>Returns <code>true</code> if the range query is inclusive </summary>
		public virtual bool IsInclusive()
		{
			return inclusive;
		}
		
		
		/// <summary>Prints a user-readable version of this query. </summary>
		public override System.String ToString(System.String field)
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			if (!GetField().Equals(field))
			{
				buffer.Append(GetField());
				buffer.Append(":");
			}
			buffer.Append(inclusive ? "[" : "{");
			buffer.Append(lowerTerm != null ? lowerTerm.Text() : "null");
			buffer.Append(" TO ");
			buffer.Append(upperTerm != null ? upperTerm.Text() : "null");
			buffer.Append(inclusive ? "]" : "}");
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}
		
		/// <summary>Returns true iff <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (this == o)
				return true;
			if (!(o is RangeQuery))
				return false;
			
			RangeQuery other = (RangeQuery) o;
			if (this.GetBoost() != other.GetBoost())
				return false;
			if (this.inclusive != other.inclusive)
				return false;
			// one of lowerTerm and upperTerm can be null
			if (this.lowerTerm != null ? !this.lowerTerm.Equals(other.lowerTerm) : other.lowerTerm != null)
				return false;
			if (this.upperTerm != null ? !this.upperTerm.Equals(other.upperTerm) : other.upperTerm != null)
				return false;
			return true;
		}
		
		/// <summary>Returns a hash code value for this object.</summary>
		public override int GetHashCode()
		{
			int h = BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0);
			h ^= (lowerTerm != null ? lowerTerm.GetHashCode() : 0);
			// reversible mix to make lower and upper position dependent and
			// to prevent them from cancelling out.
			h ^= ((h << 25) | (h >> 8));
			h ^= (upperTerm != null ? upperTerm.GetHashCode() : 0);
			h ^= (this.inclusive ? 0x2742E74A : 0);
			return h;
		}
	}
}