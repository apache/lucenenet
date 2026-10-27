using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.QueryParsers.Xml.Builders;
using Lucene.Net.Search;
using System;
using System.IO;
using System.Net;
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

        /// <summary>Controls the max size of the LRU cache used for QueryFilter objects parsed.</summary>
        // LUCENENET specific - changed from a mutable static field into a property.
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public static int MaxNumCachedFilters { get; set; } = 20;

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
                m_filterFactory, MaxNumCachedFilters));

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

        /// <summary>
        /// Returns the <see cref="XmlResolver"/> to be used when parsing XML from a <see cref="Stream"/>
        /// or <see cref="TextReader"/>. By default this returns <see cref="DisallowExternalEntityResolver"/>,
        /// which disallows the expansion of external entities (for security reasons). To restore legacy
        /// behavior, override this method to return an <see cref="XmlUrlResolver"/>.
        /// </summary>
        // LUCENENET: Corresponds to getEntityResolver() in Lucene 7.1+ (SOLR-11477)
        protected virtual XmlResolver GetXmlResolver()
        {
            return DisallowExternalEntityResolver;
        }

        /// <summary>
        /// Returns the <see cref="XmlReaderSettings"/> to be used when parsing XML from a <see cref="Stream"/>
        /// or <see cref="TextReader"/>. By default, internal DTDs are allowed, external entities are resolved
        /// with <see cref="GetXmlResolver()"/>, and the number of characters produced by expanding entities is limited
        /// to guard against entity expansion attacks. Override this method to customize these settings.
        /// <para/>
        /// Note that these settings do not apply to <see cref="Parse(XmlReader)"/> or <see cref="Parse(XmlDocument)"/>,
        /// since the caller is responsible for the settings of the supplied <see cref="XmlReader"/> or <see cref="XmlDocument"/>.
        /// </summary>
        // LUCENENET specific - .NET equivalent of FEATURE_SECURE_PROCESSING in Lucene 7.1+ (SOLR-11477)
        protected virtual XmlReaderSettings GetXmlReaderSettings()
        {
            return DOMUtils.CreateXmlReaderSettings(GetXmlResolver());
        }

        private XmlDocument ParseXML(Stream pXmlFile) // LUCENENET: No longer static, matching Lucene 7.1+
        {
            try
            {
                using XmlReader reader = XmlReader.Create(pXmlFile, GetXmlReaderSettings());
                return DOMUtils.LoadXML(reader, GetXmlResolver());
            }
            catch (Exception se) // LUCENENET: No need to call the IsException() extension method here because we are dealing only with a .NET platform method
            {
                throw new ParserException("Error parsing XML stream:" + se, se);
            }
        }

        // LUCENENET specific overload for TextReader
        private XmlDocument ParseXML(TextReader pXmlFile)
        {
            try
            {
                using XmlReader reader = XmlReader.Create(pXmlFile, GetXmlReaderSettings());
                return DOMUtils.LoadXML(reader, GetXmlResolver());
            }
            catch (Exception se) // LUCENENET: No need to call the IsException() extension method here because we are dealing only with a .NET platform method
            {
                throw new ParserException("Error parsing XML stream:" + se, se);
            }
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

        /// <summary>
        /// An <see cref="XmlResolver"/> that disallows the resolution of external entities
        /// (including external DTDs) by throwing an <see cref="XmlException"/>.
        /// </summary>
        // LUCENENET: Corresponds to DISALLOW_EXTERNAL_ENTITY_RESOLVER in Lucene 7.1+ (SOLR-11477)
        public static XmlResolver DisallowExternalEntityResolver { get; } = new DisallowExternalEntityXmlResolver();

        private sealed class DisallowExternalEntityXmlResolver : XmlResolver
        {
            public override ICredentials Credentials
            {
                set { }
            }

            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                throw new XmlException($"External Entity resolving unsupported:  systemId=\"{absoluteUri}\"");
            }
        }
    }
}
