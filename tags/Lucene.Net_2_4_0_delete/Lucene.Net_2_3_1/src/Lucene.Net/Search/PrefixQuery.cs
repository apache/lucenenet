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
	
	/// <summary>A Query that matches documents containing terms with a specified prefix. A PrefixQuery
	/// is built by QueryParser for input like <code>app*</code>. 
	/// </summary>
	[Serializable]
	public class PrefixQuery : Query
	{
		private Term prefix;
		
		/// <summary>Constructs a query for terms starting with <code>prefix</code>. </summary>
		public PrefixQuery(Term prefix)
		{
			this.prefix = prefix;
		}
		
		/// <summary>Returns the prefix of this query. </summary>
		public virtual Term GetPrefix()
		{
			return prefix;
		}
		
		public override Query Rewrite(IndexReader reader)
		{
			BooleanQuery query = new BooleanQuery(true);
			TermEnum enumerator = reader.Terms(prefix);
			try
			{
				System.String prefixText = prefix.Text();
				System.String prefixField = prefix.Field();
				do 
				{
					Term term = enumerator.Term();
#if !FRAMEWORK_1_1
                    if (term != null && term.Text().StartsWith(prefixText, StringComparison.Ordinal) && term.Field() == prefixField)
#else
                    if (term != null && term.Text().StartsWith(prefixText) && term.Field() == prefixField)
#endif
					// interned comparison 
                    {
						TermQuery tq = new TermQuery(term); // found a match
						tq.SetBoost(GetBoost()); // set the boost
						query.Add(tq, BooleanClause.Occur.SHOULD); // add to query
						//System.out.println("added " + term);
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
		
		/// <summary>Prints a user-readable version of this query. </summary>
		public override System.String ToString(System.String field)
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			if (!prefix.Field().Equals(field))
			{
				buffer.Append(prefix.Field());
				buffer.Append(":");
			}
			buffer.Append(prefix.Text());
			buffer.Append('*');
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}
		
		/// <summary>Returns true iff <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (!(o is PrefixQuery))
				return false;
			PrefixQuery other = (PrefixQuery) o;
			return (this.GetBoost() == other.GetBoost()) && this.prefix.Equals(other.prefix);
		}
		
		/// <summary>Returns a hash code value for this object.</summary>
		public override int GetHashCode()
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0) ^ prefix.GetHashCode() ^ 0x6634D93C;
		}
	}
}