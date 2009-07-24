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
using TermDocs = Lucene.Net.Index.TermDocs;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search
{
	
	/// <summary>A Query that matches documents containing a term.
	/// This may be combined with other terms with a {@link BooleanQuery}.
	/// </summary>
	[Serializable]
	public class TermQuery : Query
	{
		private Term term;
		
		[Serializable]
		private class TermWeight : Weight
		{
			private void  InitBlock(TermQuery enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private TermQuery enclosingInstance;
			public TermQuery Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private Similarity similarity;
			private float value_Renamed;
			private float idf;
			private float queryNorm;
			private float queryWeight;
			
			public TermWeight(TermQuery enclosingInstance, Searcher searcher)
			{
				InitBlock(enclosingInstance);
				this.similarity = Enclosing_Instance.GetSimilarity(searcher);
				idf = similarity.Idf(Enclosing_Instance.term, searcher); // compute idf
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
				return value_Renamed;
			}
			
			public virtual float SumOfSquaredWeights()
			{
				queryWeight = idf * Enclosing_Instance.GetBoost(); // compute query weight
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
				TermDocs termDocs = reader.TermDocs(Enclosing_Instance.term);
				
				if (termDocs == null)
					return null;
				
				return new TermScorer(this, termDocs, similarity, reader.Norms(Enclosing_Instance.term.Field()));
			}
			
			public virtual Explanation Explain(IndexReader reader, int doc)
			{
				
				ComplexExplanation result = new ComplexExplanation();
				result.SetDescription("weight(" + GetQuery() + " in " + doc + "), product of:");
				
				Explanation idfExpl = new Explanation(idf, "idf(docFreq=" + reader.DocFreq(Enclosing_Instance.term) + ", numDocs=" + reader.NumDocs() + ")");
				
				// explain query weight
				Explanation queryExpl = new Explanation();
				queryExpl.SetDescription("queryWeight(" + GetQuery() + "), product of:");
				
				Explanation boostExpl = new Explanation(Enclosing_Instance.GetBoost(), "boost");
				if (Enclosing_Instance.GetBoost() != 1.0f)
					queryExpl.AddDetail(boostExpl);
				queryExpl.AddDetail(idfExpl);
				
				Explanation queryNormExpl = new Explanation(queryNorm, "queryNorm");
				queryExpl.AddDetail(queryNormExpl);
				
				queryExpl.SetValue(boostExpl.GetValue() * idfExpl.GetValue() * queryNormExpl.GetValue());
				
				result.AddDetail(queryExpl);
				
				// explain field weight
				System.String field = Enclosing_Instance.term.Field();
				ComplexExplanation fieldExpl = new ComplexExplanation();
				fieldExpl.SetDescription("fieldWeight(" + Enclosing_Instance.term + " in " + doc + "), product of:");
				
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
		
		/// <summary>Constructs a query for the term <code>t</code>. </summary>
		public TermQuery(Term t)
		{
			term = t;
		}
		
		/// <summary>Returns the term of this query. </summary>
		public virtual Term GetTerm()
		{
			return term;
		}
		
		protected internal override Weight CreateWeight(Searcher searcher)
		{
			return new TermWeight(this, searcher);
		}
		
		public override void  ExtractTerms(System.Collections.Hashtable terms)
		{
            Term term = GetTerm();
            if (terms.Contains(term) == false)
            {
                terms.Add(term, term);
            }
        }
		
		/// <summary>Prints a user-readable version of this query. </summary>
		public override System.String ToString(System.String field)
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			if (!term.Field().Equals(field))
			{
				buffer.Append(term.Field());
				buffer.Append(":");
			}
			buffer.Append(term.Text());
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}
		
		/// <summary>Returns true iff <code>o</code> is equal to this. </summary>
		public  override bool Equals(System.Object o)
		{
			if (!(o is TermQuery))
				return false;
			TermQuery other = (TermQuery) o;
			return (this.GetBoost() == other.GetBoost()) && this.term.Equals(other.term);
		}
		
		/// <summary>Returns a hash code value for this object.</summary>
		public override int GetHashCode()
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(GetBoost()), 0) ^ term.GetHashCode();
		}
	}
}