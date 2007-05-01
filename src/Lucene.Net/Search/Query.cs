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

namespace Lucene.Net.Search
{
	
	/// <summary>The abstract base class for queries.
	/// <p>Instantiable subclasses are:
	/// <ul>
	/// <li> {@link TermQuery}
	/// <li> {@link MultiTermQuery}
	/// <li> {@link BooleanQuery}
	/// <li> {@link WildcardQuery}
	/// <li> {@link PhraseQuery}
	/// <li> {@link PrefixQuery}
	/// <li> {@link MultiPhraseQuery}
	/// <li> {@link FuzzyQuery}
	/// <li> {@link RangeQuery}
	/// <li> {@link Lucene.Net.Search.Spans.SpanQuery}
	/// </ul>
	/// <p>A parser for queries is contained in:
	/// <ul>
	/// <li>{@link Lucene.Net.QueryParsers.QueryParser QueryParser}
	/// </ul>
	/// </summary>
	[Serializable]
	public abstract class Query : System.ICloneable
	{
		private float boost = 1.0f; // query boost factor
		
		/// <summary>Sets the boost for this query clause to <code>b</code>.  Documents
		/// matching this clause will (in addition to the normal weightings) have
		/// their score multiplied by <code>b</code>.
		/// </summary>
		public virtual void  SetBoost(float b)
		{
			boost = b;
		}
		
		/// <summary>Gets the boost for this clause.  Documents matching
		/// this clause will (in addition to the normal weightings) have their score
		/// multiplied by <code>b</code>.   The boost is 1.0 by default.
		/// </summary>
		public virtual float GetBoost()
		{
			return boost;
		}
		
		/// <summary>Prints a query to a string, with <code>field</code> assumed to be the 
		/// default field and omitted.
		/// <p>The representation used is one that is supposed to be readable
		/// by {@link Lucene.Net.QueryParsers.QueryParser QueryParser}. However,
		/// there are the following limitations:
		/// <ul>
		/// <li>If the query was created by the parser, the printed
		/// representation may not be exactly what was parsed. For example,
		/// characters that need to be escaped will be represented without
		/// the required backslash.</li>
		/// <li>Some of the more complicated queries (e.g. span queries)
		/// don't have a representation that can be parsed by QueryParser.</li>
		/// </ul>
		/// </summary>
		public abstract System.String ToString(System.String field);
		
		/// <summary>Prints a query to a string. </summary>
		public override System.String ToString()
		{
			return ToString("");
		}
		
		/// <summary>Expert: Constructs an appropriate Weight implementation for this query.
		/// 
		/// <p>Only implemented by primitive queries, which re-write to themselves.
		/// </summary>
		protected internal virtual Weight CreateWeight(Searcher searcher)
		{
			throw new System.NotSupportedException();
		}
		
		/// <summary>Expert: Constructs and initializes a Weight for a top-level query. </summary>
		public virtual Weight Weight(Searcher searcher)
		{
			Query query = searcher.Rewrite(this);
			Weight weight = query.CreateWeight(searcher);
			float sum = weight.SumOfSquaredWeights();
			float norm = GetSimilarity(searcher).QueryNorm(sum);
			weight.Normalize(norm);
			return weight;
		}
		
		/// <summary>Expert: called to re-write queries into primitive queries. For example,
		/// a PrefixQuery will be rewritten into a BooleanQuery that consists
		/// of TermQuerys.
		/// </summary>
		public virtual Query Rewrite(IndexReader reader)
		{
			return this;
		}
		
