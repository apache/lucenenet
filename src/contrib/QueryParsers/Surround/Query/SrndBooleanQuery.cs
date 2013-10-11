using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Query
{
    internal static class SrndBooleanQuery
    {
        public static void AddQueriesToBoolean(
          BooleanQuery bq,
          IList<Search.Query> queries,
          Occur occur)
        {
            for (int i = 0; i < queries.Count; i++)
            {
                bq.Add(queries[i], occur);
            }
        }

        public static Search.Query MakeBooleanQuery(
          IList<Search.Query> queries,
          Occur occur)
        {
            if (queries.Count <= 1)
            {
                throw new InvalidOperationException("Too few subqueries: " + queries.Count);
            }
            BooleanQuery bq = new BooleanQuery();
            AddQueriesToBoolean(bq, queries.SubList(0, queries.Count), occur);
            return bq;
        }
    }
}
