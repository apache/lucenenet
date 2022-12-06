using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;

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
    /// This test case is a copy of the core Lucene query parser test, it was adapted
    /// to use new QueryParserHelper instead of the old query parser.
    /// 
    /// Tests QueryParser.
    /// </summary>
    // TODO: really this should extend QueryParserTestBase too!
    public class TestQPHelper : LuceneTestCase
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
            private readonly ICharTermAttribute termAtt;
            private readonly IOffsetAttribute offsetAtt;

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


            public override bool IncrementToken()
            {
                if (inPhrase)
                {
                    inPhrase = false;
                    ClearAttributes();
                    termAtt.SetEmpty().Append("phrase2");
                    offsetAtt.SetOffset(savedStart, savedEnd);
                    return true;
                }
                else
                    while (m_input.IncrementToken())
                    {
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
                    }
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

            protected internal override sealed TokenStreamComponents CreateComponents(String fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(tokenizer, new QPTestFilter(tokenizer));
            }
        }

        public class QPTestParser : StandardQueryParser
        {
            public QPTestParser(Analyzer a)
            {
                ((QueryNodeProcessorPipeline)QueryNodeProcessor)
                    .Add(new QPTestParserQueryNodeProcessor());
                this.Analyzer = (a);

            }

            private class QPTestParserQueryNodeProcessor :
                QueryNodeProcessor
            {


                protected override IQueryNode PostProcessNode(IQueryNode node)
                {

                    return node;

                }


                protected override IQueryNode PreProcessNode(IQueryNode node)
                {

                    if (node is WildcardQueryNode || node is FuzzyQueryNode)
                    {
                        // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                        throw new QueryNodeException(
                            QueryParserMessages.EMPTY_MESSAGE);

                    }

                    return node;

                }


                protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
                {

                    return children;

                }

            }

        }

        private int originalMaxClauses;


        public override void SetUp()
        {
            base.SetUp();
            originalMaxClauses = BooleanQuery.MaxClauseCount;
        }

        public StandardQueryParser GetParser(Analyzer a)
        {
            if (a is null)
                a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            StandardQueryParser qp = new StandardQueryParser();
            qp.Analyzer = (a);

            qp.DefaultOperator = (StandardQueryConfigHandler.Operator.OR);

            return qp;

        }

        public Query GetQuery(String query, Analyzer a)
        {
            return GetParser(a).Parse(query, "field");
        }

        public Query GetQueryAllowLeadingWildcard(String query, Analyzer a)
        {
            StandardQueryParser parser = GetParser(a);
            parser.AllowLeadingWildcard = (true);
            return parser.Parse(query, "field");
        }

        public void AssertQueryEquals(String query, Analyzer a, String result)
        {
            Query q = GetQuery(query, a);
            String s = q.ToString("field");
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("Query /" + query + "/ yielded /" + s + "/, expecting /" + result
                    + "/");
            }
        }

        public void AssertQueryEqualsAllowLeadingWildcard(String query, Analyzer a, String result)
        {
            Query q = GetQueryAllowLeadingWildcard(query, a);
            String s = q.ToString("field");
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("Query /" + query + "/ yielded /" + s + "/, expecting /" + result
                    + "/");
            }
        }

        public void AssertQueryEquals(StandardQueryParser qp, String field,
            String query, String result)
        {
            Query q = qp.Parse(query, field);
            String s = q.ToString(field);
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("Query /" + query + "/ yielded /" + s + "/, expecting /" + result
                    + "/");
            }
        }

        public void AssertEscapedQueryEquals(String query, Analyzer a, String result)
        {
            String escapedQuery = QueryParserUtil.Escape(query);
            if (!escapedQuery.Equals(result, StringComparison.Ordinal))
            {
                fail("Query /" + query + "/ yielded /" + escapedQuery + "/, expecting /"
                    + result + "/");
            }
        }

        public void AssertWildcardQueryEquals(String query, bool lowercase,
            String result, bool allowLeadingWildcard)
        {
            StandardQueryParser qp = GetParser(null);
            qp.LowercaseExpandedTerms = (lowercase);
            qp.AllowLeadingWildcard = (allowLeadingWildcard);
            Query q = qp.Parse(query, "field");
            String s = q.ToString("field");
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("WildcardQuery /" + query + "/ yielded /" + s + "/, expecting /"
                    + result + "/");
            }
        }

        public void AssertWildcardQueryEquals(String query, bool lowercase,
            String result)
        {
            AssertWildcardQueryEquals(query, lowercase, result, false);
        }

        public void AssertWildcardQueryEquals(String query, String result)
        {
            StandardQueryParser qp = GetParser(null);
            Query q = qp.Parse(query, "field");
            String s = q.ToString("field");
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("WildcardQuery /" + query + "/ yielded /" + s + "/, expecting /"
                    + result + "/");
            }
        }

        public Query GetQueryDOA(String query, Analyzer a)
        {
            if (a is null)
                a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            StandardQueryParser qp = new StandardQueryParser();
            qp.Analyzer = (a);
            qp.DefaultOperator = (StandardQueryConfigHandler.Operator.AND);

            return qp.Parse(query, "field");

        }

        public void AssertQueryEqualsDOA(String query, Analyzer a, String result)
        {
            Query q = GetQueryDOA(query, a);
            String s = q.ToString("field");
            if (!s.Equals(result, StringComparison.Ordinal))
            {
                fail("Query /" + query + "/ yielded /" + s + "/, expecting /" + result
                    + "/");
            }
        }
        [Test]
        public void TestConstantScoreAutoRewrite()
        {
            StandardQueryParser qp = new StandardQueryParser(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));
            Query q = qp.Parse("foo*bar", "field");
            assertTrue(q is WildcardQuery);
            assertEquals(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT, ((MultiTermQuery)q).MultiTermRewriteMethod);

            q = qp.Parse("foo*", "field");
            assertTrue(q is PrefixQuery);
            assertEquals(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT, ((MultiTermQuery)q).MultiTermRewriteMethod);

            q = qp.Parse("[a TO z]", "field");
            assertTrue(q is TermRangeQuery);
            assertEquals(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT, ((MultiTermQuery)q).MultiTermRewriteMethod);
        }
        [Test]
        public void TestCJK()
        {
            // Test Ideographic Space - As wide as a CJK character cell (fullwidth)
            // used google to translate the word "term" to japanese -> ??
            AssertQueryEquals("term\u3000term\u3000term", null,
                "term\u0020term\u0020term");
            AssertQueryEqualsAllowLeadingWildcard("??\u3000??\u3000??", null, "??\u0020??\u0020??");
        }

        //individual CJK chars as terms, like StandardAnalyzer
        private sealed class SimpleCJKTokenizer : Tokenizer
        {
            private ICharTermAttribute termAtt;

            public SimpleCJKTokenizer(TextReader input)
                        : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
            }


            public override bool IncrementToken()
            {
                int ch = m_input.Read();
                if (ch < 0)
                    return false;
                ClearAttributes();
                termAtt.SetEmpty().Append((char)ch);
                return true;
            }
        }

        private class SimpleCJKAnalyzer : Analyzer
        {

            protected internal override TokenStreamComponents CreateComponents(String fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new SimpleCJKTokenizer(reader));
            }
        }
        [Test]
        public void TestCJKTerm()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);
            assertEquals(expected, GetQuery("中国", analyzer));

            expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), Occur.MUST);
            BooleanQuery inner = new BooleanQuery();
            inner.Add(new TermQuery(new Term("field", "中")), Occur.SHOULD);
            inner.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);
            expected.Add(inner, Occur.MUST);
            assertEquals(expected, GetQuery("中 AND 中国", new SimpleCJKAnalyzer()));

        }
        [Test]
        public void TestCJKBoostedTerm()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            BooleanQuery expected = new BooleanQuery();
            expected.Boost = (0.5f);
            expected.Add(new TermQuery(new Term("field", "中")), Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);


            assertEquals(expected, GetQuery("中国^0.5", analyzer));
        }
        [Test]
        public void TestCJKPhrase()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            PhraseQuery expected = new PhraseQuery();
            expected.Add(new Term("field", "中"));
            expected.Add(new Term("field", "国"));


            assertEquals(expected, GetQuery("\"中国\"", analyzer));
        }
        [Test]
        public void TestCJKBoostedPhrase()
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
        public void TestCJKSloppyPhrase()
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
        public void TestSimple()
        {
            AssertQueryEquals("field=a", null, "a");
            AssertQueryEquals("\"term germ\"~2", null, "\"term germ\"~2");
            AssertQueryEquals("term term term", null, "term term term");
            AssertQueryEquals("t�rm term term", new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false),
                "t�rm term term");
            AssertQueryEquals("�mlaut", new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), "�mlaut");

            // FIXME: change MockAnalyzer to not extend CharTokenizer for this test
            //assertQueryEquals("\"\"", new KeywordAnalyzer(), "");
            //assertQueryEquals("foo:\"\"", new KeywordAnalyzer(), "foo:");

            AssertQueryEquals("a AND b", null, "+a +b");
            AssertQueryEquals("(a AND b)", null, "+a +b");
            AssertQueryEquals("c OR (a AND b)", null, "c (+a +b)");

            AssertQueryEquals("a AND NOT b", null, "+a -b");

            AssertQueryEquals("a AND -b", null, "+a -b");

            AssertQueryEquals("a AND !b", null, "+a -b");

            AssertQueryEquals("a && b", null, "+a +b");

            AssertQueryEquals("a && ! b", null, "+a -b");

            AssertQueryEquals("a OR b", null, "a b");
            AssertQueryEquals("a || b", null, "a b");

            AssertQueryEquals("a OR !b", null, "a -b");

            AssertQueryEquals("a OR ! b", null, "a -b");

            AssertQueryEquals("a OR -b", null, "a -b");

            AssertQueryEquals("+term -term term", null, "+term -term term");
            AssertQueryEquals("foo:term AND field:anotherTerm", null,
                "+foo:term +anotherterm");
            AssertQueryEquals("term AND \"phrase phrase\"", null,
                "+term +\"phrase phrase\"");
            AssertQueryEquals("\"hello there\"", null, "\"hello there\"");
            assertTrue(GetQuery("a AND b", null) is BooleanQuery);
            assertTrue(GetQuery("hello", null) is TermQuery);
            assertTrue(GetQuery("\"hello there\"", null) is PhraseQuery);

            AssertQueryEquals("germ term^2.0", null, "germ term^2.0");
            AssertQueryEquals("(term)^2.0", null, "term^2.0");
            AssertQueryEquals("(germ term)^2.0", null, "(germ term)^2.0");
            AssertQueryEquals("term^2.0", null, "term^2.0");
            AssertQueryEquals("term^2", null, "term^2.0");
            AssertQueryEquals("\"germ term\"^2.0", null, "\"germ term\"^2.0");
            AssertQueryEquals("\"term germ\"^2", null, "\"term germ\"^2.0");

            AssertQueryEquals("(foo OR bar) AND (baz OR boo)", null,
                "+(foo bar) +(baz boo)");
            AssertQueryEquals("((a OR b) AND NOT c) OR d", null, "(+(a b) -c) d");
            AssertQueryEquals("+(apple \"steve jobs\") -(foo bar baz)", null,
                "+(apple \"steve jobs\") -(foo bar baz)");
            AssertQueryEquals("+title:(dog OR cat) -author:\"bob dole\"", null,
                "+(title:dog title:cat) -author:\"bob dole\"");

        }
        [Test]
        public void TestPunct()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            AssertQueryEquals("a&b", a, "a&b");
            AssertQueryEquals("a&&b", a, "a&&b");
            AssertQueryEquals(".NET", a, ".NET");
        }
        [Test]
        public void TestGroup()
        {
            AssertQueryEquals("!(a AND b) OR c", null, "-(+a +b) c");
            AssertQueryEquals("!(a AND b) AND c", null, "-(+a +b) +c");
            AssertQueryEquals("((a AND b) AND c)", null, "+(+a +b) +c");
            AssertQueryEquals("(a AND b) AND c", null, "+(+a +b) +c");
            AssertQueryEquals("b !(a AND b)", null, "b -(+a +b)");
            AssertQueryEquals("(a AND b)^4 OR c", null, "((+a +b)^4.0) c");
        }
        [Test]
        public void TestSlop()
        {

            AssertQueryEquals("\"term germ\"~2", null, "\"term germ\"~2");
            AssertQueryEquals("\"term germ\"~2 flork", null, "\"term germ\"~2 flork");
            AssertQueryEquals("\"term\"~2", null, "term");
            AssertQueryEquals("\" \"~2 germ", null, "germ");
            AssertQueryEquals("\"term germ\"~2^2", null, "\"term germ\"~2^2.0");
        }
        [Test]
        public void TestNumber()
        {
            // The numbers go away because SimpleAnalzyer ignores them
            AssertQueryEquals("3", null, "");
            AssertQueryEquals("term 1.0 1 2", null, "term");
            AssertQueryEquals("term term1 term2", null, "term term term");

            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            AssertQueryEquals("3", a, "3");
            AssertQueryEquals("term 1.0 1 2", a, "term 1.0 1 2");
            AssertQueryEquals("term term1 term2", a, "term term1 term2");
        }
        [Test]
        public void TestWildcard()
        {
            AssertQueryEquals("term*", null, "term*");
            AssertQueryEquals("term*^2", null, "term*^2.0");
            AssertQueryEquals("term~", null, "term~2");
            AssertQueryEquals("term~0.7", null, "term~1");

            AssertQueryEquals("term~^3", null, "term~2^3.0");

            AssertQueryEquals("term^3~", null, "term~2^3.0");
            AssertQueryEquals("term*germ", null, "term*germ");
            AssertQueryEquals("term*germ^3", null, "term*germ^3.0");

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

            AssertQueryNodeException("term~1.1"); // value > 1, throws exception

            assertTrue(GetQuery("term*germ", null) is WildcardQuery);

            /*
             * Tests to see that wild card terms are (or are not) properly lower-cased
             * with propery parser configuration
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
            // Fuzzy queries:
            AssertWildcardQueryEquals("Term~", "term~2");
            AssertWildcardQueryEquals("Term~", true, "term~2");
            AssertWildcardQueryEquals("Term~", false, "Term~2");
            // Range queries:

            // TODO: implement this on QueryParser
            // Q0002E_INVALID_SYNTAX_CANNOT_PARSE: Syntax Error, cannot parse '[A TO
            // C]': Lexical error at line 1, column 1. Encountered: "[" (91), after
            // : ""
            AssertWildcardQueryEquals("[A TO C]", "[a TO c]");
            AssertWildcardQueryEquals("[A TO C]", true, "[a TO c]");
            AssertWildcardQueryEquals("[A TO C]", false, "[A TO C]");
            // Test suffix queries: first disallow
            try
            {
                AssertWildcardQueryEquals("*Term", true, "*term");
                fail();
            }
#pragma warning disable 168
            catch (QueryNodeException pe)
#pragma warning restore 168
            {
                // expected exception
            }
            try
            {
                AssertWildcardQueryEquals("?Term", true, "?term");
                fail();
            }
#pragma warning disable 168
            catch (QueryNodeException pe)
#pragma warning restore 168
            {
                // expected exception
            }
            // Test suffix queries: then allow
            AssertWildcardQueryEquals("*Term", true, "*term", true);
            AssertWildcardQueryEquals("?Term", true, "?term", true);
        }
        [Test]
        public void TestLeadingWildcardType()
        {
            StandardQueryParser qp = GetParser(null);
            qp.AllowLeadingWildcard = (true);
            assertEquals(typeof(WildcardQuery), qp.Parse("t*erm*", "field").GetType());
            assertEquals(typeof(WildcardQuery), qp.Parse("?term*", "field").GetType());
            assertEquals(typeof(WildcardQuery), qp.Parse("*term*", "field").GetType());
        }
        [Test]
        public void TestQPA()
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
        }
        [Test]
        public void TestRange()
        {
            AssertQueryEquals("[ a TO z]", null, "[a TO z]");
            assertEquals(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT, ((TermRangeQuery)GetQuery("[ a TO z]", null)).MultiTermRewriteMethod);

            StandardQueryParser qp = new StandardQueryParser();

            qp.MultiTermRewriteMethod = (MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
            assertEquals(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE, ((TermRangeQuery)qp.Parse("[ a TO z]", "field")).MultiTermRewriteMethod);

            // test open ranges
            AssertQueryEquals("[ a TO * ]", null, "[a TO *]");
            AssertQueryEquals("[ * TO z ]", null, "[* TO z]");
            AssertQueryEquals("[ * TO * ]", null, "[* TO *]");


            AssertQueryEquals("field>=a", null, "[a TO *]");
            AssertQueryEquals("field>a", null, "{a TO *]");
            AssertQueryEquals("field<=a", null, "[* TO a]");
            AssertQueryEquals("field<a", null, "[* TO a}");

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
            AssertQueryEquals("gack ( bar blar { a TO z}) ", null,
                "gack (bar blar {a TO z})");
        }

        /** for testing DateTools support */
        private String GetDate(String s, DateResolution resolution)

        {
            // we use the default Locale since LuceneTestCase randomizes it
            //DateFormat df = DateFormat.getDateInstance(DateFormat.SHORT, Locale.getDefault());
            //return getDate(df.parse(s), resolution);

            return GetDate(DateTime.ParseExact(s, "d", null), resolution);
        }

        /** for testing DateTools support */
        private String GetDate(DateTime d, DateResolution resolution)
        {
            return DateTools.DateToString(d, resolution);
        }

        private String EscapeDateString(String s)
        {
            if (s.Contains(" "))
            {
                return "\"" + s + "\"";
            }
            else
            {
                return s;
            }
        }

        private String GetLocalizedDate(int year, int month, int day)
        {
            DateTime date = new GregorianCalendar().ToDateTime(year, month, day, 23, 59, 59, 999);
            date = TimeZoneInfo.ConvertTime(date, TimeZoneInfo.Local);
            return date.ToString("d"); //.ToShortDateString();

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
        public void TestDateRange()
        {
            String startDate = GetLocalizedDate(2002, 1, 1);
            String endDate = GetLocalizedDate(2002, 1, 4);

            //// we use the default Locale/TZ since LuceneTestCase randomizes it
            //Calendar endDateExpected = new GregorianCalendar(TimeZone.getDefault(), Locale.getDefault());
            //endDateExpected.clear();
            //endDateExpected.set(2002, 1, 4, 23, 59, 59);
            //endDateExpected.set(Calendar.MILLISECOND, 999);

            // we use the default Locale/TZ since LuceneTestCase randomizes it
            DateTime endDateExpected = new GregorianCalendar().ToDateTime(2002, 1, 4, 23, 59, 59, 999);

            String defaultField = "default";
            String monthField = "month";
            String hourField = "hour";
            StandardQueryParser qp = new StandardQueryParser();

            IDictionary<string, DateResolution> dateRes = new Dictionary<string, DateResolution>();

            // set a field specific date resolution    
            dateRes[monthField] = DateResolution.MONTH;
#pragma warning disable 612, 618
            qp.SetDateResolution(dateRes);
#pragma warning restore 612, 618

            // set default date resolution to MILLISECOND
            qp.SetDateResolution(DateResolution.MILLISECOND);

            // set second field specific date resolution
            dateRes[hourField] = DateResolution.HOUR;
#pragma warning disable 612, 618
            qp.SetDateResolution(dateRes);
#pragma warning restore 612, 618

            // for this field no field specific date resolution has been set,
            // so verify if the default resolution is used
            AssertDateRangeQueryEquals(qp, defaultField, startDate, endDate,
                endDateExpected/*.getTime()*/, DateResolution.MILLISECOND);

            // verify if field specific date resolutions are used for these two
            // fields
            AssertDateRangeQueryEquals(qp, monthField, startDate, endDate,
                endDateExpected/*.getTime()*/, DateResolution.MONTH);

            AssertDateRangeQueryEquals(qp, hourField, startDate, endDate,
                endDateExpected/*.getTime()*/, DateResolution.HOUR);
        }

        public void AssertDateRangeQueryEquals(StandardQueryParser qp,
            String field, String startDate, String endDate, DateTime endDateInclusive,
            DateResolution resolution)
        {
            AssertQueryEquals(qp, field, field + ":[" + EscapeDateString(startDate) + " TO " + EscapeDateString(endDate)
                + "]", "[" + GetDate(startDate, resolution) + " TO "
                + GetDate(endDateInclusive, resolution) + "]");
            AssertQueryEquals(qp, field, field + ":{" + EscapeDateString(startDate) + " TO " + EscapeDateString(endDate)
                + "}", "{" + GetDate(startDate, resolution) + " TO "
                + GetDate(endDate, resolution) + "}");
        }
        [Test]
        public void TestEscaped()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);

            /*
             * assertQueryEquals("\\[brackets", a, "\\[brackets");
             * assertQueryEquals("\\[brackets", null, "brackets");
             * assertQueryEquals("\\\\", a, "\\\\"); assertQueryEquals("\\+blah", a,
             * "\\+blah"); assertQueryEquals("\\(blah", a, "\\(blah");
             * 
             * assertQueryEquals("\\-blah", a, "\\-blah"); assertQueryEquals("\\!blah",
             * a, "\\!blah"); assertQueryEquals("\\{blah", a, "\\{blah");
             * assertQueryEquals("\\}blah", a, "\\}blah"); assertQueryEquals("\\:blah",
             * a, "\\:blah"); assertQueryEquals("\\^blah", a, "\\^blah");
             * assertQueryEquals("\\[blah", a, "\\[blah"); assertQueryEquals("\\]blah",
             * a, "\\]blah"); assertQueryEquals("\\\"blah", a, "\\\"blah");
             * assertQueryEquals("\\(blah", a, "\\(blah"); assertQueryEquals("\\)blah",
             * a, "\\)blah"); assertQueryEquals("\\~blah", a, "\\~blah");
             * assertQueryEquals("\\*blah", a, "\\*blah"); assertQueryEquals("\\?blah",
             * a, "\\?blah"); //assertQueryEquals("foo \\&\\& bar", a,
             * "foo \\&\\& bar"); //assertQueryEquals("foo \\|| bar", a,
             * "foo \\|| bar"); //assertQueryEquals("foo \\AND bar", a,
             * "foo \\AND bar");
             */

            AssertQueryEquals("\\*", a, "*");


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

            AssertQueryEquals("a:b\\-?c", a, "a:b-?c");
            AssertQueryEquals("a:b\\+?c", a, "a:b+?c");
            AssertQueryEquals("a:b\\:?c", a, "a:b:?c");

            AssertQueryEquals("a:b\\\\?c", a, "a:b\\?c");

            AssertQueryEquals("a:b\\-c~", a, "a:b-c~2");
            AssertQueryEquals("a:b\\+c~", a, "a:b+c~2");
            AssertQueryEquals("a:b\\:c~", a, "a:b:c~2");
            AssertQueryEquals("a:b\\\\c~", a, "a:b\\c~2");

            // TODO: implement Range queries on QueryParser
            AssertQueryEquals("[ a\\- TO a\\+ ]", null, "[a- TO a+]");
            AssertQueryEquals("[ a\\: TO a\\~ ]", null, "[a: TO a~]");
            AssertQueryEquals("[ a\\\\ TO a\\* ]", null, "[a\\ TO a*]");

            AssertQueryEquals(
                "[\"c\\:\\\\temp\\\\\\~foo0.txt\" TO \"c\\:\\\\temp\\\\\\~foo9.txt\"]",
                a, "[c:\\temp\\~foo0.txt TO c:\\temp\\~foo9.txt]");

            AssertQueryEquals("a\\\\\\+b", a, "a\\+b");

            AssertQueryEquals("a \\\"b c\\\" d", a, "a \"b c\" d");
            AssertQueryEquals("\"a \\\"b c\\\" d\"", a, "\"a \"b c\" d\"");
            AssertQueryEquals("\"a \\+b c d\"", a, "\"a +b c d\"");

            AssertQueryEquals("c\\:\\\\temp\\\\\\~foo.txt", a, "c:\\temp\\~foo.txt");

            AssertQueryNodeException("XY\\"); // there must be a character after the
                                              // escape char

            // test unicode escaping
            AssertQueryEquals("a\\u0062c", a, "abc");
            AssertQueryEquals("XY\\u005a", a, "XYZ");
            AssertQueryEquals("XY\\u005A", a, "XYZ");
            AssertQueryEquals("\"a \\\\\\u0028\\u0062\\\" c\"", a, "\"a \\(b\" c\"");

            AssertQueryNodeException("XY\\u005G"); // test non-hex character in escaped
                                                   // unicode sequence
            AssertQueryNodeException("XY\\u005"); // test incomplete escaped unicode
                                                  // sequence

            // Tests bug LUCENE-800
            AssertQueryEquals("(item:\\\\ item:ABCD\\\\)", a, "item:\\ item:ABCD\\");
            AssertQueryNodeException("(item:\\\\ item:ABCD\\\\))"); // unmatched closing
                                                                    // paranthesis
            AssertQueryEquals("\\*", a, "*");
            AssertQueryEquals("\\\\", a, "\\"); // escaped backslash

            AssertQueryNodeException("\\"); // a backslash must always be escaped

            // LUCENE-1189
            AssertQueryEquals("(\"a\\\\\") or (\"b\")", a, "a\\ or b");
        }
        [Test]
        public void TestQueryStringEscaping()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);

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
        [Ignore("flexible queryparser shouldn't escape wildcard terms")]
        public void TestEscapedWildcard()
        {
            StandardQueryParser qp = new StandardQueryParser();
            qp.Analyzer = (new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));

            WildcardQuery q = new WildcardQuery(new Term("field", "foo\\?ba?r"));
            assertEquals(q, qp.Parse("foo\\?ba?r", "field"));
        }
        [Test]
        public void TestTabNewlineCarriageReturn()
        {
            AssertQueryEqualsDOA("+weltbank +worlbank", null, "+weltbank +worlbank");

            AssertQueryEqualsDOA("+weltbank\n+worlbank", null, "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \n+worlbank", null, "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \n +worlbank", null, "+weltbank +worlbank");

            AssertQueryEqualsDOA("+weltbank\r+worlbank", null, "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \r+worlbank", null, "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \r +worlbank", null, "+weltbank +worlbank");

            AssertQueryEqualsDOA("+weltbank\r\n+worlbank", null, "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \r\n+worlbank", null, "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \r\n +worlbank", null, "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \r \n +worlbank", null,
                "+weltbank +worlbank");

            AssertQueryEqualsDOA("+weltbank\t+worlbank", null, "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \t+worlbank", null, "+weltbank +worlbank");
            AssertQueryEqualsDOA("weltbank \t +worlbank", null, "+weltbank +worlbank");
        }
        [Test]
        public void TestSimpleDAO()
        {
            AssertQueryEqualsDOA("term term term", null, "+term +term +term");
            AssertQueryEqualsDOA("term +term term", null, "+term +term +term");
            AssertQueryEqualsDOA("term term +term", null, "+term +term +term");
            AssertQueryEqualsDOA("term +term +term", null, "+term +term +term");
            AssertQueryEqualsDOA("-term term term", null, "-term +term +term");
        }
        [Test]
        public void TestBoost()
        {
            CharacterRunAutomaton stopSet = new CharacterRunAutomaton(BasicAutomata.MakeString("on"));
            Analyzer oneStopAnalyzer = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, stopSet);
            StandardQueryParser qp = new StandardQueryParser();
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

            StandardQueryParser qp2 = new StandardQueryParser();
            qp2.Analyzer = (new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET));

            q = qp2.Parse("the^3", "field");
            // "the" is a stop word so the result is an empty query:
            assertNotNull(q);
            assertEquals("", q.toString());
            assertEquals(1.0f, q.Boost, 0.01f);
        }

        public void AssertQueryNodeException(String queryString)
        {
            try
            {
                GetQuery(queryString, null);
            }
#pragma warning disable 168
            catch (QueryNodeException expected)
#pragma warning restore 168
            {
                return;
            }
            fail("ParseException expected, not thrown");
        }
        [Test]
        public void TestException()
        {
            AssertQueryNodeException("*leadingWildcard"); // disallowed by default
            AssertQueryNodeException("\"some phrase");
            AssertQueryNodeException("(foo bar");
            AssertQueryNodeException("foo bar))");
            AssertQueryNodeException("field:term:with:colon some more terms");
            AssertQueryNodeException("(sub query)^5.0^2.0 plus more");
            AssertQueryNodeException("secret AND illegal) AND access:confidential");
        }
        [Test]
        public void TestCustomQueryParserWildcard()
        {
            try
            {
                new QPTestParser(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).Parse("a?t", "contents");
                fail("Wildcard queries should not be allowed");
            }
#pragma warning disable 168
            catch (QueryNodeException expected)
#pragma warning restore 168
            {
                // expected exception
            }
        }
        [Test]
        public void TestCustomQueryParserFuzzy()
        {
            try
            {
                new QPTestParser(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)).Parse("xunit~", "contents");
                fail("Fuzzy queries should not be allowed");
            }
