using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;
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
    /// in the <see cref="Processors.StandardQueryNodeProcessorPipeline"/> processor pipeline. It holds
    /// configuration methods that reproduce the configuration methods that could be set on the old
    /// lucene 2.4 QueryParser class.
    /// </summary>
    /// <seealso cref="Processors.StandardQueryNodeProcessorPipeline"/>
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
            Set(ConfigurationKeys.FIELD_BOOST_MAP, new JCG.LinkedDictionary<string, float>());
            Set(ConfigurationKeys.FUZZY_CONFIG, new FuzzyConfig());
            Set(ConfigurationKeys.LOCALE, null);
            Set(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);
            Set(ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP, new JCG.Dictionary<string, DateResolution>());
        }

        /// <summary>
        /// Boolean Operator: AND or OR
        /// </summary>
        public enum Operator
        {
            AND = 1,
            OR = 0 // LUCENENET: Default value if not set
        }
    }

    /// <summary>
    /// Class holding keys for <see cref="Processors.StandardQueryNodeProcessorPipeline"/> options.
    /// </summary>
    public sealed class ConfigurationKeys
    {
        /// <summary>
        /// Key used to set whether position increments is enabled
        /// </summary>
        /// <seealso cref="StandardQueryParser.EnablePositionIncrements"/>
        public readonly static ConfigurationKey<bool> ENABLE_POSITION_INCREMENTS = ConfigurationKey.NewInstance<bool>();

        /// <summary>
        /// Key used to set whether expanded terms should be lower-cased
        /// </summary>
        /// <seealso cref="StandardQueryParser.LowercaseExpandedTerms"/>
        public readonly static ConfigurationKey<bool> LOWERCASE_EXPANDED_TERMS = ConfigurationKey.NewInstance<bool>();

        /// <summary>
        /// Key used to set whether leading wildcards are supported
        /// </summary>
        /// <seealso cref="StandardQueryParser.AllowLeadingWildcard"/>
        public readonly static ConfigurationKey<bool> ALLOW_LEADING_WILDCARD = ConfigurationKey.NewInstance<bool>();

        /// <summary>
        /// Key used to set the <see cref="Analyzer"/> used for terms found in the query
        /// </summary>
        /// <seealso cref="StandardQueryParser.Analyzer"/>
        public readonly static ConfigurationKey<Analyzer> ANALYZER = ConfigurationKey.NewInstance<Analyzer>();

        /// <summary>
        /// Key used to set the default boolean operator
        /// </summary>
        /// <seealso cref="StandardQueryParser.DefaultOperator"/>
        public readonly static ConfigurationKey<Operator> DEFAULT_OPERATOR = ConfigurationKey.NewInstance<Operator>();

        /// <summary>
        /// Key used to set the default phrase slop
        /// </summary>
        /// <seealso cref="StandardQueryParser.PhraseSlop"/>
        public readonly static ConfigurationKey<int> PHRASE_SLOP = ConfigurationKey.NewInstance<int>();

        /// <summary>
        ///  Key used to set the <see cref="CultureInfo">locale</see> used when parsing the query
        /// </summary>
        /// <seealso cref="StandardQueryParser.Locale"/>
        public readonly static ConfigurationKey<CultureInfo> LOCALE = ConfigurationKey.NewInstance<CultureInfo>();

        public readonly static ConfigurationKey<TimeZoneInfo> TIMEZONE = ConfigurationKey.NewInstance<TimeZoneInfo>();

        /// <summary>
        /// Key used to set the <see cref="MultiTermQuery.RewriteMethod"/> used when creating queries
        /// </summary>
        /// <seealso cref="StandardQueryParser.MultiTermRewriteMethod"/> 
        public readonly static ConfigurationKey<MultiTermQuery.RewriteMethod> MULTI_TERM_REWRITE_METHOD = ConfigurationKey.NewInstance<MultiTermQuery.RewriteMethod>();

        /// <summary>
        /// Key used to set the fields a query should be expanded to when the field
        /// is <c>null</c>
        /// </summary>
        /// <seealso cref="StandardQueryParser.SetMultiFields(string[])"/>
        /// <seealso cref="StandardQueryParser.GetMultiFields()"/>
        public readonly static ConfigurationKey<string[]> MULTI_FIELDS = ConfigurationKey.NewInstance<string[]>();

        /// <summary>
        /// Key used to set a field to boost map that is used to set the boost for each field
        /// </summary>
        /// <seealso cref="StandardQueryParser.FieldsBoost"/>
        public readonly static ConfigurationKey<IDictionary<string, float>> FIELD_BOOST_MAP = ConfigurationKey.NewInstance<IDictionary<string, float>>();

        /// <summary>
        /// Key used to set a field to <see cref="DateResolution"/> map that is used
        /// to normalize each date field value.
        /// </summary>
        /// <seealso cref="StandardQueryParser.DateResolutionMap"/>
        public readonly static ConfigurationKey<IDictionary<string, DateResolution>> FIELD_DATE_RESOLUTION_MAP = ConfigurationKey.NewInstance<IDictionary<string, DateResolution>>();

        /// <summary>
        /// Key used to set the <see cref="FuzzyConfig"/> used to create fuzzy queries.
        /// </summary>
        /// <seealso cref="StandardQueryParser.FuzzyMinSim"/>
        /// <seealso cref="StandardQueryParser.FuzzyPrefixLength"/>
        public readonly static ConfigurationKey<FuzzyConfig> FUZZY_CONFIG = ConfigurationKey.NewInstance<FuzzyConfig>();

        /// <summary>
        /// Key used to set default <see cref="DateResolution"/>.
        /// </summary>
        /// <seealso cref="StandardQueryParser.SetDateResolution(DateResolution)"/>
        /// <seealso cref="StandardQueryParser.DateResolution"/>
        public readonly static ConfigurationKey<DateResolution> DATE_RESOLUTION = ConfigurationKey.NewInstance<DateResolution>();

        /// <summary>
        /// Key used to set the boost value in <see cref="FieldConfig"/> objects.
        /// </summary>
        /// <seealso cref="StandardQueryParser.FieldsBoost"/>
        public readonly static ConfigurationKey<float> BOOST = ConfigurationKey.NewInstance<float>();

        /// <summary>
        /// Key used to set a field to its <see cref="NumericConfig"/>.
        /// </summary>
        /// <seealso cref="StandardQueryParser.NumericConfigMap"/>
        public readonly static ConfigurationKey<NumericConfig> NUMERIC_CONFIG = ConfigurationKey.NewInstance<NumericConfig>();

        /// <summary>
        /// Key used to set the <see cref="NumericConfig"/> in <see cref="FieldConfig"/> for numeric fields.
        /// </summary>
        /// <seealso cref="StandardQueryParser.NumericConfigMap"/>
        public readonly static ConfigurationKey<IDictionary<string, NumericConfig>> NUMERIC_CONFIG_MAP = ConfigurationKey.NewInstance<IDictionary<string, NumericConfig>>();
    }
}
