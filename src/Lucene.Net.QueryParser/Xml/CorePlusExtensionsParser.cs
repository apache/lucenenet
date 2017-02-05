using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.QueryParsers.Xml.Builders;

namespace Lucene.Net.QueryParsers.Xml
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
    /// Assembles a <see cref="Util.QueryBuilder"/> which uses <see cref="Search.Query"/> objects from
    /// Lucene's <c>sandbox</c> and <c>queries</c>
    /// modules in addition to core queries.
    /// </summary>
    public class CorePlusExtensionsParser : CoreParser
    {
        /// <summary>
        /// Construct an XML parser that uses a single instance <see cref="QueryParser"/> for handling
        /// UserQuery tags - all parse operations are synchronized on this parser
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="parser">A <see cref="QueryParser"/> which will be synchronized on during parse calls.</param>
        public CorePlusExtensionsParser(Analyzer analyzer, QueryParser parser)
            : this(null, analyzer, parser)
        {
        }

        /// <summary>
        /// Constructs an XML parser that creates a <see cref="QueryParser"/> for each UserQuery request.
        /// </summary>
        /// <param name="defaultField">The default field name used by <see cref="QueryParser"/>s constructed for UserQuery tags</param>
        /// <param name="analyzer"></param>
        public CorePlusExtensionsParser(string defaultField, Analyzer analyzer)
            : this(defaultField, analyzer, null)
        {
        }

        private CorePlusExtensionsParser(string defaultField, Analyzer analyzer, QueryParser parser)
            : base(defaultField, analyzer, parser)
        {
            m_filterFactory.AddBuilder("TermsFilter", new TermsFilterBuilder(analyzer));
            m_filterFactory.AddBuilder("BooleanFilter", new BooleanFilterBuilder(m_filterFactory));
            m_filterFactory.AddBuilder("DuplicateFilter", new DuplicateFilterBuilder());
            string[] fields = { "contents" };
            m_queryFactory.AddBuilder("LikeThisQuery", new LikeThisQueryBuilder(analyzer, fields));
            m_queryFactory.AddBuilder("BoostingQuery", new BoostingQueryBuilder(m_queryFactory));
            m_queryFactory.AddBuilder("FuzzyLikeThisQuery", new FuzzyLikeThisQueryBuilder(analyzer));
        }
    }
}
