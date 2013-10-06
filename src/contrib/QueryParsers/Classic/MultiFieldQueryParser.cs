using Lucene.Net.Analysis;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.QueryParsers.Classic
{
    public class MultiFieldQueryParser : QueryParser
    {
        protected string[] fields;
        protected IDictionary<string, float> boosts;

        public MultiFieldQueryParser(Version matchVersion, string[] fields, Analyzer analyzer, IDictionary<string, float> boosts)
            : this(matchVersion, fields, analyzer)
        {
            this.boosts = boosts;
        }

        public MultiFieldQueryParser(Version matchVersion, string[] fields, Analyzer analyzer)
            : base(matchVersion, null, analyzer)
        {
            this.fields = fields;
        }

        protected override Query GetFieldQuery(string field, string queryText, int slop)
        {
            if (field == null)
            {
                IList<BooleanClause> clauses = new List<BooleanClause>();
                for (int i = 0; i < fields.Length; i++)
                {
                    Query q = base.GetFieldQuery(fields[i], queryText, true);
                    if (q != null)
                    {
                        //If the user passes a map of boosts
                        if (boosts != null)
                        {
                            //Get the boost from the map and apply them
                            float boost;
                            if (boosts.TryGetValue(fields[i], out boost))
                            {
                                q.Boost = boost;
                            }
                        }
                        ApplySlop(q, slop);
                        clauses.Add(new BooleanClause(q, Occur.SHOULD));
                    }
                }
                if (clauses.Count == 0)  // happens for stopwords
                    return null;
                return GetBooleanQuery(clauses, true);
            }
            Query q2 = base.GetFieldQuery(field, queryText, true);
            ApplySlop(q2, slop);
            return q2;
        }

        private void ApplySlop(Query q, int slop)
        {
            if (q is PhraseQuery)
            {
                ((PhraseQuery)q).Slop = slop;
            }
            else if (q is MultiPhraseQuery)
            {
                ((MultiPhraseQuery)q).Slop = slop;
            }
        }

        protected override Query GetFieldQuery(string field, string queryText, bool quoted)
        {
            if (field == null)
            {
                IList<BooleanClause> clauses = new List<BooleanClause>();
                for (int i = 0; i < fields.Length; i++)
                {
                    Query q = base.GetFieldQuery(fields[i], queryText, quoted);
                    if (q != null)
                    {
                        //If the user passes a map of boosts
                        if (boosts != null)
                        {
                            //Get the boost from the map and apply them
                            float boost;
                            if (boosts.TryGetValue(fields[i], out boost))
                            {
                                q.Boost = boost;
                            }
                        }
                        clauses.Add(new BooleanClause(q, Occur.SHOULD));
                    }
                }
                if (clauses.Count == 0)  // happens for stopwords
                    return null;
                return GetBooleanQuery(clauses, true);
            }
            Query q2 = base.GetFieldQuery(field, queryText, quoted);
            return q2;
        }

        protected override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            if (field == null)
            {
                IList<BooleanClause> clauses = new List<BooleanClause>();
                for (int i = 0; i < fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetFuzzyQuery(fields[i], termStr, minSimilarity), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetFuzzyQuery(field, termStr, minSimilarity);
        }

        protected override Query GetPrefixQuery(string field, string termStr)
        {
            if (field == null)
            {
                IList<BooleanClause> clauses = new List<BooleanClause>();
                for (int i = 0; i < fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetPrefixQuery(fields[i], termStr), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetPrefixQuery(field, termStr);
        }

        protected override Query GetWildcardQuery(string field, string termStr)
        {
            if (field == null)
            {
                IList<BooleanClause> clauses = new List<BooleanClause>();
                for (int i = 0; i < fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetWildcardQuery(fields[i], termStr), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetWildcardQuery(field, termStr);
        }

        protected override Query GetRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            if (field == null)
            {
                IList<BooleanClause> clauses = new List<BooleanClause>();
                for (int i = 0; i < fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetRangeQuery(fields[i], part1, part2, startInclusive, endInclusive), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetRangeQuery(field, part1, part2, startInclusive, endInclusive);
        }

        protected override Query GetRegexpQuery(string field, string termStr)
        {
            if (field == null)
            {
                IList<BooleanClause> clauses = new List<BooleanClause>();
                for (int i = 0; i < fields.Length; i++)
                {
                    clauses.Add(new BooleanClause(GetRegexpQuery(fields[i], termStr), Occur.SHOULD));
                }
                return GetBooleanQuery(clauses, true);
            }
            return base.GetRegexpQuery(field, termStr);
        }

        public static Query Parse(Version matchVersion, string[] queries, string[] fields, Analyzer analyzer)
        {
            if (queries.Length != fields.Length)
                throw new ArgumentException("queries.length != fields.length");
            BooleanQuery bQuery = new BooleanQuery();
            for (int i = 0; i < fields.Length; i++)
            {
                QueryParser qp = new QueryParser(matchVersion, fields[i], analyzer);
                Query q = qp.Parse(queries[i]);
                if (q != null && // q never null, just being defensive
                    (!(q is BooleanQuery) || ((BooleanQuery)q).Clauses.Length > 0))
                {
                    bQuery.Add(q, Occur.SHOULD);
                }
            }
            return bQuery;
        }

        public static Query Parse(Version matchVersion, string query, string[] fields, Occur[] flags, Analyzer analyzer)
        {
            if (fields.Length != flags.Length)
                throw new ArgumentException("fields.length != flags.length");
            BooleanQuery bQuery = new BooleanQuery();
            for (int i = 0; i < fields.Length; i++)
            {
                QueryParser qp = new QueryParser(matchVersion, fields[i], analyzer);
                Query q = qp.Parse(query);
                if (q != null && // q never null, just being defensive 
                    (!(q is BooleanQuery) || ((BooleanQuery)q).Clauses.Length > 0))
                {
                    bQuery.Add(q, flags[i]);
                }
            }
            return bQuery;
        }

        public static Query Parse(Version matchVersion, string[] queries, string[] fields, Occur[] flags, Analyzer analyzer)
        {
            if (!(queries.Length == fields.Length && queries.Length == flags.Length))
                throw new ArgumentException("queries, fields, and flags array have have different length");
            BooleanQuery bQuery = new BooleanQuery();
            for (int i = 0; i < fields.Length; i++)
            {
                QueryParser qp = new QueryParser(matchVersion, fields[i], analyzer);
                Query q = qp.Parse(queries[i]);
                if (q != null && // q never null, just being defensive
                    (!(q is BooleanQuery) || ((BooleanQuery)q).Clauses.Length > 0))
                {
                    bQuery.Add(q, flags[i]);
                }
            }
            return bQuery;
        }
    }
}
