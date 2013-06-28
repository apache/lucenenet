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
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{
	
	/// <summary> Constrains search results to only match those which also match a provided
	/// query.  
	/// 
	/// <p/> This could be used, for example, with a <see cref="TermRangeQuery" /> on a suitably
	/// formatted date field to implement date filtering.  One could re-use a single
	/// QueryFilter that matches, e.g., only documents modified within the last
	/// week.  The QueryFilter and TermRangeQuery would only need to be reconstructed
	/// once per day.
	/// 
	/// </summary>
	/// <version>  $Id:$
	/// </version>
	[Serializable]
	public class QueryWrapperFilter:Filter
	{
		private class AnonymousClassDocIdSet:DocIdSet
		{
			public AnonymousClassDocIdSet(Weight weight, AtomicReaderContext privateContext, Bits acceptDocs)
			{
			    this.weight = weight;
			    this.privateContext = privateContext;
			    this.acceptDocs = acceptDocs;
			}

		    private Weight weight;
		    private readonly AtomicReaderContext privateContext;
		    private readonly Bits acceptDocs;
			public override DocIdSetIterator Iterator()
			{
				return weight.Scorer(privateContext, true, false, acceptDocs);
			}

		    public override bool IsCacheable
		    {
		        get { return false; }
		    }
		}
		private Query query;
        public Query Query { get { return query; } }
		
		/// <summary>Constructs a filter which only matches documents matching
		/// <c>query</c>.
		/// </summary>
		public QueryWrapperFilter(Query query)
		{
		    if (query == null) throw new ArgumentNullException("query");
			this.query = query;
		}
		
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		{
		    var privateContext = context.Reader.Context;
		    var weight = new IndexSearcher(privateContext).CreateNormalizedWeight(query);
            return new AnonymousClassDocIdSet(this);
		}
		
		public override string ToString()
		{
			return "QueryWrapperFilter(" + query + ")";
		}
		
		public override bool Equals(object o)
		{
			if (!(o is QueryWrapperFilter))
				return false;
			return query.Equals(((QueryWrapperFilter) o).query);
		}
		
		public override int GetHashCode()
		{
			return query.GetHashCode() ^ unchecked((int) 0x923F64B9);
		}
	}
}