using Lucene.Net.Analysis;
using Lucene.Net.Search;
using System;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Standard
{
    /// <summary>
    /// This class defines utility methods to (help) parse query strings into
    /// {@link Query} objects.
    /// </summary>
    public sealed class QueryParserUtil
    {
        /**
   * Parses a query which searches on the fields specified.
   * <p>
   * If x fields are specified, this effectively constructs:
   * 
   * <pre>
   * <code>
   * (field1:query1) (field2:query2) (field3:query3)...(fieldx:queryx)
   * </code>
   * </pre>
   * 
   * @param queries
   *          Queries strings to parse
   * @param fields
   *          Fields to search on
   * @param analyzer
   *          Analyzer to use
   * @throws IllegalArgumentException
   *           if the length of the queries array differs from the length of the
   *           fields array
   */
        public static Query Parse(string[] queries, string[] fields, Analyzer analyzer)
        {
            if (queries.Length != fields.Length)
                throw new ArgumentException("queries.length != fields.length");
            BooleanQuery bQuery = new BooleanQuery();

            StandardQueryParser qp = new StandardQueryParser();
            qp.Analyzer = analyzer;

            for (int i = 0; i < fields.Length; i++)
            {
                Query q = qp.Parse(queries[i], fields[i]);

                if (q != null && // q never null, just being defensive
                    (!(q is BooleanQuery) || ((BooleanQuery)q).GetClauses().Count > 0))
                {
                    bQuery.Add(q, BooleanClause.Occur.SHOULD);
                }
            }
            return bQuery;
        }

        /**
         * Parses a query, searching on the fields specified. Use this if you need to
         * specify certain fields as required, and others as prohibited.
         * <p>
         * 
         * Usage:
         * <pre class="prettyprint">
         * <code>
         * String[] fields = {&quot;filename&quot;, &quot;contents&quot;, &quot;description&quot;};
         * BooleanClause.Occur[] flags = {BooleanClause.Occur.SHOULD,
         *                BooleanClause.Occur.MUST,
         *                BooleanClause.Occur.MUST_NOT};
         * MultiFieldQueryParser.parse(&quot;query&quot;, fields, flags, analyzer);
         * </code>
         * </pre>
         *<p>
         * The code above would construct a query:
         * 
         * <pre>
         * <code>
         * (filename:query) +(contents:query) -(description:query)
         * </code>
         * </pre>
         * 
         * @param query
         *          Query string to parse
         * @param fields
         *          Fields to search on
         * @param flags
         *          Flags describing the fields
         * @param analyzer
         *          Analyzer to use
         * @throws IllegalArgumentException
         *           if the length of the fields array differs from the length of the
         *           flags array
         */
        public static Query Parse(string query, string[] fields,
            BooleanClause.Occur[] flags, Analyzer analyzer)
        {
            if (fields.Length != flags.Length)
                throw new ArgumentException("fields.length != flags.length");
            BooleanQuery bQuery = new BooleanQuery();

            StandardQueryParser qp = new StandardQueryParser();
            qp.Analyzer = analyzer;

            for (int i = 0; i < fields.Length; i++)
            {
                Query q = qp.Parse(query, fields[i]);

                if (q != null && // q never null, just being defensive
                    (!(q is BooleanQuery) || ((BooleanQuery)q).GetClauses().Count > 0))
                {
                    bQuery.Add(q, flags[i]);
                }
            }
            return bQuery;
        }

        /**
         * Parses a query, searching on the fields specified. Use this if you need to
         * specify certain fields as required, and others as prohibited.
         * <p>
         * 
         * Usage:
         * <pre class="prettyprint">
         * <code>
         * String[] query = {&quot;query1&quot;, &quot;query2&quot;, &quot;query3&quot;};
         * String[] fields = {&quot;filename&quot;, &quot;contents&quot;, &quot;description&quot;};
         * BooleanClause.Occur[] flags = {BooleanClause.Occur.SHOULD,
         *                BooleanClause.Occur.MUST,
         *                BooleanClause.Occur.MUST_NOT};
         * MultiFieldQueryParser.parse(query, fields, flags, analyzer);
         * </code>
         * </pre>
         *<p>
         * The code above would construct a query:
         * 
         * <pre>
         * <code>
         * (filename:query1) +(contents:query2) -(description:query3)
         * </code>
         * </pre>
         * 
         * @param queries
         *          Queries string to parse
         * @param fields
         *          Fields to search on
         * @param flags
         *          Flags describing the fields
         * @param analyzer
         *          Analyzer to use
         * @throws IllegalArgumentException
         *           if the length of the queries, fields, and flags array differ
         */
        public static Query Parse(string[] queries, string[] fields,
            BooleanClause.Occur[] flags, Analyzer analyzer)
        {
            if (!(queries.Length == fields.Length && queries.Length == flags.Length))
                throw new ArgumentException(
                    "queries, fields, and flags array have have different length");
            BooleanQuery bQuery = new BooleanQuery();

            StandardQueryParser qp = new StandardQueryParser();
            qp.Analyzer = analyzer;

            for (int i = 0; i < fields.Length; i++)
            {
                Query q = qp.Parse(queries[i], fields[i]);

                if (q != null && // q never null, just being defensive
                    (!(q is BooleanQuery) || ((BooleanQuery)q).GetClauses().Count > 0))
                {
                    bQuery.Add(q, flags[i]);
                }
            }
            return bQuery;
        }

        /**
         * Returns a String where those characters that TextParser expects to be
         * escaped are escaped by a preceding <code>\</code>.
         */
        public static string Escape(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                // These characters are part of the query syntax and must be escaped
                if (c == '\\' || c == '+' || c == '-' || c == '!' || c == '(' || c == ')'
                    || c == ':' || c == '^' || c == '[' || c == ']' || c == '\"'
                    || c == '{' || c == '}' || c == '~' || c == '*' || c == '?'
                    || c == '|' || c == '&' || c == '/')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
