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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Regex
{
	/// <summary>
	/// A SpanQuery version of <see cref="RegexQuery"/> allowing regular expression queries to be nested
	/// within other SpanQuery subclasses.
	/// </summary>
	/// <remarks>http://www.java2s.com/Open-Source/Java-Document/Net/lucene-connector/org/apache/lucene/search/regex/SpanRegexQuery.java.htm</remarks>
	public class SpanRegexQuery : SpanQuery, IRegexQueryCapable, IEquatable<SpanRegexQuery>
	{
		private IRegexCapabilities _regexImpl = new CSharpRegexCapabilities();
		private readonly Term _term;

		public SpanRegexQuery(Term term)
		{
			_term = term;
		}

		public Term GetTerm()
		{
			return _term;
		}

		public override string ToString(string field)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("SpanRegexQuery(");
			sb.Append(_term);
			sb.Append(')');
			sb.Append(ToStringUtils.Boost(GetBoost()));
			return sb.ToString();
		}

		public override Query Rewrite(IndexReader reader)
		{
			RegexQuery orig = new RegexQuery(_term);
			orig.SetRegexImplementation(_regexImpl);

			// RegexQuery (via MultiTermQuery).Rewrite always returns a BooleanQuery
			orig.SetRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);	//@@
			BooleanQuery bq = (BooleanQuery) orig.Rewrite(reader);

			BooleanClause[] clauses = bq.GetClauses();
			SpanQuery[] sqs = new SpanQuery[clauses.Length];
			for (int i = 0; i < clauses.Length; i++)
			{
				BooleanClause clause = clauses[i];

				// Clauses from RegexQuery.Rewrite are always TermQuery's
				TermQuery tq = (TermQuery) clause.GetQuery();

				sqs[i] = new SpanTermQuery(tq.GetTerm());
				sqs[i].SetBoost(tq.GetBoost());
			} //efor

			SpanOrQuery query = new SpanOrQuery(sqs);
			query.SetBoost(orig.GetBoost());

			return query;
		}

		/// <summary>Expert: Returns the matches for this query in an index.  Used internally
		/// to search for spans. 
		/// </summary>
		public override Spans.Spans GetSpans(IndexReader reader)
		{
			throw new InvalidOperationException("Query should have been rewritten");
		}

		/// <summary>Returns the name of the field matched by this query.</summary>
		public override string GetField()
		{
			return _term.Field();
		}

		/// <summary>Returns a collection of all terms matched by this query.</summary>
		/// <deprecated> use extractTerms instead
		/// </deprecated>
		/// <seealso cref="Query.ExtractTerms">
		/// </seealso>
		public override IList<Term> GetTerms()
		{
			IList<Term> terms = new List<Term> {_term};
		    return terms;
		}
    
		public void SetRegexImplementation(IRegexCapabilities impl)
		{
			_regexImpl = impl;
		}

		public IRegexCapabilities GetRegexImplementation()
		{
			return _regexImpl;
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>
		/// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		/// <param name="other">An object to compare with this object.
		///                 </param>
		public bool Equals(SpanRegexQuery other)
		{
			if (other == null) return false;
			if (this == other) return true;

			if (!_regexImpl.Equals(other._regexImpl)) return false;
			if (!_term.Equals(other._term)) return false;

			return true;
		}

		/// <summary>
		/// True if this object equals the specified object.
		/// </summary>
		/// <param name="obj">object</param>
		/// <returns>true on equality</returns>
		public override bool Equals(object obj)
		{
			if (obj as SpanRegexQuery == null) return false;

			return Equals((SpanRegexQuery) obj);
		}

		/// <summary>
		/// Get hash code for this object.
		/// </summary>
		/// <returns>hash code</returns>
		public override int GetHashCode()
		{
			return 29 * _regexImpl.GetHashCode() + _term.GetHashCode();
		}
	}
}
