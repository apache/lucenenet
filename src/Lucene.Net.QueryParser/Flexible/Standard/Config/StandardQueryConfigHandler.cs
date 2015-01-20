/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Queryparser.Flexible.Core.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Config
{
	/// <summary>
	/// This query configuration handler is used for almost every processor defined
	/// in the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</see>
	/// processor pipeline. It holds
	/// configuration methods that reproduce the configuration methods that could be set on the old
	/// lucene 2.4 QueryParser class. <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</seealso>
	public class StandardQueryConfigHandler : QueryConfigHandler
	{
		/// <summary>Class holding keys for StandardQueryNodeProcessorPipeline options.</summary>
		/// <remarks>Class holding keys for StandardQueryNodeProcessorPipeline options.</remarks>
		public sealed class ConfigurationKeys
		{
			/// <summary>Key used to set whether position increments is enabled</summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetEnablePositionIncrements(bool)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetEnablePositionIncrements(bool)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetEnablePositionIncrements()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetEnablePositionIncrements()
			/// 	</seealso>
			public static readonly ConfigurationKey<bool> ENABLE_POSITION_INCREMENTS = ConfigurationKey
				.NewInstance();

			/// <summary>Key used to set whether expanded terms should be lower-cased</summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetLowercaseExpandedTerms(bool)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetLowercaseExpandedTerms(bool)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetLowercaseExpandedTerms()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetLowercaseExpandedTerms()
			/// 	</seealso>
			public static readonly ConfigurationKey<bool> LOWERCASE_EXPANDED_TERMS = ConfigurationKey
				.NewInstance();

			/// <summary>Key used to set whether leading wildcards are supported</summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetAllowLeadingWildcard(bool)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetAllowLeadingWildcard(bool)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetAllowLeadingWildcard()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetAllowLeadingWildcard()
			/// 	</seealso>
			public static readonly ConfigurationKey<bool> ALLOW_LEADING_WILDCARD = ConfigurationKey
				.NewInstance();

			/// <summary>
			/// Key used to set the
			/// <see cref="Lucene.Net.Analysis.Analyzer">Lucene.Net.Analysis.Analyzer
			/// 	</see>
			/// used for terms found in the query
			/// </summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetAnalyzer(Lucene.Net.Analysis.Analyzer)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetAnalyzer(Lucene.Net.Analysis.Analyzer)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetAnalyzer()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetAnalyzer()
			/// 	</seealso>
			public static readonly ConfigurationKey<Analyzer> ANALYZER = ConfigurationKey.NewInstance
				();

			/// <summary>Key used to set the default boolean operator</summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetDefaultOperator(Operator)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetDefaultOperator(Operator)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetDefaultOperator()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetDefaultOperator()
			/// 	</seealso>
			public static readonly ConfigurationKey<StandardQueryConfigHandler.Operator> DEFAULT_OPERATOR
				 = ConfigurationKey.NewInstance();

			/// <summary>Key used to set the default phrase slop</summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetPhraseSlop(int)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetPhraseSlop(int)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetPhraseSlop()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetPhraseSlop()
			/// 	</seealso>
			public static readonly ConfigurationKey<int> PHRASE_SLOP = ConfigurationKey.NewInstance
				();

			/// <summary>
			/// Key used to set the
			/// <see cref="System.Globalization.CultureInfo">System.Globalization.CultureInfo</see>
			/// used when parsing the query
			/// </summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetLocale(System.Globalization.CultureInfo)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetLocale(System.Globalization.CultureInfo)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetLocale()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetLocale()
			/// 	</seealso>
			public static readonly ConfigurationKey<CultureInfo> LOCALE = ConfigurationKey.NewInstance
				();

			public static readonly ConfigurationKey<TimeZoneInfo> TIMEZONE = ConfigurationKey
				.NewInstance();

			/// <summary>
			/// Key used to set the
			/// <see cref="Lucene.Net.Search.MultiTermQuery.RewriteMethod">Lucene.Net.Search.MultiTermQuery.RewriteMethod
			/// 	</see>
			/// used when creating queries
			/// </summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetMultiTermRewriteMethod(Lucene.Net.Search.MultiTermQuery.RewriteMethod)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetMultiTermRewriteMethod(Lucene.Net.Search.MultiTermQuery.RewriteMethod)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetMultiTermRewriteMethod()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetMultiTermRewriteMethod()
			/// 	</seealso>
			public static readonly ConfigurationKey<MultiTermQuery.RewriteMethod> MULTI_TERM_REWRITE_METHOD
				 = ConfigurationKey.NewInstance();

			/// <summary>
			/// Key used to set the fields a query should be expanded to when the field
			/// is <code>null</code>
			/// </summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetMultiFields(Sharpen.CharSequence[])
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetMultiFields(Sharpen.CharSequence[])
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetMultiFields(Sharpen.CharSequence[])
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetMultiFields(Sharpen.CharSequence[])
			/// 	</seealso>
			public static readonly ConfigurationKey<CharSequence[]> MULTI_FIELDS = ConfigurationKey
				.NewInstance();

			/// <summary>Key used to set a field to boost map that is used to set the boost for each field
			/// 	</summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetFieldsBoost(System.Collections.Generic.IDictionary{K, V})
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetFieldsBoost(System.Collections.Generic.IDictionary&lt;K, V&gt;)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetFieldsBoost()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetFieldsBoost()
			/// 	</seealso>
			public static readonly ConfigurationKey<IDictionary<string, float>> FIELD_BOOST_MAP
				 = ConfigurationKey.NewInstance();

			/// <summary>
			/// Key used to set a field to
			/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
			/// 	</see>
			/// map that is used
			/// to normalize each date field value.
			/// </summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetDateResolutionMap(System.Collections.Generic.IDictionary{K, V})
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetDateResolutionMap(System.Collections.Generic.IDictionary&lt;K, V&gt;)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetDateResolutionMap()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetDateResolutionMap()
			/// 	</seealso>
			public static readonly ConfigurationKey<IDictionary<CharSequence, DateTools.Resolution
				>> FIELD_DATE_RESOLUTION_MAP = ConfigurationKey.NewInstance();

			/// <summary>
			/// Key used to set the
			/// <see cref="FuzzyConfig">FuzzyConfig</see>
			/// used to create fuzzy queries.
			/// </summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetFuzzyMinSim(float)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetFuzzyMinSim(float)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetFuzzyPrefixLength(int)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetFuzzyPrefixLength(int)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetFuzzyMinSim()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetFuzzyMinSim()
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetFuzzyPrefixLength()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetFuzzyPrefixLength()
			/// 	</seealso>
			public static readonly ConfigurationKey<FuzzyConfig> FUZZY_CONFIG = ConfigurationKey
				.NewInstance();

			/// <summary>
			/// Key used to set default
			/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
			/// 	</see>
			/// .
			/// </summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetDateResolution(Lucene.Net.Document.DateTools.Resolution)
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetDateResolution(Lucene.Net.Document.DateTools.Resolution)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetDateResolution()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetDateResolution()
			/// 	</seealso>
			public static readonly ConfigurationKey<DateTools.Resolution> DATE_RESOLUTION = ConfigurationKey
				.NewInstance();

			/// <summary>
			/// Key used to set the boost value in
			/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfig">Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfig
			/// 	</see>
			/// objects.
			/// </summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetFieldsBoost(System.Collections.Generic.IDictionary{K, V})
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetFieldsBoost(System.Collections.Generic.IDictionary&lt;K, V&gt;)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetFieldsBoost()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetFieldsBoost()
			/// 	</seealso>
			public static readonly ConfigurationKey<float> BOOST = ConfigurationKey.NewInstance
				();

			/// <summary>
			/// Key used to set a field to its
			/// <see cref="NumericConfig">NumericConfig</see>
			/// .
			/// </summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetNumericConfigMap(System.Collections.Generic.IDictionary{K, V})
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetNumericConfigMap(System.Collections.Generic.IDictionary&lt;K, V&gt;)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetNumericConfigMap()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetNumericConfigMap()
			/// 	</seealso>
			public static readonly ConfigurationKey<NumericConfig> NUMERIC_CONFIG = ConfigurationKey
				.NewInstance();

			/// <summary>
			/// Key used to set the
			/// <see cref="NumericConfig">NumericConfig</see>
			/// in
			/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfig">Lucene.Net.Queryparser.Flexible.Core.Config.FieldConfig
			/// 	</see>
			/// for numeric fields.
			/// </summary>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetNumericConfigMap(System.Collections.Generic.IDictionary{K, V})
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.SetNumericConfigMap(System.Collections.Generic.IDictionary&lt;K, V&gt;)
			/// 	</seealso>
			/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetNumericConfigMap()
			/// 	">Lucene.Net.Queryparser.Flexible.Standard.StandardQueryParser.GetNumericConfigMap()
			/// 	</seealso>
			public static readonly ConfigurationKey<IDictionary<string, NumericConfig>> NUMERIC_CONFIG_MAP
				 = ConfigurationKey.NewInstance();
		}

		/// <summary>Boolean Operator: AND or OR</summary>
		public enum Operator
		{
			AND,
			OR
		}

		public StandardQueryConfigHandler()
		{
			// Add listener that will build the FieldConfig.
			AddFieldConfigListener(new FieldBoostMapFCListener(this));
			AddFieldConfigListener(new FieldDateResolutionFCListener(this));
			AddFieldConfigListener(new NumericFieldConfigListener(this));
			// Default Values
			Set(StandardQueryConfigHandler.ConfigurationKeys.ALLOW_LEADING_WILDCARD, false);
			// default in 2.9
			Set(StandardQueryConfigHandler.ConfigurationKeys.ANALYZER, null);
			//default value 2.4
			Set(StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR, StandardQueryConfigHandler.Operator
				.OR);
			Set(StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP, 0);
			//default value 2.4
			Set(StandardQueryConfigHandler.ConfigurationKeys.LOWERCASE_EXPANDED_TERMS, true);
			//default value 2.4
			Set(StandardQueryConfigHandler.ConfigurationKeys.ENABLE_POSITION_INCREMENTS, false
				);
			//default value 2.4
			Set(StandardQueryConfigHandler.ConfigurationKeys.FIELD_BOOST_MAP, new LinkedHashMap
				<string, float>());
			Set(StandardQueryConfigHandler.ConfigurationKeys.FUZZY_CONFIG, new FuzzyConfig());
			Set(StandardQueryConfigHandler.ConfigurationKeys.LOCALE, CultureInfo.CurrentCulture
				);
			Set(StandardQueryConfigHandler.ConfigurationKeys.MULTI_TERM_REWRITE_METHOD, MultiTermQuery
				.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);
			Set(StandardQueryConfigHandler.ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP, new Dictionary
				<CharSequence, DateTools.Resolution>());
		}
	}
}
