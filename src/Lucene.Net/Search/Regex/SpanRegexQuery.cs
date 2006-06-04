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
using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using BooleanClause = Lucene.Net.Search.BooleanClause;
using BooleanQuery = Lucene.Net.Search.BooleanQuery;
using Query = Lucene.Net.Search.Query;
using TermQuery = Lucene.Net.Search.TermQuery;
using SpanOrQuery = Lucene.Net.Search.Spans.SpanOrQuery;
using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search.Regex
{
	
	[Serializable]
	public class SpanRegexQuery:SpanQuery
	{
		virtual public Term Term
		{
			get
			{
				return term;
			}
			
		}
		private Term term;
		
		public SpanRegexQuery(Term term)
		{
			this.term = term;
		}
		
		public override Query Rewrite(IndexReader reader)
		{
			Query orig = new RegexQuery(term).Rewrite(reader);
			
			// RegexQuery (via MultiTermQuery).rewrite always returns a BooleanQuery
			BooleanQuery bq = (BooleanQuery) orig;
			
			BooleanClause[] clauses = bq.GetClauses();
			SpanQuery[] sqs = new SpanQuery[clauses.Length];
			for (int i = 0; i < clauses.Length; i++)
			{
				BooleanClause clause = clauses[i];
				
				// Clauses from RegexQuery.rewrite are always TermQuery's
				TermQuery tq = (TermQuery) clause.GetQuery();
				
				sqs[i] = new SpanTermQuery(tq.GetTerm());
				sqs[i].SetBoost(tq.GetBoost());
			}
			
			SpanOrQuery query = new SpanOrQuery(sqs);
			query.SetBoost(orig.GetBoost());
			
			return query;
		}
		
		public override Lucene.Net.Search.Spans.Spans GetSpans(IndexReader reader)
		{
			throw new System.NotSupportedException("Query should have been rewritten");
		}
		
		public override System.String GetField()
		{
			return term.Field();
		}
		
		public override System.Collections.ICollection GetTerms()
		{
			System.Collections.ArrayList terms = new System.Collections.ArrayList();
            terms.Add(term);
			return terms;
		}
		
		public  override bool Equals(System.Object o)
		{
			if (this == o)
				return true;
			if (o == null || GetType() != o.GetType())
				return false;
			
			SpanRegexQuery that = (SpanRegexQuery) o;
			
			return term.Equals(that.term) && GetBoost() == that.GetBoost();
		}
		
		public override int GetHashCode()
		{
			return term.GetHashCode();
		}
		
		public override System.String ToString(System.String field)
		{
			System.Text.StringBuilder buffer = new System.Text.StringBuilder();
			buffer.Append("spanRegexQuery(");
			buffer.Append(term);
			buffer.Append(")");
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}
	}
}