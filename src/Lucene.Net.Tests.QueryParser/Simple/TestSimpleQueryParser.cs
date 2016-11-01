using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.QueryParsers.Simple
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
    /// Tests for <see cref="SimpleQueryParser"/>
    /// </summary>
    [TestFixture]
    public class TestSimpleQueryParser : LuceneTestCase
    {
        /// <summary>
        /// helper to parse a query with whitespace+lowercase analyzer across "field",
        /// with default operator of MUST
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private Query Parse(string text)
        {
            Analyzer analyzer = new MockAnalyzer(Random());
            SimpleQueryParser parser = new SimpleQueryParser(analyzer, "field");
            parser.DefaultOperator = BooleanClause.Occur.MUST;
            return parser.Parse(text);
        }

        /// <summary>
        /// helper to parse a query with whitespace+lowercase analyzer across "field",
        /// with default operator of MUST
        /// </summary>
        /// <param name="text"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private Query Parse(string text, int flags)
        {
            Analyzer analyzer = new MockAnalyzer(Random());
            SimpleQueryParser parser = new SimpleQueryParser(analyzer, new HashMap<string, float>() { { "field", 1f } }, flags);
            parser.DefaultOperator = BooleanClause.Occur.MUST;
            return parser.Parse(text);
        }

        /** test a simple term */
        [Test]
        public virtual void TestTerm()
        {
            Query expected = new TermQuery(new Term("field", "foobar"));

            assertEquals(expected, Parse("foobar"));
        }

        /** test a fuzzy query */
        [Test]
        public virtual void TestFuzzy()
        {
            Query regular = new TermQuery(new Term("field", "foobar"));
            Query expected = new FuzzyQuery(new Term("field", "foobar"), 2);

            assertEquals(expected, Parse("foobar~2"));
            assertEquals(regular, Parse("foobar~"));
            assertEquals(regular, Parse("foobar~a"));
            assertEquals(regular, Parse("foobar~1a"));

            BooleanQuery @bool = new BooleanQuery();
            FuzzyQuery fuzzy = new FuzzyQuery(new Term("field", "foo"), LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE);
            @bool.Add(fuzzy, BooleanClause.Occur.MUST);
            @bool.Add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur.MUST);

            assertEquals(@bool, Parse("foo~" + LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE + 1 + " bar"));
        }

        /** test a simple phrase */
        [Test]
        public virtual void TestPhrase()
        {
            PhraseQuery expected = new PhraseQuery();
            expected.Add(new Term("field", "foo"));
            expected.Add(new Term("field", "bar"));

            assertEquals(expected, Parse("\"foo bar\""));
        }

        /** test a simple phrase with various slop settings */
        [Test]
        public virtual void TestPhraseWithSlop()
        {
            PhraseQuery expectedWithSlop = new PhraseQuery();
            expectedWithSlop.Add(new Term("field", "foo"));
            expectedWithSlop.Add(new Term("field", "bar"));
            expectedWithSlop.Slop = (2);

            assertEquals(expectedWithSlop, Parse("\"foo bar\"~2"));

            PhraseQuery expectedWithMultiDigitSlop = new PhraseQuery();
            expectedWithMultiDigitSlop.Add(new Term("field", "foo"));
            expectedWithMultiDigitSlop.Add(new Term("field", "bar"));
            expectedWithMultiDigitSlop.Slop = (10);

            assertEquals(expectedWithMultiDigitSlop, Parse("\"foo bar\"~10"));

            PhraseQuery expectedNoSlop = new PhraseQuery();
            expectedNoSlop.Add(new Term("field", "foo"));
            expectedNoSlop.Add(new Term("field", "bar"));

            assertEquals("Ignore trailing tilde with no slop", expectedNoSlop, Parse("\"foo bar\"~"));
            assertEquals("Ignore non-numeric trailing slop", expectedNoSlop, Parse("\"foo bar\"~a"));
            assertEquals("Ignore non-numeric trailing slop", expectedNoSlop, Parse("\"foo bar\"~1a"));
            assertEquals("Ignore negative trailing slop", expectedNoSlop, Parse("\"foo bar\"~-1"));

            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("field", "foo"));
            pq.Add(new Term("field", "bar"));
            pq.Slop = (12);

            BooleanQuery expectedBoolean = new BooleanQuery();
            expectedBoolean.Add(pq, BooleanClause.Occur.MUST);
            expectedBoolean.Add(new TermQuery(new Term("field", "baz")), BooleanClause.Occur.MUST);

            assertEquals(expectedBoolean, Parse("\"foo bar\"~12 baz"));
        }

        /** test a simple prefix */
        [Test]
        public virtual void TestPrefix()
        {
            PrefixQuery expected = new PrefixQuery(new Term("field", "foobar"));

            assertEquals(expected, Parse("foobar*"));
        }

        /** test some AND'd terms using '+' operator */
        [Test]
        public virtual void TestAND()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.MUST);
            expected.Add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur.MUST);

            assertEquals(expected, Parse("foo+bar"));
        }

        /** test some AND'd phrases using '+' operator */
        [Test]
        public virtual void TestANDPhrase()
        {
            PhraseQuery phrase1 = new PhraseQuery();
            phrase1.Add(new Term("field", "foo"));
            phrase1.Add(new Term("field", "bar"));
            PhraseQuery phrase2 = new PhraseQuery();
            phrase2.Add(new Term("field", "star"));
            phrase2.Add(new Term("field", "wars"));
            BooleanQuery expected = new BooleanQuery();
            expected.Add(phrase1, BooleanClause.Occur.MUST);
            expected.Add(phrase2, BooleanClause.Occur.MUST);

            assertEquals(expected, Parse("\"foo bar\"+\"star wars\""));
        }

        /** test some AND'd terms (just using whitespace) */
        [Test]
        public virtual void TestANDImplicit()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.MUST);
            expected.Add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur.MUST);

            assertEquals(expected, Parse("foo bar"));
        }

        /** test some OR'd terms */
        [Test]
        public virtual void TestOR()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur.SHOULD);

            assertEquals(expected, Parse("foo|bar"));
            assertEquals(expected, Parse("foo||bar"));
        }

        /** test some OR'd terms (just using whitespace) */
        [Test]
        public virtual void TestORImplicit()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur.SHOULD);

            SimpleQueryParser parser = new SimpleQueryParser(new MockAnalyzer(Random()), "field");
            assertEquals(expected, parser.Parse("foo bar"));
        }

        /** test some OR'd phrases using '|' operator */
        [Test]
        public virtual void TestORPhrase()
        {
            PhraseQuery phrase1 = new PhraseQuery();
            phrase1.Add(new Term("field", "foo"));
            phrase1.Add(new Term("field", "bar"));
            PhraseQuery phrase2 = new PhraseQuery();
            phrase2.Add(new Term("field", "star"));
            phrase2.Add(new Term("field", "wars"));
            BooleanQuery expected = new BooleanQuery();
            expected.Add(phrase1, BooleanClause.Occur.SHOULD);
            expected.Add(phrase2, BooleanClause.Occur.SHOULD);

            assertEquals(expected, Parse("\"foo bar\"|\"star wars\""));
        }

        /** test negated term */
        [Test]
        public virtual void TestNOT()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.MUST_NOT);
            expected.Add(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD);

            assertEquals(expected, Parse("-foo"));
            assertEquals(expected, Parse("-(foo)"));
            assertEquals(expected, Parse("---foo"));
        }

        /** test crazy prefixes with multiple asterisks */
        [Test]
        public virtual void TestCrazyPrefixes1()
        {
            Query expected = new PrefixQuery(new Term("field", "st*ar"));

            assertEquals(expected, Parse("st*ar*"));
        }

        /** test prefixes with some escaping */
        [Test]
        public virtual void TestCrazyPrefixes2()
        {
            Query expected = new PrefixQuery(new Term("field", "st*ar\\*"));

            assertEquals(expected, Parse("st*ar\\\\**"));
        }

        /** not a prefix query! the prefix operator is escaped */
        [Test]
        public virtual void TestTermInDisguise()
        {
            Query expected = new TermQuery(new Term("field", "st*ar\\*"));

            assertEquals(expected, Parse("sT*Ar\\\\\\*"));
        }

        // a number of test cases here have garbage/errors in
        // the syntax passed in to test that the query can
        // still be interpreted as a guess to what the human
        // input was trying to be

        [Test]
        public virtual void TestGarbageTerm()
        {
            Query expected = new TermQuery(new Term("field", "star"));

            assertEquals(expected, Parse("star"));
            assertEquals(expected, Parse("star\n"));
            assertEquals(expected, Parse("star\r"));
            assertEquals(expected, Parse("star\t"));
            assertEquals(expected, Parse("star("));
            assertEquals(expected, Parse("star)"));
            assertEquals(expected, Parse("star\""));
            assertEquals(expected, Parse("\t \r\n\nstar   \n \r \t "));
            assertEquals(expected, Parse("- + \"\" - star \\"));
        }

        [Test]
        public virtual void TestGarbageEmpty()
        {
            assertNull(Parse(""));
            assertNull(Parse("  "));
            assertNull(Parse("  "));
            assertNull(Parse("\\ "));
            assertNull(Parse("\\ \\ "));
            assertNull(Parse("\"\""));
            assertNull(Parse("\" \""));
            assertNull(Parse("\" \"|\" \""));
            assertNull(Parse("(\" \"|\" \")"));
            assertNull(Parse("\" \" \" \""));
            assertNull(Parse("(\" \" \" \")"));
        }

        [Test]
        public virtual void TestGarbageAND()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.MUST);
            expected.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.MUST);

            assertEquals(expected, Parse("star wars"));
            assertEquals(expected, Parse("star+wars"));
            assertEquals(expected, Parse("     star     wars   "));
            assertEquals(expected, Parse("     star +    wars   "));
            assertEquals(expected, Parse("  |     star + + |   wars   "));
            assertEquals(expected, Parse("  |     star + + |   wars   \\"));
        }

        [Test]
        public virtual void TestGarbageOR()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.SHOULD);

            assertEquals(expected, Parse("star|wars"));
            assertEquals(expected, Parse("     star |    wars   "));
            assertEquals(expected, Parse("  |     star | + |   wars   "));
            assertEquals(expected, Parse("  +     star | + +   wars   \\"));
        }

        [Test]
        public virtual void TestGarbageNOT()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.MUST_NOT);
            expected.Add(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD);

            assertEquals(expected, Parse("-star"));
            assertEquals(expected, Parse("---star"));
            assertEquals(expected, Parse("- -star -"));
        }

        [Test]
        public virtual void TestGarbagePhrase()
        {
            PhraseQuery expected = new PhraseQuery();
            expected.Add(new Term("field", "star"));
            expected.Add(new Term("field", "wars"));

            assertEquals(expected, Parse("\"star wars\""));
            assertEquals(expected, Parse("\"star wars\\ \""));
            assertEquals(expected, Parse("\"\" | \"star wars\""));
            assertEquals(expected, Parse("          \"star wars\"        \"\"\\"));
        }

        [Test]
        public virtual void TestGarbageSubquery()
        {
            Query expected = new TermQuery(new Term("field", "star"));

            assertEquals(expected, Parse("(star)"));
            assertEquals(expected, Parse("(star))"));
            assertEquals(expected, Parse("((star)"));
            assertEquals(expected, Parse("     -()(star)        \n\n\r     "));
            assertEquals(expected, Parse("| + - ( + - |      star    \n      ) \n"));
        }

        [Test]
        public virtual void TestCompoundAnd()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.MUST);
            expected.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.MUST);
            expected.Add(new TermQuery(new Term("field", "empire")), BooleanClause.Occur.MUST);

            assertEquals(expected, Parse("star wars empire"));
            assertEquals(expected, Parse("star+wars + empire"));
            assertEquals(expected, Parse(" | --star wars empire \n\\"));
        }

        [Test]
        public virtual void TestCompoundOr()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "empire")), BooleanClause.Occur.SHOULD);

            assertEquals(expected, Parse("star|wars|empire"));
            assertEquals(expected, Parse("star|wars | empire"));
            assertEquals(expected, Parse(" | --star|wars|empire \n\\"));
        }

        [Test]
        public virtual void TestComplex00()
        {
            BooleanQuery expected = new BooleanQuery();
            BooleanQuery inner = new BooleanQuery();
            inner.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.SHOULD);
            inner.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.SHOULD);
            expected.Add(inner, BooleanClause.Occur.MUST);
            expected.Add(new TermQuery(new Term("field", "empire")), BooleanClause.Occur.MUST);

            assertEquals(expected, Parse("star|wars empire"));
            assertEquals(expected, Parse("star|wars + empire"));
            assertEquals(expected, Parse("star| + wars + ----empire |"));
        }

        [Test]
        public virtual void TestComplex01()
        {
            BooleanQuery expected = new BooleanQuery();
            BooleanQuery inner = new BooleanQuery();
            inner.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.MUST);
            inner.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.MUST);
            expected.Add(inner, BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "empire")), BooleanClause.Occur.SHOULD);

            assertEquals(expected, Parse("star wars | empire"));
            assertEquals(expected, Parse("star + wars|empire"));
            assertEquals(expected, Parse("star + | wars | ----empire +"));
        }

        [Test]
        public virtual void TestComplex02()
        {
            BooleanQuery expected = new BooleanQuery();
            BooleanQuery inner = new BooleanQuery();
            inner.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.MUST);
            inner.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.MUST);
            expected.Add(inner, BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "empire")), BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "strikes")), BooleanClause.Occur.SHOULD);

            assertEquals(expected, Parse("star wars | empire | strikes"));
            assertEquals(expected, Parse("star + wars|empire | strikes"));
            assertEquals(expected, Parse("star + | wars | ----empire | + --strikes \\"));
        }

        [Test]
        public virtual void TestComplex03()
        {
            BooleanQuery expected = new BooleanQuery();
            BooleanQuery inner = new BooleanQuery();
            BooleanQuery inner2 = new BooleanQuery();
            inner2.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.MUST);
            inner2.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.MUST);
            inner.Add(inner2, BooleanClause.Occur.SHOULD);
            inner.Add(new TermQuery(new Term("field", "empire")), BooleanClause.Occur.SHOULD);
            inner.Add(new TermQuery(new Term("field", "strikes")), BooleanClause.Occur.SHOULD);
            expected.Add(inner, BooleanClause.Occur.MUST);
            expected.Add(new TermQuery(new Term("field", "back")), BooleanClause.Occur.MUST);

            assertEquals(expected, Parse("star wars | empire | strikes back"));
            assertEquals(expected, Parse("star + wars|empire | strikes + back"));
            assertEquals(expected, Parse("star + | wars | ----empire | + --strikes + | --back \\"));
        }

        [Test]
        public virtual void TestComplex04()
        {
            BooleanQuery expected = new BooleanQuery();
            BooleanQuery inner = new BooleanQuery();
            BooleanQuery inner2 = new BooleanQuery();
            inner.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.MUST);
            inner.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.MUST);
            inner2.Add(new TermQuery(new Term("field", "strikes")), BooleanClause.Occur.MUST);
            inner2.Add(new TermQuery(new Term("field", "back")), BooleanClause.Occur.MUST);
            expected.Add(inner, BooleanClause.Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "empire")), BooleanClause.Occur.SHOULD);
            expected.Add(inner2, BooleanClause.Occur.SHOULD);

            assertEquals(expected, Parse("(star wars) | empire | (strikes back)"));
            assertEquals(expected, Parse("(star + wars) |empire | (strikes + back)"));
            assertEquals(expected, Parse("(star + | wars |) | ----empire | + --(strikes + | --back) \\"));
        }

        [Test]
        public virtual void TestComplex05()
        {
            BooleanQuery expected = new BooleanQuery();
            BooleanQuery inner1 = new BooleanQuery();
            BooleanQuery inner2 = new BooleanQuery();
            BooleanQuery inner3 = new BooleanQuery();
            BooleanQuery inner4 = new BooleanQuery();

            expected.Add(inner1, BooleanClause.Occur.SHOULD);
            expected.Add(inner2, BooleanClause.Occur.SHOULD);

            inner1.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.MUST);
            inner1.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.MUST);

            inner2.Add(new TermQuery(new Term("field", "empire")), BooleanClause.Occur.SHOULD);
            inner2.Add(inner3, BooleanClause.Occur.SHOULD);

            inner3.Add(new TermQuery(new Term("field", "strikes")), BooleanClause.Occur.MUST);
            inner3.Add(new TermQuery(new Term("field", "back")), BooleanClause.Occur.MUST);
            inner3.Add(inner4, BooleanClause.Occur.MUST);

            inner4.Add(new TermQuery(new Term("field", "jarjar")), BooleanClause.Occur.MUST_NOT);
            inner4.Add(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD);

            assertEquals(expected, Parse("(star wars) | (empire | (strikes back -jarjar))"));
            assertEquals(expected, Parse("(star + wars) |(empire | (strikes + back -jarjar) () )"));
            assertEquals(expected, Parse("(star + | wars |) | --(--empire | + --(strikes + | --back + -jarjar) \"\" ) \""));
        }

        [Test]
        public virtual void TestComplex06()
        {
            BooleanQuery expected = new BooleanQuery();
            BooleanQuery inner1 = new BooleanQuery();
            BooleanQuery inner2 = new BooleanQuery();
            BooleanQuery inner3 = new BooleanQuery();

            expected.Add(new TermQuery(new Term("field", "star")), BooleanClause.Occur.MUST);
            expected.Add(inner1, BooleanClause.Occur.MUST);

            inner1.Add(new TermQuery(new Term("field", "wars")), BooleanClause.Occur.SHOULD);
            inner1.Add(inner2, BooleanClause.Occur.SHOULD);

            inner2.Add(inner3, BooleanClause.Occur.MUST);
            inner3.Add(new TermQuery(new Term("field", "empire")), BooleanClause.Occur.SHOULD);
            inner3.Add(new TermQuery(new Term("field", "strikes")), BooleanClause.Occur.SHOULD);
            inner2.Add(new TermQuery(new Term("field", "back")), BooleanClause.Occur.MUST);
            inner2.Add(new TermQuery(new Term("field", "jar+|jar")), BooleanClause.Occur.MUST);

            assertEquals(expected, Parse("star (wars | (empire | strikes back jar\\+\\|jar))"));
            assertEquals(expected, Parse("star + (wars |(empire | strikes + back jar\\+\\|jar) () )"));
            assertEquals(expected, Parse("star + (| wars | | --(--empire | + --strikes + | --back + jar\\+\\|jar) \"\" ) \""));
        }

        /** test a term with field weights */
        [Test]
        public virtual void TestWeightedTerm()
        {
            IDictionary<string, float> weights = new Dictionary<string, float>();
            weights["field0"] = 5f;
            weights["field1"] = 10f;

            BooleanQuery expected = new BooleanQuery(true);
            Query field0 = new TermQuery(new Term("field0", "foo"));
            field0.Boost = (5f);
            expected.Add(field0, BooleanClause.Occur.SHOULD);
            Query field1 = new TermQuery(new Term("field1", "foo"));
            field1.Boost = (10f);
            expected.Add(field1, BooleanClause.Occur.SHOULD);

            Analyzer analyzer = new MockAnalyzer(Random());
            SimpleQueryParser parser = new SimpleQueryParser(analyzer, weights);
            assertEquals(expected, parser.Parse("foo"));
        }

        /** test a more complex query with field weights */
        [Test]
        public virtual void testWeightedOR()
        {
            IDictionary<string, float> weights = new Dictionary<string, float>();
            weights["field0"] = 5f;
            weights["field1"] = 10f;

            BooleanQuery expected = new BooleanQuery();
            BooleanQuery foo = new BooleanQuery(true);
            Query field0 = new TermQuery(new Term("field0", "foo"));
            field0.Boost = (5f);
            foo.Add(field0, BooleanClause.Occur.SHOULD);
            Query field1 = new TermQuery(new Term("field1", "foo"));
            field1.Boost = (10f);
            foo.Add(field1, BooleanClause.Occur.SHOULD);
            expected.Add(foo, BooleanClause.Occur.SHOULD);

            BooleanQuery bar = new BooleanQuery(true);
            field0 = new TermQuery(new Term("field0", "bar"));
            field0.Boost = (5f);
            bar.Add(field0, BooleanClause.Occur.SHOULD);
            field1 = new TermQuery(new Term("field1", "bar"));
            field1.Boost = (10f);
            bar.Add(field1, BooleanClause.Occur.SHOULD);
            expected.Add(bar, BooleanClause.Occur.SHOULD);

            Analyzer analyzer = new MockAnalyzer(Random());
            SimpleQueryParser parser = new SimpleQueryParser(analyzer, weights);
            assertEquals(expected, parser.Parse("foo|bar"));
        }

        /** helper to parse a query with keyword analyzer across "field" */
        private Query ParseKeyword(string text, int flags)
        {
            Analyzer analyzer = new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false);
            SimpleQueryParser parser = new SimpleQueryParser(analyzer,
                new HashMap<string, float>() { { "field", 1f } },
                flags);
            return parser.Parse(text);
        }

        /** test the ability to enable/disable phrase operator */
        [Test]
        public virtual void TestDisablePhrase()
        {
            Query expected = new TermQuery(new Term("field", "\"test\""));
            assertEquals(expected, ParseKeyword("\"test\"", ~SimpleQueryParser.PHRASE_OPERATOR));
        }

        /** test the ability to enable/disable prefix operator */
        [Test]
        public virtual void TestDisablePrefix()
        {
            Query expected = new TermQuery(new Term("field", "test*"));
            assertEquals(expected, ParseKeyword("test*", ~SimpleQueryParser.PREFIX_OPERATOR));
        }

        /** test the ability to enable/disable AND operator */
        [Test]
        public virtual void TestDisableAND()
        {
            Query expected = new TermQuery(new Term("field", "foo+bar"));
            assertEquals(expected, ParseKeyword("foo+bar", ~SimpleQueryParser.AND_OPERATOR));
            expected = new TermQuery(new Term("field", "+foo+bar"));
            assertEquals(expected, ParseKeyword("+foo+bar", ~SimpleQueryParser.AND_OPERATOR));
        }

        /** test the ability to enable/disable OR operator */
        [Test]
        public virtual void TestDisableOR()
        {
            Query expected = new TermQuery(new Term("field", "foo|bar"));
            assertEquals(expected, ParseKeyword("foo|bar", ~SimpleQueryParser.OR_OPERATOR));
            expected = new TermQuery(new Term("field", "|foo|bar"));
            assertEquals(expected, ParseKeyword("|foo|bar", ~SimpleQueryParser.OR_OPERATOR));
        }

        /** test the ability to enable/disable NOT operator */
        [Test]
        public virtual void TestDisableNOT()
        {
            Query expected = new TermQuery(new Term("field", "-foo"));
            assertEquals(expected, ParseKeyword("-foo", ~SimpleQueryParser.NOT_OPERATOR));
        }

        /** test the ability to enable/disable precedence operators */
        [Test]
        public virtual void TestDisablePrecedence()
        {
            Query expected = new TermQuery(new Term("field", "(foo)"));
            assertEquals(expected, ParseKeyword("(foo)", ~SimpleQueryParser.PRECEDENCE_OPERATORS));
            expected = new TermQuery(new Term("field", ")foo("));
            assertEquals(expected, ParseKeyword(")foo(", ~SimpleQueryParser.PRECEDENCE_OPERATORS));
        }

        /** test the ability to enable/disable escape operators */
        [Test]
        public virtual void TestDisableEscape()
        {
            Query expected = new TermQuery(new Term("field", "foo\\bar"));
            assertEquals(expected, ParseKeyword("foo\\bar", ~SimpleQueryParser.ESCAPE_OPERATOR));
            assertEquals(expected, ParseKeyword("(foo\\bar)", ~SimpleQueryParser.ESCAPE_OPERATOR));
            assertEquals(expected, ParseKeyword("\"foo\\bar\"", ~SimpleQueryParser.ESCAPE_OPERATOR));
        }

        [Test]
        public virtual void TestDisableWhitespace()
        {
            Query expected = new TermQuery(new Term("field", "foo foo"));
            assertEquals(expected, ParseKeyword("foo foo", ~SimpleQueryParser.WHITESPACE_OPERATOR));
            expected = new TermQuery(new Term("field", " foo foo\n "));
            assertEquals(expected, ParseKeyword(" foo foo\n ", ~SimpleQueryParser.WHITESPACE_OPERATOR));
            expected = new TermQuery(new Term("field", "\t\tfoo foo foo"));
            assertEquals(expected, ParseKeyword("\t\tfoo foo foo", ~SimpleQueryParser.WHITESPACE_OPERATOR));
        }

        [Test]
        public virtual void TestDisableFuzziness()
        {
            Query expected = new TermQuery(new Term("field", "foo~1"));
            assertEquals(expected, ParseKeyword("foo~1", ~SimpleQueryParser.FUZZY_OPERATOR));
        }

        [Test]
        public virtual void TestDisableSlop()
        {
            PhraseQuery expectedPhrase = new PhraseQuery();
            expectedPhrase.Add(new Term("field", "foo"));
            expectedPhrase.Add(new Term("field", "bar"));

            BooleanQuery expected = new BooleanQuery();
            expected.Add(expectedPhrase, BooleanClause.Occur.MUST);
            expected.Add(new TermQuery(new Term("field", "~2")), BooleanClause.Occur.MUST);
            assertEquals(expected, Parse("\"foo bar\"~2", ~SimpleQueryParser.NEAR_OPERATOR));
        }

        // we aren't supposed to barf on any input...
        [Test]
        public virtual void TestRandomQueries()
        {
            for (int i = 0; i < 1000; i++)
            {
                string query = TestUtil.RandomUnicodeString(Random());
                Parse(query); // no exception
                ParseKeyword(query, TestUtil.NextInt(Random(), 0, 1024)); // no exception
            }
        }

        [Test]
        public virtual void testRandomQueries2()
        {
            char[] chars = new char[] { 'a', '1', '|', '&', ' ', '(', ')', '"', '-', '~' };
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                sb.Length = (0);
                int queryLength = Random().Next(20);
                for (int j = 0; j < queryLength; j++)
                {
                    sb.append(chars[Random().Next(chars.Length)]);
                }
                Parse(sb.toString()); // no exception
                ParseKeyword(sb.toString(), TestUtil.NextInt(Random(), 0, 1024)); // no exception
            }
        }
    }
}
