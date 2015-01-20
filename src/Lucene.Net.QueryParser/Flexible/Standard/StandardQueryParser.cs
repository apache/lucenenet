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
using Lucene.Net.Queryparser.Flexible.Core;
using Lucene.Net.Queryparser.Flexible.Core.Config;
using Lucene.Net.Queryparser.Flexible.Standard;
using Lucene.Net.Queryparser.Flexible.Standard.Builders;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Parser;
using Lucene.Net.Queryparser.Flexible.Standard.Processors;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard
{
	/// <summary>
	/// <p>
	/// This class is a helper that enables users to easily use the Lucene query
	/// parser.
	/// </summary>
	/// <remarks>
	/// <p>
	/// This class is a helper that enables users to easily use the Lucene query
	/// parser.
	/// </p>
	/// <p>
	/// To construct a Query object from a query string, use the
	/// <see cref="Parse(string, string)">Parse(string, string)</see>
	/// method:
	/// <ul>
	/// StandardQueryParser queryParserHelper = new StandardQueryParser(); <br/>
	/// Query query = queryParserHelper.parse("a AND b", "defaultField");
	/// </ul>
	/// <p>
	/// To change any configuration before parsing the query string do, for example:
	/// <p/>
	/// <ul>
	/// // the query config handler returned by
	/// <see cref="StandardQueryParser">StandardQueryParser</see>
	/// is a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler
	/// 	</see>
	/// <br/>
	/// queryParserHelper.getQueryConfigHandler().setAnalyzer(new
	/// WhitespaceAnalyzer());
	/// </ul>
	/// <p>
	/// The syntax for query strings is as follows (copied from the old QueryParser
	/// javadoc):
	/// <ul>
	/// A Query is a series of clauses. A clause may be prefixed by:
	/// <ul>
	/// <li>a plus (<code>+</code>) or a minus (<code>-</code>) sign, indicating that
	/// the clause is required or prohibited respectively; or
	/// <li>a term followed by a colon, indicating the field to be searched. This
	/// enables one to construct queries which search multiple fields.
	/// </ul>
	/// A clause may be either:
	/// <ul>
	/// <li>a term, indicating all the documents that contain this term; or
	/// <li>a nested query, enclosed in parentheses. Note that this may be used with
	/// a <code>+</code>/<code>-</code> prefix to require any of a set of terms.
	/// </ul>
	/// Thus, in BNF, the query grammar is:
	/// <pre>
	/// Query  ::= ( Clause )
	/// Clause ::= [&quot;+&quot;, &quot;-&quot;] [&lt;TERM&gt; &quot;:&quot;] ( &lt;TERM&gt; | &quot;(&quot; Query &quot;)&quot; )
	/// </pre>
	/// <p>
	/// Examples of appropriately formatted queries can be found in the &lt;a
	/// href="
	/// <docRoot></docRoot>
	/// /org/apache/lucene/queryparser/classic/package-summary.html#package_description"&gt;
	/// query syntax documentation</a>.
	/// </p>
	/// </ul>
	/// <p>
	/// The text parser used by this helper is a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.StandardSyntaxParser
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Parser.StandardSyntaxParser</see>
	/// .
	/// <p/>
	/// <p>
	/// The query node processor used by this helper is a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</see>
	/// .
	/// <p/>
	/// <p>
	/// The builder used by this helper is a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Builders.StandardQueryTreeBuilder
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Builders.StandardQueryTreeBuilder
	/// 	</see>
	/// .
	/// <p/>
	/// </remarks>
	/// <seealso cref="StandardQueryParser">StandardQueryParser</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.StandardSyntaxParser
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Parser.StandardSyntaxParser</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Builders.StandardQueryTreeBuilder
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Builders.StandardQueryTreeBuilder
	/// 	</seealso>
	public class StandardQueryParser : QueryParserHelper, CommonQueryParserConfiguration
	{
		/// <summary>
		/// Constructs a
		/// <see cref="StandardQueryParser">StandardQueryParser</see>
		/// object.
		/// </summary>
		public StandardQueryParser() : base(new StandardQueryConfigHandler(), new StandardSyntaxParser
			(), new StandardQueryNodeProcessorPipeline(null), new StandardQueryTreeBuilder()
			)
		{
			SetEnablePositionIncrements(true);
		}

		/// <summary>
		/// Constructs a
		/// <see cref="StandardQueryParser">StandardQueryParser</see>
		/// object and sets an
		/// <see cref="Lucene.Net.Analysis.Analyzer">Lucene.Net.Analysis.Analyzer
		/// 	</see>
		/// to it. The same as:
		/// <ul>
		/// StandardQueryParser qp = new StandardQueryParser();
		/// qp.getQueryConfigHandler().setAnalyzer(analyzer);
		/// </ul>
		/// </summary>
		/// <param name="analyzer">the analyzer to be used by this query parser helper</param>
		public StandardQueryParser(Analyzer analyzer) : this()
		{
			this.SetAnalyzer(analyzer);
		}

		public override string ToString()
		{
			return "<StandardQueryParser config=\"" + this.GetQueryConfigHandler() + "\"/>";
		}

		/// <summary>
		/// Overrides
		/// <see cref="Lucene.Net.Queryparser.Flexible.Core.QueryParserHelper.Parse(string, string)
		/// 	">Lucene.Net.Queryparser.Flexible.Core.QueryParserHelper.Parse(string, string)
		/// 	</see>
		/// so it casts the
		/// return object to
		/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
		/// . For more reference about this method, check
		/// <see cref="Lucene.Net.Queryparser.Flexible.Core.QueryParserHelper.Parse(string, string)
		/// 	">Lucene.Net.Queryparser.Flexible.Core.QueryParserHelper.Parse(string, string)
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="query">the query string</param>
		/// <param name="defaultField">the default field used by the text parser</param>
		/// <returns>the object built from the query</returns>
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">if something wrong happens along the three phases
		/// 	</exception>
		public override object Parse(string query, string defaultField)
		{
			return (Query)base.Parse(query, defaultField);
		}

		/// <summary>
		/// Gets implicit operator setting, which will be either
		/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.AND
		/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.AND
		/// 	</see>
		/// or
		/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.OR
		/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.OR
		/// 	</see>
		/// .
		/// </summary>
		public virtual StandardQueryConfigHandler.Operator GetDefaultOperator()
		{
			return GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR
				);
		}

		/// <summary>Sets the boolean operator of the QueryParser.</summary>
		/// <remarks>
		/// Sets the boolean operator of the QueryParser. In default mode (
		/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.OR
		/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.OR
		/// 	</see>
		/// ) terms without any modifiers are considered optional:
		/// for example <code>capital of Hungary</code> is equal to
		/// <code>capital OR of OR Hungary</code>.<br/>
		/// In
		/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.AND
		/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.AND
		/// 	</see>
		/// mode terms are considered to be in conjunction: the
		/// above mentioned query is parsed as <code>capital AND of AND Hungary</code>
		/// </remarks>
		public virtual void SetDefaultOperator(StandardQueryConfigHandler.Operator @operator
			)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR
				, @operator);
		}

		/// <summary>Set to <code>true</code> to allow leading wildcard characters.</summary>
		/// <remarks>
		/// Set to <code>true</code> to allow leading wildcard characters.
		/// <p>
		/// When set, <code>*</code> or <code>?</code> are allowed as the first
		/// character of a PrefixQuery and WildcardQuery. Note that this can produce
		/// very slow queries on big indexes.
		/// <p>
		/// Default: false.
		/// </remarks>
		public virtual void SetLowercaseExpandedTerms(bool lowercaseExpandedTerms)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.LOWERCASE_EXPANDED_TERMS
				, lowercaseExpandedTerms);
		}

		/// <seealso cref="SetLowercaseExpandedTerms(bool)">SetLowercaseExpandedTerms(bool)</seealso>
		public virtual bool GetLowercaseExpandedTerms()
		{
			bool lowercaseExpandedTerms = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.LOWERCASE_EXPANDED_TERMS);
			if (lowercaseExpandedTerms == null)
			{
				return true;
			}
			else
			{
				return lowercaseExpandedTerms;
			}
		}

		/// <summary>Set to <code>true</code> to allow leading wildcard characters.</summary>
		/// <remarks>
		/// Set to <code>true</code> to allow leading wildcard characters.
		/// <p>
		/// When set, <code>*</code> or <code>?</code> are allowed as the first
		/// character of a PrefixQuery and WildcardQuery. Note that this can produce
		/// very slow queries on big indexes.
		/// <p>
		/// Default: false.
		/// </remarks>
		public virtual void SetAllowLeadingWildcard(bool allowLeadingWildcard)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.ALLOW_LEADING_WILDCARD
				, allowLeadingWildcard);
		}

		/// <summary>Set to <code>true</code> to enable position increments in result query.</summary>
		/// <remarks>
		/// Set to <code>true</code> to enable position increments in result query.
		/// <p>
		/// When set, result phrase and multi-phrase queries will be aware of position
		/// increments. Useful when e.g. a StopFilter increases the position increment
		/// of the token that follows an omitted token.
		/// <p>
		/// Default: false.
		/// </remarks>
		public virtual void SetEnablePositionIncrements(bool enabled)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.ENABLE_POSITION_INCREMENTS
				, enabled);
		}

		/// <seealso cref="SetEnablePositionIncrements(bool)">SetEnablePositionIncrements(bool)
		/// 	</seealso>
		public virtual bool GetEnablePositionIncrements()
		{
			bool enablePositionsIncrements = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.ENABLE_POSITION_INCREMENTS);
			if (enablePositionsIncrements == null)
			{
				return false;
			}
			else
			{
				return enablePositionsIncrements;
			}
		}

		/// <summary>
		/// By default, it uses
		/// <see cref="Lucene.Net.Search.MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
		/// 	">Lucene.Net.Search.MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT</see>
		/// when creating a
		/// prefix, wildcard and range queries. This implementation is generally
		/// preferable because it a) Runs faster b) Does not have the scarcity of terms
		/// unduly influence score c) avoids any
		/// <see cref="Sharpen.TooManyListenersException">Sharpen.TooManyListenersException</see>
		/// exception. However, if your application really needs to use the
		/// old-fashioned boolean queries expansion rewriting and the above points are
		/// not relevant then use this change the rewrite method.
		/// </summary>
		public virtual void SetMultiTermRewriteMethod(MultiTermQuery.RewriteMethod method
			)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.MULTI_TERM_REWRITE_METHOD
				, method);
		}

		/// <seealso cref="SetMultiTermRewriteMethod(Lucene.Net.Search.MultiTermQuery.RewriteMethod)
		/// 	">SetMultiTermRewriteMethod(Lucene.Net.Search.MultiTermQuery.RewriteMethod)
		/// 	</seealso>
		public virtual MultiTermQuery.RewriteMethod GetMultiTermRewriteMethod()
		{
			return GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys.MULTI_TERM_REWRITE_METHOD
				);
		}

		/// <summary>
		/// Set the fields a query should be expanded to when the field is
		/// <code>null</code>
		/// </summary>
		/// <param name="fields">the fields used to expand the query</param>
		public virtual void SetMultiFields(CharSequence[] fields)
		{
			if (fields == null)
			{
				fields = new CharSequence[0];
			}
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.MULTI_FIELDS
				, fields);
		}

		/// <summary>
		/// Returns the fields used to expand the query when the field for a
		/// certain query is <code>null</code>
		/// </summary>
		/// <param name="fields">the fields used to expand the query</param>
		public virtual void GetMultiFields(CharSequence[] fields)
		{
			GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys.MULTI_FIELDS
				);
		}

		/// <summary>Set the prefix length for fuzzy queries.</summary>
		/// <remarks>Set the prefix length for fuzzy queries. Default is 0.</remarks>
		/// <param name="fuzzyPrefixLength">The fuzzyPrefixLength to set.</param>
		public virtual void SetFuzzyPrefixLength(int fuzzyPrefixLength)
		{
			QueryConfigHandler config = GetQueryConfigHandler();
			FuzzyConfig fuzzyConfig = config.Get(StandardQueryConfigHandler.ConfigurationKeys
				.FUZZY_CONFIG);
			if (fuzzyConfig == null)
			{
				fuzzyConfig = new FuzzyConfig();
				config.Set(StandardQueryConfigHandler.ConfigurationKeys.FUZZY_CONFIG, fuzzyConfig
					);
			}
			fuzzyConfig.SetPrefixLength(fuzzyPrefixLength);
		}

		public virtual void SetNumericConfigMap(IDictionary<string, NumericConfig> numericConfigMap
			)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG_MAP
				, numericConfigMap);
		}

		public virtual IDictionary<string, NumericConfig> GetNumericConfigMap()
		{
			return GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG_MAP
				);
		}

		/// <summary>Set locale used by date range parsing.</summary>
		/// <remarks>Set locale used by date range parsing.</remarks>
		public virtual void SetLocale(CultureInfo locale)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.LOCALE, 
				locale);
		}

		/// <summary>Returns current locale, allowing access by subclasses.</summary>
		/// <remarks>Returns current locale, allowing access by subclasses.</remarks>
		public virtual CultureInfo GetLocale()
		{
			return GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys.LOCALE
				);
		}

		public virtual void SetTimeZone(TimeZoneInfo timeZone)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.TIMEZONE
				, timeZone);
		}

		public virtual TimeZoneInfo GetTimeZone()
		{
			return GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys.TIMEZONE
				);
		}

		/// <summary>Sets the default slop for phrases.</summary>
		/// <remarks>
		/// Sets the default slop for phrases. If zero, then exact phrase matches are
		/// required. Default value is zero.
		/// </remarks>
		[Obsolete]
		[System.ObsoleteAttribute(@"renamed to SetPhraseSlop(int)")]
		public virtual void SetDefaultPhraseSlop(int defaultPhraseSlop)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP
				, defaultPhraseSlop);
		}

		/// <summary>Sets the default slop for phrases.</summary>
		/// <remarks>
		/// Sets the default slop for phrases. If zero, then exact phrase matches are
		/// required. Default value is zero.
		/// </remarks>
		public virtual void SetPhraseSlop(int defaultPhraseSlop)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP
				, defaultPhraseSlop);
		}

		public virtual void SetAnalyzer(Analyzer analyzer)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.ANALYZER
				, analyzer);
		}

		public virtual Analyzer GetAnalyzer()
		{
			return GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys.ANALYZER
				);
		}

		/// <seealso cref="SetAllowLeadingWildcard(bool)">SetAllowLeadingWildcard(bool)</seealso>
		public virtual bool GetAllowLeadingWildcard()
		{
			bool allowLeadingWildcard = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.ALLOW_LEADING_WILDCARD);
			if (allowLeadingWildcard == null)
			{
				return false;
			}
			else
			{
				return allowLeadingWildcard;
			}
		}

		/// <summary>Get the minimal similarity for fuzzy queries.</summary>
		/// <remarks>Get the minimal similarity for fuzzy queries.</remarks>
		public virtual float GetFuzzyMinSim()
		{
			FuzzyConfig fuzzyConfig = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.FUZZY_CONFIG);
			if (fuzzyConfig == null)
			{
				return FuzzyQuery.defaultMinSimilarity;
			}
			else
			{
				return fuzzyConfig.GetMinSimilarity();
			}
		}

		/// <summary>Get the prefix length for fuzzy queries.</summary>
		/// <remarks>Get the prefix length for fuzzy queries.</remarks>
		/// <returns>Returns the fuzzyPrefixLength.</returns>
		public virtual int GetFuzzyPrefixLength()
		{
			FuzzyConfig fuzzyConfig = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.FUZZY_CONFIG);
			if (fuzzyConfig == null)
			{
				return FuzzyQuery.defaultPrefixLength;
			}
			else
			{
				return fuzzyConfig.GetPrefixLength();
			}
		}

		/// <summary>Gets the default slop for phrases.</summary>
		/// <remarks>Gets the default slop for phrases.</remarks>
		public virtual int GetPhraseSlop()
		{
			int phraseSlop = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.PHRASE_SLOP);
			if (phraseSlop == null)
			{
				return 0;
			}
			else
			{
				return phraseSlop;
			}
		}

		/// <summary>Set the minimum similarity for fuzzy queries.</summary>
		/// <remarks>
		/// Set the minimum similarity for fuzzy queries. Default is defined on
		/// <see cref="Lucene.Net.Search.FuzzyQuery.defaultMinSimilarity">Lucene.Net.Search.FuzzyQuery.defaultMinSimilarity
		/// 	</see>
		/// .
		/// </remarks>
		public virtual void SetFuzzyMinSim(float fuzzyMinSim)
		{
			QueryConfigHandler config = GetQueryConfigHandler();
			FuzzyConfig fuzzyConfig = config.Get(StandardQueryConfigHandler.ConfigurationKeys
				.FUZZY_CONFIG);
			if (fuzzyConfig == null)
			{
				fuzzyConfig = new FuzzyConfig();
				config.Set(StandardQueryConfigHandler.ConfigurationKeys.FUZZY_CONFIG, fuzzyConfig
					);
			}
			fuzzyConfig.SetMinSimilarity(fuzzyMinSim);
		}

		/// <summary>Sets the boost used for each field.</summary>
		/// <remarks>Sets the boost used for each field.</remarks>
		/// <param name="boosts">a collection that maps a field to its boost</param>
		public virtual void SetFieldsBoost(IDictionary<string, float> boosts)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.FIELD_BOOST_MAP
				, boosts);
		}

		/// <summary>Returns the field to boost map used to set boost for each field.</summary>
		/// <remarks>Returns the field to boost map used to set boost for each field.</remarks>
		/// <returns>the field to boost map</returns>
		public virtual IDictionary<string, float> GetFieldsBoost()
		{
			return GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys.FIELD_BOOST_MAP
				);
		}

		/// <summary>
		/// Sets the default
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// used for certain field when
		/// no
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// is defined for this field.
		/// </summary>
		/// <param name="dateResolution">
		/// the default
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// </param>
		public virtual void SetDateResolution(DateTools.Resolution dateResolution)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION
				, dateResolution);
		}

		/// <summary>
		/// Returns the default
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// used for certain field when
		/// no
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// is defined for this field.
		/// </summary>
		/// <returns>
		/// the default
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// </returns>
		public virtual DateTools.Resolution GetDateResolution()
		{
			return GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys.DATE_RESOLUTION
				);
		}

		/// <summary>
		/// Sets the
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// used for each field
		/// </summary>
		/// <param name="dateRes">
		/// a collection that maps a field to its
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// </param>
		[Obsolete]
		[System.ObsoleteAttribute(@"this method was renamed to SetDateResolutionMap(System.Collections.Generic.IDictionary{K, V})"
			)]
		public virtual void SetDateResolution(IDictionary<CharSequence, DateTools.Resolution
			> dateRes)
		{
			SetDateResolutionMap(dateRes);
		}

		/// <summary>
		/// Returns the field to
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// map used to normalize each date field.
		/// </summary>
		/// <returns>
		/// the field to
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// map
		/// </returns>
		public virtual IDictionary<CharSequence, DateTools.Resolution> GetDateResolutionMap
			()
		{
			return GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP
				);
		}

		/// <summary>
		/// Sets the
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// used for each field
		/// </summary>
		/// <param name="dateRes">
		/// a collection that maps a field to its
		/// <see cref="Lucene.Net.Document.DateTools.Resolution">Lucene.Net.Document.DateTools.Resolution
		/// 	</see>
		/// </param>
		public virtual void SetDateResolutionMap(IDictionary<CharSequence, DateTools.Resolution
			> dateRes)
		{
			GetQueryConfigHandler().Set(StandardQueryConfigHandler.ConfigurationKeys.FIELD_DATE_RESOLUTION_MAP
				, dateRes);
		}
	}
}
