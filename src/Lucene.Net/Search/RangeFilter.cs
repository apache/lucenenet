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
using CompareInfo = System.Globalization.CompareInfo;

using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using TermDocs = Lucene.Net.Index.TermDocs;
using TermEnum = Lucene.Net.Index.TermEnum;
using OpenBitSet = Lucene.Net.Util.OpenBitSet;

namespace Lucene.Net.Search
{
	
	/// <summary> A Filter that restricts search results to a range of values in a given
	/// field.
	/// 
	/// <p>
	/// This code borrows heavily from {@link RangeQuery}, but is implemented as a Filter
	/// 
	/// </p>
	/// </summary>
	[Serializable]
	public class RangeFilter : Filter
	{
		
		private System.String fieldName;
		private System.String lowerTerm;
		private System.String upperTerm;
		private bool includeLower;
		private bool includeUpper;
        private CompareInfo collator;
		
		/// <param name="fieldName">The field this range applies to
		/// </param>
		/// <param name="lowerTerm">The lower bound on this range
		/// </param>
		/// <param name="upperTerm">The upper bound on this range
		/// </param>
		/// <param name="includeLower">Does this range include the lower bound?
		/// </param>
		/// <param name="includeUpper">Does this range include the upper bound?
		/// </param>
		/// <throws>  IllegalArgumentException if both terms are null or if </throws>
		/// <summary>  lowerTerm is null and includeLower is true (similar for upperTerm
		/// and includeUpper)
		/// </summary>
		public RangeFilter(System.String fieldName, System.String lowerTerm, System.String upperTerm, bool includeLower, bool includeUpper)
		{
			this.fieldName = fieldName;
			this.lowerTerm = lowerTerm;
			this.upperTerm = upperTerm;
			this.includeLower = includeLower;
			this.includeUpper = includeUpper;
			
			if (null == lowerTerm && null == upperTerm)
			{
				throw new System.ArgumentException("At least one value must be non-null");
			}
			if (includeLower && null == lowerTerm)
			{
				throw new System.ArgumentException("The lower bound must be non-null to be inclusive");
			}
			if (includeUpper && null == upperTerm)
			{
				throw new System.ArgumentException("The upper bound must be non-null to be inclusive");
			}
		}

        /// <summary>
        /// WARNING: Using this constructor and supplying a non-null
        /// value in the collator paramter will cause every single
        /// index Term in the Field referenced by lowerTerm and/or upperTerm
        /// to be examined.  Depending on the number of index Terms in this 
        /// Field, the operation could be very slow.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="lowerTerm">lower bound</param>
        /// <param name="upperTerm">upper bound</param>
        /// <param name="includeLower">is lower bound inclusive</param>
        /// <param name="includeUpper">is upper bound inclusive</param>
        /// <param name="collator">the collator that determines range inclusion; when set to null Unicode code point ordering is used</param>
        public RangeFilter(string fieldName, string lowerTerm, string upperTerm, bool includeLower, bool includeUpper, CompareInfo collator)
            : this(fieldName, lowerTerm, upperTerm, includeLower, includeUpper)
        {
            this.collator = collator;
        }

		/// <summary> Constructs a filter for field <code>fieldName</code> matching
		/// less than or equal to <code>upperTerm</code>.
		/// </summary>
		public static RangeFilter Less(System.String fieldName, System.String upperTerm)
		{
			return new RangeFilter(fieldName, null, upperTerm, false, true);
		}
		
		/// <summary> Constructs a filter for field <code>fieldName</code> matching
		/// greater than or equal to <code>lowerTerm</code>.
		/// </summary>
		public static RangeFilter More(System.String fieldName, System.String lowerTerm)
		{
			return new RangeFilter(fieldName, lowerTerm, null, true, false);
		}
		
