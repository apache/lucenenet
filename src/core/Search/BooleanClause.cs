namespace Lucene.Net.Search
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	/// <summary>
	/// A clause in a BooleanQuery. </summary>
	public class BooleanClause
	{

	  /// <summary>
	  /// Specifies how clauses are to occur in matching documents. </summary>
	  public enum Occur
	  {

		/// <summary>
		/// Use this operator for clauses that <i>must</i> appear in the matching documents. </summary>
		MUST 
        { 
            public string toString() 
            { 
                return "+";
	        }
	    },

		/// <summary>
		/// Use this operator for clauses that <i>should</i> appear in the 
		/// matching documents. For a BooleanQuery with no <code>MUST</code> 
		/// clauses one or more <code>SHOULD</code> clauses must match a document 
		/// for the BooleanQuery to match. </summary>
		/// <seealso cref= BooleanQuery#setMinimumNumberShouldMatch </seealso>
		SHOULD
		{
			public string ToString()
			{
				return "";
			}
		},

		/// <summary>
		/// Use this operator for clauses that <i>must not</i> appear in the matching documents.
		/// Note that it is not possible to search for queries that only consist
		/// of a <code>MUST_NOT</code> clause. 
		/// </summary>
		MUST_NOT
		{
			public string ToString()
			{
				return "-";
			}
		}

      }

	  /// <summary>
	  /// The query whose matching documents are combined by the boolean query.
	  /// </summary>
	  private Query query;

	  private Occur occur;


	  /// <summary>
	  /// Constructs a BooleanClause.
	  /// </summary>
	  public BooleanClause(Query query, Occur occur)
	  {
		this.query = query;
		this.occur = occur;

	  }

	  public Occur Occur
	  {
		return occur;
	  }

	  public void setOccur(Occur occur)
	  {
		this.occur = occur;

	  }

	  public Query Query
	  {
		return query;
	  }

	  public void setQuery(Query query)
	  {
		this.query = query;
	  }

	  public bool Prohibited
	  {
		return Occur.MUST_NOT == occur;
	  }

	  public bool Required
	  {
		return Occur.MUST == occur;
	  }



	  /// <summary>
	  /// Returns true if <code>o</code> is equal to this. </summary>
	  public bool Equals(object o)
	  {
		if (o == null || !(o is BooleanClause))
		{
		  return false;
		}
		BooleanClause other = (BooleanClause)o;
		return this.query.Equals(other.query) && this.occur == other.occur;
	  }

	  /// <summary>
	  /// Returns a hash code value for this object. </summary>
	  public int GetHashCode()
	  {
		return query.HashCode() ^ (Occur.MUST == occur?1:0) ^ (Occur.MUST_NOT == occur?2:0);
	  }


	  public string ToString()
	  {
		return occur.ToString() + query.ToString();
	  }
	}

}