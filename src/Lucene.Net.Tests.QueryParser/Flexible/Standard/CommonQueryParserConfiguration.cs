/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Globalization;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Queryparser.Flexible.Standard;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard
{
	/// <summary>Configuration options common across queryparser implementations.</summary>
	/// <remarks>Configuration options common across queryparser implementations.</remarks>
	public interface CommonQueryParserConfiguration
	{
		/// <summary>
		/// Whether terms of multi-term queries (e.g., wildcard,
		/// prefix, fuzzy and range) should be automatically
		/// lower-cased or not.
		/// </summary>
		/// <remarks>
		/// Whether terms of multi-term queries (e.g., wildcard,
		/// prefix, fuzzy and range) should be automatically
		/// lower-cased or not.  Default is <code>true</code>.
		/// </remarks>
		void SetLowercaseExpandedTerms(bool lowercaseExpandedTerms);

		/// <seealso cref="SetLowercaseExpandedTerms(bool)">SetLowercaseExpandedTerms(bool)</seealso>
		bool GetLowercaseExpandedTerms();

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
		void SetAllowLeadingWildcard(bool allowLeadingWildcard);

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
		void SetEnablePositionIncrements(bool enabled);

		/// <seealso cref="SetEnablePositionIncrements(bool)">SetEnablePositionIncrements(bool)
		/// 	</seealso>
		bool GetEnablePositionIncrements();

		/// <summary>
		/// By default, it uses
		/// <see cref="Org.Apache.Lucene.Search.MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
		/// 	">Org.Apache.Lucene.Search.MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT</see>
		/// when creating a
		/// prefix, wildcard and range queries. This implementation is generally
		/// preferable because it a) Runs faster b) Does not have the scarcity of terms
		/// unduly influence score c) avoids any
		/// <see cref="Sharpen.TooManyListenersException">Sharpen.TooManyListenersException</see>
		/// exception. However, if your application really needs to use the
		/// old-fashioned boolean queries expansion rewriting and the above points are
		/// not relevant then use this change the rewrite method.
		/// </summary>
		void SetMultiTermRewriteMethod(MultiTermQuery.RewriteMethod method);

		/// <seealso cref="SetMultiTermRewriteMethod(Org.Apache.Lucene.Search.MultiTermQuery.RewriteMethod)
		/// 	">SetMultiTermRewriteMethod(Org.Apache.Lucene.Search.MultiTermQuery.RewriteMethod)
		/// 	</seealso>
		MultiTermQuery.RewriteMethod GetMultiTermRewriteMethod();

		/// <summary>Set the prefix length for fuzzy queries.</summary>
		/// <remarks>Set the prefix length for fuzzy queries. Default is 0.</remarks>
		/// <param name="fuzzyPrefixLength">The fuzzyPrefixLength to set.</param>
		void SetFuzzyPrefixLength(int fuzzyPrefixLength);

		/// <summary>Set locale used by date range parsing.</summary>
		/// <remarks>Set locale used by date range parsing.</remarks>
		void SetLocale(CultureInfo locale);

		/// <summary>Returns current locale, allowing access by subclasses.</summary>
		/// <remarks>Returns current locale, allowing access by subclasses.</remarks>
		CultureInfo GetLocale();

		void SetTimeZone(TimeZoneInfo timeZone);

		TimeZoneInfo GetTimeZone();

		/// <summary>Sets the default slop for phrases.</summary>
		/// <remarks>
		/// Sets the default slop for phrases. If zero, then exact phrase matches are
		/// required. Default value is zero.
		/// </remarks>
		void SetPhraseSlop(int defaultPhraseSlop);

		Analyzer GetAnalyzer();

		/// <seealso cref="SetAllowLeadingWildcard(bool)">SetAllowLeadingWildcard(bool)</seealso>
		bool GetAllowLeadingWildcard();

		/// <summary>Get the minimal similarity for fuzzy queries.</summary>
		/// <remarks>Get the minimal similarity for fuzzy queries.</remarks>
		float GetFuzzyMinSim();

		/// <summary>Get the prefix length for fuzzy queries.</summary>
		/// <remarks>Get the prefix length for fuzzy queries.</remarks>
		/// <returns>Returns the fuzzyPrefixLength.</returns>
		int GetFuzzyPrefixLength();

		/// <summary>Gets the default slop for phrases.</summary>
		/// <remarks>Gets the default slop for phrases.</remarks>
		int GetPhraseSlop();

		/// <summary>Set the minimum similarity for fuzzy queries.</summary>
		/// <remarks>
		/// Set the minimum similarity for fuzzy queries. Default is defined on
		/// <see cref="Org.Apache.Lucene.Search.FuzzyQuery.defaultMinSimilarity">Org.Apache.Lucene.Search.FuzzyQuery.defaultMinSimilarity
		/// 	</see>
		/// .
		/// </remarks>
		void SetFuzzyMinSim(float fuzzyMinSim);

		/// <summary>
		/// Sets the default
		/// <see cref="Org.Apache.Lucene.Document.DateTools.Resolution">Org.Apache.Lucene.Document.DateTools.Resolution
		/// 	</see>
		/// used for certain field when
		/// no
		/// <see cref="Org.Apache.Lucene.Document.DateTools.Resolution">Org.Apache.Lucene.Document.DateTools.Resolution
		/// 	</see>
		/// is defined for this field.
		/// </summary>
		/// <param name="dateResolution">
		/// the default
		/// <see cref="Org.Apache.Lucene.Document.DateTools.Resolution">Org.Apache.Lucene.Document.DateTools.Resolution
		/// 	</see>
		/// </param>
		void SetDateResolution(DateTools.Resolution dateResolution);
	}
}
