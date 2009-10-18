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
	
	/// <summary> A query that matches all documents.
	/// 
	/// </summary>
	/// <author>  John Wang
	/// </author>
	[Serializable]
	public class MatchAllDocsQuery : Query
	{
		
		public MatchAllDocsQuery()
		{
		}
		
		private class MatchAllScorer : Scorer
		{
			private void  InitBlock(MatchAllDocsQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private MatchAllDocsQuery enclosingInstance;
			public MatchAllDocsQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			internal IndexReader reader;
			internal int id;
			internal int maxId;
			internal float score;
			
			internal MatchAllScorer(MatchAllDocsQuery enclosingInstance, IndexReader reader, Similarity similarity, Weight w):base(similarity)
			{
				InitBlock(enclosingInstance);
				this.reader = reader;
				id = - 1;
				maxId = reader.MaxDoc() - 1;
				score = w.GetValue();
			}
			
			public override Explanation Explain(int doc)
			{
				return null; // not called... see MatchAllDocsWeight.explain()
			}
			
			public override int Doc()
			{
				return id;
			}
			
			public override bool Next()
			{
				while (id < maxId)
				{
					id++;
					if (!reader.IsDeleted(id))
					{
						return true;
					}
				}
				return false;
			}
			
			public override float Score()
			{
				return score;
			}
			
			public override bool SkipTo(int target)
			{
				id = target - 1;
				return Next();
			}
		}
		
		[Serializable]
		private class MatchAllDocsWeight : Weight
		{
			private void  InitBlock(MatchAllDocsQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private MatchAllDocsQuery enclosingInstance;
			public MatchAllDocsQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private Similarity similarity;
			private float queryWeight;
			private float queryNorm;
			
			public MatchAllDocsWeight(MatchAllDocsQuery enclosingInstance, Searcher searcher)
			{
				InitBlock(enclosingInstance);
				this.similarity = searcher.GetSimilarity();
			}
			
			public override System.String ToString()
			{
				return "weight(" + Enclosing_Instance + ")";
			}
			
			public virtual Query GetQuery()
			{
				return Enclosing_Instance;
			}
			
			public virtual float GetValue()
			{
				return queryWeight;
			}
			
			public virtual float SumOfSquaredWeights()
			{
				queryWeight = Enclosing_Instance.GetBoost();
				return queryWeight * queryWeight;
			}
			
			public virtual void  Normalize(float queryNorm)
			{
				this.queryNorm = queryNorm;
				queryWeight *= this.queryNorm;
			}
			
			public virtual Scorer Scorer(IndexReader reader)
			{
				return new MatchAllScorer(enclosingInstance, reader, similarity, this);
			}
			
			public virtual Explanation Explain(IndexReader reader, int doc)
			{
				// explain query weight
				Explanation queryExpl = new ComplexExplanation(true, GetValue(), "MatchAllDocsQuery, product of:");
				if (Enclosing_Instance.GetBoost() != 1.0f)
				{
					queryExpl.AddDetail(new Explanation(Enclosing_Instance.GetBoost(), "boost"));
				}
				queryExpl.AddDetail(new Explanation(queryNorm, "queryNorm"));
				
				return queryExpl;
			}
		}
		
		protected internal override Weight CreateWeight(Searcher searcher)
		{
			return new MatchAllDocsWeight(this, searcher);
		}
		
		public override void  ExtractTerms(System.Collections.Hashtable terms)
		{
		}
		
		public override System.String ToString(System.String field)
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			buffer.Append("MatchAllDocsQuery");
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}
		
		public  override bool Equals(System.Object o)
		{
			if (!(o is MatchAllDocsQuery))
				return false;
			MatchAllDocsQuery other = (MatchAllDocsQuery) o;
			return this.GetBoost() == other.GetBoost();
		}
		
		public override int GetHashCode()
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0) ^ 0x1AA71190;
		}
	}
}