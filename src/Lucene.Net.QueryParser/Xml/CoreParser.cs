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
        protected Analyzer m_analyzer;
        protected QueryParser m_parser;
        protected QueryBuilderFactory m_queryFactory;
        protected FilterBuilderFactory m_filterFactory;
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
            this.m_analyzer = analyzer;
            this.m_parser = parser;
            m_filterFactory = new FilterBuilderFactory();
            m_filterFactory.AddBuilder("RangeFilter", new RangeFilterBuilder());
            m_filterFactory.AddBuilder("NumericRangeFilter", new NumericRangeFilterBuilder());

            m_queryFactory = new QueryBuilderFactory();
            m_queryFactory.AddBuilder("TermQuery", new TermQueryBuilder());
            m_queryFactory.AddBuilder("TermsQuery", new TermsQueryBuilder(analyzer));
            m_queryFactory.AddBuilder("MatchAllDocsQuery", new MatchAllDocsQueryBuilder());
            m_queryFactory.AddBuilder("BooleanQuery", new BooleanQueryBuilder(m_queryFactory));
            m_queryFactory.AddBuilder("NumericRangeQuery", new NumericRangeQueryBuilder());
            m_queryFactory.AddBuilder("DisjunctionMaxQuery", new DisjunctionMaxQueryBuilder(m_queryFactory));
            if (parser != null)
            {
                m_queryFactory.AddBuilder("UserQuery", new UserInputQueryBuilder(parser));
            }
            else
            {
                m_queryFactory.AddBuilder("UserQuery", new UserInputQueryBuilder(defaultField, analyzer));
            }
            m_queryFactory.AddBuilder("FilteredQuery", new FilteredQueryBuilder(m_filterFactory, m_queryFactory));
            m_queryFactory.AddBuilder("ConstantScoreQuery", new ConstantScoreQueryBuilder(m_filterFactory));

            m_filterFactory.AddBuilder("CachedFilter", new CachedFilterBuilder(m_queryFactory,
                m_filterFactory, maxNumCachedFilters));

            SpanQueryBuilderFactory sqof = new SpanQueryBuilderFactory();

            SpanNearBuilder snb = new SpanNearBuilder(sqof);
            sqof.AddBuilder("SpanNear", snb);
            m_queryFactory.AddBuilder("SpanNear", snb);

            BoostingTermBuilder btb = new BoostingTermBuilder();
            sqof.AddBuilder("BoostingTermQuery", btb);
            m_queryFactory.AddBuilder("BoostingTermQuery", btb);

            SpanTermBuilder snt = new SpanTermBuilder();
            sqof.AddBuilder("SpanTerm", snt);
            m_queryFactory.AddBuilder("SpanTerm", snt);

            SpanOrBuilder sot = new SpanOrBuilder(sqof);
            sqof.AddBuilder("SpanOr", sot);
            m_queryFactory.AddBuilder("SpanOr", sot);

            SpanOrTermsBuilder sots = new SpanOrTermsBuilder(analyzer);
            sqof.AddBuilder("SpanOrTerms", sots);
            m_queryFactory.AddBuilder("SpanOrTerms", sots);

            SpanFirstBuilder sft = new SpanFirstBuilder(sqof);
            sqof.AddBuilder("SpanFirst", sft);
            m_queryFactory.AddBuilder("SpanFirst", sft);

            SpanNotBuilder snot = new SpanNotBuilder(sqof);
            sqof.AddBuilder("SpanNot", snot);
            m_queryFactory.AddBuilder("SpanNot", snot);
        }

        public virtual Query Parse(Stream xmlStream)
        {
            return GetQuery(ParseXML(xmlStream).DocumentElement);
        }

        // LUCENENET specific overload for TextReader
        public virtual Query Parse(TextReader xmlTextReader)
        {
            return GetQuery(ParseXML(xmlTextReader).DocumentElement);
        }

        // LUCENENET specific overload for XmlReader
        public virtual Query Parse(XmlReader xmlReader)
        {
            return GetQuery(ParseXML(xmlReader).DocumentElement);
        }

        // LUCENENET specific overload for XmlDocument
        public virtual Query Parse(XmlDocument xmlDocument)
        {
            return GetQuery(xmlDocument.DocumentElement);
        }

        public virtual void AddQueryBuilder(string nodeName, IQueryBuilder builder)
        {
            m_queryFactory.AddBuilder(nodeName, builder);
        }

        public virtual void AddFilterBuilder(string nodeName, IFilterBuilder builder)
        {
            m_filterFactory.AddBuilder(nodeName, builder);
        }

        private static XmlDocument ParseXML(Stream pXmlFile)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(pXmlFile);
            }
            catch (Exception se) // LUCENENET: No need to call the IsException() extension method here because we are dealing only with a .NET platform method
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
            catch (Exception se) // LUCENENET: No need to call the IsException() extension method here because we are dealing only with a .NET platform method
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
            catch (Exception se) // LUCENENET: No need to call the IsException() extension method here because we are dealing only with a .NET platform method
            {
                throw new ParserException("Error parsing XML stream:" + se, se);
            }
            return doc;
        }

        public virtual Query GetQuery(XmlElement e)
        {
            return m_queryFactory.GetQuery(e);
        }
    }
}