		/// <summary> Returns a BitSet with true for documents which should be
		/// permitted in search results, and false for those that should
		/// not.
		/// </summary>
        [Obsolete("use GetDocIdSet(IndexReader) instead")]
		public override System.Collections.BitArray Bits(IndexReader reader)
		{
			System.Collections.BitArray bits = new System.Collections.BitArray(
                (reader.MaxDoc() % 64 == 0 ? reader.MaxDoc() / 64 : reader.MaxDoc() / 64 + 1) * 64);

			TermEnum enumerator = (null != lowerTerm && collator == null ?
                reader.Terms(new Term(fieldName, lowerTerm)) : 
                reader.Terms(new Term(fieldName)));
			
			try
			{
				
				if (enumerator.Term() == null)
				{
					return bits;
				}

                TermDocs termDocs = reader.TermDocs();
                try
                {
                    if (collator != null)
                    {
                        do
                        {
                            Term term = enumerator.Term();
                            if (term != null && term.Field().Equals(fieldName))
                            {
                                if ((lowerTerm == null ||
                                    (includeLower ? collator.Compare(term.Text(), lowerTerm) >= 0 : collator.Compare(term.Text(), lowerTerm) > 0)) &&
                                    (upperTerm == null ||
                                    (includeUpper ? collator.Compare(term.Text(), upperTerm) <= 0 : collator.Compare(term.Text(), upperTerm) < 0)))
                                {
                                    // term in range, lookup docs
                                    termDocs.Seek(enumerator.Term());
                                    while (termDocs.Next())
                                    {
                                        bits.Set(termDocs.Doc(), true);
                                    }
                                }
                            }
                        }
                        while (enumerator.Next());
                    }
                    else // null collator; using Unicode code point ordering
                    {
                        bool checkLower = false;
                        if (!includeLower) // make adjustments to set to exclusive
                            checkLower = true;
                        do
                        {
                            Term term = enumerator.Term();
                            if (term != null && term.Field().Equals(fieldName))
                            {
                                if (!checkLower || null == lowerTerm || String.CompareOrdinal(term.Text(), lowerTerm) > 0)
                                {
                                    checkLower = false;
                                    if (upperTerm != null)
                                    {
                                        int compare = String.CompareOrdinal(upperTerm, term.Text());
                                        /* if beyond the upper term, or is exclusive and
                                        * this is equal to the upper term, break out */
                                        if ((compare < 0) || (!includeUpper && compare == 0))
                                        {
                                            break;
                                        }
                                    }
                                    /* we have a good term, find the docs */

                                    termDocs.Seek(enumerator.Term());
                                    while (termDocs.Next())
                                    {
                                        bits.Set(termDocs.Doc(), true);
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        while (enumerator.Next());
                    }
				}
				finally
				{
					termDocs.Close();
				}
			}
			finally
			{
				enumerator.Close();
			}
			
			return bits;
		}

        /// <summary> Returns a DocIdSet with documents that should be
        /// permitted in search results.
        /// </summary>
        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            OpenBitSet bits = new OpenBitSet(reader.MaxDoc());

            TermEnum enumerator = (null != lowerTerm && collator == null ?
                reader.Terms(new Term(fieldName, lowerTerm)) :
                reader.Terms(new Term(fieldName)));

            try
            {

                if (enumerator.Term() == null)
                {
                    return bits;
                }

                TermDocs termDocs = reader.TermDocs();
                try
                {
                    if (collator != null)
                    {
                        do
                        {
                            Term term = enumerator.Term();
                            if (term != null && term.Field().Equals(fieldName))
                            {
                                if ((lowerTerm == null ||
                                    (includeLower ? collator.Compare(term.Text(), lowerTerm) >= 0 : collator.Compare(term.Text(), lowerTerm) > 0)) &&
                                    (upperTerm == null ||
                                    (includeUpper ? collator.Compare(term.Text(), upperTerm) <= 0 : collator.Compare(term.Text(), upperTerm) < 0)))
                                {
                                    // term in range, lookup docs
                                    termDocs.Seek(enumerator.Term());
                                    while (termDocs.Next())
                                    {
                                        bits.Set(termDocs.Doc());
                                    }
                                }
                            }
                        }
                        while (enumerator.Next());
                    }
                    else // null collator; using Unicode code point ordering
                    {
                        bool checkLower = false;
                        if (!includeLower) // make adjustments to set to exclusive
                            checkLower = true;
                        do
                        {
                            Term term = enumerator.Term();
                            if (term != null && term.Field().Equals(fieldName))
                            {
                                if (!checkLower || null == lowerTerm || String.CompareOrdinal(term.Text(), lowerTerm) > 0)
                                {
                                    checkLower = false;
                                    if (upperTerm != null)
                                    {
                                        int compare = String.CompareOrdinal(upperTerm, term.Text());
                                        /* if beyond the upper term, or is exclusive and
                                        * this is equal to the upper term, break out */
                                        if ((compare < 0) || (!includeUpper && compare == 0))
                                        {
                                            break;
                                        }
                                    }
                                    /* we have a good term, find the docs */

                                    termDocs.Seek(enumerator.Term());
                                    while (termDocs.Next())
                                    {
                                        bits.Set(termDocs.Doc());
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        while (enumerator.Next());
                    }
                }
                finally
                {
                    termDocs.Close();
                }
            }
            finally
            {
                enumerator.Close();
            }

            return bits;
        }

        public override System.String ToString()
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			buffer.Append(fieldName);
			buffer.Append(":");
			buffer.Append(includeLower ? "[" : "{");
			if (null != lowerTerm)
			{
				buffer.Append(lowerTerm);
			}
			buffer.Append("-");
			if (null != upperTerm)
			{
				buffer.Append(upperTerm);
			}
			buffer.Append(includeUpper ? "]" : "}");
			return buffer.ToString();
		}
		
		/// <summary>Returns true if <code>o</code> is equal to this. </summary>
		public  override bool Equals(object o)
		{
			if (this == o)
				return true;
			if (!(o is RangeFilter))
				return false;
			RangeFilter other = (RangeFilter) o;
			
			if (!this.fieldName.Equals(other.fieldName) ||
                this.includeLower != other.includeLower ||
                this.includeUpper != other.includeUpper ||
                (this.collator != null && !this.collator.Equals(other.collator)))
			{
				return false;
			}
			if (this.lowerTerm != null ? !this.lowerTerm.Equals(other.lowerTerm) : other.lowerTerm != null)
				return false;
			if (this.upperTerm != null ? !this.upperTerm.Equals(other.upperTerm) : other.upperTerm != null)
				return false;
			return true;
		}
		
		/// <summary>Returns a hash code value for this object.</summary>
		public override int GetHashCode()
		{
			int h = fieldName.GetHashCode();
			h ^= (lowerTerm != null ? lowerTerm.GetHashCode() : unchecked((int) 0xB6ECE882));           // {{Aroush-1.9}} is this OK?!
			h = (h << 1) | (SupportClass.Number.URShift(h, 31)); // rotate to distinguish lower from upper
			h ^= (upperTerm != null ? (upperTerm.GetHashCode()) : unchecked((int) 0x91BEC2C2));         // {{Aroush-1.9}} is this OK?!
			h ^= (includeLower ? unchecked((int) 0xD484B933) : 0) ^ (includeUpper ? 0x6AE423AC : 0);    // {{Aroush-1.9}} is this OK?!
            h ^= collator != null ? collator.GetHashCode() : 0;
			return h;
		}
	}
}