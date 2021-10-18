using Lucene.Net.Analysis;
using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
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
    /// A <see cref="IQueryMaker"/> that makes queries for a collection created 
    /// using <see cref="SingleDocSource"/>.
    /// </summary>
    public class SimpleQueryMaker : AbstractQueryMaker, IQueryMaker
    {
        /// <summary>
        /// Prepare the queries for this test.
        /// Extending classes can override this method for preparing different queries.
        /// </summary>
        /// <returns>Prepared queries.</returns>
        /// <exception cref="Exception">If cannot prepare the queries.</exception>
        protected override Query[] PrepareQueries()
        {
            // analyzer (default is standard analyzer)
            Analyzer anlzr = NewAnalyzerTask.CreateAnalyzer(m_config.Get("analyzer",
                typeof(Lucene.Net.Analysis.Standard.StandardAnalyzer).AssemblyQualifiedName));

            QueryParser qp = new QueryParser(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
                DocMaker.BODY_FIELD, anlzr);
            JCG.List<Query> qq = new JCG.List<Query>();
            Query q1 = new TermQuery(new Term(DocMaker.ID_FIELD, "doc2"));
            qq.Add(q1);
            Query q2 = new TermQuery(new Term(DocMaker.BODY_FIELD, "simple"));
            qq.Add(q2);
            BooleanQuery bq = new BooleanQuery
            {
                { q1, Occur.MUST },
                { q2, Occur.MUST }
            };
            qq.Add(bq);
            qq.Add(qp.Parse("synthetic body"));
            qq.Add(qp.Parse("\"synthetic body\""));
            qq.Add(qp.Parse("synthetic text"));
            qq.Add(qp.Parse("\"synthetic text\""));
            qq.Add(qp.Parse("\"synthetic text\"~3"));
            qq.Add(qp.Parse("zoom*"));
            qq.Add(qp.Parse("synth*"));
            return qq.ToArray();
        }
    }
}
