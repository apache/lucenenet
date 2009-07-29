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
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search
{
	
	
	/// <summary>
    /// A query that applies a filter to the results of another query.
	/// <para>
    /// Note: the bits are retrieved from the filter each time this
	/// query is used in a search - use a CachingWrapperFilter to avoid
	/// regenerating the bits every time.
	/// </para>
	/// </summary>
	/// <since>1.4</since>
    /// <version>$Id:$</version>
	/// <seealso cref="CachingWrapperFilter"/>
	[Serializable]
	public class FilteredQuery : Query
	{		
		internal Query query;
		internal Filter filter;
		
		/// <summary> Constructs a new query which applies a filter to the results of the original query.
		/// Filter.GetDocIdSet() will be called every time this query is used in a search.
		/// </summary>
		/// <param name="query"> Query to be filtered, cannot be <code>null</code>.
		/// </param>
		/// <param name="filter">Filter to apply to query results, cannot be <code>null</code>.
		/// </param>
		public FilteredQuery(Query query, Filter filter)
		{
			this.query = query;
			this.filter = filter;
		}
		
		
		
		/// <summary> Returns a Weight that applies the filter to the enclosed query's Weight.
		/// This is accomplished by overriding the Scorer returned by the Weight.
		/// </summary>
		protected internal override Weight CreateWeight(Searcher searcher)
		{
			Weight weight = query.CreateWeight(searcher);
			Similarity similarity = query.GetSimilarity(searcher);
			return new AnonymousClassWeight(weight, similarity, this);
		}

        [Serializable]
        private class AnonymousClassWeight : Weight
        {
            private Lucene.Net.Search.Weight weight;
            private Lucene.Net.Search.Similarity similarity;
            private FilteredQuery enclosingInstance;
            private float value_Renamed;

            public FilteredQuery Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
            }

            public AnonymousClassWeight(Lucene.Net.Search.Weight weight, Lucene.Net.Search.Similarity similarity, FilteredQuery enclosingInstance)
            {
                this.weight = weight;
                this.similarity = similarity;
                this.enclosingInstance = enclosingInstance;
            }

            // pass these methods through to enclosed query's weight
            public virtual float GetValue()
            {
                return value_Renamed;
            }

            public virtual float SumOfSquaredWeights()
            {
                return weight.SumOfSquaredWeights() * Enclosing_Instance.GetBoost() * Enclosing_Instance.GetBoost();
            }
            
            public virtual void Normalize(float v)
            {
                weight.Normalize(v);
                value_Renamed = weight.GetValue() * Enclosing_Instance.GetBoost();
            }
            
            public virtual Explanation Explain(IndexReader ir, int i)
            {
                Explanation inner = weight.Explain(ir, i);
                if (Enclosing_Instance.GetBoost() != 1)
                {
                    Explanation preBoost = inner;
                    inner = new Explanation(inner.GetValue() * Enclosing_Instance.GetBoost(), "product of:");
                    inner.AddDetail(new Explanation(Enclosing_Instance.GetBoost(), "boost"));
                    inner.AddDetail(preBoost);
                }
                Filter f = Enclosing_Instance.filter;
                DocIdSetIterator docIdSetIterator = f.GetDocIdSet(ir).Iterator();
                if (docIdSetIterator.SkipTo(i) && docIdSetIterator.Doc() == i)
                {
                    return inner;
                }
                else
                {
                    Explanation result = new Explanation(0.0f, "failure to match filter: " + f.ToString());
                    result.AddDetail(inner);
                    return result;
                }
            }

            // return this query
            public virtual Query GetQuery()
            {
                return Enclosing_Instance;
            }

            // return a filtering scorer
            public virtual Scorer Scorer(IndexReader indexReader)
            {
                Scorer scorer = weight.Scorer(indexReader);
                DocIdSetIterator docIdSetIterator = Enclosing_Instance.filter.GetDocIdSet(indexReader).Iterator();
                return new AnonymousClassScorer(docIdSetIterator, scorer, this, similarity);
            }

            private class AnonymousClassScorer : Scorer
            {
                private DocIdSetIterator docIdSetIterator;
                private Lucene.Net.Search.Scorer scorer;
                private AnonymousClassWeight enclosingInstance;

                internal AnonymousClassScorer(DocIdSetIterator docIdSetIterator, Lucene.Net.Search.Scorer scorer, AnonymousClassWeight enclosingInstance, Lucene.Net.Search.Similarity similarity)
                    : base(similarity)
                {
                    this.docIdSetIterator = docIdSetIterator;
                    this.scorer = scorer;
                    this.enclosingInstance = enclosingInstance;
                }

                public AnonymousClassWeight Enclosing_Instance
                {
                    get
                    {
                        return enclosingInstance;
                    }
                }

                private bool AdvanceToCommon()
                {
                    while (scorer.Doc() != docIdSetIterator.Doc())
                    {
                        if (scorer.Doc() < docIdSetIterator.Doc())
                        {
                            if (!scorer.SkipTo(docIdSetIterator.Doc()))
                            {
                                return false;
                            }
                        }
                        else if (!docIdSetIterator.SkipTo(scorer.Doc()))
                        {
                            return false;
                        }
                    }
                    return true;
                }

                public override bool Next()
                {
                    return docIdSetIterator.Next() && scorer.Next() && AdvanceToCommon();
                }

                public override int Doc()
                {
                    return scorer.Doc();
                }

                public override bool SkipTo(int i)
                {
                    return docIdSetIterator.SkipTo(i) && scorer.SkipTo(docIdSetIterator.Doc()) && AdvanceToCommon();
                }

                public override float Score()
                {
                    return Enclosing_Instance.Enclosing_Instance.GetBoost() * scorer.Score();
                }

                // add an explanation about whether the document was filtered
                public override Explanation Explain(int i)
                {
                    Explanation exp = scorer.Explain(i);
                    if (docIdSetIterator.SkipTo(i) && docIdSetIterator.Doc() == i)
                    {
                        exp.SetDescription("allowed by filter: " + exp.GetDescription());
                        exp.SetValue(Enclosing_Instance.Enclosing_Instance.GetBoost() * exp.GetValue());
                    }
                    else
                    {
                        exp.SetDescription("removed by filter: " + exp.GetDescription());
                        exp.SetValue(0.0F);
                    }
                    return exp;
                }
            }
        }
        
        /// <summary>Rewrites the wrapped query. </summary>
		public override Query Rewrite(IndexReader reader)
		{
			Query rewritten = query.Rewrite(reader);
			if (rewritten != query)
			{
				FilteredQuery clone = (FilteredQuery) this.Clone();
				clone.query = rewritten;
				return clone;
			}
			else
			{
				return this;
			}
		}
		
		public virtual Query GetQuery()
		{
			return query;
		}
		
		public virtual Filter GetFilter()
		{
			return filter;
		}
		
		// inherit javadoc
		public override void  ExtractTerms(System.Collections.Hashtable terms)
		{
			GetQuery().ExtractTerms(terms);
		}
		
		/// <summary>Prints a user-readable version of this query. </summary>
		public override System.String ToString(System.String s)
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			buffer.Append("filtered(");
			buffer.Append(query.ToString(s));
			buffer.Append(")->");
			buffer.Append(filter);
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}
		
		/// <summary>Returns true iff <code>o</code> is equal to this. </summary>
		public  override bool Equals(object o)
		{
			if (o is FilteredQuery)
			{
				FilteredQuery fq = (FilteredQuery) o;
				return (query.Equals(fq.query) && filter.Equals(fq.filter) && GetBoost() == fq.GetBoost());
			}
			return false;
		}
		
		/// <summary>Returns a hash code value for this object. </summary>
		public override int GetHashCode()
		{
			return query.GetHashCode() ^ filter.GetHashCode() + System.Convert.ToInt32(GetBoost());
		}

		override public object Clone()
		{
            // {{Aroush-2.0}} is this Clone() OK?
            FilteredQuery clone = (FilteredQuery) base.Clone();
            clone.filter = this.filter;
            clone.query = this.query;
            return clone;
        }
	}
}