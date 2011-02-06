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
using Term = Lucene.Net.Index.Term;
using TermPositions = Lucene.Net.Index.TermPositions;
using Lucene.Net.Search;
using Searchable = Lucene.Net.Search.Searchable;
using SpanScorer = Lucene.Net.Search.Spans.SpanScorer;
using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
using SpanWeight = Lucene.Net.Search.Spans.SpanWeight;
using TermSpans = Lucene.Net.Search.Spans.TermSpans;

namespace Lucene.Net.Search.Payloads
{
	
	/// <summary> The BoostingTermQuery is very similar to the {@link Lucene.Net.Search.Spans.SpanTermQuery} except
	/// that it factors in the value of the payload located at each of the positions where the
	/// {@link Lucene.Net.Index.Term} occurs.
	/// <p>
	/// In order to take advantage of this, you must override {@link Lucene.Net.Search.Similarity#ScorePayload(String, byte[],int,int)}
	/// which returns 1 by default.
	/// <p>
	/// Payload scores are averaged across term occurrences in the document.  
	/// 
	/// </summary>
	/// <seealso cref="Lucene.Net.Search.Similarity.ScorePayload(String, byte[], int, int)">
	/// </seealso>
	[Serializable]
	public class BoostingTermQuery : SpanTermQuery
	{
		
		
		public BoostingTermQuery(Term term) : base(term)
		{
		}
		
		
		protected internal override Weight CreateWeight(Searcher searcher)
		{
			return new BoostingTermWeight(this, this, searcher);
		}
		
		[Serializable]
		protected internal class BoostingTermWeight : SpanWeight, Weight
		{
			private void  InitBlock(BoostingTermQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private BoostingTermQuery enclosingInstance;
			public BoostingTermQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			
			public BoostingTermWeight(BoostingTermQuery enclosingInstance, BoostingTermQuery query, Searcher searcher) : base(query, searcher)
			{
				InitBlock(enclosingInstance);
			}
			
			
			
			
			public override Scorer Scorer(IndexReader reader)
			{
				return new BoostingSpanScorer(this, (TermSpans) query.GetSpans(reader), this, similarity, reader.Norms(query.GetField()));
			}
			
			internal class BoostingSpanScorer : SpanScorer
			{
				private void  InitBlock(BoostingTermWeight enclosingInstance)
				{
					this.enclosingInstance = enclosingInstance;
				}
				private BoostingTermWeight enclosingInstance;
				public BoostingTermWeight Enclosing_Instance
				{
					get
					{
						return enclosingInstance;
					}
					
				}
				
				//TODO: is this the best way to allocate this?
				internal byte[] payload = new byte[256];
				private TermPositions positions;
				protected internal float payloadScore;
				private int payloadsSeen;
				
				public BoostingSpanScorer(BoostingTermWeight enclosingInstance, TermSpans spans, Weight weight, Similarity similarity, byte[] norms) : base(spans, weight, similarity, norms)
				{
					InitBlock(enclosingInstance);
					positions = spans.GetPositions();
				}
				
				protected internal override bool SetFreqCurrentDoc()
				{
					if (!more)
					{
						return false;
					}
					doc = spans.Doc();
					freq = 0.0f;
					payloadScore = 0;
					payloadsSeen = 0;
					Similarity similarity1 = GetSimilarity();
					while (more && doc == spans.Doc())
					{
						int matchLength = spans.End() - spans.Start();
						
						freq += similarity1.SloppyFreq(matchLength);
						ProcessPayload(similarity1);
						
						more = spans.Next(); //this moves positions to the next match in this document
					}
					return more || (freq != 0);
				}
				
				
				protected internal virtual void  ProcessPayload(Similarity similarity)
				{
					if (positions.IsPayloadAvailable())
					{
						payload = positions.GetPayload(payload, 0);
						payloadScore += similarity.ScorePayload(Enclosing_Instance.Enclosing_Instance.term.Field(), payload, 0, positions.GetPayloadLength());
						payloadsSeen++;
					}
					else
					{
						//zero out the payload?
					}
				}
				
				public override float Score()
				{
					
					return base.Score() * (payloadsSeen > 0 ? (payloadScore / payloadsSeen) : 1);
				}
				
				
				public override Explanation Explain(int doc)
				{
					Explanation result = new Explanation();
					Explanation nonPayloadExpl = base.Explain(doc);
					result.AddDetail(nonPayloadExpl);
					//QUESTION: Is there a wau to avoid this skipTo call?  We need to know whether to load the payload or not
					
					Explanation payloadBoost = new Explanation();
					result.AddDetail(payloadBoost);
					/*
					if (skipTo(doc) == true) {
					processPayload();
					}*/
					
					float avgPayloadScore = (payloadsSeen > 0 ? (payloadScore / payloadsSeen) : 1);
					payloadBoost.SetValue(avgPayloadScore);
					//GSI: I suppose we could toString the payload, but I don't think that would be a good idea 
					payloadBoost.SetDescription("scorePayload(...)");
					result.SetValue(nonPayloadExpl.GetValue() * avgPayloadScore);
					result.SetDescription("btq, product of:");
					return result;
				}
			}
		}
		
		
		public  override bool Equals(System.Object o)
		{
			if (!(o is BoostingTermQuery))
				return false;
			BoostingTermQuery other = (BoostingTermQuery) o;
			return (this.GetBoost() == other.GetBoost()) && this.term.Equals(other.term);
		}
		
		public override int GetHashCode()   // {{Aroush-2.3.1}} Do we need this methods?
		{
			return base.GetHashCode();
		}
	}
}