/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Classic;
using Org.Apache.Lucene.Queryparser.Flexible.Standard;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Classic
{
	/// <summary>
	/// This class is overridden by QueryParser in QueryParser.jj
	/// and acts to separate the majority of the Java code from the .jj grammar file.
	/// </summary>
	/// <remarks>
	/// This class is overridden by QueryParser in QueryParser.jj
	/// and acts to separate the majority of the Java code from the .jj grammar file.
	/// </remarks>
	public abstract class QueryParserBase : QueryBuilder, CommonQueryParserConfiguration
	{
		/// <summary>Do not catch this exception in your code, it means you are using methods that you should no longer use.
		/// 	</summary>
		/// <remarks>Do not catch this exception in your code, it means you are using methods that you should no longer use.
		/// 	</remarks>
		[System.Serializable]
		public class MethodRemovedUseAnother : Exception
		{
		}

		internal const int CONJ_NONE = 0;

		internal const int CONJ_AND = 1;

		internal const int CONJ_OR = 2;

		internal const int MOD_NONE = 0;

		internal const int MOD_NOT = 10;

		internal const int MOD_REQ = 11;

		/// <summary>Alternative form of QueryParser.Operator.AND</summary>
		public static readonly QueryParser.Operator AND_OPERATOR = QueryParser.Operator.AND;

		/// <summary>Alternative form of QueryParser.Operator.OR</summary>
		public static readonly QueryParser.Operator OR_OPERATOR = QueryParser.Operator.OR;

		/// <summary>The actual operator that parser uses to combine query terms</summary>
		internal QueryParser.Operator @operator = OR_OPERATOR;

		internal bool lowercaseExpandedTerms = true;

		internal MultiTermQuery.RewriteMethod multiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;

		internal bool allowLeadingWildcard = false;

		protected internal string field;

		internal int phraseSlop = 0;

		internal float fuzzyMinSim = FuzzyQuery.defaultMinSimilarity;

		internal int fuzzyPrefixLength = FuzzyQuery.defaultPrefixLength;

		internal CultureInfo locale = CultureInfo.CurrentCulture;

		internal TimeZoneInfo timeZone = System.TimeZoneInfo.Local;

		internal DateTools.Resolution dateResolution = null;

		internal IDictionary<string, DateTools.Resolution> fieldToDateResolution = null;

		internal bool analyzeRangeTerms = false;

		internal bool autoGeneratePhraseQueries;

		public QueryParserBase() : base(null)
		{
		}

		// make it possible to call setDefaultOperator() without accessing
		// the nested class:
		// the default date resolution
		// maps field names to date resolutions
		//Whether or not to analyze range terms when constructing RangeQuerys
		// (For example, analyzing terms into collation keys for locale-sensitive RangeQuery)
		// So the generated QueryParser(CharStream) won't error out
		/// <summary>Initializes a query parser.</summary>
		/// <remarks>Initializes a query parser.  Called by the QueryParser constructor</remarks>
		/// <param name="matchVersion">Lucene version to match. See <a href="QueryParser.html#version">here</a>.
		/// 	</param>
		/// <param name="f">the default field for query terms.</param>
		/// <param name="a">used to find terms in the query text.</param>
		public virtual void Init(Version matchVersion, string f, Analyzer a)
		{
			SetAnalyzer(a);
			field = f;
			if (VersionHelper.OnOrAfter(matchVersion, Version.LUCENE_31))
			{
				SetAutoGeneratePhraseQueries(false);
			}
			else
			{
				SetAutoGeneratePhraseQueries(true);
			}
		}

		// the generated parser will create these in QueryParser
		public abstract void ReInit(CharStream stream);

		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		public abstract Query TopLevelQuery(string field);

		/// <summary>
		/// Parses a query string, returning a
		/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
		/// .
		/// </summary>
		/// <param name="query">the query string to be parsed.</param>
		/// <exception cref="ParseException">if the parsing fails</exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		public virtual Query Parse(string query)
		{
			ReInit(new FastCharStream(new StringReader(query)));
			try
			{
				// TopLevelQuery is a Query followed by the end-of-input (EOF)
				Query res = TopLevelQuery(field);
				return res != null ? res : NewBooleanQuery(false);
			}
			catch (ParseException tme)
			{
				// rethrow to include the original query:
				ParseException e = new ParseException("Cannot parse '" + query + "': " + tme.Message
					);
				Sharpen.Extensions.InitCause(e, tme);
				throw e;
			}
			catch (TokenMgrError tme)
			{
				ParseException e = new ParseException("Cannot parse '" + query + "': " + tme.Message
					);
				Sharpen.Extensions.InitCause(e, tme);
				throw e;
			}
			catch (BooleanQuery.TooManyClauses tmc)
			{
				ParseException e = new ParseException("Cannot parse '" + query + "': too many boolean clauses"
					);
				Sharpen.Extensions.InitCause(e, tmc);
				throw e;
			}
		}

		/// <returns>Returns the default field.</returns>
		public virtual string GetField()
		{
			return field;
		}

		/// <seealso cref="SetAutoGeneratePhraseQueries(bool)">SetAutoGeneratePhraseQueries(bool)
		/// 	</seealso>
		public bool GetAutoGeneratePhraseQueries()
		{
			return autoGeneratePhraseQueries;
		}

		/// <summary>
		/// Set to true if phrase queries will be automatically generated
		/// when the analyzer returns more than one term from whitespace
		/// delimited text.
		/// </summary>
		/// <remarks>
		/// Set to true if phrase queries will be automatically generated
		/// when the analyzer returns more than one term from whitespace
		/// delimited text.
		/// NOTE: this behavior may not be suitable for all languages.
		/// <p>
		/// Set to false if phrase queries should only be generated when
		/// surrounded by double quotes.
		/// </remarks>
		public void SetAutoGeneratePhraseQueries(bool value)
		{
			this.autoGeneratePhraseQueries = value;
		}

		/// <summary>Get the minimal similarity for fuzzy queries.</summary>
		/// <remarks>Get the minimal similarity for fuzzy queries.</remarks>
		public virtual float GetFuzzyMinSim()
		{
			return fuzzyMinSim;
		}

		/// <summary>Set the minimum similarity for fuzzy queries.</summary>
		/// <remarks>
		/// Set the minimum similarity for fuzzy queries.
		/// Default is 2f.
		/// </remarks>
		public virtual void SetFuzzyMinSim(float fuzzyMinSim)
		{
			this.fuzzyMinSim = fuzzyMinSim;
		}

		/// <summary>Get the prefix length for fuzzy queries.</summary>
		/// <remarks>Get the prefix length for fuzzy queries.</remarks>
		/// <returns>Returns the fuzzyPrefixLength.</returns>
		public virtual int GetFuzzyPrefixLength()
		{
			return fuzzyPrefixLength;
		}

		/// <summary>Set the prefix length for fuzzy queries.</summary>
		/// <remarks>Set the prefix length for fuzzy queries. Default is 0.</remarks>
		/// <param name="fuzzyPrefixLength">The fuzzyPrefixLength to set.</param>
		public virtual void SetFuzzyPrefixLength(int fuzzyPrefixLength)
		{
			this.fuzzyPrefixLength = fuzzyPrefixLength;
		}

		/// <summary>Sets the default slop for phrases.</summary>
		/// <remarks>
		/// Sets the default slop for phrases.  If zero, then exact phrase matches
		/// are required.  Default value is zero.
		/// </remarks>
		public virtual void SetPhraseSlop(int phraseSlop)
		{
			this.phraseSlop = phraseSlop;
		}

		/// <summary>Gets the default slop for phrases.</summary>
		/// <remarks>Gets the default slop for phrases.</remarks>
		public virtual int GetPhraseSlop()
		{
			return phraseSlop;
		}

		/// <summary>Set to <code>true</code> to allow leading wildcard characters.</summary>
		/// <remarks>
		/// Set to <code>true</code> to allow leading wildcard characters.
		/// <p>
		/// When set, <code>*</code> or <code>?</code> are allowed as
		/// the first character of a PrefixQuery and WildcardQuery.
		/// Note that this can produce very slow
		/// queries on big indexes.
		/// <p>
		/// Default: false.
		/// </remarks>
		public virtual void SetAllowLeadingWildcard(bool allowLeadingWildcard)
		{
			this.allowLeadingWildcard = allowLeadingWildcard;
		}

		/// <seealso cref="SetAllowLeadingWildcard(bool)">SetAllowLeadingWildcard(bool)</seealso>
		public virtual bool GetAllowLeadingWildcard()
		{
			return allowLeadingWildcard;
		}

		/// <summary>Sets the boolean operator of the QueryParser.</summary>
		/// <remarks>
		/// Sets the boolean operator of the QueryParser.
		/// In default mode (<code>OR_OPERATOR</code>) terms without any modifiers
		/// are considered optional: for example <code>capital of Hungary</code> is equal to
		/// <code>capital OR of OR Hungary</code>.<br/>
		/// In <code>AND_OPERATOR</code> mode terms are considered to be in conjunction: the
		/// above mentioned query is parsed as <code>capital AND of AND Hungary</code>
		/// </remarks>
		public virtual void SetDefaultOperator(QueryParser.Operator op)
		{
			this.@operator = op;
		}

		/// <summary>
		/// Gets implicit operator setting, which will be either AND_OPERATOR
		/// or OR_OPERATOR.
		/// </summary>
		/// <remarks>
		/// Gets implicit operator setting, which will be either AND_OPERATOR
		/// or OR_OPERATOR.
		/// </remarks>
		public virtual QueryParser.Operator GetDefaultOperator()
		{
			return @operator;
		}

		/// <summary>
		/// Whether terms of wildcard, prefix, fuzzy and range queries are to be automatically
		/// lower-cased or not.
		/// </summary>
		/// <remarks>
		/// Whether terms of wildcard, prefix, fuzzy and range queries are to be automatically
		/// lower-cased or not.  Default is <code>true</code>.
		/// </remarks>
		public virtual void SetLowercaseExpandedTerms(bool lowercaseExpandedTerms)
		{
			this.lowercaseExpandedTerms = lowercaseExpandedTerms;
		}

		/// <seealso cref="SetLowercaseExpandedTerms(bool)">SetLowercaseExpandedTerms(bool)</seealso>
		public virtual bool GetLowercaseExpandedTerms()
		{
			return lowercaseExpandedTerms;
		}

		/// <summary>
		/// By default QueryParser uses
		/// <see cref="Org.Apache.Lucene.Search.MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
		/// 	">Org.Apache.Lucene.Search.MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT</see>
		/// when creating a
		/// <see cref="Org.Apache.Lucene.Search.PrefixQuery">Org.Apache.Lucene.Search.PrefixQuery
		/// 	</see>
		/// ,
		/// <see cref="Org.Apache.Lucene.Search.WildcardQuery">Org.Apache.Lucene.Search.WildcardQuery
		/// 	</see>
		/// or
		/// <see cref="Org.Apache.Lucene.Search.TermRangeQuery">Org.Apache.Lucene.Search.TermRangeQuery
		/// 	</see>
		/// . This implementation is generally preferable because it
		/// a) Runs faster b) Does not have the scarcity of terms unduly influence score
		/// c) avoids any
		/// <see cref="Org.Apache.Lucene.Search.BooleanQuery.TooManyClauses">Org.Apache.Lucene.Search.BooleanQuery.TooManyClauses
		/// 	</see>
		/// exception.
		/// However, if your application really needs to use the
		/// old-fashioned
		/// <see cref="Org.Apache.Lucene.Search.BooleanQuery">Org.Apache.Lucene.Search.BooleanQuery
		/// 	</see>
		/// expansion rewriting and the above
		/// points are not relevant then use this to change
		/// the rewrite method.
		/// </summary>
		public virtual void SetMultiTermRewriteMethod(MultiTermQuery.RewriteMethod method
			)
		{
			multiTermRewriteMethod = method;
		}

		/// <seealso cref="SetMultiTermRewriteMethod(Org.Apache.Lucene.Search.MultiTermQuery.RewriteMethod)
		/// 	">SetMultiTermRewriteMethod(Org.Apache.Lucene.Search.MultiTermQuery.RewriteMethod)
		/// 	</seealso>
		public virtual MultiTermQuery.RewriteMethod GetMultiTermRewriteMethod()
		{
			return multiTermRewriteMethod;
		}

		/// <summary>
		/// Set locale used by date range parsing, lowercasing, and other
		/// locale-sensitive operations.
		/// </summary>
		/// <remarks>
		/// Set locale used by date range parsing, lowercasing, and other
		/// locale-sensitive operations.
		/// </remarks>
		public virtual void SetLocale(CultureInfo locale)
		{
			this.locale = locale;
		}

		/// <summary>Returns current locale, allowing access by subclasses.</summary>
		/// <remarks>Returns current locale, allowing access by subclasses.</remarks>
		public virtual CultureInfo GetLocale()
		{
			return locale;
		}

		public virtual void SetTimeZone(TimeZoneInfo timeZone)
		{
			this.timeZone = timeZone;
		}

		public virtual TimeZoneInfo GetTimeZone()
		{
			return timeZone;
		}

		/// <summary>
		/// Sets the default date resolution used by RangeQueries for fields for which no
		/// specific date resolutions has been set.
		/// </summary>
		/// <remarks>
		/// Sets the default date resolution used by RangeQueries for fields for which no
		/// specific date resolutions has been set. Field specific resolutions can be set
		/// with
		/// <see cref="SetDateResolution(string, Org.Apache.Lucene.Document.DateTools.Resolution)
		/// 	">SetDateResolution(string, Org.Apache.Lucene.Document.DateTools.Resolution)</see>
		/// .
		/// </remarks>
		/// <param name="dateResolution">the default date resolution to set</param>
		public virtual void SetDateResolution(DateTools.Resolution dateResolution)
		{
			this.dateResolution = dateResolution;
		}

		/// <summary>Sets the date resolution used by RangeQueries for a specific field.</summary>
		/// <remarks>Sets the date resolution used by RangeQueries for a specific field.</remarks>
		/// <param name="fieldName">field for which the date resolution is to be set</param>
		/// <param name="dateResolution">date resolution to set</param>
		public virtual void SetDateResolution(string fieldName, DateTools.Resolution dateResolution
			)
		{
			if (fieldName == null)
			{
				throw new ArgumentException("Field cannot be null.");
			}
			if (fieldToDateResolution == null)
			{
				// lazily initialize HashMap
				fieldToDateResolution = new Dictionary<string, DateTools.Resolution>();
			}
			fieldToDateResolution.Put(fieldName, dateResolution);
		}

		/// <summary>Returns the date resolution that is used by RangeQueries for the given field.
		/// 	</summary>
		/// <remarks>
		/// Returns the date resolution that is used by RangeQueries for the given field.
		/// Returns null, if no default or field specific date resolution has been set
		/// for the given field.
		/// </remarks>
		public virtual DateTools.Resolution GetDateResolution(string fieldName)
		{
			if (fieldName == null)
			{
				throw new ArgumentException("Field cannot be null.");
			}
			if (fieldToDateResolution == null)
			{
				// no field specific date resolutions set; return default date resolution instead
				return this.dateResolution;
			}
			DateTools.Resolution resolution = fieldToDateResolution.Get(fieldName);
			if (resolution == null)
			{
				// no date resolutions set for the given field; return default date resolution instead
				resolution = this.dateResolution;
			}
			return resolution;
		}

		/// <summary>
		/// Set whether or not to analyze range terms when constructing
		/// <see cref="Org.Apache.Lucene.Search.TermRangeQuery">Org.Apache.Lucene.Search.TermRangeQuery
		/// 	</see>
		/// s.
		/// For example, setting this to true can enable analyzing terms into
		/// collation keys for locale-sensitive
		/// <see cref="Org.Apache.Lucene.Search.TermRangeQuery">Org.Apache.Lucene.Search.TermRangeQuery
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="analyzeRangeTerms">whether or not terms should be analyzed for RangeQuerys
		/// 	</param>
		public virtual void SetAnalyzeRangeTerms(bool analyzeRangeTerms)
		{
			this.analyzeRangeTerms = analyzeRangeTerms;
		}

		/// <returns>
		/// whether or not to analyze range terms when constructing
		/// <see cref="Org.Apache.Lucene.Search.TermRangeQuery">Org.Apache.Lucene.Search.TermRangeQuery
		/// 	</see>
		/// s.
		/// </returns>
		public virtual bool GetAnalyzeRangeTerms()
		{
			return analyzeRangeTerms;
		}

		protected internal virtual void AddClause(IList<BooleanClause> clauses, int conj, 
			int mods, Query q)
		{
			bool required;
			bool prohibited;
			// If this term is introduced by AND, make the preceding term required,
			// unless it's already prohibited
			if (clauses.Count > 0 && conj == CONJ_AND)
			{
				BooleanClause c = clauses[clauses.Count - 1];
				if (!c.IsProhibited())
				{
					c.SetOccur(BooleanClause.Occur.MUST);
				}
			}
			if (clauses.Count > 0 && @operator == AND_OPERATOR && conj == CONJ_OR)
			{
				// If this term is introduced by OR, make the preceding term optional,
				// unless it's prohibited (that means we leave -a OR b but +a OR b-->a OR b)
				// notice if the input is a OR b, first term is parsed as required; without
				// this modification a OR b would parsed as +a OR b
				BooleanClause c = clauses[clauses.Count - 1];
				if (!c.IsProhibited())
				{
					c.SetOccur(BooleanClause.Occur.SHOULD);
				}
			}
			// We might have been passed a null query; the term might have been
			// filtered away by the analyzer.
			if (q == null)
			{
				return;
			}
			if (@operator == OR_OPERATOR)
			{
				// We set REQUIRED if we're introduced by AND or +; PROHIBITED if
				// introduced by NOT or -; make sure not to set both.
				prohibited = (mods == MOD_NOT);
				required = (mods == MOD_REQ);
				if (conj == CONJ_AND && !prohibited)
				{
					required = true;
				}
			}
			else
			{
				// We set PROHIBITED if we're introduced by NOT or -; We set REQUIRED
				// if not PROHIBITED and not introduced by OR
				prohibited = (mods == MOD_NOT);
				required = (!prohibited && conj != CONJ_OR);
			}
			if (required && !prohibited)
			{
				clauses.AddItem(NewBooleanClause(q, BooleanClause.Occur.MUST));
			}
			else
			{
				if (!required && !prohibited)
				{
					clauses.AddItem(NewBooleanClause(q, BooleanClause.Occur.SHOULD));
				}
				else
				{
					if (!required && prohibited)
					{
						clauses.AddItem(NewBooleanClause(q, BooleanClause.Occur.MUST_NOT));
					}
					else
					{
						throw new RuntimeException("Clause cannot be both required and prohibited");
					}
				}
			}
		}

		/// <exception>
		/// org.apache.lucene.queryparser.classic.ParseException
		/// throw in overridden method to disallow
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		protected internal virtual Query GetFieldQuery(string field, string queryText, bool
			 quoted)
		{
			return NewFieldQuery(GetAnalyzer(), field, queryText, quoted);
		}

		/// <exception>
		/// org.apache.lucene.queryparser.classic.ParseException
		/// throw in overridden method to disallow
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		protected internal virtual Query NewFieldQuery(Analyzer analyzer, string field, string
			 queryText, bool quoted)
		{
			BooleanClause.Occur occur = @operator == QueryParser.Operator.AND ? BooleanClause.Occur
				.MUST : BooleanClause.Occur.SHOULD;
			return CreateFieldQuery(analyzer, occur, field, queryText, quoted || autoGeneratePhraseQueries
				, phraseSlop);
		}

		/// <summary>
		/// Base implementation delegates to
		/// <see cref="GetFieldQuery(string, string, bool)">GetFieldQuery(string, string, bool)
		/// 	</see>
		/// .
		/// This method may be overridden, for example, to return
		/// a SpanNearQuery instead of a PhraseQuery.
		/// </summary>
		/// <exception>
		/// org.apache.lucene.queryparser.classic.ParseException
		/// throw in overridden method to disallow
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		protected internal virtual Query GetFieldQuery(string field, string queryText, int
			 slop)
		{
			Query query = GetFieldQuery(field, queryText, true);
			if (query is PhraseQuery)
			{
				((PhraseQuery)query).SetSlop(slop);
			}
			if (query is MultiPhraseQuery)
			{
				((MultiPhraseQuery)query).SetSlop(slop);
			}
			return query;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		protected internal virtual Query GetRangeQuery(string field, string part1, string
			 part2, bool startInclusive, bool endInclusive)
		{
			if (lowercaseExpandedTerms)
			{
				part1 = part1 == null ? null : part1.ToLower(locale);
				part2 = part2 == null ? null : part2.ToLower(locale);
			}
			DateFormat df = DateFormat.GetDateInstance(DateFormat.SHORT, locale);
			df.SetLenient(true);
			DateTools.Resolution resolution = GetDateResolution(field);
			try
			{
				part1 = DateTools.DateToString(df.Parse(part1), resolution);
			}
			catch (Exception)
			{
			}
			try
			{
				DateTime d2 = df.Parse(part2);
				if (endInclusive)
				{
					// The user can only specify the date, not the time, so make sure
					// the time is set to the latest possible time of that date to really
					// include all documents:
					Calendar cal = Calendar.GetInstance(timeZone, locale);
					cal.SetTime(d2);
					cal.Set(Calendar.HOUR_OF_DAY, 23);
					cal.Set(Calendar.MINUTE, 59);
					cal.Set(Calendar.SECOND, 59);
					cal.Set(Calendar.MILLISECOND, 999);
					d2 = cal.GetTime();
				}
				part2 = DateTools.DateToString(d2, resolution);
			}
			catch (Exception)
			{
			}
			return NewRangeQuery(field, part1, part2, startInclusive, endInclusive);
		}

		/// <summary>Builds a new BooleanClause instance</summary>
		/// <param name="q">sub query</param>
		/// <param name="occur">how this clause should occur when matching documents</param>
		/// <returns>new BooleanClause instance</returns>
		protected internal virtual BooleanClause NewBooleanClause(Query q, BooleanClause.Occur
			 occur)
		{
			return new BooleanClause(q, occur);
		}

		/// <summary>Builds a new PrefixQuery instance</summary>
		/// <param name="prefix">Prefix term</param>
		/// <returns>new PrefixQuery instance</returns>
		protected internal virtual Query NewPrefixQuery(Term prefix)
		{
			PrefixQuery query = new PrefixQuery(prefix);
			query.SetRewriteMethod(multiTermRewriteMethod);
			return query;
		}

		/// <summary>Builds a new RegexpQuery instance</summary>
		/// <param name="regexp">Regexp term</param>
		/// <returns>new RegexpQuery instance</returns>
		protected internal virtual Query NewRegexpQuery(Term regexp)
		{
			RegexpQuery query = new RegexpQuery(regexp);
			query.SetRewriteMethod(multiTermRewriteMethod);
			return query;
		}

		/// <summary>Builds a new FuzzyQuery instance</summary>
		/// <param name="term">Term</param>
		/// <param name="minimumSimilarity">minimum similarity</param>
		/// <param name="prefixLength">prefix length</param>
		/// <returns>new FuzzyQuery Instance</returns>
		protected internal virtual Query NewFuzzyQuery(Term term, float minimumSimilarity
			, int prefixLength)
		{
			// FuzzyQuery doesn't yet allow constant score rewrite
			string text = term.Text();
			int numEdits = FuzzyQuery.FloatToEdits(minimumSimilarity, text.CodePointCount(0, 
				text.Length));
			return new FuzzyQuery(term, numEdits, prefixLength);
		}

		// TODO: Should this be protected instead?
		private BytesRef AnalyzeMultitermTerm(string field, string part)
		{
			return AnalyzeMultitermTerm(field, part, GetAnalyzer());
		}

		protected internal virtual BytesRef AnalyzeMultitermTerm(string field, string part
			, Analyzer analyzerIn)
		{
			if (analyzerIn == null)
			{
				analyzerIn = GetAnalyzer();
			}
			TokenStream source = null;
			try
			{
				source = analyzerIn.TokenStream(field, part);
				source.Reset();
				TermToBytesRefAttribute termAtt = source.GetAttribute<TermToBytesRefAttribute>();
				BytesRef bytes = termAtt.GetBytesRef();
				if (!source.IncrementToken())
				{
					throw new ArgumentException("analyzer returned no terms for multiTerm term: " + part
						);
				}
				termAtt.FillBytesRef();
				if (source.IncrementToken())
				{
					throw new ArgumentException("analyzer returned too many terms for multiTerm term: "
						 + part);
				}
				source.End();
				return BytesRef.DeepCopyOf(bytes);
			}
			catch (IOException e)
			{
				throw new RuntimeException("Error analyzing multiTerm term: " + part, e);
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(source);
			}
		}

		/// <summary>
		/// Builds a new
		/// <see cref="Org.Apache.Lucene.Search.TermRangeQuery">Org.Apache.Lucene.Search.TermRangeQuery
		/// 	</see>
		/// instance
		/// </summary>
		/// <param name="field">Field</param>
		/// <param name="part1">min</param>
		/// <param name="part2">max</param>
		/// <param name="startInclusive">true if the start of the range is inclusive</param>
		/// <param name="endInclusive">true if the end of the range is inclusive</param>
		/// <returns>
		/// new
		/// <see cref="Org.Apache.Lucene.Search.TermRangeQuery">Org.Apache.Lucene.Search.TermRangeQuery
		/// 	</see>
		/// instance
		/// </returns>
		protected internal virtual Query NewRangeQuery(string field, string part1, string
			 part2, bool startInclusive, bool endInclusive)
		{
			BytesRef start;
			BytesRef end;
			if (part1 == null)
			{
				start = null;
			}
			else
			{
				start = analyzeRangeTerms ? AnalyzeMultitermTerm(field, part1) : new BytesRef(part1
					);
			}
			if (part2 == null)
			{
				end = null;
			}
			else
			{
				end = analyzeRangeTerms ? AnalyzeMultitermTerm(field, part2) : new BytesRef(part2
					);
			}
			TermRangeQuery query = new TermRangeQuery(field, start, end, startInclusive, endInclusive
				);
			query.SetRewriteMethod(multiTermRewriteMethod);
			return query;
		}

		/// <summary>Builds a new MatchAllDocsQuery instance</summary>
		/// <returns>new MatchAllDocsQuery instance</returns>
		protected internal virtual Query NewMatchAllDocsQuery()
		{
			return new MatchAllDocsQuery();
		}

		/// <summary>Builds a new WildcardQuery instance</summary>
		/// <param name="t">wildcard term</param>
		/// <returns>new WildcardQuery instance</returns>
		protected internal virtual Query NewWildcardQuery(Term t)
		{
			WildcardQuery query = new WildcardQuery(t);
			query.SetRewriteMethod(multiTermRewriteMethod);
			return query;
		}

		/// <summary>Factory method for generating query, given a set of clauses.</summary>
		/// <remarks>
		/// Factory method for generating query, given a set of clauses.
		/// By default creates a boolean query composed of clauses passed in.
		/// Can be overridden by extending classes, to modify query being
		/// returned.
		/// </remarks>
		/// <param name="clauses">
		/// List that contains
		/// <see cref="Org.Apache.Lucene.Search.BooleanClause">Org.Apache.Lucene.Search.BooleanClause
		/// 	</see>
		/// instances
		/// to join.
		/// </param>
		/// <returns>
		/// Resulting
		/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
		/// object.
		/// </returns>
		/// <exception>
		/// org.apache.lucene.queryparser.classic.ParseException
		/// throw in overridden method to disallow
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		protected internal virtual Query GetBooleanQuery(IList<BooleanClause> clauses)
		{
			return GetBooleanQuery(clauses, false);
		}

		/// <summary>Factory method for generating query, given a set of clauses.</summary>
		/// <remarks>
		/// Factory method for generating query, given a set of clauses.
		/// By default creates a boolean query composed of clauses passed in.
		/// Can be overridden by extending classes, to modify query being
		/// returned.
		/// </remarks>
		/// <param name="clauses">
		/// List that contains
		/// <see cref="Org.Apache.Lucene.Search.BooleanClause">Org.Apache.Lucene.Search.BooleanClause
		/// 	</see>
		/// instances
		/// to join.
		/// </param>
		/// <param name="disableCoord">true if coord scoring should be disabled.</param>
		/// <returns>
		/// Resulting
		/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
		/// object.
		/// </returns>
		/// <exception>
		/// org.apache.lucene.queryparser.classic.ParseException
		/// throw in overridden method to disallow
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		protected internal virtual Query GetBooleanQuery(IList<BooleanClause> clauses, bool
			 disableCoord)
		{
			if (clauses.Count == 0)
			{
				return null;
			}
			// all clause words were filtered away by the analyzer.
			BooleanQuery query = NewBooleanQuery(disableCoord);
			foreach (BooleanClause clause in clauses)
			{
				query.Add(clause);
			}
			return query;
		}

		/// <summary>Factory method for generating a query.</summary>
		/// <remarks>
		/// Factory method for generating a query. Called when parser
		/// parses an input term token that contains one or more wildcard
		/// characters (? and *), but is not a prefix term token (one
		/// that has just a single * character at the end)
		/// <p>
		/// Depending on settings, prefix term may be lower-cased
		/// automatically. It will not go through the default Analyzer,
		/// however, since normal Analyzers are unlikely to work properly
		/// with wildcard templates.
		/// <p>
		/// Can be overridden by extending classes, to provide custom handling for
		/// wildcard queries, which may be necessary due to missing analyzer calls.
		/// </remarks>
		/// <param name="field">Name of the field query will use.</param>
		/// <param name="termStr">
		/// Term token that contains one or more wild card
		/// characters (? or *), but is not simple prefix term
		/// </param>
		/// <returns>
		/// Resulting
		/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
		/// built for the term
		/// </returns>
		/// <exception>
		/// org.apache.lucene.queryparser.classic.ParseException
		/// throw in overridden method to disallow
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		protected internal virtual Query GetWildcardQuery(string field, string termStr)
		{
			if ("*".Equals(field))
			{
				if ("*".Equals(termStr))
				{
					return NewMatchAllDocsQuery();
				}
			}
			if (!allowLeadingWildcard && (termStr.StartsWith("*") || termStr.StartsWith("?")))
			{
				throw new ParseException("'*' or '?' not allowed as first character in WildcardQuery"
					);
			}
			if (lowercaseExpandedTerms)
			{
				termStr = termStr.ToLower(locale);
			}
			Term t = new Term(field, termStr);
			return NewWildcardQuery(t);
		}

		/// <summary>Factory method for generating a query.</summary>
		/// <remarks>
		/// Factory method for generating a query. Called when parser
		/// parses an input term token that contains a regular expression
		/// query.
		/// <p>
		/// Depending on settings, pattern term may be lower-cased
		/// automatically. It will not go through the default Analyzer,
		/// however, since normal Analyzers are unlikely to work properly
		/// with regular expression templates.
		/// <p>
		/// Can be overridden by extending classes, to provide custom handling for
		/// regular expression queries, which may be necessary due to missing analyzer
		/// calls.
		/// </remarks>
		/// <param name="field">Name of the field query will use.</param>
		/// <param name="termStr">Term token that contains a regular expression</param>
		/// <returns>
		/// Resulting
		/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
		/// built for the term
		/// </returns>
		/// <exception>
		/// org.apache.lucene.queryparser.classic.ParseException
		/// throw in overridden method to disallow
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		protected internal virtual Query GetRegexpQuery(string field, string termStr)
		{
			if (lowercaseExpandedTerms)
			{
				termStr = termStr.ToLower(locale);
			}
			Term t = new Term(field, termStr);
			return NewRegexpQuery(t);
		}

		/// <summary>
		/// Factory method for generating a query (similar to
		/// <see cref="GetWildcardQuery(string, string)">GetWildcardQuery(string, string)</see>
		/// ). Called when parser parses an input term
		/// token that uses prefix notation; that is, contains a single '*' wildcard
		/// character as its last character. Since this is a special case
		/// of generic wildcard term, and such a query can be optimized easily,
		/// this usually results in a different query object.
		/// <p>
		/// Depending on settings, a prefix term may be lower-cased
		/// automatically. It will not go through the default Analyzer,
		/// however, since normal Analyzers are unlikely to work properly
		/// with wildcard templates.
		/// <p>
		/// Can be overridden by extending classes, to provide custom handling for
		/// wild card queries, which may be necessary due to missing analyzer calls.
		/// </summary>
		/// <param name="field">Name of the field query will use.</param>
		/// <param name="termStr">
		/// Term token to use for building term for the query
		/// (<b>without</b> trailing '*' character!)
		/// </param>
		/// <returns>
		/// Resulting
		/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
		/// built for the term
		/// </returns>
		/// <exception>
		/// org.apache.lucene.queryparser.classic.ParseException
		/// throw in overridden method to disallow
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		protected internal virtual Query GetPrefixQuery(string field, string termStr)
		{
			if (!allowLeadingWildcard && termStr.StartsWith("*"))
			{
				throw new ParseException("'*' not allowed as first character in PrefixQuery");
			}
			if (lowercaseExpandedTerms)
			{
				termStr = termStr.ToLower(locale);
			}
			Term t = new Term(field, termStr);
			return NewPrefixQuery(t);
		}

		/// <summary>
		/// Factory method for generating a query (similar to
		/// <see cref="GetWildcardQuery(string, string)">GetWildcardQuery(string, string)</see>
		/// ). Called when parser parses
		/// an input term token that has the fuzzy suffix (~) appended.
		/// </summary>
		/// <param name="field">Name of the field query will use.</param>
		/// <param name="termStr">Term token to use for building term for the query</param>
		/// <returns>
		/// Resulting
		/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
		/// built for the term
		/// </returns>
		/// <exception>
		/// org.apache.lucene.queryparser.classic.ParseException
		/// throw in overridden method to disallow
		/// </exception>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		protected internal virtual Query GetFuzzyQuery(string field, string termStr, float
			 minSimilarity)
		{
			if (lowercaseExpandedTerms)
			{
				termStr = termStr.ToLower(locale);
			}
			Term t = new Term(field, termStr);
			return NewFuzzyQuery(t, minSimilarity, fuzzyPrefixLength);
		}

		// extracted from the .jj grammar
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		internal virtual Query HandleBareTokenQuery(string qfield, Token term, Token fuzzySlop
			, bool prefix, bool wildcard, bool fuzzy, bool regexp)
		{
			Query q;
			string termImage = DiscardEscapeChar(term.image);
			if (wildcard)
			{
				q = GetWildcardQuery(qfield, term.image);
			}
			else
			{
				if (prefix)
				{
					q = GetPrefixQuery(qfield, DiscardEscapeChar(Sharpen.Runtime.Substring(term.image
						, 0, term.image.Length - 1)));
				}
				else
				{
					if (regexp)
					{
						q = GetRegexpQuery(qfield, Sharpen.Runtime.Substring(term.image, 1, term.image.Length
							 - 1));
					}
					else
					{
						if (fuzzy)
						{
							q = HandleBareFuzzy(qfield, fuzzySlop, termImage);
						}
						else
						{
							q = GetFieldQuery(qfield, termImage, false);
						}
					}
				}
			}
			return q;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		internal virtual Query HandleBareFuzzy(string qfield, Token fuzzySlop, string termImage
			)
		{
			Query q;
			float fms = fuzzyMinSim;
			try
			{
				fms = float.ValueOf(Sharpen.Runtime.Substring(fuzzySlop.image, 1));
			}
			catch (Exception)
			{
			}
			if (fms < 0.0f)
			{
				throw new ParseException("Minimum similarity for a FuzzyQuery has to be between 0.0f and 1.0f !"
					);
			}
			else
			{
				if (fms >= 1.0f && fms != (int)fms)
				{
					throw new ParseException("Fractional edit distances are not allowed!");
				}
			}
			q = GetFuzzyQuery(qfield, termImage, fms);
			return q;
		}

		// extracted from the .jj grammar
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		internal virtual Query HandleQuotedTerm(string qfield, Token term, Token fuzzySlop
			)
		{
			int s = phraseSlop;
			// default
			if (fuzzySlop != null)
			{
				try
				{
					s = float.ValueOf(Sharpen.Runtime.Substring(fuzzySlop.image, 1));
				}
				catch (Exception)
				{
				}
			}
			return GetFieldQuery(qfield, DiscardEscapeChar(Sharpen.Runtime.Substring(term.image
				, 1, term.image.Length - 1)), s);
		}

		// extracted from the .jj grammar
		internal virtual Query HandleBoost(Query q, Token boost)
		{
			if (boost != null)
			{
				float f = (float)1.0;
				try
				{
					f = float.ValueOf(boost.image);
				}
				catch (Exception)
				{
				}
				// avoid boosting null queries, such as those caused by stop words
				if (q != null)
				{
					q.SetBoost(f);
				}
			}
			return q;
		}

		/// <summary>
		/// Returns a String where the escape char has been
		/// removed, or kept only once if there was a double escape.
		/// </summary>
		/// <remarks>
		/// Returns a String where the escape char has been
		/// removed, or kept only once if there was a double escape.
		/// Supports escaped unicode characters, e. g. translates
		/// <code>\\u0041</code> to <code>A</code>.
		/// </remarks>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		internal virtual string DiscardEscapeChar(string input)
		{
			// Create char array to hold unescaped char sequence
			char[] output = new char[input.Length];
			// The length of the output can be less than the input
			// due to discarded escape chars. This variable holds
			// the actual length of the output
			int length = 0;
			// We remember whether the last processed character was
			// an escape character
			bool lastCharWasEscapeChar = false;
			// The multiplier the current unicode digit must be multiplied with.
			// E. g. the first digit must be multiplied with 16^3, the second with 16^2...
			int codePointMultiplier = 0;
			// Used to calculate the codepoint of the escaped unicode character
			int codePoint = 0;
			for (int i = 0; i < input.Length; i++)
			{
				char curChar = input[i];
				if (codePointMultiplier > 0)
				{
					codePoint += HexToInt(curChar) * codePointMultiplier;
					codePointMultiplier = (int)(((uint)codePointMultiplier) >> 4);
					if (codePointMultiplier == 0)
					{
						output[length++] = (char)codePoint;
						codePoint = 0;
					}
				}
				else
				{
					if (lastCharWasEscapeChar)
					{
						if (curChar == 'u')
						{
							// found an escaped unicode character
							codePointMultiplier = 16 * 16 * 16;
						}
						else
						{
							// this character was escaped
							output[length] = curChar;
							length++;
						}
						lastCharWasEscapeChar = false;
					}
					else
					{
						if (curChar == '\\')
						{
							lastCharWasEscapeChar = true;
						}
						else
						{
							output[length] = curChar;
							length++;
						}
					}
				}
			}
			if (codePointMultiplier > 0)
			{
				throw new ParseException("Truncated unicode escape sequence.");
			}
			if (lastCharWasEscapeChar)
			{
				throw new ParseException("Term can not end with escape character.");
			}
			return new string(output, 0, length);
		}

		/// <summary>Returns the numeric value of the hexadecimal character</summary>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Classic.ParseException"></exception>
		internal static int HexToInt(char c)
		{
			if ('0' <= c && c <= '9')
			{
				return c - '0';
			}
			else
			{
				if ('a' <= c && c <= 'f')
				{
					return c - 'a' + 10;
				}
				else
				{
					if ('A' <= c && c <= 'F')
					{
						return c - 'A' + 10;
					}
					else
					{
						throw new ParseException("Non-hex character in Unicode escape sequence: " + c);
					}
				}
			}
		}

		/// <summary>
		/// Returns a String where those characters that QueryParser
		/// expects to be escaped are escaped by a preceding <code>\</code>.
		/// </summary>
		/// <remarks>
		/// Returns a String where those characters that QueryParser
		/// expects to be escaped are escaped by a preceding <code>\</code>.
		/// </remarks>
		public static string Escape(string s)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				// These characters are part of the query syntax and must be escaped
				if (c == '\\' || c == '+' || c == '-' || c == '!' || c == '(' || c == ')' || c ==
					 ':' || c == '^' || c == '[' || c == ']' || c == '\"' || c == '{' || c == '}' ||
					 c == '~' || c == '*' || c == '?' || c == '|' || c == '&' || c == '/')
				{
					sb.Append('\\');
				}
				sb.Append(c);
			}
			return sb.ToString();
		}
	}
}
