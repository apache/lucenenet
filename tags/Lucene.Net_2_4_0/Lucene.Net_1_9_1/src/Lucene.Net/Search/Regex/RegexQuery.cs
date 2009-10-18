/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
using FilteredTermEnum = Lucene.Net.Search.FilteredTermEnum;
using MultiTermQuery = Lucene.Net.Search.MultiTermQuery;

namespace Lucene.Net.Search.Regex
{
	
	[Serializable]
	public class RegexQuery : MultiTermQuery
	{
		public RegexQuery(Term term) : base(term)
		{
		}
		
		protected internal override FilteredTermEnum GetEnum(IndexReader reader)
		{
			Term term = new Term(GetTerm().Field(), GetTerm().Text());
			return new RegexTermEnum(reader, term);
		}
		
		public  override bool Equals(System.Object o)
		{
			if (o is RegexQuery)
				return base.Equals(o);
			
			return false;
		}
		
        public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}