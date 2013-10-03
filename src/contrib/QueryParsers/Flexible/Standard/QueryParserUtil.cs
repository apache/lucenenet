using Lucene.Net.Analysis;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard
{
    public static class QueryParserUtil
    {
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
                    (!(q is BooleanQuery) || ((BooleanQuery)q).Clauses.Length > 0))
                {
                    bQuery.Add(q, Occur.SHOULD);
                }
            }
            return bQuery;
        }

        public static Query Parse(string query, string[] fields, Occur[] flags, Analyzer analyzer)
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
                    (!(q is BooleanQuery) || ((BooleanQuery)q).Clauses.Length > 0))
                {
                    bQuery.Add(q, flags[i]);
                }
            }
            return bQuery;
        }

        public static Query Parse(string[] queries, string[] fields, Occur[] flags, Analyzer analyzer)
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
                    (!(q is BooleanQuery) || ((BooleanQuery)q).Clauses.Length > 0))
                {
                    bQuery.Add(q, flags[i]);
                }
            }
            return bQuery;
        }

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
                    || c == '|' || c == '&')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

    }
}
