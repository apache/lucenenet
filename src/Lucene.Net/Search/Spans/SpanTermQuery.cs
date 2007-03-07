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
using TermPositions = Lucene.Net.Index.TermPositions;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search.Spans
{
	
	/// <summary>Matches spans containing a term. </summary>
	[Serializable]
	public class SpanTermQuery:SpanQuery
	{
		private class AnonymousClassSpans : Spans
		{
			public AnonymousClassSpans(Lucene.Net.Index.IndexReader reader, SpanTermQuery enclosingInstance)
			{
				InitBlock(reader, enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Index.IndexReader reader, SpanTermQuery enclosingInstance)
			{
				this.reader = reader;
				this.enclosingInstance = enclosingInstance;
				positions = reader.TermPositions(Enclosing_Instance.term);
			}
			private Lucene.Net.Index.IndexReader reader;
			private SpanTermQuery enclosingInstance;
			public SpanTermQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private TermPositions positions;
			
			private int doc = - 1;
			private int freq;
			private int count;
			private int position;
			
			public virtual bool Next()
			{
				if (count == freq)
				{
					if (!positions.Next())
					{
						doc = System.Int32.MaxValue;
						return false;
					}
					doc = positions.Doc();
					freq = positions.Freq();
					count = 0;
				}
				position = positions.NextPosition();
				count++;
				return true;
			}
			
			public virtual bool SkipTo(int target)
			{
				// are we already at the correct position?
				if (doc >= target)
				{
					return true;
				}
				
				if (!positions.SkipTo(target))
				{
					doc = System.Int32.MaxValue;
					return false;
				}
				
				doc = positions.Doc();
				freq = positions.Freq();
				count = 0;
				
				position = positions.NextPosition();
				count++;
				
				return true;
			}
			
			public virtual int Doc()
			{
				return doc;
			}
			public virtual int Start()
			{
				return position;
			}
			public virtual int End()
			{
				return position + 1;
			}
			
			public override System.String ToString()
			{
				return "spans(" + Enclosing_Instance.ToString() + ")@" + (doc == - 1 ? "START" : ((doc == System.Int32.MaxValue) ? "END" : doc + "-" + position));
			}
		}
		private Term term;
		
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
        /// <seealso cref="#extractTerms(Set)">
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
            {
                terms.Add(term, term);
            }
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
			return new AnonymousClassSpans(reader, this);
		}
	}
}