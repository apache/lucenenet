using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using Operator = Lucene.Net.QueryParsers.Flexible.Standard.Config.StandardQueryConfigHandler.Operator;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
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
    /// This query configuration handler is used for almost every processor defined
    /// in the {@link StandardQueryNodeProcessorPipeline} processor pipeline. It holds
    /// configuration methods that reproduce the configuration methods that could be set on the old
    /// lucene 2.4 QueryParser class.
    /// </summary>
    /// <seealso cref="StandardQueryNodeProcessorPipeline"/>
    public class StandardQueryConfigHandler : QueryConfigHandler
    {
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
            Set(ConfigurationKeys.FIELD_BOOST_MAP, new LinkedHashMap<string, float?>());
            Set(ConfigurationKeys.FUZZY_CONFIG, new FuzzyConfig());
            Set(ConfigurationKeys.LOCALE, CultureInfo.InvariantCulture);
            Set(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);
            Set(ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP, new HashMap<string, DateTools.Resolution?>());
        }

        /**
        * Boolean Operator: AND or OR
        */
        public enum Operator
        {
            AND,
            OR
        }
    }

    /**
   * Class holding keys for StandardQueryNodeProcessorPipeline options.
   */
    public sealed class ConfigurationKeys
    {
        /**
         * Key used to set whether position increments is enabled
         * 
         * @see StandardQueryParser#setEnablePositionIncrements(boolean)
         * @see StandardQueryParser#getEnablePositionIncrements()
         */
        public readonly static ConfigurationKey<bool?> ENABLE_POSITION_INCREMENTS = ConfigurationKey.NewInstance<bool?>();

        /**
         * Key used to set whether expanded terms should be lower-cased
         * 
         * @see StandardQueryParser#setLowercaseExpandedTerms(boolean)
         * @see StandardQueryParser#getLowercaseExpandedTerms()
         */
        public readonly static ConfigurationKey<bool?> LOWERCASE_EXPANDED_TERMS = ConfigurationKey.NewInstance<bool?>();

        /**
         * Key used to set whether leading wildcards are supported
         * 
         * @see StandardQueryParser#setAllowLeadingWildcard(boolean)
         * @see StandardQueryParser#getAllowLeadingWildcard()
         */
        public readonly static ConfigurationKey<bool?> ALLOW_LEADING_WILDCARD = ConfigurationKey.NewInstance<bool?>();

        /**
         * Key used to set the {@link Analyzer} used for terms found in the query
         * 
         * @see StandardQueryParser#setAnalyzer(Analyzer)
         * @see StandardQueryParser#getAnalyzer()
         */
        public readonly static ConfigurationKey<Analyzer> ANALYZER = ConfigurationKey.NewInstance<Analyzer>();

        /**
         * Key used to set the default boolean operator
         * 
         * @see StandardQueryParser#setDefaultOperator(org.apache.lucene.queryparser.flexible.standard.config.StandardQueryConfigHandler.Operator)
         * @see StandardQueryParser#getDefaultOperator()
         */
        public readonly static ConfigurationKey<Operator?> DEFAULT_OPERATOR = ConfigurationKey.NewInstance<Operator?>();

        /**
         * Key used to set the default phrase slop
         * 
         * @see StandardQueryParser#setPhraseSlop(int)
         * @see StandardQueryParser#getPhraseSlop()
         */
        public readonly static ConfigurationKey<int?> PHRASE_SLOP = ConfigurationKey.NewInstance<int?>();

        /**
         * Key used to set the {@link Locale} used when parsing the query
         * 
         * @see StandardQueryParser#setLocale(Locale)
         * @see StandardQueryParser#getLocale()
         */
        public readonly static ConfigurationKey<CultureInfo> LOCALE = ConfigurationKey.NewInstance<CultureInfo>();

        public readonly static ConfigurationKey<TimeZoneInfo> TIMEZONE = ConfigurationKey.NewInstance<TimeZoneInfo>();

        /**
         * Key used to set the {@link RewriteMethod} used when creating queries
         * 
         * @see StandardQueryParser#setMultiTermRewriteMethod(org.apache.lucene.search.MultiTermQuery.RewriteMethod)
         * @see StandardQueryParser#getMultiTermRewriteMethod()
         */
        public readonly static ConfigurationKey<MultiTermQuery.RewriteMethod> MULTI_TERM_REWRITE_METHOD = ConfigurationKey.NewInstance<MultiTermQuery.RewriteMethod>();

        /**
         * Key used to set the fields a query should be expanded to when the field
         * is <code>null</code>
         * 
         * @see StandardQueryParser#setMultiFields(CharSequence[])
         * @see StandardQueryParser#getMultiFields(CharSequence[])
         */
        public readonly static ConfigurationKey<string[]> MULTI_FIELDS = ConfigurationKey.NewInstance<string[]>();

        /**
         * Key used to set a field to boost map that is used to set the boost for each field
         * 
         * @see StandardQueryParser#setFieldsBoost(Map)
         * @see StandardQueryParser#getFieldsBoost()
         */
        public readonly static ConfigurationKey<IDictionary<string, float?>> FIELD_BOOST_MAP = ConfigurationKey.NewInstance<IDictionary<string, float?>>();

        /**
         * Key used to set a field to {@link Resolution} map that is used
         * to normalize each date field value.
         * 
         * @see StandardQueryParser#setDateResolutionMap(Map)
         * @see StandardQueryParser#getDateResolutionMap()
         */
        public readonly static ConfigurationKey<IDictionary<string, DateTools.Resolution?>> FIELD_DATE_RESOLUTION_MAP = ConfigurationKey.NewInstance<IDictionary<string, DateTools.Resolution?>>();

        /**
         * Key used to set the {@link FuzzyConfig} used to create fuzzy queries.
         * 
         * @see StandardQueryParser#setFuzzyMinSim(float)
         * @see StandardQueryParser#setFuzzyPrefixLength(int)
         * @see StandardQueryParser#getFuzzyMinSim()
         * @see StandardQueryParser#getFuzzyPrefixLength()
         */
        public readonly static ConfigurationKey<FuzzyConfig> FUZZY_CONFIG = ConfigurationKey.NewInstance<FuzzyConfig>();

        /**
         * Key used to set default {@link Resolution}.
         * 
         * @see StandardQueryParser#setDateResolution(org.apache.lucene.document.DateTools.Resolution)
         * @see StandardQueryParser#getDateResolution()
         */
        public readonly static ConfigurationKey<DateTools.Resolution> DATE_RESOLUTION = ConfigurationKey.NewInstance<DateTools.Resolution>();

        /**
         * Key used to set the boost value in {@link FieldConfig} objects.
         * 
         * @see StandardQueryParser#setFieldsBoost(Map)
         * @see StandardQueryParser#getFieldsBoost()
         */
        public readonly static ConfigurationKey<float?> BOOST = ConfigurationKey.NewInstance<float?>();

        /**
         * Key used to set a field to its {@link NumericConfig}.
         * 
         * @see StandardQueryParser#setNumericConfigMap(Map)
         * @see StandardQueryParser#getNumericConfigMap()
         */
        public readonly static ConfigurationKey<NumericConfig> NUMERIC_CONFIG = ConfigurationKey.NewInstance<NumericConfig>();

        /**
         * Key used to set the {@link NumericConfig} in {@link FieldConfig} for numeric fields.
         * 
         * @see StandardQueryParser#setNumericConfigMap(Map)
         * @see StandardQueryParser#getNumericConfigMap()
         */
        public readonly static ConfigurationKey<IDictionary<string, NumericConfig>> NUMERIC_CONFIG_MAP = ConfigurationKey.NewInstance<IDictionary<string, NumericConfig>>();
    }
}