		/// <summary>Expert: called when re-writing queries under MultiSearcher.
		/// 
		/// Create a single query suitable for use by all subsearchers (in 1-1
		/// correspondence with queries). This is an optimization of the OR of
		/// all queries. We handle the common optimization cases of equal
		/// queries and overlapping clauses of boolean OR queries (as generated
		/// by MultiTermQuery.rewrite() and RangeQuery.rewrite()).
		/// Be careful overriding this method as queries[0] determines which
		/// method will be called and is not necessarily of the same type as
		/// the other queries.
		/// </summary>
		public virtual Query Combine(Query[] queries)
		{
			System.Collections.Hashtable uniques = new System.Collections.Hashtable();
			for (int i = 0; i < queries.Length; i++)
			{
				Query query = queries[i];
				BooleanClause[] clauses = null;
				// check if we can split the query into clauses
				bool splittable = (query is BooleanQuery);
				if (splittable)
				{
					BooleanQuery bq = (BooleanQuery) query;
					splittable = bq.IsCoordDisabled();
					clauses = bq.GetClauses();
					for (int j = 0; splittable && j < clauses.Length; j++)
					{
						splittable = (clauses[j].GetOccur() == BooleanClause.Occur.SHOULD);
					}
				}
				if (splittable)
				{
					for (int j = 0; j < clauses.Length; j++)
					{
                        Query tmp = clauses[j].GetQuery();
                        if (uniques.Contains(tmp) == false)
                        {
                            uniques.Add(tmp, tmp);
                        }
					}
				}
				else
				{
                    if (uniques.Contains(query) == false)
                    {
                        uniques.Add(query, query);
                    }
				}
			}
			// optimization: if we have just one query, just return it
			if (uniques.Count == 1)
			{
                System.Collections.IDictionaryEnumerator iter = uniques.GetEnumerator();
                iter.MoveNext();
                return iter.Value as Query;
			}
			System.Collections.IDictionaryEnumerator it = uniques.GetEnumerator();
			BooleanQuery result = new BooleanQuery(true);
			while (it.MoveNext())
			{
				result.Add((Query) it.Value, BooleanClause.Occur.SHOULD);
			}
			return result;
		}
		
		/// <summary> Expert: adds all terms occuring in this query to the terms set. Only
		/// works if this query is in its {@link #rewrite rewritten} form.
		/// 
		/// </summary>
		/// <throws>  UnsupportedOperationException if this query is not yet rewritten </throws>
		public virtual void  ExtractTerms(System.Collections.Hashtable terms)
		{
			// needs to be implemented by query subclasses
			throw new System.NotSupportedException();
		}
		
		
		/// <summary>Expert: merges the clauses of a set of BooleanQuery's into a single
		/// BooleanQuery.
		/// 
		/// <p>A utility for use by {@link #Combine(Query[])} implementations.
		/// </summary>
		public static Query MergeBooleanQueries(Query[] queries)
		{
			System.Collections.Hashtable allClauses = new System.Collections.Hashtable();
			for (int i = 0; i < queries.Length; i++)
			{
				BooleanClause[] clauses = ((BooleanQuery) queries[i]).GetClauses();
				for (int j = 0; j < clauses.Length; j++)
				{
					allClauses.Add(clauses[j], clauses[j]);
				}
			}
			
			bool coordDisabled = queries.Length == 0 ? false : ((BooleanQuery) queries[0]).IsCoordDisabled();
			BooleanQuery result = new BooleanQuery(coordDisabled);
            foreach (BooleanClause booleanClause in allClauses.Keys)
            {
                result.Add(booleanClause);
            }
			return result;
		}
		
		/// <summary>Expert: Returns the Similarity implementation to be used for this query.
		/// Subclasses may override this method to specify their own Similarity
		/// implementation, perhaps one that delegates through that of the Searcher.
		/// By default the Searcher's Similarity implementation is returned.
		/// </summary>
		public virtual Similarity GetSimilarity(Searcher searcher)
		{
			return searcher.GetSimilarity();
		}
		
		/// <summary>Returns a clone of this query. </summary>
		public virtual System.Object Clone()
		{
			try
			{
				return (Query) base.MemberwiseClone();
			}
			catch (System.Exception e)
			{
				throw new System.SystemException("Clone not supported: " + e.Message);
			}
		}
	}
}