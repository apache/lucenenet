using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard
{
    public interface ICommonQueryParserConfiguration
    {
        bool LowercaseExpandedTerms { get; set; }

        bool AllowLeadingWildcard { get; set; }

        bool EnablePositionIncrements { get; set; }

        MultiTermQuery.RewriteMethod MultiTermRewriteMethod { get; set; }

        int FuzzyPrefixLength { get; set; }

        CultureInfo Locale { get; set; }

        TimeZone TimeZone { get; set; }

        int PhraseSlop { get; set; }

        Analyzer Analyzer { get; }

        float FuzzyMinSim { get; set; }

        DateTools.Resolution DateResolution { get; set; }
    }
}
