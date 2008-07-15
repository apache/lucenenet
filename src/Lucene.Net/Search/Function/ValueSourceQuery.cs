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
using Lucene.Net.Search;
using Searchable = Lucene.Net.Search.Searchable;

namespace Lucene.Net.Search.Function
{
	
	/// <summary> Expert: A Query that sets the scores of document to the
	/// values obtained from a {@link Lucene.Net.Search.Function.ValueSource ValueSource}.
	/// <p>   
	/// The value source can be based on a (cached) value of an indexd  field, but it
	/// can also be based on an external source, e.g. values read from an external database. 
	/// <p>
	/// Score is set as: Score(doc,query) = query.getBoost()<sup>2</sup> * valueSource(doc).  
	/// 
	/// <p><font color="#FF0000">
	/// WARNING: The status of the <b>search.function</b> package is experimental. 
	/// The APIs introduced here might change in the future and will not be 
	/// supported anymore in such a case.</font>
	/// 
	/// </summary>
	/// <author>  yonik
	/// </author>
	[Serializable]
	public class ValueSourceQuery : Query
	{
		internal ValueSource valSrc;

        // for testing
        public ValueSource ValSrc_ForNUnitTest
        {
            get { return valSrc; }
        }
		
		/// <summary> Create a value source query</summary>
		/// <param name="valSrc">provides the values defines the function to be used for scoring
		/// </param>
		public ValueSourceQuery(ValueSource valSrc)
		{
			this.valSrc = valSrc;
		}
		
		/*(non-Javadoc) @see Lucene.Net.Search.Query#rewrite(Lucene.Net.Index.IndexReader) */
		public override Query Rewrite(IndexReader reader)
		{
			return this;
		}
		
		/*(non-Javadoc) @see Lucene.Net.Search.Query#extractTerms(java.util.Set) */
		public override void  ExtractTerms(System.Collections.Hashtable terms)
		{
			// no terms involved here
		}
		
		[Serializable]
		private class ValueSourceWeight : Weight
		{
			private void  InitBlock(ValueSourceQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private ValueSourceQuery enclosingInstance;
			public ValueSourceQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal Similarity similarity;
			internal float queryNorm;
			internal float queryWeight;
			
			public ValueSourceWeight(ValueSourceQuery enclosingInstance, Searcher searcher)
			{
				InitBlock(enclosingInstance);
				this.similarity = Enclosing_Instance.GetSimilarity(searcher);
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Weight#getQuery() */
			public virtual Query GetQuery()
			{
				return Enclosing_Instance;
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Weight#getValue() */
			public virtual float GetValue()
			{
				return queryWeight;
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Weight#sumOfSquaredWeights() */
			public virtual float SumOfSquaredWeights()
			{
				queryWeight = Enclosing_Instance.GetBoost();
				return queryWeight * queryWeight;
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Weight#normalize(float) */
			public virtual void  Normalize(float norm)
			{
				this.queryNorm = norm;
				queryWeight *= this.queryNorm;
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Weight#scorer(Lucene.Net.Index.IndexReader) */
			public virtual Scorer Scorer(IndexReader reader)
			{
				return new ValueSourceScorer(enclosingInstance, similarity, reader, this);
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Weight#explain(Lucene.Net.Index.IndexReader, int) */
			public virtual Explanation Explain(IndexReader reader, int doc)
			{
				return Scorer(reader).Explain(doc);
			}
		}
		
		/// <summary> A scorer that (simply) matches all documents, and scores each document with 
		/// the value of the value soure in effect. As an example, if the value source 
		/// is a (cached) field source, then value of that field in that document will 
		/// be used. (assuming field is indexed for this doc, with a single token.)   
		/// </summary>
		private class ValueSourceScorer : Scorer
		{
			private void  InitBlock(ValueSourceQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private ValueSourceQuery enclosingInstance;
			public ValueSourceQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private IndexReader reader;
			private ValueSourceWeight weight;
			private int maxDoc;
			private float qWeight;
			private int doc = - 1;
			private DocValues vals;
			
			// constructor
			internal ValueSourceScorer(ValueSourceQuery enclosingInstance, Similarity similarity, IndexReader reader, ValueSourceWeight w) : base(similarity)
			{
				InitBlock(enclosingInstance);
				this.weight = w;
				this.qWeight = w.GetValue();
				this.reader = reader;
				this.maxDoc = reader.MaxDoc();
				// this is when/where the values are first created.
				vals = Enclosing_Instance.valSrc.GetValues(reader);
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Scorer#next() */
			public override bool Next()
			{
				for (; ; )
				{
					++doc;
					if (doc >= maxDoc)
					{
						return false;
					}
					if (reader.IsDeleted(doc))
					{
						continue;
					}
					return true;
				}
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Scorer#doc()
			*/
			public override int Doc()
			{
				return doc;
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Scorer#score() */
			public override float Score()
			{
				return qWeight * vals.FloatVal(doc);
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Scorer#skipTo(int) */
			public override bool SkipTo(int target)
			{
				doc = target - 1;
				return Next();
			}
			
			/*(non-Javadoc) @see Lucene.Net.Search.Scorer#explain(int) */
			public override Explanation Explain(int doc)
			{
				float sc = qWeight * vals.FloatVal(doc);
				
				Explanation result = new ComplexExplanation(true, sc, Enclosing_Instance.ToString() + ", product of:");
				
				result.AddDetail(vals.Explain(doc));
				result.AddDetail(new Explanation(Enclosing_Instance.GetBoost(), "boost"));
				result.AddDetail(new Explanation(weight.queryNorm, "queryNorm"));
				return result;
			}
		}
		
		/*(non-Javadoc) @see Lucene.Net.Search.Query#createWeight(Lucene.Net.Search.Searcher) */
		protected internal override Weight CreateWeight(Searcher searcher)
		{
			return new ValueSourceQuery.ValueSourceWeight(this, searcher);
		}
		
		/* (non-Javadoc) @see Lucene.Net.Search.Query#toString(java.lang.String) */
		public override System.String ToString(System.String field)
		{
			return valSrc.ToString() + ToStringUtils.Boost(GetBoost());
		}
		
		/// <summary>Returns true if <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (GetType() != o.GetType())
			{
				return false;
			}
			ValueSourceQuery other = (ValueSourceQuery) o;
			return this.GetBoost() == other.GetBoost() && this.valSrc.Equals(other.valSrc);
		}
		
		/// <summary>Returns a hash code value for this object. </summary>
		public override int GetHashCode()
		{
			return (GetType().GetHashCode() + valSrc.GetHashCode()) ^ BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0);
		}
		override public System.Object Clone()
		{
			return this.MemberwiseClone();
		}
	}
}