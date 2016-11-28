using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Builders;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard
{
    public class StandardQueryParser : QueryParserHelper, ICommonQueryParserConfiguration
    {
        
        /**
* Constructs a {@link StandardQueryParser} object.
*/
        public StandardQueryParser()
            : base(new StandardQueryConfigHandler(), new StandardSyntaxParser(),
                new StandardQueryNodeProcessorPipeline(null),
                new StandardQueryTreeBuilder())
        {
            EnablePositionIncrements = true;
        }

        /**
         * Constructs a {@link StandardQueryParser} object and sets an
         * {@link Analyzer} to it. The same as:
         * 
         * <ul>
         * StandardQueryParser qp = new StandardQueryParser();
         * qp.getQueryConfigHandler().setAnalyzer(analyzer);
         * </ul>
         * 
         * @param analyzer
         *          the analyzer to be used by this query parser helper
         */
        public StandardQueryParser(Analyzer analyzer)
            : this()
        {
            this.Analyzer = analyzer;
        }


        public override string ToString()
        {
            return "<StandardQueryParser config=\"" + this.GetQueryConfigHandler()
                + "\"/>";
        }

        /**
         * Overrides {@link QueryParserHelper#parse(String, String)} so it casts the
         * return object to {@link Query}. For more reference about this method, check
         * {@link QueryParserHelper#parse(String, String)}.
         * 
         * @param query
         *          the query string
         * @param defaultField
         *          the default field used by the text parser
         * 
         * @return the object built from the query
         * 
         * @throws QueryNodeException
         *           if something wrong happens along the three phases
         */
        public override /*Query*/ object Parse(string query, string defaultField) // LUCENENET TODO: Can this be return type Query?
        {

            return (Query)base.Parse(query, defaultField);

        }

        /**
         * Gets implicit operator setting, which will be either {@link Operator#AND}
         * or {@link Operator#OR}.
         */
        public Operator? GetDefaultOperator()
        {
            return GetQueryConfigHandler().Get(ConfigurationKeys.DEFAULT_OPERATOR);
        }

        /**
         * Sets the boolean operator of the QueryParser. In default mode (
         * {@link Operator#OR}) terms without any modifiers are considered optional:
         * for example <code>capital of Hungary</code> is equal to
         * <code>capital OR of OR Hungary</code>.<br/>
         * In {@link Operator#AND} mode terms are considered to be in conjunction: the
         * above mentioned query is parsed as <code>capital AND of AND Hungary</code>
         */
        public void SetDefaultOperator(Operator @operator)
        {
            GetQueryConfigHandler().Set(ConfigurationKeys.DEFAULT_OPERATOR, @operator);
        }


        public bool LowercaseExpandedTerms
        {
            get
            {
                bool? lowercaseExpandedTerms = GetQueryConfigHandler().Get(ConfigurationKeys.LOWERCASE_EXPANDED_TERMS);
                return lowercaseExpandedTerms.HasValue ? lowercaseExpandedTerms.Value : true;
            }
            set
            {
                GetQueryConfigHandler().Set(ConfigurationKeys.LOWERCASE_EXPANDED_TERMS, value);
            }
        }

        ///**
        // * Set to <code>true</code> to allow leading wildcard characters.
        // * <p>
        // * When set, <code>*</code> or <code>?</code> are allowed as the first
        // * character of a PrefixQuery and WildcardQuery. Note that this can produce
        // * very slow queries on big indexes.
        // * <p>
        // * Default: false.
        // */

        //public override void SetLowercaseExpandedTerms(bool lowercaseExpandedTerms)
        //{
        //    GetQueryConfigHandler().Set(ConfigurationKeys.LOWERCASE_EXPANDED_TERMS, lowercaseExpandedTerms);
        //}

        ///**
        // * @see #setLowercaseExpandedTerms(boolean)
        // */

        //public override bool GetLowercaseExpandedTerms()
        //{
        //    bool? lowercaseExpandedTerms = GetQueryConfigHandler().Get(ConfigurationKeys.LOWERCASE_EXPANDED_TERMS);

        //    if (lowercaseExpandedTerms == null)
        //    {
        //        return true;

        //    }
        //    else
        //    {
        //        return lowercaseExpandedTerms.Value;
        //    }

        //}

        public bool AllowLeadingWildcard
        {
            get
            {
                bool? allowLeadingWildcard = GetQueryConfigHandler().Get(ConfigurationKeys.ALLOW_LEADING_WILDCARD);
                return allowLeadingWildcard.HasValue ? allowLeadingWildcard.Value : false;
            }
            set
            {
                GetQueryConfigHandler().Set(ConfigurationKeys.ALLOW_LEADING_WILDCARD, value);
            }
        }

        ///**
        // * Set to <code>true</code> to allow leading wildcard characters.
        // * <p>
        // * When set, <code>*</code> or <code>?</code> are allowed as the first
        // * character of a PrefixQuery and WildcardQuery. Note that this can produce
        // * very slow queries on big indexes.
        // * <p>
        // * Default: false.
        // */

        //public override void SetAllowLeadingWildcard(bool allowLeadingWildcard)
        //{
        //    GetQueryConfigHandler().Set(ConfigurationKeys.ALLOW_LEADING_WILDCARD, allowLeadingWildcard);
        //}

        public bool EnablePositionIncrements
        {
            get
            {
                bool? enablePositionsIncrements = GetQueryConfigHandler().Get(ConfigurationKeys.ENABLE_POSITION_INCREMENTS);
                return enablePositionsIncrements.HasValue ? enablePositionsIncrements.Value : false;
            }
            set
            {
                GetQueryConfigHandler().Set(ConfigurationKeys.ENABLE_POSITION_INCREMENTS, value);
            }
        }

        ///**
        // * Set to <code>true</code> to enable position increments in result query.
        // * <p>
        // * When set, result phrase and multi-phrase queries will be aware of position
        // * increments. Useful when e.g. a StopFilter increases the position increment
        // * of the token that follows an omitted token.
        // * <p>
        // * Default: false.
        // */

        //public override void SetEnablePositionIncrements(bool enabled)
        //{
        //    GetQueryConfigHandler().Set(ConfigurationKeys.ENABLE_POSITION_INCREMENTS, enabled);
        //}

        ///**
        // * @see #setEnablePositionIncrements(boolean)
        // */

        //public override bool GetEnablePositionIncrements()
        //{
        //    bool? enablePositionsIncrements = GetQueryConfigHandler().Get(ConfigurationKeys.ENABLE_POSITION_INCREMENTS);

        //    if (enablePositionsIncrements == null)
        //    {
        //        return false;

        //    }
        //    else
        //    {
        //        return enablePositionsIncrements.Value;
        //    }

        //}

        public MultiTermQuery.RewriteMethod MultiTermRewriteMethod
        {
            get
            {
                return GetQueryConfigHandler().Get(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD);
            }
            set
            {
                GetQueryConfigHandler().Set(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD, value);
            }
        }

        ///**
        // * By default, it uses
        // * {@link MultiTermQuery#CONSTANT_SCORE_AUTO_REWRITE_DEFAULT} when creating a
        // * prefix, wildcard and range queries. This implementation is generally
        // * preferable because it a) Runs faster b) Does not have the scarcity of terms
        // * unduly influence score c) avoids any {@link TooManyListenersException}
        // * exception. However, if your application really needs to use the
        // * old-fashioned boolean queries expansion rewriting and the above points are
        // * not relevant then use this change the rewrite method.
        // */

        //public override void SetMultiTermRewriteMethod(MultiTermQuery.RewriteMethod method)
        //{
        //    GetQueryConfigHandler().Set(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD, method);
        //}

        ///**
        // * @see #setMultiTermRewriteMethod(org.apache.lucene.search.MultiTermQuery.RewriteMethod)
        // */

        //public override MultiTermQuery.RewriteMethod GetMultiTermRewriteMethod()
        //{
        //    return GetQueryConfigHandler().Get(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD);
        //}

        /**
         * Set the fields a query should be expanded to when the field is
         * <code>null</code>
         * 
         * @param fields the fields used to expand the query
         */
        public void SetMultiFields(string[] fields)
        {

            if (fields == null)
            {
                fields = new string[0];
            }

            GetQueryConfigHandler().Set(ConfigurationKeys.MULTI_FIELDS, fields);

        }

        /**
         * Returns the fields used to expand the query when the field for a
         * certain query is <code>null</code>
         * 
         * @param fields the fields used to expand the query
         */
        public void GetMultiFields(string[] fields)
        {
            GetQueryConfigHandler().Get(ConfigurationKeys.MULTI_FIELDS);
        }

        public int FuzzyPrefixLength
        {
            get
            {
                FuzzyConfig fuzzyConfig = GetQueryConfigHandler().Get(ConfigurationKeys.FUZZY_CONFIG);

                if (fuzzyConfig == null)
                {
                    return FuzzyQuery.DefaultPrefixLength;
                }
                else
                {
                    return fuzzyConfig.GetPrefixLength();
                }
            }
            set
            {
                QueryConfigHandler config = GetQueryConfigHandler();
                FuzzyConfig fuzzyConfig = config.Get(ConfigurationKeys.FUZZY_CONFIG);

                if (fuzzyConfig == null)
                {
                    fuzzyConfig = new FuzzyConfig();
                    config.Set(ConfigurationKeys.FUZZY_CONFIG, fuzzyConfig);
                }

                fuzzyConfig.SetPrefixLength(value);
            }
        }

        ///**
        // * Set the prefix length for fuzzy queries. Default is 0.
        // * 
        // * @param fuzzyPrefixLength
        // *          The fuzzyPrefixLength to set.
        // */

        //public void SetFuzzyPrefixLength(int fuzzyPrefixLength)
        //{
        //    QueryConfigHandler config = GetQueryConfigHandler();
        //    FuzzyConfig fuzzyConfig = config.Get(ConfigurationKeys.FUZZY_CONFIG);

        //    if (fuzzyConfig == null)
        //    {
        //        fuzzyConfig = new FuzzyConfig();
        //        config.Set(ConfigurationKeys.FUZZY_CONFIG, fuzzyConfig);
        //    }

        //    fuzzyConfig.SetPrefixLength(fuzzyPrefixLength);

        //}

        public void SetNumericConfigMap(IDictionary<string, NumericConfig> numericConfigMap)
        {
            GetQueryConfigHandler().Set(ConfigurationKeys.NUMERIC_CONFIG_MAP, numericConfigMap);
        }

        public IDictionary<string, NumericConfig> GetNumericConfigMap()
        {
            return GetQueryConfigHandler().Get(ConfigurationKeys.NUMERIC_CONFIG_MAP);
        }

        public CultureInfo Locale
        {
            get
            {
                return GetQueryConfigHandler().Get(ConfigurationKeys.LOCALE);
            }
            set
            {
                GetQueryConfigHandler().Set(ConfigurationKeys.LOCALE, value);
            }
        }

        ///**
        // * Set locale used by date range parsing.
        // */

        //public override void SetLocale(CultureInfo locale)
        //{
        //    GetQueryConfigHandler().Set(ConfigurationKeys.LOCALE, locale);
        //}

        ///**
        // * Returns current locale, allowing access by subclasses.
        // */

        //public override CultureInfo GetLocale()
        //{
        //    return GetQueryConfigHandler().Get(ConfigurationKeys.LOCALE);
        //}

        public TimeZoneInfo TimeZone
        {
            get
            {
                return GetQueryConfigHandler().Get(ConfigurationKeys.TIMEZONE);
            }
            set
            {
                GetQueryConfigHandler().Set(ConfigurationKeys.TIMEZONE, value);
            }
        }

        //public override void SetTimeZone(TimeZoneInfo timeZone)
        //{
        //    GetQueryConfigHandler().Set(ConfigurationKeys.TIMEZONE, timeZone);
        //}


        //public override TimeZoneInfo GetTimeZone()
        //{
        //    return GetQueryConfigHandler().Get(ConfigurationKeys.TIMEZONE);
        //}

        /**
         * Sets the default slop for phrases. If zero, then exact phrase matches are
         * required. Default value is zero.
         * 
         * @deprecated renamed to {@link #setPhraseSlop(int)}
         */
        [Obsolete]
        public void SetDefaultPhraseSlop(int defaultPhraseSlop)
        {
            GetQueryConfigHandler().Set(ConfigurationKeys.PHRASE_SLOP, defaultPhraseSlop);
        }

        ///**
        // * Sets the default slop for phrases. If zero, then exact phrase matches are
        // * required. Default value is zero.
        // */
        //[Obsolete]
        //public void SetPhraseSlop(int defaultPhraseSlop)
        //{
        //    GetQueryConfigHandler().Set(ConfigurationKeys.PHRASE_SLOP, defaultPhraseSlop);
        //}

        public Analyzer Analyzer
        {
            get
            {
                return GetQueryConfigHandler().Get(ConfigurationKeys.ANALYZER);
            }
            set
            {
                GetQueryConfigHandler().Set(ConfigurationKeys.ANALYZER, value);
            }
        }

        //public void SetAnalyzer(Analyzer analyzer)
        //{
        //    GetQueryConfigHandler().Set(ConfigurationKeys.ANALYZER, analyzer);
        //}

        //public override Analyzer GetAnalyzer()
        //{
        //    return GetQueryConfigHandler().Get(ConfigurationKeys.ANALYZER);
        //}

        ///**
        // * @see #setAllowLeadingWildcard(boolean)
        // */

        //public override bool GetAllowLeadingWildcard()
        //{
        //    bool? allowLeadingWildcard = GetQueryConfigHandler().Get(ConfigurationKeys.ALLOW_LEADING_WILDCARD);

        //    if (allowLeadingWildcard == null)
        //    {
        //        return false;

        //    }
        //    else
        //    {
        //        return allowLeadingWildcard.Value;
        //    }
        //}

        /**
         * Get the minimal similarity for fuzzy queries.
         */

        //public override float GetFuzzyMinSim()
        //{
        //    FuzzyConfig fuzzyConfig = GetQueryConfigHandler().Get(ConfigurationKeys.FUZZY_CONFIG);
        //    return (fuzzyConfig != null) ? fuzzyConfig.GetMinSimilarity() : FuzzyQuery.DefaultMinSimilarity;
        //}

        ///**
        // * Get the prefix length for fuzzy queries.
        // * 
        // * @return Returns the fuzzyPrefixLength.
        // */

        //public override int GetFuzzyPrefixLength()
        //{
        //    FuzzyConfig fuzzyConfig = GetQueryConfigHandler().Get(ConfigurationKeys.FUZZY_CONFIG);

        //    if (fuzzyConfig == null)
        //    {
        //        return FuzzyQuery.DefaultPrefixLength;
        //    }
        //    else
        //    {
        //        return fuzzyConfig.GetPrefixLength();
        //    }
        //}

        public int PhraseSlop
        {
            get
            {
                int? phraseSlop = GetQueryConfigHandler().Get(ConfigurationKeys.PHRASE_SLOP);
                return phraseSlop.HasValue ? phraseSlop.Value : 0;
            }

            set // LUCENENET TODO: obsolete
            {
                GetQueryConfigHandler().Set(ConfigurationKeys.PHRASE_SLOP, value);
            }
        }

        ///**
        // * Gets the default slop for phrases.
        // */

        //public override int GetPhraseSlop()
        //{
        //    int? phraseSlop = GetQueryConfigHandler().Get(ConfigurationKeys.PHRASE_SLOP);

        //    if (phraseSlop == null)
        //    {
        //        return 0;

        //    }
        //    else
        //    {
        //        return phraseSlop.Value;
        //    }
        //}

        public float FuzzyMinSim
        {
            get
            {
                FuzzyConfig fuzzyConfig = GetQueryConfigHandler().Get(ConfigurationKeys.FUZZY_CONFIG);
                return (fuzzyConfig != null) ? fuzzyConfig.GetMinSimilarity() : FuzzyQuery.DefaultMinSimilarity;
            }
            set
            {
                QueryConfigHandler config = GetQueryConfigHandler();
                FuzzyConfig fuzzyConfig = config.Get(ConfigurationKeys.FUZZY_CONFIG);

                if (fuzzyConfig == null)
                {
                    fuzzyConfig = new FuzzyConfig();
                    config.Set(ConfigurationKeys.FUZZY_CONFIG, fuzzyConfig);
                }

                fuzzyConfig.SetMinSimilarity(value);
            }
        }

        ///**
        // * Set the minimum similarity for fuzzy queries. Default is defined on
        // * {@link FuzzyQuery#defaultMinSimilarity}.
        // */

        //public override void SetFuzzyMinSim(float fuzzyMinSim)
        //{
        //    QueryConfigHandler config = GetQueryConfigHandler();
        //    FuzzyConfig fuzzyConfig = config.Get(ConfigurationKeys.FUZZY_CONFIG);

        //    if (fuzzyConfig == null)
        //    {
        //        fuzzyConfig = new FuzzyConfig();
        //        config.Set(ConfigurationKeys.FUZZY_CONFIG, fuzzyConfig);
        //    }

        //    fuzzyConfig.SetMinSimilarity(fuzzyMinSim);
        //}

        /**
         * Sets the boost used for each field.
         * 
         * @param boosts a collection that maps a field to its boost 
         */
        public void SetFieldsBoost(IDictionary<string, float?> boosts)
        {
            GetQueryConfigHandler().Set(ConfigurationKeys.FIELD_BOOST_MAP, boosts);
        }

        /**
         * Returns the field to boost map used to set boost for each field.
         * 
         * @return the field to boost map 
         */
        public IDictionary<string, float?> GetFieldsBoost()
        {
            return GetQueryConfigHandler().Get(ConfigurationKeys.FIELD_BOOST_MAP);
        }

        /**
         * Sets the default {@link Resolution} used for certain field when
         * no {@link Resolution} is defined for this field.
         * 
         * @param dateResolution the default {@link Resolution}
         */

        public virtual void SetDateResolution(DateTools.Resolution dateResolution)
        {
            GetQueryConfigHandler().Set(ConfigurationKeys.DATE_RESOLUTION, dateResolution);
        }

        /**
         * Returns the default {@link Resolution} used for certain field when
         * no {@link Resolution} is defined for this field.
         * 
         * @return the default {@link Resolution}
         */
        public DateTools.Resolution? GetDateResolution()
        {
            return GetQueryConfigHandler().Get(ConfigurationKeys.DATE_RESOLUTION);
        }

        /**
         * Sets the {@link Resolution} used for each field
         * 
         * @param dateRes a collection that maps a field to its {@link Resolution}
         * 
         * @deprecated this method was renamed to {@link #setDateResolutionMap(Map)} 
         */
        [Obsolete]
        public void SetDateResolution(IDictionary<string, DateTools.Resolution?> dateRes)
        {
            SetDateResolutionMap(dateRes);
        }

        /**
         * Returns the field to {@link Resolution} map used to normalize each date field.
         * 
         * @return the field to {@link Resolution} map
         */
        public IDictionary<string, DateTools.Resolution?> GetDateResolutionMap()
        {
            return GetQueryConfigHandler().Get(ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP);
        }

        /**
         * Sets the {@link Resolution} used for each field
         * 
         * @param dateRes a collection that maps a field to its {@link Resolution}
         */
        public void SetDateResolutionMap(IDictionary<string, DateTools.Resolution?> dateRes)
        {
            GetQueryConfigHandler().Set(ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP, dateRes);
        }
    }
}
