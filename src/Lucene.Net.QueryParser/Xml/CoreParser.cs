/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Javax.Xml.Parsers;
using Lucene.Net.Analysis;
using Lucene.Net.Queryparser.Classic;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml
{
	/// <summary>Assembles a QueryBuilder which uses only core Lucene Query objects</summary>
	public class CoreParser : QueryBuilder
	{
		protected internal Analyzer analyzer;

		protected internal QueryParser parser;

		protected internal QueryBuilderFactory queryFactory;

		protected internal FilterBuilderFactory filterFactory;

		public static int maxNumCachedFilters = 20;

		/// <summary>
		/// Construct an XML parser that uses a single instance QueryParser for handling
		/// UserQuery tags - all parse operations are synchronised on this parser
		/// </summary>
		/// <param name="parser">A QueryParser which will be synchronized on during parse calls.
		/// 	</param>
		public CoreParser(Analyzer analyzer, QueryParser parser) : this(null, analyzer, parser
			)
		{
		}

		/// <summary>Constructs an XML parser that creates a QueryParser for each UserQuery request.
		/// 	</summary>
		/// <remarks>Constructs an XML parser that creates a QueryParser for each UserQuery request.
		/// 	</remarks>
		/// <param name="defaultField">The default field name used by QueryParsers constructed for UserQuery tags
		/// 	</param>
		public CoreParser(string defaultField, Analyzer analyzer) : this(defaultField, analyzer
			, null)
		{
		}

		protected internal CoreParser(string defaultField, Analyzer analyzer, QueryParser
			 parser)
		{
			//Controls the max size of the LRU cache used for QueryFilter objects parsed.
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
			queryFactory.AddBuilder("DisjunctionMaxQuery", new DisjunctionMaxQueryBuilder(queryFactory
				));
			if (parser != null)
			{
				queryFactory.AddBuilder("UserQuery", new UserInputQueryBuilder(parser));
			}
			else
			{
				queryFactory.AddBuilder("UserQuery", new UserInputQueryBuilder(defaultField, analyzer
					));
			}
			queryFactory.AddBuilder("FilteredQuery", new FilteredQueryBuilder(filterFactory, 
				queryFactory));
			queryFactory.AddBuilder("ConstantScoreQuery", new ConstantScoreQueryBuilder(filterFactory
				));
			filterFactory.AddBuilder("CachedFilter", new CachedFilterBuilder(queryFactory, filterFactory
				, maxNumCachedFilters));
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

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Query Parse(InputStream xmlStream)
		{
			return GetQuery(ParseXML(xmlStream).GetDocumentElement());
		}

		public virtual void AddQueryBuilder(string nodeName, QueryBuilder builder)
		{
			queryFactory.AddBuilder(nodeName, builder);
		}

		public virtual void AddFilterBuilder(string nodeName, FilterBuilder builder)
		{
			filterFactory.AddBuilder(nodeName, builder);
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		private static Document ParseXML(InputStream pXmlFile)
		{
			DocumentBuilderFactory dbf = DocumentBuilderFactory.NewInstance();
			DocumentBuilder db = null;
			try
			{
				db = dbf.NewDocumentBuilder();
			}
			catch (Exception se)
			{
				throw new ParserException("XML Parser configuration error", se);
			}
			Document doc = null;
			try
			{
				doc = db.Parse(pXmlFile);
			}
			catch (Exception se)
			{
				throw new ParserException("Error parsing XML stream:" + se, se);
			}
			return doc;
		}

		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
		{
			return queryFactory.GetQuery(e);
		}
	}
}
