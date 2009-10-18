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

namespace Lucene.Net.Search
{
	
	/// <summary>Implements the wildcard search query. Supported wildcards are <code>*</code>, which
	/// matches any character sequence (including the empty one), and <code>?</code>,
	/// which matches any single character. Note this query can be slow, as it
	/// needs to iterate over many terms. In order to prevent extremely slow WildcardQueries,
	/// a Wildcard term should not start with one of the wildcards <code>*</code> or
	/// <code>?</code>.
	/// 
	/// </summary>
	/// <seealso cref="WildcardTermEnum">
	/// </seealso>
	[Serializable]
	public class WildcardQuery : MultiTermQuery
	{
		private bool termContainsWildcard;
		
		public WildcardQuery(Term term) : base(term)
		{
			this.termContainsWildcard = (term.Text().IndexOf('*') != - 1) || (term.Text().IndexOf('?') != - 1);
		}
		
		protected internal override FilteredTermEnum GetEnum(IndexReader reader)
		{
			return new WildcardTermEnum(reader, GetTerm());
		}
		
		public  override bool Equals(System.Object o)
		{
			if (o is WildcardQuery)
				return base.Equals(o);
			
			return false;
		}
		
		public override Query Rewrite(IndexReader reader)
		{
			if (this.termContainsWildcard)
			{
				return base.Rewrite(reader);
			}
			
			return new TermQuery(GetTerm());
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}