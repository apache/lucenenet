using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Benchmarks.Quality.Utils
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
    /// Simplistic quality query parser. A Lucene query is created by passing 
    /// the value of the specified <see cref="QualityQuery"/> name-value pair(s) into 
    /// a Lucene's <see cref="QueryParser"/> using <see cref="StandardAnalyzer"/>.
    /// </summary>
    public class SimpleQQParser : IQualityQueryParser
    {
        private readonly string[] qqNames;
        private readonly string indexField;
        private readonly DisposableThreadLocal<QueryParser> queryParser = new DisposableThreadLocal<QueryParser>();

        /// <summary>
        /// Constructor of a simple qq parser.
        /// </summary>
        /// <param name="qqNames">Name-value pairs of quality query to use for creating the query.</param>
        /// <param name="indexField">Corresponding index field.</param>
        public SimpleQQParser(string[] qqNames, string indexField)
        {
            this.qqNames = qqNames;
            this.indexField = indexField;
        }

        /// <summary>
        /// Constructor of a simple qq parser.
        /// </summary>
        /// <param name="qqName">Name-value pair of quality query to use for creating the query.</param>
        /// <param name="indexField">Corresponding index field.</param>
        public SimpleQQParser(string qqName, string indexField)
            : this(new string[] { qqName }, indexField)
        {
        }

        /// <seealso cref="IQualityQueryParser.Parse(QualityQuery)"/>
        public virtual Query Parse(QualityQuery qq)
        {
            QueryParser qp = queryParser.Value;
            if (qp is null)
            {
#pragma warning disable 612, 618
                qp = new QueryParser(LuceneVersion.LUCENE_CURRENT, indexField, new StandardAnalyzer(LuceneVersion.LUCENE_CURRENT));
#pragma warning restore 612, 618
                queryParser.Value = qp;
            }
            BooleanQuery bq = new BooleanQuery();
            for (int i = 0; i < qqNames.Length; i++)
                bq.Add(qp.Parse(QueryParserBase.Escape(qq.GetValue(qqNames[i]))), Occur.SHOULD);

            return bq;
        }
    }
}
