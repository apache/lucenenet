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
using Parameter = Lucene.Net.Util.Parameter;

namespace Lucene.Net.Search
{
	
	/// <summary>A clause in a BooleanQuery. </summary>
	[Serializable]
	public class BooleanClause
	{
		
        /// <summary>Specifies how terms may occur in matching documents. </summary>
        [Serializable]
		public sealed class Occur : Parameter
		{
			
			internal Occur(System.String name) : base(name)
			{
			}
			
			public override System.String ToString()
			{
				if (this == MUST)
					return "+";
				if (this == MUST_NOT)
					return "-";
				return "";
			}
			
			/// <summary>Use this operator for terms that <i>must</i> appear in the matching documents. </summary>
			public static readonly Occur MUST = new Occur("MUST");
			/// <summary>Use this operator for terms that <i>should</i> appear in the 
			/// matching documents. For a BooleanQuery with two <code>SHOULD</code> 
			/// subqueries, at least one of the queries must appear in the matching documents. 
			/// </summary>
			public static readonly Occur SHOULD = new Occur("SHOULD");
			/// <summary>Use this operator for terms that <i>must not</i> appear in the matching documents.
			/// Note that it is not possible to search for queries that only consist
			/// of a <code>MUST_NOT</code> query. 
			/// </summary>
			public static readonly Occur MUST_NOT = new Occur("MUST_NOT");
		}
		
		/// <summary>The query whose matching documents are combined by the boolean query.</summary>
		private Query query; // TODO: decrease visibility for Lucene 2.0
		
		private Occur occur = Occur.SHOULD;
		
		
		/// <summary>Constructs a BooleanClause.</summary>
		public BooleanClause(Query query, Occur occur)
		{
			this.query = query;
			this.occur = occur;
		}
		
        public virtual Occur GetOccur()
        {
            return occur;
        }
		
        public virtual void  SetOccur(Occur occur)
        {
            this.occur = occur;
        }
		
        public virtual Query GetQuery()
		{
			return query;
		}
		
		public virtual void  SetQuery(Query query)
		{
			this.query = query;
		}
		
        public virtual bool IsProhibited()
        {
            return Occur.MUST_NOT.Equals(occur);
        }
		
        public virtual bool IsRequired()
        {
            return Occur.MUST.Equals(occur);
        }
		
		
		
        /// <summary>Returns true iff <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (!(o is BooleanClause))
				return false;
			BooleanClause other = (BooleanClause) o;
			return this.query.Equals(other.query) && this.occur.Equals(other.occur);
		}
		
		/// <summary>Returns a hash code value for this object.</summary>
		public override int GetHashCode()
		{
			return query.GetHashCode() ^ (Occur.MUST.Equals(occur) ? 1 : 0) ^ (Occur.MUST_NOT.Equals(occur) ? 2 : 0);
		}
		
		
		public override System.String ToString()
		{
			return occur.ToString() + query.ToString();
		}
	}
}