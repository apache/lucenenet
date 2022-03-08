using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Builders;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using Operator = Lucene.Net.QueryParsers.Flexible.Standard.Config.StandardQueryConfigHandler.Operator;

namespace Lucene.Net.QueryParsers.Flexible.Standard
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
    /// This class is a helper that enables users to easily use the Lucene query
    /// parser.
    /// <para/>
    /// To construct a Query object from a query string, use the
    /// <see cref="Parse(string, string)"/> method:
    /// <code>
    /// StandardQueryParser queryParserHelper = new StandardQueryParser();
    /// Query query = queryParserHelper.Parse("a AND b", "defaultField");
    /// </code>
    /// <para/>
    /// To change any configuration before parsing the query string do, for example:
    /// <code>
    /// queryParserHelper.Analyzer = new WhitespaceAnalyzer();
    /// queryParserHelper.AllowLeadingWildcard = true;
    /// // Or alternativley use the query config handler returned by StandardQueryParser which is a
    /// // StandardQueryConfigHandler:
    /// queryParserHelper.QueryConfigHandler.Set(ConfigurationKeys.ALLOW_LEADING_WILDCARD, true);
    /// </code>
    /// <para/>
    /// The syntax for query strings is as follows (copied from the old QueryParser
    /// javadoc):
    /// <para/>
    /// A Query is a series of clauses. A clause may be prefixed by:
    /// <list type="bullet">
    ///     <item><description>
    ///     a plus (<c>+</c>) or a minus (<c>-</c>) sign, indicating that
    ///     the clause is required or prohibited respectively; or
    ///     </description></item>
    ///     <item><description>
    ///     a term followed by a colon, indicating the field to be searched. This
    ///     enables one to construct queries which search multiple fields.
    ///     </description></item>
    /// </list>
    /// 
    /// A clause may be either:
    /// <list type="bullet">
    ///     <item><description>
    ///     a term, indicating all the documents that contain this term; or
    ///     </description></item>
    ///     <item><description>
    ///     a nested query, enclosed in parentheses. Note that this may be used with
    ///     a <c>+</c>/<c>-</c> prefix to require any of a set of terms.
    ///     </description></item>
    /// </list>
    /// 
    /// Thus, in BNF, the query grammar is:
    /// <code>
    ///     Query  ::= ( Clause )*
    ///     Clause ::= [&quot;+&quot;, &quot;-&quot;] [&lt;TERM&gt; &quot;:&quot;] ( &lt;TERM&gt; | &quot;(&quot; Query &quot;)&quot; )
    /// </code>
    /// 
    /// <para>
    /// Examples of appropriately formatted queries can be found in the query syntax documentation.
    /// </para>
    /// <para>
    /// The text parser used by this helper is a <see cref="StandardSyntaxParser"/>.
    /// </para>
    /// <para>
    /// The query node processor used by this helper is a
    /// <see cref="StandardQueryNodeProcessorPipeline"/>.
    /// </para>
    /// <para>
    /// The builder used by this helper is a <see cref="StandardQueryTreeBuilder"/>.
    /// </para>
    /// </summary>
    /// <seealso cref="StandardQueryParser"/>
    /// <seealso cref="StandardQueryConfigHandler"/>
    /// <seealso cref="StandardSyntaxParser"/>
    /// <seealso cref="StandardQueryNodeProcessorPipeline"/>
    /// <seealso cref="StandardQueryTreeBuilder"/>
    public class StandardQueryParser : QueryParserHelper<Query>, ICommonQueryParserConfiguration
    {
        /// <summary>
        /// Constructs a <see cref="StandardQueryParser"/> object.
        /// </summary>
        public StandardQueryParser()
            : base(new StandardQueryConfigHandler(), new StandardSyntaxParser(),
                new StandardQueryNodeProcessorPipeline(null),
                new StandardQueryTreeBuilder())
        {
            EnablePositionIncrements = true;
        }

        /// <summary>
        /// Constructs a <see cref="StandardQueryParser"/> object and sets an
        /// <see cref="Analysis.Analyzer"/> to it. The same as:
        /// <code>
        /// StandardQueryParser qp = new StandardQueryParser();
        /// qp.QueryConfigHandler.Analyzer = analyzer;
        /// </code>
        /// </summary>
        /// <param name="analyzer">the analyzer to be used by this query parser helper</param>
        public StandardQueryParser(Analyzer analyzer)
            : this()
        {
            this.Analyzer = analyzer;
        }

        public override string ToString()
        {
            return "<StandardQueryParser config=\"" + this.QueryConfigHandler
                + "\"/>";
        }

        /// <summary>
        /// Overrides <see cref="QueryParserHelper{TQuery}.Parse(string, string)"/> so it casts the
        /// return object to <see cref="Query"/>. For more reference about this method, check
        /// <see cref="QueryParserHelper{TQuery}.Parse(string, string)"/>.
        /// </summary>
        /// <param name="query">the query string</param>
        /// <param name="defaultField">the default field used by the text parser</param>
        /// <returns>the object built from the query</returns>
        /// <exception cref="QueryNodeException">if something wrong happens along the three phases</exception>
        public override Query Parse(string query, string defaultField)
        {
            return base.Parse(query, defaultField);
        }

        /// <summary>
        /// Gets or Sets the boolean operator of the QueryParser. In default mode (
        /// <see cref="Operator.OR"/>) terms without any modifiers are considered optional:
        /// for example <c>capital of Hungary</c> is equal to
        /// <c>capital OR of OR Hungary</c>.
        /// <para/>
        /// In <see cref="Operator.AND"/> mode terms are considered to be in conjunction: the
        /// above mentioned query is parsed as <c>capital AND of AND Hungary</c>
        /// </summary>
        public virtual Operator DefaultOperator
        {
            get => QueryConfigHandler.Get(ConfigurationKeys.DEFAULT_OPERATOR); // LUCENENET: The default value is OR, so we just rely on the compiler if it doesn't exist
            set => QueryConfigHandler.Set(ConfigurationKeys.DEFAULT_OPERATOR, value);
        }

        /// <summary>
        /// Set to <c>true</c> to allow leading wildcard characters.
        /// <para/>
        /// When set, <c>*</c> or <c>?</c> are allowed as the first
        /// character of a <see cref="PrefixQuery"/> and <see cref="WildcardQuery"/>. Note that this can produce
        /// very slow queries on big indexes.
        /// <para/>
        /// Default: false.
        /// </summary>
        public virtual bool LowercaseExpandedTerms
        {
            get => QueryConfigHandler.TryGetValue(ConfigurationKeys.LOWERCASE_EXPANDED_TERMS, out bool value) ? value : true;
            set => QueryConfigHandler.Set(ConfigurationKeys.LOWERCASE_EXPANDED_TERMS, value);
        }

        /// <summary>
        /// Set to <c>true</c> to allow leading wildcard characters.
        /// <para/>
        /// When set, <c>*</c> or <c>?</c> are allowed as the first
        /// character of a <see cref="PrefixQuery"/> and <see cref="WildcardQuery"/>. Note that this can produce
        /// very slow queries on big indexes.
        /// <para/>
        /// Default: false.
        /// </summary>
        public virtual bool AllowLeadingWildcard
        {
            get => QueryConfigHandler.Get(ConfigurationKeys.ALLOW_LEADING_WILDCARD); // LUCENENET: The default value is false, so we just rely on the compiler if it doesn't exist
            set => QueryConfigHandler.Set(ConfigurationKeys.ALLOW_LEADING_WILDCARD, value);
        }

        /// <summary>
        /// Set to <c>true</c> to enable position increments in result query.
        /// <para/>
        /// When set, result phrase and multi-phrase queries will be aware of position
        /// increments. Useful when e.g. a <see cref="Analysis.Core.StopFilter"/> increases the position increment
        /// of the token that follows an omitted token.
        /// <para/>
        /// Default: false.
        /// </summary>
        public virtual bool EnablePositionIncrements
        {
            get => QueryConfigHandler.Get(ConfigurationKeys.ENABLE_POSITION_INCREMENTS); // LUCENENET: The default value is false, so we just rely on the compiler if it doesn't exist
            set => QueryConfigHandler.Set(ConfigurationKeys.ENABLE_POSITION_INCREMENTS, value);
        }

        /// <summary>
        /// By default, it uses 
        /// <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/> when creating a
        /// prefix, wildcard and range queries. This implementation is generally
        /// preferable because it a) Runs faster b) Does not have the scarcity of terms
        /// unduly influence score c) avoids any Exception due to too many listeners.
        /// However, if your application really needs to use the
        /// old-fashioned boolean queries expansion rewriting and the above points are
        /// not relevant then use this change the rewrite method.
        /// </summary>
        public virtual MultiTermQuery.RewriteMethod MultiTermRewriteMethod
        {
            get => QueryConfigHandler.Get(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD);
            set => QueryConfigHandler.Set(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD, value);
        }

        /// <summary>
        /// Set the fields a query should be expanded to when the field is
        /// <c>null</c>
        /// </summary>
        /// <param name="fields">the fields used to expand the query</param>
        public virtual void SetMultiFields(string[] fields)
        {

            if (fields is null)
            {
                fields = Arrays.Empty<string>();
            }

            QueryConfigHandler.Set(ConfigurationKeys.MULTI_FIELDS, fields);

        }

        /// <summary>
        /// Returns the fields used to expand the query when the field for a
        /// certain query is <c>null</c>
        /// </summary>
        /// <returns>the fields used to expand the query</returns>
        public virtual string[] GetMultiFields()
        {
            return QueryConfigHandler.Get(ConfigurationKeys.MULTI_FIELDS);
        }

        /// <summary>
        /// Gets or Sets the prefix length for fuzzy queries. Default is 0.
        /// </summary>
        public virtual int FuzzyPrefixLength
        {
            get
            {
                FuzzyConfig fuzzyConfig = QueryConfigHandler.Get(ConfigurationKeys.FUZZY_CONFIG);

                if (fuzzyConfig is null)
                {
                    return FuzzyQuery.DefaultPrefixLength;
                }
                else
                {
                    return fuzzyConfig.PrefixLength;
                }
            }
            set
            {
                QueryConfigHandler config = QueryConfigHandler;
                FuzzyConfig fuzzyConfig = config.Get(ConfigurationKeys.FUZZY_CONFIG);

                if (fuzzyConfig is null)
                {
                    fuzzyConfig = new FuzzyConfig();
                    config.Set(ConfigurationKeys.FUZZY_CONFIG, fuzzyConfig);
                }

                fuzzyConfig.PrefixLength = value;
            }
        }

        public virtual IDictionary<string, NumericConfig> NumericConfigMap
        {
            get => QueryConfigHandler.Get(ConfigurationKeys.NUMERIC_CONFIG_MAP);
            set => QueryConfigHandler.Set(ConfigurationKeys.NUMERIC_CONFIG_MAP, value);
        }

        /// <summary>
        /// Gets or Sets current locale, allowing access by subclasses. Used by date range parsing
        /// </summary>
        public virtual CultureInfo Locale
        {
            get
            {
                var culture = QueryConfigHandler.Get(ConfigurationKeys.LOCALE);
                return culture ?? CultureInfo.CurrentCulture;
            }
            set => QueryConfigHandler.Set(ConfigurationKeys.LOCALE, value);
        }

        public virtual TimeZoneInfo TimeZone
        {
            get => QueryConfigHandler.Get(ConfigurationKeys.TIMEZONE) ?? TimeZoneInfo.Local;
            set => QueryConfigHandler.Set(ConfigurationKeys.TIMEZONE, value);
        }

        /// <summary>
        /// Sets the default slop for phrases. If zero, then exact phrase matches are
        /// required. Default value is zero.
        /// </summary>
        /// <param name="defaultPhraseSlop"></param>
        [Obsolete("Use PhraseSlop property setter instead.")]
        public virtual void SetDefaultPhraseSlop(int defaultPhraseSlop)
        {
            QueryConfigHandler.Set(ConfigurationKeys.PHRASE_SLOP, defaultPhraseSlop);
        }

        public virtual Analyzer Analyzer
        {
            get => QueryConfigHandler.Get(ConfigurationKeys.ANALYZER);
            set => QueryConfigHandler.Set(ConfigurationKeys.ANALYZER, value);
        }

        /// <summary>
        /// Gets or Sets the default slop for phrases. If zero, then exact phrase matches are
        /// required. Default value is zero. NOTE: Setter is deprecated.
        /// </summary>
        public virtual int PhraseSlop
        {
            get => QueryConfigHandler.Get(ConfigurationKeys.PHRASE_SLOP); // LUCENENET: The default value is 0, so we just rely on the compiler if it doesn't exist
            set => QueryConfigHandler.Set(ConfigurationKeys.PHRASE_SLOP, value);
        }

        /// <summary>
        /// Gets or Sets the minimum similarity for fuzzy queries. Default is defined on
        /// <see cref="FuzzyQuery.DefaultMinSimilarity"/>.
        /// </summary>
        public virtual float FuzzyMinSim
        {
            get
            {
                FuzzyConfig fuzzyConfig = QueryConfigHandler.Get(ConfigurationKeys.FUZZY_CONFIG);
                return (fuzzyConfig != null) ? fuzzyConfig.MinSimilarity
#pragma warning disable 612, 618
                    : FuzzyQuery.DefaultMinSimilarity;
#pragma warning restore 612, 618
            }
            set
            {
                QueryConfigHandler config = QueryConfigHandler;
                FuzzyConfig fuzzyConfig = config.Get(ConfigurationKeys.FUZZY_CONFIG);

                if (fuzzyConfig is null)
                {
                    fuzzyConfig = new FuzzyConfig();
                    config.Set(ConfigurationKeys.FUZZY_CONFIG, fuzzyConfig);
                }

                fuzzyConfig.MinSimilarity = value;
            }
        }

        /// <summary>
        ///  Gets or Sets the field to boost map used to set boost for each field.
        /// </summary>
        public virtual IDictionary<string, float> FieldsBoost
        {
            get => QueryConfigHandler.Get(ConfigurationKeys.FIELD_BOOST_MAP);
            set => QueryConfigHandler.Set(ConfigurationKeys.FIELD_BOOST_MAP, value);
        }

        /// <summary>
        /// Sets the default <see cref="Documents.DateResolution"/> used for certain field when
        /// no <see cref="Documents.DateResolution"/> is defined for this field.
        /// </summary>
        /// <param name="dateResolution">the default <see cref="Documents.DateResolution"/></param>
        // LUCENENET NOTE: This method is required by the ICommonQueryParserConfiguration interface
        public virtual void SetDateResolution(DateResolution dateResolution)
        {
            QueryConfigHandler.Set(ConfigurationKeys.DATE_RESOLUTION, dateResolution);
        }

        /// <summary>
        /// Gets the default <see cref="Documents.DateResolution"/> used for certain field when
        /// no <see cref="Documents.DateResolution"/> is defined for this field.
        /// </summary>
        [ExceptionToNullableEnumConvention]
        public virtual DateResolution? DateResolution => QueryConfigHandler.TryGetValue(ConfigurationKeys.DATE_RESOLUTION, out DateResolution value) ? value : null;

        /// <summary>
        /// Sets the <see cref="Documents.DateResolution"/> used for each field
        /// </summary>
        /// <param name="dateRes">a collection that maps a field to its <see cref="Documents.DateResolution"/></param>
        [Obsolete("Use DateResolutionMap property instead.")]
        public virtual void SetDateResolution(IDictionary<string, DateResolution> dateRes)
        {
            DateResolutionMap = dateRes;
        }

        /// <summary>
        /// Gets or Sets the field to <see cref="Documents.DateResolution"/> map used to normalize each date field.
        /// </summary>
        public virtual IDictionary<string, DateResolution> DateResolutionMap
        {
            get => QueryConfigHandler.Get(ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP);
            set => QueryConfigHandler.Set(ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP, value);
        }
    }
}
