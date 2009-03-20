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
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search.Spans
{
	
	/// <summary>Matches spans containing a term. </summary>
	[Serializable]
	public class SpanTermQuery : SpanQuery
	{
		protected internal Term term;
		
		/// <summary>Construct a SpanTermQuery matching the named term's spans. </summary>
		public SpanTermQuery(Term term)
		{
			this.term = term;
		}
		
		/// <summary>Return the term whose spans are matched. </summary>
		public virtual Term GetTerm()
		{
			return term;
		}
		
		public override System.String GetField()
		{
			return term.Field();
		}
		
		/// <summary>Returns a collection of all terms matched by this query.</summary>
		/// <deprecated> use extractTerms instead
		/// </deprecated>
		/// <seealso cref="#ExtractTerms(Set)">
		/// </seealso>
		public override System.Collections.ICollection GetTerms()
		{
			System.Collections.ArrayList terms = new System.Collections.ArrayList();
			terms.Add(term);
			return terms;
		}

		public override void  ExtractTerms(System.Collections.Hashtable terms)
		{
			if (terms.Contains(term) == false)
				terms.Add(term, term);
		}
		
		public override System.String ToString(System.String field)
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			if (term.Field().Equals(field))
				buffer.Append(term.Text());
			else
			{
				buffer.Append(term.ToString());
			}
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}
		
		/// <summary>Returns true iff <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (!(o is SpanTermQuery))
				return false;
			SpanTermQuery other = (SpanTermQuery) o;
			return (this.GetBoost() == other.GetBoost()) && this.term.Equals(other.term);
		}
		
		/// <summary>Returns a hash code value for this object.</summary>
		public override int GetHashCode()
		{
			return GetBoost().ToString().GetHashCode() ^ term.GetHashCode() ^ unchecked((int) 0xD23FE494);    // {{Aroush-1.9}} Is this OK?
		}
		
		public override Spans GetSpans(IndexReader reader)
		{
			return new TermSpans(reader.TermPositions(term), term);
		}
	}
}