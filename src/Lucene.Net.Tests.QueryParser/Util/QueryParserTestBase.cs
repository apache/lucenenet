using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.QueryParsers.Flexible.Standard;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using System;
using System.Globalization;

namespace Lucene.Net.QueryParsers.Util
{
    /// <summary>
    /// In .NET the abstact members were moved to AbstractQueryParserTestBase
    /// because the Visual Studio test runner does not find or run tests in 
    /// abstract classes.
    /// </summary>
    [TestFixture]
    public abstract class QueryParserTestBase : LuceneTestCase
    {
        public static Analyzer qpAnalyzer;

        [OneTimeSetUp]
        public static void BeforeClass()
        {
            qpAnalyzer = new QPTestAnalyzer();
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            qpAnalyzer = null;
        }

        public sealed class QPTestFilter : TokenFilter
        {
            ICharTermAttribute termAtt;
            IOffsetAttribute offsetAtt;

            /**
             * Filter which discards the token 'stop' and which expands the
             * token 'phrase' into 'phrase1 phrase2'
             */
            public QPTestFilter(TokenStream @in)
                : base(@in)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
            }

            bool inPhrase = false;
            int savedStart = 0, savedEnd = 0;

            public override sealed bool IncrementToken()
            {
                if (inPhrase)
                {
                    inPhrase = false;
                    ClearAttributes();
                    termAtt.Append("phrase2");
                    offsetAtt.SetOffset(savedStart, savedEnd);
                    return true;
                }
                else
                    while (input.IncrementToken())
                    {
                        if (termAtt.toString().Equals("phrase"))
                        {
                            inPhrase = true;
                            savedStart = offsetAtt.StartOffset();
                            savedEnd = offsetAtt.EndOffset();
                            termAtt.SetEmpty().Append("phrase1");
                            offsetAtt.SetOffset(savedStart, savedEnd);
                            return true;
                        }
                        else if (!termAtt.toString().equals("stop"))
                            return true;
                    }
                return false;
            }
        }

