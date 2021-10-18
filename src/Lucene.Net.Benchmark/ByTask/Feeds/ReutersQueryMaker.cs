using Lucene.Net.Analysis;
using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// A <see cref="IQueryMaker"/> that makes queries devised manually (by Grant Ingersoll) for
    /// searching in the Reuters collection.
    /// </summary>
    public class ReutersQueryMaker : AbstractQueryMaker, IQueryMaker
    {
        private static readonly string[] STANDARD_QUERIES = { // LUCENENET: marked readonly
            //Start with some short queries
            "Salomon", "Comex", "night trading", "Japan Sony",
            //Try some Phrase Queries
            "\"Sony Japan\"", "\"food needs\"~3",
            "\"World Bank\"^2 AND Nigeria", "\"World Bank\" -Nigeria",
            "\"Ford Credit\"~5",
            //Try some longer queries
            "airline Europe Canada destination",
            "Long term pressure by trade " +
            "ministers is necessary if the current Uruguay round of talks on " +
            "the General Agreement on Trade and Tariffs (GATT) is to " +
            "succeed"
        };

        private static Query[] GetPrebuiltQueries(string field)
        {
            //  be wary of unanalyzed text
            return new Query[] {
                new SpanFirstQuery(new SpanTermQuery(new Term(field, "ford")), 5),
                new SpanNearQuery(new SpanQuery[]{new SpanTermQuery(new Term(field, "night")), new SpanTermQuery(new Term(field, "trading"))}, 4, false),
                new SpanNearQuery(new SpanQuery[]{new SpanFirstQuery(new SpanTermQuery(new Term(field, "ford")), 10), new SpanTermQuery(new Term(field, "credit"))}, 10, false),
                new WildcardQuery(new Term(field, "fo*")),
            };
        }

        /// <summary>
        /// Parse the strings containing Lucene queries.
        /// </summary>
        /// <param name="qs">array of strings containing query expressions</param>
        /// <param name="a">analyzer to use when parsing queries</param>
        /// <returns>array of Lucene queries</returns>
        private static Query[] CreateQueries(IList<object> qs, Analyzer a)
        {
            QueryParser qp = new QueryParser(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
                DocMaker.BODY_FIELD, a);
            JCG.List<Query> queries = new JCG.List<Query>();
            for (int i = 0; i < qs.Count; i++)
            {
                try
                {
                    object query = qs[i];
                    Query q = null;
                    if (query is string queryString)
                    {
                        q = qp.Parse(queryString);

                    }
                    else if (query is Query queryObj)
                    {
                        q = queryObj;
                    }
                    else
                    {
                        Console.Error.WriteLine("Unsupported Query Type: " + query);
                    }

                    if (q != null)
                    {
                        queries.Add(q);
                    }

                }
                catch (Exception e) when (e.IsException())
                {
                    Console.Error.WriteLine(e.ToString());
                }
            }

            return queries.ToArray();
        }

        protected override Query[] PrepareQueries()
        {
            // analyzer (default is standard analyzer)
            Analyzer anlzr = NewAnalyzerTask.CreateAnalyzer(m_config.Get("analyzer",
                typeof(Lucene.Net.Analysis.Standard.StandardAnalyzer).AssemblyQualifiedName));

            JCG.List<object> queryList = new JCG.List<object>(20);
            queryList.AddRange(STANDARD_QUERIES);
            queryList.AddRange(GetPrebuiltQueries(DocMaker.BODY_FIELD));
            return CreateQueries(queryList, anlzr);
        }
    }
}