#pragma warning disable 168
            catch (QueryNodeException expected)
#pragma warning restore 168
            {
                // expected exception
            }
        }

        [Test]
        public void TestBooleanQuery()
        {
            BooleanQuery.MaxClauseCount = (2);
            try
            {
                StandardQueryParser qp = new StandardQueryParser();
                qp.Analyzer = (new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));

                qp.Parse("one two three", "field");
                fail("ParseException expected due to too many boolean clauses");
            }
#pragma warning disable 168
            catch (QueryNodeException expected)
#pragma warning restore 168
            {
                // too many boolean clauses, so ParseException is expected
            }
        }

        /**
         * This test differs from TestPrecedenceQueryParser
         */
        [Test]
        public void TestPrecedence()
        {
            StandardQueryParser qp = new StandardQueryParser();
            qp.Analyzer = (new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));

            Query query1 = qp.Parse("A AND B OR C AND D", "field");
            Query query2 = qp.Parse("+A +B +C +D", "field");

            assertEquals(query1, query2);
        }

        // [Test]
        // Todo: Convert from DateField to DateUtil
        //  public void TestLocalDateFormat() throws IOException, QueryNodeException {
        //    Directory ramDir = newDirectory();
        //    IndexWriter iw = new IndexWriter(ramDir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random, MockTokenizer.WHITESPACE, false)));
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
        //  private void addDateDoc(String content, int year, int month, int day,
        //                          int hour, int minute, int second, IndexWriter iw) throws IOException {
        //    Document d = new Document();
        //    d.add(newField("f", content, Field.Store.YES, Field.Index.ANALYZED));
        //    Calendar cal = Calendar.getInstance(Locale.ENGLISH);
        //    cal.set(year, month - 1, day, hour, minute, second);
        //    d.add(newField("date", DateField.dateToString(cal.getTime()),
        //        Field.Store.YES, Field.Index.NOT_ANALYZED));
        //    iw.addDocument(d);
        //  }

        [Test]
        public void TestStarParsing()
        {
            // final int[] type = new int[1];
            // StandardQueryParser qp = new StandardQueryParser("field", new
            // WhitespaceAnalyzer()) {
            // protected Query getWildcardQuery(String field, String termStr) throws
            // ParseException {
            // // override error checking of superclass
            // type[0]=1;
            // return new TermQuery(new Term(field,termStr));
            // }
            // protected Query getPrefixQuery(String field, String termStr) throws
            // ParseException {
            // // override error checking of superclass
            // type[0]=2;
            // return new TermQuery(new Term(field,termStr));
            // }
            //
            // protected Query getFieldQuery(String field, String queryText) throws
            // ParseException {
            // type[0]=3;
            // return super.getFieldQuery(field, queryText);
            // }
            // };
            //
            // TermQuery tq;
            //
            // tq = (TermQuery)qp.parse("foo:zoo*");
            // assertEquals("zoo",tq.getTerm().text());
            // assertEquals(2,type[0]);
            //
            // tq = (TermQuery)qp.parse("foo:zoo*^2");
            // assertEquals("zoo",tq.getTerm().text());
            // assertEquals(2,type[0]);
            // assertEquals(tq.getBoost(),2,0);
            //
            // tq = (TermQuery)qp.parse("foo:*");
            // assertEquals("*",tq.getTerm().text());
            // assertEquals(1,type[0]); // could be a valid prefix query in the
            // future too
            //
            // tq = (TermQuery)qp.parse("foo:*^2");
            // assertEquals("*",tq.getTerm().text());
            // assertEquals(1,type[0]);
            // assertEquals(tq.getBoost(),2,0);
            //
            // tq = (TermQuery)qp.parse("*:foo");
            // assertEquals("*",tq.getTerm().field());
            // assertEquals("foo",tq.getTerm().text());
            // assertEquals(3,type[0]);
            //
            // tq = (TermQuery)qp.parse("*:*");
            // assertEquals("*",tq.getTerm().field());
            // assertEquals("*",tq.getTerm().text());
            // assertEquals(1,type[0]); // could be handled as a prefix query in the
            // future
            //
            // tq = (TermQuery)qp.parse("(*:*)");
            // assertEquals("*",tq.getTerm().field());
            // assertEquals("*",tq.getTerm().text());
            // assertEquals(1,type[0]);

        }
        [Test]
        public void TestRegexps()
        {
            StandardQueryParser qp = new StandardQueryParser();
            String df = "field";
            RegexpQuery q = new RegexpQuery(new Term("field", "[a-z][123]"));
            assertEquals(q, qp.Parse("/[a-z][123]/", df));
            qp.LowercaseExpandedTerms = (true);
            assertEquals(q, qp.Parse("/[A-Z][123]/", df));
            q.Boost = (0.5f);
            assertEquals(q, qp.Parse("/[A-Z][123]/^0.5", df));
            qp.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
            q.MultiTermRewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
            assertTrue(qp.Parse("/[A-Z][123]/^0.5", df) is RegexpQuery);
            assertEquals(q, qp.Parse("/[A-Z][123]/^0.5", df));
            assertEquals(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE, ((RegexpQuery)qp.Parse("/[A-Z][123]/^0.5", df)).MultiTermRewriteMethod);
            qp.MultiTermRewriteMethod = (MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);

            Query escaped = new RegexpQuery(new Term("field", "[a-z]\\/[123]"));
            assertEquals(escaped, qp.Parse("/[a-z]\\/[123]/", df));
            Query escaped2 = new RegexpQuery(new Term("field", "[a-z]\\*[123]"));
            assertEquals(escaped2, qp.Parse("/[a-z]\\*[123]/", df));

            BooleanQuery complex = new BooleanQuery();
            complex.Add(new RegexpQuery(new Term("field", "[a-z]\\/[123]")), Occur.MUST);
            complex.Add(new TermQuery(new Term("path", "/etc/init.d/")), Occur.MUST);
            complex.Add(new TermQuery(new Term("field", "/etc/init[.]d/lucene/")), Occur.SHOULD);
            assertEquals(complex, qp.Parse("/[a-z]\\/[123]/ AND path:\"/etc/init.d/\" OR \"/etc\\/init\\[.\\]d/lucene/\" ", df));

            Query re = new RegexpQuery(new Term("field", "http.*"));
            assertEquals(re, qp.Parse("field:/http.*/", df));
            assertEquals(re, qp.Parse("/http.*/", df));

            re = new RegexpQuery(new Term("field", "http~0.5"));
            assertEquals(re, qp.Parse("field:/http~0.5/", df));
            assertEquals(re, qp.Parse("/http~0.5/", df));

            re = new RegexpQuery(new Term("field", "boo"));
            assertEquals(re, qp.Parse("field:/boo/", df));
            assertEquals(re, qp.Parse("/boo/", df));


            assertEquals(new TermQuery(new Term("field", "/boo/")), qp.Parse("\"/boo/\"", df));
            assertEquals(new TermQuery(new Term("field", "/boo/")), qp.Parse("\\/boo\\/", df));

            BooleanQuery two = new BooleanQuery();
            two.Add(new RegexpQuery(new Term("field", "foo")), Occur.SHOULD);
            two.Add(new RegexpQuery(new Term("field", "bar")), Occur.SHOULD);
            assertEquals(two, qp.Parse("field:/foo/ field:/bar/", df));
            assertEquals(two, qp.Parse("/foo/ /bar/", df));
        }
        [Test]
        public void TestStopwords()
        {
            StandardQueryParser qp = new StandardQueryParser();
            CharacterRunAutomaton stopSet = new CharacterRunAutomaton(new RegExp("the|foo").ToAutomaton());
            qp.Analyzer = (new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, stopSet));

            Query result = qp.Parse("a:the OR a:foo", "a");
            assertNotNull("result is null and it shouldn't be", result);
            assertTrue("result is not a BooleanQuery", result is BooleanQuery);
            assertTrue(((BooleanQuery)result).Clauses.size() + " does not equal: "
                + 0, ((BooleanQuery)result).Clauses.size() == 0);
            result = qp.Parse("a:woo OR a:the", "a");
            assertNotNull("result is null and it shouldn't be", result);
            assertTrue("result is not a TermQuery", result is TermQuery);
            result = qp.Parse(
                    "(fieldX:xxxxx OR fieldy:xxxxxxxx)^2 AND (fieldx:the OR fieldy:foo)",
                    "a");
            assertNotNull("result is null and it shouldn't be", result);
            assertTrue("result is not a BooleanQuery", result is BooleanQuery);
            if (Verbose)
                Console.WriteLine("Result: " + result);
            assertTrue(((BooleanQuery)result).Clauses.size() + " does not equal: "
                + 2, ((BooleanQuery)result).Clauses.size() == 2);
        }
        [Test]
        public void TestPositionIncrement()
        {
            StandardQueryParser qp = new StandardQueryParser();
            qp.Analyzer = (
                    new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET));

            qp.EnablePositionIncrements = (true);

            String qtxt = "\"the words in poisitions pos02578 are stopped in this phrasequery\"";
            // 0 2 5 7 8
            int[] expectedPositions = { 1, 3, 4, 6, 9 };
            PhraseQuery pq = (PhraseQuery)qp.Parse(qtxt, "a");
            // System.out.println("Query text: "+qtxt);
            // System.out.println("Result: "+pq);
            Term[] t = pq.GetTerms();
            int[] pos = pq.GetPositions();
            for (int i = 0; i < t.Length; i++)
            {
                // System.out.println(i+". "+t[i]+"  pos: "+pos[i]);
                assertEquals("term " + i + " = " + t[i] + " has wrong term-position!",
                    expectedPositions[i], pos[i]);
            }
        }
        [Test]
        public void TestMatchAllDocs()
        {
            StandardQueryParser qp = new StandardQueryParser();
            qp.Analyzer = (new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));

            assertEquals(new MatchAllDocsQuery(), qp.Parse("*:*", "field"));
            assertEquals(new MatchAllDocsQuery(), qp.Parse("(*:*)", "field"));
            BooleanQuery bq = (BooleanQuery)qp.Parse("+*:* -*:*", "field");
            assertTrue(bq.Clauses[0].Query is MatchAllDocsQuery);
            assertTrue(bq.Clauses[1].Query is MatchAllDocsQuery);
        }

        private void AssertHits(int expected, String query, IndexSearcher @is)
        {
            StandardQueryParser qp = new StandardQueryParser();
            qp.Analyzer = (new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));
            qp.Locale = new CultureInfo("en");//  (Locale.ENGLISH);

            Query q = qp.Parse(query, "date");
            ScoreDoc[] hits = @is.Search(q, null, 1000).ScoreDocs;
            assertEquals(expected, hits.Length);
        }


        public override void TearDown()
        {
            BooleanQuery.MaxClauseCount = (originalMaxClauses);
            base.TearDown();
        }

        private sealed class CannedTokenizer : Tokenizer
        {
            private int upto = 0;
            private readonly IPositionIncrementAttribute posIncr;
            private readonly ICharTermAttribute term;

            public CannedTokenizer(TextReader reader)
                        : base(reader)
            {
                posIncr = AddAttribute<IPositionIncrementAttribute>();
                term = AddAttribute<ICharTermAttribute>();
            }


            public override bool IncrementToken()
            {
                ClearAttributes();
                if (upto == 4)
                {
                    return false;
                }
                if (upto == 0)
                {
                    posIncr.PositionIncrement = (1);
                    term.SetEmpty().Append('a');
                }
                else if (upto == 1)
                {
                    posIncr.PositionIncrement = (1);
                    term.SetEmpty().Append('b');
                }
                else if (upto == 2)
                {
                    posIncr.PositionIncrement = (0);
                    term.SetEmpty().Append('c');
                }
                else
                {
                    posIncr.PositionIncrement = (0);
                    term.SetEmpty().Append('d');
                }
                upto++;
                return true;
            }


            public override void Reset()
            {
                base.Reset();
                this.upto = 0;
            }
        }

        private class CannedAnalyzer : Analyzer
        {

            protected internal override TokenStreamComponents CreateComponents(String ignored, TextReader alsoIgnored)
            {
                return new TokenStreamComponents(new CannedTokenizer(alsoIgnored));
            }
        }
        [Test]
        public void TestMultiPhraseQuery()
        {
            Store.Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new CannedAnalyzer()));
            Document doc = new Document();
            doc.Add(NewTextField("field", "", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = DirectoryReader.Open(w, true);
            IndexSearcher s = NewSearcher(r);

            Query q = new StandardQueryParser(new CannedAnalyzer()).Parse("\"a\"", "field");
            assertTrue(q is MultiPhraseQuery);
            assertEquals(1, s.Search(q, 10).TotalHits);
            r.Dispose();
            w.Dispose();
            dir.Dispose();
        }
        [Test]
        public void TestRegexQueryParsing()
        {
            String[]
            fields = { "b", "t" };

            StandardQueryParser parser = new StandardQueryParser();
            parser.SetMultiFields(fields);
            parser.DefaultOperator = (StandardQueryConfigHandler.Operator.AND);
            parser.Analyzer = (new MockAnalyzer(Random));

            BooleanQuery exp = new BooleanQuery();
            exp.Add(new BooleanClause(new RegexpQuery(new Term("b", "ab.+")), Occur.SHOULD));//TODO spezification? was "MUST"
            exp.Add(new BooleanClause(new RegexpQuery(new Term("t", "ab.+")), Occur.SHOULD));//TODO spezification? was "MUST"

            assertEquals(exp, parser.Parse("/ab.+/", null));

            RegexpQuery regexpQueryexp = new RegexpQuery(new Term("test", "[abc]?[0-9]"));

            assertEquals(regexpQueryexp, parser.Parse("test:/[abc]?[0-9]/", null));

        }

    }
}