        public sealed class QPTestAnalyzer : Analyzer
        {
            /// <summary>
            /// Filters MockTokenizer with StopFilter.
            /// </summary>
            public override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(tokenizer, new QPTestFilter(tokenizer));
            }
        }

        private int originalMaxClauses;

        private string defaultField = "field";
        public string DefaultField { get { return defaultField; } set { defaultField = value; } }

        public override void SetUp()
        {
            base.SetUp();
            originalMaxClauses = BooleanQuery.MaxClauseCount;
        }

        public abstract ICommonQueryParserConfiguration GetParserConfig(Analyzer a);

        public abstract void SetDefaultOperatorOR(ICommonQueryParserConfiguration cqpC);

        public abstract void SetDefaultOperatorAND(ICommonQueryParserConfiguration cqpC);

        public abstract void SetAnalyzeRangeTerms(ICommonQueryParserConfiguration cqpC, bool value);

        public abstract void SetAutoGeneratePhraseQueries(ICommonQueryParserConfiguration cqpC, bool value);

        public abstract void SetDateResolution(ICommonQueryParserConfiguration cqpC, string field, DateTools.Resolution value);

        public abstract Query GetQuery(string query, ICommonQueryParserConfiguration cqpC);

        public abstract Query GetQuery(string query, Analyzer a);

        public abstract bool IsQueryParserException(Exception exception);

        public Query GetQuery(string query)
        {
            return GetQuery(query, (Analyzer)null);
        }

        public void AssertQueryEquals(string query, Analyzer a, string result)
        {
            Query q = GetQuery(query, a);
            string s = q.ToString("field");
            if (!s.equals(result))
            {
                fail("Query /" + query + "/ yielded /" + s
                     + "/, expecting /" + result + "/");
            }
        }

        public void AssertQueryEquals(ICommonQueryParserConfiguration cqpC, string field, string query, string result)
        {
            Query q = GetQuery(query, cqpC);
            string s = q.ToString(field);
            if (!s.Equals(result))
            {
                fail("Query /" + query + "/ yielded /" + s
                     + "/, expecting /" + result + "/");
            }
        }

        public void AssertEscapedQueryEquals(string query, Analyzer a, string result)
        {
            string escapedQuery = QueryParserBase.Escape(query);
            if (!escapedQuery.Equals(result))
            {
                fail("Query /" + query + "/ yielded /" + escapedQuery
                    + "/, expecting /" + result + "/");
            }
        }

        public void AssertWildcardQueryEquals(string query, bool lowercase, string result, bool allowLeadingWildcard)
        {
            ICommonQueryParserConfiguration cqpC = GetParserConfig(null);
            cqpC.LowercaseExpandedTerms = lowercase;
            cqpC.AllowLeadingWildcard = allowLeadingWildcard;
            Query q = GetQuery(query, cqpC);
            string s = q.ToString("field");
            if (!s.equals(result))
            {
                fail("WildcardQuery /" + query + "/ yielded /" + s
                     + "/, expecting /" + result + "/");
            }
        }

        public void AssertWildcardQueryEquals(string query, bool lowercase, string result)
        {
            AssertWildcardQueryEquals(query, lowercase, result, false);
        }

        public void AssertWildcardQueryEquals(string query, string result)
        {
            Query q = GetQuery(query);
            string s = q.ToString("field");
            if (!s.Equals(result))
            {
                fail("WildcardQuery /" + query + "/ yielded /" + s + "/, expecting /"
                    + result + "/");
            }
        }

        public Query GetQueryDOA(string query, Analyzer a)
        {
            if (a == null)
                a = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true);
            ICommonQueryParserConfiguration qp = GetParserConfig(a);
            SetDefaultOperatorAND(qp);
            return GetQuery(query, qp);
        }

        public void AssertQueryEqualsDOA(string query, Analyzer a, string result)
        {
            Query q = GetQueryDOA(query, a);
            string s = q.ToString("field");
            if (!s.Equals(result))
            {
                fail("Query /" + query + "/ yielded /" + s
                     + "/, expecting /" + result + "/");
            }
        }

        [Test]
        public virtual void TestCJK()
        {
            // Test Ideographic Space - As wide as a CJK character cell (fullwidth)
            // used google to translate the word "term" to japanese -> 用語
            AssertQueryEquals("term\u3000term\u3000term", null, "term\u0020term\u0020term");
            AssertQueryEquals("用語\u3000用語\u3000用語", null, "用語\u0020用語\u0020用語");
        }

        protected class SimpleCJKTokenizer : Tokenizer
        {
            private ICharTermAttribute termAtt;

            public SimpleCJKTokenizer(System.IO.TextReader input)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                int ch = input.Read();
                if (ch < 0)
                    return false;
                ClearAttributes();
                termAtt.SetEmpty().Append((char)ch);
                return true;
            }
        }

        private class SimpleCJKAnalyzer : Analyzer
        {
            public override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                return new TokenStreamComponents(new SimpleCJKTokenizer(reader));
            }
        }

        [Test]
        public virtual void TestCJKTerm()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "国")), BooleanClause.Occur.SHOULD);

            assertEquals(expected, GetQuery("中国", analyzer));
        }

        [Test]
        public virtual void TestCJKBoostedTerm()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            BooleanQuery expected = new BooleanQuery();
            expected.Boost = (0.5f);
            expected.Add(new TermQuery(new Term("field", "中")), BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "国")), BooleanClause.Occur.SHOULD);

            assertEquals(expected, GetQuery("中国^0.5", analyzer));
        }

        [Test]
        public virtual void TestCJKPhrase()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            PhraseQuery expected = new PhraseQuery();
            expected.Add(new Term("field", "中"));
            expected.Add(new Term("field", "国"));

            assertEquals(expected, GetQuery("\"中国\"", analyzer));
        }

        [Test]
        public virtual void TestCJKBoostedPhrase()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            PhraseQuery expected = new PhraseQuery();
            expected.Boost = (0.5f);
            expected.Add(new Term("field", "中"));
            expected.Add(new Term("field", "国"));

            assertEquals(expected, GetQuery("\"中国\"^0.5", analyzer));
        }

        [Test]
        public virtual void TestCJKSloppyPhrase()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            PhraseQuery expected = new PhraseQuery();
            expected.Slop = (3);
            expected.Add(new Term("field", "中"));
            expected.Add(new Term("field", "国"));

            assertEquals(expected, GetQuery("\"中国\"~3", analyzer));
        }

        [Test]
        public virtual void TestAutoGeneratePhraseQueriesOn()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            PhraseQuery expected = new PhraseQuery();
            expected.Add(new Term("field", "中"));
            expected.Add(new Term("field", "国"));
            ICommonQueryParserConfiguration qp = GetParserConfig(analyzer);
            SetAutoGeneratePhraseQueries(qp, true);
            assertEquals(expected, GetQuery("中国", qp));
        }

        [Test]
        public virtual void TestSimple()
        {
            AssertQueryEquals("term term term", null, "term term term");
            AssertQueryEquals("türm term term", new MockAnalyzer(Random()), "türm term term");
            AssertQueryEquals("ümlaut", new MockAnalyzer(Random()), "ümlaut");

            // FIXME: enhance MockAnalyzer to be able to support this
            // it must no longer extend CharTokenizer
            //AssertQueryEquals("\"\"", new KeywordAnalyzer(), "");
            //AssertQueryEquals("foo:\"\"", new KeywordAnalyzer(), "foo:");

            AssertQueryEquals("a AND b", null, "+a +b");
            AssertQueryEquals("(a AND b)", null, "+a +b");
            AssertQueryEquals("c OR (a AND b)", null, "c (+a +b)");
            AssertQueryEquals("a AND NOT b", null, "+a -b");
            AssertQueryEquals("a AND -b", null, "+a -b");
            AssertQueryEquals("a AND !b", null, "+a -b");
            AssertQueryEquals("a && b", null, "+a +b");
            //    AssertQueryEquals("a && ! b", null, "+a -b");

            AssertQueryEquals("a OR b", null, "a b");
            AssertQueryEquals("a || b", null, "a b");
            AssertQueryEquals("a OR !b", null, "a -b");
            //    AssertQueryEquals("a OR ! b", null, "a -b");
            AssertQueryEquals("a OR -b", null, "a -b");

            AssertQueryEquals("+term -term term", null, "+term -term term");
            AssertQueryEquals("foo:term AND field:anotherTerm", null,
                              "+foo:term +anotherterm");
            AssertQueryEquals("term AND \"phrase phrase\"", null,
                              "+term +\"phrase phrase\"");
            AssertQueryEquals("\"hello there\"", null, "\"hello there\"");
            assertTrue(GetQuery("a AND b") is BooleanQuery);
            assertTrue(GetQuery("hello") is TermQuery);
            assertTrue(GetQuery("\"hello there\"") is PhraseQuery);

            AssertQueryEquals("germ term^2.0", null, "germ term^2.0");
            AssertQueryEquals("(term)^2.0", null, "term^2.0");
            AssertQueryEquals("(germ term)^2.0", null, "(germ term)^2.0");
            AssertQueryEquals("term^2.0", null, "term^2.0");
            AssertQueryEquals("term^2", null, "term^2.0");
            AssertQueryEquals("\"germ term\"^2.0", null, "\"germ term\"^2.0");
            AssertQueryEquals("\"term germ\"^2", null, "\"term germ\"^2.0");

            AssertQueryEquals("(foo OR bar) AND (baz OR boo)", null,
                              "+(foo bar) +(baz boo)");
            AssertQueryEquals("((a OR b) AND NOT c) OR d", null,
                              "(+(a b) -c) d");
            AssertQueryEquals("+(apple \"steve jobs\") -(foo bar baz)", null,
                              "+(apple \"steve jobs\") -(foo bar baz)");
            AssertQueryEquals("+title:(dog OR cat) -author:\"bob dole\"", null,
                              "+(title:dog title:cat) -author:\"bob dole\"");

        }

        public abstract void TestDefaultOperator();

        private class OperatorVsWhitespaceAnalyzer : Analyzer
        {
            public override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, false));
            }
        }

        [Test]
        public virtual void TestOperatorVsWhitespace()
        { //LUCENE-2566
            // +,-,! should be directly adjacent to operand (i.e. not separated by whitespace) to be treated as an operator
            Analyzer a = new OperatorVsWhitespaceAnalyzer();
            AssertQueryEquals("a - b", a, "a - b");
            AssertQueryEquals("a + b", a, "a + b");
            AssertQueryEquals("a ! b", a, "a ! b");
        }

        [Test]
        public virtual void TestPunct()
        {
            Analyzer a = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false);
            AssertQueryEquals("a&b", a, "a&b");
            AssertQueryEquals("a&&b", a, "a&&b");
            AssertQueryEquals(".NET", a, ".NET");
        }

        [Test]
        public virtual void TestSlop()
        {
            AssertQueryEquals("\"term germ\"~2", null, "\"term germ\"~2");
            AssertQueryEquals("\"term germ\"~2 flork", null, "\"term germ\"~2 flork");
            AssertQueryEquals("\"term\"~2", null, "term");
            AssertQueryEquals("\" \"~2 germ", null, "germ");
            AssertQueryEquals("\"term germ\"~2^2", null, "\"term germ\"~2^2.0");
        }

        [Test]
        public virtual void TestNumber()
        {
            // The numbers go away because SimpleAnalzyer ignores them
            AssertQueryEquals("3", null, "");
            AssertQueryEquals("term 1.0 1 2", null, "term");
            AssertQueryEquals("term term1 term2", null, "term term term");

            Analyzer a = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, true);
            AssertQueryEquals("3", a, "3");
            AssertQueryEquals("term 1.0 1 2", a, "term 1.0 1 2");
            AssertQueryEquals("term term1 term2", a, "term term1 term2");
        }

        [Test]
        public virtual void TestWildcard()
        {
            AssertQueryEquals("term*", null, "term*");
            AssertQueryEquals("term*^2", null, "term*^2.0");
            AssertQueryEquals("term~", null, "term~2");
            AssertQueryEquals("term~1", null, "term~1");
            AssertQueryEquals("term~0.7", null, "term~1");
            AssertQueryEquals("term~^3", null, "term~2^3.0");
            AssertQueryEquals("term^3~", null, "term~2^3.0");
            AssertQueryEquals("term*germ", null, "term*germ");
            AssertQueryEquals("term*germ^3", null, "term*germ^3.0");

            assertTrue(GetQuery("term*") is PrefixQuery);
            assertTrue(GetQuery("term*^2") is PrefixQuery);
            assertTrue(GetQuery("term~") is FuzzyQuery);
            assertTrue(GetQuery("term~0.7") is FuzzyQuery);
            FuzzyQuery fq = (FuzzyQuery)GetQuery("term~0.7");
            assertEquals(1, fq.MaxEdits);
            assertEquals(FuzzyQuery.DefaultPrefixLength, fq.PrefixLength);
            fq = (FuzzyQuery)GetQuery("term~");
            assertEquals(2, fq.MaxEdits);
            assertEquals(FuzzyQuery.DefaultPrefixLength, fq.PrefixLength);

            AssertParseException("term~1.1"); // value > 1, throws exception

            assertTrue(GetQuery("term*germ") is WildcardQuery);

            /* Tests to see that wild card terms are (or are not) properly
               * lower-cased with propery parser configuration
               */
            // First prefix queries:
            // by default, convert to lowercase:
            AssertWildcardQueryEquals("Term*", true, "term*");
            // explicitly set lowercase:
            AssertWildcardQueryEquals("term*", true, "term*");
            AssertWildcardQueryEquals("Term*", true, "term*");
            AssertWildcardQueryEquals("TERM*", true, "term*");
            // explicitly disable lowercase conversion:
            AssertWildcardQueryEquals("term*", false, "term*");
            AssertWildcardQueryEquals("Term*", false, "Term*");
            AssertWildcardQueryEquals("TERM*", false, "TERM*");
            // Then 'full' wildcard queries:
            // by default, convert to lowercase:
            AssertWildcardQueryEquals("Te?m", "te?m");
            // explicitly set lowercase:
            AssertWildcardQueryEquals("te?m", true, "te?m");
            AssertWildcardQueryEquals("Te?m", true, "te?m");
            AssertWildcardQueryEquals("TE?M", true, "te?m");
            AssertWildcardQueryEquals("Te?m*gerM", true, "te?m*germ");
            // explicitly disable lowercase conversion:
            AssertWildcardQueryEquals("te?m", false, "te?m");
            AssertWildcardQueryEquals("Te?m", false, "Te?m");
            AssertWildcardQueryEquals("TE?M", false, "TE?M");
            AssertWildcardQueryEquals("Te?m*gerM", false, "Te?m*gerM");
            //  Fuzzy queries:
            AssertWildcardQueryEquals("Term~", "term~2");
            AssertWildcardQueryEquals("Term~", true, "term~2");
            AssertWildcardQueryEquals("Term~", false, "Term~2");
            //  Range queries:
            AssertWildcardQueryEquals("[A TO C]", "[a TO c]");
            AssertWildcardQueryEquals("[A TO C]", true, "[a TO c]");
            AssertWildcardQueryEquals("[A TO C]", false, "[A TO C]");
            // Test suffix queries: first disallow
            try
            {
                AssertWildcardQueryEquals("*Term", true, "*term");
            }
            catch (Exception pe)
            {
                // expected exception
                if (!IsQueryParserException(pe))
                {
                    fail();
                }
            }
            try
            {
                AssertWildcardQueryEquals("?Term", true, "?term");
                fail();
            }
            catch (Exception pe)
            {
                // expected exception
                if (!IsQueryParserException(pe))
                {
                    fail();
                }
            }
            // Test suffix queries: then allow
            AssertWildcardQueryEquals("*Term", true, "*term", true);
            AssertWildcardQueryEquals("?Term", true, "?term", true);
        }

        [Test]
        public virtual void TestLeadingWildcardType()
        {
            ICommonQueryParserConfiguration cqpC = GetParserConfig(null);
            cqpC.AllowLeadingWildcard = (true);
            assertEquals(typeof(WildcardQuery), GetQuery("t*erm*", cqpC).GetType());
            assertEquals(typeof(WildcardQuery), GetQuery("?term*", cqpC).GetType());
            assertEquals(typeof(WildcardQuery), GetQuery("*term*", cqpC).GetType());
        }

        [Test]
        public virtual void TestQPA()
        {
            AssertQueryEquals("term term^3.0 term", qpAnalyzer, "term term^3.0 term");
            AssertQueryEquals("term stop^3.0 term", qpAnalyzer, "term term");

            AssertQueryEquals("term term term", qpAnalyzer, "term term term");
            AssertQueryEquals("term +stop term", qpAnalyzer, "term term");
            AssertQueryEquals("term -stop term", qpAnalyzer, "term term");

            AssertQueryEquals("drop AND (stop) AND roll", qpAnalyzer, "+drop +roll");
            AssertQueryEquals("term +(stop) term", qpAnalyzer, "term term");
            AssertQueryEquals("term -(stop) term", qpAnalyzer, "term term");

            AssertQueryEquals("drop AND stop AND roll", qpAnalyzer, "+drop +roll");
            AssertQueryEquals("term phrase term", qpAnalyzer,
                              "term (phrase1 phrase2) term");
            AssertQueryEquals("term AND NOT phrase term", qpAnalyzer,
                              "+term -(phrase1 phrase2) term");
            AssertQueryEquals("stop^3", qpAnalyzer, "");
            AssertQueryEquals("stop", qpAnalyzer, "");
            AssertQueryEquals("(stop)^3", qpAnalyzer, "");
            AssertQueryEquals("((stop))^3", qpAnalyzer, "");
            AssertQueryEquals("(stop^3)", qpAnalyzer, "");
            AssertQueryEquals("((stop)^3)", qpAnalyzer, "");
            AssertQueryEquals("(stop)", qpAnalyzer, "");
            AssertQueryEquals("((stop))", qpAnalyzer, "");
            assertTrue(GetQuery("term term term", qpAnalyzer) is BooleanQuery);
            assertTrue(GetQuery("term +stop", qpAnalyzer) is TermQuery);

            ICommonQueryParserConfiguration cqpc = GetParserConfig(qpAnalyzer);
            SetDefaultOperatorAND(cqpc);
            AssertQueryEquals(cqpc, "field", "term phrase term",
                "+term +(+phrase1 +phrase2) +term");
            AssertQueryEquals(cqpc, "field", "phrase",
                "+phrase1 +phrase2");
        }

        [Test]
        public virtual void TestRange()
        {
            AssertQueryEquals("[ a TO z]", null, "[a TO z]");
            AssertQueryEquals("[ a TO z}", null, "[a TO z}");
            AssertQueryEquals("{ a TO z]", null, "{a TO z]");

            assertEquals(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT, ((TermRangeQuery)GetQuery("[ a TO z]")).GetRewriteMethod());

            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true));

            qp.MultiTermRewriteMethod=(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
            assertEquals(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE, ((TermRangeQuery)GetQuery("[ a TO z]", qp)).GetRewriteMethod());

            // test open ranges
            AssertQueryEquals("[ a TO * ]", null, "[a TO *]");
            AssertQueryEquals("[ * TO z ]", null, "[* TO z]");
            AssertQueryEquals("[ * TO * ]", null, "[* TO *]");

            // mixing exclude and include bounds
            AssertQueryEquals("{ a TO z ]", null, "{a TO z]");
            AssertQueryEquals("[ a TO z }", null, "[a TO z}");
            AssertQueryEquals("{ a TO * ]", null, "{a TO *]");
            AssertQueryEquals("[ * TO z }", null, "[* TO z}");

            AssertQueryEquals("[ a TO z ]", null, "[a TO z]");
            AssertQueryEquals("{ a TO z}", null, "{a TO z}");
            AssertQueryEquals("{ a TO z }", null, "{a TO z}");
            AssertQueryEquals("{ a TO z }^2.0", null, "{a TO z}^2.0");
            AssertQueryEquals("[ a TO z] OR bar", null, "[a TO z] bar");
            AssertQueryEquals("[ a TO z] AND bar", null, "+[a TO z] +bar");
            AssertQueryEquals("( bar blar { a TO z}) ", null, "bar blar {a TO z}");
            AssertQueryEquals("gack ( bar blar { a TO z}) ", null, "gack (bar blar {a TO z})");

            AssertQueryEquals("[* TO Z]", null, "[* TO z]");
            AssertQueryEquals("[A TO *]", null, "[a TO *]");
            AssertQueryEquals("[* TO *]", null, "[* TO *]");
        }

        [Test]
        public virtual void TestRangeWithPhrase()
        {
            AssertQueryEquals("[\\* TO \"*\"]", null, "[\\* TO \\*]");
            AssertQueryEquals("[\"*\" TO *]", null, "[\\* TO *]");
        }

        private string EscapeDateString(string s)
        {
            if (s.IndexOf(" ") > -1)
            {
                return "\"" + s + "\"";
            }
            else
            {
                return s;
            }
        }

        /// <summary>for testing DateTools support</summary>
        private string GetDate(string s, DateTools.Resolution resolution)
        {
            // TODO: Is this the correct way to parse the string?
            DateTime d = DateTime.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            return GetDate(d, resolution);

            //// we use the default Locale since LuceneTestCase randomizes it
            //DateFormat df = DateFormat.getDateInstance(DateFormat.SHORT, Locale.getDefault());
            //return GetDate(df.Parse(s), resolution);      
        }

        /// <summary>for testing DateTools support</summary>
        private string GetDate(DateTime d, DateTools.Resolution resolution)
        {
            return DateTools.DateToString(d, resolution);
        }

        private string GetLocalizedDate(int year, int month, int day)
        {
            DateTime d = new DateTime(year, month, day, 23, 59, 59, 999);
            return d.ToString("d");

            //// we use the default Locale/TZ since LuceneTestCase randomizes it
            //DateFormat df = DateFormat.getDateInstance(DateFormat.SHORT, Locale.getDefault());
            //Calendar calendar = new GregorianCalendar(TimeZone.getDefault(), Locale.getDefault());
            //calendar.clear();
            //calendar.set(year, month, day);
            //calendar.set(Calendar.HOUR_OF_DAY, 23);
            //calendar.set(Calendar.MINUTE, 59);
            //calendar.set(Calendar.SECOND, 59);
            //calendar.set(Calendar.MILLISECOND, 999);
            //return df.format(calendar.getTime());
        }

        [Test]
        public virtual void TestDateRange()
        {
            string startDate = GetLocalizedDate(2002, 1, 1);
            string endDate = GetLocalizedDate(2002, 1, 4);

            // we use the default Locale/TZ since LuceneTestCase randomizes it
            //Calendar endDateExpected = new GregorianCalendar(TimeZone.getDefault(), Locale.getDefault());
            //endDateExpected.clear();
            //endDateExpected.set(2002, 1, 4, 23, 59, 59);
            //endDateExpected.set(Calendar.MILLISECOND, 999);

            // we use the default Locale/TZ since LuceneTestCase randomizes it
            DateTime endDateExpected = new GregorianCalendar().ToDateTime(2002, 1, 4, 23, 59, 59, 999);
            string defaultField = "default";
            string monthField = "month";
            string hourField = "hour";
            Analyzer a = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true);
            ICommonQueryParserConfiguration qp = GetParserConfig(a);

            // set a field specific date resolution
            SetDateResolution(qp, monthField, DateTools.Resolution.MONTH);

            // set default date resolution to MILLISECOND
            qp.SetDateResolution(DateTools.Resolution.MILLISECOND);

            // set second field specific date resolution    
            SetDateResolution(qp, hourField, DateTools.Resolution.HOUR);

            // for this field no field specific date resolution has been set,
            // so verify if the default resolution is used
            AssertDateRangeQueryEquals(qp, defaultField, startDate, endDate,
                    endDateExpected /*.getTime()*/, DateTools.Resolution.MILLISECOND);

            // verify if field specific date resolutions are used for these two fields
            AssertDateRangeQueryEquals(qp, monthField, startDate, endDate,
                    endDateExpected /*.getTime()*/, DateTools.Resolution.MONTH);

            AssertDateRangeQueryEquals(qp, hourField, startDate, endDate,
                    endDateExpected /*.getTime()*/, DateTools.Resolution.HOUR);
        }

        public void AssertDateRangeQueryEquals(ICommonQueryParserConfiguration cqpC, string field, string startDate, string endDate,
            DateTime endDateInclusive, DateTools.Resolution resolution)
        {
            AssertQueryEquals(cqpC, field, field + ":[" + EscapeDateString(startDate) + " TO " + EscapeDateString(endDate) + "]",
                       "[" + GetDate(startDate, resolution) + " TO " + GetDate(endDateInclusive, resolution) + "]");
            AssertQueryEquals(cqpC, field, field + ":{" + EscapeDateString(startDate) + " TO " + EscapeDateString(endDate) + "}",
                       "{" + GetDate(startDate, resolution) + " TO " + GetDate(endDate, resolution) + "}");
        }

        [Test]
        public virtual void TestEscaped()
        {
            Analyzer a = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false);

            /*AssertQueryEquals("\\[brackets", a, "\\[brackets");
            AssertQueryEquals("\\[brackets", null, "brackets");
            AssertQueryEquals("\\\\", a, "\\\\");
            AssertQueryEquals("\\+blah", a, "\\+blah");
            AssertQueryEquals("\\(blah", a, "\\(blah");

            AssertQueryEquals("\\-blah", a, "\\-blah");
            AssertQueryEquals("\\!blah", a, "\\!blah");
            AssertQueryEquals("\\{blah", a, "\\{blah");
            AssertQueryEquals("\\}blah", a, "\\}blah");
            AssertQueryEquals("\\:blah", a, "\\:blah");
            AssertQueryEquals("\\^blah", a, "\\^blah");
            AssertQueryEquals("\\[blah", a, "\\[blah");
            AssertQueryEquals("\\]blah", a, "\\]blah");
            AssertQueryEquals("\\\"blah", a, "\\\"blah");
            AssertQueryEquals("\\(blah", a, "\\(blah");
            AssertQueryEquals("\\)blah", a, "\\)blah");
            AssertQueryEquals("\\~blah", a, "\\~blah");
            AssertQueryEquals("\\*blah", a, "\\*blah");
            AssertQueryEquals("\\?blah", a, "\\?blah");
            //AssertQueryEquals("foo \\&\\& bar", a, "foo \\&\\& bar");
            //AssertQueryEquals("foo \\|| bar", a, "foo \\|| bar");
            //AssertQueryEquals("foo \\AND bar", a, "foo \\AND bar");*/

            AssertQueryEquals("\\a", a, "a");

            AssertQueryEquals("a\\-b:c", a, "a-b:c");
            AssertQueryEquals("a\\+b:c", a, "a+b:c");
            AssertQueryEquals("a\\:b:c", a, "a:b:c");
            AssertQueryEquals("a\\\\b:c", a, "a\\b:c");

            AssertQueryEquals("a:b\\-c", a, "a:b-c");
            AssertQueryEquals("a:b\\+c", a, "a:b+c");
            AssertQueryEquals("a:b\\:c", a, "a:b:c");
            AssertQueryEquals("a:b\\\\c", a, "a:b\\c");

            AssertQueryEquals("a:b\\-c*", a, "a:b-c*");
            AssertQueryEquals("a:b\\+c*", a, "a:b+c*");
            AssertQueryEquals("a:b\\:c*", a, "a:b:c*");

            AssertQueryEquals("a:b\\\\c*", a, "a:b\\c*");

            AssertQueryEquals("a:b\\-c~", a, "a:b-c~2");
            AssertQueryEquals("a:b\\+c~", a, "a:b+c~2");
            AssertQueryEquals("a:b\\:c~", a, "a:b:c~2");
            AssertQueryEquals("a:b\\\\c~", a, "a:b\\c~2");

            AssertQueryEquals("[ a\\- TO a\\+ ]", null, "[a- TO a+]");
            AssertQueryEquals("[ a\\: TO a\\~ ]", null, "[a: TO a~]");
            AssertQueryEquals("[ a\\\\ TO a\\* ]", null, "[a\\ TO a*]");

            AssertQueryEquals("[\"c\\:\\\\temp\\\\\\~foo0.txt\" TO \"c\\:\\\\temp\\\\\\~foo9.txt\"]", a,
                              "[c:\\temp\\~foo0.txt TO c:\\temp\\~foo9.txt]");

            AssertQueryEquals("a\\\\\\+b", a, "a\\+b");

            AssertQueryEquals("a \\\"b c\\\" d", a, "a \"b c\" d");
            AssertQueryEquals("\"a \\\"b c\\\" d\"", a, "\"a \"b c\" d\"");
            AssertQueryEquals("\"a \\+b c d\"", a, "\"a +b c d\"");

            AssertQueryEquals("c\\:\\\\temp\\\\\\~foo.txt", a, "c:\\temp\\~foo.txt");

            AssertParseException("XY\\"); // there must be a character after the escape char

            // test unicode escaping
            AssertQueryEquals("a\\u0062c", a, "abc");
            AssertQueryEquals("XY\\u005a", a, "XYZ");
            AssertQueryEquals("XY\\u005A", a, "XYZ");
            AssertQueryEquals("\"a \\\\\\u0028\\u0062\\\" c\"", a, "\"a \\(b\" c\"");

            AssertParseException("XY\\u005G");  // test non-hex character in escaped unicode sequence
            AssertParseException("XY\\u005");   // test incomplete escaped unicode sequence

            // Tests bug LUCENE-800
            AssertQueryEquals("(item:\\\\ item:ABCD\\\\)", a, "item:\\ item:ABCD\\");
            AssertParseException("(item:\\\\ item:ABCD\\\\))"); // unmatched closing paranthesis 
            AssertQueryEquals("\\*", a, "*");
            AssertQueryEquals("\\\\", a, "\\");  // escaped backslash

            AssertParseException("\\"); // a backslash must always be escaped

            // LUCENE-1189
            AssertQueryEquals("(\"a\\\\\") or (\"b\")", a, "a\\ or b");
        }

        [Test]
        public virtual void TestEscapedVsQuestionMarkAsWildcard()
        {
            Analyzer a = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false);
            AssertQueryEquals("a:b\\-?c", a, "a:b\\-?c");
            AssertQueryEquals("a:b\\+?c", a, "a:b\\+?c");
            AssertQueryEquals("a:b\\:?c", a, "a:b\\:?c");

            AssertQueryEquals("a:b\\\\?c", a, "a:b\\\\?c");
        }

        [Test]
        public virtual void TestQueryStringEscaping()
        {
            Analyzer a = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false);

            AssertEscapedQueryEquals("a-b:c", a, "a\\-b\\:c");
            AssertEscapedQueryEquals("a+b:c", a, "a\\+b\\:c");
            AssertEscapedQueryEquals("a:b:c", a, "a\\:b\\:c");
            AssertEscapedQueryEquals("a\\b:c", a, "a\\\\b\\:c");

            AssertEscapedQueryEquals("a:b-c", a, "a\\:b\\-c");
            AssertEscapedQueryEquals("a:b+c", a, "a\\:b\\+c");
            AssertEscapedQueryEquals("a:b:c", a, "a\\:b\\:c");
            AssertEscapedQueryEquals("a:b\\c", a, "a\\:b\\\\c");

            AssertEscapedQueryEquals("a:b-c*", a, "a\\:b\\-c\\*");
            AssertEscapedQueryEquals("a:b+c*", a, "a\\:b\\+c\\*");
            AssertEscapedQueryEquals("a:b:c*", a, "a\\:b\\:c\\*");

            AssertEscapedQueryEquals("a:b\\\\c*", a, "a\\:b\\\\\\\\c\\*");

            AssertEscapedQueryEquals("a:b-?c", a, "a\\:b\\-\\?c");
            AssertEscapedQueryEquals("a:b+?c", a, "a\\:b\\+\\?c");
            AssertEscapedQueryEquals("a:b:?c", a, "a\\:b\\:\\?c");

            AssertEscapedQueryEquals("a:b?c", a, "a\\:b\\?c");

            AssertEscapedQueryEquals("a:b-c~", a, "a\\:b\\-c\\~");
            AssertEscapedQueryEquals("a:b+c~", a, "a\\:b\\+c\\~");
            AssertEscapedQueryEquals("a:b:c~", a, "a\\:b\\:c\\~");
            AssertEscapedQueryEquals("a:b\\c~", a, "a\\:b\\\\c\\~");

            AssertEscapedQueryEquals("[ a - TO a+ ]", null, "\\[ a \\- TO a\\+ \\]");
            AssertEscapedQueryEquals("[ a : TO a~ ]", null, "\\[ a \\: TO a\\~ \\]");
            AssertEscapedQueryEquals("[ a\\ TO a* ]", null, "\\[ a\\\\ TO a\\* \\]");

            // LUCENE-881
            AssertEscapedQueryEquals("|| abc ||", a, "\\|\\| abc \\|\\|");
            AssertEscapedQueryEquals("&& abc &&", a, "\\&\\& abc \\&\\&");
        }

        [Test]
        public virtual void TestTabNewlineCarriageReturn()
        {
            AssertQueryEqualsDOA("+weltbank +worlbank", null,
              "+weltbank +worlbank");

            AssertQueryEqualsDOA("+weltbank\n+worlbank", null,
              "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \n+worlbank", null,
              "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \n +worlbank", null,
              "+weltbank +worlbank");

            AssertQueryEqualsDOA("+weltbank\r+worlbank", null,
              "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \r+worlbank", null,
              "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \r +worlbank", null,
              "+weltbank +worlbank");

            AssertQueryEqualsDOA("+weltbank\r\n+worlbank", null,
              "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \r\n+worlbank", null,
              "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \r\n +worlbank", null,
              "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \r \n +worlbank", null,
              "+weltbank +worlbank");

            AssertQueryEqualsDOA("+weltbank\t+worlbank", null,
              "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \t+worlbank", null,
              "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \t +worlbank", null,
              "+weltbank +worlbank");
        }

        [Test]
        public virtual void TestSimpleDAO()
        {
            AssertQueryEqualsDOA("term term term", null, "+term +term +term");
            AssertQueryEqualsDOA("term +term term", null, "+term +term +term");
            AssertQueryEqualsDOA("term term +term", null, "+term +term +term");
            AssertQueryEqualsDOA("term +term +term", null, "+term +term +term");
            AssertQueryEqualsDOA("-term term term", null, "-term +term +term");
        }

        [Test]
        public virtual void TestBoost()
        {
            CharacterRunAutomaton stopWords = new CharacterRunAutomaton(BasicAutomata.MakeString("on"));
            Analyzer oneStopAnalyzer = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, stopWords);
            ICommonQueryParserConfiguration qp = GetParserConfig(oneStopAnalyzer);
            Query q = GetQuery("on^1.0", qp);
            assertNotNull(q);
            q = GetQuery("\"hello\"^2.0", qp);
            assertNotNull(q);
            assertEquals(q.Boost, (float)2.0, (float)0.5);
            q = GetQuery("hello^2.0", qp);
            assertNotNull(q);
            assertEquals(q.Boost, (float)2.0, (float)0.5);
            q = GetQuery("\"on\"^1.0", qp);
            assertNotNull(q);

            Analyzer a2 = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
            ICommonQueryParserConfiguration qp2 = GetParserConfig(a2);
            q = GetQuery("the^3", qp2);
            // "the" is a stop word so the result is an empty query:
            assertNotNull(q);
            assertEquals("", q.toString());
            assertEquals(1.0f, q.Boost, 0.01f);
        }

        public void AssertParseException(string queryString)
        {
            try
            {
                GetQuery(queryString);
            }
            catch (Exception expected)
            {
                if (IsQueryParserException(expected))
                {
                    return;
                }
            }
            fail("ParseException expected, not thrown");
        }

        public void AssertParseException(string queryString, Analyzer a)
        {
            try
            {
                GetQuery(queryString, a);
            }
            catch (Exception expected)
            {
                if (IsQueryParserException(expected))
                {
                    return;
                }
            }
            fail("ParseException expected, not thrown");
        }

        [Test]
        public virtual void TestException()
        {
            AssertParseException("\"some phrase");
            AssertParseException("(foo bar");
            AssertParseException("foo bar))");
            AssertParseException("field:term:with:colon some more terms");
            AssertParseException("(sub query)^5.0^2.0 plus more");
            AssertParseException("secret AND illegal) AND access:confidential");
        }

        [Test]
        public virtual void TestBooleanQuery()
        {
            BooleanQuery.MaxClauseCount = (2);
            Analyzer purWhitespaceAnalyzer = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false);
            AssertParseException("one two three", purWhitespaceAnalyzer);
        }

        [Test]
        public virtual void TestPrecedence()
        {
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false));
            Query query1 = GetQuery("A AND B OR C AND D", qp);
            Query query2 = GetQuery("+A +B +C +D", qp);
            assertEquals(query1, query2);
        }

        // LUCENETODO: convert this from DateField to DateUtil
        //  public void testLocalDateFormat() throws IOException, ParseException {
        //    Directory ramDir = newDirectory();
        //    IndexWriter iw = new IndexWriter(ramDir, newIndexWriterConfig( TEST_VERSION_CURRENT, new MockAnalyzer(random, MockTokenizer.WHITESPACE, false)));
        //    addDateDoc("a", 2005, 12, 2, 10, 15, 33, iw);
        //    addDateDoc("b", 2005, 12, 4, 22, 15, 00, iw);
        //    iw.close();
        //    IndexSearcher is = new IndexSearcher(ramDir, true);
        //    assertHits(1, "[12/1/2005 TO 12/3/2005]", is);
        //    assertHits(2, "[12/1/2005 TO 12/4/2005]", is);
        //    assertHits(1, "[12/3/2005 TO 12/4/2005]", is);
        //    assertHits(1, "{12/1/2005 TO 12/3/2005}", is);
        //    assertHits(1, "{12/1/2005 TO 12/4/2005}", is);
        //    assertHits(0, "{12/3/2005 TO 12/4/2005}", is);
        //    is.close();
        //    ramDir.close();
        //  }
        //
        //  private void addDateDoc(String content, int year, int month,
        //                          int day, int hour, int minute, int second, IndexWriter iw) throws IOException {
        //    Document d = new Document();
        //    d.add(newField("f", content, Field.Store.YES, Field.Index.ANALYZED));
        //    Calendar cal = Calendar.getInstance(Locale.ENGLISH);
        //    cal.set(year, month - 1, day, hour, minute, second);
        //    d.add(newField("date", DateField.dateToString(cal.getTime()), Field.Store.YES, Field.Index.NOT_ANALYZED));
        //    iw.addDocument(d);
        //  }

        public abstract void TestStarParsing();

        [Test]
        public virtual void TestEscapedWildcard()
        {
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false));
            WildcardQuery q = new WildcardQuery(new Term("field", "foo\\?ba?r"));
            assertEquals(q, GetQuery("foo\\?ba?r", qp));
        }

        [Test]
        public virtual void TestRegexps()
        {
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false));
            RegexpQuery q = new RegexpQuery(new Term("field", "[a-z][123]"));
            assertEquals(q, GetQuery("/[a-z][123]/", qp));
            qp.LowercaseExpandedTerms = (true);
            assertEquals(q, GetQuery("/[A-Z][123]/", qp));
            q.Boost = (0.5f);
            assertEquals(q, GetQuery("/[A-Z][123]/^0.5", qp));
            qp.MultiTermRewriteMethod=(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
            q.SetRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
            assertTrue(GetQuery("/[A-Z][123]/^0.5", qp) is RegexpQuery);
            assertEquals(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE, ((RegexpQuery)GetQuery("/[A-Z][123]/^0.5", qp)).GetRewriteMethod());
            assertEquals(q, GetQuery("/[A-Z][123]/^0.5", qp));
            qp.MultiTermRewriteMethod=(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);

            Query escaped = new RegexpQuery(new Term("field", "[a-z]\\/[123]"));
            assertEquals(escaped, GetQuery("/[a-z]\\/[123]/", qp));
            Query escaped2 = new RegexpQuery(new Term("field", "[a-z]\\*[123]"));
            assertEquals(escaped2, GetQuery("/[a-z]\\*[123]/", qp));

            BooleanQuery complex = new BooleanQuery();
            complex.Add(new RegexpQuery(new Term("field", "[a-z]\\/[123]")), BooleanClause.Occur.MUST);
            complex.Add(new TermQuery(new Term("path", "/etc/init.d/")), BooleanClause.Occur.MUST);
            complex.Add(new TermQuery(new Term("field", "/etc/init[.]d/lucene/")), BooleanClause.Occur.SHOULD);
            assertEquals(complex, GetQuery("/[a-z]\\/[123]/ AND path:\"/etc/init.d/\" OR \"/etc\\/init\\[.\\]d/lucene/\" ", qp));

            Query re = new RegexpQuery(new Term("field", "http.*"));
            assertEquals(re, GetQuery("field:/http.*/", qp));
            assertEquals(re, GetQuery("/http.*/", qp));

            re = new RegexpQuery(new Term("field", "http~0.5"));
            assertEquals(re, GetQuery("field:/http~0.5/", qp));
            assertEquals(re, GetQuery("/http~0.5/", qp));

            re = new RegexpQuery(new Term("field", "boo"));
            assertEquals(re, GetQuery("field:/boo/", qp));
            assertEquals(re, GetQuery("/boo/", qp));

            assertEquals(new TermQuery(new Term("field", "/boo/")), GetQuery("\"/boo/\"", qp));
            assertEquals(new TermQuery(new Term("field", "/boo/")), GetQuery("\\/boo\\/", qp));

            BooleanQuery two = new BooleanQuery();
            two.Add(new RegexpQuery(new Term("field", "foo")), BooleanClause.Occur.SHOULD);
            two.Add(new RegexpQuery(new Term("field", "bar")), BooleanClause.Occur.SHOULD);
            assertEquals(two, GetQuery("field:/foo/ field:/bar/", qp));
            assertEquals(two, GetQuery("/foo/ /bar/", qp));
        }

        [Test]
        public virtual void TestStopwords()
        {
            CharacterRunAutomaton stopSet = new CharacterRunAutomaton(new RegExp("the|foo").ToAutomaton());
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, stopSet));
            Query result = GetQuery("field:the OR field:foo", qp);
            assertNotNull("result is null and it shouldn't be", result);
            assertTrue("result is not a BooleanQuery", result is BooleanQuery);
            assertTrue(((BooleanQuery)result).Clauses.Length + " does not equal: " + 0, ((BooleanQuery)result).Clauses.Length == 0);
            result = GetQuery("field:woo OR field:the", qp);
            assertNotNull("result is null and it shouldn't be", result);
            assertTrue("result is not a TermQuery", result is TermQuery);
            result = GetQuery("(fieldX:xxxxx OR fieldy:xxxxxxxx)^2 AND (fieldx:the OR fieldy:foo)", qp);
            assertNotNull("result is null and it shouldn't be", result);
            assertTrue("result is not a BooleanQuery", result is BooleanQuery);
            if (VERBOSE) Console.WriteLine("Result: " + result);
            assertTrue(((BooleanQuery)result).Clauses.Length + " does not equal: " + 2, ((BooleanQuery)result).Clauses.Length == 2);
        }

        [Test]
        public virtual void TestPositionIncrement()
        {
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET));
            qp.EnablePositionIncrements = (true);
            String qtxt = "\"the words in poisitions pos02578 are stopped in this phrasequery\"";
            //               0         2                      5           7  8
            int[] expectedPositions = { 1, 3, 4, 6, 9 };
            PhraseQuery pq = (PhraseQuery)GetQuery(qtxt, qp);
            //System.out.println("Query text: "+qtxt);
            //System.out.println("Result: "+pq);
            Term[] t = pq.Terms;
            int[] pos = pq.Positions;
            for (int i = 0; i < t.Length; i++)
            {
                //System.out.println(i+". "+t[i]+"  pos: "+pos[i]);
                assertEquals("term " + i + " = " + t[i] + " has wrong term-position!", expectedPositions[i], pos[i]);
            }
        }

        [Test]
        public virtual void TestMatchAllDocs()
        {
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false));
            assertEquals(new MatchAllDocsQuery(), GetQuery("*:*", qp));
            assertEquals(new MatchAllDocsQuery(), GetQuery("(*:*)", qp));
            BooleanQuery bq = (BooleanQuery)GetQuery("+*:* -*:*", qp);
            assertTrue(bq.Clauses[0].Query is MatchAllDocsQuery);
            assertTrue(bq.Clauses[1].Query is MatchAllDocsQuery);
        }

        private void AssertHits(int expected, String query, IndexSearcher @is)
        {
            string oldDefaultField = DefaultField;
            DefaultField = "date";
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false));
            qp.Locale = new CultureInfo("en");
            Query q = GetQuery(query, qp);
            ScoreDoc[] hits = @is.Search(q, null, 1000).ScoreDocs;
            assertEquals(expected, hits.Length);
            DefaultField = oldDefaultField;
        }

        public override void TearDown()
        {
            BooleanQuery.MaxClauseCount = originalMaxClauses;
            base.TearDown();
        }

        // LUCENE-2002: make sure defaults for StandardAnalyzer's
        // enableStopPositionIncr & QueryParser's enablePosIncr
        // "match"
        [Test]
        public virtual void TestPositionIncrements()
        {
            using (Directory dir = NewDirectory())
            {
                Analyzer a = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
                using (IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, a)))
                {
                    Document doc = new Document();
                    doc.Add(NewTextField("field", "the wizard of ozzy", Field.Store.NO));
                    w.AddDocument(doc);
                    using (IndexReader r = DirectoryReader.Open(w, true))
                    {
                        IndexSearcher s = NewSearcher(r);

                        Query q = GetQuery("\"wizard of ozzy\"", a);
                        assertEquals(1, s.Search(q, 1).TotalHits);
                    }
                }
            }
        }

        /// <summary>
        /// adds synonym of "dog" for "dogs".
        /// </summary>
        protected class MockSynonymFilter : TokenFilter
        {
            ICharTermAttribute termAtt;
            IPositionIncrementAttribute posIncAtt;
            bool addSynonym = false;

            public MockSynonymFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                if (addSynonym)
                { // inject our synonym
                    ClearAttributes();
                    termAtt.SetEmpty().Append("dog");
                    posIncAtt.PositionIncrement = (0);
                    addSynonym = false;
                    return true;
                }

                if (input.IncrementToken())
                {
                    addSynonym = termAtt.toString().equals("dogs");
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// whitespace+lowercase analyzer without synonyms
        /// </summary>
        protected class Analyzer1 : Analyzer
        {
            public Analyzer1()
            { }

            public override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
                return new TokenStreamComponents(tokenizer, new MockSynonymFilter(tokenizer));
            }
        }

        /// <summary>
        /// whitespace+lowercase analyzer without synonyms
        /// </summary>
        protected class Analyzer2 : Analyzer
        {
            public Analyzer2()
            { }

            public override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, true));
            }
        }

        public abstract void TestNewFieldQuery();

        /// <summary>
        /// Mock collation analyzer: indexes terms as "collated" + term
        /// </summary>
        private class MockCollationFilter : TokenFilter
        {
            private ICharTermAttribute termAtt;

            public MockCollationFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                if (input.IncrementToken())
                {
                    string term = termAtt.toString();
                    termAtt.SetEmpty().Append("collated").Append(term);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private class MockCollationAnalyzer : Analyzer
        {
            public override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
                return new TokenStreamComponents(tokenizer, new MockCollationFilter(tokenizer));
            }
        }

        [Test]
        public virtual void TestCollatedRange()
        {
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockCollationAnalyzer());
            SetAnalyzeRangeTerms(qp, true);
            Query expected = TermRangeQuery.NewStringRange(DefaultField, "collatedabc", "collateddef", true, true);
            Query actual = GetQuery("[abc TO def]", qp);
            assertEquals(expected, actual);
        }

        [Test]
        public virtual void TestDistanceAsEditsParsing()
        {
            FuzzyQuery q = (FuzzyQuery)GetQuery("foobar~2", new MockAnalyzer(Random()));
            assertEquals(2, q.MaxEdits);
        }

        [Test]
        public virtual void TestPhraseQueryToString()
        {
            Analyzer analyzer = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
            ICommonQueryParserConfiguration qp = GetParserConfig(analyzer);
            qp.EnablePositionIncrements = (true);
            PhraseQuery q = (PhraseQuery)GetQuery("\"this hi this is a test is\"", qp);
            assertEquals("field:\"? hi ? ? ? test\"", q.toString());
        }

        [Test]
        public virtual void TestParseWildcardAndPhraseQueries()
        {
            string field = "content";
            string oldDefaultField = DefaultField;
            DefaultField = (field);
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random()));
            qp.AllowLeadingWildcard=(true);

            string[][] prefixQueries = new string[3][] {
                new string[] {"a*", "ab*", "abc*",},
                new string[] {"h*", "hi*", "hij*", "\\\\7*"},
                new string[] {"o*", "op*", "opq*", "\\\\\\\\*"},
            };

            string[][] wildcardQueries = new string[3][] {
                new string[] {"*a*", "*ab*", "*abc**", "ab*e*", "*g?", "*f?1", "abc**"},
                new string[] {"*h*", "*hi*", "*hij**", "hi*k*", "*n?", "*m?1", "hij**"},
                new string[] {"*o*", "*op*", "*opq**", "op*q*", "*u?", "*t?1", "opq**"},
            };

            // test queries that must be prefix queries
            for (int i = 0; i < prefixQueries.Length; i++)
            {
                for (int j = 0; j < prefixQueries[i].Length; j++)
                {
                    string queryString = prefixQueries[i][j];
                    Query q = GetQuery(queryString, qp);
                    assertEquals(typeof(PrefixQuery), q.GetType());
                }
            }

            // test queries that must be wildcard queries
            for (int i = 0; i < wildcardQueries.Length; i++)
            {
                for (int j = 0; j < wildcardQueries[i].Length; j++)
                {
                    string qtxt = wildcardQueries[i][j];
                    Query q = GetQuery(qtxt, qp);
                    assertEquals(typeof(WildcardQuery), q.GetType());
                }
            }
            DefaultField = (oldDefaultField);
        }

        [Test]
        public virtual void TestPhraseQueryPositionIncrements()
        {
            CharacterRunAutomaton stopStopList =
            new CharacterRunAutomaton(new RegExp("[sS][tT][oO][pP]").ToAutomaton());

            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false, stopStopList));

            qp = GetParserConfig(
                                 new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false, stopStopList));
            qp.EnablePositionIncrements=(true);

            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term("field", "1"));
            phraseQuery.Add(new Term("field", "2"), 2);
            assertEquals(phraseQuery, GetQuery("\"1 stop 2\"", qp));
        }

        [Test]
        public virtual void TestMatchAllQueryParsing()
        {
            // test simple parsing of MatchAllDocsQuery
            string oldDefaultField = DefaultField;
            DefaultField = ("key");
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random()));
            assertEquals(new MatchAllDocsQuery(), GetQuery(new MatchAllDocsQuery().toString(), qp));

            // test parsing with non-default boost
            MatchAllDocsQuery query = new MatchAllDocsQuery();
            query.Boost = (2.3f);
            assertEquals(query, GetQuery(query.toString(), qp));
            DefaultField = (oldDefaultField);
        }

        [Test]
        public virtual void TestNestedAndClausesFoo()
        {
            string query = "(field1:[1 TO *] AND field1:[* TO 2]) AND field2:(z)";
            BooleanQuery q = new BooleanQuery();
            BooleanQuery bq = new BooleanQuery();
            bq.Add(TermRangeQuery.NewStringRange("field1", "1", null, true, true), BooleanClause.Occur.MUST);
            bq.Add(TermRangeQuery.NewStringRange("field1", null, "2", true, true), BooleanClause.Occur.MUST);
            q.Add(bq, BooleanClause.Occur.MUST);
            q.Add(new TermQuery(new Term("field2", "z")), BooleanClause.Occur.MUST);
            assertEquals(q, GetQuery(query, new MockAnalyzer(Random())));
        }
    }
}
