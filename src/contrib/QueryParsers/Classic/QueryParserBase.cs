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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Operator = Lucene.Net.QueryParsers.Classic.QueryParser.Operator;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.QueryParsers.Classic
{
    public abstract class QueryParserBase : ICommonQueryParserConfiguration
    {
        /** Do not catch this exception in your code, it means you are using methods that you should no longer use. */
        public class MethodRemovedUseAnother : Exception { }

        internal const int CONJ_NONE = 0;
        internal const int CONJ_AND = 1;
        internal const int CONJ_OR = 2;

        internal const int MOD_NONE = 0;
        internal const int MOD_NOT = 10;
        internal const int MOD_REQ = 11;

        // make it possible to call setDefaultOperator() without accessing
        // the nested class:
        /** Alternative form of QueryParser.Operator.AND */
        public static readonly Operator AND_OPERATOR = Operator.AND;
        /** Alternative form of QueryParser.Operator.OR */
        public static readonly Operator OR_OPERATOR = Operator.OR;

        /** The actual operator that parser uses to combine query terms */
        internal Operator operator_renamed = OR_OPERATOR;

        internal bool lowercaseExpandedTerms = true;
        internal MultiTermQuery.RewriteMethod multiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;
        internal bool allowLeadingWildcard = false;
        internal bool enablePositionIncrements = true;

        internal Analyzer analyzer;
        internal String field;
        internal int phraseSlop = 0;
        internal float fuzzyMinSim = FuzzyQuery.defaultMinSimilarity;
        internal int fuzzyPrefixLength = FuzzyQuery.defaultPrefixLength;
        internal CultureInfo locale = CultureInfo.InvariantCulture;
        internal TimeZone timeZone = TimeZone.CurrentTimeZone;

        // the default date resolution
        internal DateTools.Resolution dateResolution = null;
        // maps field names to date resolutions
        internal IDictionary<String, DateTools.Resolution> fieldToDateResolution = null;

        //Whether or not to analyze range terms when constructing RangeQuerys
        // (For example, analyzing terms into collation keys for locale-sensitive RangeQuery)
        internal bool analyzeRangeTerms = false;

        internal bool autoGeneratePhraseQueries;

        // So the generated QueryParser(CharStream) won't error out
        protected QueryParserBase()
        {
        }

        public void Init(Version matchVersion, String f, Analyzer a)
        {
            analyzer = a;
            field = f;
            if (matchVersion.OnOrAfter(Version.LUCENE_31))
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
        public abstract Query TopLevelQuery(String field);

        public virtual Query Parse(String query)
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
                ParseException e = new ParseException("Cannot parse '" + query + "': " + tme.Message, tme);
                throw e;
            }
            catch (TokenMgrError tme)
            {
                ParseException e = new ParseException("Cannot parse '" + query + "': " + tme.Message, tme);
                throw e;
            }
            catch (BooleanQuery.TooManyClauses tmc)
            {
                ParseException e = new ParseException("Cannot parse '" + query + "': too many boolean clauses", tmc);
                throw e;
            }
        }

        public Analyzer Analyzer
        {
            get { return analyzer; }
        }

        public string Field
        {
            get { return field; }
        }

        public bool AutoGeneratePhraseQueries
        {
            get { return autoGeneratePhraseQueries; }
            set { autoGeneratePhraseQueries = value; }
        }

        public float FuzzyMinSim
        {
            get { return fuzzyMinSim; }
            set { fuzzyMinSim = value; }
        }

        public int FuzzyPrefixLength
        {
            get { return fuzzyPrefixLength; }
            set { fuzzyPrefixLength = value; }
        }

        public int PhraseSlop
        {
            get { return phraseSlop; }
            set { phraseSlop = value; }
        }

        public bool AllowLeadingWildcard
        {
            get { return allowLeadingWildcard; }
            set { allowLeadingWildcard = value; }
        }

        public bool EnablePositionIncrements
        {
            get { return enablePositionIncrements; }
            set { enablePositionIncrements = value; }
        }

        public Operator DefaultOperator
        {
            get { return operator_renamed; }
            set { operator_renamed = value; }
        }

        public bool LowercaseExpandedTerms
        {
            get { return lowercaseExpandedTerms; }
            set { lowercaseExpandedTerms = value; }
        }

        public MultiTermQuery.RewriteMethod MultiTermRewriteMethod
        {
            get { return multiTermRewriteMethod; }
            set { multiTermRewriteMethod = value; }
        }

        public CultureInfo Locale
        {
            get { return locale; }
            set { locale = value; }
        }

        public TimeZone TimeZone
        {
            get { return timeZone; }
            set { timeZone = value; }
        }

        public DateTools.Resolution DateResolution
        {
            get { return dateResolution; }
            set { dateResolution = value; }
        }

        public void SetDateResolution(string fieldName, DateTools.Resolution dateResolution)
        {
            if (fieldName == null)
            {
                throw new ArgumentException("Field cannot be null.");
            }

            if (fieldToDateResolution == null)
            {
                // lazily initialize HashMap
                fieldToDateResolution = new HashMap<String, DateTools.Resolution>();
            }

            fieldToDateResolution[fieldName] = dateResolution;
        }

        public DateTools.Resolution GetDateResolution(string fieldName)
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

            DateTools.Resolution resolution = fieldToDateResolution[fieldName];
            if (resolution == null)
            {
                // no date resolutions set for the given field; return default date resolution instead
                resolution = this.dateResolution;
            }

            return resolution;
        }

        public bool AnalyzeRangeTerms
        {
            get { return analyzeRangeTerms; }
            set { analyzeRangeTerms = value; }
        }

        protected void AddClause(IList<BooleanClause> clauses, int conj, int mods, Query q)
        {
            bool required, prohibited;

            // If this term is introduced by AND, make the preceding term required,
            // unless it's already prohibited
            if (clauses.Count > 0 && conj == CONJ_AND)
            {
                BooleanClause c = clauses[clauses.Count - 1];
                if (!c.IsProhibited)
                    c.Occur = Occur.MUST;
            }

            if (clauses.Count > 0 && operator_renamed == AND_OPERATOR && conj == CONJ_OR)
            {
                // If this term is introduced by OR, make the preceding term optional,
                // unless it's prohibited (that means we leave -a OR b but +a OR b-->a OR b)
                // notice if the input is a OR b, first term is parsed as required; without
                // this modification a OR b would parsed as +a OR b
                BooleanClause c = clauses[clauses.Count - 1];
                if (!c.IsProhibited)
                    c.Occur = Occur.SHOULD;
            }

            // We might have been passed a null query; the term might have been
            // filtered away by the analyzer.
            if (q == null)
                return;

            if (operator_renamed == OR_OPERATOR)
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
                clauses.Add(NewBooleanClause(q, Occur.MUST));
            else if (!required && !prohibited)
                clauses.Add(NewBooleanClause(q, Occur.SHOULD));
            else if (!required && prohibited)
                clauses.Add(NewBooleanClause(q, Occur.MUST_NOT));
            else
                throw new SystemException("Clause cannot be both required and prohibited");
        }

        protected virtual Query GetFieldQuery(String field, String queryText, bool quoted)
        {
            return NewFieldQuery(analyzer, field, queryText, quoted);
        }

        protected Query NewFieldQuery(Analyzer analyzer, String field, String queryText, bool quoted)
        {
            // Use the analyzer to get all the tokens, and then build a TermQuery,
            // PhraseQuery, or nothing based on the term count

            TokenStream source;
            try
            {
                source = analyzer.TokenStream(field, new StringReader(queryText));
                source.Reset();
            }
            catch (IOException e)
            {
                ParseException p = new ParseException("Unable to initialize TokenStream to analyze query text", e);
                throw p;
            }
            CachingTokenFilter buffer = new CachingTokenFilter(source);
            ITermToBytesRefAttribute termAtt = null;
            IPositionIncrementAttribute posIncrAtt = null;
            int numTokens = 0;

            buffer.Reset();

            if (buffer.HasAttribute<ITermToBytesRefAttribute>())
            {
                termAtt = buffer.GetAttribute<ITermToBytesRefAttribute>();
            }
            if (buffer.HasAttribute<IPositionIncrementAttribute>())
            {
                posIncrAtt = buffer.GetAttribute<IPositionIncrementAttribute>();
            }

            int positionCount = 0;
            bool severalTokensAtSamePosition = false;

            bool hasMoreTokens = false;
            if (termAtt != null)
            {
                try
                {
                    hasMoreTokens = buffer.IncrementToken();
                    while (hasMoreTokens)
                    {
                        numTokens++;
                        int positionIncrement = (posIncrAtt != null) ? posIncrAtt.PositionIncrement : 1;
                        if (positionIncrement != 0)
                        {
                            positionCount += positionIncrement;
                        }
                        else
                        {
                            severalTokensAtSamePosition = true;
                        }
                        hasMoreTokens = buffer.IncrementToken();
                    }
                }
                catch (IOException)
                {
                    // ignore
                }
            }
            try
            {
                // rewind the buffer stream
                buffer.Reset();

                // close original stream - all tokens buffered
                source.Dispose();
            }
            catch (IOException e)
            {
                ParseException p = new ParseException("Cannot close TokenStream analyzing query text", e);
                throw p;
            }

            BytesRef bytes = termAtt == null ? null : termAtt.BytesRef;

            if (numTokens == 0)
                return null;
            else if (numTokens == 1)
            {
                try
                {
                    bool hasNext = buffer.IncrementToken();
                    //assert hasNext == true;
                    termAtt.FillBytesRef();
                }
                catch (IOException)
                {
                    // safe to ignore, because we know the number of tokens
                }
                return NewTermQuery(new Term(field, BytesRef.DeepCopyOf(bytes)));
            }
            else
            {
                if (severalTokensAtSamePosition || (!quoted && !autoGeneratePhraseQueries))
                {
                    if (positionCount == 1 || (!quoted && !autoGeneratePhraseQueries))
                    {
                        // no phrase query:

                        if (positionCount == 1)
                        {
                            // simple case: only one position, with synonyms
                            BooleanQuery q = NewBooleanQuery(true);
                            for (int i = 0; i < numTokens; i++)
                            {
                                try
                                {
                                    bool hasNext = buffer.IncrementToken();
                                    //assert hasNext == true;
                                    termAtt.FillBytesRef();
                                }
                                catch (IOException)
                                {
                                    // safe to ignore, because we know the number of tokens
                                }
                                Query currentQuery = NewTermQuery(
                                    new Term(field, BytesRef.DeepCopyOf(bytes)));
                                q.Add(currentQuery, Occur.SHOULD);
                            }
                            return q;
                        }
                        else
                        {
                            // multiple positions
                            BooleanQuery q = NewBooleanQuery(false);
                            Occur occur = operator_renamed == Operator.AND ? Occur.MUST : Occur.SHOULD;
                            Query currentQuery = null;
                            for (int i = 0; i < numTokens; i++)
                            {
                                try
                                {
                                    bool hasNext = buffer.IncrementToken();
                                    //assert hasNext == true;
                                    termAtt.FillBytesRef();
                                }
                                catch (IOException)
                                {
                                    // safe to ignore, because we know the number of tokens
                                }
                                if (posIncrAtt != null && posIncrAtt.PositionIncrement == 0)
                                {
                                    if (!(currentQuery is BooleanQuery))
                                    {
                                        Query t = currentQuery;
                                        currentQuery = NewBooleanQuery(true);
                                        ((BooleanQuery)currentQuery).Add(t, Occur.SHOULD);
                                    }
                                    ((BooleanQuery)currentQuery).Add(NewTermQuery(new Term(field, BytesRef.DeepCopyOf(bytes))), Occur.SHOULD);
                                }
                                else
                                {
                                    if (currentQuery != null)
                                    {
                                        q.Add(currentQuery, occur);
                                    }
                                    currentQuery = NewTermQuery(new Term(field, BytesRef.DeepCopyOf(bytes)));
                                }
                            }
                            q.Add(currentQuery, occur);
                            return q;
                        }
                    }
                    else
                    {
                        // phrase query:
                        MultiPhraseQuery mpq = NewMultiPhraseQuery();
                        mpq.Slop = phraseSlop;
                        List<Term> multiTerms = new List<Term>();
                        int position = -1;
                        for (int i = 0; i < numTokens; i++)
                        {
                            int positionIncrement = 1;
                            try
                            {
                                bool hasNext = buffer.IncrementToken();
                                //assert hasNext == true;
                                termAtt.FillBytesRef();
                                if (posIncrAtt != null)
                                {
                                    positionIncrement = posIncrAtt.PositionIncrement;
                                }
                            }
                            catch (IOException)
                            {
                                // safe to ignore, because we know the number of tokens
                            }

                            if (positionIncrement > 0 && multiTerms.Count > 0)
                            {
                                if (enablePositionIncrements)
                                {
                                    mpq.Add(multiTerms.ToArray(), position);
                                }
                                else
                                {
                                    mpq.Add(multiTerms.ToArray());
                                }
                                multiTerms.Clear();
                            }
                            position += positionIncrement;
                            multiTerms.Add(new Term(field, BytesRef.DeepCopyOf(bytes)));
                        }
                        if (enablePositionIncrements)
                        {
                            mpq.Add(multiTerms.ToArray(), position);
                        }
                        else
                        {
                            mpq.Add(multiTerms.ToArray());
                        }
                        return mpq;
                    }
                }
                else
                {
                    PhraseQuery pq = NewPhraseQuery();
                    pq.Slop = phraseSlop;
                    int position = -1;

                    for (int i = 0; i < numTokens; i++)
                    {
                        int positionIncrement = 1;

                        try
                        {
                            bool hasNext = buffer.IncrementToken();
                            //assert hasNext == true;
                            termAtt.FillBytesRef();
                            if (posIncrAtt != null)
                            {
                                positionIncrement = posIncrAtt.PositionIncrement;
                            }
                        }
                        catch (IOException)
                        {
                            // safe to ignore, because we know the number of tokens
                        }

                        if (enablePositionIncrements)
                        {
                            position += positionIncrement;
                            pq.Add(new Term(field, BytesRef.DeepCopyOf(bytes)), position);
                        }
                        else
                        {
                            pq.Add(new Term(field, BytesRef.DeepCopyOf(bytes)));
                        }
                    }
                    return pq;
                }
            }
        }

        protected virtual Query GetFieldQuery(String field, String queryText, int slop)
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

        protected virtual Query GetRangeQuery(String field, String part1, String part2, bool startInclusive, bool endInclusive)
        {
            if (lowercaseExpandedTerms)
            {
                part1 = part1 == null ? null : part1.ToLower(locale);
                part2 = part2 == null ? null : part2.ToLower(locale);
            }


            //DateTimeFormatInfo df = DateTimeFormatInfo.GetInstance(locale);
            //df.setLenient(true);
            DateTools.Resolution resolution = GetDateResolution(field);

            try
            {
                part1 = DateTools.DateToString(DateTime.Parse(part1, locale), resolution);
            }
            catch (Exception) { }

            try
            {
                DateTime d2 = DateTime.Parse(part2, locale);
                if (endInclusive)
                {
                    // The user can only specify the date, not the time, so make sure
                    // the time is set to the latest possible time of that date to really
                    // include all documents:
                    d2 = d2.AddHours(23);
                    d2 = d2.AddMinutes(59);
                    d2 = d2.AddSeconds(59);
                    d2 = d2.AddMilliseconds(999);
                    // .NET Port TODO: is this right?
                }
                part2 = DateTools.DateToString(d2, resolution);
            }
            catch (Exception) { }

            return NewRangeQuery(field, part1, part2, startInclusive, endInclusive);
        }

        protected virtual BooleanQuery NewBooleanQuery(bool disableCoord)
        {
            return new BooleanQuery(disableCoord);
        }

        protected virtual BooleanClause NewBooleanClause(Query q, Occur occur)
        {
            return new BooleanClause(q, occur);
        }

        protected virtual Query NewTermQuery(Term term)
        {
            return new TermQuery(term);
        }

        protected virtual PhraseQuery NewPhraseQuery()
        {
            return new PhraseQuery();
        }

        protected virtual MultiPhraseQuery NewMultiPhraseQuery()
        {
            return new MultiPhraseQuery();
        }

        protected virtual Query NewPrefixQuery(Term prefix)
        {
            PrefixQuery query = new PrefixQuery(prefix);
            query.SetRewriteMethod(multiTermRewriteMethod);
            return query;
        }

        protected virtual Query NewRegexpQuery(Term regexp)
        {
            RegexpQuery query = new RegexpQuery(regexp);
            query.SetRewriteMethod(multiTermRewriteMethod);
            return query;
        }

        protected virtual Query NewFuzzyQuery(Term term, float minimumSimilarity, int prefixLength)
        {
            // FuzzyQuery doesn't yet allow constant score rewrite
            String text = term.Text;
            int numEdits = FuzzyQuery.FloatToEdits(minimumSimilarity,
                text.Length);
            return new FuzzyQuery(term, numEdits, prefixLength);
        }

        // TODO: Should this be protected instead?
        private BytesRef AnalyzeMultitermTerm(String field, String part)
        {
            return AnalyzeMultitermTerm(field, part, analyzer);
        }

        protected BytesRef AnalyzeMultitermTerm(String field, String part, Analyzer analyzerIn)
        {
            TokenStream source;

            if (analyzerIn == null) analyzerIn = analyzer;

            try
            {
                source = analyzerIn.TokenStream(field, new StringReader(part));
                source.Reset();
            }
            catch (IOException e)
            {
                throw new SystemException("Unable to initialize TokenStream to analyze multiTerm term: " + part, e);
            }

            ITermToBytesRefAttribute termAtt = source.GetAttribute<ITermToBytesRefAttribute>();
            BytesRef bytes = termAtt.BytesRef;

            try
            {
                if (!source.IncrementToken())
                    throw new ArgumentException("analyzer returned no terms for multiTerm term: " + part);
                termAtt.FillBytesRef();
                if (source.IncrementToken())
                    throw new ArgumentException("analyzer returned too many terms for multiTerm term: " + part);
            }
            catch (IOException e)
            {
                throw new SystemException("error analyzing range part: " + part, e);
            }

            try
            {
                source.End();
                source.Dispose();
            }
            catch (IOException e)
            {
                throw new SystemException("Unable to end & close TokenStream after analyzing multiTerm term: " + part, e);
            }

            return BytesRef.DeepCopyOf(bytes);
        }

        protected virtual Query NewRangeQuery(String field, String part1, String part2, bool startInclusive, bool endInclusive)
        {
            BytesRef start;
            BytesRef end;

            if (part1 == null)
            {
                start = null;
            }
            else
            {
                start = analyzeRangeTerms ? AnalyzeMultitermTerm(field, part1) : new BytesRef(part1);
            }

            if (part2 == null)
            {
                end = null;
            }
            else
            {
                end = analyzeRangeTerms ? AnalyzeMultitermTerm(field, part2) : new BytesRef(part2);
            }

            TermRangeQuery query = new TermRangeQuery(field, start, end, startInclusive, endInclusive);

            query.SetRewriteMethod(multiTermRewriteMethod);
            return query;
        }

        protected Query NewMatchAllDocsQuery()
        {
            return new MatchAllDocsQuery();
        }

        protected Query NewWildcardQuery(Term t)
        {
            WildcardQuery query = new WildcardQuery(t);
            query.SetRewriteMethod(multiTermRewriteMethod);
            return query;
        }

        protected Query GetBooleanQuery(IList<BooleanClause> clauses)
        {
            return GetBooleanQuery(clauses, false);
        }

        protected Query GetBooleanQuery(IList<BooleanClause> clauses, bool disableCoord)
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

        protected virtual Query GetWildcardQuery(String field, String termStr)
        {
            if ("*".Equals(field))
            {
                if ("*".Equals(termStr)) return NewMatchAllDocsQuery();
            }
            if (!allowLeadingWildcard && (termStr.StartsWith("*") || termStr.StartsWith("?")))
                throw new ParseException("'*' or '?' not allowed as first character in WildcardQuery");
            if (lowercaseExpandedTerms)
            {
                termStr = termStr.ToLower(locale);
            }
            Term t = new Term(field, termStr);
            return NewWildcardQuery(t);
        }

        protected virtual Query GetRegexpQuery(String field, String termStr)
        {
            if (lowercaseExpandedTerms)
            {
                termStr = termStr.ToLower(locale);
            }
            Term t = new Term(field, termStr);
            return NewRegexpQuery(t);
        }

        protected virtual Query GetPrefixQuery(String field, String termStr)
        {
            if (!allowLeadingWildcard && termStr.StartsWith("*"))
                throw new ParseException("'*' not allowed as first character in PrefixQuery");
            if (lowercaseExpandedTerms)
            {
                termStr = termStr.ToLower(locale);
            }
            Term t = new Term(field, termStr);
            return NewPrefixQuery(t);
        }

        protected virtual Query GetFuzzyQuery(String field, String termStr, float minSimilarity)
        {
            if (lowercaseExpandedTerms)
            {
                termStr = termStr.ToLower(locale);
            }
            Term t = new Term(field, termStr);
            return NewFuzzyQuery(t, minSimilarity, fuzzyPrefixLength);
        }

        internal Query HandleBareTokenQuery(String qfield, Token term, Token fuzzySlop, bool prefix, bool wildcard, bool fuzzy, bool regexp)
        {
            Query q;

            String termImage = DiscardEscapeChar(term.image);
            if (wildcard)
            {
                q = GetWildcardQuery(qfield, term.image);
            }
            else if (prefix)
            {
                q = GetPrefixQuery(qfield,
                    DiscardEscapeChar(term.image.Substring
                        (0, term.image.Length - 1)));
            }
            else if (regexp)
            {
                q = GetRegexpQuery(qfield, term.image.Substring(1, term.image.Length - 1));
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

        internal Query HandleBareFuzzy(String qfield, Token fuzzySlop, String termImage)
        {
            Query q;
            float fms = fuzzyMinSim;
            try
            {
                fms = float.Parse(fuzzySlop.image.Substring(1));
            }
            catch (Exception) { }
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

        internal Query HandleQuotedTerm(String qfield, Token term, Token fuzzySlop)
        {
            int s = phraseSlop;  // default
            if (fuzzySlop != null)
            {
                try
                {
                    s = (int)float.Parse(fuzzySlop.image.Substring(1));
                }
                catch (Exception) { }
            }
            return GetFieldQuery(qfield, DiscardEscapeChar(term.image.Substring(1, term.image.Length - 1)), s);
        }

        internal Query HandleBoost(Query q, Token boost)
        {
            if (boost != null)
            {
                float f = (float)1.0;
                try
                {
                    f = float.Parse(boost.image);
                }
                catch (Exception)
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

        internal String DiscardEscapeChar(String input)
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

            return new String(output, 0, length);
        }

        internal static int HexToInt(char c)
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

        public static String Escape(String s)
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
