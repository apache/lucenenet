using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Precedence
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
    /// This test case tests <see cref="PrecedenceQueryParser"/>.
    /// <para/>
    /// It contains all tests from <see cref="Util.QueryParserTestBase"/>
    /// with some adjusted to fit the precedence requirement, plus some precedence test cases.
    /// </summary>
    /// <see cref="Util.QueryParserTestBase"/>
    //TODO: refactor this to actually extend that class, overriding the tests
    //that it adjusts to fit the precedence requirement, adding its extra tests.
    public class TestPrecedenceQueryParser : LuceneTestCase
    {
        public static Analyzer qpAnalyzer;

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();
            qpAnalyzer = new QPTestAnalyzer();
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            qpAnalyzer = null;
            base.AfterClass();
        }

        public sealed class QPTestFilter : TokenFilter
        {
            /**
             * Filter which discards the token 'stop' and which expands the token
             * 'phrase' into 'phrase1 phrase2'
             */
            public QPTestFilter(TokenStream @in)
                        : base(@in)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
            }

            private bool inPhrase = false;

            private int savedStart = 0;
            private int savedEnd = 0;

            private readonly ICharTermAttribute termAtt;

            private readonly IOffsetAttribute offsetAtt;


            public override bool IncrementToken()
            {
                if (inPhrase)
                {
                    inPhrase = false;
                    termAtt.SetEmpty().Append("phrase2");
                    offsetAtt.SetOffset(savedStart, savedEnd);
                    return true;
                }
                else
                    while (m_input.IncrementToken())
                        if (termAtt.toString().Equals("phrase", StringComparison.Ordinal))
                        {
                            inPhrase = true;
                            savedStart = offsetAtt.StartOffset;
                            savedEnd = offsetAtt.EndOffset;
                            termAtt.SetEmpty().Append("phrase1");
                            offsetAtt.SetOffset(savedStart, savedEnd);
                            return true;
                        }
                        else if (!termAtt.toString().Equals("stop", StringComparison.Ordinal))
                            return true;
                return false;
            }


            public override void Reset()
            {
                base.Reset();
                this.inPhrase = false;
                this.savedStart = 0;
                this.savedEnd = 0;
            }
        }

        public sealed class QPTestAnalyzer : Analyzer
        {

            /** Filters MockTokenizer with StopFilter. */

            protected internal override sealed TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(tokenizer, new QPTestFilter(tokenizer));
            }
        }

        private int originalMaxClauses;


        public override void SetUp()
        {
            base.SetUp();
            originalMaxClauses = BooleanQuery.MaxClauseCount;
        }

        public PrecedenceQueryParser GetParser(Analyzer a)
        {
            if (a is null)
                a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            PrecedenceQueryParser qp = new PrecedenceQueryParser();
            qp.Analyzer = (a);
            qp.DefaultOperator = (StandardQueryConfigHandler.Operator.OR);
            return qp;
        }

        public Query GetQuery(string query, Analyzer a)
        {
            return GetParser(a).Parse(query, "field");
        }

        public void assertQueryEquals(string query, Analyzer a, string result)
        {
            Query q = GetQuery(query, a);
            String s = q.ToString("field");
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("Query /" + query + "/ yielded /" + s + "/, expecting /" + result
                    + "/");
            }
        }

        public void assertWildcardQueryEquals(String query, bool lowercase,
            String result)
        {
            PrecedenceQueryParser qp = GetParser(null);
            qp.LowercaseExpandedTerms = (lowercase);
            Query q = qp.Parse(query, "field");
            String s = q.ToString("field");
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("WildcardQuery /" + query + "/ yielded /" + s + "/, expecting /"
                    + result + "/");
            }
        }

        public void assertWildcardQueryEquals(String query, String result)
        {
            PrecedenceQueryParser qp = GetParser(null);
            Query q = qp.Parse(query, "field");
            String s = q.ToString("field");
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("WildcardQuery /" + query + "/ yielded /" + s + "/, expecting /"
                    + result + "/");
            }
        }

        public Query getQueryDOA(String query, Analyzer a)
        {
            if (a is null)
                a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            PrecedenceQueryParser qp = new PrecedenceQueryParser();
            qp.Analyzer = (a);
            qp.DefaultOperator = (StandardQueryConfigHandler.Operator.AND);
            return qp.Parse(query, "field");
        }

        public void assertQueryEqualsDOA(String query, Analyzer a, String result)
        {
            Query q = getQueryDOA(query, a);
            String s = q.ToString("field");
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("Query /" + query + "/ yielded /" + s + "/, expecting /" + result
                    + "/");
            }
        }

        [Test]
        public void TestSimple()
        {
            assertQueryEquals("term term term", null, "term term term");
            assertQueryEquals("türm term term", null, "türm term term");
            assertQueryEquals("ümlaut", null, "ümlaut");

            assertQueryEquals("a AND b", null, "+a +b");
            assertQueryEquals("(a AND b)", null, "+a +b");
            assertQueryEquals("c OR (a AND b)", null, "c (+a +b)");
            assertQueryEquals("a AND NOT b", null, "+a -b");
            assertQueryEquals("a AND -b", null, "+a -b");
            assertQueryEquals("a AND !b", null, "+a -b");
            assertQueryEquals("a && b", null, "+a +b");
            assertQueryEquals("a && ! b", null, "+a -b");

            assertQueryEquals("a OR b", null, "a b");
            assertQueryEquals("a || b", null, "a b");

            assertQueryEquals("+term -term term", null, "+term -term term");
            assertQueryEquals("foo:term AND field:anotherTerm", null,
                "+foo:term +anotherterm");
            assertQueryEquals("term AND \"phrase phrase\"", null,
                "+term +\"phrase phrase\"");
            assertQueryEquals("\"hello there\"", null, "\"hello there\"");
            assertTrue(GetQuery("a AND b", null) is BooleanQuery);
            assertTrue(GetQuery("hello", null) is TermQuery);
            assertTrue(GetQuery("\"hello there\"", null) is PhraseQuery);

            assertQueryEquals("germ term^2.0", null, "germ term^2.0");
            assertQueryEquals("(term)^2.0", null, "term^2.0");
            assertQueryEquals("(germ term)^2.0", null, "(germ term)^2.0");
            assertQueryEquals("term^2.0", null, "term^2.0");
            assertQueryEquals("term^2", null, "term^2.0");
            assertQueryEquals("\"germ term\"^2.0", null, "\"germ term\"^2.0");
            assertQueryEquals("\"term germ\"^2", null, "\"term germ\"^2.0");

            assertQueryEquals("(foo OR bar) AND (baz OR boo)", null,
                "+(foo bar) +(baz boo)");
            assertQueryEquals("((a OR b) AND NOT c) OR d", null, "(+(a b) -c) d");
            assertQueryEquals("+(apple \"steve jobs\") -(foo bar baz)", null,
                "+(apple \"steve jobs\") -(foo bar baz)");
            assertQueryEquals("+title:(dog OR cat) -author:\"bob dole\"", null,
                "+(title:dog title:cat) -author:\"bob dole\"");

            PrecedenceQueryParser qp = new PrecedenceQueryParser();
            qp.Analyzer = (new MockAnalyzer(Random));
            // make sure OR is the default:
            assertEquals(StandardQueryConfigHandler.Operator.OR, qp.DefaultOperator);
            qp.DefaultOperator = (StandardQueryConfigHandler.Operator.AND);
            assertEquals(StandardQueryConfigHandler.Operator.AND, qp.DefaultOperator);
            qp.DefaultOperator = (StandardQueryConfigHandler.Operator.OR);
            assertEquals(StandardQueryConfigHandler.Operator.OR, qp.DefaultOperator);

            assertQueryEquals("a OR !b", null, "a -b");
            assertQueryEquals("a OR ! b", null, "a -b");
            assertQueryEquals("a OR -b", null, "a -b");
        }

        [Test]
        public void TestPunct()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            assertQueryEquals("a&b", a, "a&b");
            assertQueryEquals("a&&b", a, "a&&b");
            assertQueryEquals(".NET", a, ".NET");
        }

        [Test]
        public void TestSlop()
        {
            assertQueryEquals("\"term germ\"~2", null, "\"term germ\"~2");
            assertQueryEquals("\"term germ\"~2 flork", null, "\"term germ\"~2 flork");
            assertQueryEquals("\"term\"~2", null, "term");
            assertQueryEquals("\" \"~2 germ", null, "germ");
            assertQueryEquals("\"term germ\"~2^2", null, "\"term germ\"~2^2.0");
        }

        [Test]
        public void TestNumber()
        {
            // The numbers go away because SimpleAnalzyer ignores them
            assertQueryEquals("3", null, "");
            assertQueryEquals("term 1.0 1 2", null, "term");
            assertQueryEquals("term term1 term2", null, "term term term");

            Analyzer a = new MockAnalyzer(Random);
            assertQueryEquals("3", a, "3");
            assertQueryEquals("term 1.0 1 2", a, "term 1.0 1 2");
            assertQueryEquals("term term1 term2", a, "term term1 term2");
        }

        [Test]
        public void TestWildcard()
        {
            assertQueryEquals("term*", null, "term*");
            assertQueryEquals("term*^2", null, "term*^2.0");
            assertQueryEquals("term~", null, "term~2");
            assertQueryEquals("term~0.7", null, "term~1");
            assertQueryEquals("term~^3", null, "term~2^3.0");
            assertQueryEquals("term^3~", null, "term~2^3.0");
            assertQueryEquals("term*germ", null, "term*germ");
            assertQueryEquals("term*germ^3", null, "term*germ^3.0");

            assertTrue(GetQuery("term*", null) is PrefixQuery);
            assertTrue(GetQuery("term*^2", null) is PrefixQuery);
            assertTrue(GetQuery("term~", null) is FuzzyQuery);
            assertTrue(GetQuery("term~0.7", null) is FuzzyQuery);
            FuzzyQuery fq = (FuzzyQuery)GetQuery("term~0.7", null);
            assertEquals(1, fq.MaxEdits);
            assertEquals(FuzzyQuery.DefaultPrefixLength, fq.PrefixLength);
            fq = (FuzzyQuery)GetQuery("term~", null);
            assertEquals(2, fq.MaxEdits);
            assertEquals(FuzzyQuery.DefaultPrefixLength, fq.PrefixLength);
            try
            {
                GetQuery("term~1.1", null); // value > 1, throws exception
                fail();
            }
#pragma warning disable 168
            catch (Lucene.Net.QueryParsers.Flexible.Standard.Parser.ParseException pe)
#pragma warning restore 168
            {
                // expected exception
            }
            assertTrue(GetQuery("term*germ", null) is WildcardQuery);

            /*
             * Tests to see that wild card terms are (or are not) properly lower-cased
             * with propery parser configuration
             */
            // First prefix queries:
            // by default, convert to lowercase:
            assertWildcardQueryEquals("Term*", true, "term*");
            // explicitly set lowercase:
            assertWildcardQueryEquals("term*", true, "term*");
            assertWildcardQueryEquals("Term*", true, "term*");
            assertWildcardQueryEquals("TERM*", true, "term*");
            // explicitly disable lowercase conversion:
            assertWildcardQueryEquals("term*", false, "term*");
            assertWildcardQueryEquals("Term*", false, "Term*");
            assertWildcardQueryEquals("TERM*", false, "TERM*");
            // Then 'full' wildcard queries:
            // by default, convert to lowercase:
            assertWildcardQueryEquals("Te?m", "te?m");
            // explicitly set lowercase:
            assertWildcardQueryEquals("te?m", true, "te?m");
            assertWildcardQueryEquals("Te?m", true, "te?m");
            assertWildcardQueryEquals("TE?M", true, "te?m");
            assertWildcardQueryEquals("Te?m*gerM", true, "te?m*germ");
            // explicitly disable lowercase conversion:
            assertWildcardQueryEquals("te?m", false, "te?m");
            assertWildcardQueryEquals("Te?m", false, "Te?m");
            assertWildcardQueryEquals("TE?M", false, "TE?M");
            assertWildcardQueryEquals("Te?m*gerM", false, "Te?m*gerM");
            // Fuzzy queries:
            assertWildcardQueryEquals("Term~", "term~2");
            assertWildcardQueryEquals("Term~", true, "term~2");
            assertWildcardQueryEquals("Term~", false, "Term~2");
            // Range queries:
            assertWildcardQueryEquals("[A TO C]", "[a TO c]");
            assertWildcardQueryEquals("[A TO C]", true, "[a TO c]");
            assertWildcardQueryEquals("[A TO C]", false, "[A TO C]");
        }

        [Test]
        public void TestQPA()
        {
            assertQueryEquals("term term term", qpAnalyzer, "term term term");
            assertQueryEquals("term +stop term", qpAnalyzer, "term term");
            assertQueryEquals("term -stop term", qpAnalyzer, "term term");
            assertQueryEquals("drop AND stop AND roll", qpAnalyzer, "+drop +roll");
            assertQueryEquals("term phrase term", qpAnalyzer,
                "term (phrase1 phrase2) term");
            // note the parens in this next assertion differ from the original
            // QueryParser behavior
            assertQueryEquals("term AND NOT phrase term", qpAnalyzer,
                "(+term -(phrase1 phrase2)) term");
            assertQueryEquals("stop", qpAnalyzer, "");
            assertQueryEquals("stop OR stop AND stop", qpAnalyzer, "");
            assertTrue(GetQuery("term term term", qpAnalyzer) is BooleanQuery);
            assertTrue(GetQuery("term +stop", qpAnalyzer) is TermQuery);
        }

        [Test]
        public void TestRange()
        {
            assertQueryEquals("[ a TO z]", null, "[a TO z]");
            assertTrue(GetQuery("[ a TO z]", null) is TermRangeQuery);
            assertQueryEquals("[ a TO z ]", null, "[a TO z]");
            assertQueryEquals("{ a TO z}", null, "{a TO z}");
            assertQueryEquals("{ a TO z }", null, "{a TO z}");
            assertQueryEquals("{ a TO z }^2.0", null, "{a TO z}^2.0");
            assertQueryEquals("[ a TO z] OR bar", null, "[a TO z] bar");
            assertQueryEquals("[ a TO z] AND bar", null, "+[a TO z] +bar");
            assertQueryEquals("( bar blar { a TO z}) ", null, "bar blar {a TO z}");
            assertQueryEquals("gack ( bar blar { a TO z}) ", null,
                "gack (bar blar {a TO z})");
        }

        private String escapeDateString(String s)
        {
            if (s.IndexOf(' ') > -1)
            {
                return "\"" + s + "\"";
            }
            else
            {
                return s;
            }
        }

        public String getDate(String s)
        {
            // we use the default Locale since LuceneTestCase randomizes it
            //DateFormat df = DateFormat.getDateInstance(DateFormat.SHORT, Locale.getDefault());
            //return DateTools.DateToString(df.parse(s), DateResolution.DAY);

            //DateFormat df = DateFormat.getDateInstance(DateFormat.SHORT, Locale.getDefault());
            return DateTools.DateToString(DateTime.Parse(s), DateResolution.DAY);
        }

        private String getLocalizedDate(int year, int month, int day,
            bool extendLastDate)
        {
            //// we use the default Locale/TZ since LuceneTestCase randomizes it
            //DateFormat df = DateFormat.getDateInstance(DateFormat.SHORT, Locale.getDefault());
            //Calendar calendar = new GregorianCalendar(TimeZone.getDefault(), Locale.getDefault());
            //calendar.set(year, month, day);
            //if (extendLastDate)
            //{
            //    calendar.set(Calendar.HOUR_OF_DAY, 23);
            //    calendar.set(Calendar.MINUTE, 59);
            //    calendar.set(Calendar.SECOND, 59);
            //    calendar.set(Calendar.MILLISECOND, 999);
            //}
            //return df.format(calendar.getTime());

            var calendar = new GregorianCalendar(GregorianCalendarTypes.Localized);
            DateTime lastDate = calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            lastDate = TimeZoneInfo.ConvertTime(lastDate, TimeZoneInfo.Local);

            if (extendLastDate)
            {
                lastDate = calendar.AddHours(lastDate, 23);
                lastDate = calendar.AddMinutes(lastDate, 59);
                lastDate = calendar.AddSeconds(lastDate, 59);
                lastDate = calendar.AddMilliseconds(lastDate, 999);
            }

            return lastDate.ToString("d"); //.ToShortDateString();
        }

        [Test]
        public void TestDateRange()
        {
            String startDate = getLocalizedDate(2002, 1, 1, false);
            String endDate = getLocalizedDate(2002, 1, 4, false);
            // we use the default Locale/TZ since LuceneTestCase randomizes it
            //Calendar endDateExpected = new GregorianCalendar(TimeZone.getDefault(), Locale.getDefault());
            //endDateExpected.set(2002, 1, 4, 23, 59, 59);
            //endDateExpected.set(Calendar.MILLISECOND, 999);
            DateTime endDateExpected = new GregorianCalendar().ToDateTime(2002, 1, 4, 23, 59, 59, 999);


            String defaultField = "default";
            String monthField = "month";
            String hourField = "hour";
            PrecedenceQueryParser qp = new PrecedenceQueryParser(new MockAnalyzer(Random));

            IDictionary<string, DateResolution> fieldMap = new JCG.Dictionary<string, DateResolution>();
            // set a field specific date resolution
            fieldMap[monthField] = DateResolution.MONTH;
#pragma warning disable 612, 618
            qp.SetDateResolution(fieldMap);
#pragma warning restore 612, 618

            // set default date resolution to MILLISECOND
            qp.SetDateResolution(DateResolution.MILLISECOND);

            // set second field specific date resolution
            fieldMap[hourField] = DateResolution.HOUR;
#pragma warning disable 612, 618
            qp.SetDateResolution(fieldMap);
#pragma warning restore 612, 618

            // for this field no field specific date resolution has been set,
            // so verify if the default resolution is used
            assertDateRangeQueryEquals(qp, defaultField, startDate, endDate,
                endDateExpected, DateResolution.MILLISECOND);

            // verify if field specific date resolutions are used for these two fields
            assertDateRangeQueryEquals(qp, monthField, startDate, endDate,
                endDateExpected, DateResolution.MONTH);

            assertDateRangeQueryEquals(qp, hourField, startDate, endDate,
                endDateExpected, DateResolution.HOUR);
        }

        /** for testing DateTools support */
        private String getDate(String s, DateResolution resolution)
        {
            // we use the default Locale since LuceneTestCase randomizes it
            //DateFormat df = DateFormat.getDateInstance(DateFormat.SHORT, Locale.getDefault());
            //return getDate(df.parse(s), resolution);
            return getDate(DateTime.ParseExact(s, "d", CultureInfo.CurrentCulture), resolution);
        }

        /** for testing DateTools support */
        private String getDate(DateTime d, DateResolution resolution)
        {
            return DateTools.DateToString(d, resolution);
        }

        public void assertQueryEquals(PrecedenceQueryParser qp, String field, String query,
            String result)
        {
            Query q = qp.Parse(query, field);
            String s = q.ToString(field);
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("Query /" + query + "/ yielded /" + s + "/, expecting /" + result
                    + "/");
            }
        }

        public void assertDateRangeQueryEquals(PrecedenceQueryParser qp, String field,
            String startDate, String endDate, DateTime endDateInclusive,
            DateResolution resolution)
        {
            assertQueryEquals(qp, field, field + ":[" + escapeDateString(startDate)
                + " TO " + escapeDateString(endDate) + "]", "["
                + getDate(startDate, resolution) + " TO "
                + getDate(endDateInclusive, resolution) + "]");
            assertQueryEquals(qp, field, field + ":{" + escapeDateString(startDate)
                + " TO " + escapeDateString(endDate) + "}", "{"
                + getDate(startDate, resolution) + " TO "
                + getDate(endDate, resolution) + "}");
        }

        [Test]
        public void TestEscaped()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);

            assertQueryEquals("a\\-b:c", a, "a-b:c");
            assertQueryEquals("a\\+b:c", a, "a+b:c");
            assertQueryEquals("a\\:b:c", a, "a:b:c");
            assertQueryEquals("a\\\\b:c", a, "a\\b:c");

            assertQueryEquals("a:b\\-c", a, "a:b-c");
            assertQueryEquals("a:b\\+c", a, "a:b+c");
            assertQueryEquals("a:b\\:c", a, "a:b:c");
            assertQueryEquals("a:b\\\\c", a, "a:b\\c");

            assertQueryEquals("a:b\\-c*", a, "a:b-c*");
            assertQueryEquals("a:b\\+c*", a, "a:b+c*");
            assertQueryEquals("a:b\\:c*", a, "a:b:c*");

            assertQueryEquals("a:b\\\\c*", a, "a:b\\c*");

            assertQueryEquals("a:b\\-?c", a, "a:b-?c");
            assertQueryEquals("a:b\\+?c", a, "a:b+?c");
            assertQueryEquals("a:b\\:?c", a, "a:b:?c");

            assertQueryEquals("a:b\\\\?c", a, "a:b\\?c");

            assertQueryEquals("a:b\\-c~", a, "a:b-c~2");
            assertQueryEquals("a:b\\+c~", a, "a:b+c~2");
            assertQueryEquals("a:b\\:c~", a, "a:b:c~2");
            assertQueryEquals("a:b\\\\c~", a, "a:b\\c~2");

            assertQueryEquals("[ a\\- TO a\\+ ]", null, "[a- TO a+]");
            assertQueryEquals("[ a\\: TO a\\~ ]", null, "[a: TO a~]");
            assertQueryEquals("[ a\\\\ TO a\\* ]", null, "[a\\ TO a*]");
        }

        [Test]
        public void TestTabNewlineCarriageReturn()
        {
            assertQueryEqualsDOA("+weltbank +worlbank", null, "+weltbank +worlbank");

            assertQueryEqualsDOA("+weltbank\n+worlbank", null, "+weltbank +worlbank");
            assertQueryEqualsDOA("weltbank \n+worlbank", null, "+weltbank +worlbank");
            assertQueryEqualsDOA("weltbank \n +worlbank", null, "+weltbank +worlbank");

            assertQueryEqualsDOA("+weltbank\r+worlbank", null, "+weltbank +worlbank");
            assertQueryEqualsDOA("weltbank \r+worlbank", null, "+weltbank +worlbank");
            assertQueryEqualsDOA("weltbank \r +worlbank", null, "+weltbank +worlbank");

            assertQueryEqualsDOA("+weltbank\r\n+worlbank", null, "+weltbank +worlbank");
            assertQueryEqualsDOA("weltbank \r\n+worlbank", null, "+weltbank +worlbank");
            assertQueryEqualsDOA("weltbank \r\n +worlbank", null, "+weltbank +worlbank");
            assertQueryEqualsDOA("weltbank \r \n +worlbank", null,
                "+weltbank +worlbank");

            assertQueryEqualsDOA("+weltbank\t+worlbank", null, "+weltbank +worlbank");
            assertQueryEqualsDOA("weltbank \t+worlbank", null, "+weltbank +worlbank");
            assertQueryEqualsDOA("weltbank \t +worlbank", null, "+weltbank +worlbank");
        }

        [Test]
        public void TestSimpleDAO()
        {
            assertQueryEqualsDOA("term term term", null, "+term +term +term");
            assertQueryEqualsDOA("term +term term", null, "+term +term +term");
            assertQueryEqualsDOA("term term +term", null, "+term +term +term");
            assertQueryEqualsDOA("term +term +term", null, "+term +term +term");
            assertQueryEqualsDOA("-term term term", null, "-term +term +term");
        }

        [Test]
        public void TestBoost()
        {
            CharacterRunAutomaton stopSet = new CharacterRunAutomaton(BasicAutomata.MakeString("on"));
            Analyzer oneStopAnalyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, stopSet);

            PrecedenceQueryParser qp = new PrecedenceQueryParser();
            qp.Analyzer = (oneStopAnalyzer);
            Query q = qp.Parse("on^1.0", "field");
            assertNotNull(q);
            q = qp.Parse("\"hello\"^2.0", "field");
            assertNotNull(q);
            assertEquals(q.Boost, (float)2.0, (float)0.5);
            q = qp.Parse("hello^2.0", "field");
            assertNotNull(q);
            assertEquals(q.Boost, (float)2.0, (float)0.5);
            q = qp.Parse("\"on\"^1.0", "field");
            assertNotNull(q);

            q = GetParser(new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET)).Parse("the^3",
                    "field");
            assertNotNull(q);
        }

        [Test]
        public void TestException()
        {
            try
            {
                assertQueryEquals("\"some phrase", null, "abc");
                fail("ParseException expected, not thrown");
            }
#pragma warning disable 168
            catch (QueryNodeParseException expected)
#pragma warning restore 168
            {
            }
        }

        [Test]
        public void TestBooleanQuery()
        {
            BooleanQuery.MaxClauseCount = (2);
            try
            {
                GetParser(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).Parse("one two three", "field");
                fail("ParseException expected due to too many boolean clauses");
            }
#pragma warning disable 168
            catch (QueryNodeException expected)
#pragma warning restore 168
            {
                // too many boolean clauses, so ParseException is expected
            }
        }

        // LUCENE-792
        [Test]
        public void TestNOT()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            assertQueryEquals("NOT foo AND bar", a, "-foo +bar");
        }

        /**
         * This test differs from the original QueryParser, showing how the precedence
         * issue has been corrected.
         */
        [Test]
        public void TestPrecedence()
        {
            PrecedenceQueryParser parser = GetParser(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));
            Query query1 = parser.Parse("A AND B OR C AND D", "field");
            Query query2 = parser.Parse("(A AND B) OR (C AND D)", "field");
            assertEquals(query1, query2);

            query1 = parser.Parse("A OR B C", "field");
            query2 = parser.Parse("(A B) C", "field");
            assertEquals(query1, query2);

            query1 = parser.Parse("A AND B C", "field");
            query2 = parser.Parse("(+A +B) C", "field");
            assertEquals(query1, query2);

            query1 = parser.Parse("A AND NOT B", "field");
            query2 = parser.Parse("+A -B", "field");
            assertEquals(query1, query2);

            query1 = parser.Parse("A OR NOT B", "field");
            query2 = parser.Parse("A -B", "field");
            assertEquals(query1, query2);

            query1 = parser.Parse("A OR NOT B AND C", "field");
            query2 = parser.Parse("A (-B +C)", "field");
            assertEquals(query1, query2);

            parser.DefaultOperator = (StandardQueryConfigHandler.Operator.AND);
            query1 = parser.Parse("A AND B OR C AND D", "field");
            query2 = parser.Parse("(A AND B) OR (C AND D)", "field");
            assertEquals(query1, query2);

            query1 = parser.Parse("A AND B C", "field");
            query2 = parser.Parse("(A B) C", "field");
            assertEquals(query1, query2);

            query1 = parser.Parse("A AND B C", "field");
            query2 = parser.Parse("(+A +B) C", "field");
            assertEquals(query1, query2);

            query1 = parser.Parse("A AND NOT B", "field");
            query2 = parser.Parse("+A -B", "field");
            assertEquals(query1, query2);

            query1 = parser.Parse("A AND NOT B OR C", "field");
            query2 = parser.Parse("(+A -B) OR C", "field");
            assertEquals(query1, query2);

        }


        public override void TearDown()
        {
            BooleanQuery.MaxClauseCount = (originalMaxClauses);
            base.TearDown();
        }
    }
}
