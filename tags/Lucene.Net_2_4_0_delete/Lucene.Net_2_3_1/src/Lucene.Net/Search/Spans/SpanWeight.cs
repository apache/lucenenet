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
using Lucene.Net.Search;
using Searchable = Lucene.Net.Search.Searchable;

namespace Lucene.Net.Search.Spans
{
	
	/// <summary> Expert-only.  Public for use by other weight implementations</summary>
	[Serializable]
	public class SpanWeight : Weight
	{
		protected internal Similarity similarity;
		protected internal float value_Renamed;
		protected internal float idf;
		protected internal float queryNorm;
		protected internal float queryWeight;
		
		protected internal System.Collections.Hashtable terms;
		protected internal SpanQuery query;
		
		public SpanWeight(SpanQuery query, Searcher searcher)
		{
			this.similarity = query.GetSimilarity(searcher);
			this.query = query;
			terms = new System.Collections.Hashtable();
			query.ExtractTerms(terms);
			
			System.Collections.ArrayList tmp = new System.Collections.ArrayList(terms.Values);
			
			idf = this.query.GetSimilarity(searcher).Idf(tmp, searcher);
		}
		
		public virtual Query GetQuery()
		{
			return query;
		}
		public virtual float GetValue()
		{
			return value_Renamed;
		}
		
		public virtual float SumOfSquaredWeights()
		{
			queryWeight = idf * query.GetBoost(); // compute query weight
			return queryWeight * queryWeight; // square it
		}
		
		public virtual void  Normalize(float queryNorm)
		{
			this.queryNorm = queryNorm;
			queryWeight *= queryNorm; // normalize query weight
			value_Renamed = queryWeight * idf; // idf for document
		}
		
		public virtual Scorer Scorer(IndexReader reader)
		{
			return new SpanScorer(query.GetSpans(reader), this, similarity, reader.Norms(query.GetField()));
		}
		
		public virtual Explanation Explain(IndexReader reader, int doc)
		{
			
			ComplexExplanation result = new ComplexExplanation();
			result.SetDescription("weight(" + GetQuery() + " in " + doc + "), product of:");
			System.String field = ((SpanQuery) GetQuery()).GetField();
			
			System.Text.StringBuilder docFreqs = new System.Text.StringBuilder();
			System.Collections.IEnumerator i = terms.GetEnumerator();
			while (i.MoveNext())
			{
				System.Collections.DictionaryEntry tmp = (System.Collections.DictionaryEntry) i.Current;
				Term term = (Term) tmp.Key;
				docFreqs.Append(term.Text());
				docFreqs.Append("=");
				docFreqs.Append(reader.DocFreq(term));
				
				if (i.MoveNext())
				{
					docFreqs.Append(" ");
				}
			}
			
			Explanation idfExpl = new Explanation(idf, "idf(" + field + ": " + docFreqs + ")");
			
			// explain query weight
			Explanation queryExpl = new Explanation();
			queryExpl.SetDescription("queryWeight(" + GetQuery() + "), product of:");
			
			Explanation boostExpl = new Explanation(GetQuery().GetBoost(), "boost");
			if (GetQuery().GetBoost() != 1.0f)
				queryExpl.AddDetail(boostExpl);
			queryExpl.AddDetail(idfExpl);
			
			Explanation queryNormExpl = new Explanation(queryNorm, "queryNorm");
			queryExpl.AddDetail(queryNormExpl);
			
			queryExpl.SetValue(boostExpl.GetValue() * idfExpl.GetValue() * queryNormExpl.GetValue());
			
			result.AddDetail(queryExpl);
			
			// explain field weight
			ComplexExplanation fieldExpl = new ComplexExplanation();
			fieldExpl.SetDescription("fieldWeight(" + field + ":" + query.ToString(field) + " in " + doc + "), product of:");
			
			Explanation tfExpl = Scorer(reader).Explain(doc);
			fieldExpl.AddDetail(tfExpl);
			fieldExpl.AddDetail(idfExpl);
			
			Explanation fieldNormExpl = new Explanation();
			byte[] fieldNorms = reader.Norms(field);
			float fieldNorm = fieldNorms != null ? Similarity.DecodeNorm(fieldNorms[doc]) : 0.0f;
			fieldNormExpl.SetValue(fieldNorm);
			fieldNormExpl.SetDescription("fieldNorm(field=" + field + ", doc=" + doc + ")");
			fieldExpl.AddDetail(fieldNormExpl);
			
			fieldExpl.SetMatch(tfExpl.IsMatch());
			fieldExpl.SetValue(tfExpl.GetValue() * idfExpl.GetValue() * fieldNormExpl.GetValue());
			
			result.AddDetail(fieldExpl);
			System.Boolean tempAux = fieldExpl.GetMatch();
			result.SetMatch(tempAux);
			
			// combine them
			result.SetValue(queryExpl.GetValue() * fieldExpl.GetValue());
			
			if (queryExpl.GetValue() == 1.0f)
				return fieldExpl;
			
			return result;
		}
	}
}