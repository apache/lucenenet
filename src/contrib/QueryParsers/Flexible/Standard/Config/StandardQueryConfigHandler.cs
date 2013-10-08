using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
{
    public class StandardQueryConfigHandler : QueryConfigHandler
    {
        public static class ConfigurationKeys
        {
            public static readonly ConfigurationKey<bool> ENABLE_POSITION_INCREMENTS = ConfigurationKey.NewInstance<bool>();

            public static readonly ConfigurationKey<bool> LOWERCASE_EXPANDED_TERMS = ConfigurationKey.NewInstance<bool>();

            public static readonly ConfigurationKey<bool> ALLOW_LEADING_WILDCARD = ConfigurationKey.NewInstance<bool>();

            public static readonly ConfigurationKey<Analyzer> ANALYZER = ConfigurationKey.NewInstance<Analyzer>();

            public static readonly ConfigurationKey<Operator> DEFAULT_OPERATOR = ConfigurationKey.NewInstance<Operator>();

            public static readonly ConfigurationKey<int> PHRASE_SLOP = ConfigurationKey.NewInstance<int>();

            public static readonly ConfigurationKey<CultureInfo> LOCALE = ConfigurationKey.NewInstance<CultureInfo>();
    
            public static readonly ConfigurationKey<TimeZoneInfo> TIMEZONE = ConfigurationKey.NewInstance<TimeZoneInfo>();

            public static readonly ConfigurationKey<MultiTermQuery.RewriteMethod> MULTI_TERM_REWRITE_METHOD = ConfigurationKey.NewInstance<MultiTermQuery.RewriteMethod>();

            public static readonly ConfigurationKey<string[]> MULTI_FIELDS = ConfigurationKey.NewInstance<string[]>();

            public static readonly ConfigurationKey<IDictionary<string, float>> FIELD_BOOST_MAP = ConfigurationKey.NewInstance<IDictionary<string, float>>();

            public static readonly ConfigurationKey<IDictionary<string, DateTools.Resolution>> FIELD_DATE_RESOLUTION_MAP = ConfigurationKey.NewInstance<IDictionary<string, DateTools.Resolution>>();

            public static readonly ConfigurationKey<FuzzyConfig> FUZZY_CONFIG = ConfigurationKey.NewInstance<FuzzyConfig>();

            public static readonly ConfigurationKey<DateTools.Resolution> DATE_RESOLUTION = ConfigurationKey.NewInstance<DateTools.Resolution>();
    
            public static readonly ConfigurationKey<float> BOOST = ConfigurationKey.NewInstance<float>();

            public static readonly ConfigurationKey<NumericConfig> NUMERIC_CONFIG = ConfigurationKey.NewInstance<NumericConfig>();
    
            public static readonly ConfigurationKey<IDictionary<string, NumericConfig>> NUMERIC_CONFIG_MAP = ConfigurationKey.NewInstance<IDictionary<string, NumericConfig>>();
        }

        public enum Operator
        {
            AND, OR
        }

        public StandardQueryConfigHandler()
        {
            // Add listener that will build the FieldConfig.
            AddFieldConfigListener(new FieldBoostMapFCListener(this));
            AddFieldConfigListener(new FieldDateResolutionFCListener(this));
            AddFieldConfigListener(new NumericFieldConfigListener(this));

            // Default Values
            Set(ConfigurationKeys.ALLOW_LEADING_WILDCARD, false); // default in 2.9
            Set(ConfigurationKeys.ANALYZER, null); //default value 2.4
            Set(ConfigurationKeys.DEFAULT_OPERATOR, Operator.OR);
            Set(ConfigurationKeys.PHRASE_SLOP, 0); //default value 2.4
            Set(ConfigurationKeys.LOWERCASE_EXPANDED_TERMS, true); //default value 2.4
            Set(ConfigurationKeys.ENABLE_POSITION_INCREMENTS, false); //default value 2.4
            Set(ConfigurationKeys.FIELD_BOOST_MAP, new HashMap<string, float>());
            Set(ConfigurationKeys.FUZZY_CONFIG, new FuzzyConfig());
            Set(ConfigurationKeys.LOCALE, CultureInfo.CurrentCulture);
            Set(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);
            Set(ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP, new HashMap<string, DateTools.Resolution>());
        }
    }
}
