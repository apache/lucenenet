/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Queryparser.Flexible.Standard;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard
{
	/// <summary>
	/// This class defines utility methods to (help) parse query strings into
	/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
	/// objects.
	/// </summary>
	public sealed class QueryParserUtil
	{
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
		/// <param name="queries">Queries strings to parse</param>
		/// <param name="fields">Fields to search on</param>
		/// <param name="analyzer">Analyzer to use</param>
		/// <exception cref="System.ArgumentException">
		/// if the length of the queries array differs from the length of the
		/// fields array
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public static Query Parse(string[] queries, string[] fields, Analyzer analyzer)
		{
			if (queries.Length != fields.Length)
			{
				throw new ArgumentException("queries.length != fields.length");
			}
			BooleanQuery bQuery = new BooleanQuery();
			StandardQueryParser qp = new StandardQueryParser();
			qp.SetAnalyzer(analyzer);
			for (int i = 0; i < fields.Length; i++)
			{
				Query q = ((Query)qp.Parse(queries[i], fields[i]));
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
		/// Parses a query, searching on the fields specified. Use this if you need to
		/// specify certain fields as required, and others as prohibited.
		/// <p>
		/// Usage:
		/// <pre class="prettyprint">
		/// <code>
		/// String[] fields = {&quot;filename&quot;, &quot;contents&quot;, &quot;description&quot;};
		/// BooleanClause.Occur[] flags = {BooleanClause.Occur.SHOULD,
		/// BooleanClause.Occur.MUST,
		/// BooleanClause.Occur.MUST_NOT};
		/// MultiFieldQueryParser.parse(&quot;query&quot;, fields, flags, analyzer);
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
		/// <param name="query">Query string to parse</param>
		/// <param name="fields">Fields to search on</param>
		/// <param name="flags">Flags describing the fields</param>
		/// <param name="analyzer">Analyzer to use</param>
		/// <exception cref="System.ArgumentException">
		/// if the length of the fields array differs from the length of the
		/// flags array
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public static Query Parse(string query, string[] fields, BooleanClause.Occur[] flags
			, Analyzer analyzer)
		{
			if (fields.Length != flags.Length)
			{
				throw new ArgumentException("fields.length != flags.length");
			}
			BooleanQuery bQuery = new BooleanQuery();
			StandardQueryParser qp = new StandardQueryParser();
			qp.SetAnalyzer(analyzer);
			for (int i = 0; i < fields.Length; i++)
			{
				Query q = ((Query)qp.Parse(query, fields[i]));
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
		/// Parses a query, searching on the fields specified. Use this if you need to
		/// specify certain fields as required, and others as prohibited.
		/// <p>
		/// Usage:
		/// <pre class="prettyprint">
		/// <code>
		/// String[] query = {&quot;query1&quot;, &quot;query2&quot;, &quot;query3&quot;};
		/// String[] fields = {&quot;filename&quot;, &quot;contents&quot;, &quot;description&quot;};
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
		/// <param name="queries">Queries string to parse</param>
		/// <param name="fields">Fields to search on</param>
		/// <param name="flags">Flags describing the fields</param>
		/// <param name="analyzer">Analyzer to use</param>
		/// <exception cref="System.ArgumentException">if the length of the queries, fields, and flags array differ
		/// 	</exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public static Query Parse(string[] queries, string[] fields, BooleanClause.Occur[]
			 flags, Analyzer analyzer)
		{
			if (!(queries.Length == fields.Length && queries.Length == flags.Length))
			{
				throw new ArgumentException("queries, fields, and flags array have have different length"
					);
			}
			BooleanQuery bQuery = new BooleanQuery();
			StandardQueryParser qp = new StandardQueryParser();
			qp.SetAnalyzer(analyzer);
			for (int i = 0; i < fields.Length; i++)
			{
				Query q = ((Query)qp.Parse(queries[i], fields[i]));
				if (q != null && (!(q is BooleanQuery) || ((BooleanQuery)q).GetClauses().Length >
					 0))
				{
					// q never null, just being defensive
					bQuery.Add(q, flags[i]);
				}
			}
			return bQuery;
		}

		/// <summary>
		/// Returns a String where those characters that TextParser expects to be
		/// escaped are escaped by a preceding <code>\</code>.
		/// </summary>
		/// <remarks>
		/// Returns a String where those characters that TextParser expects to be
		/// escaped are escaped by a preceding <code>\</code>.
		/// </remarks>
		public static string Escape(string s)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				// These characters are part of the query syntax and must be escaped
				if (c == '\\' || c == '+' || c == '-' || c == '!' || c == '(' || c == ')' || c ==
					 ':' || c == '^' || c == '[' || c == ']' || c == '\"' || c == '{' || c == '}' ||
					 c == '~' || c == '*' || c == '?' || c == '|' || c == '&' || c == '/')
				{
					sb.Append('\\');
				}
				sb.Append(c);
			}
			return sb.ToString();
		}
	}
}
