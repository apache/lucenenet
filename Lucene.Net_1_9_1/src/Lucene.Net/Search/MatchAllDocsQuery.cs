/*
 * Copyright 2005 The Apache Software Foundation
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
		
		private class MatchAllScorer:Scorer
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
			internal int count;
			internal int maxDoc;
			
			internal MatchAllScorer(MatchAllDocsQuery enclosingInstance, IndexReader reader, Similarity similarity) : base(similarity)
			{
				InitBlock(enclosingInstance);
				this.reader = reader;
				count = - 1;
				maxDoc = reader.MaxDoc();
			}
			
			public override int Doc()
			{
				return count;
			}
			
			public override Explanation Explain(int doc)
			{
				Explanation explanation = new Explanation();
				explanation.SetValue(1.0f);
				explanation.SetDescription("MatchAllDocsQuery");
				return explanation;
			}
			
			public override bool Next()
			{
				while (count < (maxDoc - 1))
				{
					count++;
					if (!reader.IsDeleted(count))
					{
						return true;
					}
				}
				return false;
			}
			
			public override float Score()
			{
				return 1.0f;
			}
			
			public override bool SkipTo(int target)
			{
				count = target - 1;
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
			private Searcher searcher;
			
			public MatchAllDocsWeight(MatchAllDocsQuery enclosingInstance, Searcher searcher)
			{
				InitBlock(enclosingInstance);
				this.searcher = searcher;
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
				return 1.0f;
			}
			
			public virtual float SumOfSquaredWeights()
			{
				return 1.0f;
			}
			
			public virtual void  Normalize(float queryNorm)
			{
			}
			
			public virtual Scorer Scorer(IndexReader reader)
			{
				return new MatchAllScorer(enclosingInstance, reader, Enclosing_Instance.GetSimilarity(searcher));
			}
			
			public virtual Explanation Explain(IndexReader reader, int doc)
			{
				// explain query weight
				Explanation queryExpl = new Explanation();
				queryExpl.SetDescription("MatchAllDocsQuery:");
				
				Explanation boostExpl = new Explanation(Enclosing_Instance.GetBoost(), "boost");
				if (Enclosing_Instance.GetBoost() != 1.0f)
					queryExpl.AddDetail(boostExpl);
				queryExpl.SetValue(boostExpl.GetValue());
				
				return queryExpl;
			}
		}
		
		protected internal override Weight CreateWeight(Searcher searcher)
		{
			return new MatchAllDocsWeight(this, searcher);
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
			return BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0);
		}

        // {{Aroush-1.9}} Do we need this?!
        override public System.Object Clone()
		{
			return null;
		}
	}
}