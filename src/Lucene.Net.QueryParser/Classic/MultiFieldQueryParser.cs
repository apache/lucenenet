/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Queryparser.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Queryparser.Classic
{
	/// <summary>A QueryParser which constructs queries to search multiple fields.</summary>
	/// <remarks>A QueryParser which constructs queries to search multiple fields.</remarks>
	public class MultiFieldQueryParser : QueryParser
	{
		protected internal string[] fields;

		protected internal IDictionary<string, float> boosts;

		/// <summary>Creates a MultiFieldQueryParser.</summary>
		/// <remarks>
		/// Creates a MultiFieldQueryParser.
		/// Allows passing of a map with term to Boost, and the boost to apply to each term.
		/// <p>It will, when parse(String query)
		/// is called, construct a query like this (assuming the query consists of
		/// two terms and you specify the two fields <code>title</code> and <code>body</code>):</p>
		/// <code>
		/// (title:term1 body:term1) (title:term2 body:term2)
		/// </code>
		/// <p>When setDefaultOperator(AND_OPERATOR) is set, the result will be:</p>
		/// <code>
		/// +(title:term1 body:term1) +(title:term2 body:term2)
		/// </code>
		/// <p>When you pass a boost (title=&gt;5 body=&gt;10) you can get </p>
		/// <code>
		/// +(title:term1^5.0 body:term1^10.0) +(title:term2^5.0 body:term2^10.0)
		/// </code>
		/// <p>In other words, all the query's terms must appear, but it doesn't matter in
		/// what fields they appear.</p>
		/// </remarks>
		public MultiFieldQueryParser(Version matchVersion, string[] fields, Analyzer analyzer
			, IDictionary<string, float> boosts) : this(matchVersion, fields, analyzer)
		{
			this.boosts = boosts;
		}

		/// <summary>Creates a MultiFieldQueryParser.</summary>
		/// <remarks>
		/// Creates a MultiFieldQueryParser.
		/// <p>It will, when parse(String query)
		/// is called, construct a query like this (assuming the query consists of
		/// two terms and you specify the two fields <code>title</code> and <code>body</code>):</p>
		/// <code>
		/// (title:term1 body:term1) (title:term2 body:term2)
		/// </code>
		/// <p>When setDefaultOperator(AND_OPERATOR) is set, the result will be:</p>
		/// <code>
		/// +(title:term1 body:term1) +(title:term2 body:term2)
		/// </code>
		/// <p>In other words, all the query's terms must appear, but it doesn't matter in
		/// what fields they appear.</p>
		/// </remarks>
		public MultiFieldQueryParser(Version matchVersion, string[] fields, Analyzer analyzer
			) : base(matchVersion, null, analyzer)
		{
			this.fields = fields;
		}

		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetFieldQuery(string field, string queryText, int
			 slop)
		{
			if (field == null)
			{
				IList<BooleanClause> clauses = new AList<BooleanClause>();
				for (int i = 0; i < fields.Length; i++)
				{
					Query q = base.GetFieldQuery(fields[i], queryText, true);
					if (q != null)
					{
						//If the user passes a map of boosts
						if (boosts != null)
						{
							//Get the boost from the map and apply them
							float boost = boosts.Get(fields[i]);
							if (boost != null)
							{
								q.SetBoost(boost);
							}
						}
						ApplySlop(q, slop);
						clauses.AddItem(new BooleanClause(q, BooleanClause.Occur.SHOULD));
					}
				}
				if (clauses.Count == 0)
				{
					// happens for stopwords
					return null;
				}
				return GetBooleanQuery(clauses, true);
			}
			Query q_1 = base.GetFieldQuery(field, queryText, true);
			ApplySlop(q_1, slop);
			return q_1;
		}

		private void ApplySlop(Query q, int slop)
		{
			if (q is PhraseQuery)
			{
				((PhraseQuery)q).SetSlop(slop);
			}
			else
			{
				if (q is MultiPhraseQuery)
				{
					((MultiPhraseQuery)q).SetSlop(slop);
				}
			}
		}

		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetFieldQuery(string field, string queryText, bool
			 quoted)
		{
			if (field == null)
			{
				IList<BooleanClause> clauses = new AList<BooleanClause>();
				for (int i = 0; i < fields.Length; i++)
				{
					Query q = base.GetFieldQuery(fields[i], queryText, quoted);
					if (q != null)
					{
						//If the user passes a map of boosts
						if (boosts != null)
						{
							//Get the boost from the map and apply them
							float boost = boosts.Get(fields[i]);
							if (boost != null)
							{
								q.SetBoost(boost);
							}
						}
						clauses.AddItem(new BooleanClause(q, BooleanClause.Occur.SHOULD));
					}
				}
				if (clauses.Count == 0)
				{
					// happens for stopwords
					return null;
				}
				return GetBooleanQuery(clauses, true);
			}
			Query q_1 = base.GetFieldQuery(field, queryText, quoted);
			return q_1;
		}

		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetFuzzyQuery(string field, string termStr, float
			 minSimilarity)
		{
			if (field == null)
			{
				IList<BooleanClause> clauses = new AList<BooleanClause>();
				for (int i = 0; i < fields.Length; i++)
				{
					clauses.AddItem(new BooleanClause(GetFuzzyQuery(fields[i], termStr, minSimilarity
						), BooleanClause.Occur.SHOULD));
				}
				return GetBooleanQuery(clauses, true);
			}
			return base.GetFuzzyQuery(field, termStr, minSimilarity);
		}

		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetPrefixQuery(string field, string termStr)
		{
			if (field == null)
			{
				IList<BooleanClause> clauses = new AList<BooleanClause>();
				for (int i = 0; i < fields.Length; i++)
				{
					clauses.AddItem(new BooleanClause(GetPrefixQuery(fields[i], termStr), BooleanClause.Occur
						.SHOULD));
				}
				return GetBooleanQuery(clauses, true);
			}
			return base.GetPrefixQuery(field, termStr);
		}

		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetWildcardQuery(string field, string termStr)
		{
			if (field == null)
			{
				IList<BooleanClause> clauses = new AList<BooleanClause>();
				for (int i = 0; i < fields.Length; i++)
				{
					clauses.AddItem(new BooleanClause(GetWildcardQuery(fields[i], termStr), BooleanClause.Occur
						.SHOULD));
				}
				return GetBooleanQuery(clauses, true);
			}
			return base.GetWildcardQuery(field, termStr);
		}

		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetRangeQuery(string field, string part1, string
			 part2, bool startInclusive, bool endInclusive)
		{
			if (field == null)
			{
				IList<BooleanClause> clauses = new AList<BooleanClause>();
				for (int i = 0; i < fields.Length; i++)
				{
					clauses.AddItem(new BooleanClause(GetRangeQuery(fields[i], part1, part2, startInclusive
						, endInclusive), BooleanClause.Occur.SHOULD));
				}
				return GetBooleanQuery(clauses, true);
			}
			return base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);
		}

		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		protected internal override Query GetRegexpQuery(string field, string termStr)
		{
			if (field == null)
			{
				IList<BooleanClause> clauses = new AList<BooleanClause>();
				for (int i = 0; i < fields.Length; i++)
				{
					clauses.AddItem(new BooleanClause(GetRegexpQuery(fields[i], termStr), BooleanClause.Occur
						.SHOULD));
				}
				return GetBooleanQuery(clauses, true);
			}
			return base.GetRegexpQuery(field, termStr);
		}

		/// <summary>Parses a query which searches on the fields specified.</summary>
		/// <remarks>
		/// Parses a query which searches on the fields specified.
		/// <p>
		/// If x fields are specified, this effectively constructs:
		/// <pre>
		/// <code>
		/// (field1:query1) (field2:query2) (field3:query3)...(fieldx:queryx)
		/// </code>
		/// </pre>
		/// </remarks>
		/// <param name="matchVersion">Lucene version to match; this is passed through to QueryParser.
		/// 	</param>
		/// <param name="queries">Queries strings to parse</param>
		/// <param name="fields">Fields to search on</param>
		/// <param name="analyzer">Analyzer to use</param>
		/// <exception cref="ParseException">if query parsing fails</exception>
		/// <exception cref="System.ArgumentException">
		/// if the length of the queries array differs
		/// from the length of the fields array
		/// </exception>
		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		public static Query Parse(Version matchVersion, string[] queries, string[] fields
			, Analyzer analyzer)
		{
			if (queries.Length != fields.Length)
			{
				throw new ArgumentException("queries.length != fields.length");
			}
			BooleanQuery bQuery = new BooleanQuery();
			for (int i = 0; i < fields.Length; i++)
			{
				QueryParser qp = new QueryParser(matchVersion, fields[i], analyzer);
				Query q = qp.Parse(queries[i]);
				if (q != null && (!(q is BooleanQuery) || ((BooleanQuery)q).GetClauses().Length >
					 0))
				{
					// q never null, just being defensive
					bQuery.Add(q, BooleanClause.Occur.SHOULD);
				}
			}
			return bQuery;
		}

		/// <summary>Parses a query, searching on the fields specified.</summary>
		/// <remarks>
		/// Parses a query, searching on the fields specified.
		/// Use this if you need to specify certain fields as required,
		/// and others as prohibited.
		/// <p>
		/// Usage:
		/// <pre class="prettyprint">
		/// <code>
		/// String[] fields = {"filename", "contents", "description"};
		/// BooleanClause.Occur[] flags = {BooleanClause.Occur.SHOULD,
		/// BooleanClause.Occur.MUST,
		/// BooleanClause.Occur.MUST_NOT};
		/// MultiFieldQueryParser.parse("query", fields, flags, analyzer);
		/// </code>
		/// </pre>
		/// <p>
		/// The code above would construct a query:
		/// <pre>
		/// <code>
		/// (filename:query) +(contents:query) -(description:query)
		/// </code>
		/// </pre>
		/// </remarks>
		/// <param name="matchVersion">Lucene version to match; this is passed through to QueryParser.
		/// 	</param>
		/// <param name="query">Query string to parse</param>
		/// <param name="fields">Fields to search on</param>
		/// <param name="flags">Flags describing the fields</param>
		/// <param name="analyzer">Analyzer to use</param>
		/// <exception cref="ParseException">if query parsing fails</exception>
		/// <exception cref="System.ArgumentException">
		/// if the length of the fields array differs
		/// from the length of the flags array
		/// </exception>
		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		public static Query Parse(Version matchVersion, string query, string[] fields, BooleanClause.Occur
			[] flags, Analyzer analyzer)
		{
			if (fields.Length != flags.Length)
			{
				throw new ArgumentException("fields.length != flags.length");
			}
			BooleanQuery bQuery = new BooleanQuery();
			for (int i = 0; i < fields.Length; i++)
			{
				QueryParser qp = new QueryParser(matchVersion, fields[i], analyzer);
				Query q = qp.Parse(query);
				if (q != null && (!(q is BooleanQuery) || ((BooleanQuery)q).GetClauses().Length >
					 0))
				{
					// q never null, just being defensive 
					bQuery.Add(q, flags[i]);
				}
			}
			return bQuery;
		}

		/// <summary>Parses a query, searching on the fields specified.</summary>
		/// <remarks>
		/// Parses a query, searching on the fields specified.
		/// Use this if you need to specify certain fields as required,
		/// and others as prohibited.
		/// <p>
		/// Usage:
		/// <pre class="prettyprint">
		/// <code>
		/// String[] query = {"query1", "query2", "query3"};
		/// String[] fields = {"filename", "contents", "description"};
		/// BooleanClause.Occur[] flags = {BooleanClause.Occur.SHOULD,
		/// BooleanClause.Occur.MUST,
		/// BooleanClause.Occur.MUST_NOT};
		/// MultiFieldQueryParser.parse(query, fields, flags, analyzer);
		/// </code>
		/// </pre>
		/// <p>
		/// The code above would construct a query:
		/// <pre>
		/// <code>
		/// (filename:query1) +(contents:query2) -(description:query3)
		/// </code>
		/// </pre>
		/// </remarks>
		/// <param name="matchVersion">Lucene version to match; this is passed through to QueryParser.
		/// 	</param>
		/// <param name="queries">Queries string to parse</param>
		/// <param name="fields">Fields to search on</param>
		/// <param name="flags">Flags describing the fields</param>
		/// <param name="analyzer">Analyzer to use</param>
		/// <exception cref="ParseException">if query parsing fails</exception>
		/// <exception cref="System.ArgumentException">
		/// if the length of the queries, fields,
		/// and flags array differ
		/// </exception>
		/// <exception cref="Lucene.Net.Queryparser.Classic.ParseException"></exception>
		public static Query Parse(Version matchVersion, string[] queries, string[] fields
			, BooleanClause.Occur[] flags, Analyzer analyzer)
		{
			if (!(queries.Length == fields.Length && queries.Length == flags.Length))
			{
				throw new ArgumentException("queries, fields, and flags array have have different length"
					);
			}
			BooleanQuery bQuery = new BooleanQuery();
			for (int i = 0; i < fields.Length; i++)
			{
				QueryParser qp = new QueryParser(matchVersion, fields[i], analyzer);
				Query q = qp.Parse(queries[i]);
				if (q != null && (!(q is BooleanQuery) || ((BooleanQuery)q).GetClauses().Length >
					 0))
				{
					// q never null, just being defensive
					bQuery.Add(q, flags[i]);
				}
			}
			return bQuery;
		}
	}
}
