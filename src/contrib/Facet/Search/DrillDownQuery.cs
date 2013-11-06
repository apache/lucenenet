using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Facet.Search
{
    public sealed class DrillDownQuery : Query
    {
        public static Term Term(FacetIndexingParams iParams, CategoryPath path)
        {
            CategoryListParams clp = iParams.GetCategoryListParams(path);
            char[] buffer = new char[path.FullPathLength()];
            iParams.DrillDownTermText(path, buffer);
            return new Term(clp.field, new string(buffer));
        }

        private readonly BooleanQuery query;
        private readonly IDictionary<string, int?> drillDownDims = new HashMap<string, int?>();
        internal readonly FacetIndexingParams fip;

        internal DrillDownQuery(FacetIndexingParams fip, BooleanQuery query, IDictionary<string, int?> drillDownDims)
        {
            this.fip = fip;
            this.query = (BooleanQuery)query.Clone();
            this.drillDownDims.PutAll(drillDownDims);
        }

        internal DrillDownQuery(Filter filter, DrillDownQuery other)
        {
            query = new BooleanQuery(true);
            BooleanClause[] clauses = other.query.Clauses;
            if (clauses.Length == other.drillDownDims.Count)
            {
                throw new ArgumentException(@"cannot apply filter unless baseQuery isn't null; pass ConstantScoreQuery instead");
            }

            drillDownDims.PutAll(other.drillDownDims);
            query.Add(new FilteredQuery(clauses[0].Query, filter), Occur.MUST);
            for (int i = 1; i < clauses.Length; i++)
            {
                query.Add(clauses[i].Query, Occur.MUST);
            }

            fip = other.fip;
        }

        internal DrillDownQuery(FacetIndexingParams fip, Query baseQuery, List<Query> clauses)
        {
            this.fip = fip;
            this.query = new BooleanQuery(true);
            if (baseQuery != null)
            {
                query.Add(baseQuery, Occur.MUST);
            }

            foreach (Query clause in clauses)
            {
                query.Add(clause, Occur.MUST);
                drillDownDims[GetDim(clause)] = drillDownDims.Count;
            }
        }

        internal string GetDim(Query clause)
        {
            clause = ((ConstantScoreQuery)clause).Query;
            string term;
            if (clause is TermQuery)
            {
                term = ((TermQuery)clause).Term.Text;
            }
            else
            {
                term = ((TermQuery)((BooleanQuery)clause).Clauses[0].Query).Term.Text;
            }

            return term.Split(new[] { Regex.Escape(fip.FacetDelimChar.ToString()) }, StringSplitOptions.None)[0];
        }

        public DrillDownQuery(FacetIndexingParams fip)
            : this(fip, null)
        {
        }

        public DrillDownQuery(FacetIndexingParams fip, Query baseQuery)
        {
            query = new BooleanQuery(true);
            if (baseQuery != null)
            {
                query.Add(baseQuery, Occur.MUST);
            }

            this.fip = fip;
        }

        public void Add(params CategoryPath[] paths)
        {
            Query q;
            if (paths[0].length == 0)
            {
                throw new ArgumentException(@"all CategoryPaths must have length > 0");
            }

            string dim = paths[0].components[0];
            if (drillDownDims.ContainsKey(dim))
            {
                throw new ArgumentException(@"dimension '" + dim + @"' was already added");
            }

            if (paths.Length == 1)
            {
                q = new TermQuery(Term(fip, paths[0]));
            }
            else
            {
                BooleanQuery bq = new BooleanQuery(true);
                foreach (CategoryPath cp in paths)
                {
                    if (cp.length == 0)
                    {
                        throw new ArgumentException(@"all CategoryPaths must have length > 0");
                    }

                    if (!cp.components[0].Equals(dim))
                    {
                        throw new ArgumentException(@"multiple (OR'd) drill-down paths must be under same dimension; got '" + dim + @"' and '" + cp.components[0] + @"'");
                    }

                    bq.Add(new TermQuery(Term(fip, cp)), Occur.SHOULD);
                }

                q = bq;
            }

            drillDownDims[dim] = drillDownDims.Count;
            ConstantScoreQuery drillDownQuery = new ConstantScoreQuery(q);
            drillDownQuery.Boost = 0F;
            query.Add(drillDownQuery, Occur.MUST);
        }

        public override object Clone()
        {
            return new DrillDownQuery(fip, query, drillDownDims);
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            return prime * result + query.GetHashCode();
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is DrillDownQuery))
            {
                return false;
            }

            DrillDownQuery other = (DrillDownQuery)obj;
            return query.Equals(other.query) && base.Equals(other);
        }

        public override Query Rewrite(IndexReader r)
        {
            if (query.Clauses.Count() == 0)
            {
                throw new InvalidOperationException(@"no base query or drill-down categories given");
            }

            return query;
        }

        public override string ToString(string field)
        {
            return query.ToString(field);
        }

        internal BooleanQuery BooleanQuery
        {
            get
            {
                return query;
            }
        }

        internal IDictionary<string, int?> Dims
        {
            get
            {
                return drillDownDims;
            }
        }
    }
}
