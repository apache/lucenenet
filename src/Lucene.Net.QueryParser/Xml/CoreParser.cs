using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.QueryParsers.Xml.Builders;
using Lucene.Net.Search;
using System;
using System.IO;
using System.Xml;

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
    /// Assembles a <see cref="IQueryBuilder"/> which uses only core Lucene Query objects
    /// </summary>
    public class CoreParser : IQueryBuilder
    {
        protected Analyzer analyzer;
        protected QueryParser parser;
        protected QueryBuilderFactory queryFactory;
        protected FilterBuilderFactory filterFactory;
        //Controls the max size of the LRU cache used for QueryFilter objects parsed.
        public static int maxNumCachedFilters = 20;

        /// <summary>
        /// Construct an XML parser that uses a single instance <see cref="QueryParser"/> for handling
        /// UserQuery tags - all parse operations are synchronised on this parser
        /// </summary>
        /// <param name="analyzer"></param>
        /// <param name="parser">A <see cref="QueryParser"/> which will be synchronized on during parse calls.</param>
        public CoreParser(Analyzer analyzer, QueryParser parser)
            : this(null, analyzer, parser)
        {
        }

        /// <summary>
        /// Constructs an XML parser that creates a <see cref="QueryParser"/> for each UserQuery request.
        /// </summary>
        /// <param name="defaultField">The default field name used by <see cref="QueryParser"/>s constructed for UserQuery tags</param>
        /// <param name="analyzer"></param>
        public CoreParser(string defaultField, Analyzer analyzer)
            : this(defaultField, analyzer, null)
        {
        }

        protected CoreParser(string defaultField, Analyzer analyzer, QueryParser parser)
        {
            this.analyzer = analyzer;
            this.parser = parser;
            filterFactory = new FilterBuilderFactory();
            filterFactory.AddBuilder("RangeFilter", new RangeFilterBuilder());
            filterFactory.AddBuilder("NumericRangeFilter", new NumericRangeFilterBuilder());

            queryFactory = new QueryBuilderFactory();
            queryFactory.AddBuilder("TermQuery", new TermQueryBuilder());
            queryFactory.AddBuilder("TermsQuery", new TermsQueryBuilder(analyzer));
            queryFactory.AddBuilder("MatchAllDocsQuery", new MatchAllDocsQueryBuilder());
            queryFactory.AddBuilder("BooleanQuery", new BooleanQueryBuilder(queryFactory));
            queryFactory.AddBuilder("NumericRangeQuery", new NumericRangeQueryBuilder());
            queryFactory.AddBuilder("DisjunctionMaxQuery", new DisjunctionMaxQueryBuilder(queryFactory));
            if (parser != null)
            {
                queryFactory.AddBuilder("UserQuery", new UserInputQueryBuilder(parser));
            }
            else
            {
                queryFactory.AddBuilder("UserQuery", new UserInputQueryBuilder(defaultField, analyzer));
            }
            queryFactory.AddBuilder("FilteredQuery", new FilteredQueryBuilder(filterFactory, queryFactory));
            queryFactory.AddBuilder("ConstantScoreQuery", new ConstantScoreQueryBuilder(filterFactory));

            filterFactory.AddBuilder("CachedFilter", new CachedFilterBuilder(queryFactory,
                filterFactory, maxNumCachedFilters));

            SpanQueryBuilderFactory sqof = new SpanQueryBuilderFactory();

            SpanNearBuilder snb = new SpanNearBuilder(sqof);
            sqof.AddBuilder("SpanNear", snb);
            queryFactory.AddBuilder("SpanNear", snb);

            BoostingTermBuilder btb = new BoostingTermBuilder();
            sqof.AddBuilder("BoostingTermQuery", btb);
            queryFactory.AddBuilder("BoostingTermQuery", btb);

            SpanTermBuilder snt = new SpanTermBuilder();
            sqof.AddBuilder("SpanTerm", snt);
            queryFactory.AddBuilder("SpanTerm", snt);

            SpanOrBuilder sot = new SpanOrBuilder(sqof);
            sqof.AddBuilder("SpanOr", sot);
            queryFactory.AddBuilder("SpanOr", sot);

            SpanOrTermsBuilder sots = new SpanOrTermsBuilder(analyzer);
            sqof.AddBuilder("SpanOrTerms", sots);
            queryFactory.AddBuilder("SpanOrTerms", sots);

            SpanFirstBuilder sft = new SpanFirstBuilder(sqof);
            sqof.AddBuilder("SpanFirst", sft);
            queryFactory.AddBuilder("SpanFirst", sft);

            SpanNotBuilder snot = new SpanNotBuilder(sqof);
            sqof.AddBuilder("SpanNot", snot);
            queryFactory.AddBuilder("SpanNot", snot);
        }

        public Query Parse(Stream xmlStream)
        {
            return GetQuery(ParseXML(xmlStream).DocumentElement);
        }

        public void AddQueryBuilder(string nodeName, IQueryBuilder builder)
        {
            queryFactory.AddBuilder(nodeName, builder);
        }

        public void AddFilterBuilder(string nodeName, IFilterBuilder builder)
        {
            filterFactory.AddBuilder(nodeName, builder);
        }

        private static XmlDocument ParseXML(Stream pXmlFile)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(pXmlFile);
            }
            catch (Exception se)
            {
                throw new ParserException("Error parsing XML stream:" + se, se);
            }
            return doc;
        }

        // LUCENENET specific overload for TextReader
        private static XmlDocument ParseXML(TextReader pXmlFile)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(pXmlFile);
            }
            catch (Exception se)
            {
                throw new ParserException("Error parsing XML stream:" + se, se);
            }
            return doc;
        }

        // LUCENENET specific overload for XmlReader
        private static XmlDocument ParseXML(XmlReader pXmlFile)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(pXmlFile);
            }
            catch (Exception se)
            {
                throw new ParserException("Error parsing XML stream:" + se, se);
            }
            return doc;
        }

        public virtual Query GetQuery(XmlElement e)
        {
            return queryFactory.GetQuery(e);
        }
    }
}
