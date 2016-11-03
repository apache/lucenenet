using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Standard;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lucene.Net.QueryParsers.Classic
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

    public abstract class QueryParserBase : QueryBuilder, ICommonQueryParserConfiguration
    {
        /// <summary>
        /// Do not catch this exception in your code, it means you are using methods that you should no longer use.
        /// </summary>
        public class MethodRemovedUseAnother : Exception {}

        protected const int CONJ_NONE = 0;
        protected const int CONJ_AND = 1;
        protected const int CONJ_OR = 2;

        protected const int MOD_NONE = 0;
        protected const int MOD_NOT = 10;
        protected const int MOD_REQ = 11;


        // make it possible to call setDefaultOperator() without accessing
        // the nested class:
        
        /// <summary>
        /// Alternative form of QueryParser.Operator.AND
        /// </summary>
        public const Operator AND_OPERATOR = Operator.AND;
        /// <summary>
        /// Alternative form of QueryParser.Operator.OR
        /// </summary>
        public const Operator OR_OPERATOR = Operator.OR;

        ///// <summary>
        ///// The actual operator that parser uses to combine query terms
        ///// </summary>
        //Operator operator_Renamed = OR_OPERATOR;


        // Note: In Java, this was part of the QueryParser class. 
        // However, in .NET we need to put it here for the constants
        // defined above.

        /// <summary>
        /// The default operator for parsing queries. 
        /// Use <see cref="QueryParserBase.SetDefaultOperator"/> to change it.
        /// </summary>
        public enum Operator
        {
            OR,
            AND
        }

        //bool lowercaseExpandedTerms = true;
        //MultiTermQuery.RewriteMethod multiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;
        //bool allowLeadingWildcard = false;

        // LUCENENET-423 - DateRange differences with Java and .NET
        private bool _useJavaStyleDateRangeParsing = false;

        protected string field;
        //int phraseSlop = 0;
        //float fuzzyMinSim = FuzzyQuery.DefaultMinSimilarity;
        //int fuzzyPrefixLength = FuzzyQuery.DefaultPrefixLength;
        //CultureInfo locale = CultureInfo.CurrentCulture;
        //TimeZoneInfo timeZone = TimeZoneInfo.Local;

        // TODO: Work out what the default date resolution SHOULD be (was null in Java, which isn't valid for an enum type)
        
        /// <summary>
        /// the default date resolution
        /// </summary>
        DateTools.Resolution dateResolution = DateTools.Resolution.DAY;
        /// <summary>
        ///  maps field names to date resolutions
        /// </summary>
        IDictionary<string, DateTools.Resolution> fieldToDateResolution = null;

        /// <summary>
        /// Whether or not to analyze range terms when constructing RangeQuerys
        /// (For example, analyzing terms into collation keys for locale-sensitive RangeQuery)
        /// </summary>
        //bool analyzeRangeTerms = false;

        /// <summary>
        /// So the generated QueryParser(CharStream) won't error out
        /// </summary>
        protected QueryParserBase()
            : base(null)
        {
            // Set property defaults.
            DefaultOperator = OR_OPERATOR;
            LowercaseExpandedTerms = true;
            MultiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;
            AllowLeadingWildcard = false;

            PhraseSlop = 0;
#pragma warning disable 612, 618
            FuzzyMinSim = FuzzyQuery.DefaultMinSimilarity;
#pragma warning restore 612, 618
            FuzzyPrefixLength = FuzzyQuery.DefaultPrefixLength;
            Locale = CultureInfo.CurrentCulture;
            TimeZone = TimeZoneInfo.Local;
            
            AnalyzeRangeTerms = false;
        }

        /// <summary>
        /// Initializes a query parser.  Called by the QueryParser constructor
        /// </summary>
        /// <param name="matchVersion">Lucene version to match.</param>
        /// <param name="f">the default field for query terms.</param>
        /// <param name="a">used to find terms in the query text.</param>
        public virtual void Init(LuceneVersion matchVersion, string f, Analyzer a)
        {
            Analyzer = a;
            field = f;
#pragma warning disable 612, 618
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31))
#pragma warning restore 612, 618
            {
                AutoGeneratePhraseQueries = false;
            }
            else
            {
                AutoGeneratePhraseQueries = true;
            }
        }

        // the generated parser will create these in QueryParser
        public abstract void ReInit(ICharStream stream);
        public abstract Query TopLevelQuery(string field);

        /// <summary>
        /// Parses a query string, returning a <see cref="T:Query"/>.
        /// </summary>
        /// <remarks>
        /// throws ParseException if the parsing fails
        /// </remarks>
        /// <param name="query">the query string to be parsed.</param>
        /// <returns></returns>
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
                throw new ParseException("Cannot parse '" + query + "': " + tme.Message, tme);
            }
            catch (TokenMgrError tme)
            {
                throw new ParseException("Cannot parse '" + query + "': " + tme.Message, tme);
            }
            catch (BooleanQuery.TooManyClauses tmc)
            {
                throw new ParseException("Cannot parse '" + query + "': too many boolean clauses", tmc);
            }
        }

        /// <summary>
        /// Returns the default field.
        /// </summary>
        public virtual string Field
        {
            get { return field; }
        }

        /// <summary>
        /// Set to true if phrase queries will be automatically generated
        /// when the analyzer returns more than one term from whitespace
        /// delimited text.
        /// NOTE: this behavior may not be suitable for all languages.
        /// <p>
        /// Set to false if phrase queries should only be generated when
        /// surrounded by double quotes.
        /// </summary>
        public bool AutoGeneratePhraseQueries { get; set; }

        /// <summary>
        /// Get or Set the minimum similarity for fuzzy queries.
        /// Default is 2f.
        /// </summary>
        public virtual float FuzzyMinSim { get; set; }

        /// <summary>
        /// Get or Set the prefix length for fuzzy queries. 
        /// Default is 0.
        /// </summary>
        public virtual int FuzzyPrefixLength { get; set; }

        /// <summary>
        /// Gets or Sets the default slop for phrases. 
        /// If zero, then exact phrase matches are required. 
        /// Default value is zero.
        /// </summary>
        public virtual int PhraseSlop { get; set; }

        /// <summary>
        /// Set to <code>true</code> to allow leading wildcard characters.
        /// <p>
        /// When set, <code>*</code> or <code>?</code> are allowed as
        /// the first character of a PrefixQuery and WildcardQuery.
        /// Note that this can produce very slow
        /// queries on big indexes.
        /// <p>
        /// Default: false.
        /// </summary>
        public virtual bool AllowLeadingWildcard { get; set; }

        /// <summary>
        /// Gets or Sets the boolean operator of the QueryParser.
        /// In default mode (<code>OR_OPERATOR</code>) terms without any modifiers
        /// are considered optional: for example <code>capital of Hungary</code> is equal to
        /// <code>capital OR of OR Hungary</code>.<br/>
        /// In <code>AND_OPERATOR</code> mode terms are considered to be in conjunction: the
        /// above mentioned query is parsed as <code>capital AND of AND Hungary
        /// </summary>
        public virtual Operator DefaultOperator { get; set; }

        /// <summary>
        /// Whether terms of wildcard, prefix, fuzzy and range queries are to be automatically
        //  lower-cased or not.  Default is <code>true</code>.
        /// </summary>
        public virtual bool LowercaseExpandedTerms { get; set; }

        /// <summary>
        /// By default QueryParser uses <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/>
        /// when creating a <see cref="PrefixQuery"/>, <see cref="WildcardQuery"/> or <see cref="TermRangeQuery"/>. This implementation is generally preferable because it
        /// a) Runs faster b) Does not have the scarcity of terms unduly influence score
        /// c) avoids any <see cref="TooManyClauses"/> exception.
        /// However, if your application really needs to use the
        /// old-fashioned <see cref="BooleanQuery"/> expansion rewriting and the above
        /// points are not relevant then use this to change
        /// the rewrite method.
        /// </summary>
        public virtual MultiTermQuery.RewriteMethod MultiTermRewriteMethod { get; set; }

        /// <summary>
        /// Get or Set locale used by date range parsing, lowercasing, and other
        /// locale-sensitive operations.
        /// </summary>
        public virtual CultureInfo Locale { get; set; }

        public virtual TimeZoneInfo TimeZone { get; set; }

        /// <summary>
        /// Gets or Sets the default date resolution used by RangeQueries for fields for which no
        /// specific date resolutions has been set. Field specific resolutions can be set
        /// with <see cref="SetDateResolution(string,DateTools.Resolution)"/>.
        /// </summary>
        public virtual void SetDateResolution(DateTools.Resolution dateResolution)
        {
            this.dateResolution = dateResolution;
        }

        /// <summary>
        /// Sets the date resolution used by RangeQueries for a specific field.
        /// </summary>
        /// <param name="fieldName">field for which the date resolution is to be set</param>
        /// <param name="dateResolution">date resolution to set</param>
        public virtual void SetDateResolution(string fieldName, DateTools.Resolution dateResolution)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException("fieldName cannot be null or empty string.");
            }

            if (fieldToDateResolution == null)
            {
                // lazily initialize Dictionary
                fieldToDateResolution = new Dictionary<string, DateTools.Resolution>();
            }

            fieldToDateResolution[fieldName] = dateResolution;
        }

        /// <summary>
        /// Returns the date resolution that is used by RangeQueries for the given field.
        /// Returns null, if no default or field specific date resolution has been set 
        /// for the given field.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public virtual DateTools.Resolution GetDateResolution(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException("fieldName cannot be null or empty string.");
            }

            if (fieldToDateResolution == null)
            {
                // no field specific date resolutions set; return default date resolution instead
                return this.dateResolution;
            }

            if (!fieldToDateResolution.ContainsKey(fieldName))
            {
                // no date resolutions set for the given field; return default date resolution instead
                return this.dateResolution;
            }

            return fieldToDateResolution[fieldName];
        }

        /// <summary>
        /// Get or Set whether or not to analyze range terms when constructing <see cref="TermRangeQuery"/>s.
        /// For example, setting this to true can enable analyzing terms into 
        /// collation keys for locale-sensitive <see cref="TermRangeQuery"/>.
        /// </summary>
        public virtual bool AnalyzeRangeTerms { get; set; }

        protected internal virtual void AddClause(IList<BooleanClause> clauses, int conj, int mods, Query q)
        {
            bool required, prohibited;

            // If this term is introduced by AND, make the preceding term required,
            // unless it's already prohibited
            if (clauses.Count > 0 && conj == CONJ_AND)
            {
                BooleanClause c = clauses[clauses.Count - 1];
                if (!c.Prohibited)
                    c.Occur_ = BooleanClause.Occur.MUST;
            }

            if (clauses.Count > 0 && DefaultOperator == AND_OPERATOR && conj == CONJ_OR)
            {
                // If this term is introduced by OR, make the preceding term optional,
                // unless it's prohibited (that means we leave -a OR b but +a OR b-->a OR b)
                // notice if the input is a OR b, first term is parsed as required; without
                // this modification a OR b would parsed as +a OR b
                BooleanClause c = clauses[clauses.Count - 1];
                if (!c.Prohibited)
                    c.Occur_ = BooleanClause.Occur.SHOULD;
            }

            // We might have been passed a null query; the term might have been
            // filtered away by the analyzer.
            if (q == null)
                return;

            if (DefaultOperator == OR_OPERATOR)
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
                clauses.Add(NewBooleanClause(q, BooleanClause.Occur.MUST));
            else if (!required && !prohibited)
                clauses.Add(NewBooleanClause(q, BooleanClause.Occur.SHOULD));
            else if (!required && prohibited)
                clauses.Add(NewBooleanClause(q, BooleanClause.Occur.MUST_NOT));
            else
                throw new Exception("Clause cannot be both required and prohibited");
        }

        /// <exception cref="ParseException">throw in overridden method to disallow</exception>
        protected internal virtual Query GetFieldQuery(string field, string queryText, bool quoted)
        {
            return NewFieldQuery(Analyzer, field, queryText, quoted);
        }

        /// <exception cref="ParseException">throw in overridden method to disallow</exception>
        protected internal virtual Query NewFieldQuery(Analyzer analyzer, string field, string queryText, bool quoted)
        {
            BooleanClause.Occur occur = DefaultOperator == Operator.AND ? BooleanClause.Occur.MUST : BooleanClause.Occur.SHOULD;
            return CreateFieldQuery(analyzer, occur, field, queryText, quoted || AutoGeneratePhraseQueries, PhraseSlop);
        }

        /// <summary>
        /// Base implementation delegates to <see cref="GetFieldQuery(string,string,bool)"/>.
        /// This method may be overridden, for example, to return
        /// a SpanNearQuery instead of a PhraseQuery.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="queryText"></param>
        /// <param name="slop"></param>
        /// <exception cref="ParseException">throw in overridden method to disallow</exception>
        /// <returns></returns>
        protected internal virtual Query GetFieldQuery(string field, string queryText, int slop)
        {
            Query query = GetFieldQuery(field, queryText, true);

            if (query is PhraseQuery)
            {
                ((PhraseQuery)query).Slop = slop;
            }
            if (query is MultiPhraseQuery)
            {
                ((MultiPhraseQuery)query).Slop = slop;
            }

            return query;
        }

        protected internal virtual Query GetRangeQuery(string field,
                              string part1,
                              string part2,
                              bool startInclusive,
                              bool endInclusive)
        {
            if (LowercaseExpandedTerms)
            {
                part1 = part1 == null ? null : Locale.TextInfo.ToLower(part1);
                part2 = part2 == null ? null : Locale.TextInfo.ToLower(part2);
            }

            try
            {
                DateTime d1, d2;
                if (_useJavaStyleDateRangeParsing)
                {
                    // LUCENENET TODO: This doesn't emulate java perfectly.
                    // Java allows parsing of the string up to the end of the pattern
                    // and then ignores everything else.  .NET will throw an exception, 
                    // so this will fail in those cases, though the code below is clear
                    // that users can only specify the date, not the time.
                    var shortFormat = Locale.DateTimeFormat.ShortDatePattern;
                    d1 = DateTime.ParseExact(part1, shortFormat, Locale);
                    d2 = DateTime.ParseExact(part2, shortFormat, Locale);
                }
                else
                {
                    d1 = DateTime.Parse(part1, Locale);
                    d2 = DateTime.Parse(part2, Locale);
                }

                if (endInclusive)
                {
                    // The user can only specify the date, not the time, so make sure
                    // the time is set to the latest possible time of that date to really
                    // include all documents:

                    // LUCENENET TODO: Try to work out if the Time Zone is pertinent here or
                    // whether it should just be removed from the API entirely.
                    // In Java:
                    // Calendar cal = Calendar.getInstance(timeZone, locale);
                    
                    var cal = Locale.Calendar;
                    d2 = cal.AddHours(d2, 23);
                    d2 = cal.AddMinutes(d2, 59);
                    d2 = cal.AddSeconds(d2, 59);
                    d2 = cal.AddMilliseconds(d2, 999);
                }
                DateTools.Resolution resolution = GetDateResolution(field);

                part1 = DateTools.DateToString(d1, resolution);
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
        /// <returns> new BooleanClause instance</returns>
        protected internal virtual BooleanClause NewBooleanClause(Query q, BooleanClause.Occur occur)
        {
            return new BooleanClause(q, occur);
        }

        /// <summary>
        /// Builds a new PrefixQuery instance
        /// </summary>
        /// <param name="prefix">Prefix term</param>
        /// <returns>new PrefixQuery instance</returns>
        protected internal virtual Query NewPrefixQuery(Term prefix)
        {
            PrefixQuery query = new PrefixQuery(prefix);
            query.SetRewriteMethod(MultiTermRewriteMethod);
            return query;
        }

        /// <summary>
        /// Builds a new RegexpQuery instance
        /// </summary>
        /// <param name="regexp">Regexp term</param>
        /// <returns>new RegexpQuery instance</returns>
        protected internal virtual Query NewRegexpQuery(Term regexp)
        {
            RegexpQuery query = new RegexpQuery(regexp);
            query.SetRewriteMethod(MultiTermRewriteMethod);
            return query;
        }

        /// <summary>
        /// Builds a new FuzzyQuery instance
        /// </summary>
        /// <param name="term">Term</param>
        /// <param name="minimumSimilarity">minimum similarity</param>
        /// <param name="prefixLength">prefix length</param>
        /// <returns>new FuzzyQuery Instance</returns>
        protected internal virtual Query NewFuzzyQuery(Term term, float minimumSimilarity, int prefixLength)
        {
            // FuzzyQuery doesn't yet allow constant score rewrite
            string text = term.Text();
#pragma warning disable 612, 618
            int numEdits = FuzzyQuery.FloatToEdits(minimumSimilarity,
                Character.CodePointCount(text,0, text.Length));
#pragma warning restore 612, 618
            return new FuzzyQuery(term, numEdits, prefixLength);
        }

        // LUCENETODO: Should this be protected instead?
        private BytesRef AnalyzeMultitermTerm(string field, string part)
        {
            return AnalyzeMultitermTerm(field, part, Analyzer);
        }

        protected internal virtual BytesRef AnalyzeMultitermTerm(string field, string part, Analyzer analyzerIn)
        {
            if (analyzerIn == null) analyzerIn = Analyzer;

            TokenStream source = null;
            try
            {
                source = analyzerIn.TokenStream(field, part);
                source.Reset();

                ITermToBytesRefAttribute termAtt = source.GetAttribute<ITermToBytesRefAttribute>();
                BytesRef bytes = termAtt.BytesRef;

                if (!source.IncrementToken())
                    throw new ArgumentException("analyzer returned no terms for multiTerm term: " + part);
                termAtt.FillBytesRef();
                if (source.IncrementToken())
                    throw new ArgumentException("analyzer returned too many terms for multiTerm term: " + part);
                source.End();
                return BytesRef.DeepCopyOf(bytes);
            }
            catch (IOException e)
            {
                throw new Exception("Error analyzing multiTerm term: " + part, e);
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(source);
            }
        }

        /// <summary>
        /// Builds a new {@link TermRangeQuery} instance
        /// </summary>
        /// <param name="field">Field</param>
        /// <param name="part1">min</param>
        /// <param name="part2">max</param>
        /// <param name="startInclusive">true if the start of the range is inclusive</param>
        /// <param name="endInclusive">true if the end of the range is inclusive</param>
        /// <returns>new <see cref="T:TermRangeQuery"/> instance</returns>
        protected internal virtual Query NewRangeQuery(string field, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            BytesRef start;
            BytesRef end;

            if (part1 == null)
            {
                start = null;
            }
            else
            {
                start = AnalyzeRangeTerms ? AnalyzeMultitermTerm(field, part1) : new BytesRef(part1);
            }

            if (part2 == null)
            {
                end = null;
            }
            else
            {
                end = AnalyzeRangeTerms ? AnalyzeMultitermTerm(field, part2) : new BytesRef(part2);
            }

            TermRangeQuery query = new TermRangeQuery(field, start, end, startInclusive, endInclusive);

            query.SetRewriteMethod(MultiTermRewriteMethod);
            return query;
        }

        /// <summary>
        /// Builds a new MatchAllDocsQuery instance
        /// </summary>
        /// <returns>new MatchAllDocsQuery instance</returns>
        protected internal virtual Query NewMatchAllDocsQuery()
        {
            return new MatchAllDocsQuery();
        }

        /// <summary>
        /// Builds a new WildcardQuery instance
        /// </summary>
        /// <param name="t">wildcard term</param>
        /// <returns>new WildcardQuery instance</returns>
        protected internal virtual Query NewWildcardQuery(Term t)
        {
            WildcardQuery query = new WildcardQuery(t);
            query.SetRewriteMethod(MultiTermRewriteMethod);
            return query;
        }

        /// <summary>
        /// Factory method for generating query, given a set of clauses.
        /// By default creates a boolean query composed of clauses passed in.
        ///
        /// Can be overridden by extending classes, to modify query being
        /// returned.
        /// </summary>
        /// <param name="clauses">List that contains {@link org.apache.lucene.search.BooleanClause} instances 
        /// to join.</param>
        /// <exception cref="T:ParseException">throw in overridden method to disallow</exception>
        /// <returns>Resulting <see cref="T:Query"/> object.</returns>
        protected internal virtual Query GetBooleanQuery(IList<BooleanClause> clauses)
        {
            return GetBooleanQuery(clauses, false);
        }

        /// <summary>
        /// Factory method for generating query, given a set of clauses.
        /// By default creates a boolean query composed of clauses passed in.
        /// 
        /// Can be overridden by extending classes, to modify query being
        /// returned.
        /// </summary>
        /// <param name="clauses">List that contains <see cref="T:BooleanClause"/> instances
        /// to join.</param>
        /// <param name="disableCoord">true if coord scoring should be disabled.</param>
        /// <exception cref="ParseException">throw in overridden method to disallow</exception>
        /// <returns>Resulting <see cref="T:Query"/> object.</returns>
        protected internal virtual Query GetBooleanQuery(IList<BooleanClause> clauses, bool disableCoord)
        {
            if (clauses.Count == 0)
            {
                return null; // all clause words were filtered away by the analyzer.
            }
            BooleanQuery query = NewBooleanQuery(disableCoord);
            foreach (BooleanClause clause in clauses)
            {
                query.Add(clause);
            }
            return query;
        }

        /// <summary>
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
        /// </summary>
        /// <param name="field">Name of the field query will use.</param>
        /// <param name="termStr">Term token that contains one or more wild card
        /// characters (? or *), but is not simple prefix term</param>
        /// <exception cref="ParseException">throw in overridden method to disallow</exception>
        /// <returns>Resulting <see cref="T:Query"/> built for the term</returns>
        protected internal virtual Query GetWildcardQuery(string field, string termStr)
        {
            if ("*".Equals(field))
            {
                if ("*".Equals(termStr)) return NewMatchAllDocsQuery();
            }
            if (!AllowLeadingWildcard && (termStr.StartsWith("*") || termStr.StartsWith("?")))
                throw new ParseException("'*' or '?' not allowed as first character in WildcardQuery");
            if (LowercaseExpandedTerms)
            {
                termStr = Locale.TextInfo.ToLower(termStr);
            }
            Term t = new Term(field, termStr);
            return NewWildcardQuery(t);
        }

        /// <summary>
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
        /// </summary>
        /// <param name="field">Name of the field query will use.</param>
        /// <param name="termStr">Term token that contains a regular expression</param>
        /// <exception cref="ParseException">throw in overridden method to disallow</exception>
        /// <returns>Resulting <see cref="T:Query"/> built for the term</returns>
        protected internal virtual Query GetRegexpQuery(string field, string termStr)
        {
            if (LowercaseExpandedTerms)
            {
                termStr = Locale.TextInfo.ToLower(termStr);
            }
            Term t = new Term(field, termStr);
            return NewRegexpQuery(t);
        }

        /// <summary>
        /// Factory method for generating a query (similar to
        /// <see cref="M:GetWildcardQuery"/>). Called when parser parses an input term
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
        /// <param name="termStr">Term token to use for building term for the query</param>
        /// <exception cref="ParseException">throw in overridden method to disallow</exception>
        /// <returns>Resulting <see cref="T:Query"/> built for the term</returns>
        protected internal virtual Query GetPrefixQuery(string field, string termStr)
        {
            if (!AllowLeadingWildcard && termStr.StartsWith("*"))
                throw new ParseException("'*' not allowed as first character in PrefixQuery");
            if (LowercaseExpandedTerms)
            {
                termStr = Locale.TextInfo.ToLower(termStr);
            }
            Term t = new Term(field, termStr);
            return NewPrefixQuery(t);
        }

        /// <summary>
        /// Factory method for generating a query (similar to
        /// <see cref="M:GetWildcardQuery"/>). Called when parser parses
        /// an input term token that has the fuzzy suffix (~) appended.
        /// </summary>
        /// <param name="field">Name of the field query will use.</param>
        /// <param name="termStr">Term token to use for building term for the query</param>
        /// <param name="minSimilarity">minimum similarity</param>
        /// <exception cref="ParseException">throw in overridden method to disallow</exception>
        /// <returns>Resulting <see cref="T:Query"/> built for the term</returns>
        protected internal virtual Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            if (LowercaseExpandedTerms)
            {
                termStr = Locale.TextInfo.ToLower(termStr);
            }
            Term t = new Term(field, termStr);
            return NewFuzzyQuery(t, minSimilarity, FuzzyPrefixLength);
        }

        // extracted from the .jj grammar
        internal virtual Query HandleBareTokenQuery(string qfield, Token term, Token fuzzySlop, bool prefix, bool wildcard, bool fuzzy, bool regexp)
        {
            Query q;

            string termImage = DiscardEscapeChar(term.image);
            if (wildcard)
            {
                q = GetWildcardQuery(qfield, term.image);
            }
            else if (prefix)
            {
                q = GetPrefixQuery(qfield, DiscardEscapeChar(term.image.Substring(0, term.image.Length - 1)));
            }
            else if (regexp)
            {
                q = GetRegexpQuery(qfield, term.image.Substring(1, term.image.Length - 2));
            }
            else if (fuzzy)
            {
                q = HandleBareFuzzy(qfield, fuzzySlop, termImage);
            }
            else
            {
                q = GetFieldQuery(qfield, termImage, false);
            }
            return q;
        }

        internal virtual Query HandleBareFuzzy(string qfield, Token fuzzySlop, string termImage)
        {
            Query q;
            float fms = FuzzyMinSim;
            try
            {
                fms = float.Parse(fuzzySlop.image.Substring(1), Locale);
            }
            catch (Exception /*ignored*/) { }
            if (fms < 0.0f)
            {
                throw new ParseException("Minimum similarity for a FuzzyQuery has to be between 0.0f and 1.0f !");
            }
            else if (fms >= 1.0f && fms != (int)fms)
            {
                throw new ParseException("Fractional edit distances are not allowed!");
            }
            q = GetFuzzyQuery(qfield, termImage, fms);
            return q;
        }

        // extracted from the .jj grammar
        internal virtual Query HandleQuotedTerm(string qfield, Token term, Token fuzzySlop)
        {
            int s = PhraseSlop;  // default
            if (fuzzySlop != null)
            {
                try
                {
                    s = (int)float.Parse(fuzzySlop.image.Substring(1), Locale);
                }
                catch (Exception /*ignored*/) { }
            }
            return GetFieldQuery(qfield, DiscardEscapeChar(term.image.Substring(1, term.image.Length - 2)), s);
        }

        // extracted from the .jj grammar
        internal virtual Query HandleBoost(Query q, Token boost)
        {
            if (boost != null)
            {
                float f = (float)1.0;
                try
                {
                    f = float.Parse(boost.image, Locale);
                }
                catch (Exception /*ignored*/)
                {
                    /* Should this be handled somehow? (defaults to "no boost", if
                     * boost number is invalid)
                     */
                }

                // avoid boosting null queries, such as those caused by stop words
                if (q != null)
                {
                    q.Boost = f;
                }
            }
            return q;
        }

        /// <summary>
        /// Returns a string where the escape char has been
        /// removed, or kept only once if there was a double escape.
        /// 
        /// Supports escaped unicode characters, e. g. translates 
        /// <code>\\u0041</code> to <code>A</code>.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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
                    codePointMultiplier = Number.URShift(codePointMultiplier, 4);
                    if (codePointMultiplier == 0)
                    {
                        output[length++] = (char)codePoint;
                        codePoint = 0;
                    }
                }
                else if (lastCharWasEscapeChar)
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

        /// <summary>
        /// Returns the numeric value of the hexadecimal character
        /// </summary>
        private static int HexToInt(char c)
        {
            if ('0' <= c && c <= '9')
            {
                return c - '0';
            }
            else if ('a' <= c && c <= 'f')
            {
                return c - 'a' + 10;
            }
            else if ('A' <= c && c <= 'F')
            {
                return c - 'A' + 10;
            }
            else
            {
                throw new ParseException("Non-hex character in Unicode escape sequence: " + c);
            }
        }

        /// <summary>
        /// Returns a string where those characters that QueryParser
        /// expects to be escaped are escaped by a preceding <code>\</code>.
        /// </summary>
        public static string Escape(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                // These characters are part of the query syntax and must be escaped
                if (c == '\\' || c == '+' || c == '-' || c == '!' || c == '(' || c == ')' || c == ':'
                  || c == '^' || c == '[' || c == ']' || c == '\"' || c == '{' || c == '}' || c == '~'
                  || c == '*' || c == '?' || c == '|' || c == '&' || c == '/')
                {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
