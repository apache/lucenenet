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
using Parameter = Lucene.Net.Util.Parameter;

namespace Lucene.Net.Search
{
	
	/// <summary>A clause in a BooleanQuery. </summary>
	[Serializable]
	public class BooleanClause
	{
		
		[Serializable]
		public sealed class Occur : Parameter
		{
			
			internal Occur(System.String name):base(name)
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
		/// <deprecated> use {@link #SetQuery(Query)} instead 
		/// </deprecated>
		public Query query; // TODO: decrease visibility for Lucene 2.0
		
		/// <summary>If true, documents documents which <i>do not</i>
		/// match this sub-query will <i>not</i> match the boolean query.
		/// </summary>
		/// <deprecated> use {@link #SetOccur(BooleanClause.Occur)} instead 
		/// </deprecated>
		public bool required = false; // TODO: decrease visibility for Lucene 2.0
		
		/// <summary>If true, documents documents which <i>do</i>
		/// match this sub-query will <i>not</i> match the boolean query.
		/// </summary>
		/// <deprecated> use {@link #SetOccur(BooleanClause.Occur)} instead 
		/// </deprecated>
		public bool prohibited = false; // TODO: decrease visibility for Lucene 2.0
		
		private Occur occur = Occur.SHOULD;
		
		/// <summary>Constructs a BooleanClause with query <code>q</code>, required
		/// <code>r</code> and prohibited <code>p</code>.
		/// </summary>
		/// <deprecated> use BooleanClause(Query, Occur) instead
		/// <ul>
		/// <li>For BooleanClause(query, true, false) use BooleanClause(query, BooleanClause.Occur.MUST)
		/// <li>For BooleanClause(query, false, false) use BooleanClause(query, BooleanClause.Occur.SHOULD)
		/// <li>For BooleanClause(query, false, true) use BooleanClause(query, BooleanClause.Occur.MUST_NOT)
		/// </ul>
		/// </deprecated>
		public BooleanClause(Query q, bool r, bool p)
		{
			// TODO: remove for Lucene 2.0
			query = q;
			required = r;
			prohibited = p;
			if (required)
			{
				if (prohibited)
				{
					// prohibited && required doesn't make sense, but we want the old behaviour:
					occur = Occur.MUST_NOT;
				}
				else
				{
					occur = Occur.MUST;
				}
			}
			else
			{
				if (prohibited)
				{
					occur = Occur.MUST_NOT;
				}
				else
				{
					occur = Occur.SHOULD;
				}
			}
		}
		
		/// <summary>Constructs a BooleanClause.</summary>
		public BooleanClause(Query query, Occur occur)
		{
			this.query = query;
			this.occur = occur;
			SetFields(occur);
		}
		
		public virtual Occur GetOccur()
		{
			return occur;
		}
		
		public virtual void  SetOccur(Occur occur)
		{
			this.occur = occur;
			SetFields(occur);
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
			return prohibited;
		}
		
		public virtual bool IsRequired()
		{
			return required;
		}
		
		private void  SetFields(Occur occur)
		{
			if (occur == Occur.MUST)
			{
				required = true;
				prohibited = false;
			}
			else if (occur == Occur.SHOULD)
			{
				required = false;
				prohibited = false;
			}
			else if (occur == Occur.MUST_NOT)
			{
				required = false;
				prohibited = true;
			}
			else
			{
				throw new System.ArgumentException("Unknown operator " + occur);
			}
		}
		
		/// <summary>Returns true iff <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (!(o is BooleanClause))
				return false;
			BooleanClause other = (BooleanClause) o;
			return this.query.Equals(other.query) && (this.required == other.required) && (this.prohibited == other.prohibited);
		}
		
		/// <summary>Returns a hash code value for this object.</summary>
		public override int GetHashCode()
		{
			return query.GetHashCode() ^ (this.required?1:0) ^ (this.prohibited?2:0);
		}
		
		
		public override System.String ToString()
		{
			return occur.ToString() + query.ToString();
		}
	}
}