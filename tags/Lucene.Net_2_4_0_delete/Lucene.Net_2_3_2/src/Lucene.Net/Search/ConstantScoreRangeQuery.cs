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

namespace Lucene.Net.Search
{
	
	/// <summary> A range query that returns a constant score equal to its boost for
	/// all documents in the range.
	/// <p>
	/// It does not have an upper bound on the number of clauses covered in the range.
	/// <p>
	/// If an endpoint is null, it is said to be "open".
	/// Either or both endpoints may be open.  Open endpoints may not be exclusive
	/// (you can't select all but the first or last term without explicitly specifying the term to exclude.)
	/// 
	/// 
	/// </summary>
	/// <version>  $Id: ConstantScoreRangeQuery.java 564236 2007-08-09 15:21:19Z gsingers $
	/// </version>
	
	[Serializable]
	public class ConstantScoreRangeQuery : Query
	{
		private System.String fieldName;
		private System.String lowerVal;
		private System.String upperVal;
		private bool includeLower;
		private bool includeUpper;
		
		
		public ConstantScoreRangeQuery(System.String fieldName, System.String lowerVal, System.String upperVal, bool includeLower, bool includeUpper)
		{
			// do a little bit of normalization...
			// open ended range queries should always be inclusive.
			if (lowerVal == null)
			{
				includeLower = true;
			}
			else if (includeLower && lowerVal.Equals(""))
			{
				lowerVal = null;
			}
			if (upperVal == null)
			{
				includeUpper = true;
			}
			
			
			this.fieldName = String.Intern(fieldName); // intern it, just like terms...
			this.lowerVal = lowerVal;
			this.upperVal = upperVal;
			this.includeLower = includeLower;
			this.includeUpper = includeUpper;
		}
		
		/// <summary>Returns the field name for this query </summary>
		public virtual System.String GetField()
		{
			return fieldName;
		}
		/// <summary>Returns the value of the lower endpoint of this range query, null if open ended </summary>
		public virtual System.String GetLowerVal()
		{
			return lowerVal;
		}
		/// <summary>Returns the value of the upper endpoint of this range query, null if open ended </summary>
		public virtual System.String GetUpperVal()
		{
			return upperVal;
		}
		/// <summary>Returns <code>true</code> if the lower endpoint is inclusive </summary>
		public virtual bool IncludesLower()
		{
			return includeLower;
		}
		/// <summary>Returns <code>true</code> if the upper endpoint is inclusive </summary>
		public virtual bool IncludesUpper()
		{
			return includeUpper;
		}
		
		public override Query Rewrite(IndexReader reader)
		{
			// Map to RangeFilter semantics which are slightly different...
			RangeFilter rangeFilt = new RangeFilter(fieldName, lowerVal != null ? lowerVal : "", upperVal, (System.Object) lowerVal == (System.Object) "" ? false : includeLower, upperVal == null ? false : includeUpper);
			Query q = new ConstantScoreQuery(rangeFilt);
			q.SetBoost(GetBoost());
			return q;
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
			buffer.Append(includeLower ? '[' : '{');
			buffer.Append(lowerVal != null ? lowerVal : "*");
			buffer.Append(" TO ");
			buffer.Append(upperVal != null ? upperVal : "*");
			buffer.Append(includeUpper ? ']' : '}');
            buffer.Append(Lucene.Net.Util.ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}
		
		/// <summary>Returns true if <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (this == o)
				return true;
			if (!(o is ConstantScoreRangeQuery))
				return false;
			ConstantScoreRangeQuery other = (ConstantScoreRangeQuery) o;
			
			if ((System.Object) this.fieldName != (System.Object) other.fieldName || this.includeLower != other.includeLower || this.includeUpper != other.includeUpper)
			{
				return false;
			}
			if (this.lowerVal != null ? !this.lowerVal.Equals(other.lowerVal) : other.lowerVal != null)
				return false;
			if (this.upperVal != null ? !this.upperVal.Equals(other.upperVal) : other.upperVal != null)
				return false;
			return this.GetBoost() == other.GetBoost();
		}
		
		/// <summary>Returns a hash code value for this object.</summary>
		public override int GetHashCode()
		{
			int h = BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0) ^ fieldName.GetHashCode();
			// hashCode of "" is 0, so don't use that for null...
			h ^= (lowerVal != null ? lowerVal.GetHashCode() : unchecked((int) 0x965a965a));     // {{Aroush-1.9}} Is this OK?!
			// don't just XOR upperVal with out mixing either it or h, as it will cancel
			// out lowerVal if they are equal.
			h ^= ((h << 17) | (SupportClass.Number.URShift(h, 16))); // a reversible (one to one) 32 bit mapping mix
			h ^= (upperVal != null ? (upperVal.GetHashCode()) : 0x5a695a69);
			h ^= (includeLower ? 0x665599aa : 0) ^ (includeUpper ? unchecked((int) 0x99aa5566) : 0);    // {{Aroush-1.9}} Is this OK?!
			return h;
		}
		
		override public System.Object Clone()
		{
            // {{Aroush-1.9}} is this all that we need to clone?!
            ConstantScoreRangeQuery clone = (ConstantScoreRangeQuery) base.Clone();
            clone.fieldName = (System.String) this.fieldName.Clone();
            clone.lowerVal = (System.String) this.lowerVal.Clone();
            clone.upperVal = (System.String) this.upperVal.Clone();
            clone.includeLower = this.includeLower;
            clone.includeUpper = this.includeUpper;
            return clone;
        }
	}
}