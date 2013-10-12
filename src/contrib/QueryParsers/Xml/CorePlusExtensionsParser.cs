using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.QueryParsers.Xml.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Xml
{
    public class CorePlusExtensionsParser : CoreParser
    {
        public CorePlusExtensionsParser(Analyzer analyzer, QueryParser parser)
            : this(null, analyzer, parser)
        {
        }

        public CorePlusExtensionsParser(string defaultField, Analyzer analyzer)
            : this(defaultField, analyzer, null)
        {
        }

        private CorePlusExtensionsParser(string defaultField, Analyzer analyzer, QueryParser parser)
            : base(defaultField, analyzer, parser)
        {
            filterFactory.AddBuilder("TermsFilter", new TermsFilterBuilder(analyzer));
            filterFactory.AddBuilder("BooleanFilter", new BooleanFilterBuilder(filterFactory));
            filterFactory.AddBuilder("DuplicateFilter", new DuplicateFilterBuilder());
            string[] fields = { "contents" };
            queryFactory.AddBuilder("LikeThisQuery", new LikeThisQueryBuilder(analyzer, fields));
            queryFactory.AddBuilder("BoostingQuery", new BoostingQueryBuilder(queryFactory));
            queryFactory.AddBuilder("FuzzyLikeThisQuery", new FuzzyLikeThisQueryBuilder(analyzer));
        }
    }
}
