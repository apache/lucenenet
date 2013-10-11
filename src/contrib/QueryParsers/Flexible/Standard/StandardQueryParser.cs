using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Standard.Builders;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigurationKeys = Lucene.Net.QueryParsers.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys;

namespace Lucene.Net.QueryParsers.Flexible.Standard
{
    public class StandardQueryParser : QueryParserHelper, ICommonQueryParserConfiguration
    {
        public StandardQueryParser()
            : base(new StandardQueryConfigHandler(), new StandardSyntaxParser(),
                new StandardQueryNodeProcessorPipeline(null),
                new StandardQueryTreeBuilder())
        {
            EnablePositionIncrements = true;
        }

        public StandardQueryParser(Analyzer analyzer)
            : this()
        {
            this.Analyzer = analyzer;
        }

        public override string ToString()
        {
            return "<StandardQueryParser config=\"" + this.QueryConfigHandler + "\"/>";
        }

        public new Query Parse(string query, string defaultField)
        {
            return (Query)base.Parse(query, defaultField);
        }

        public StandardQueryConfigHandler.Operator? DefaultOperator
        {
            get { return QueryConfigHandler.Get(ConfigurationKeys.DEFAULT_OPERATOR); }
            set { QueryConfigHandler.Set(ConfigurationKeys.DEFAULT_OPERATOR, value); }
        }

        public bool LowercaseExpandedTerms
        {
            get
            {
                bool? lowercaseExpandedTerms = QueryConfigHandler.Get(ConfigurationKeys.LOWERCASE_EXPANDED_TERMS);

                if (lowercaseExpandedTerms == null)
                {
                    return true;
                }
                else
                {
                    return lowercaseExpandedTerms.Value;
                }
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.LOWERCASE_EXPANDED_TERMS, value);
            }
        }

        public bool AllowLeadingWildcard
        {
            get
            {
                bool? allowLeadingWildcard = QueryConfigHandler.Get(ConfigurationKeys.ALLOW_LEADING_WILDCARD);

                if (allowLeadingWildcard == null)
                {
                    return false;
                }
                else
                {
                    return allowLeadingWildcard.Value;
                }
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.ALLOW_LEADING_WILDCARD, value);
            }
        }

        public bool EnablePositionIncrements
        {
            get
            {
                bool? enablePositionIncrements = QueryConfigHandler.Get(ConfigurationKeys.ENABLE_POSITION_INCREMENTS);

                if (enablePositionIncrements == null)
                {
                    return false;
                }
                else
                {
                    return enablePositionIncrements.Value;
                }
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.ENABLE_POSITION_INCREMENTS, value);
            }
        }

        public MultiTermQuery.RewriteMethod MultiTermRewriteMethod
        {
            get
            {
                return QueryConfigHandler.Get(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD);
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD, value);
            }
        }

        public string[] MultiFields
        {
            get
            {
                return QueryConfigHandler.Get(ConfigurationKeys.MULTI_FIELDS);
            }
            set
            {
                if (value == null)
                {
                    value = new string[0];
                }

                QueryConfigHandler.Set(ConfigurationKeys.MULTI_FIELDS, value);
            }
        }

        public int FuzzyPrefixLength
        {
            get
            {
                FuzzyConfig fuzzyConfig = QueryConfigHandler.Get(ConfigurationKeys.FUZZY_CONFIG);

                if (fuzzyConfig == null)
                {
                    return FuzzyQuery.defaultPrefixLength;
                }
                else
                {
                    return fuzzyConfig.PrefixLength;
                }
            }
            set
            {
                var config = QueryConfigHandler;
                FuzzyConfig fuzzyConfig = config.Get(StandardQueryConfigHandler.ConfigurationKeys.FUZZY_CONFIG);

                if (fuzzyConfig == null)
                {
                    fuzzyConfig = new FuzzyConfig();
                    config.Set(StandardQueryConfigHandler.ConfigurationKeys.FUZZY_CONFIG, fuzzyConfig);
                }

                fuzzyConfig.PrefixLength = value;
            }
        }

        public IDictionary<string, NumericConfig> NumericConfigMap
        {
            get
            {
                return QueryConfigHandler.Get(ConfigurationKeys.NUMERIC_CONFIG_MAP);
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.NUMERIC_CONFIG_MAP, value);
            }
        }

        public CultureInfo Locale
        {
            get
            {
                return QueryConfigHandler.Get(ConfigurationKeys.LOCALE);
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.LOCALE, value);
            }
        }

        public TimeZoneInfo TimeZone
        {
            get
            {
                return QueryConfigHandler.Get(ConfigurationKeys.TIMEZONE);
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.TIMEZONE, value);
            }
        }

        [Obsolete]
        public int DefaultPhraseSlop
        {
            get
            {
                return QueryConfigHandler.Get(ConfigurationKeys.PHRASE_SLOP).GetValueOrDefault();
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.PHRASE_SLOP, value);
            }
        }

        public int PhraseSlop
        {
            get
            {
                return QueryConfigHandler.Get(ConfigurationKeys.PHRASE_SLOP).GetValueOrDefault();
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.PHRASE_SLOP, value);
            }
        }

        public Analyzer Analyzer
        {
            get { return QueryConfigHandler.Get(ConfigurationKeys.ANALYZER); }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.ANALYZER, value);
            }
        }

        public float FuzzyMinSim
        {
            get
            {
                FuzzyConfig fuzzyConfig = QueryConfigHandler.Get(ConfigurationKeys.FUZZY_CONFIG);

                if (fuzzyConfig == null)
                {
                    return FuzzyQuery.defaultMinSimilarity;
                }
                else
                {
                    return fuzzyConfig.MinSimilarity;
                }
            }
            set
            {
                var config = QueryConfigHandler;
                FuzzyConfig fuzzyConfig = config.Get(ConfigurationKeys.FUZZY_CONFIG);

                if (fuzzyConfig == null)
                {
                    fuzzyConfig = new FuzzyConfig();
                    config.Set(ConfigurationKeys.FUZZY_CONFIG, fuzzyConfig);
                }

                fuzzyConfig.MinSimilarity = value;
            }
        }

        public IDictionary<string, float> FieldsBoost
        {
            get
            {
                return QueryConfigHandler.Get(ConfigurationKeys.FIELD_BOOST_MAP);
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.FIELD_BOOST_MAP, value);
            }
        }

        public DateTools.Resolution DateResolution
        {
            get
            {
                return QueryConfigHandler.Get(ConfigurationKeys.DATE_RESOLUTION);
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.DATE_RESOLUTION, value);
            }
        }

        public IDictionary<string, DateTools.Resolution> DateResolutionMap
        {
            get
            {
                return QueryConfigHandler.Get(ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP);
            }
            set
            {
                QueryConfigHandler.Set(ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP, value);
            }
        }
    }
}
