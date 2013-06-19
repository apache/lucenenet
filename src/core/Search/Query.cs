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
using System.Linq;
using Lucene.Net.Index;
using IndexReader = Lucene.Net.Index.IndexReader;
using System.Collections.Generic;

namespace Lucene.Net.Search
{
	
	/// <summary>The abstract base class for queries.
	/// <p/>Instantiable subclasses are:
	/// <list type="bullet">
	/// <item> <see cref="TermQuery" /> </item>
    /// <item> <see cref="MultiTermQuery" /> </item>
    /// <item> <see cref="BooleanQuery" /> </item>
    /// <item> <see cref="WildcardQuery" /> </item>
    /// <item> <see cref="PhraseQuery" /> </item>
    /// <item> <see cref="PrefixQuery" /> </item>
    /// <item> <see cref="MultiPhraseQuery" /> </item>
    /// <item> <see cref="FuzzyQuery" /> </item>
    /// <item> <see cref="TermRangeQuery" /> </item>
    /// <item> <see cref="NumericRangeQuery{T}" /> </item>
    /// <item> <see cref="Lucene.Net.Search.Spans.SpanQuery" /> </item>
	/// </list>
	/// <p/>A parser for queries is contained in:
	/// <list type="bullet">
    /// <item><see cref="Lucene.Net.QueryParsers.QueryParser">QueryParser</see> </item>
	/// </list>
	/// </summary>
	[Serializable]
	public abstract class Query : ICloneable
	{
		private float boost = 1.0f; // query boost factor

	    /// <summary>Gets or sets the boost for this query clause to <c>b</c>.  Documents
	    /// matching this clause will (in addition to the normal weightings) have
	    /// their score multiplied by <c>b</c>.  The boost is 1.0 by default.
	    /// </summary>
	    public virtual float Boost
	    {
	        get { return boost; }
	        set { boost = value; }
	    }

	    /// <summary>Prints a query to a string, with <c>field</c> assumed to be the 
		/// default field and omitted.
		/// <p/>The representation used is one that is supposed to be readable
		/// by <see cref="Lucene.Net.QueryParsers.QueryParser">QueryParser</see>. However,
		/// there are the following limitations:
		/// <list type="bullet">
		/// <item>If the query was created by the parser, the printed
		/// representation may not be exactly what was parsed. For example,
		/// characters that need to be escaped will be represented without
		/// the required backslash.</item>
		/// <item>Some of the more complicated queries (e.g. span queries)
		/// don't have a representation that can be parsed by QueryParser.</item>
		/// </list>
		/// </summary>
		public abstract String ToString(String field);
		
		/// <summary>Prints a query to a string. </summary>
		public override String ToString()
		{
			return ToString("");
		}
		
		/// <summary> Expert: Constructs an appropriate Weight implementation for this query.
		/// 
		/// <p/>
		/// Only implemented by primitive queries, which re-write to themselves.
		/// </summary>
		public virtual Weight CreateWeight(IndexSearcher searcher)
		{
            throw new NotSupportedException("Query " + this + " does not implement createWeight");
		}
		
		/// <summary>Expert: called to re-write queries into primitive queries. For example,
		/// a PrefixQuery will be rewritten into a BooleanQuery that consists
		/// of TermQuerys.
		/// </summary>
		public virtual Query Rewrite(IndexReader reader)
		{
			return this;
		}
				
		/// <summary> Expert: adds all terms occuring in this query to the terms set. Only
		/// works if this query is in its <see cref="Rewrite">rewritten</see> form.
		/// 
		/// </summary>
		/// <throws>  UnsupportedOperationException if this query is not yet rewritten </throws>
		public virtual void ExtractTerms(ISet<Term> terms)
		{
			// needs to be implemented by query subclasses
			throw new NotSupportedException();
		}
				
		/// <summary>Returns a clone of this query. </summary>
		public virtual Object Clone()
		{
			try
			{
				return base.MemberwiseClone();
			}
			catch (Exception e)
			{
				throw new SystemException("Clone not supported: " + e.Message, e);
			}
		}
		
		public override int GetHashCode()
		{
			int prime = 31;
			int result = 1;
			result = prime * result + BitConverter.ToInt32(BitConverter.GetBytes(boost), 0);
			return result;
		}
		
		public  override bool Equals(Object obj)
		{
			if (this == obj)
				return true;
			if (obj == null)
				return false;
			if (GetType() != obj.GetType())
				return false;
			Query other = (Query) obj;
			if (BitConverter.ToInt32(BitConverter.GetBytes(boost), 0) != BitConverter.ToInt32(BitConverter.GetBytes(other.boost), 0))
				return false;
			return true;
		}
	}
}