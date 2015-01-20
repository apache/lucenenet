/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Queryparser.Classic;
using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml
{
	/// <summary>
	/// Assembles a QueryBuilder which uses Query objects from
	/// Lucene's <code>sandbox</code> and <code>queries</code>
	/// modules in addition to core queries.
	/// </summary>
	/// <remarks>
	/// Assembles a QueryBuilder which uses Query objects from
	/// Lucene's <code>sandbox</code> and <code>queries</code>
	/// modules in addition to core queries.
	/// </remarks>
	public class CorePlusExtensionsParser : CoreParser
	{
		/// <summary>
		/// Construct an XML parser that uses a single instance QueryParser for handling
		/// UserQuery tags - all parse operations are synchronized on this parser
		/// </summary>
		/// <param name="parser">A QueryParser which will be synchronized on during parse calls.
		/// 	</param>
		public CorePlusExtensionsParser(Analyzer analyzer, QueryParser parser) : this(null
			, analyzer, parser)
		{
		}

		/// <summary>Constructs an XML parser that creates a QueryParser for each UserQuery request.
		/// 	</summary>
		/// <remarks>Constructs an XML parser that creates a QueryParser for each UserQuery request.
		/// 	</remarks>
		/// <param name="defaultField">The default field name used by QueryParsers constructed for UserQuery tags
		/// 	</param>
		public CorePlusExtensionsParser(string defaultField, Analyzer analyzer) : this(defaultField
			, analyzer, null)
		{
		}

		protected internal CorePlusExtensionsParser(string defaultField, Analyzer analyzer
			, QueryParser parser) : base(defaultField, analyzer, parser)
		{
			filterFactory.AddBuilder("TermsFilter", new TermsFilterBuilder(analyzer));
			filterFactory.AddBuilder("BooleanFilter", new BooleanFilterBuilder(filterFactory)
				);
			filterFactory.AddBuilder("DuplicateFilter", new DuplicateFilterBuilder());
			string[] fields = new string[] { "contents" };
			queryFactory.AddBuilder("LikeThisQuery", new LikeThisQueryBuilder(analyzer, fields
				));
			queryFactory.AddBuilder("BoostingQuery", new BoostingQueryBuilder(queryFactory));
			queryFactory.AddBuilder("FuzzyLikeThisQuery", new FuzzyLikeThisQueryBuilder(analyzer
				));
		}
	}
}
