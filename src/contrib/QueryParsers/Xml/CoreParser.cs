using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.QueryParsers.Xml.Builders;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Lucene.Net.QueryParsers.Xml
{
    public class CoreParser : IQueryBuilder
    {
        protected Analyzer analyzer;
        protected QueryParser parser;
        protected QueryBuilderFactory queryFactory;
        protected FilterBuilderFactory filterFactory;
        //Controls the max size of the LRU cache used for QueryFilter objects parsed.
        public static int maxNumCachedFilters = 20;

        public CoreParser(Analyzer analyzer, QueryParser parser)
            : this(null, analyzer, parser)
        {
        }

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
            return GetQuery(ParseXML(xmlStream).Root);
        }

        public void AddQueryBuilder(string nodeName, IQueryBuilder builder)
        {
            queryFactory.AddBuilder(nodeName, builder);
        }

        public void AddFilterBuilder(string nodeName, IFilterBuilder builder)
        {
            filterFactory.AddBuilder(nodeName, builder);
        }

        private static XDocument ParseXML(Stream pXmlFile) 
        {
            return XDocument.Load(pXmlFile);
        }
        
        public Query GetQuery(XElement e)
        {
            return queryFactory.GetQuery(e);
        }
    }
}
